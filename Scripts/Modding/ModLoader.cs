using Godot;
using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Loader;

public partial class ModLoader : Node {
    public override void _EnterTree() {
        // Assembly resolver (for shared dependencies)
        AppDomain.CurrentDomain.AssemblyResolve += (sender, args) => {
            string name = new AssemblyName(args.Name).Name;
            if (name == "Polygons") // never resolve the main game assembly
                return null;

            string modsPath = ProjectSettings.GlobalizePath("user://mods");
            string dllPath = Path.Combine(modsPath, name + ".dll");
            if (File.Exists(dllPath))
                return Assembly.LoadFrom(dllPath);

            return null;
        };

        LoadMods();
    }

    private void LoadMods() {
        string modsPath = ProjectSettings.GlobalizePath("user://mods");
        GD.Print($"Looking for mods in: {modsPath}");

        if (!DirAccess.DirExistsAbsolute(modsPath)) {
            var error = DirAccess.MakeDirRecursiveAbsolute(modsPath);
            if (error != Error.Ok) {
                GD.PrintErr($"Failed to create mods directory: {error}");
                return;
            }
        }

        using var dir = DirAccess.Open(modsPath);
        if (dir == null) {
            GD.PrintErr($"Failed to open mods directory: {modsPath}");
            return;
        }

        dir.ListDirBegin();
        string fileName = dir.GetNext();
        while (fileName != "") {
            if (!dir.CurrentIsDir() && fileName.EndsWith(".dll")) {
                string fullPath = Path.Combine(modsPath, fileName);
                LoadDll(fullPath);
            }
            fileName = dir.GetNext();
        }
        dir.ListDirEnd();
    }

    private void LoadDll(string path) {
        GD.Print($"Attempting to load mod from: {path}");

        try {
            Assembly asm = AssemblyLoadContext.Default.LoadFromAssemblyPath(path);
            GD.Print($"Assembly loaded: {asm.FullName}");

            Type[] types = asm.GetTypes();
            GD.Print($"Found {types.Length} types in assembly");

            foreach (var type in types) {
                if (type == null || type.IsAbstract || type.IsInterface)
                    continue;

                // Check for IModNode first (Node-based mod)
                if (typeof(Modding.IModNode).IsAssignableFrom(type)) {
                    GD.Print($"✓ Found Node mod: {type.FullName}");
                    var modLogic = (Modding.IModNode)Activator.CreateInstance(type)!;

                    var proxy = new Modding.ModNodeProxy() {
                        ModLogic = modLogic
                    };

                    GetTree().Root.AddChild(proxy);
                    GD.Print("Node mod added to scene tree");

                    continue;
                }

                // Otherwise, check for plain IMod
                if (typeof(Modding.IMod).IsAssignableFrom(type)) {
                    GD.Print($"✓ Found plain mod: {type.FullName}");
                    var modInstance = (Modding.IMod)Activator.CreateInstance(type)!;
                    modInstance.OnLoad();
                    GD.Print($"Loaded mod {modInstance.ModName} ({modInstance.ModId})");
                }
            }
        }
        catch (Exception e) {
            GD.PrintErr($"Failed to load mod {path}: {e}");
        }
    }
}