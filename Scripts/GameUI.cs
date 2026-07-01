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
    private ColorRect _progressFill;

    // Title
    private Control _titleScreen;

    // Loading
    private Control _loadingScreen;
    private Label _loadingText;

    // Char Select
    private Control _charScreen;
    private Label _charName;
    private Label _charDesc;
    private Label _charStats;

    // Board Select
    private Control _boardScreen;
    private Label _boardName;
    private Label _boardDesc;

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
    }

    private Label MakeRetroLabel(int size, string text, Color color, Control parent)
    {
        var label = new Label();
        label.AddThemeFontSizeOverride("font_size", size);
        label.AddThemeFontOverride("font", _font);
        label.Text = text;
        label.Modulate = color;
        label.HorizontalAlignment = HorizontalAlignment.Center;
        label.VerticalAlignment = VerticalAlignment.Center;
        parent.AddChild(label);
        return label;
    }

    private PanelContainer MakePanel(Control parent, Color bgColor, Color borderColor)
    {
        var panel = new PanelContainer();
        var ps = new StyleBoxFlat();
        ps.BgColor = bgColor;
        ps.BorderColor = borderColor;
        ps.CornerRadiusTopLeft = 4; ps.CornerRadiusTopRight = 4;
        ps.CornerRadiusBottomLeft = 4; ps.CornerRadiusBottomRight = 4;
        ps.ContentMarginLeft = 16; ps.ContentMarginRight = 16;
        ps.ContentMarginTop = 12; ps.ContentMarginBottom = 12;
        panel.AddThemeStyleboxOverride("panel", ps);
        parent.AddChild(panel);
        return panel;
    }

    // ── HUD ──────────────────────────────────────

    private void CreateHUD(CanvasLayer canvas)
    {
        var hud = new Control();
        hud.SetAnchorsPreset(Control.LayoutPreset.FullRect);
        canvas.AddChild(hud);

        // Top-left panel
        var panel = MakePanel(hud, new Color(0, 0, 0, 0.6f), new Color(0.3f, 0.3f, 0.4f));
        panel.AnchorLeft = 0; panel.AnchorTop = 0;
        panel.AnchorRight = 0; panel.AnchorBottom = 0;
        panel.OffsetLeft = 12; panel.OffsetTop = 10;
        panel.OffsetRight = 220; panel.OffsetBottom = 120;

        var vbox = new VBoxContainer();
        vbox.AddThemeConstantOverride("separation", 4);
        panel.AddChild(vbox);

        _speedLabel = MakeRetroLabel(18, "0 km/h", new Color(1, 1, 1), new Control());
        vbox.AddChild(_speedLabel);
        _distLabel = MakeRetroLabel(12, "0 m", new Color(1, 1, 1), new Control());
        vbox.AddChild(_distLabel);
        _timerLabel = MakeRetroLabel(12, "0:00.0", new Color(1, 1, 1), new Control());
        vbox.AddChild(_timerLabel);
        _bestLabel = MakeRetroLabel(10, "", new Color(0.7f, 0.85f, 1f), new Control());
        vbox.AddChild(_bestLabel);

        // Center labels
        _pauseLabel = MakeRetroLabel(24, "", new Color(0.8f, 0.6f, 1f), hud);
        _pauseLabel.SetAnchorsPreset(Control.LayoutPreset.Center);
        _pauseLabel.OffsetTop = -20;

        _countdownLabel = MakeRetroLabel(48, "", new Color(1f, 0.88f, 0.23f), hud);
        _countdownLabel.SetAnchorsPreset(Control.LayoutPreset.Center);
        _countdownLabel.OffsetTop = -30;

        _nearMissLabel = MakeRetroLabel(14, "", new Color(1f, 0.85f, 0.3f), hud);
        _nearMissLabel.SetAnchorsPreset(Control.LayoutPreset.Center);
        _nearMissLabel.AnchorTop = 0.65f;
        _nearMissLabel.OffsetTop = 0;

        _promptLabel = MakeRetroLabel(10, "", new Color(1f, 0.88f, 0.23f), hud);
        _promptLabel.SetAnchorsPreset(Control.LayoutPreset.Center);
        _promptLabel.AnchorTop = 0.88f;
        _promptLabel.OffsetTop = 0;

        // Progress bar
        var barBg = new ColorRect();
        barBg.Color = new Color(0, 0, 0, 0.5f);
        barBg.SetAnchorsPreset(Control.LayoutPreset.BottomWide);
        barBg.AnchorTop = 0.96f;
        barBg.OffsetTop = 0;
        hud.AddChild(barBg);

        _progressFill = new ColorRect();
        _progressFill.Color = new Color(0.2f, 0.85f, 0.4f, 0.8f);
        _progressFill.SetAnchorsPreset(Control.LayoutPreset.BottomWide);
        _progressFill.AnchorTop = 0.96f;
        _progressFill.AnchorRight = 0f;
        _progressFill.OffsetTop = 0;
        hud.AddChild(_progressFill);
    }

    // ── Title Screen ─────────────────────────────

    private void CreateTitleScreen(CanvasLayer canvas)
    {
        _titleScreen = new Control();
        _titleScreen.SetAnchorsPreset(Control.LayoutPreset.FullRect);
        canvas.AddChild(_titleScreen);

        // Dark overlay
        var bg = new ColorRect();
        bg.SetAnchorsPreset(Control.LayoutPreset.FullRect);
        bg.Color = new Color(0.03f, 0.02f, 0.06f, 0.85f);
        _titleScreen.AddChild(bg);

        // Title
        var title = MakeRetroLabel(28, "VERY LONG BOARDS", new Color(1f, 0.18f, 0.61f), _titleScreen);
        title.SetAnchorsPreset(Control.LayoutPreset.Center);
        title.AnchorTop = 0.15f;
        title.OffsetTop = 0;

        // Subtitle
        var sub = MakeRetroLabel(10, "A Carl Spatski Game", new Color(0.6f, 0.6f, 0.7f), _titleScreen);
        sub.SetAnchorsPreset(Control.LayoutPreset.Center);
        sub.AnchorTop = 0.25f;
        sub.OffsetTop = 0;

        // Course name
        var course = MakeRetroLabel(8, Main.CourseName, new Color(0.4f, 0.55f, 0.4f), _titleScreen);
        course.SetAnchorsPreset(Control.LayoutPreset.Center);
        course.AnchorTop = 0.32f;
        course.OffsetTop = 0;

        // Prompt
        var prompt = MakeRetroLabel(12, "PRESS \u2191 TO START", new Color(1f, 0.88f, 0.23f), _titleScreen);
        prompt.SetAnchorsPreset(Control.LayoutPreset.Center);
        prompt.AnchorTop = 0.75f;
        prompt.OffsetTop = 0;

        // Controls hint
        var controls = MakeRetroLabel(7, "\u2190 \u2192 STEER    SPACE BRAKE    \u2191 KICK OFF", new Color(0.4f, 0.4f, 0.5f), _titleScreen);
        controls.SetAnchorsPreset(Control.LayoutPreset.Center);
        controls.AnchorTop = 0.85f;
        controls.OffsetTop = 0;
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

        _loadingText = MakeRetroLabel(14, "LOADING...", new Color(1f, 0.88f, 0.23f), _loadingScreen);
        _loadingText.SetAnchorsPreset(Control.LayoutPreset.Center);
        _loadingText.OffsetTop = 0;
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
        bg.Color = new Color(0.03f, 0.02f, 0.06f, 0.9f);
        _charScreen.AddChild(bg);

        // Title
        var title = MakeRetroLabel(16, "SELECT YOUR CARL", new Color(1f, 0.18f, 0.61f), _charScreen);
        title.SetAnchorsPreset(Control.LayoutPreset.Center);
        title.AnchorTop = 0.12f;
        title.OffsetTop = 0;

        // Character name
        _charName = MakeRetroLabel(22, "", new Color(1f, 0.88f, 0.23f), _charScreen);
        _charName.SetAnchorsPreset(Control.LayoutPreset.Center);
        _charName.AnchorTop = 0.3f;
        _charName.OffsetTop = 0;

        // Description
        _charDesc = MakeRetroLabel(10, "", new Color(0.7f, 0.7f, 0.8f), _charScreen);
        _charDesc.SetAnchorsPreset(Control.LayoutPreset.Center);
        _charDesc.AnchorTop = 0.4f;
        _charDesc.OffsetTop = 0;

        // Stats
        _charStats = MakeRetroLabel(8, "", new Color(0.5f, 0.8f, 0.5f), _charScreen);
        _charStats.SetAnchorsPreset(Control.LayoutPreset.Center);
        _charStats.AnchorTop = 0.5f;
        _charStats.OffsetTop = 0;

        // Arrow indicators
        var arrowL = MakeRetroLabel(18, "\u25C0", new Color(0.5f, 0.5f, 0.6f), _charScreen);
        arrowL.SetAnchorsPreset(Control.LayoutPreset.Center);
        arrowL.AnchorLeft = 0.15f;
        arrowL.AnchorRight = 0.15f;
        arrowL.OffsetTop = -10;

        var arrowR = MakeRetroLabel(18, "\u25B6", new Color(0.5f, 0.5f, 0.6f), _charScreen);
        arrowR.SetAnchorsPreset(Control.LayoutPreset.Center);
        arrowR.AnchorLeft = 0.85f;
        arrowR.AnchorRight = 0.85f;
        arrowR.OffsetTop = -10;

        // Hint
        var hint = MakeRetroLabel(8, "\u2190 \u2192 SELECT    ENTER CONFIRM", new Color(0.4f, 0.4f, 0.5f), _charScreen);
        hint.SetAnchorsPreset(Control.LayoutPreset.Center);
        hint.AnchorTop = 0.82f;
        hint.OffsetTop = 0;
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
        bg.Color = new Color(0.03f, 0.02f, 0.06f, 0.9f);
        _boardScreen.AddChild(bg);

        // Title
        var title = MakeRetroLabel(16, "CHOOSE YOUR BOARD", new Color(1f, 0.18f, 0.61f), _boardScreen);
        title.SetAnchorsPreset(Control.LayoutPreset.Center);
        title.AnchorTop = 0.15f;
        title.OffsetTop = 0;

        // Board name
        _boardName = MakeRetroLabel(22, "", new Color(1f, 0.88f, 0.23f), _boardScreen);
        _boardName.SetAnchorsPreset(Control.LayoutPreset.Center);
        _boardName.AnchorTop = 0.35f;
        _boardName.OffsetTop = 0;

        // Description
        _boardDesc = MakeRetroLabel(10, "", new Color(0.7f, 0.7f, 0.8f), _boardScreen);
        _boardDesc.SetAnchorsPreset(Control.LayoutPreset.Center);
        _boardDesc.AnchorTop = 0.45f;
        _boardDesc.OffsetTop = 0;

        // Board preview (colored rectangle)
        // This will be set dynamically

        // Arrow indicators
        var arrowL = MakeRetroLabel(18, "\u25C0", new Color(0.5f, 0.5f, 0.6f), _boardScreen);
        arrowL.SetAnchorsPreset(Control.LayoutPreset.Center);
        arrowL.AnchorLeft = 0.15f;
        arrowL.AnchorRight = 0.15f;
        arrowL.OffsetTop = -10;

        var arrowR = MakeRetroLabel(18, "\u25B6", new Color(0.5f, 0.5f, 0.6f), _boardScreen);
        arrowR.SetAnchorsPreset(Control.LayoutPreset.Center);
        arrowR.AnchorLeft = 0.85f;
        arrowR.AnchorRight = 0.85f;
        arrowR.OffsetTop = -10;

        // Hint
        var hint = MakeRetroLabel(8, "\u2190 \u2192 SELECT    ENTER CONFIRM", new Color(0.4f, 0.4f, 0.5f), _boardScreen);
        hint.SetAnchorsPreset(Control.LayoutPreset.Center);
        hint.AnchorTop = 0.82f;
        hint.OffsetTop = 0;
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
            main.StartRide();
    }

    // ── Show/Hide ────────────────────────────────

    public void ShowTitle(Main main)
    {
        _titleScreen.Visible = true;
        _charScreen.Visible = false;
        _boardScreen.Visible = false;
        _loadingScreen.Visible = false;
        _promptLabel.Text = "";
        _countdownLabel.Text = "";
        _nearMissLabel.Text = "";
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

    public void HideAllSelectors()
    {
        _titleScreen.Visible = false;
        _charScreen.Visible = false;
        _boardScreen.Visible = false;
        _loadingScreen.Visible = false;
        _promptLabel.Text = "";
    }

    public void ShowLoading()
    {
        _loadingScreen.Visible = true;
    }

    public void HideLoading()
    {
        _loadingScreen.Visible = false;
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

    // ── HUD ──────────────────────────────────────

    public void UpdateHUD(Main main)
    {
        var player = main.PlayerMgr;
        float kmh = player.Speed * 18f;
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

        float progress = Mathf.Clamp(player.Distance / Main.CourseLength, 0f, 1f);
        _progressFill.AnchorRight = progress;
    }

    public void ShowFinish(Main main)
    {
        _promptLabel.Modulate = new Color(0.22f, 1f, 0.43f);
        int mins = (int)(main.FinishTime / 60f);
        float secs = main.FinishTime % 60f;
        string bestText = "";
        if (main.BestTime > 0f)
        {
            int bMins = (int)(main.BestTime / 60f);
            float bSecs = main.BestTime % 60f;
            bestText = $"  BEST {bMins}:{bSecs:00.0}";
        }
        _promptLabel.Text = $"FINISH! {mins}:{secs:00.0}{bestText}  |  \u2191 RIDE AGAIN";
        _progressFill.AnchorRight = 1f;
    }

    public void SetCountdown(string text, Color color)
    {
        _countdownLabel.Text = text;
        _countdownLabel.Modulate = color;
    }

    public void SetPause(string text)
    {
        _pauseLabel.Text = text;
    }

    public void SetPrompt(string text)
    {
        _promptLabel.Text = text;
    }
}
