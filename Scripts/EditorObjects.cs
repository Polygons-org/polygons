using Godot;
using System;

public partial class EditorObjects : Node2D {
    public override void _Process(double delta) {
        if (Input.IsActionJustPressed("PlaceObject")) {
            var MousePos = GetGlobalMousePosition();
            PackedScene Object = GD.Load<PackedScene>("res://Prefabs/Object.tscn");
            var NewObject = Object.Instantiate<Node2D>();
            NewObject.Position = MousePos;
            AddChild(NewObject);
        }
    }
}
