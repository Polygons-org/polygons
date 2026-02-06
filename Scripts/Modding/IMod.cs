using Godot;

namespace Modding {
    public interface IModNode {
        void Ready(Node proxy);
        void Process(Node proxy, double delta);
    }

    public interface IMod {
        string ModId { get; }
        string ModName { get; }
        string ModVersion { get; }
        bool ModEnabled { get; set; }

        void OnLoad();
    }

    public partial class ModNodeProxy : Node {
        public IModNode ModLogic { get; set; } = null!;

        public override void _Ready() {
            ModLogic.Ready(this); // pass this Node so the mod can safely call GetTree(), AddChild(), etc.
        }

        public override void _Process(double delta) {
            ModLogic.Process(this, delta);
        }
    }
}