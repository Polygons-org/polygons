using Godot;
using System;

public partial class ObjectButton : Button {
    // In the inspector name this the same as the path to the sprite of the object.
    // Specifically from res://Assets/Textures/Objects/ without the .png at the end.
    // For example, if the sprite is at res://Assets/Textures/Objects/Blocks/block.png, name this Blocks/block.
    [Export] public string ObjectName = "Blocks/block";

    public override void _Ready() {
        Pressed += _on_pressed;
    }

    public void _on_pressed() {
        Globals.SelectedObject = ObjectName;
    }
}
