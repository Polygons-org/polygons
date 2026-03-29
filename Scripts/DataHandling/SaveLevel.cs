using Godot;
using System;
using System.Linq;

public partial class SaveLevel : Node {
    public static string SaveLevelToData(Node context) {
        string data = "";
        Node2D Objects = context.GetNode<Node2D>("/root/Editor/Objects");

        string BaseTexturePath = "res://Assets/Textures/Objects/";

        foreach (Node2D obj in Objects.GetChildren().Cast<Node2D>()) {
            string ObjectType = obj.GetNode<Sprite2D>("Sprite2D").Texture.ResourcePath.Replace(BaseTexturePath, "").Replace(".png", "");
            data += "pos:" + obj.Position.X + "," + obj.Position.Y + ";" +
            "rot:" + obj.RotationDegrees + ";" +
            "type:" + ObjectType + ";;";
        }

        data = data[..^2];

        return data;
    }
}