using Godot;

public class GameUI
{
    private Main _main;
    private FontFile _font;

    // HUD
    private Label _speedLabel;
    private Label _distLabel;
    private Label _timerLabel;
    private Label _bestLabel;
    private Label _promptLabel;
    private Label _pauseLabel;
    private Label _countdownLabel;
    private Label _nearMissLabel;
    private Label _wobbleLabel;
    private ColorRect _progressFill;
    private ColorRect _speedVignette;

    // Screens
    private Control _titleScreen;
    private Control _loadingScreen;
    private Control _charScreen;
    private Control _boardScreen;

    // Char Select
    private Label _charName;
    private Label _charDesc;
    private Label _charStats;

    // Board Select
    private Label _boardName;
    private Label _boardDesc;

    // Level Select
    private Control _levelScreen;
    private Label _levelName;
    private Label _levelDesc;

    public GameUI(Main main)
    {
        _main = main;
    }

    public void Create()
    {
        _font = GD.Load<FontFile>("res://Assets/Fonts/PressStart2P-Regular.ttf");

        var canvas = new CanvasLayer();
        _main.AddChild(canvas);

        CreateHUD(canvas);
        CreateTitleScreen(canvas);
        CreateLoadingScreen(canvas);
        CreateCharSelect(canvas);
        CreateBoardSelect(canvas);
        CreateLevelSelect(canvas);
    }

    private Label Retro(int size, string text, Color color)
    {
        var label = new Label();
        label.AddThemeFontSizeOverride("font_size", size);
        if (_font != null) label.AddThemeFontOverride("font", _font);
        label.Text = text;
        label.Modulate = color;
        label.HorizontalAlignment = HorizontalAlignment.Center;
        label.VerticalAlignment = VerticalAlignment.Center;
        return label;
    }

    // ── HUD ──────────────────────────────────────

    private void CreateHUD(CanvasLayer canvas)
    {
        var hud = new Control();
        hud.SetAnchorsPreset(Control.LayoutPreset.FullRect);
        canvas.AddChild(hud);

        // Speed (top-left, big)
        _speedLabel = Retro(20, "0 km/h", new Color(1, 1, 1));
        _speedLabel.SetAnchorsPreset(Control.LayoutPreset.TopLeft);
        _speedLabel.OffsetLeft = 16;
        _speedLabel.OffsetTop = 10;
        hud.AddChild(_speedLabel);

        // Distance
        _distLabel = Retro(12, "0 m", new Color(1, 1, 1));
        _distLabel.SetAnchorsPreset(Control.LayoutPreset.TopLeft);
        _distLabel.OffsetLeft = 16;
        _distLabel.OffsetTop = 38;
        hud.AddChild(_distLabel);

        // Timer
        _timerLabel = Retro(12, "0:00.0", new Color(1, 1, 1));
        _timerLabel.SetAnchorsPreset(Control.LayoutPreset.TopLeft);
        _timerLabel.OffsetLeft = 16;
        _timerLabel.OffsetTop = 58;
        hud.AddChild(_timerLabel);

        // Best time
        _bestLabel = Retro(10, "", new Color(0.7f, 0.85f, 1f));
        _bestLabel.SetAnchorsPreset(Control.LayoutPreset.TopLeft);
        _bestLabel.OffsetLeft = 16;
        _bestLabel.OffsetTop = 78;
        hud.AddChild(_bestLabel);

        // Pause (center)
        _pauseLabel = Retro(24, "", new Color(0.8f, 0.6f, 1f));
        _pauseLabel.SetAnchorsPreset(Control.LayoutPreset.Center);
        _pauseLabel.OffsetTop = -20;
        hud.AddChild(_pauseLabel);

        // Countdown (center)
        _countdownLabel = Retro(48, "", new Color(1f, 0.88f, 0.23f));
        _countdownLabel.SetAnchorsPreset(Control.LayoutPreset.Center);
        _countdownLabel.OffsetTop = -30;
        hud.AddChild(_countdownLabel);

        // Near miss (center-low)
        _nearMissLabel = Retro(14, "", new Color(1f, 0.85f, 0.3f));
        _nearMissLabel.SetAnchorsPreset(Control.LayoutPreset.Center);
        _nearMissLabel.OffsetTop = 80;
        hud.AddChild(_nearMissLabel);

        // Wobble warning (center-low, below near miss)
        _wobbleLabel = Retro(14, "", new Color(1f, 0.85f, 0.3f));
        _wobbleLabel.SetAnchorsPreset(Control.LayoutPreset.Center);
        _wobbleLabel.OffsetTop = 110;
        hud.AddChild(_wobbleLabel);

        // Prompt (bottom-center)
        _promptLabel = Retro(10, "", new Color(1f, 0.88f, 0.23f));
        _promptLabel.SetAnchorsPreset(Control.LayoutPreset.Center);
        _promptLabel.OffsetTop = 200;
        hud.AddChild(_promptLabel);

        // Progress bar background
        var barBg = new ColorRect();
        barBg.Color = new Color(0, 0, 0, 0.5f);
        barBg.SetAnchorsPreset(Control.LayoutPreset.BottomWide);
        barBg.AnchorTop = 0.96f;
        barBg.OffsetTop = 0;
        hud.AddChild(barBg);

        // Progress bar fill
        _progressFill = new ColorRect();
        _progressFill.Color = new Color(0.2f, 0.85f, 0.4f, 0.8f);
        _progressFill.SetAnchorsPreset(Control.LayoutPreset.BottomWide);
        _progressFill.AnchorTop = 0.96f;
        _progressFill.AnchorRight = 0f;
        _progressFill.OffsetTop = 0;
        hud.AddChild(_progressFill);

        // Speed vignette — darkens edges at high speed
        _speedVignette = new ColorRect();
        _speedVignette.SetAnchorsPreset(Control.LayoutPreset.FullRect);
        _speedVignette.Color = new Color(0, 0, 0, 0f);
        _speedVignette.MouseFilter = Control.MouseFilterEnum.Ignore;
        hud.AddChild(_speedVignette);
    }

    // ── Title Screen ─────────────────────────────

    private void CreateTitleScreen(CanvasLayer canvas)
    {
        _titleScreen = new Control();
        _titleScreen.SetAnchorsPreset(Control.LayoutPreset.FullRect);
        canvas.AddChild(_titleScreen);

        var bg = new ColorRect();
        bg.SetAnchorsPreset(Control.LayoutPreset.FullRect);
        bg.Color = new Color(0.04f, 0.02f, 0.08f, 0.88f);
        _titleScreen.AddChild(bg);

        var title = Retro(28, "VERY LONG BOARDS", new Color(1f, 0.18f, 0.61f));
        title.SetAnchorsPreset(Control.LayoutPreset.Center);
        title.OffsetTop = -100;
        _titleScreen.AddChild(title);

        var sub = Retro(10, "A Carl Spatski Game", new Color(0.6f, 0.6f, 0.7f));
        sub.SetAnchorsPreset(Control.LayoutPreset.Center);
        sub.OffsetTop = -50;
        _titleScreen.AddChild(sub);

        var course = Retro(8, Main.LevelNames[0], new Color(0.4f, 0.55f, 0.4f));
        course.SetAnchorsPreset(Control.LayoutPreset.Center);
        course.OffsetTop = -20;
        _titleScreen.AddChild(course);

        var prompt = Retro(12, "PRESS \u2191 TO START", new Color(1f, 0.88f, 0.23f));
        prompt.SetAnchorsPreset(Control.LayoutPreset.Center);
        prompt.OffsetTop = 120;
        _titleScreen.AddChild(prompt);

        var controls = Retro(7, "\u2190 \u2192 STEER    SPACE BRAKE    \u2191/ENTER KICK", new Color(0.4f, 0.4f, 0.5f));
        controls.SetAnchorsPreset(Control.LayoutPreset.Center);
        controls.OffsetTop = 170;
        _titleScreen.AddChild(controls);
    }

    // ── Loading Screen ───────────────────────────

    private void CreateLoadingScreen(CanvasLayer canvas)
    {
        _loadingScreen = new Control();
        _loadingScreen.SetAnchorsPreset(Control.LayoutPreset.FullRect);
        _loadingScreen.Visible = false;
        canvas.AddChild(_loadingScreen);

        var bg = new ColorRect();
        bg.SetAnchorsPreset(Control.LayoutPreset.FullRect);
        bg.Color = new Color(0.03f, 0.02f, 0.06f, 1f);
        _loadingScreen.AddChild(bg);

        var text = Retro(14, "LOADING...", new Color(1f, 0.88f, 0.23f));
        text.SetAnchorsPreset(Control.LayoutPreset.Center);
        _loadingScreen.AddChild(text);
    }

    // ── Character Select ─────────────────────────

    private void CreateCharSelect(CanvasLayer canvas)
    {
        _charScreen = new Control();
        _charScreen.SetAnchorsPreset(Control.LayoutPreset.FullRect);
        _charScreen.Visible = false;
        canvas.AddChild(_charScreen);

        var bg = new ColorRect();
        bg.SetAnchorsPreset(Control.LayoutPreset.FullRect);
        bg.Color = new Color(0.02f, 0.03f, 0.08f, 0.92f);
        _charScreen.AddChild(bg);

        var title = Retro(16, "SELECT YOUR CARL", new Color(1f, 0.18f, 0.61f));
        title.SetAnchorsPreset(Control.LayoutPreset.Center);
        title.OffsetTop = -120;
        _charScreen.AddChild(title);

        _charName = Retro(22, "", new Color(1f, 0.88f, 0.23f));
        _charName.SetAnchorsPreset(Control.LayoutPreset.Center);
        _charName.OffsetTop = -40;
        _charScreen.AddChild(_charName);

        _charDesc = Retro(10, "", new Color(0.7f, 0.7f, 0.8f));
        _charDesc.SetAnchorsPreset(Control.LayoutPreset.Center);
        _charDesc.OffsetTop = 0;
        _charScreen.AddChild(_charDesc);

        _charStats = Retro(8, "", new Color(0.5f, 0.8f, 0.5f));
        _charStats.SetAnchorsPreset(Control.LayoutPreset.Center);
        _charStats.OffsetTop = 40;
        _charScreen.AddChild(_charStats);

        var arrowL = Retro(20, "\u25C0", new Color(0.5f, 0.5f, 0.6f));
        arrowL.SetAnchorsPreset(Control.LayoutPreset.Center);
        arrowL.OffsetLeft = -300;
        arrowL.OffsetTop = -40;
        _charScreen.AddChild(arrowL);

        var arrowR = Retro(20, "\u25B6", new Color(0.5f, 0.5f, 0.6f));
        arrowR.SetAnchorsPreset(Control.LayoutPreset.Center);
        arrowR.OffsetLeft = 300;
        arrowR.OffsetTop = -40;
        _charScreen.AddChild(arrowR);

        var hint = Retro(8, "\u2190 \u2192 SELECT    ENTER CONFIRM", new Color(0.4f, 0.4f, 0.5f));
        hint.SetAnchorsPreset(Control.LayoutPreset.Center);
        hint.OffsetTop = 160;
        _charScreen.AddChild(hint);
    }

    // ── Board Select ─────────────────────────────

    private void CreateBoardSelect(CanvasLayer canvas)
    {
        _boardScreen = new Control();
        _boardScreen.SetAnchorsPreset(Control.LayoutPreset.FullRect);
        _boardScreen.Visible = false;
        canvas.AddChild(_boardScreen);

        var bg = new ColorRect();
        bg.SetAnchorsPreset(Control.LayoutPreset.FullRect);
        bg.Color = new Color(0.02f, 0.06f, 0.03f, 0.92f);
        _boardScreen.AddChild(bg);

        var title = Retro(16, "CHOOSE YOUR BOARD", new Color(1f, 0.18f, 0.61f));
        title.SetAnchorsPreset(Control.LayoutPreset.Center);
        title.OffsetTop = -100;
        _boardScreen.AddChild(title);

        _boardName = Retro(22, "", new Color(1f, 0.88f, 0.23f));
        _boardName.SetAnchorsPreset(Control.LayoutPreset.Center);
        _boardName.OffsetTop = -20;
        _boardScreen.AddChild(_boardName);

        _boardDesc = Retro(10, "", new Color(0.7f, 0.7f, 0.8f));
        _boardDesc.SetAnchorsPreset(Control.LayoutPreset.Center);
        _boardDesc.OffsetTop = 20;
        _boardScreen.AddChild(_boardDesc);

        var arrowL = Retro(20, "\u25C0", new Color(0.5f, 0.5f, 0.6f));
        arrowL.SetAnchorsPreset(Control.LayoutPreset.Center);
        arrowL.OffsetLeft = -300;
        arrowL.OffsetTop = -20;
        _boardScreen.AddChild(arrowL);

        var arrowR = Retro(20, "\u25B6", new Color(0.5f, 0.5f, 0.6f));
        arrowR.SetAnchorsPreset(Control.LayoutPreset.Center);
        arrowR.OffsetLeft = 300;
        arrowR.OffsetTop = -20;
        _boardScreen.AddChild(arrowR);

        var hint = Retro(8, "\u2190 \u2192 SELECT    ENTER CONFIRM", new Color(0.4f, 0.4f, 0.5f));
        hint.SetAnchorsPreset(Control.LayoutPreset.Center);
        hint.OffsetTop = 120;
        _boardScreen.AddChild(hint);
    }

    // ── Level Select ─────────────────────────────

    private void CreateLevelSelect(CanvasLayer canvas)
    {
        _levelScreen = new Control();
        _levelScreen.SetAnchorsPreset(Control.LayoutPreset.FullRect);
        _levelScreen.Visible = false;
        canvas.AddChild(_levelScreen);

        var bg = new ColorRect();
        bg.SetAnchorsPreset(Control.LayoutPreset.FullRect);
        bg.Color = new Color(0.06f, 0.04f, 0.02f, 0.92f);
        _levelScreen.AddChild(bg);

        var title = Retro(16, "CHOOSE YOUR COURSE", new Color(1f, 0.18f, 0.61f));
        title.SetAnchorsPreset(Control.LayoutPreset.Center);
        title.OffsetTop = -100;
        _levelScreen.AddChild(title);

        _levelName = Retro(22, "", new Color(1f, 0.88f, 0.23f));
        _levelName.SetAnchorsPreset(Control.LayoutPreset.Center);
        _levelName.OffsetTop = -20;
        _levelScreen.AddChild(_levelName);

        _levelDesc = Retro(10, "", new Color(0.7f, 0.7f, 0.8f));
        _levelDesc.SetAnchorsPreset(Control.LayoutPreset.Center);
        _levelDesc.OffsetTop = 20;
        _levelScreen.AddChild(_levelDesc);

        var arrowL = Retro(20, "\u25C0", new Color(0.5f, 0.5f, 0.6f));
        arrowL.SetAnchorsPreset(Control.LayoutPreset.Center);
        arrowL.OffsetLeft = -300;
        arrowL.OffsetTop = -20;
        _levelScreen.AddChild(arrowL);

        var arrowR = Retro(20, "\u25B6", new Color(0.5f, 0.5f, 0.6f));
        arrowR.SetAnchorsPreset(Control.LayoutPreset.Center);
        arrowR.OffsetLeft = 300;
        arrowR.OffsetTop = -20;
        _levelScreen.AddChild(arrowR);

        var hint = Retro(8, "\u2190 \u2192 SELECT    ENTER CONFIRM", new Color(0.4f, 0.4f, 0.5f));
        hint.SetAnchorsPreset(Control.LayoutPreset.Center);
        hint.OffsetTop = 120;
        _levelScreen.AddChild(hint);
    }

    // ── Input Handlers ───────────────────────────

    public void HandleTitleInput(Main main)
    {
        if (Input.IsActionJustPressed("kick_off"))
            main.ShowCharSelect();
    }

    public void HandleCharSelectInput(Main main)
    {
        if (Input.IsActionJustPressed("move_left"))
        {
            main.Carl = (Main.CarlType)(((int)main.Carl + 2) % 3);
            UpdateCharSelect(main);
        }
        if (Input.IsActionJustPressed("move_right"))
        {
            main.Carl = (Main.CarlType)(((int)main.Carl + 1) % 3);
            UpdateCharSelect(main);
        }
        if (Input.IsActionJustPressed("kick_off"))
            main.ShowBoardSelect();
    }

    public void HandleBoardSelectInput(Main main)
    {
        if (Input.IsActionJustPressed("move_left"))
        {
            main.Board = (Main.BoardType)(((int)main.Board + 3) % 4);
            UpdateBoardSelect(main);
        }
        if (Input.IsActionJustPressed("move_right"))
        {
            main.Board = (Main.BoardType)(((int)main.Board + 1) % 4);
            UpdateBoardSelect(main);
        }
        if (Input.IsActionJustPressed("kick_off"))
            main.ShowLevelSelect();
    }

    public void HandleLevelSelectInput(Main main)
    {
        int levelCount = Main.LevelNames.Length;
        if (Input.IsActionJustPressed("move_left"))
        {
            main.Level = (Main.LevelType)(((int)main.Level + levelCount - 1) % levelCount);
            UpdateLevelSelect(main);
        }
        if (Input.IsActionJustPressed("move_right"))
        {
            main.Level = (Main.LevelType)(((int)main.Level + 1) % levelCount);
            UpdateLevelSelect(main);
        }
        if (Input.IsActionJustPressed("kick_off"))
            main.StartRide();
    }

    // ── Show/Hide ────────────────────────────────

    public void ShowTitle(Main main)
    {
        _titleScreen.Visible = true;
        _charScreen.Visible = false;
        _boardScreen.Visible = false;
        _levelScreen.Visible = false;
        _loadingScreen.Visible = false;
        _promptLabel.Text = "";
        _countdownLabel.Text = "";
        _nearMissLabel.Text = "";
        _wobbleLabel.Text = "";
        _progressFill.AnchorRight = 0f;
    }

    public void ShowCharSelect(Main main)
    {
        _titleScreen.Visible = false;
        _charScreen.Visible = true;
        _boardScreen.Visible = false;
        UpdateCharSelect(main);
    }

    public void ShowBoardSelect(Main main)
    {
        _charScreen.Visible = false;
        _boardScreen.Visible = true;
        UpdateBoardSelect(main);
    }

    public void ShowLevelSelect(Main main)
    {
        _boardScreen.Visible = false;
        _levelScreen.Visible = true;
        UpdateLevelSelect(main);
    }

    public void HideAllSelectors()
    {
        _titleScreen.Visible = false;
        _charScreen.Visible = false;
        _boardScreen.Visible = false;
        _levelScreen.Visible = false;
        _loadingScreen.Visible = false;
        _promptLabel.Text = "";
    }

    private void UpdateCharSelect(Main main)
    {
        _charName.Text = Main.CarlNames[(int)main.Carl];
        _charDesc.Text = Main.CarlDescs[(int)main.Carl];

        string stats = "";
        switch (main.Carl)
        {
            case Main.CarlType.Office:
                stats = "SPD [\u2584\u2584\u2584\u2584\u2581] HAND [\u2584\u2584\u2584\u2584\u2581] TRK [\u2584\u2584\u2584\u2584\u2581]";
                break;
            case Main.CarlType.Party:
                stats = "SPD [\u2588\u2588\u2588\u2588\u2588] HAND [\u2588\u2588\u2588\u2581\u2581] TRK [\u2588\u2588\u2588\u2581\u2581]";
                break;
            case Main.CarlType.Dark:
                stats = "SPD [\u2588\u2588\u2588\u2588\u2581] HAND [\u2588\u2588\u2588\u2588\u2581] TRK [\u2588\u2588\u2588\u2588\u2588]";
                break;
        }
        _charStats.Text = stats;
    }

    private void UpdateBoardSelect(Main main)
    {
        _boardName.Text = Main.BoardNames[(int)main.Board];
        _boardDesc.Text = Main.BoardDescs[(int)main.Board];
    }

    private void UpdateLevelSelect(Main main)
    {
        _levelName.Text = Main.LevelNames[(int)main.Level];
        _levelDesc.Text = Main.LevelDescs[(int)main.Level];
    }

    // ── HUD ──────────────────────────────────────

    public void UpdateHUD(Main main)
    {
        var player = main.PlayerMgr;
        float kmh = player.Speed * 14f;
        _speedLabel.Text = $"{kmh:F0} km/h";

        float speedRatio = Mathf.Clamp(player.Speed / PlayerManager.MaxSpeed, 0f, 1f);
        if (speedRatio > 0.8f)
            _speedLabel.Modulate = new Color(1f, 0.3f, 0.3f);
        else if (speedRatio > 0.5f)
            _speedLabel.Modulate = new Color(1f, 0.9f, 0.3f);
        else
            _speedLabel.Modulate = new Color(1f, 1f, 1f);

        _distLabel.Text = $"{player.Distance:F0} m";
        int mins = (int)(main.Timer / 60f);
        float secs = main.Timer % 60f;
        _timerLabel.Text = $"{mins}:{secs:00.0}";

        if (main.BestTime > 0f)
        {
            int bMins = (int)(main.BestTime / 60f);
            float bSecs = main.BestTime % 60f;
            _bestLabel.Text = $"Best: {bMins}:{bSecs:00.0}";
        }

        float progress = Mathf.Clamp(player.Distance / main.CourseLength, 0f, 1f);
        _progressFill.AnchorRight = progress;

        // Speed vignette — darken edges at high speed
        float vignetteAlpha = Mathf.Clamp((speedRatio - 0.6f) / 0.4f, 0f, 1f) * 0.35f;
        _speedVignette.Color = new Color(0, 0, 0, vignetteAlpha);

        // Wobble warning
        if (player.WobbleLevel > 0.3f)
        {
            float wobbleT = (player.WobbleLevel - 0.3f) / 0.7f;
            _wobbleLabel.Text = "WOBBLING!";
            _wobbleLabel.Modulate = new Color(1f, Mathf.Lerp(0.85f, 0.2f, wobbleT), Mathf.Lerp(0.3f, 0.1f, wobbleT));
        }
        else
        {
            _wobbleLabel.Text = "";
        }
    }

    public void ShowFinish(Main main)
    {
        int mins = (int)(main.FinishTime / 60f);
        float secs = main.FinishTime % 60f;
        string bestText = "";
        if (main.BestTime > 0f)
        {
            int bMins = (int)(main.BestTime / 60f);
            float bSecs = main.BestTime % 60f;
            bestText = $"  BEST {bMins}:{bSecs:00.0}";
        }
        _promptLabel.Modulate = new Color(0.22f, 1f, 0.43f);
        _promptLabel.Text = $"FINISH! {mins}:{secs:00.0}{bestText}  |  \u2191 RIDE AGAIN";
        _progressFill.AnchorRight = 1f;
    }

    public void ShowCrash()
    {
        _promptLabel.Modulate = new Color(1f, 0.3f, 0.3f);
        _promptLabel.Text = "WIPEOUT! OFF ROAD  |  \u2191 TRY AGAIN";
    }

    public void SetCountdown(string text, Color color)
    {
        _countdownLabel.Text = text;
        _countdownLabel.Modulate = color;
    }

    public void SetPause(string text) { _pauseLabel.Text = text; }
    public void SetPrompt(string text) { _promptLabel.Text = text; }
}
