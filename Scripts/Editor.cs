using Godot;
using System;

public partial class Editor : Node2D {
    public override void _Ready() {
        GetNode<Sprite2D>($"Background").Modulate = new Color(Globals.BackGroundRed / 255f, Globals.BackGroundGreen / 255f, Globals.BackGroundBlue / 255f);
    }
}
