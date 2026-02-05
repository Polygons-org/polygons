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
            if (!file.EndsWith(".dll"))
                continue;

            LoadDll($"{modsPath}/{file}");
        }
    }

    private void LoadDll(string path) {
        try {
            var bytes = FileAccess.GetFileAsBytes(path);
            var asm = System.Reflection.Assembly.Load(bytes);

            foreach (var type in asm.GetTypes()) {
                if (!typeof(Modding.IMod).IsAssignableFrom(type))
                    continue;

                var mod = (Modding.IMod)Activator.CreateInstance(type);
                mod.OnLoad();
            }
        }
        catch (Exception e) {
            GD.PrintErr($"Failed to load mod {path}: {e}");
        }
    }
}