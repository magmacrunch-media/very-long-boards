using Godot;

/// <summary>
/// The title screen: Carl's garage seen from the driveway on a summer afternoon. A world of its
/// own rather than a camera angle on the garage interior, so the title has its own light, its own
/// weather and its own framing, and pressing ↑ reads as walking in.
///
/// Same shape as <see cref="GarageManager"/> — a code-built root that Show()/Hide() toggles, with
/// its own lighting. Neither manager touches the ride world; Main owns that.
///
/// Layout, looking down the driveway toward the building:
///   Building front at z = -6, ridge running left-right
///   Driveway from the door out toward the camera at +z
///   Treeline behind and to both sides
/// </summary>
public class TitleManager
{
    private Main _main;
    private Node3D _titleRoot;
    private Node3D _leaningBoard;

    // ── Layout anchors ──
    private const float FrontZ = -6f;      // building face
    private const float DoorW = 3.4f;
    private const float DoorH = 2.5f;
    private const float BodyW = 7f;
    private const float BodyH = 3.2f;
    private const float BodyD = 5f;

    // ── Camera ──
    private Vector3 _camPos;
    private Vector3 _camLookAt;
    private bool _camInitialized = false;
    private const float CamLerpSpeed = 3f;

    public TitleManager(Main main)
    {
        _main = main;
    }

    public void Create()
    {
        _titleRoot = new Node3D();
        _titleRoot.Visible = false;
        _main.AddChild(_titleRoot);

        BuildGround();
        BuildBuilding();
        BuildDoor();
        BuildSignAndWindow();
        BuildMailbox();
        BuildTreeline();
        BuildLighting();

        UpdateLeaningBoard();
    }

    // ═══════════════════════════════════════════
    //  GROUND
    // ═══════════════════════════════════════════

    private void BuildGround()
    {
        // Lawn out to well past the treeline; fog and the sky take it from there.
        MeshKit.Box(_titleRoot, new Vector3(90f, 0.2f, 90f),
            MeshKit.Mat(Colors.White, texture: TextureKit.Grass, uvScale: 30f),
            new Vector3(0, -0.1f, 6f));

        // Gravel apron running from the door out under the camera.
        MeshKit.Box(_titleRoot, new Vector3(DoorW + 1.2f, 0.06f, 16f),
            MeshKit.Mat(Colors.White, texture: TextureKit.Dirt, uvScale: 8f),
            new Vector3(0, 0.01f, FrontZ + 8.2f));
    }

    // ═══════════════════════════════════════════
    //  THE BUILDING
    // ═══════════════════════════════════════════

    private void BuildBuilding()
    {
        var clapboard = MeshKit.Mat(new Color(1.15f, 1.12f, 1.05f),
            texture: TextureKit.Plank, uvScale: 7f);
        var shingle = MeshKit.Mat(new Color(0.26f, 0.26f, 0.29f));

        // Body, sitting behind the front face so the face lands exactly on FrontZ.
        MeshKit.Box(_titleRoot, new Vector3(BodyW, BodyH, BodyD), clapboard,
            new Vector3(0, BodyH / 2f, FrontZ - BodyD / 2f));

        // Gable: two pitched slabs meeting over the ridge.
        float pitch = 0.52f;                       // ~30 degrees
        float slope = BodyW / 2f / Mathf.Cos(pitch);
        foreach (float side in new[] { -1f, 1f })
        {
            var roof = MeshKit.Box(_titleRoot, new Vector3(slope + 0.3f, 0.14f, BodyD + 0.6f),
                shingle, new Vector3(side * BodyW / 4f, BodyH + 0.5f, FrontZ - BodyD / 2f));
            roof.Rotation = new Vector3(0, 0, -side * pitch);
        }

        // Gable end filling the triangle under the ridge, and the fascia across the front.
        MeshKit.Box(_titleRoot, new Vector3(BodyW * 0.5f, 0.9f, 0.12f), clapboard,
            new Vector3(0, BodyH + 0.42f, FrontZ + 0.02f));
        MeshKit.Box(_titleRoot, new Vector3(BodyW + 0.4f, 0.16f, 0.16f),
            MeshKit.Mat(new Color(0.86f, 0.86f, 0.82f)),
            new Vector3(0, BodyH + 0.06f, FrontZ + 0.06f));
    }

    private void BuildDoor()
    {
        // Roll-up door, closed. Grooves every 0.5 m are what sell it as a garage door rather
        // than a flat panel — the silhouette alone reads as a wall.
        var doorMat = MeshKit.Mat(new Color(0.74f, 0.74f, 0.70f));
        var grooveMat = MeshKit.Mat(new Color(0.55f, 0.55f, 0.52f));
        var trimMat = MeshKit.Mat(new Color(0.88f, 0.88f, 0.84f));

        MeshKit.Box(_titleRoot, new Vector3(DoorW, DoorH, 0.1f), doorMat,
            new Vector3(0, DoorH / 2f, FrontZ + 0.06f));

        for (int i = 1; i < 5; i++)
        {
            MeshKit.Box(_titleRoot, new Vector3(DoorW - 0.06f, 0.03f, 0.02f), grooveMat,
                new Vector3(0, i * DoorH / 5f, FrontZ + 0.12f));
        }

        // Trim down both jambs and across the head.
        foreach (float side in new[] { -1f, 1f })
        {
            MeshKit.Box(_titleRoot, new Vector3(0.16f, DoorH + 0.2f, 0.12f), trimMat,
                new Vector3(side * (DoorW / 2f + 0.08f), (DoorH + 0.2f) / 2f, FrontZ + 0.07f));
        }
        MeshKit.Box(_titleRoot, new Vector3(DoorW + 0.32f, 0.16f, 0.12f), trimMat,
            new Vector3(0, DoorH + 0.18f, FrontZ + 0.07f));

        // The board you last picked, leaning by the door.
        _leaningBoard = new Node3D();
        _titleRoot.AddChild(_leaningBoard);
    }

    /// <summary>Rebuild the leaning board so the title shows the deck currently selected.</summary>
    public void UpdateLeaningBoard()
    {
        if (_leaningBoard == null) return;
        foreach (var child in _leaningBoard.GetChildren())
        {
            _leaningBoard.RemoveChild(child);
            child.QueueFree();
        }

        var board = BoardBuilder.Build(_main.Board);
        // Tail on the gravel, nose tipped back against the clapboard.
        board.Position = new Vector3(DoorW / 2f + 0.75f, 0.62f, FrontZ + 0.34f);
        board.Rotation = new Vector3(Mathf.Pi / 2f - 0.22f, 0.16f, 0f);
        _leaningBoard.AddChild(board);
    }

    private void BuildSignAndWindow()
    {
        // VLB sign over the door, same emissive treatment as the one inside.
        var sign = new Label3D();
        sign.Text = "VLB";
        sign.FontSize = 22;
        sign.OutlineSize = 0;
        sign.MaterialOverride = MeshKit.EmissiveMat(
            new Color(1f, 0.18f, 0.61f), new Color(1f, 0.18f, 0.61f), 2.2f);
        sign.Position = new Vector3(0, BodyH - 0.42f, FrontZ + 0.14f);
        _titleRoot.AddChild(sign);

        var tagline = new Label3D();
        tagline.Text = "DOWNHILL SKATEBOARDS";
        tagline.FontSize = 7;
        tagline.OutlineSize = 0;
        tagline.MaterialOverride = MeshKit.EmissiveMat(
            new Color(1f, 0.55f, 0.78f), new Color(0.4f, 0.08f, 0.22f));
        tagline.Position = new Vector3(0, BodyH - 0.72f, FrontZ + 0.14f);
        _titleRoot.AddChild(tagline);

        // Lit side window — the hint that the garage is where you're headed.
        var frame = MeshKit.Mat(new Color(0.82f, 0.82f, 0.78f));
        MeshKit.Box(_titleRoot, new Vector3(0.9f, 0.75f, 0.1f), frame,
            new Vector3(-BodyW / 2f + 1.3f, 1.75f, FrontZ + 0.05f));
        MeshKit.Box(_titleRoot, new Vector3(0.76f, 0.6f, 0.06f),
            MeshKit.EmissiveMat(new Color(1f, 0.88f, 0.62f), new Color(1f, 0.78f, 0.42f), 1.6f),
            new Vector3(-BodyW / 2f + 1.3f, 1.75f, FrontZ + 0.11f));
        MeshKit.Box(_titleRoot, new Vector3(0.04f, 0.6f, 0.02f), frame,
            new Vector3(-BodyW / 2f + 1.3f, 1.75f, FrontZ + 0.15f));
    }

    private void BuildMailbox()
    {
        var postMat = MeshKit.Mat(Colors.White, texture: TextureKit.Bark, uvScale: 2f);
        var boxMat = MeshKit.Mat(new Color(0.30f, 0.32f, 0.36f));

        var post = MeshKit.Cylinder(_titleRoot, 0.05f, 1.1f, postMat,
            new Vector3(3.4f, 0.55f, FrontZ + 12f), segments: 8);
        MeshKit.Box(_titleRoot, new Vector3(0.22f, 0.2f, 0.42f), boxMat,
            new Vector3(3.4f, 1.2f, FrontZ + 12f));
        MeshKit.Box(_titleRoot, new Vector3(0.04f, 0.16f, 0.03f),
            MeshKit.Mat(new Color(0.85f, 0.2f, 0.15f)),
            new Vector3(3.28f, 1.32f, FrontZ + 12f));
    }

    // ═══════════════════════════════════════════
    //  TREELINE
    // ═══════════════════════════════════════════

    private void BuildTreeline()
    {
        var rng = new RandomNumberGenerator();
        rng.Seed = 1080;

        // Ring the clearing, keeping the driveway mouth and the building face clear.
        for (int i = 0; i < 26; i++)
        {
            float angle = rng.RandfRange(0f, Mathf.Tau);
            float dist = rng.RandfRange(11f, 26f);
            float x = Mathf.Sin(angle) * dist;
            float z = FrontZ + 2f + Mathf.Cos(angle) * dist;
            if (Mathf.Abs(x) < 3.6f && z > FrontZ) continue;   // don't block the shot

            bool pine = rng.Randf() > 0.32f;
            AddTree(new Vector3(x, 0f, z), 5.5f + rng.RandfRange(0f, 5f), pine, rng);
        }
    }

    /// <summary>
    /// Three crossed alpha-cut quads, the same construction SceneryManager uses. Kept local
    /// because that one is welded to the scrolling Items list, which a static diorama doesn't want.
    /// </summary>
    private void AddTree(Vector3 pos, float h, bool pine, RandomNumberGenerator rng)
    {
        var tree = new Node3D();
        tree.Position = pos;
        _titleRoot.AddChild(tree);

        var trunkMat = MeshKit.Mat(new Color(1f, 0.95f + rng.RandfRange(0, 0.1f), 0.95f),
            texture: TextureKit.Bark, uvScale: 1.5f);
        var trunk = MeshKit.Cylinder(tree, pine ? 0.05f : 0.07f, h * 0.45f, trunkMat,
            new Vector3(0, h * 0.22f, 0), segments: 8);

        var tint = new Color(0.88f + rng.RandfRange(0, 0.24f),
                             0.90f + rng.RandfRange(0, 0.20f),
                             0.85f + rng.RandfRange(0, 0.20f));
        var folMat = pine
            ? MeshKit.CutoutMat(TextureKit.PineBillboard, tint)
            : MeshKit.CutoutMat(TextureKit.LeafBillboard, tint);

        float fw = pine ? h * 0.52f : h * 0.66f;
        float fh = pine ? h * 0.85f : h * 0.55f;
        float fy = pine ? h * 0.52f : h * 0.68f;
        float yaw = rng.RandfRange(0f, Mathf.Pi);

        for (int j = 0; j < 3; j++)
        {
            MeshKit.Quad(tree, new Vector2(fw, fh), folMat,
                new Vector3(0, fy, 0), new Vector3(0, yaw + j * Mathf.Pi / 3f, 0));
        }
    }

    private void BuildLighting()
    {
        // Warm late-afternoon key raking across the front of the building, so the clapboard
        // and the door grooves throw some relief instead of reading flat.
        var key = new DirectionalLight3D();
        key.Position = new Vector3(6f, 8f, FrontZ + 10f);
        key.Rotation = new Vector3(-0.62f, -0.85f, 0f);
        key.LightEnergy = 1.1f;
        key.LightColor = new Color(1f, 0.94f, 0.82f);
        _titleRoot.AddChild(key);
    }

    // ═══════════════════════════════════════════
    //  VISIBILITY
    // ═══════════════════════════════════════════

    public void Show()
    {
        _titleRoot.Visible = true;
        _main.SetRideWorldVisible(false);
        // CameraMount hangs off Player, so the world-absolute camera positions below only line
        // up while Player sits at the origin.
        _main.Player.Position = Vector3.Zero;
        UpdateLeaningBoard();

        _main.GetNode<DirectionalLight3D>("Sun").LightEnergy = 1.25f;
        var env = _main.GetNode<WorldEnvironment>("WorldEnvironment").Environment;
        env.AmbientLightEnergy = 0.75f;
        env.AmbientLightColor = new Color(0.62f, 0.72f, 0.86f);
        // Thin haze only — enough to soften the treeline without hiding the building.
        env.FogEnabled = true;
    }

    public void Hide()
    {
        _titleRoot.Visible = false;
    }

    // ═══════════════════════════════════════════
    //  CAMERA
    // ═══════════════════════════════════════════

    public void ResetCamera()
    {
        _camInitialized = false;
    }

    /// <summary>
    /// A slow drift across the driveway. The building sits in the middle band of the frame —
    /// the title type takes the top of the screen and the prompt the bottom.
    /// </summary>
    public void UpdateCamera(float dt)
    {
        float t = _main.TitleTime;

        var targetPos = new Vector3(
            2.2f + Mathf.Sin(t * 0.18f) * 1.5f,
            1.9f + Mathf.Sin(t * 0.13f) * 0.22f,
            FrontZ + 9.5f + Mathf.Sin(t * 0.09f) * 0.8f);
        var targetLook = new Vector3(0f, 1.5f, FrontZ);
        const float targetFov = 50f;

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
        cam.Rotation = new Vector3(cam.Rotation.X, cam.Rotation.Y, 0f);
        cam.Fov = Mathf.Lerp(cam.Fov, targetFov, 3f * dt);
    }
}
