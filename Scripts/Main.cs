using Godot;
using System.Collections.Generic;

public partial class Main : Node3D
{
    public enum GameState { Title, CharSelect, BoardSelect, LevelSelect, Riding, Paused, Finished, Countdown }
    /// <summary>Carl's three looks. There's only one Carl Spatski — this is what he's wearing.</summary>
    public enum CarlType { Office, Party, Dark }
    public enum BoardType { Classic, Neon, Dark, Natural }
    public enum LevelType { FrogwoodNH, BlockIsland, Unknown3, Unknown4 }

    // State
    public GameState State = GameState.Title;
    public CarlType Carl = CarlType.Office;
    public BoardType Board = BoardType.Classic;
    public LevelType Level = LevelType.FrogwoodNH;
    public float Timer = 0f;
    public float BestTime = 0f;
    public float FinishTime = 0f;
    public float CountdownTimer = 0f;
    public float TitleTime = 0f;

    /// <summary>
    /// A rider's handling, as 1-5 pips. This is the single source of truth: the bars drawn on
    /// the char-select screen and the numbers PlayerManager rides with both derive from it, so
    /// what the screen promises is what you get.
    ///   SPD  — top speed
    ///   HAND — steering authority
    ///   TRK  — trucks; how long he holds a line before speed wobble sets in
    /// </summary>
    public struct CarlStat
    {
        public int Speed, Handling, Tracking;
        public CarlStat(int s, int h, int t) { Speed = s; Handling = h; Tracking = t; }
    }

    // Constants
    public static readonly string[] CarlNames = { "Office Carl", "Party Carl", "Dark Carl" };
    public static readonly string[] CarlDescs = { "The everyman", "The maniac", "The enigma" };
    public static readonly CarlStat[] CarlStats = {
        new CarlStat(4, 4, 4),   // Office — the everyman, balanced
        new CarlStat(5, 3, 3),   // Party  — the maniac, fast but wobbly
        new CarlStat(4, 4, 5)    // Dark   — the enigma, smooth and controlled
    };
    public static readonly string[] BoardNames = { "Classic", "Neon", "Dark", "Natural" };
    public static readonly string[] BoardDescs = { "Brown wood deck", "Bright neon colors", "Black with purple accent", "Light natural wood" };

    // Four poster slots on the garage's left wall. The last two are placeholders for
    // courses that don't exist yet — they show on the wall but can't be started.
    public static readonly string[] LevelNames = { "Frogwood, NH", "Block Island", "???", "???" };
    public static readonly string[] LevelDescs = {
        "Rolling hills through quiet Frogwood",
        "Coastal cliffs over the Atlantic",
        "Course not built yet",
        "Course not built yet"
    };
    // Frogwood's entry is overwritten in _Ready from the CourseDesign, so the level-select
    // screen and the finish trigger can never disagree about how long the ride is.
    public static float[] LevelLengths = { 2000f, 0f, 0f, 0f };
    public static readonly string[] LevelSeasons = { "Summer", "Fall", "", "" };
    public static readonly bool[] LevelUnlocked = { true, false, false, false };
    public float CourseLength
    {
        get
        {
            if (Level == LevelType.FrogwoodNH && Course != null) return Course.Length;
            return LevelLengths[(int)Level];
        }
    }

    // ── Design resources ────────────────────────
    // What Carl, his board and the course look like. Assigned in Main.tscn and editable in the
    // Inspector; each falls back to stock defaults so an empty slot degrades instead of
    // crashing. See Scenes/CarlPreview.tscn and Scenes/CoursePreview.tscn to tune them live.
    [Export] public CarlDesign CarlLook { get; set; }
    [Export] public BoardDesign BoardLook { get; set; }
    [Export] public CourseDesign Course { get; set; }

    // Node references
    public Node3D Player;
    public Node3D CameraMount;

    // Subsystems
    public TerrainManager Terrain;
    public PlayerManager PlayerMgr;
    public SceneryManager Scenery;
    public GameUI UI;
    public GameCamera Cam;
    public GarageManager Garage;
    public TitleManager Title;

    public override void _Ready()
    {
        Player = GetNode<Node3D>("Player");
        CameraMount = GetNode<Node3D>("Player/CameraMount");

        CarlLook ??= new CarlDesign();
        BoardLook ??= new BoardDesign();
        Course ??= new CourseDesign();
        LevelLengths[(int)LevelType.FrogwoodNH] = Course.Length;

        Terrain = new TerrainManager(this, Course);
        PlayerMgr = new PlayerManager(this);
        Scenery = new SceneryManager(this, Terrain, Course);
        UI = new GameUI(this);
        Cam = new GameCamera(this);
        Garage = new GarageManager(this);
        Title = new TitleManager(this);

        Terrain.Create();
        PlayerMgr.Create();
        Scenery.Create();
        Cam.Create();
        UI.Create();
        Garage.Create();
        Title.Create();

        // Open outside the garage, not in it.
        Title.Show();
        Title.ResetCamera();
        UI.ShowTitle(this);
    }

    public override void _PhysicsProcess(double delta)
    {
        float dt = (float)delta;
        float steer = 0f;
        if (Input.IsActionPressed("move_left")) steer += 1f;
        if (Input.IsActionPressed("move_right")) steer -= 1f;
        bool braking = Input.IsActionPressed("brake");

        switch (State)
        {
            case GameState.Title:
                TitleTime += dt;
                Title.UpdateCamera(dt);
                UI.UpdateTitleBlink(TitleTime);
                UI.HandleTitleInput(this);
                break;

            case GameState.CharSelect:
                Garage.UpdateCamera(dt, GarageManager.Shot.Char);
                UI.HandleCharSelectInput(this);
                break;

            case GameState.BoardSelect:
                Garage.UpdateCamera(dt, GarageManager.Shot.Board);
                Garage.UpdateRack(dt);
                UI.HandleBoardSelectInput(this);
                break;

            case GameState.LevelSelect:
                Garage.UpdateCamera(dt, GarageManager.Shot.Level);
                UI.HandleLevelSelectInput(this);
                break;

            case GameState.Countdown:
                CountdownTimer -= dt;
                Cam.Update();
                if (CountdownTimer <= 0f)
                {
                    State = GameState.Riding;
                    PlayerMgr.Speed = 0.1f;
                    Timer = 0f;
                    UI.SetCountdown("", new Color(1, 1, 1));
                }
                else
                {
                    int count = Mathf.CeilToInt(CountdownTimer);
                    UI.SetCountdown(count.ToString(), count == 1 ?
                        new Color(0.2f, 1f, 0.4f) : new Color(1f, 0.88f, 0.23f));
                }
                break;

            case GameState.Riding:
                Timer += dt;
                PlayerMgr.Update(dt, steer, braking);
                Terrain.Update(PlayerMgr.Distance);
                Scenery.UpdateAll(dt);
                Cam.Update();
                UI.UpdateHUD(this);

                if (Input.IsActionJustPressed("pause"))
                {
                    State = GameState.Paused;
                    UI.SetPause("PAUSED");
                    UI.SetPrompt("Press Esc to resume");
                    break;
                }

                if (PlayerMgr.Distance >= CourseLength)
                {
                    State = GameState.Finished;
                    FinishTime = Timer;
                    if (BestTime <= 0f || FinishTime < BestTime)
                        BestTime = FinishTime;
                    PlayerMgr.SpawnConfetti();
                    UI.ShowFinish(this);
                }
                else if (PlayerMgr.Crashed)
                {
                    State = GameState.Finished;
                    UI.ShowCrash();
                }
                break;

            case GameState.Paused:
                Cam.Update();
                if (Input.IsActionJustPressed("pause"))
                {
                    State = GameState.Riding;
                    UI.SetPause("");
                    UI.SetPrompt("");
                }
                break;

            case GameState.Finished:
                Scenery.UpdateClouds(dt);
                Cam.Update();
                if (Input.IsActionJustPressed("kick_off"))
                    ResetGame();
                break;
        }
    }

    /// <summary>
    /// Show or hide everything that belongs to the ride. The world managers own only their own
    /// geometry now, so switching between title, garage and road is Main's call.
    /// </summary>
    public void SetRideWorldVisible(bool visible)
    {
        PlayerMgr.SetVisible(visible);
        Terrain.SetMeshesVisible(visible);
        Scenery.SetItemsVisible(visible);
    }

    /// <summary>
    /// Bright summer daylight with distance haze — the look for riding. The sky bounce is kept
    /// weak and near-neutral on purpose: a strong blue ambient turns the grass teal and the
    /// asphalt purple, which reads as dusk rather than a summer afternoon.
    /// </summary>
    public void ApplyRideLighting()
    {
        GetNode<DirectionalLight3D>("Sun").LightEnergy = 1.75f;
        var env = GetNode<WorldEnvironment>("WorldEnvironment").Environment;
        env.AmbientLightEnergy = 0.55f;
        env.AmbientLightColor = new Color(0.74f, 0.76f, 0.78f);
        env.FogEnabled = true;
    }

    // Each transition resets the camera of the world being entered. Without that the camera
    // lerps between two worlds' coordinates and sweeps through the scenery on the way.
    public void ShowTitle()
    {
        Garage.Hide();
        Title.Show();
        Title.ResetCamera();
        State = GameState.Title;
        UI.ShowTitle(this);
    }

    public void ShowCharSelect()
    {
        Title.Hide();
        Garage.Show();
        Garage.ResetCamera();
        State = GameState.CharSelect;
        UI.ShowCharSelect(this);
    }

    public void ShowBoardSelect()
    {
        State = GameState.BoardSelect;
        Garage.UpdateRackHighlight();
        UI.ShowBoardSelect(this);
    }

    public void ShowLevelSelect()
    {
        State = GameState.LevelSelect;
        Garage.UpdatePosterHighlight();
        UI.ShowLevelSelect(this);
    }

    /// <summary>Step back one screen. The camera glides rather than cutting.</summary>
    public void GoBack()
    {
        switch (State)
        {
            case GameState.LevelSelect: ShowBoardSelect(); break;
            case GameState.BoardSelect: ShowCharSelect(); break;
            case GameState.CharSelect: ShowTitle(); break;
        }
    }

    public void StartRide()
    {
        Garage.Hide();
        SetRideWorldVisible(true);
        ApplyRideLighting();
        UI.ShowHUD();
        State = GameState.Countdown;
        CountdownTimer = 3f;
        UI.HideAllSelectors();
        PlayerMgr.ApplyBoard();
        PlayerMgr.ApplyCarl();
    }

    public void ResetGame()
    {
        // Back to the garage, not to an empty road.
        PlayerMgr.Reset();
        Terrain.Update(PlayerMgr.Distance);
        Scenery.UpdatePositions(Terrain.ScrollOffset);
        SetRideWorldVisible(false);
        ShowTitle();
    }

}
