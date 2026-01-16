using Godot;
using System;

public partial class OutdatedVersionLogic : Control {
    public override void _Ready() {
        if (Globals.AccessLevel == "client") {
            GetNode<Label>("Header/SecondaryText").Text = "You can not access the servers!";
        } else if (Globals.AccessLevel == "none") {
            GetNode<Button>("Buttons/ContinueButton").Visible = false;
        }
    }
}