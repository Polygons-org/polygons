using Godot;
using System;

public partial class EditorObjects : Node2D {
    public override void _Process(double delta) {
        var globalMousePos = GetGlobalMousePosition();
        var mousePos = GetViewport().GetMousePosition();
        bool mouseOverUI = GetViewport().GuiGetHoveredControl() != null;

        if (Input.IsActionJustPressed("PlaceObject") && mousePos.Y < 680f && !mouseOverUI) {
            float gridSize = 120f;
            Vector2 gridOrigin = new Vector2(0, 666);

            Vector2 snappedPos = new Vector2(
                Mathf.Round((globalMousePos.X - gridOrigin.X) / gridSize) * gridSize + gridOrigin.X,
                Mathf.Round((globalMousePos.Y - gridOrigin.Y) / gridSize) * gridSize + gridOrigin.Y
            );

            PackedScene scene = GD.Load<PackedScene>("res://Prefabs/Object.tscn");
            var newObject = scene.Instantiate<Node2D>();
            newObject.GetNode<Sprite2D>("Sprite2D").Texture =
                GD.Load<Texture2D>("res://Assets/Textures/Objects/" + Globals.SelectedObject + ".png");

            newObject.Position = snappedPos;
            AddChild(newObject);
        }
    }
}