using Godot;

public class GameUI
{
    private Main _main;
    private FontFile _font;

    // HUD
    private Control _hud;
    private Label _speedLabel;
    private Label _distLabel;
    private Label _timerLabel;
    private Label _bestLabel;
    private Label _promptLabel;
    private Label _pauseLabel;
    private Label _countdownLabel;
    private Label _wobbleLabel;
    private ColorRect _progressFill;
    private ColorRect _speedVignette;

    // Screens
    private Control _titleScreen;
    private Label _titlePrompt;
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
        CreateCharSelect(canvas);
        CreateBoardSelect(canvas);
        CreateLevelSelect(canvas);
        HideHUD();
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

    private ColorRect Backing(Control parent, float x, float y, float w, float h)
    {
        var rect = new ColorRect();
        rect.Color = new Color(0.05f, 0.04f, 0.08f, 0.92f);
        rect.Position = new Vector2(x, y);
        rect.Size = new Vector2(w, h);
        parent.AddChild(rect);
        return rect;
    }

    // Base viewport is 320x240; these screens sit over the live 3D garage, so text
    // blocks need a dark strip under them to stay readable.
    private const float ScreenW = 320f;
    private const float ScreenH = 240f;

    /// <summary>Backing strip centred horizontally, positioned by offset from screen centre.</summary>
    private ColorRect BackingCentered(Control parent, float offsetY, float w, float h)
    {
        var rect = Backing(parent, (ScreenW - w) / 2f, ScreenH / 2f + offsetY, w, h);
        rect.Color = new Color(0.05f, 0.04f, 0.08f, 0.82f);
        return rect;
    }

    /// <summary>
    /// A screen-wide label row, vertically placed by its offset from centre. The row has to
    /// span the full width for HorizontalAlignment.Center to mean anything — a Center anchor
    /// preset leaves the label zero-width, which just runs the text off the right edge.
    /// </summary>
    private Label Row(Control parent, int size, string text, Color color, float offsetY)
    {
        var label = Retro(size, text, color);
        label.SetAnchorsPreset(Control.LayoutPreset.TopWide);
        label.AnchorTop = 0.5f;
        label.AnchorBottom = 0.5f;
        label.OffsetLeft = 0;
        label.OffsetRight = 0;
        label.OffsetTop = offsetY;
        label.OffsetBottom = offsetY + size + 5;
        parent.AddChild(label);
        return label;
    }

    // ── HUD ──────────────────────────────────────

    private void CreateHUD(CanvasLayer canvas)
    {
        _hud = new Control();
        _hud.SetAnchorsPreset(Control.LayoutPreset.FullRect);
        canvas.AddChild(_hud);

        // Dark backing behind top-left HUD cluster
        Backing(_hud, 0, 0, 80, 30);

        // Speed (top-left, big)
        _speedLabel = Retro(7, "0 km/h", new Color(1, 1, 1));
        _speedLabel.SetAnchorsPreset(Control.LayoutPreset.TopLeft);
        _speedLabel.OffsetLeft = 3;
        _speedLabel.OffsetTop = 2;
        _hud.AddChild(_speedLabel);

        // Distance
        _distLabel = Retro(5, "0 m", new Color(1, 1, 1));
        _distLabel.SetAnchorsPreset(Control.LayoutPreset.TopLeft);
        _distLabel.OffsetLeft = 3;
        _distLabel.OffsetTop = 10;
        _hud.AddChild(_distLabel);

        // Timer
        _timerLabel = Retro(5, "0:00.0", new Color(1, 1, 1));
        _timerLabel.SetAnchorsPreset(Control.LayoutPreset.TopLeft);
        _timerLabel.OffsetLeft = 3;
        _timerLabel.OffsetTop = 16;
        _hud.AddChild(_timerLabel);

        // Best time
        _bestLabel = Retro(5, "", new Color(0.7f, 0.85f, 1f));
        _bestLabel.SetAnchorsPreset(Control.LayoutPreset.TopLeft);
        _bestLabel.OffsetLeft = 3;
        _bestLabel.OffsetTop = 22;
        _hud.AddChild(_bestLabel);

        // Pause (center)
        _pauseLabel = Retro(8, "", new Color(0.8f, 0.6f, 1f));
        _pauseLabel.SetAnchorsPreset(Control.LayoutPreset.Center);
        _pauseLabel.OffsetTop = -5;
        _hud.AddChild(_pauseLabel);

        // Countdown (center)
        _countdownLabel = Retro(16, "", new Color(1f, 0.88f, 0.23f));
        _countdownLabel.SetAnchorsPreset(Control.LayoutPreset.Center);
        _countdownLabel.OffsetTop = -8;
        _hud.AddChild(_countdownLabel);

        // Wobble warning (center-low)
        _wobbleLabel = Retro(5, "", new Color(1f, 0.85f, 0.3f));
        _wobbleLabel.SetAnchorsPreset(Control.LayoutPreset.Center);
        _wobbleLabel.OffsetTop = 24;
        _hud.AddChild(_wobbleLabel);

        // Prompt (bottom-center)
        _promptLabel = Retro(5, "", new Color(1f, 0.88f, 0.23f));
        _promptLabel.SetAnchorsPreset(Control.LayoutPreset.Center);
        _promptLabel.OffsetTop = 45;
        _hud.AddChild(_promptLabel);

        // Progress bar background
        var barBg = new ColorRect();
        barBg.Color = new Color(0, 0, 0, 0.5f);
        barBg.SetAnchorsPreset(Control.LayoutPreset.BottomWide);
        barBg.AnchorTop = 0.96f;
        barBg.OffsetTop = 0;
        _hud.AddChild(barBg);

        // Progress bar fill
        _progressFill = new ColorRect();
        _progressFill.Color = new Color(0.2f, 0.85f, 0.4f, 0.8f);
        _progressFill.SetAnchorsPreset(Control.LayoutPreset.BottomWide);
        _progressFill.AnchorTop = 0.96f;
        _progressFill.AnchorRight = 0f;
        _progressFill.OffsetTop = 0;
        _hud.AddChild(_progressFill);

        // Speed vignette — darkens edges at high speed
        _speedVignette = new ColorRect();
        _speedVignette.SetAnchorsPreset(Control.LayoutPreset.FullRect);
        _speedVignette.Color = new Color(0, 0, 0, 0f);
        _speedVignette.MouseFilter = Control.MouseFilterEnum.Ignore;
        _hud.AddChild(_speedVignette);
    }

    // ── Title Screen ─────────────────────────────

    // The backdrop is the live 3D garage, so this is text over transparency —
    // just dark strips behind each block to keep it legible against the room.
    private void CreateTitleScreen(CanvasLayer canvas)
    {
        _titleScreen = new Control();
        _titleScreen.SetAnchorsPreset(Control.LayoutPreset.FullRect);
        canvas.AddChild(_titleScreen);

        // Title up top over blank wall, prompt down low — the middle band stays clear so
        // the garage itself is the picture.
        BackingCentered(_titleScreen, -98, 320, 40);
        Row(_titleScreen, 12, "VERY LONG BOARDS", new Color(1f, 0.18f, 0.61f), -94);
        Row(_titleScreen, 6, "A Carl Spatski Game", new Color(0.62f, 0.62f, 0.72f), -76);

        BackingCentered(_titleScreen, 62, 320, 16);
        _titlePrompt = Row(_titleScreen, 7, "PRESS \u2191 TO START", new Color(1f, 0.88f, 0.23f), 65);

        BackingCentered(_titleScreen, 100, 320, 14);
        Row(_titleScreen, 5, "\u2190\u2192 STEER   SPACE BRAKE   \u2191 KICK",
            new Color(0.45f, 0.45f, 0.55f), 103);
    }

    /// <summary>Pulse the start prompt. Driven from Main's Title state.</summary>
    public void UpdateTitleBlink(float titleTime)
    {
        if (_titlePrompt == null) return;
        float blink = Mathf.Sin(titleTime * 3f) * 0.3f + 0.7f;
        _titlePrompt.Modulate = new Color(1f, 0.88f, 0.23f, blink);
    }

    // ── Character Select ─────────────────────────

    // All three select screens use the same frame: a title banner pinned near the top and
    // an info block near the bottom, leaving the middle clear for what the camera is on.
    private void SelectBanner(Control screen, string text)
    {
        BackingCentered(screen, -104, 320, 16);
        Row(screen, 6, text, new Color(1f, 0.18f, 0.61f), -101);
    }

    private void SelectHint(Control screen)
    {
        BackingCentered(screen, 92, 320, 14);
        Row(screen, 5, "\u2190\u2192 PICK   \u2191 OK   SPACE BACK", new Color(0.45f, 0.45f, 0.55f), 95);
    }

    private void CreateCharSelect(CanvasLayer canvas)
    {
        _charScreen = new Control();
        _charScreen.SetAnchorsPreset(Control.LayoutPreset.FullRect);
        _charScreen.Visible = false;
        canvas.AddChild(_charScreen);

        SelectBanner(_charScreen, "CARL'S STYLE");

        BackingCentered(_charScreen, 52, 320, 38);
        _charName = Row(_charScreen, 8, "", new Color(1f, 0.88f, 0.23f), 55);
        _charDesc = Row(_charScreen, 5, "", new Color(0.7f, 0.7f, 0.8f), 68);
        _charStats = Row(_charScreen, 5, "", new Color(0.5f, 0.8f, 0.5f), 78);

        SelectHint(_charScreen);
    }

    // ── Board Select ─────────────────────────────

    private void CreateBoardSelect(CanvasLayer canvas)
    {
        _boardScreen = new Control();
        _boardScreen.SetAnchorsPreset(Control.LayoutPreset.FullRect);
        _boardScreen.Visible = false;
        canvas.AddChild(_boardScreen);

        SelectBanner(_boardScreen, "CHOOSE YOUR BOARD");

        BackingCentered(_boardScreen, 58, 320, 28);
        _boardName = Row(_boardScreen, 8, "", new Color(1f, 0.88f, 0.23f), 61);
        _boardDesc = Row(_boardScreen, 5, "", new Color(0.7f, 0.7f, 0.8f), 74);

        SelectHint(_boardScreen);
    }

    // ── Level Select ─────────────────────────────

    private void CreateLevelSelect(CanvasLayer canvas)
    {
        _levelScreen = new Control();
        _levelScreen.SetAnchorsPreset(Control.LayoutPreset.FullRect);
        _levelScreen.Visible = false;
        canvas.AddChild(_levelScreen);

        SelectBanner(_levelScreen, "CHOOSE YOUR COURSE");

        BackingCentered(_levelScreen, 58, 320, 28);
        _levelName = Row(_levelScreen, 8, "", new Color(1f, 0.88f, 0.23f), 61);
        _levelDesc = Row(_levelScreen, 5, "", new Color(0.7f, 0.7f, 0.8f), 74);

        SelectHint(_levelScreen);
    }


    // ── Input Handlers ───────────────────────────

    public void HandleTitleInput(Main main)
    {
        if (Input.IsActionJustPressed("kick_off"))
            main.ShowCharSelect();
    }

    /// <summary>Space/↓ or Esc backs out of any select screen.</summary>
    private static bool BackPressed()
    {
        return Input.IsActionJustPressed("brake") || Input.IsActionJustPressed("pause");
    }

    public void HandleCharSelectInput(Main main)
    {
        int count = Main.CarlNames.Length;
        if (Input.IsActionJustPressed("move_left"))
        {
            main.Carl = (Main.CarlType)(((int)main.Carl + count - 1) % count);
            main.Garage.UpdateDisplayModel();
            UpdateCharSelect(main);
        }
        if (Input.IsActionJustPressed("move_right"))
        {
            main.Carl = (Main.CarlType)(((int)main.Carl + 1) % count);
            main.Garage.UpdateDisplayModel();
            UpdateCharSelect(main);
        }
        if (Input.IsActionJustPressed("kick_off")) main.ShowBoardSelect();
        else if (BackPressed()) main.GoBack();
    }

    public void HandleBoardSelectInput(Main main)
    {
        int count = Main.BoardNames.Length;
        if (Input.IsActionJustPressed("move_left"))
        {
            main.Board = (Main.BoardType)(((int)main.Board + count - 1) % count);
            main.Garage.UpdateRackHighlight();
            main.Garage.UpdatePickedBoard();
            UpdateBoardSelect(main);
        }
        if (Input.IsActionJustPressed("move_right"))
        {
            main.Board = (Main.BoardType)(((int)main.Board + 1) % count);
            main.Garage.UpdateRackHighlight();
            main.Garage.UpdatePickedBoard();
            UpdateBoardSelect(main);
        }
        if (Input.IsActionJustPressed("kick_off")) main.ShowLevelSelect();
        else if (BackPressed()) main.GoBack();
    }

    public void HandleLevelSelectInput(Main main)
    {
        int count = Main.LevelNames.Length;
        if (Input.IsActionJustPressed("move_left"))
        {
            main.Level = (Main.LevelType)(((int)main.Level + count - 1) % count);
            main.Garage.UpdatePosterHighlight();
            UpdateLevelSelect(main);
        }
        if (Input.IsActionJustPressed("move_right"))
        {
            main.Level = (Main.LevelType)(((int)main.Level + 1) % count);
            main.Garage.UpdatePosterHighlight();
            UpdateLevelSelect(main);
        }
        // Only built courses can be started; the placeholder posters just say so.
        if (Input.IsActionJustPressed("kick_off") && Main.LevelUnlocked[(int)main.Level])
            main.StartRide();
        else if (BackPressed()) main.GoBack();
    }

    // ── Show/Hide ────────────────────────────────

    public void ShowTitle(Main main)
    {
        HideHUD();
        _titleScreen.Visible = true;
        _charScreen.Visible = false;
        _boardScreen.Visible = false;
        _levelScreen.Visible = false;
        _promptLabel.Text = "";
        _countdownLabel.Text = "";
        _wobbleLabel.Text = "";
        _progressFill.AnchorRight = 0f;
    }

    // Each of these sets every screen explicitly — navigation runs backwards as well as
    // forwards now, so "hide the one I came from" isn't enough.
    public void ShowCharSelect(Main main)
    {
        HideHUD();
        _titleScreen.Visible = false;
        _charScreen.Visible = true;
        _boardScreen.Visible = false;
        _levelScreen.Visible = false;
        UpdateCharSelect(main);
    }

    public void ShowBoardSelect(Main main)
    {
        HideHUD();
        _titleScreen.Visible = false;
        _charScreen.Visible = false;
        _boardScreen.Visible = true;
        _levelScreen.Visible = false;
        UpdateBoardSelect(main);
    }

    public void ShowLevelSelect(Main main)
    {
        HideHUD();
        _titleScreen.Visible = false;
        _charScreen.Visible = false;
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
        _promptLabel.Text = "";
    }

    public void HideHUD()
    {
        if (_hud != null) _hud.Visible = false;
    }

    public void ShowHUD()
    {
        if (_hud != null) _hud.Visible = true;
    }

    private void UpdateCharSelect(Main main)
    {
        _charName.Text = "\u25C0 " + Main.CarlNames[(int)main.Carl] + " \u25B6";
        _charDesc.Text = Main.CarlDescs[(int)main.Carl];

        // Drawn from the same table PlayerManager rides with, so the bars can't drift
        // away from the handling they advertise.
        var s = Main.CarlStats[(int)main.Carl];
        _charStats.Text = $"SPD {Bar(s.Speed)} HAND {Bar(s.Handling)} TRK {Bar(s.Tracking)}";
    }

    private static string Bar(int pips)
    {
        var sb = new System.Text.StringBuilder("[");
        for (int i = 0; i < 5; i++) sb.Append(i < pips ? '\u2588' : '\u2581');
        return sb.Append(']').ToString();
    }

    private void UpdateBoardSelect(Main main)
    {
        _boardName.Text = "\u25C0 " + Main.BoardNames[(int)main.Board] + " \u25B6";
        _boardDesc.Text = Main.BoardDescs[(int)main.Board];
    }

    private void UpdateLevelSelect(Main main)
    {
        int i = (int)main.Level;
        bool unlocked = Main.LevelUnlocked[i];

        _levelName.Text = "\u25C0 " + Main.LevelNames[i] + " \u25B6";
        _levelName.Modulate = unlocked ? new Color(1f, 0.88f, 0.23f) : new Color(0.55f, 0.55f, 0.6f);
        _levelDesc.Text = unlocked ? Main.LevelDescs[i] : "LOCKED - COMING SOON";
        _levelDesc.Modulate = unlocked ? new Color(0.7f, 0.7f, 0.8f) : new Color(0.85f, 0.4f, 0.4f);
    }

    // ── HUD ──────────────────────────────────────

    public void UpdateHUD(Main main)
    {
        var player = main.PlayerMgr;
        float kmh = player.Speed * 3.6f;   // Speed is m/s
        _speedLabel.Text = $"{kmh:F0} km/h";

        float speedRatio = Mathf.Clamp(player.Speed / player.MaxSpeed, 0f, 1f);
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
