using Godot;

public class GameUI
{
    private Main _main;

    // HUD
    public Label SpeedLabel;
    public Label DistLabel;
    public Label TimerLabel;
    public Label BestLabel;
    public Label PromptLabel;
    public Label PauseLabel;
    public Label CountdownLabel;
    public Label NearMissLabel;
    private ColorRect _progressFill;

    // Title
    public Label TitleLabel;
    public Label SubtitleLabel;
    public Label CourseLabel;

    // Char Select
    private PanelContainer _charPanel;
    private Label _charTitle;
    private Label _charName;
    private Label _charDesc;
    private Label _charStats;
    private Label _charHint;

    // Board Select
    private PanelContainer _boardPanel;
    private Label _boardTitle;
    private Label _boardName;
    private Label _boardDesc;
    private Label _boardHint;

    public GameUI(Main main)
    {
        _main = main;
    }

    public void Create()
    {
        var canvas = new CanvasLayer();
        _main.AddChild(canvas);

        CreateHUD(canvas);
        CreateTitle(canvas);
        CreateCharSelect(canvas);
        CreateBoardSelect(canvas);
    }

    private void CreateHUD(CanvasLayer canvas)
    {
        var panel = new PanelContainer();
        panel.AnchorLeft = 0; panel.AnchorTop = 0;
        panel.AnchorRight = 0; panel.AnchorBottom = 0;
        panel.OffsetLeft = 16; panel.OffsetTop = 12;
        panel.OffsetRight = 260; panel.OffsetBottom = 150;
        var ps = new StyleBoxFlat();
        ps.BgColor = new Color(0, 0, 0, 0.55f);
        ps.CornerRadiusTopLeft = 6; ps.CornerRadiusTopRight = 6;
        ps.CornerRadiusBottomLeft = 6; ps.CornerRadiusBottomRight = 6;
        panel.AddThemeStyleboxOverride("panel", ps);

        var vbox = new VBoxContainer();
        vbox.AddThemeConstantOverride("separation", 6);

        SpeedLabel = MakeLabel(22, "0 km/h");
        vbox.AddChild(SpeedLabel);
        DistLabel = MakeLabel(16, "0 m");
        vbox.AddChild(DistLabel);
        TimerLabel = MakeLabel(16, "0:00.0");
        vbox.AddChild(TimerLabel);
        BestLabel = MakeLabel(12, "");
        BestLabel.Modulate = new Color(0.7f, 0.85f, 1f);
        vbox.AddChild(BestLabel);

        panel.AddChild(vbox);
        canvas.AddChild(panel);

        PauseLabel = MakeCenterLabel(28, "", new Color(0.8f, 0.6f, 1f));
        SetAnchor(PauseLabel, 0.5f, 0.4f, -100, 100);
        canvas.AddChild(PauseLabel);

        CountdownLabel = MakeCenterLabel(64, "", new Color(1f, 0.88f, 0.23f));
        SetAnchor(CountdownLabel, 0.5f, 0.35f, -50, 50);
        canvas.AddChild(CountdownLabel);

        NearMissLabel = MakeCenterLabel(16, "", new Color(1f, 0.85f, 0.3f));
        SetAnchor(NearMissLabel, 0.5f, 0.7f, -100, 100);
        canvas.AddChild(NearMissLabel);

        PromptLabel = MakeCenterLabel(16, "", new Color(1f, 0.88f, 0.23f));
        SetAnchor(PromptLabel, 0.5f, 0.85f, -160, 160);
        canvas.AddChild(PromptLabel);

        var progressBg = new ColorRect();
        progressBg.Color = new Color(0, 0, 0, 0.4f);
        progressBg.SetAnchorsPreset(Control.LayoutPreset.BottomWide);
        progressBg.AnchorTop = 0.96f;
        canvas.AddChild(progressBg);

        _progressFill = new ColorRect();
        _progressFill.Color = new Color(0.2f, 0.85f, 0.4f, 0.8f);
        _progressFill.SetAnchorsPreset(Control.LayoutPreset.BottomWide);
        _progressFill.AnchorTop = 0.96f;
        _progressFill.AnchorRight = 0f;
        canvas.AddChild(_progressFill);
    }

    private void CreateTitle(CanvasLayer canvas)
    {
        TitleLabel = MakeCenterLabel(36, "VERY LONG BOARDS", new Color(1f, 0.18f, 0.61f));
        SetAnchor(TitleLabel, 0.5f, 0.1f, -240, 240);
        canvas.AddChild(TitleLabel);

        SubtitleLabel = MakeCenterLabel(16, "A Carl Spatski Game", new Color(0.7f, 0.7f, 0.8f));
        SetAnchor(SubtitleLabel, 0.5f, 0.17f, -140, 140);
        canvas.AddChild(SubtitleLabel);

        CourseLabel = MakeCenterLabel(12, Main.CourseName, new Color(0.5f, 0.65f, 0.5f));
        SetAnchor(CourseLabel, 0.5f, 0.23f, -100, 100);
        canvas.AddChild(CourseLabel);
    }

    private void CreateCharSelect(CanvasLayer canvas)
    {
        _charPanel = MakePanel(canvas, 0.25f, 0.2f, 0.75f, 0.8f);

        _charTitle = MakeCenterLabel(20, "SELECT YOUR CARL", new Color(1f, 0.18f, 0.61f));
        SetAnchor(_charTitle, 0.5f, 0.08f, -160, 160);
        _charPanel.AddChild(_charTitle);

        _charName = MakeCenterLabel(24, "", new Color(1f, 0.88f, 0.23f));
        SetAnchor(_charName, 0.5f, 0.25f, -140, 140);
        _charPanel.AddChild(_charName);

        _charDesc = MakeCenterLabel(14, "", new Color(0.7f, 0.7f, 0.8f));
        SetAnchor(_charDesc, 0.5f, 0.38f, -120, 120);
        _charPanel.AddChild(_charDesc);

        _charStats = MakeCenterLabel(11, "", new Color(0.6f, 0.8f, 0.6f));
        SetAnchor(_charStats, 0.5f, 0.5f, -140, 140);
        _charPanel.AddChild(_charStats);

        _charHint = MakeCenterLabel(11, "\u2190 \u2192 to select  |  Enter to confirm", new Color(0.5f, 0.5f, 0.6f));
        SetAnchor(_charHint, 0.5f, 0.85f, -180, 180);
        _charPanel.AddChild(_charHint);

        _charPanel.Visible = false;
    }

    private void CreateBoardSelect(CanvasLayer canvas)
    {
        _boardPanel = MakePanel(canvas, 0.25f, 0.25f, 0.75f, 0.75f);

        _boardTitle = MakeCenterLabel(20, "CHOOSE YOUR BOARD", new Color(1f, 0.18f, 0.61f));
        SetAnchor(_boardTitle, 0.5f, 0.08f, -160, 160);
        _boardPanel.AddChild(_boardTitle);

        _boardName = MakeCenterLabel(24, "", new Color(1f, 0.88f, 0.23f));
        SetAnchor(_boardName, 0.5f, 0.28f, -140, 140);
        _boardPanel.AddChild(_boardName);

        _boardDesc = MakeCenterLabel(14, "", new Color(0.7f, 0.7f, 0.8f));
        SetAnchor(_boardDesc, 0.5f, 0.42f, -120, 120);
        _boardPanel.AddChild(_boardDesc);

        _boardHint = MakeCenterLabel(11, "\u2190 \u2192 to select  |  Enter to confirm", new Color(0.5f, 0.5f, 0.6f));
        SetAnchor(_boardHint, 0.5f, 0.85f, -180, 180);
        _boardPanel.AddChild(_boardHint);

        _boardPanel.Visible = false;
    }

    private PanelContainer MakePanel(CanvasLayer canvas, float left, float top, float right, float bottom)
    {
        var panel = new PanelContainer();
        panel.AnchorLeft = left; panel.AnchorTop = top;
        panel.AnchorRight = right; panel.AnchorBottom = bottom;
        var ps = new StyleBoxFlat();
        ps.BgColor = new Color(0.05f, 0.05f, 0.08f, 0.92f);
        ps.BorderColor = new Color(0.3f, 0.3f, 0.4f);
        ps.CornerRadiusTopLeft = 8; ps.CornerRadiusTopRight = 8;
        ps.CornerRadiusBottomLeft = 8; ps.CornerRadiusBottomRight = 8;
        panel.AddThemeStyleboxOverride("panel", ps);
        canvas.AddChild(panel);
        return panel;
    }

    // ── Input Handlers ───────────────────────────

    public void HandleTitleInput(Main main)
    {
        if (Input.IsActionJustPressed("kick_off"))
        {
            main.ShowCharSelect();
        }
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
        {
            main.ShowBoardSelect();
        }
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
        {
            main.StartRide();
        }
    }

    // ── Show/Hide ────────────────────────────────

    public void ShowTitle(Main main)
    {
        TitleLabel.Text = "VERY LONG BOARDS";
        SubtitleLabel.Text = "A Carl Spatski Game";
        CourseLabel.Text = Main.CourseName;
        PromptLabel.Text = "Press \u2191 to start";
        CountdownLabel.Text = "";
        NearMissLabel.Text = "";
        _progressFill.AnchorRight = 0f;
        _charPanel.Visible = false;
        _boardPanel.Visible = false;
    }

    public void ShowCharSelect(Main main)
    {
        TitleLabel.Text = "";
        SubtitleLabel.Text = "";
        CourseLabel.Text = "";
        PromptLabel.Text = "";
        _charPanel.Visible = true;
        _boardPanel.Visible = false;
        UpdateCharSelect(main);
    }

    public void ShowBoardSelect(Main main)
    {
        _charPanel.Visible = false;
        _boardPanel.Visible = true;
        UpdateBoardSelect(main);
    }

    public void HideAllSelectors()
    {
        TitleLabel.Text = "";
        SubtitleLabel.Text = "";
        CourseLabel.Text = "";
        PromptLabel.Text = "";
        _charPanel.Visible = false;
        _boardPanel.Visible = false;
    }

    private void UpdateCharSelect(Main main)
    {
        _charName.Text = Main.CarlNames[(int)main.Carl];
        _charDesc.Text = Main.CarlDescs[(int)main.Carl];

        string stats = "";
        switch (main.Carl)
        {
            case Main.CarlType.Office:
                stats = "SPD \u2584\u2584\u2584\u2584\u2581  HAND \u2584\u2584\u2584\u2584\u2581  TRK \u2584\u2584\u2584\u2584\u2581";
                break;
            case Main.CarlType.Party:
                stats = "SPD \u2588\u2588\u2588\u2588\u2588  HAND \u2588\u2588\u2588\u2581\u2581  TRK \u2588\u2588\u2588\u2581\u2581";
                break;
            case Main.CarlType.Dark:
                stats = "SPD \u2588\u2588\u2588\u2588\u2581  HAND \u2588\u2588\u2588\u2588\u2581  TRK \u2588\u2588\u2588\u2588\u2588";
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
        SpeedLabel.Text = $"{kmh:F0} km/h";

        float speedRatio = Mathf.Clamp(player.Speed / PlayerManager.MaxSpeed, 0f, 1f);
        if (speedRatio > 0.8f)
            SpeedLabel.Modulate = new Color(1f, 0.3f, 0.3f);
        else if (speedRatio > 0.5f)
            SpeedLabel.Modulate = new Color(1f, 0.9f, 0.3f);
        else
            SpeedLabel.Modulate = new Color(1f, 1f, 1f);

        DistLabel.Text = $"{player.Distance:F0} m";
        int mins = (int)(main.Timer / 60f);
        float secs = main.Timer % 60f;
        TimerLabel.Text = $"{mins}:{secs:00.0}";

        if (main.BestTime > 0f)
        {
            int bMins = (int)(main.BestTime / 60f);
            float bSecs = main.BestTime % 60f;
            BestLabel.Text = $"Best: {bMins}:{bSecs:00.0}";
        }

        float progress = Mathf.Clamp(player.Distance / Main.CourseLength, 0f, 1f);
        _progressFill.AnchorRight = progress;
    }

    public void ShowFinish(Main main)
    {
        TitleLabel.Modulate = new Color(0.22f, 1f, 0.43f);
        TitleLabel.Text = "FINISH!";
        int mins = (int)(main.FinishTime / 60f);
        float secs = main.FinishTime % 60f;
        string bestText = "";
        if (main.BestTime > 0f)
        {
            int bMins = (int)(main.BestTime / 60f);
            float bSecs = main.BestTime % 60f;
            bestText = $"  |  Best: {bMins}:{bSecs:00.0}";
        }
        PromptLabel.Text = $"Time: {mins}:{secs:00.0}{bestText}   |   Press \u2191 to ride again";
        _progressFill.AnchorRight = 1f;
    }

    // ── Helpers ──────────────────────────────────

    private Label MakeLabel(int fontSize, string text)
    {
        var label = new Label();
        label.AddThemeFontSizeOverride("font_size", fontSize);
        label.Text = text;
        return label;
    }

    private Label MakeCenterLabel(int fontSize, string text, Color color)
    {
        var label = new Label();
        label.HorizontalAlignment = HorizontalAlignment.Center;
        label.AddThemeFontSizeOverride("font_size", fontSize);
        label.Text = text;
        label.Modulate = color;
        return label;
    }

    private void SetAnchor(Control control, float x, float y, float leftOff, float rightOff)
    {
        control.AnchorLeft = x; control.AnchorTop = y;
        control.AnchorRight = x; control.AnchorBottom = y;
        control.OffsetLeft = leftOff; control.OffsetRight = rightOff;
    }
}
