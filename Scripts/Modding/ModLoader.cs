using Godot;
using System;

// This whole thing is basically written with chatgpt
// I didn't feel like making dll loader shit
public partial class ModLoader : Node {
    public override void _EnterTree() {
        LoadMods();
    }

    private void LoadMods() {
        var modsPath = "user://mods";
        DirAccess.MakeDirRecursiveAbsolute(modsPath);

        foreach (var file in DirAccess.GetFilesAt(modsPath)) {
            if (!file.EndsWith(".dll")) {
                GD.Print($"{file} is not a dll, skipping");
                continue;
            }

            LoadDll($"{modsPath}/{file}");
        }
    }

    private void LoadDll(string path) {
        GD.Print($"Loading mod from {path}");
        try {
            GD.Print("Reading dll bytes");
            var bytes = FileAccess.GetFileAsBytes(path);
            var asm = System.Reflection.Assembly.Load(bytes);
            GD.Print("DLL loaded successfully");

            Type[] types;
            try {
                types = asm.GetTypes();
            }
            catch (System.Reflection.ReflectionTypeLoadException ex) {
                GD.PrintErr("Failed to get types from assembly:");
                foreach (var t in ex.Types) GD.Print(t?.FullName ?? "<null>");
                foreach (var loaderEx in ex.LoaderExceptions) GD.PrintErr(loaderEx);
                return;
            }

            foreach (var type in types) {
                GD.Print($"Found type: {type.FullName}");
                if (!typeof(Modding.IMod).IsAssignableFrom(type))
                    continue;

                GD.Print("Creating mod instance");
                var modInstance = (Modding.IMod)Activator.CreateInstance(type);

                // If the mod is a node then this adds it to the root
                if (modInstance is Node nodeMod) {
                    GetTree().Root.AddChild(nodeMod);
                    GD.Print("Added mod to root");
                } else {
                    GD.Print("Mod is not a node, skipping adding to root");
                }

                modInstance.OnLoad();
            }
        }
        catch (Exception e) {
            GD.PrintErr($"Failed to load mod {path}: {e}");
        }
    }
}