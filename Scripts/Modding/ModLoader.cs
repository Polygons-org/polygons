using System;
using Godot;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using System.Reflection;
using System.Collections.Generic;
using System.Reflection.Metadata;

// ModLoader is a Godot Node that handles loading, compiling, and running mods at runtime.
// Mods are folders inside "user://mods/", each containing a mod.json manifest and .cs source files.
// The mod source files are compiled in-memory using Roslyn (Microsoft.CodeAnalysis),
// then loaded as assemblies and initialized by calling their Mod.Init() method.
public partial class ModLoader : Node {
    // Called when the node enters the scene tree.
    // Sets up assembly resolution, hooks into the node-added signal, then loads all mods.
    public override void _Ready() {
        // Register a fallback handler for when .NET can't find an assembly on its own.
        // This lets us redirect lookups to already-loaded assemblies or known disk locations.
        AppDomain.CurrentDomain.AssemblyResolve += ResolveAssembly;

        // Hook into the scene tree so we can notify mods whenever a new node is added.
        GetTree().NodeAdded += OnNodeAdded;

        LoadAllMods();
    }

    // A registry that maps a node type name or scene path to a list of callbacks.
    // Mods can register callbacks here to be notified when a specific node enters the tree.
    public static Dictionary<string, List<Action<Node>>> OnNodeReady = new();

    // Allows mods to subscribe to a node by its type name or scene path.
    // If the node is already in the tree when this is called, the callback fires immediately.
    public static void RegisterNodeCallback(string key, Action<Node> callback) {
        // Create the list for this key if it doesn't exist yet, then add the callback.
        if (!OnNodeReady.ContainsKey(key))
            OnNodeReady[key] = new List<Action<Node>>();
        OnNodeReady[key].Add(callback);

        // If a node matching this key is already in the scene tree, fire the callback right away
        // so mods that register late don't miss nodes that are already present.
        var node = ((SceneTree)Engine.GetMainLoop()).Root.GetNodeOrNull(key);
        if (node != null)
            callback(node);
    }

    // Called automatically by Godot every time any node is added to the scene tree.
    // Checks both the node's type name and its full scene path against registered callbacks.
    void OnNodeAdded(Node node) {
        // First, check if any callbacks are registered for this node's class name (e.g. "Player").
        var typeName = node.GetType().Name;
        if (OnNodeReady.TryGetValue(typeName, out var callbacks))
            foreach (var cb in callbacks)
                cb(node);

        // Path-based lookups are deferred because the node's full scene path
        // isn't finalized until the end of the current frame.
        Callable.From(() => {
            var path = node.GetPath().ToString();
            if (OnNodeReady.TryGetValue(path, out var pathCallbacks))
                foreach (var cb in pathCallbacks)
                    cb(node);
        }).CallDeferred();
    }

    // Reads the raw PE (Portable Executable) bytes from an already-loaded in-memory assembly.
    // This is needed to create Roslyn MetadataReferences for assemblies that have no file on disk,
    // such as assemblies that were themselves loaded from a MemoryStream (e.g. other compiled mods).
    static byte[] GetAssemblyBytes(Assembly asm) {
        unsafe {
            // TryGetRawMetadata gives us a pointer directly into the assembly's in-memory metadata.
            asm.TryGetRawMetadata(out byte* blob, out int length);

            // Copy the raw bytes into a managed array so we can work with them safely.
            var bytes = new byte[length];
            System.Runtime.InteropServices.Marshal.Copy((IntPtr)blob, bytes, 0, length);
            return bytes;
        }
    }

    // Fallback handler for .NET's assembly resolution system.
    // Fires when the runtime can't find a required assembly through its normal search paths.
    Assembly ResolveAssembly(object sender, ResolveEventArgs args) {
        var name = new AssemblyName(args.Name).Name;

        // Check if the assembly is already loaded in the current AppDomain before touching disk.
        foreach (var asm in AppDomain.CurrentDomain.GetAssemblies()) {
            if (asm.GetName().Name == name)
                return asm;
        }

        // If not already loaded, look for a .dll next to any of the currently loaded assemblies.
        foreach (var asm in AppDomain.CurrentDomain.GetAssemblies()) {
            var loc = asm.Location;
            if (string.IsNullOrEmpty(loc)) continue;

            var dir = System.IO.Path.GetDirectoryName(loc);
            var candidate = System.IO.Path.Combine(dir, name + ".dll");

            if (System.IO.File.Exists(candidate))
                return Assembly.LoadFrom(candidate);
        }

        GD.PrintErr($"Could not resolve assembly: {args.Name}");
        return null;
    }

    // Scans the "user://mods/" directory and attempts to load each subdirectory as a mod.
    void LoadAllMods() {
        var dir = DirAccess.Open("user://mods/");

        // If the mods folder doesn't exist yet, there's nothing to do.
        if (dir == null) return;

        dir.ListDirBegin();
        string entry;

        while ((entry = dir.GetNext()) != "") {
            // Only descend into subdirectories, and skip the special "." and ".." entries.
            if (dir.CurrentIsDir() && entry != "." && entry != "..")
                LoadMod("user://mods/" + entry);
        }
    }

    // Loads a single mod from a folder path.
    // Expects a mod.json manifest and one or more .cs source files inside the folder.
    // The source files are compiled at runtime using Roslyn and executed immediately.
    void LoadMod(string modPath) {
        var manifestPath = modPath + "/mod.json";

        // A mod.json manifest is required — skip folders that don't have one.
        if (!FileAccess.FileExists(manifestPath)) return;

        // Parse the manifest to get mod metadata (currently we use "name" for the assembly name).
        var manifest = Json.ParseString(FileAccess.GetFileAsString(manifestPath)).AsGodotDictionary();

        // Collect and parse all .cs files in the mod folder into Roslyn syntax trees.
        var sources = new List<SyntaxTree>();
        var dir = DirAccess.Open(modPath);
        dir.ListDirBegin();
        string fileName;

        while ((fileName = dir.GetNext()) != "") {
            if (fileName.EndsWith(".cs")) {
                var code = FileAccess.GetFileAsString(modPath + "/" + fileName);

                // ParseText turns raw source code into a syntax tree Roslyn can compile.
                // We pass the filename so error messages include the correct file name.
                sources.Add(CSharpSyntaxTree.ParseText(code, path: fileName));
            }
        }

        // Build the list of assemblies the mod is allowed to reference (i.e. everything loaded so far).
        var refs = GetMetadataReferences();
        if (refs == null) return;

        // Set up a Roslyn compilation targeting a DLL (in-memory, no file output).
        var compilation = CSharpCompilation.Create(
            assemblyName: manifest["name"].ToString(),
            syntaxTrees: sources,
            references: refs,
            options: new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary)
        );

        // Emit (compile) the assembly into a MemoryStream instead of writing to disk.
        using var ms = new System.IO.MemoryStream();
        var result = compilation.Emit(ms);

        // If compilation failed, print each error and bail out without loading the mod.
        if (!result.Success) {
            foreach (var diag in result.Diagnostics)
                GD.PrintErr($"[{manifest["name"]}] {diag}");
            return;
        }

        // Load the compiled bytes as a live assembly in the current AppDomain.
        ms.Seek(0, System.IO.SeekOrigin.Begin);
        var assembly = Assembly.Load(ms.ToArray());

        // Every mod must define a class called "Mod" with an Init(Node) method.
        // We find it by name, instantiate it, and call Init with the scene root.
        var modClass = assembly.GetType("Mod");
        var instance = Activator.CreateInstance(modClass);
        modClass.GetMethod("Init")?.Invoke(instance, new object[] { GetTree().Root });
    }

    // Builds a list of Roslyn MetadataReferences from every assembly currently loaded in the AppDomain.
    // This gives mod code access to Godot, the base game, and any previously loaded mods.
    List<MetadataReference> GetMetadataReferences() {
        var refs = new List<MetadataReference>();

        foreach (var asm in AppDomain.CurrentDomain.GetAssemblies()) {
            try {
                var loc = asm.Location;

                if (!string.IsNullOrEmpty(loc) && System.IO.File.Exists(loc)) {
                    // Preferred path: assembly has a real file on disk, so just point Roslyn at it.
                    refs.Add(MetadataReference.CreateFromFile(loc));
                } else {
                    // Fallback for in-memory assemblies (e.g. other compiled mods):
                    // read the raw metadata bytes and build a reference from them directly.
                    unsafe {
                        if (asm.TryGetRawMetadata(out byte* blob, out int length)) {
                            var moduleMetadata = ModuleMetadata.CreateFromMetadata((IntPtr)blob, length);
                            var assemblyMetadata = AssemblyMetadata.Create(moduleMetadata);
                            refs.Add(assemblyMetadata.GetReference());
                        }
                    }
                }
            }
            catch { }  // Skip any assembly we can't read metadata from.
        }

        return refs;
    }
}