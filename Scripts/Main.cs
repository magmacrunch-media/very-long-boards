using Godot;
using System.Collections.Generic;

public partial class Main : Node3D
{
    public enum GameState { Title, CharSelect, BoardSelect, LevelSelect, Riding, Paused, Finished, Countdown }
    public enum CarlType { Office, Party, Dark }
    public enum BoardType { Classic, Neon, Dark, Natural }
    public enum LevelType { FrogwoodNH, BlockIsland }

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

    // Constants
    public static readonly string[] CarlNames = { "Office Carl", "Party Carl", "Dark Carl" };
    public static readonly string[] CarlDescs = { "The everyman", "The maniac", "The enigma" };
    public static readonly string[] BoardNames = { "Classic", "Neon", "Dark", "Natural" };
    public static readonly string[] BoardDescs = { "Brown wood deck", "Bright neon colors", "Black with purple accent", "Light natural wood" };
    public static readonly string[] LevelNames = { "Frogwood, NH", "Block Island" };
    public static readonly string[] LevelDescs = { "Rolling hills through quiet Frogwood", "Coastal cliffs over the Atlantic" };
    public static readonly float[] LevelLengths = { 2000f, 0f };
    public static readonly string[] LevelSeasons = { "Summer", "Fall" };
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

    // Title screen 3D board display
    private SubViewport _titleViewport;
    private Node3D _titleBoardRoot;
    private TextureRect _titleBoardRect;

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
        CreateTitleBoardDisplay();
        UI.AddTitleOverlay(_titleBoardRect);
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
                if (_titleBoardRoot != null)
                {
                    _titleBoardRoot.Rotation = new Vector3(0.3f, TitleTime * 0.8f, 0f);
                    _titleBoardRect.Visible = true;
                }
                UI.HandleTitleInput(this);
                break;

            case GameState.CharSelect:
                Garage.UpdateCamera(dt, true, false, false);
                UI.HandleCharSelectInput(this);
                break;

            case GameState.BoardSelect:
                Garage.UpdateCamera(dt, false, true, false);
                Garage.UpdateBoardRotation(dt);
                UI.HandleBoardSelectInput(this);
                break;

            case GameState.LevelSelect:
                Garage.UpdateCamera(dt, false, false, true);
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

    public void ShowCharSelect()
    {
        if (_titleBoardRect != null) _titleBoardRect.Visible = false;
        State = GameState.CharSelect;
        Garage.Show();
        UI.ShowCharSelect(this);
    }

    public void ShowBoardSelect()
    {
        State = GameState.BoardSelect;
        Garage.UpdateDisplayModel();
        UI.ShowBoardSelect(this);
    }

    public void ShowLevelSelect()
    {
        State = GameState.LevelSelect;
        Garage.UpdatePosterHighlight();
        UI.ShowLevelSelect(this);
    }

    public void StartRide()
    {
        if (_titleBoardRect != null) _titleBoardRect.Visible = false;
        Garage.Hide();
        UI.ShowHUD();
        State = GameState.Countdown;
        CountdownTimer = 3f;
        UI.HideAllSelectors();
        PlayerMgr.ApplyBoard();
    }

    public void ResetGame()
    {
        Garage.Hide();
        State = GameState.Title;
        PlayerMgr.Reset();
        UI.ShowTitle(this);
        Terrain.Update();
        Scenery.UpdatePositions(Terrain.ScrollOffset);
        Cam.Update();
        if (_titleBoardRoot != null) _titleBoardRoot.Visible = true;
    }

    // ═══════════════════════════════════════════
    //  TITLE SCREEN 3D BOARD
    // ═══════════════════════════════════════════

    private void CreateTitleBoardDisplay()
    {
        // SubViewport for the rotating board
        _titleViewport = new SubViewport();
        _titleViewport.Size = new Vector2I(80, 80);
        _titleViewport.RenderTargetUpdateMode = SubViewport.UpdateMode.Always;
        AddChild(_titleViewport);

        // Camera inside the viewport
        var cam = new Camera3D();
        cam.Position = new Vector3(0, 0.8f, 2f);
        cam.LookAt(Vector3.Zero);
        cam.Fov = 35f;
        _titleViewport.AddChild(cam);

        // Light
        var light = new DirectionalLight3D();
        light.Position = new Vector3(2, 3, 1);
        light.LightEnergy = 1.5f;
        light.LightColor = new Color(1f, 0.95f, 0.9f);
        _titleViewport.AddChild(light);

        // Board root
        _titleBoardRoot = new Node3D();
        _titleViewport.AddChild(_titleBoardRoot);

        // Build board mesh (same as PlayerManager)
        var deckMat = new StandardMaterial3D();
        deckMat.AlbedoColor = BoardDeckColors[(int)Board];
        deckMat.SpecularMode = BaseMaterial3D.SpecularModeEnum.Disabled;

        var gripMat = new StandardMaterial3D();
        gripMat.AlbedoColor = BoardGripColors[(int)Board];

        var truckMat = new StandardMaterial3D();
        truckMat.AlbedoColor = new Color(0.62f, 0.62f, 0.65f);

        var wheelMat = new StandardMaterial3D();
        wheelMat.AlbedoColor = new Color(0.12f, 0.12f, 0.12f);

        // Deck
        AddTitleBox(_titleBoardRoot, new Vector3(0.62f, 0.045f, 1.4f), deckMat, new Vector3(0, 0, 0));
        AddTitleBox(_titleBoardRoot, new Vector3(0.48f, 0.04f, 0.4f), deckMat, new Vector3(0, 0, 0.9f));
        AddTitleBox(_titleBoardRoot, new Vector3(0.48f, 0.04f, 0.35f), deckMat, new Vector3(0, 0, -0.88f));
        AddTitleBox(_titleBoardRoot, new Vector3(0.58f, 0.015f, 1.3f), gripMat, new Vector3(0, 0.03f, 0));

        // Trucks
        AddTitleBox(_titleBoardRoot, new Vector3(0.18f, 0.04f, 0.14f), truckMat, new Vector3(0, -0.05f, 0.55f));
        AddTitleBox(_titleBoardRoot, new Vector3(0.18f, 0.04f, 0.14f), truckMat, new Vector3(0, -0.05f, -0.55f));

        // Wheels
        foreach (var pos in new[] {
            new Vector3(-0.30f, -0.08f, 0.55f), new Vector3(0.30f, -0.08f, 0.55f),
            new Vector3(-0.30f, -0.08f, -0.55f), new Vector3(0.30f, -0.08f, -0.55f) })
        {
            var wheel = new MeshInstance3D();
            var wMesh = new CylinderMesh();
            wMesh.TopRadius = 0.055f;
            wMesh.BottomRadius = 0.055f;
            wMesh.Height = 0.07f;
            wMesh.RadialSegments = 6;
            wheel.Mesh = wMesh;
            wheel.MaterialOverride = wheelMat;
            wheel.Position = pos;
            wheel.Rotation = new Vector3(0, 0, Mathf.Pi / 2f);
            _titleBoardRoot.AddChild(wheel);
        }

        // TextureRect to display the viewport in the UI
        _titleBoardRect = new TextureRect();
        _titleBoardRect.Texture = _titleViewport.GetTexture();
        _titleBoardRect.SetAnchorsPreset(Control.LayoutPreset.FullRect);
        _titleBoardRect.AnchorLeft = 0.7f;
        _titleBoardRect.AnchorTop = 0.55f;
        _titleBoardRect.AnchorRight = 0.95f;
        _titleBoardRect.AnchorBottom = 0.9f;
        _titleBoardRect.OffsetLeft = 0;
        _titleBoardRect.OffsetTop = 0;
        _titleBoardRect.OffsetRight = 0;
        _titleBoardRect.OffsetBottom = 0;
        _titleBoardRect.StretchMode = TextureRect.StretchModeEnum.Scale;
        _titleBoardRect.Visible = false;

        // Add to canvas (need to find the canvas layer)
        // We'll add it via ShowTitle instead
    }

    private void AddTitleBox(Node3D parent, Vector3 size, StandardMaterial3D mat, Vector3 pos)
    {
        var m = new MeshInstance3D();
        var mesh = new BoxMesh();
        mesh.Size = size;
        m.Mesh = mesh;
        m.MaterialOverride = mat;
        m.Position = pos;
        parent.AddChild(m);
    }
}
