using Godot;
using System;

public partial class EditorObjects : Node2D {
    public override void _Process(double delta) {
        var MousePos = GetGlobalMousePosition();
        bool mouseOverUI = GetViewport().GuiGetHoveredControl() != null;

        if (Input.IsActionJustPressed("PlaceObject") && MousePos.Y < 680f && !mouseOverUI) {
            PackedScene Object = GD.Load<PackedScene>("res://Prefabs/Object.tscn");
            var NewObject = Object.Instantiate<Node2D>();
            NewObject.GetNode<Sprite2D>("Sprite2D").Texture = GD.Load<Texture2D>("res://Assets/Textures/Objects/" + Globals.SelectedObject + ".png");
            NewObject.Position = MousePos;
            AddChild(NewObject);
        }
    }
}