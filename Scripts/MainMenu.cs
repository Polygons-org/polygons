using Godot;
using System;
using System.Text;
using System.Collections.Generic;

public partial class MainMenu : Control {
    public override void _Ready() {
        Label VersionLabel = GetNode<Label>("VersionText/ClientVersion");
        string GameVersion = ProjectSettings.GetSetting("application/config/version").AsString();

        VersionLabel.Text = "Version: " + GameVersion;

        if (Globals.VersionChecked) {
            Label LatestVersionLabel = GetNode<Label>("VersionText/LatestVersion");

            LatestVersionLabel.Text = "Latest Version: " + Globals.LatestVersion;
        } else {
            HttpRequest GetLatestVersionNode = GetNode<HttpRequest>("VersionText/LatestVersion/HTTPRequest");

            GetLatestVersionNode.RequestCompleted += OnRequestCompleted;

            GD.Print(GameVersion);

            GetLatestVersionNode.Request(Globals.BaseURL + "version-check.php" + 
                "?ClientVersion=" + GameVersion + 
                "&ClientPlatform=" + OS.GetName() + 
                "&Requester=" + "PolygonsClient"
            );
        }
    }

    private void OnRequestCompleted(long result, long responseCode, string[] headers, byte[] body) {
        string ResponseBody = Encoding.UTF8.GetString(body);
        GD.Print(ResponseBody);

        List<string> ResponseValues = [.. ResponseBody.Split(";")];

        Globals.AccessLevel = ResponseValues[0];
        Globals.LatestVersion = ResponseValues[1];

        Label LatestVersionLabel = GetNode<Label>("VersionText/LatestVersion");

        LatestVersionLabel.Text = "Latest Version: " + Globals.LatestVersion;

        Globals.VersionChecked = true;

        string GameVersion = ProjectSettings.GetSetting("application/config/version").AsString();

        if (Globals.AccessLevel != "all" || GameVersion != Globals.LatestVersion) {
            var error = GetTree().ChangeSceneToFile("res://Scenes/OutdatedVersion.tscn");

            if (error != Error.Ok) {
                GD.PrintErr("Failed to change scene: " + error);
            }
        }
    }
}