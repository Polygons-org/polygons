using Godot;
using System;

public partial class Game : Node2D {
    public override void _Process(double delta) {

        for (int i = 0; i <= 8; i++) {
            GetNode<Sprite2D>($"Ground/Texture/Ground{i}").Modulate = new Color(Globals.GroundRed / 255f, Globals.GroundGreen / 255f, Globals.GroundBlue / 255f);
        }


        for (int i = 0; i <= 2; i++) {
            GetNode<Sprite2D>($"Background/BG{i}").Modulate = new Color(Globals.BackGroundRed / 255f, Globals.BackGroundGreen / 255f, Globals.BackGroundBlue / 255f);
        }

        // Loops the ground
        for (int i = 0; i <= 8; i++) {
            Sprite2D Sprite = GetNode<Sprite2D>($"Ground/Texture/Ground{i}");

            if (Sprite.GlobalPosition.X < GetNode<Camera2D>("Camera2D").GlobalPosition.X - 1260) {
                Sprite.GlobalPosition += new Vector2((281.6f * 9f) - (7 * 9), 0);
            }
        }

        // Loops the bg
        for (int i = 0; i <= 2; i++) {
            Sprite2D Sprite = GetNode<Sprite2D>($"Background/BG{i}");

            if (Sprite.GlobalPosition.X < GetNode<Camera2D>("Camera2D").GlobalPosition.X - 1920) {
                Sprite.GlobalPosition += new Vector2(1920 * 2, 0);
            }
        }

        GetNode<Node2D>("Background").Position += new Vector2(600.0f * (float)delta, 0);
    }
}
