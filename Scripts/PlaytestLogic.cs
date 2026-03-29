using Godot;
using System;

public partial class PlaytestLogic : Control {
    public void on_playtest_pressed() {
        Globals.LevelData = SaveLevel.SaveLevelToData(this);
    }
}
