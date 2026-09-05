using Godot;

/// <summary>
/// Carl's garage — the game's hub. It is the title screen backdrop and all three select
/// screens; only the camera moves between them.
///
/// Floor plan (keep in sync with garage_layout.py, which plots frustums and checks clearances):
///   Left wall  (x=-5.75)  four level posters
///   Back wall  (z=-2.75)  workbench + pegboard on the left, board rack on the right
///   Right wall (x= 5.75)  shelf and window
///   Floor centre-left     Carl's podium
/// </summary>
public class GarageManager
{
    /// <summary>Which camera setup the garage is holding. One per game screen.</summary>
    public enum Shot { Char, Board, Level }

    private Main _main;
    private Node3D _garageRoot;

    // ── Layout anchors (mirrored in garage_layout.py) ──
    private const float WallLeftX = -5.75f;
    private const float WallBackZ = -2.75f;
    private const float PosterX = -5.72f;
    private const float PosterY = 2.4f;
    private static readonly float[] PosterZ = { -1.8f, -0.4f, 1.0f, 2.4f };
    private const float PosterCamX = -3.2f;

    private static readonly float[] RackX = { 0.8f, 1.6f, 2.4f, 3.2f };
    private const float RackZ = -2.5f;
    private const float RackY = 1.5f;
    private const float RackSelectedZ = -2.24f;
    private const float RackTilt = 0.15f;

    private const float PodiumX = -1.2f;
    private const float PodiumZ = 1.4f;
    private const float PodiumTop = 0.12f;

    // ── Character display ──
    private Node3D _charDisplay;   // podium-anchored root; Carl is rebuilt inside it
    private Node3D _carl;
    private Node3D _floorBoard;    // the picked board, lying beside the podium
    private MeshInstance3D _podiumRing;
    private StandardMaterial3D _podiumRingMat;
    private float _styleFlash = 0f;

    // ── Board rack ──
    private readonly Node3D[] _rackSlots = new Node3D[4];
    private readonly Node3D[] _rackBoards = new Node3D[4];

    // ── Level posters ──
    private readonly Node3D[] _posters = new Node3D[4];
    private readonly Label3D[] _posterTitles = new Label3D[4];
    private readonly Label3D[] _posterSubs = new Label3D[4];

    // ── Camera lerp state ──
    private Vector3 _camPos;
    private Vector3 _camLookAt;
    private bool _camInitialized = false;

    private const float CamLerpSpeed = 3f;
    private const float DimFactor = 0.45f;

    public GarageManager(Main main)
    {
        _main = main;
    }

    public void Create()
    {
        _garageRoot = new Node3D();
        _garageRoot.Visible = false;
        _main.AddChild(_garageRoot);

        BuildRoom();
        BuildWorkbench();
        BuildPegboard();
        BuildShelf();
        BuildWindow();
        BuildNeonSign();
        BuildLighting();
        BuildBoardRack();
        BuildPodium();
        BuildLevelPosters();

        UpdateDisplayModel();
        UpdateRackHighlight();
        UpdatePosterHighlight();
    }

    // ═══════════════════════════════════════════
    //  ROOM STRUCTURE
    // ═══════════════════════════════════════════

    private void BuildRoom()
    {
        var grooveColor = new Color(0.2f, 0.2f, 0.2f);

        // Shell
        Box(new Vector3(12f, 5f, 0.5f), new Color(0.23f, 0.23f, 0.23f), new Vector3(0, 2.5f, -3f));
        Box(new Vector3(0.5f, 5f, 8.5f), new Color(0.21f, 0.21f, 0.21f), new Vector3(-6f, 2.5f, 0.75f));
        Box(new Vector3(0.5f, 5f, 8.5f), new Color(0.21f, 0.21f, 0.21f), new Vector3(6f, 2.5f, 0.75f));
        Box(new Vector3(12f, 0.1f, 8.5f), new Color(0.33f, 0.33f, 0.33f), new Vector3(0, -0.05f, 0.75f));
        Box(new Vector3(12f, 0.15f, 8.5f), new Color(0.2f, 0.2f, 0.2f), new Vector3(0, 5.05f, 0.75f));

        // Cinder block courses — back wall
        for (int row = 0; row < 6; row++)
        {
            float y = row * 0.8f + 0.4f;
            Box(new Vector3(12f, 0.02f, 0.01f), grooveColor, new Vector3(0, y, WallBackZ + 0.02f));
            for (int col = 0; col < 8; col++)
            {
                float x = -5f + col * 1.5f + (row % 2 == 0 ? 0f : 0.75f);
                Box(new Vector3(0.02f, 0.8f, 0.01f), grooveColor, new Vector3(x, y, WallBackZ + 0.02f));
            }
        }

        // Cinder block courses — side walls
        foreach (float wallX in new[] { WallLeftX + 0.02f, -WallLeftX - 0.02f })
        {
            for (int row = 0; row < 6; row++)
            {
                float y = row * 0.8f + 0.4f;
                Box(new Vector3(0.01f, 0.02f, 8.5f), grooveColor, new Vector3(wallX, y, 0.75f));
                for (int col = 0; col < 6; col++)
                {
                    float z = -2.5f + col * 1.5f + (row % 2 == 0 ? 0f : 0.75f);
                    Box(new Vector3(0.01f, 0.8f, 0.02f), grooveColor, new Vector3(wallX, y, z));
                }
            }
        }

        // Floor seams
        for (int i = 0; i < 6; i++)
            Box(new Vector3(12f, 0.005f, 0.02f), new Color(0.3f, 0.3f, 0.3f),
                new Vector3(0, 0.01f, -2f + i * 1.5f));

        // Oil stain
        var stainMat = MeshKit.Mat(new Color(0.18f, 0.18f, 0.18f, 0.4f));
        stainMat.Transparency = BaseMaterial3D.TransparencyEnum.Alpha;
        MeshKit.Cylinder(_garageRoot, 0.5f, 0.5f, 0.01f, stainMat, new Vector3(1.2f, 0.01f, 0.4f),
            segments: 6);
    }

    private void BuildWorkbench()
    {
        var wood = new Color(0.43f, 0.3f, 0.16f);
        var darkWood = new Color(0.35f, 0.24f, 0.12f);

        Box(new Vector3(2.4f, 0.1f, 0.7f), wood, new Vector3(-4f, 0.85f, -2.35f));
        Box(new Vector3(0.08f, 0.85f, 0.08f), darkWood, new Vector3(-5.1f, 0.42f, -2.35f));
        Box(new Vector3(0.08f, 0.85f, 0.08f), darkWood, new Vector3(-2.9f, 0.42f, -2.35f));
        Box(new Vector3(2.1f, 0.06f, 0.55f), darkWood, new Vector3(-4f, 0.35f, -2.35f));

        // Clutter
        Box(new Vector3(0.15f, 0.12f, 0.12f), new Color(0.55f, 0.55f, 0.55f), new Vector3(-4.8f, 0.97f, -2.35f));
        Box(new Vector3(0.1f, 0.08f, 0.1f), new Color(0.4f, 0.4f, 0.4f), new Vector3(-4.4f, 0.94f, -2.35f));
        Box(new Vector3(0.3f, 0.1f, 0.15f), new Color(0.6f, 0.6f, 0.6f), new Vector3(-3.6f, 0.95f, -2.35f));
        Box(new Vector3(0.15f, 0.04f, 0.06f), new Color(1f, 0.42f, 0.21f), new Vector3(-3.6f, 1.01f, -2.35f));
    }

    private void BuildPegboard()
    {
        Box(new Vector3(1.8f, 1.2f, 0.05f), new Color(0.28f, 0.28f, 0.28f), new Vector3(-4f, 2.4f, -2.72f));

        // Hammer
        Box(new Vector3(0.04f, 0.5f, 0.04f), new Color(0.43f, 0.3f, 0.16f), new Vector3(-4.6f, 2.3f, -2.66f));
        Box(new Vector3(0.2f, 0.08f, 0.08f), new Color(0.55f, 0.55f, 0.55f), new Vector3(-4.6f, 2.6f, -2.66f));
        // Wrench
        Box(new Vector3(0.04f, 0.5f, 0.04f), new Color(0.6f, 0.6f, 0.6f), new Vector3(-4f, 2.2f, -2.66f));
        Box(new Vector3(0.14f, 0.08f, 0.08f), new Color(0.6f, 0.6f, 0.6f), new Vector3(-4f, 2.5f, -2.66f));
        // Screwdriver
        Box(new Vector3(0.03f, 0.35f, 0.03f), new Color(1f, 0.18f, 0.61f), new Vector3(-3.4f, 2.25f, -2.66f));
        Box(new Vector3(0.02f, 0.15f, 0.02f), new Color(0.8f, 0.8f, 0.8f), new Vector3(-3.4f, 2.5f, -2.66f));
    }

    private void BuildShelf()
    {
        // On the right wall now — the back wall belongs to the rack.
        var darkWood = new Color(0.35f, 0.24f, 0.12f);
        Box(new Vector3(0.4f, 0.06f, 2f), darkWood, new Vector3(5.5f, 2.6f, -1f));
        Box(new Vector3(0.06f, 0.8f, 0.06f), darkWood, new Vector3(5.5f, 2.2f, -1.9f));
        Box(new Vector3(0.06f, 0.8f, 0.06f), darkWood, new Vector3(5.5f, 2.2f, -0.1f));

        Box(new Vector3(0.12f, 0.22f, 0.12f), new Color(0.29f, 0.56f, 0.85f), new Vector3(5.5f, 2.75f, -1.6f));
        Box(new Vector3(0.12f, 0.18f, 0.12f), new Color(1f, 0.27f, 0.27f), new Vector3(5.5f, 2.73f, -1.2f));
        Box(new Vector3(0.14f, 0.2f, 0.14f), new Color(0.27f, 0.8f, 0.27f), new Vector3(5.5f, 2.74f, -0.8f));
        Box(new Vector3(0.14f, 0.14f, 0.2f), new Color(1f, 0.67f, 0f), new Vector3(5.5f, 2.7f, -0.4f));
    }

    private void BuildWindow()
    {
        var frame = new Color(0.33f, 0.33f, 0.33f);
        Box(new Vector3(0.08f, 1.4f, 1.2f), frame, new Vector3(5.72f, 3.2f, 1.2f));

        var glassMat = MeshKit.Mat(new Color(0.48f, 0.68f, 0.8f, 0.85f));
        glassMat.Transparency = BaseMaterial3D.TransparencyEnum.Alpha;
        MeshKit.Box(_garageRoot, new Vector3(0.02f, 1.2f, 1f), glassMat, new Vector3(5.68f, 3.2f, 1.2f));

        Box(new Vector3(0.04f, 1.2f, 0.04f), frame, new Vector3(5.66f, 3.2f, 1.2f));
        Box(new Vector3(0.04f, 0.04f, 1f), frame, new Vector3(5.66f, 3.2f, 1.2f));

        // Daylight pooling on the floor below the window
        var glowMat = MeshKit.Mat(new Color(0.48f, 0.68f, 0.8f, 0.07f));
        glowMat.Transparency = BaseMaterial3D.TransparencyEnum.Alpha;
        MeshKit.Box(_garageRoot, new Vector3(2f, 0.005f, 1.5f), glowMat, new Vector3(4.6f, 0.015f, 1.2f));
    }

    private void BuildNeonSign()
    {
        var sign = new Label3D();
        sign.Text = "VLB";
        sign.FontSize = 20;
        sign.OutlineSize = 0;
        // Modulate, never MaterialOverride — overriding replaces the glyph shader and the
        // sign renders as a solid rectangle. Label3D is unshaded already, so this reads as neon.
        sign.Modulate = new Color(1f, 0.32f, 0.70f);
        sign.Position = new Vector3(-1.2f, 4.3f, -2.7f);
        _garageRoot.AddChild(sign);

        var tagline = new Label3D();
        tagline.Text = "DOWNHILL SKATEBOARDS";
        tagline.FontSize = 6;
        tagline.OutlineSize = 0;
        tagline.Modulate = new Color(1f, 0.55f, 0.78f);
        tagline.Position = new Vector3(-1.2f, 4.0f, -2.7f);
        _garageRoot.AddChild(tagline);
    }

    private void BuildLighting()
    {
        // Ceiling fluorescents — general fill so the room isn't mud.
        foreach (float x in new[] { -2.5f, 1.5f })
        {
            Box(new Vector3(1.6f, 0.06f, 0.2f), new Color(0.85f, 0.85f, 0.85f), new Vector3(x, 4.9f, -0.5f));

            var fill = new OmniLight3D();
            fill.Position = new Vector3(x, 4.6f, -0.5f);
            fill.LightEnergy = 1.1f;
            fill.LightColor = new Color(1f, 0.98f, 0.94f);
            fill.OmniRange = 11f;
            fill.OmniAttenuation = 0.9f;
            _garageRoot.AddChild(fill);
        }

        // Podium key light, straight down onto Carl.
        var podiumSpot = new SpotLight3D();
        podiumSpot.Position = new Vector3(PodiumX, 3.6f, PodiumZ);
        podiumSpot.Rotation = new Vector3(-Mathf.Pi / 2f, 0, 0);
        podiumSpot.LightEnergy = 2.6f;
        podiumSpot.LightColor = new Color(1f, 0.97f, 0.9f);
        podiumSpot.SpotRange = 7f;
        podiumSpot.SpotAngle = 34f;
        podiumSpot.SpotAngleAttenuation = 0.6f;
        _garageRoot.AddChild(podiumSpot);

        // Rack wash — angled so it hits the deck faces, not just their top edges.
        var rackSpot = new SpotLight3D();
        rackSpot.Position = new Vector3(2f, 3.7f, -1.5f);
        rackSpot.Rotation = new Vector3(-1.05f, 0, 0);
        rackSpot.LightEnergy = 2.8f;
        rackSpot.LightColor = new Color(1f, 0.96f, 0.92f);
        rackSpot.SpotRange = 8f;
        rackSpot.SpotAngle = 42f;
        rackSpot.SpotAngleAttenuation = 0.7f;
        _garageRoot.AddChild(rackSpot);

        // Poster wall wash.
        var posterSpot = new SpotLight3D();
        posterSpot.Position = new Vector3(-4.4f, 4.2f, 0.3f);
        posterSpot.Rotation = new Vector3(-1.15f, -Mathf.Pi / 2f, 0);
        posterSpot.LightEnergy = 2.2f;
        posterSpot.LightColor = new Color(1f, 0.95f, 0.85f);
        posterSpot.SpotRange = 9f;
        posterSpot.SpotAngle = 50f;
        posterSpot.SpotAngleAttenuation = 0.8f;
        _garageRoot.AddChild(posterSpot);
    }

    // ═══════════════════════════════════════════
    //  BOARD RACK
    // ═══════════════════════════════════════════

    private void BuildBoardRack()
    {
        var darkWood = new Color(0.32f, 0.22f, 0.11f);
        var steel = new Color(0.5f, 0.5f, 0.54f);

        // Backing panel and rails
        Box(new Vector3(3.4f, 2.4f, 0.04f), darkWood, new Vector3(2f, 1.5f, -2.7f));
        Box(new Vector3(3.5f, 0.08f, 0.16f), steel, new Vector3(2f, 2.45f, -2.62f));
        Box(new Vector3(3.5f, 0.08f, 0.16f), steel, new Vector3(2f, 0.40f, -2.62f));
        Box(new Vector3(0.08f, 2.2f, 0.16f), steel, new Vector3(0.3f, 1.45f, -2.62f));
        Box(new Vector3(0.08f, 2.2f, 0.16f), steel, new Vector3(3.7f, 1.45f, -2.62f));

        // "BOARDS" placard above the rack
        var placard = new Label3D();
        placard.Text = "BOARDS";
        placard.FontSize = 10;
        placard.OutlineSize = 0;
        placard.Modulate = new Color(1f, 0.85f, 0.35f);
        placard.Position = new Vector3(2f, 2.75f, -2.66f);
        _garageRoot.AddChild(placard);

        for (int i = 0; i < 4; i++)
        {
            // Peg the board rests on
            Box(new Vector3(0.24f, 0.05f, 0.14f), steel, new Vector3(RackX[i], 0.46f, -2.56f));

            var slot = new Node3D();
            slot.Position = new Vector3(RackX[i], RackY, RackZ);
            _garageRoot.AddChild(slot);
            _rackSlots[i] = slot;

            // Stand the board on its tail, nose up. A -90 deg pitch turns the deck's
            // underside toward the room, which is how a shop racks them — face-out shows
            // the deck colour instead of a slab of black grip tape.
            var board = BoardBuilder.Build((Main.BoardType)i, _main.BoardLook);
            board.Rotation = new Vector3(-Mathf.Pi / 2f, 0, 0);
            slot.AddChild(board);
            _rackBoards[i] = board;
        }
    }

    /// <summary>Pull the selected board off the wall and dim the rest.</summary>
    public void UpdateRackHighlight()
    {
        for (int i = 0; i < 4; i++)
        {
            if (_rackSlots[i] == null) continue;
            bool selected = (int)_main.Board == i;

            _rackSlots[i].Position = new Vector3(RackX[i], RackY, selected ? RackSelectedZ : RackZ);
            _rackBoards[i].Rotation = new Vector3(
                -Mathf.Pi / 2f + (selected ? RackTilt : 0f), 0, 0);
            MeshKit.Tint(_rackBoards[i], selected ? 1f : DimFactor);
        }
    }

    /// <summary>Subtle bob on the selected board so the rack doesn't read as a still life.</summary>
    public void UpdateRack(float dt)
    {
        int sel = (int)_main.Board;
        if (_rackSlots[sel] == null) return;

        float t = (float)Time.GetTicksMsec() * 0.001f;
        float bob = Mathf.Sin(t * 1.6f) * 0.03f;
        _rackSlots[sel].Position = new Vector3(RackX[sel], RackY + bob, RackSelectedZ);
        _rackBoards[sel].Rotation = new Vector3(
            -Mathf.Pi / 2f + RackTilt, Mathf.Sin(t * 0.9f) * 0.10f, 0);
    }

    // ═══════════════════════════════════════════
    //  CARL'S PODIUM
    // ═══════════════════════════════════════════

    private void BuildPodium()
    {
        var podiumMat = MeshKit.Mat(new Color(0.26f, 0.26f, 0.3f));
        MeshKit.Cylinder(_garageRoot, 0.85f, 0.85f, PodiumTop, podiumMat,
            new Vector3(PodiumX, PodiumTop / 2f, PodiumZ), segments: 12);

        // Glowing rim just under the lip, pulsed when the outfit changes.
        _podiumRingMat = MeshKit.EmissiveMat(
            new Color(1f, 0.18f, 0.61f), new Color(1f, 0.18f, 0.61f), 1.2f);
        _podiumRing = MeshKit.Cylinder(_garageRoot, 0.9f, 0.9f, 0.045f, _podiumRingMat,
            new Vector3(PodiumX, 0.035f, PodiumZ), segments: 12);

        _charDisplay = new Node3D();
        _charDisplay.Position = new Vector3(PodiumX, PodiumTop, PodiumZ);
        _garageRoot.AddChild(_charDisplay);
    }

    /// <summary>Rebuild Carl in his current outfit and the board lying beside him.</summary>
    public void UpdateDisplayModel()
    {
        if (_charDisplay == null) return;

        if (_carl != null)
        {
            _charDisplay.RemoveChild(_carl);
            _carl.QueueFree();
        }

        _carl = CarlBuilder.Build(_main.Carl, _main.CarlLook, out var joints);
        CarlBuilder.PoseStanding(joints, _main.CarlLook);
        // The rig faces +X in its own space; this turns him three-quarters toward the
        // char-select camera, which sits front-right of the podium.
        _carl.Rotation = new Vector3(0, 2.34f, 0);
        _charDisplay.AddChild(_carl);

        _styleFlash = 1f;

        UpdatePickedBoard();
    }

    /// <summary>The currently picked board, lying on the floor next to the podium.</summary>
    public void UpdatePickedBoard()
    {
        if (_floorBoard != null)
        {
            _garageRoot.RemoveChild(_floorBoard);
            _floorBoard.QueueFree();
        }

        _floorBoard = BoardBuilder.Build(_main.Board, _main.BoardLook);
        _floorBoard.Position = new Vector3(-2.6f, 0.14f, 1.5f);
        _floorBoard.Rotation = new Vector3(0, 0.45f, 0);
        _garageRoot.AddChild(_floorBoard);
    }

    // ═══════════════════════════════════════════
    //  LEVEL POSTERS
    // ═══════════════════════════════════════════

    private void BuildLevelPosters()
    {
        for (int i = 0; i < 4; i++)
        {
            var poster = new Node3D();
            poster.Position = new Vector3(PosterX, PosterY, PosterZ[i]);
            _garageRoot.AddChild(poster);
            _posters[i] = poster;

            bool unlocked = Main.LevelUnlocked[i];

            // Backing frame, then the paper itself
            MeshKit.Box(poster, new Vector3(0.03f, 1.82f, 1.32f),
                MeshKit.Mat(new Color(0.14f, 0.13f, 0.12f)), new Vector3(-0.02f, 0, 0));
            MeshKit.Box(poster, new Vector3(0.05f, 1.7f, 1.2f),
                MeshKit.Mat(unlocked ? new Color(0.93f, 0.9f, 0.83f) : new Color(0.22f, 0.22f, 0.24f)),
                Vector3.Zero);

            if (i == 0) PaintFrogwood(poster);
            else if (i == 1) PaintBlockIsland(poster);
            else PaintUnknown(poster);

            // Header strip
            MeshKit.Box(poster, new Vector3(0.01f, 0.2f, 1.14f),
                MeshKit.Mat(HeaderColor(i)), new Vector3(0.028f, 0.72f, 0));

            // Text lies flat on the wall (no billboarding) and reads from inside the room.
            _posterTitles[i] = PosterLabel(poster, Main.LevelNames[i], 9, new Vector3(0.04f, 0.72f, 0));
            _posterSubs[i] = PosterLabel(poster,
                unlocked ? Main.LevelSeasons[i].ToUpper() : "LOCKED", 6, new Vector3(0.04f, -0.72f, 0));
        }
    }

    private Label3D PosterLabel(Node3D parent, string text, int size, Vector3 pos)
    {
        var label = new Label3D();
        label.Text = text;
        label.FontSize = size;
        label.Billboard = BaseMaterial3D.BillboardModeEnum.Disabled;
        label.RotationDegrees = new Vector3(0, 90, 0);
        label.Position = pos;
        label.Modulate = new Color(0.1f, 0.1f, 0.1f);
        label.OutlineSize = 0;
        parent.AddChild(label);
        return label;
    }

    private static Color HeaderColor(int level)
    {
        switch (level)
        {
            case 0: return new Color(0.18f, 0.52f, 0.18f);   // Frogwood green
            case 1: return new Color(0.22f, 0.38f, 0.62f);   // Block Island blue
            default: return new Color(0.3f, 0.3f, 0.32f);
        }
    }

    // Poster art is painted in the poster's local YZ plane, a hair proud of the paper.
    private const float ArtX = 0.028f;

    private void PaintFrogwood(Node3D poster)
    {
        var skyMat = MeshKit.Mat(new Color(0.55f, 0.76f, 0.92f));
        var farMat = MeshKit.Mat(new Color(0.28f, 0.5f, 0.28f));
        var nearMat = MeshKit.Mat(new Color(0.15f, 0.33f, 0.16f));
        var roadMat = MeshKit.Mat(new Color(0.30f, 0.29f, 0.31f));
        var pineMat = MeshKit.Mat(new Color(0.06f, 0.19f, 0.09f));

        MeshKit.Box(poster, new Vector3(0.005f, 0.28f, 1.14f), skyMat, new Vector3(ArtX, 0.43f, 0));
        MeshKit.Box(poster, new Vector3(0.006f, 0.26f, 1.14f), farMat, new Vector3(ArtX, 0.16f, 0));
        MeshKit.Box(poster, new Vector3(0.007f, 0.5f, 1.14f), nearMat, new Vector3(ArtX, -0.28f, 0));

        // Road: five stacked segments tapering to a vanishing point and easing left, so it
        // reads as tarmac running away over the hill rather than a grey post.
        for (int i = 0; i < 5; i++)
        {
            float k = i / 4f;
            MeshKit.Box(poster,
                new Vector3(0.009f + i * 0.0004f, 0.13f, Mathf.Lerp(0.34f, 0.05f, k)),
                roadMat,
                new Vector3(ArtX, Mathf.Lerp(-0.5f, 0.0f, k), Mathf.Lerp(0.13f, -0.06f, k)));
        }

        // Pines, staggered along the treeline at the base of the far ridge.
        foreach (var p in new[] {
            new Vector2(-0.02f, 0.50f), new Vector2(0.03f, 0.38f), new Vector2(-0.04f, -0.34f),
            new Vector2(0.02f, -0.46f), new Vector2(-0.01f, -0.22f) })
        {
            MeshKit.Box(poster, new Vector3(0.012f, 0.22f, 0.07f), pineMat,
                new Vector3(ArtX, 0.06f + p.X, p.Y));
        }
    }

    private void PaintBlockIsland(Node3D poster)
    {
        MeshKit.Box(poster, new Vector3(0.005f, 0.36f, 1.14f),
            MeshKit.Mat(new Color(0.62f, 0.78f, 0.9f)), new Vector3(ArtX, 0.39f, 0));       // sky
        MeshKit.Box(poster, new Vector3(0.006f, 0.34f, 1.14f),
            MeshKit.Mat(new Color(0.16f, 0.36f, 0.58f)), new Vector3(ArtX, 0.04f, 0));      // ocean
        MeshKit.Box(poster, new Vector3(0.007f, 0.38f, 1.14f),
            MeshKit.Mat(new Color(0.62f, 0.55f, 0.36f)), new Vector3(ArtX, -0.34f, 0));     // bluff

        // Lighthouse, standing on the bluff rather than hovering over the water —
        // the bluff's top edge is at y=-0.15, so the tower base has to start there.
        MeshKit.Box(poster, new Vector3(0.009f, 0.34f, 0.09f),
            MeshKit.Mat(new Color(0.92f, 0.9f, 0.86f)), new Vector3(ArtX, 0.02f, -0.36f));
        MeshKit.Box(poster, new Vector3(0.01f, 0.07f, 0.11f),
            MeshKit.Mat(new Color(0.78f, 0.2f, 0.18f)), new Vector3(ArtX, 0.22f, -0.36f));

        // Whitecaps
        foreach (float z in new[] { 0.34f, 0.05f, -0.12f })
            MeshKit.Box(poster, new Vector3(0.009f, 0.03f, 0.2f),
                MeshKit.Mat(new Color(0.85f, 0.9f, 0.95f)), new Vector3(ArtX, -0.02f, z));
    }

    private void PaintUnknown(Node3D poster)
    {
        MeshKit.Box(poster, new Vector3(0.006f, 1.1f, 1.06f),
            MeshKit.Mat(new Color(0.16f, 0.16f, 0.18f)), new Vector3(ArtX, -0.06f, 0));

        // An actual glyph — a bar and a dot built from boxes just read as "!".
        var q = new Label3D();
        q.Text = "?";
        q.FontSize = 64;
        q.Billboard = BaseMaterial3D.BillboardModeEnum.Disabled;
        q.RotationDegrees = new Vector3(0, 90, 0);
        q.Position = new Vector3(0.04f, -0.06f, 0);
        q.Modulate = new Color(0.42f, 0.42f, 0.46f);
        q.OutlineSize = 0;
        q.PixelSize = 0.006f;
        poster.AddChild(q);
    }

    public void UpdatePosterHighlight()
    {
        for (int i = 0; i < 4; i++)
        {
            if (_posters[i] == null) continue;
            bool selected = (int)_main.Level == i;

            _posters[i].Position = new Vector3(PosterX + (selected ? 0.05f : 0f), PosterY, PosterZ[i]);
            MeshKit.Tint(_posters[i], selected ? 1f : DimFactor);

            var textColor = selected ? new Color(0.08f, 0.08f, 0.08f) : new Color(0.42f, 0.42f, 0.42f);
            if (_posterTitles[i] != null) _posterTitles[i].Modulate = textColor;
            if (_posterSubs[i] != null)
            {
                _posterSubs[i].Modulate = Main.LevelUnlocked[i]
                    ? textColor
                    : (selected ? new Color(0.8f, 0.25f, 0.25f) : new Color(0.45f, 0.25f, 0.25f));
            }
        }
    }

    // ═══════════════════════════════════════════
    //  VISIBILITY
    // ═══════════════════════════════════════════

    public void Show()
    {
        _garageRoot.Visible = true;
        _main.SetRideWorldVisible(false);
        // Park the Player at the origin — CameraMount hangs off it, so the world-absolute
        // camera positions below only line up while it sits there.
        _main.Player.Position = Vector3.Zero;
        _main.GetNode<DirectionalLight3D>("Sun").LightEnergy = 0.1f;

        // The world environment's bright blue sky ambient is tuned for riding outdoors;
        // left on, it floods the garage and washes every surface the same cold grey.
        // Indoors the fixtures should be doing the work.
        var env = GarageEnv();
        env.AmbientLightEnergy = 0.16f;
        env.AmbientLightColor = new Color(0.42f, 0.38f, 0.36f);
        env.FogEnabled = false;
    }

    /// <summary>
    /// Put the garage away. Only the garage — restoring the ride world is Main's job, because
    /// leaving the garage no longer always means going riding.
    /// </summary>
    public void Hide()
    {
        _garageRoot.Visible = false;
    }

    private Godot.Environment GarageEnv()
    {
        return _main.GetNode<WorldEnvironment>("WorldEnvironment").Environment;
    }

    // ═══════════════════════════════════════════
    //  CAMERA
    // ═══════════════════════════════════════════

    public void ResetCamera()
    {
        _camInitialized = false;
    }

    public void UpdateCamera(float dt, Shot shot)
    {
        Vector3 targetPos;
        Vector3 targetLook;
        float targetFov;

        switch (shot)
        {
            case Shot.Board:
                // Pulled back far enough that the selected board, which sits ~0.3m nearer
                // the lens than its neighbours, still fits inside the frame.
                targetPos = new Vector3(2f, 1.5f, 1.5f);
                targetLook = new Vector3(2f, 1.45f, RackZ);
                targetFov = 50f;
                break;

            case Shot.Level:
                // Dolly along the wall so every poster is read head-on.
                float z = PosterZ[(int)_main.Level];
                targetPos = new Vector3(PosterCamX, PosterY, z + 0.6f);
                targetLook = new Vector3(WallLeftX, PosterY, z);
                targetFov = 45f;
                break;

            default:
                // Char — framed so his soles clear the info block and there's headroom above.
                targetPos = new Vector3(1.26f, 1.85f, 3.79f);
                targetLook = new Vector3(PodiumX, 1.25f, PodiumZ);
                targetFov = 45f;
                break;
        }

        var cam = _main.CameraMount.GetNode<Camera3D>("Camera3D");

        if (!_camInitialized)
        {
            _camPos = targetPos;
            _camLookAt = targetLook;
            _camInitialized = true;
            cam.Fov = targetFov;
        }

        _camPos = _camPos.Lerp(targetPos, CamLerpSpeed * dt);
        _camLookAt = _camLookAt.Lerp(targetLook, CamLerpSpeed * dt);

        _main.CameraMount.Position = _camPos;
        cam.LookAt(_camLookAt, Vector3.Up);
        cam.Fov = Mathf.Lerp(cam.Fov, targetFov, 3f * dt);

        // Podium rim flashes when the outfit changes.
        if (_styleFlash > 0f)
        {
            _styleFlash = Mathf.Max(0f, _styleFlash - dt * 2.2f);
            if (_podiumRingMat != null)
                _podiumRingMat.EmissionEnergyMultiplier = 1.2f + _styleFlash * 4f;
        }
    }

    // ═══════════════════════════════════════════
    //  MESH HELPER
    // ═══════════════════════════════════════════

    private MeshInstance3D Box(Vector3 size, Color color, Vector3 pos)
    {
        return MeshKit.Box(_garageRoot, size, color, pos);
    }
}
