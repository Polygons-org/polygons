using Godot;
using System;

public partial class Editor : Node2D {
    public override void _Ready() {
        GetNode<Sprite2D>($"Background").Modulate = new Color(Globals.BackGroundRed / 255f, Globals.BackGroundGreen / 255f, Globals.BackGroundBlue / 255f);

        for (int i = 0; i <= 8; i++) {
            GetNode<Sprite2D>($"Ground/Ground{i}").Modulate = new Color(Globals.GroundRed / 255f, Globals.GroundGreen / 255f, Globals.GroundBlue / 255f);
        }
    }
}
