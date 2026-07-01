using Godot;
using System.Collections.Generic;

public partial class Main : Node3D
{
    public enum GameState { Title, CharSelect, BoardSelect, Riding, Paused, Finished, Countdown }
    public enum CarlType { Office, Party, Dark }
    public enum BoardType { Classic, Neon, Dark, Natural }

    // State
    public GameState State = GameState.Title;
    public CarlType Carl = CarlType.Office;
    public BoardType Board = BoardType.Classic;
    public float Timer = 0f;
    public float BestTime = 0f;
    public float FinishTime = 0f;
    public float CountdownTimer = 0f;
    public float TitleTime = 0f;

    // Constants
    public static readonly string[] CarlNames = { "Office Carl", "Party Carl", "Dark Carl" };
    public static readonly string[] CarlDescs = { "The everyman", "The maniac", "The enigma" };
    public static readonly string[] BoardNames = { "Classic", "Neon", "Dark", "Natural" };
    public static readonly string[] BoardDescs = { "Brown wood deck", "Bright neon colors", "Black with purple accent", "Light natural wood" };
    public static readonly string CourseName = "New Hampshire Summer";
    public static readonly Color[] CarlShirtColors = {
        new Color(0.55f, 0.18f, 0.18f),
        new Color(0.2f, 0.15f, 0.6f),
        new Color(0.1f, 0.1f, 0.12f)
    };
    public static readonly Color[] CarlPantsColors = {
        new Color(0.2f, 0.24f, 0.32f),
        new Color(0.9f, 0.4f, 0.1f),
        new Color(0.08f, 0.08f, 0.1f)
    };
    public static readonly Color[] BoardDeckColors = {
        new Color(0.45f, 0.22f, 0.06f),  // Classic brown
        new Color(0.9f, 0.2f, 0.9f),     // Neon pink
        new Color(0.08f, 0.08f, 0.1f),   // Dark black
        new Color(0.75f, 0.6f, 0.35f)    // Natural wood
    };
    public static readonly Color[] BoardGripColors = {
        new Color(0.12f, 0.12f, 0.12f),  // Classic black
        new Color(0.1f, 0.1f, 0.4f),     // Neon blue
        new Color(0.3f, 0.0f, 0.5f),     // Dark purple
        new Color(0.5f, 0.4f, 0.25f)     // Natural tan
    };
    public const float CourseLength = 2000f;

    // Node references
    public CharacterBody3D Player;
    public Node3D CameraMount;

    // Subsystems
    public TerrainManager Terrain;
    public PlayerManager PlayerMgr;
    public SceneryManager Scenery;
    public GameUI UI;
    public GameCamera Cam;

    public override void _Ready()
    {
        Player = GetNode<CharacterBody3D>("Player");
        CameraMount = GetNode<Node3D>("Player/CameraMount");

        Terrain = new TerrainManager(this);
        PlayerMgr = new PlayerManager(this);
        Scenery = new SceneryManager(this);
        UI = new GameUI(this);
        Cam = new GameCamera(this);

        Terrain.Create();
        PlayerMgr.Create();
        Scenery.Create();
        Cam.Create();
        UI.Create();
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
                Scenery.UpdateClouds(dt);
                Cam.UpdateTitle(TitleTime);
                UI.HandleTitleInput(this);
                break;

            case GameState.CharSelect:
                Scenery.UpdateClouds(dt);
                Cam.UpdateTitle(TitleTime);
                UI.HandleCharSelectInput(this);
                break;

            case GameState.BoardSelect:
                Scenery.UpdateClouds(dt);
                Cam.UpdateTitle(TitleTime);
                UI.HandleBoardSelectInput(this);
                break;

            case GameState.Countdown:
                CountdownTimer -= dt;
                Cam.Update();
                if (CountdownTimer <= 0f)
                {
                    State = GameState.Riding;
                    PlayerMgr.Kicked = true;
                    PlayerMgr.Speed = 0.3f;
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

    public void ShowCharSelect()
    {
        State = GameState.CharSelect;
        UI.ShowCharSelect(this);
    }

    public void ShowBoardSelect()
    {
        State = GameState.BoardSelect;
        UI.ShowBoardSelect(this);
    }

    public void StartRide()
    {
        State = GameState.Countdown;
        CountdownTimer = 3f;
        UI.HideAllSelectors();
        PlayerMgr.ApplyBoard();
    }

    public void ResetGame()
    {
        State = GameState.Title;
        PlayerMgr.Reset();
        UI.ShowTitle(this);
        Terrain.Update();
        Scenery.UpdatePositions(Terrain.ScrollOffset);
        Cam.Update();
    }
}
