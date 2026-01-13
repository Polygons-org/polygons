using Godot;
using System;
using System.Security.AccessControl;

public partial class Editor : Node2D {
    int CameraSpeed = 10;

    public override void _Ready() {
        GetNode<Sprite2D>($"Background").Modulate = new Color(Globals.BackGroundRed / 255f, Globals.BackGroundGreen / 255f, Globals.BackGroundBlue / 255f);

        for (int i = 0; i <= 8; i++) {
            GetNode<Sprite2D>($"Ground/Ground{i}").Modulate = new Color(Globals.GroundRed / 255f, Globals.GroundGreen / 255f, Globals.GroundBlue / 255f);
        }
    }

    public override void _Process(double delta) {
        if (Input.IsKeyPressed(Key.Shift)) {
            CameraSpeed = 30;
        } else {
            CameraSpeed = 10;
        }

        if (Input.IsKeyPressed(Key.W)) {
            GetNode<Camera2D>("Camera2D").Position += new Vector2(0, -CameraSpeed);
            GetNode<ColorRect>("StartLine").Position += new Vector2(0, -CameraSpeed);
            GetNode<Sprite2D>("Background").Position += new Vector2(0, -CameraSpeed);
        } if (Input.IsKeyPressed(Key.S)) {
            GetNode<Camera2D>("Camera2D").Position += new Vector2(0, CameraSpeed);
            GetNode<ColorRect>("StartLine").Position += new Vector2(0, CameraSpeed);
            GetNode<Sprite2D>("Background").Position += new Vector2(0, CameraSpeed);
        } if (Input.IsKeyPressed(Key.A)) {
            GetNode<Camera2D>("Camera2D").Position += new Vector2(-CameraSpeed, 0);
            GetNode<Node2D>("Ground").Position += new Vector2(-CameraSpeed, 0);
            GetNode<Sprite2D>("Background").Position += new Vector2(-CameraSpeed, 0);
        } if (Input.IsKeyPressed(Key.D)) {
            GetNode<Camera2D>("Camera2D").Position += new Vector2(CameraSpeed, 0);
            GetNode<Node2D>("Ground").Position += new Vector2(CameraSpeed, 0);
            GetNode<Sprite2D>("Background").Position += new Vector2(CameraSpeed, 0);
        }
    }
}
