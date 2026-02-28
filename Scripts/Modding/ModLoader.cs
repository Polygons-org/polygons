using System;
using Godot;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using System.Reflection;
using System.Collections.Generic;

public partial class ModLoader : Node {
    public override void _Ready() {
        AppDomain.CurrentDomain.AssemblyResolve += ResolveAssembly;
        LoadAllMods();
    }

    Assembly ResolveAssembly(object sender, ResolveEventArgs args)
    {
        var name = new AssemblyName(args.Name).Name;
        
        // Search already-loaded assemblies first
        foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
        {
            if (asm.GetName().Name == name)
                return asm;
        }

        // Then search disk locations of loaded assemblies
        foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
        {
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
        var seenDirs = new HashSet<string>();

        foreach (var asm in AppDomain.CurrentDomain.GetAssemblies()) {
            var loc = asm.Location;
            if (string.IsNullOrEmpty(loc) || !System.IO.File.Exists(loc)) continue;

            // Only grab managed DLLs, skip native ones that cause CS0009
            try {
                refs.Add(MetadataReference.CreateFromFile(loc));
                GD.Print("Added ref: " + loc);
            }
            catch { }
        }

        return refs;
    }
}