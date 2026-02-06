using Godot;
using System;

namespace Modding {
    public interface IMod {
        string ModId { get; }
        string ModName { get; }
        string ModVersion { get; }
        bool ModEnabled { get; set; }

        void OnLoad();
    }
}