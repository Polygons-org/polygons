using System;
using Godot;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using System.Reflection;
using System.Collections.Generic;
using System.Reflection.Metadata;
using System.Linq;

public partial class ModLoader : Node {
    public override void _Ready() {
        AppDomain.CurrentDomain.AssemblyResolve += ResolveAssembly;
        GetTree().NodeAdded += OnNodeAdded;
        LoadAllMods();
    }

    public static Dictionary<string, List<Action<Node>>> OnNodeReady = new();

    public static void RegisterNodeCallback(string key, Action<Node> callback)
    {
        if (!OnNodeReady.ContainsKey(key))
            OnNodeReady[key] = new List<Action<Node>>();
        OnNodeReady[key].Add(callback);

        // If the node already exists in the tree, fire immediately
        var node = ((SceneTree)Engine.GetMainLoop()).Root.GetNodeOrNull(key);
        if (node != null)
            callback(node);
    }

    void OnNodeAdded(Node node) {
        var typeName = node.GetType().Name;
        if (OnNodeReady.TryGetValue(typeName, out var callbacks))
            foreach (var cb in callbacks) cb(node);

        Callable.From(() => {
            var path = node.GetPath().ToString();
            if (OnNodeReady.TryGetValue(path, out var pathCallbacks))
                foreach (var cb in pathCallbacks) cb(node);
        }).CallDeferred();
    }

    static byte[] GetAssemblyBytes(Assembly asm) {
        // Use the unsafe accessor to get raw PE bytes from a loaded in-memory assembly
        var module = asm.Modules.First();
        // Trick: re-serialize via MetadataReader path
        unsafe {
            asm.TryGetRawMetadata(out byte* blob, out int length);
            var bytes = new byte[length];
            System.Runtime.InteropServices.Marshal.Copy((IntPtr)blob, bytes, 0, length);
            return bytes;
        }
    }

    Assembly ResolveAssembly(object sender, ResolveEventArgs args) {
        var name = new AssemblyName(args.Name).Name;

        // Search already-loaded assemblies first
        foreach (var asm in AppDomain.CurrentDomain.GetAssemblies()) {
            if (asm.GetName().Name == name)
                return asm;
        }

        // Then search disk locations of loaded assemblies
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

    void LoadAllMods() {
        var dir = DirAccess.Open("user://mods/");
        if (dir == null) return;
        dir.ListDirBegin();
        string entry;
        while ((entry = dir.GetNext()) != "") {
            if (dir.CurrentIsDir() && entry != "." && entry != "..")
                LoadMod("user://mods/" + entry);
        }
    }

    void LoadMod(string modPath) {
        var manifestPath = modPath + "/mod.json";
        if (!FileAccess.FileExists(manifestPath)) return;
        var manifest = Json.ParseString(FileAccess.GetFileAsString(manifestPath)).AsGodotDictionary();

        var sources = new List<SyntaxTree>();
        var dir = DirAccess.Open(modPath);
        dir.ListDirBegin();
        string fileName;
        while ((fileName = dir.GetNext()) != "") {
            if (fileName.EndsWith(".cs")) {
                var code = FileAccess.GetFileAsString(modPath + "/" + fileName);
                sources.Add(CSharpSyntaxTree.ParseText(code, path: fileName));
            }
        }

        var refs = GetMetadataReferences();
        if (refs == null) return;

        var compilation = CSharpCompilation.Create(
            assemblyName: manifest["name"].ToString(),
            syntaxTrees: sources,
            references: refs,
            options: new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary)
        );

        using var ms = new System.IO.MemoryStream();
        var result = compilation.Emit(ms);

        if (!result.Success) {
            foreach (var diag in result.Diagnostics)
                GD.PrintErr($"[{manifest["name"]}] {diag}");
            return;
        }

        ms.Seek(0, System.IO.SeekOrigin.Begin);
        var assembly = Assembly.Load(ms.ToArray());
        var modClass = assembly.GetType("Mod");
        var instance = Activator.CreateInstance(modClass);
        modClass.GetMethod("Init")?.Invoke(instance, new object[] { GetTree().Root });
    }

    List<MetadataReference> GetMetadataReferences() {
        var refs = new List<MetadataReference>();

        foreach (var asm in AppDomain.CurrentDomain.GetAssemblies()) {
            try {
                var loc = asm.Location;
                if (!string.IsNullOrEmpty(loc) && System.IO.File.Exists(loc)) {
                    refs.Add(MetadataReference.CreateFromFile(loc));
                } else {
                    unsafe {
                        if (asm.TryGetRawMetadata(out byte* blob, out int length)) {
                            var moduleMetadata = ModuleMetadata.CreateFromMetadata((IntPtr)blob, length);
                            var assemblyMetadata = AssemblyMetadata.Create(moduleMetadata);
                            refs.Add(assemblyMetadata.GetReference());
                        }
                    }
                }
            }
            catch { }
        }

        return refs;
    }
}