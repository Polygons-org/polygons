using Godot;
using System;
using System.Reflection;
using System.IO;
using System.Linq;

public partial class ModLoader : Node {
    public override void _EnterTree() {
        // Handle assembly resolution for shared dependencies
        AppDomain.CurrentDomain.AssemblyResolve += (sender, args) => {
            string assemblyName = new AssemblyName(args.Name).Name;

            // Try to resolve from mods directory first
            string modsPath = ProjectSettings.GlobalizePath("user://mods");
            if (Directory.Exists(modsPath)) {
                string dllPath = Path.Combine(modsPath, assemblyName + ".dll");
                if (File.Exists(dllPath)) {
                    return Assembly.LoadFrom(dllPath);
                }
            }

            // Try to resolve from app directory
            string appDir = OS.GetExecutablePath().GetBaseDir();
            string appDllPath = Path.Combine(appDir, assemblyName + ".dll");
            if (File.Exists(appDllPath)) {
                return Assembly.LoadFrom(appDllPath);
            }

            return null;
        };

        LoadMods();
    }

    private void LoadMods() {
        string modsPath = ProjectSettings.GlobalizePath("user://mods");
        GD.Print($"Looking for mods in: {modsPath}");

        // Create directory if it doesn't exist
        if (!DirAccess.DirExistsAbsolute(modsPath)) {
            Error error = DirAccess.MakeDirRecursiveAbsolute(modsPath);
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
            byte[] assemblyBytes = File.ReadAllBytes(path);
            Assembly asm = Assembly.Load(assemblyBytes);
            GD.Print($"Assembly loaded: {asm.FullName}");

            Type[] types = asm.GetTypes();
            GD.Print($"Found {types.Length} types in assembly");

            // Get the IMod type we're checking against
            Type imodType = typeof(Modding.IMod);
            GD.Print($"IMod interface location: {imodType.Assembly.Location}");
            GD.Print($"IMod full name: {imodType.FullName}");

            // Try to find IMod in the mod's assembly
            Type modImodType = asm.GetType("Modding.IMod", false);
            if (modImodType != null) {
                GD.Print($"Found IMod in mod assembly: {modImodType.FullName}");
                GD.Print($"Same type? {imodType == modImodType}");
                GD.Print($"Same assembly? {imodType.Assembly == modImodType.Assembly}");
            } else {
                GD.Print("WARNING: IMod NOT found in mod assembly!");
            }

            bool foundMod = false;
            foreach (var type in types) {
                if (type == null || type.IsAbstract || type.IsInterface) continue;

                GD.Print($"\n--- Checking type: {type.FullName} ---");

                // Check all interfaces
                var interfaces = type.GetInterfaces();
                GD.Print($"Implements {interfaces.Length} interfaces:");
                foreach (var iface in interfaces) {
                    GD.Print($"  - {iface.FullName}");
                    GD.Print($"    Assembly: {iface.Assembly.GetName().Name}");
                }

                // Check if type implements IMod
                bool implementsIMod = imodType.IsAssignableFrom(type);
                GD.Print($"imodType.IsAssignableFrom(type): {implementsIMod}");

                if (implementsIMod) {
                    GD.Print($"✓ Found mod class: {type.FullName}");

                    try {
                        var modInstance = Activator.CreateInstance(type) as Modding.IMod;
                        if (modInstance != null) {
                            foundMod = true;
                            GD.Print($"Mod instance created: Id={modInstance.ModId}, Name={modInstance.ModName}");

                            modInstance.OnLoad();

                            if (modInstance is Node nodeMod) {
                                GetTree().Root.AddChild(nodeMod);
                                GD.Print("Mod added to scene tree");
                            }
                        }
                    }
                    catch (Exception ex) {
                        GD.PrintErr($"Failed to instantiate mod: {ex}");
                    }
                } else {
                    GD.Print($"✗ Type does not implement IMod");

                    // Additional check - does it have the required members?
                    GD.Print("Checking for IMod members:");
                    var hasOnLoad = type.GetMethod("OnLoad") != null;
                    var hasId = type.GetProperty("ModId") != null;
                    var hasModName = type.GetProperty("ModName") != null;
                    var hasVersion = type.GetProperty("ModVersion") != null;
                    var hasEnabled = type.GetProperty("ModEnabled") != null;

                    GD.Print($"  Has OnLoad method: {hasOnLoad}");
                    GD.Print($"  Has Id property: {hasId}");
                    GD.Print($"  Has Name property: {hasModName}");
                    GD.Print($"  Has Version property: {hasVersion}");
                    GD.Print($"  Has Enabled property: {hasEnabled}");
                }
            }

            if (!foundMod) {
                GD.PrintErr($"ERROR: No valid mod found in {Path.GetFileName(path)}");
            }
        }
        catch (Exception e) {
            GD.PrintErr($"Failed to load mod {path}: {e}");
        }
    }
}