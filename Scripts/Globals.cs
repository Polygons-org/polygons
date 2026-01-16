using Godot;
using System;

public partial class Globals : Node {
    public static string BaseURL = "https://polygons.puppet57.xyz/polygons-server/";
    public static bool VersionChecked = false;
    public static string LatestVersion = "Loading...";
    public static string AccessLevel = "all";
    public static int GroundRed = 0;
    public static int GroundGreen = 0;
    public static int GroundBlue = 100;

    public static int BackGroundRed = 0;
    public static int BackGroundGreen = 0;
    public static int BackGroundBlue = 100;
}
