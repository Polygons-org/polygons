using Godot;
using System;

namespace Modding {
    public interface IMod {
        string Id { get; }
        string Name { get; }
        string Version { get; }
        bool Enabled { get; set; }

        void OnLoad();
    }
}