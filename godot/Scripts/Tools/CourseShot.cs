using Godot;

/// <summary>
/// Stand the camera on a course and save a frame. Not part of the game; it exists because a
/// palette is the one thing a headless number cannot check.
///
///   godot --path godot --scene res://Scenes/CourseShot.tscn -- --course=BlockIsland --at=600
/// </summary>
public partial class CourseShot : Node3D
{
    private Main _main;
    private int _frame;
    private string _course = "BlockIsland";
    private float _at = 600f;

    public override void _Ready()
    {
        foreach (string a in OS.GetCmdlineUserArgs())
        {
            if (a.StartsWith("--course=")) _course = a.Substring(9);
            if (a.StartsWith("--at=")) _at = a.Substring(5).ToFloat();
        }

        _main = GD.Load<PackedScene>("res://Scenes/Main.tscn").Instantiate<Main>();
        AddChild(_main);
    }

    public override void _Process(double delta)
    {
        _frame++;
        if (_frame == 2)
        {
            _main.Level = _course == "Frogwood" ? Main.LevelType.FrogwoodNH : Main.LevelType.BlockIsland;
            _main.StartRide();
            _main.State = Main.GameState.Riding;
            _main.PlayerMgr.Distance = _at;
            _main.PlayerMgr.Speed = 14f;
        }
        if (_frame > 2 && _frame < 14)
        {
            _main.Terrain.Update(_main.PlayerMgr.Distance);
            _main.Scenery.UpdatePositions(_main.Terrain.ScrollOffset);
            _main.Scenery.UpdateAll(1f / 60f);
            _main.Cam.Update();
        }
        if (_frame == 14)
        {
            var img = GetViewport().GetTexture().GetImage();
            string path = "user://shot_" + _course + "_" + (int)_at + ".png";
            img.SavePng(path);
            GD.Print("wrote " + ProjectSettings.GlobalizePath(path));
            GetTree().Quit();
        }
    }
}
