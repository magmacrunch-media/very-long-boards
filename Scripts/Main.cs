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
    public static readonly float[] LevelLengths = { 2000f, 0f, 0f, 0f };
    public static readonly string[] LevelSeasons = { "Summer", "Fall", "", "" };
    public static readonly bool[] LevelUnlocked = { true, false, false, false };
    public static readonly Color[] CarlShirtColors = {
        new Color(0.65f, 0.22f, 0.22f),   // Office: red
        new Color(0.28f, 0.2f, 0.7f),     // Party: purple
        new Color(0.15f, 0.15f, 0.18f)    // Dark: black
    };
    public static readonly Color[] CarlPantsColors = {
        new Color(0.25f, 0.28f, 0.38f),   // Office: slacks
        new Color(0.95f, 0.45f, 0.15f),   // Party: orange
        new Color(0.12f, 0.12f, 0.15f)    // Dark: black
    };
    public static readonly Color[] BoardDeckColors = {
        new Color(0.52f, 0.26f, 0.1f),   // Classic brown
        new Color(0.95f, 0.25f, 0.95f),  // Neon pink
        new Color(0.12f, 0.12f, 0.15f),  // Dark black
        new Color(0.82f, 0.65f, 0.42f)   // Natural wood
    };
    public static readonly Color[] BoardGripColors = {
        new Color(0.16f, 0.16f, 0.16f),  // Classic black
        new Color(0.15f, 0.15f, 0.45f),  // Neon blue
        new Color(0.35f, 0.05f, 0.55f),  // Dark purple
        new Color(0.55f, 0.45f, 0.3f)    // Natural tan
    };
    public float CourseLength { get { return LevelLengths[(int)Level]; } }

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

    public override void _Ready()
    {
        Player = GetNode<Node3D>("Player");
        CameraMount = GetNode<Node3D>("Player/CameraMount");

        Terrain = new TerrainManager(this);
        PlayerMgr = new PlayerManager(this);
        Scenery = new SceneryManager(this);
        UI = new GameUI(this);
        Cam = new GameCamera(this);
        Garage = new GarageManager(this);

        Terrain.Create();
        PlayerMgr.Create();
        Scenery.Create();
        Cam.Create();
        UI.Create();
        Garage.Create();

        // The title screen is a shot of the garage, so open the game standing inside it.
        Garage.Show();
        Garage.ResetCamera();
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
                Garage.UpdateCamera(dt, GarageManager.Shot.Title);
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
                    PlayerMgr.Kicked = true;
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
                Terrain.Update();
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

    public void ShowTitle()
    {
        Garage.Show();
        State = GameState.Title;
        UI.ShowTitle(this);
    }

    public void ShowCharSelect()
    {
        Garage.Show();
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
        Terrain.Update();
        Scenery.UpdatePositions(Terrain.ScrollOffset);
        Garage.ResetCamera();
        ShowTitle();
    }

}
