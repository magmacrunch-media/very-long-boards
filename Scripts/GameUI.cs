using Godot;

public class GameUI
{
    private Main _main;

    public Label SpeedLabel;
    public Label DistLabel;
    public Label TimerLabel;
    public Label PromptLabel;
    public Label TitleLabel;
    public Label SubtitleLabel;
    public Label CharLabel;
    public Label BestLabel;
    public Label CourseLabel;
    public Label PauseLabel;
    public Label CountdownLabel;
    public Label NearMissLabel;
    private ColorRect _progressFill;
    private float _nearMissDisplayTimer = 0f;

    public GameUI(Main main)
    {
        _main = main;
    }

    public void Create()
    {
        var canvas = new CanvasLayer();
        _main.AddChild(canvas);

        // HUD panel
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

        // Title
        TitleLabel = MakeCenterLabel(36, "VERY LONG BOARDS", new Color(1f, 0.18f, 0.61f));
        SetAnchor(TitleLabel, 0.5f, 0.08f, -240, 240);
        canvas.AddChild(TitleLabel);

        SubtitleLabel = MakeCenterLabel(16, "A Carl Spatski Game", new Color(0.7f, 0.7f, 0.8f));
        SetAnchor(SubtitleLabel, 0.5f, 0.15f, -140, 140);
        canvas.AddChild(SubtitleLabel);

        CharLabel = MakeCenterLabel(14, "< Office Carl >", new Color(0.9f, 0.85f, 0.6f));
        SetAnchor(CharLabel, 0.5f, 0.22f, -120, 120);
        canvas.AddChild(CharLabel);

        CourseLabel = MakeCenterLabel(12, Main.CourseName, new Color(0.5f, 0.65f, 0.5f));
        SetAnchor(CourseLabel, 0.5f, 0.27f, -100, 100);
        canvas.AddChild(CourseLabel);

        PromptLabel = MakeCenterLabel(18, "Press \u2191 to kick off", new Color(1f, 0.88f, 0.23f));
        SetAnchor(PromptLabel, 0.5f, 0.8f, -140, 140);
        canvas.AddChild(PromptLabel);

        PauseLabel = MakeCenterLabel(28, "", new Color(0.8f, 0.6f, 1f));
        SetAnchor(PauseLabel, 0.5f, 0.4f, -100, 100);
        canvas.AddChild(PauseLabel);

        CountdownLabel = MakeCenterLabel(64, "", new Color(1f, 0.88f, 0.23f));
        SetAnchor(CountdownLabel, 0.5f, 0.35f, -50, 50);
        canvas.AddChild(CountdownLabel);

        NearMissLabel = MakeCenterLabel(16, "", new Color(1f, 0.85f, 0.3f));
        SetAnchor(NearMissLabel, 0.5f, 0.7f, -100, 100);
        canvas.AddChild(NearMissLabel);

        // Progress bar
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

    public void HandleTitleInput(Main main)
    {
        if (Input.IsActionJustPressed("move_left"))
        {
            main.Carl = (Main.CarlType)(((int)main.Carl + 2) % 3);
            CharLabel.Text = $"< {Main.CarlNames[(int)main.Carl]} >";
        }
        if (Input.IsActionJustPressed("move_right"))
        {
            main.Carl = (Main.CarlType)(((int)main.Carl + 1) % 3);
            CharLabel.Text = $"< {Main.CarlNames[(int)main.Carl]} >";
        }
        if (Input.IsActionJustPressed("kick_off"))
        {
            GD.Print($"KICK OFF as {Main.CarlNames[(int)main.Carl]}!");
            main.StartRide();
        }
    }

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

    public void ShowTitle(Main main)
    {
        TitleLabel.Modulate = new Color(1f, 0.18f, 0.61f);
        TitleLabel.Text = "VERY LONG BOARDS";
        SubtitleLabel.Text = "A Carl Spatski Game";
        CharLabel.Text = $"< {Main.CarlNames[(int)main.Carl]} >";
        CourseLabel.Text = Main.CourseName;
        PromptLabel.Text = "Press \u2191 to kick off";
        CountdownLabel.Text = "";
        NearMissLabel.Text = "";
        _progressFill.AnchorRight = 0f;
    }

    public void HideTitle()
    {
        TitleLabel.Text = "";
        SubtitleLabel.Text = "";
        CharLabel.Text = "";
        CourseLabel.Text = "";
        PromptLabel.Text = "";
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

    private void SetAnchor(Label label, float x, float y, float leftOff, float rightOff)
    {
        label.AnchorLeft = x; label.AnchorTop = y;
        label.AnchorRight = x; label.AnchorBottom = y;
        label.OffsetLeft = leftOff; label.OffsetRight = rightOff;
    }
}
