using Godot;

/// <summary>
/// Ride a course the way a player would, and report what happened.
///
/// This is not physics_sim.py. That mirrors PlayerManager's arithmetic and answers whether the
/// numbers work; this boots the actual game, presses actual input actions through Godot's own
/// input system, and lets the real PlayerManager, TerrainManager and GameCamera do it. What it
/// catches is everything the arithmetic cannot: a course that reads badly, a camera that loses
/// the road on a real bend, a wobble that arrives somewhere stupid.
///
/// The rider is deliberately competent rather than perfect. It holds the crown of the road and
/// carves when wobble climbs, which is exactly the loop the game is built around - hold a line
/// for speed, carve to shed the wobble that holding a line builds.
///
///   godot --path godot --scene res://Scenes/Playthrough.tscn -- --course=Frogwood
///   godot --path godot --scene res://Scenes/Playthrough.tscn -- --course=BlockIsland --shots
/// </summary>
public partial class Playthrough : Node3D
{
    private Main _main;
    private string _course = "Frogwood";
    private bool _shots;
    private bool _noPush;

    // What the push-off is worth. Not by summing each stroke's gain - gravity is
    // acting the whole time and there is no honest way to split the two apart in
    // one run. Run it twice, with --nopush, and difference the times.
    private int _pushes, _pushableFrames;
    private float _timeTo30 = -1f, _distTo30 = -1f;
    private int _frame;
    private int _shotIndex;
    private string _release = "";
    private int _releaseIn;

    // What the run is doing, sampled as it goes.
    private float _peak;
    private int _wobbleFrames, _floorFrames, _carves, _ridden;
    private float _lastCarveAt = -999f;
    private bool _carving;
    private string _steering = "";

    public override void _Ready()
    {
        foreach (string a in OS.GetCmdlineUserArgs())
        {
            if (a.StartsWith("--course=")) _course = a.Substring(9);
            if (a == "--shots") _shots = true;
            if (a == "--nopush") _noPush = true;
        }
        _main = GD.Load<PackedScene>("res://Scenes/Main.tscn").Instantiate<Main>();
        AddChild(_main);
    }

    public override void _PhysicsProcess(double delta)
    {
        _frame++;
        if (_frame == 3)
        {
            _main.Level = _course == "BlockIsland" ? Main.LevelType.BlockIsland
                                                   : Main.LevelType.FrogwoodNH;
            _main.StartRide();
            GD.Print("riding " + _course + ", " + _main.CourseLength.ToString("F0") + " m");
            return;
        }
        if (_frame < 3) return;

        // Skip the countdown rather than waiting it out.
        if (_main.State == Main.GameState.Countdown)
        {
            _main.CountdownTimer = 0.001f;
            return;
        }

        if (_main.State == Main.GameState.Riding)
        {
            Ride();
            return;
        }

        if (_main.State == Main.GameState.Finished)
        {
            Report();
            GetTree().Quit();
        }
    }

    /// <summary>The rider. Kick off, hold the crown, carve the wobble off.</summary>
    private void Ride()
    {
        if (_release != "" && --_releaseIn <= 0)
        {
            Input.ActionRelease(_release);
            _release = "";
        }

        var p = _main.PlayerMgr;
        _ridden++;
        _peak = Mathf.Max(_peak, p.Speed);
        if (p.WobbleLevel > 0.3f) _wobbleFrames++;
        if (p.Speed <= 2.55f) _floorFrames++;

        float t = _main.Timer;

        // Kick off the line, the way you would from a standstill: press again the
        // moment the last stroke finishes, until your leg cannot keep up.
        if (!_noPush && p.Speed < 8f && p.PushOffTimer <= 0f)
        {
            _pushes++;
            Press("kick_off", 2);
        }
        if (p.Speed < 8f) _pushableFrames++;
        if (_timeTo30 < 0f && p.Speed * 3.6f >= 30f)
        {
            _timeTo30 = t;
            _distTo30 = p.Distance;
        }

        // Carve when wobble builds, and hold the carve long enough to actually
        // shed it - a tap does nothing, the decay is per second.
        bool wantCarve = p.WobbleLevel > 0.45f || (_carving && p.WobbleLevel > 0.12f);
        if (wantCarve && !_carving)
        {
            _carving = true;
            _carves++;
            _lastCarveAt = t;
            // Carve toward the crown, so carving never walks you off the road.
            _steering = p.PosX > 0f ? "move_right" : "move_left";
            Input.ActionPress(_steering);
        }
        else if (!wantCarve && _carving)
        {
            _carving = false;
            Input.ActionRelease(_steering);
            _steering = "";
        }

        // Not carving: steer gently back toward the middle of the road.
        if (!_carving)
        {
            string want = p.PosX > 0.9f ? "move_right" : p.PosX < -0.9f ? "move_left" : "";
            if (want != _steering)
            {
                if (_steering != "") Input.ActionRelease(_steering);
                if (want != "") Input.ActionPress(want);
                _steering = want;
            }
        }

        if (_shots && _ridden % 900 == 0) Shot();
    }

    /// <summary>
    /// Hold an action for a number of PHYSICS frames, not seconds.
    ///
    /// A SceneTree timer releases on wall-clock time, so in a windowed run the number of
    /// physics frames a press covers drifts with frame pacing - and the whole ride drifts with
    /// it. Three runs of one identical build came out 108, 116 and 117 seconds, with the time
    /// bogged down ranging 7% to 14%: noise wider than the differences it was being used to
    /// measure. Counting frames makes a run repeatable.
    /// </summary>
    private void Press(string action, int frames)
    {
        Input.ActionPress(action);
        _release = action;
        _releaseIn = frames;
    }

    private void Shot()
    {
        var img = GetViewport().GetTexture().GetImage();
        string path = $"user://ride_{_course}_{++_shotIndex}.png";
        img.SavePng(path);
        GD.Print("  shot " + ProjectSettings.GlobalizePath(path)
                 + $"  at {_main.PlayerMgr.Distance:F0} m, {_main.PlayerMgr.Speed * 3.6f:F0} km/h");
    }

    private void Report()
    {
        var p = _main.PlayerMgr;
        if (_steering != "") Input.ActionRelease(_steering);

        GD.Print("");
        GD.Print("== " + _course + " ==");
        GD.Print($"  outcome       {(p.Crashed ? "CRASHED at " + p.Distance.ToString("F0") + " m" : "finished")}");
        GD.Print($"  time          {_main.FinishTime:F1} s");
        GD.Print($"  distance      {p.Distance:F0} m of {_main.CourseLength:F0}");
        GD.Print($"  peak speed    {_peak * 3.6f:F0} km/h   (top speed {p.MaxSpeed * 3.6f:F0})");
        GD.Print($"  in the wobble {100f * _wobbleFrames / Mathf.Max(1, _ridden):F0}% of the run");
        GD.Print($"  bogged down   {100f * _floorFrames / Mathf.Max(1, _ridden):F0}%");
        GD.Print($"  carves needed {_carves}");
        GD.Print($"  pushes        {_pushes} over {100f * _pushableFrames / Mathf.Max(1, _ridden):F0}% of the run spent under 29 km/h");
        GD.Print($"  reached 30    {(_timeTo30 < 0f ? "never" : _timeTo30.ToString("F1") + " s, " + _distTo30.ToString("F0") + " m")}");
    }
}
