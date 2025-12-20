using Godot;
using System;

public partial class Game : Node2D {
    public override void _Process(double delta) {
        // Sets the ground color
        int GroundRed = 0;
        int GroundGreen = 0;
        int GroundBlue = 100;

        for (int i = 0; i <= 8; i++) {
            GetNode<Sprite2D>($"Ground/Texture/Ground{i}").Modulate = new Color(GroundRed / 255f, GroundGreen / 255f, GroundBlue / 255f);
        }

        for (int i = 0; i <= 8; i++) {
            Sprite2D Sprite = GetNode<Sprite2D>($"Ground/Texture/Ground{i}");

            if (Sprite.GlobalPosition.X < GetNode<Camera2D>("Camera2D").GlobalPosition.X - 1260) {
                Sprite.GlobalPosition += new Vector2((281.6f * 9f) - (7 * 9), 0);
            }
        }
    }
}
