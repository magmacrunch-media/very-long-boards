using Godot;
using System.Collections.Generic;

public partial class Main : Node3D
{
    private enum GameState { Title, Riding, Finished }
    private enum CarlType { Office, Party, Dark }
    private GameState _state = GameState.Title;
    private CarlType _carl = CarlType.Office;
    private static readonly string[] CarlNames = { "Office Carl", "Party Carl", "Dark Carl" };
    private static readonly Color[] CarlShirtColors = {
        new Color(0.55f, 0.18f, 0.18f),  // Office: dress shirt red
        new Color(0.2f, 0.15f, 0.6f),    // Party: loud purple
        new Color(0.1f, 0.1f, 0.12f)     // Dark: black
    };
    private static readonly Color[] CarlPantsColors = {
        new Color(0.2f, 0.24f, 0.32f),   // Office: slacks
        new Color(0.9f, 0.4f, 0.1f),     // Party: orange
        new Color(0.08f, 0.08f, 0.1f)    // Dark: black
    };
    private float _timer = 0f;
    private float _bestTime = 0f;
    private float _finishTime = 0f;

    // Terrain meshes
    private MeshInstance3D _roadMesh;
    private MeshInstance3D _lineCenterMesh;
    private MeshInstance3D _lineEdgeLMesh;
    private MeshInstance3D _lineEdgeRMesh;
    private MeshInstance3D _groundMesh;
    private float _scrollOffset = 0f;
    private const float RoadW = 8f;
    private const float GroundW = 300f;
    private const int Segs = 350;
    private const int Back = 60;
    private const float SegLen = 3f;

    // Player
    private CharacterBody3D _player;
    private Node3D _cameraMount;
    private Node3D _skaterRoot;
    private MeshInstance3D _shirtMesh;
    private MeshInstance3D _pantsMesh;
    private float _playerSpeed = 0f;
    private float _playerX = 0f;
    private float _playerDistance = 0f;
    private bool _kicked = false;
    private float _lean = 0f;

    private const float Gravity = 0.06f;
    private const float Friction = 0.998f;
    private const float MaxSpeed = 5f;
    private const float Handling = 0.18f;
    private const float CourseLength = 2000f;

    // Scenery — stored as data so we can reposition each frame
    private struct SceneryItem { public Node3D Node; public float WorldZ; public float OffsetX; }
    private List<SceneryItem> _scenery = new List<SceneryItem>();

    // UI
    private Label _speedLabel;
    private Label _distLabel;
    private Label _timerLabel;
    private Label _promptLabel;
    private Label _titleLabel;

    public override void _Ready()
    {
        _player = GetNode<CharacterBody3D>("Player");
        _cameraMount = GetNode<Node3D>("Player/CameraMount");

        CreateTerrain();
        CreatePlayerMesh();
        CreateScenery();
        CreateUI();
        UpdateCamera();
    }

    private float CurveAt(float z)
    {
        return Mathf.Sin(z * 0.002f) * 0.15f
             + Mathf.Sin(z * 0.0008f) * 0.22f
             + Mathf.Sin(z * 0.005f) * 0.06f;
    }

    private float HillAt(float z)
    {
        return -z * 0.08f
             + Mathf.Sin(z * 0.003f) * 5f
             + Mathf.Sin(z * 0.008f) * 2f;
    }

    // ── Terrain ──────────────────────────────────

    private void CreateTerrain()
    {
        _roadMesh = MakeMesh(new Color(0.35f, 0.35f, 0.38f));
        _lineCenterMesh = MakeMesh(new Color(0.85f, 0.85f, 0.72f));
        _lineEdgeLMesh = MakeMesh(new Color(0.8f, 0.8f, 0.68f));
        _lineEdgeRMesh = MakeMesh(new Color(0.8f, 0.8f, 0.68f));
        _groundMesh = MakeMesh(new Color(0.24f, 0.5f, 0.18f));
        AddChild(_roadMesh);
        AddChild(_lineCenterMesh);
        AddChild(_lineEdgeLMesh);
        AddChild(_lineEdgeRMesh);
        AddChild(_groundMesh);
        UpdateTerrain();
    }

    private MeshInstance3D MakeMesh(Color color)
    {
        var m = new MeshInstance3D();
        var mat = new StandardMaterial3D();
        mat.AlbedoColor = color;
        m.MaterialOverride = mat;
        return m;
    }

    private void UpdateTerrain()
    {
        _roadMesh.Mesh = BuildRibbon(RoadW, 0f);
        _lineCenterMesh.Mesh = BuildRibbon(0.12f, 0.015f);
        _lineEdgeLMesh.Mesh = BuildRibbon(0.1f, 0.015f, -RoadW / 2f + 0.3f);
        _lineEdgeRMesh.Mesh = BuildRibbon(0.1f, 0.015f, RoadW / 2f - 0.3f);
        _groundMesh.Mesh = BuildRibbon(GroundW, -0.4f);
    }

    private Mesh BuildRibbon(float width, float yOffset, float xOffset = 0f)
    {
        var st = new SurfaceTool();
        st.Begin(Mesh.PrimitiveType.Triangles);
        for (int i = 0; i < Segs - 1; i++)
        {
            float lz0 = (i - Back) * SegLen;
            float lz1 = (i + 1 - Back) * SegLen;
            float wz0 = lz0 + _scrollOffset;
            float wz1 = lz1 + _scrollOffset;
            float cx0 = CurveAt(wz0) * lz0 + xOffset;
            float cy0 = HillAt(wz0) + yOffset;
            float cx1 = CurveAt(wz1) * lz1 + xOffset;
            float cy1 = HillAt(wz1) + yOffset;
            float hw = width / 2f;

            st.AddVertex(new Vector3(cx0 - hw, cy0, lz0));
            st.AddVertex(new Vector3(cx0 + hw, cy0, lz0));
            st.AddVertex(new Vector3(cx1 - hw, cy1, lz1));
            st.AddVertex(new Vector3(cx0 + hw, cy0, lz0));
            st.AddVertex(new Vector3(cx1 + hw, cy1, lz1));
            st.AddVertex(new Vector3(cx1 - hw, cy1, lz1));
        }
        st.GenerateNormals();
        return st.Commit();
    }

    // ── Player ───────────────────────────────────

    private void CreatePlayerMesh()
    {
        _skaterRoot = new Node3D();
        _player.AddChild(_skaterRoot);

        // ── Board: long in Z (direction of motion), narrow in X ──
        // Deck
        var deck = MakeBox(new Vector3(0.7f, 0.05f, 1.8f), new Color(0.42f, 0.2f, 0.05f));
        deck.Position = new Vector3(0, 0.13f, 0);
        _skaterRoot.AddChild(deck);

        // Grip tape
        var grip = MakeBox(new Vector3(0.65f, 0.02f, 1.6f), new Color(0.15f, 0.15f, 0.15f));
        grip.Position = new Vector3(0, 0.165f, 0);
        _skaterRoot.AddChild(grip);

        // Nose/tail kick (angled tips)
        var noseKick = MakeBox(new Vector3(0.55f, 0.04f, 0.2f), new Color(0.42f, 0.2f, 0.05f));
        noseKick.Position = new Vector3(0, 0.18f, 0.9f);
        noseKick.Rotation = new Vector3(0.25f, 0, 0);
        _skaterRoot.AddChild(noseKick);

        var tailKick = MakeBox(new Vector3(0.55f, 0.04f, 0.2f), new Color(0.42f, 0.2f, 0.05f));
        tailKick.Position = new Vector3(0, 0.18f, -0.9f);
        tailKick.Rotation = new Vector3(-0.25f, 0, 0);
        _skaterRoot.AddChild(tailKick);

        // Trucks (metal axle assemblies)
        var truckCol = new Color(0.5f, 0.5f, 0.52f);
        var truckF = MakeBox(new Vector3(0.6f, 0.04f, 0.12f), truckCol);
        truckF.Position = new Vector3(0, 0.08f, 0.55f);
        _skaterRoot.AddChild(truckF);
        var truckR = MakeBox(new Vector3(0.6f, 0.04f, 0.12f), truckCol);
        truckR.Position = new Vector3(0, 0.08f, -0.55f);
        _skaterRoot.AddChild(truckR);

        // Wheels (4 wheels, on truck axles)
        var wheelCol = new Color(0.12f, 0.12f, 0.12f);
        foreach (var pos in new[] {
            new Vector3(-0.32f, 0.04f, 0.55f), new Vector3(0.32f, 0.04f, 0.55f),
            new Vector3(-0.32f, 0.04f, -0.55f), new Vector3(0.32f, 0.04f, -0.55f) })
        {
            var wheel = MakeCylinder(0.06f, 0.08f, wheelCol);
            wheel.Position = pos;
            wheel.Rotation = new Vector3(0, 0, Mathf.Pi / 2f);
            _skaterRoot.AddChild(wheel);
        }

        // ── Skater body — facing right (+X), standard footing ──
        // Rotate the body group 90° so skater faces +X
        var bodyGroup = new Node3D();
        bodyGroup.Rotation = new Vector3(0, Mathf.Pi / 2f, 0);
        _skaterRoot.AddChild(bodyGroup);

        // Shoes (on the board)
        var shoeCol = new Color(0.15f, 0.15f, 0.15f);
        // Front foot (left) — toward -Z of board (forward in world)
        var shoeL = MakeBox(new Vector3(0.14f, 0.06f, 0.25f), shoeCol);
        shoeL.Position = new Vector3(-0.1f, 0.19f, -0.3f);
        bodyGroup.AddChild(shoeL);
        // Back foot (right) — toward +Z of board (back in world)
        var shoeR = MakeBox(new Vector3(0.14f, 0.06f, 0.25f), shoeCol);
        shoeR.Position = new Vector3(0.1f, 0.19f, 0.25f);
        bodyGroup.AddChild(shoeR);

        // Legs (jeans)
        _pantsMesh = MakeBox(new Vector3(0.14f, 0.32f, 0.14f), CarlPantsColors[(int)_carl]);
        _pantsMesh.Position = new Vector3(-0.1f, 0.38f, -0.25f);
        _pantsMesh.Rotation = new Vector3(0.1f, 0, 0);
        bodyGroup.AddChild(_pantsMesh);
        var legR = MakeBox(new Vector3(0.14f, 0.32f, 0.14f), CarlPantsColors[(int)_carl]);
        legR.Position = new Vector3(0.1f, 0.38f, 0.2f);
        legR.Rotation = new Vector3(-0.1f, 0, 0);
        bodyGroup.AddChild(legR);

        // Torso (t-shirt)
        _shirtMesh = MakeBox(new Vector3(0.32f, 0.42f, 0.3f), CarlShirtColors[(int)_carl]);
        _shirtMesh.Position = new Vector3(0, 0.72f, -0.05f);
        bodyGroup.AddChild(_shirtMesh);

        // Arms (skin, slightly out for balance)
        var skinCol = new Color(0.88f, 0.78f, 0.6f);
        var armL = MakeBox(new Vector3(0.08f, 0.3f, 0.08f), skinCol);
        armL.Position = new Vector3(-0.22f, 0.78f, -0.05f);
        armL.Rotation = new Vector3(0, 0, 0.2f);
        bodyGroup.AddChild(armL);
        var armR = MakeBox(new Vector3(0.08f, 0.3f, 0.08f), skinCol);
        armR.Position = new Vector3(0.22f, 0.78f, -0.05f);
        armR.Rotation = new Vector3(0, 0, -0.2f);
        bodyGroup.AddChild(armR);

        // Head
        var head = MakeBox(new Vector3(0.24f, 0.24f, 0.24f), skinCol);
        head.Position = new Vector3(0, 1.06f, -0.05f);
        bodyGroup.AddChild(head);

        // Hair
        var hair = MakeBox(new Vector3(0.26f, 0.07f, 0.26f), new Color(0.3f, 0.18f, 0.08f));
        hair.Position = new Vector3(0, 1.2f, -0.05f);
        bodyGroup.AddChild(hair);

        // Collision
        var col = new CollisionShape3D();
        var shape = new BoxShape3D();
        shape.Size = new Vector3(0.7f, 1.3f, 1.8f);
        col.Shape = shape;
        col.Position = new Vector3(0, 0.65f, 0);
        _player.AddChild(col);
    }

    private MeshInstance3D MakeBox(Vector3 size, Color color)
    {
        var m = new MeshInstance3D();
        var mesh = new BoxMesh();
        mesh.Size = size;
        m.Mesh = mesh;
        var mat = new StandardMaterial3D();
        mat.AlbedoColor = color;
        m.MaterialOverride = mat;
        return m;
    }

    private MeshInstance3D MakeCylinder(float topR, float height, Color color)
    {
        var m = new MeshInstance3D();
        var mesh = new CylinderMesh();
        mesh.TopRadius = topR;
        mesh.BottomRadius = topR;
        mesh.Height = height;
        m.Mesh = mesh;
        var mat = new StandardMaterial3D();
        mat.AlbedoColor = color;
        m.MaterialOverride = mat;
        return m;
    }

    // ── Scenery ──────────────────────────────────

    private void CreateScenery()
    {
        var rng = new RandomNumberGenerator();
        rng.Seed = 42;

        var trunkCol = new Color(0.32f, 0.2f, 0.1f);
        var pineCol = new Color(0.12f, 0.28f, 0.1f);
        var pineCol2 = new Color(0.14f, 0.32f, 0.1f);
        var leafCol = new Color(0.28f, 0.52f, 0.18f);

        // Pine trees (close to road, dense)
        for (int i = 0; i < 100; i++)
        {
            float z = rng.RandfRange(-80f, 900f);
            float side = rng.Randf() > 0.5f ? 1f : -1f;
            float offset = side * (5.5f + rng.RandfRange(0f, 12f));
            float h = 5f + rng.RandfRange(0f, 7f);
            AddTree(z, offset, h, true, rng);
        }

        // Pine trees (further back, taller)
        for (int i = 0; i < 50; i++)
        {
            float z = rng.RandfRange(-80f, 900f);
            float side = rng.Randf() > 0.5f ? 1f : -1f;
            float offset = side * (18f + rng.RandfRange(0f, 25f));
            float h = 8f + rng.RandfRange(0f, 10f);
            AddTree(z, offset, h, true, rng);
        }

        // Deciduous trees
        for (int i = 0; i < 40; i++)
        {
            float z = rng.RandfRange(-80f, 900f);
            float side = rng.Randf() > 0.5f ? 1f : -1f;
            float offset = side * (7f + rng.RandfRange(0f, 18f));
            float h = 5f + rng.RandfRange(0f, 5f);
            AddTree(z, offset, h, false, rng);
        }

        // Rocks (roadside)
        for (int i = 0; i < 20; i++)
        {
            float z = rng.RandfRange(-50f, 800f);
            float side = rng.Randf() > 0.5f ? 1f : -1f;
            float offset = side * (4.5f + rng.RandfRange(0f, 4f));
            float size = 0.12f + rng.RandfRange(0f, 0.3f);
            AddSceneryMesh(z, offset, MakeSphere(size, new Color(0.45f, 0.43f, 0.4f)),
                new Vector3(rng.RandfRange(0, 0.3f), rng.RandfRange(0, 3f), 0));
        }

        // Stumps
        for (int i = 0; i < 15; i++)
        {
            float z = rng.RandfRange(-30f, 700f);
            float side = rng.Randf() > 0.5f ? 1f : -1f;
            float offset = side * (5f + rng.RandfRange(0f, 6f));
            float h = 0.15f + rng.RandfRange(0f, 0.2f);
            AddSceneryMesh(z, offset, MakeCylinder(0.15f, h, new Color(0.35f, 0.22f, 0.1f)));
        }

        // Wildflowers (small colored dots near road edge)
        for (int i = 0; i < 60; i++)
        {
            float z = rng.RandfRange(-30f, 800f);
            float side = rng.Randf() > 0.5f ? 1f : -1f;
            float offset = side * (4.2f + rng.RandfRange(0f, 3f));
            var flowerCol = new Color[] {
                new Color(0.9f, 0.85f, 0.2f),  // yellow
                new Color(0.9f, 0.4f, 0.5f),   // pink
                new Color(0.8f, 0.8f, 0.85f),  // white
                new Color(0.6f, 0.4f, 0.8f)    // purple
            }[rng.RandiRange(0, 3)];
            AddSceneryMesh(z, offset, MakeSphere(0.04f, flowerCol));
        }

        // Mailbox (one iconic detail)
        AddMailbox(60f, 1f);
        AddMailbox(350f, -1f);
        AddMailbox(700f, 1f);

        UpdateSceneryPositions();
    }

    private void AddTree(float z, float offset, float h, bool isPine, RandomNumberGenerator rng)
    {
        var tree = new Node3D();
        var trunk = MakeCylinder(0.05f, h * 0.45f, new Color(0.32f, 0.2f, 0.1f));
        trunk.Position = new Vector3(0, h * 0.22f, 0);
        tree.AddChild(trunk);

        if (isPine)
        {
            for (int j = 0; j < 4; j++)
            {
                float t = j / 4f;
                float lh = h * 0.2f;
                float lr = (1f - t * 0.35f) * h * 0.2f;
                var col = new Color(0.1f + rng.RandfRange(0, 0.06f), 0.26f + rng.RandfRange(0, 0.1f), 0.08f);
                var foliage = MakeCylinder(lr, lh, col);
                foliage.Position = new Vector3(0, h * 0.32f + j * lh * 0.55f, 0);
                tree.AddChild(foliage);
            }
        }
        else
        {
            var foliage = MakeSphere(h * 0.24f, new Color(0.24f + rng.RandfRange(0, 0.12f), 0.48f + rng.RandfRange(0, 0.12f), 0.14f));
            foliage.Position = new Vector3(0, h * 0.62f, 0);
            tree.AddChild(foliage);
        }

        AddChild(tree);
        _scenery.Add(new SceneryItem { Node = tree, WorldZ = z, OffsetX = offset });
    }

    private void AddSceneryMesh(float z, float offset, MeshInstance3D mesh, Vector3 rotation = default)
    {
        mesh.Rotation = rotation;
        AddChild(mesh);
        _scenery.Add(new SceneryItem { Node = mesh, WorldZ = z, OffsetX = offset });
    }

    private MeshInstance3D MakeSphere(float radius, Color color)
    {
        var m = new MeshInstance3D();
        var mesh = new SphereMesh();
        mesh.Radius = radius;
        mesh.Height = radius * 2f;
        m.Mesh = mesh;
        var mat = new StandardMaterial3D();
        mat.AlbedoColor = color;
        m.MaterialOverride = mat;
        return m;
    }

    private void AddMailbox(float z, float side)
    {
        var box = new Node3D();
        // Post
        var post = MakeCylinder(0.03f, 0.8f, new Color(0.35f, 0.22f, 0.1f));
        post.Position = new Vector3(0, 0.4f, 0);
        box.AddChild(post);
        // Box
        var mail = MakeBox(new Vector3(0.2f, 0.15f, 0.3f), new Color(0.2f, 0.2f, 0.7f));
        mail.Position = new Vector3(0, 0.85f, 0);
        box.AddChild(mail);
        // Flag
        var flag = MakeBox(new Vector3(0.02f, 0.12f, 0.02f), new Color(0.8f, 0.1f, 0.1f));
        flag.Position = new Vector3(0.12f, 0.9f, 0);
        box.AddChild(flag);

        AddChild(box);
        _scenery.Add(new SceneryItem { Node = box, WorldZ = z, OffsetX = side * 5f });
    }

    private void UpdateSceneryPositions()
    {
        foreach (var item in _scenery)
        {
            float relZ = item.WorldZ - _scrollOffset;
            float cx = CurveAt(item.WorldZ) * relZ;
            float cy = HillAt(item.WorldZ) - 0.4f;
            item.Node.Position = new Vector3(item.OffsetX + cx, cy, relZ);
        }
    }

    private Label _subtitleLabel;
    private Label _charLabel;

    // ── UI ───────────────────────────────────────

    private void CreateUI()
    {
        var canvas = new CanvasLayer();
        AddChild(canvas);

        // HUD panel (top left)
        var panel = new PanelContainer();
        panel.AnchorLeft = 0; panel.AnchorTop = 0;
        panel.AnchorRight = 0; panel.AnchorBottom = 0;
        panel.OffsetLeft = 16; panel.OffsetTop = 12;
        panel.OffsetRight = 260; panel.OffsetBottom = 140;
        var ps = new StyleBoxFlat();
        ps.BgColor = new Color(0, 0, 0, 0.55f);
        ps.CornerRadiusTopLeft = 6; ps.CornerRadiusTopRight = 6;
        ps.CornerRadiusBottomLeft = 6; ps.CornerRadiusBottomRight = 6;
        panel.AddThemeStyleboxOverride("panel", ps);
        var vbox = new VBoxContainer();
        vbox.AddThemeConstantOverride("separation", 6);
        _speedLabel = new Label();
        _speedLabel.AddThemeFontSizeOverride("font_size", 22);
        _speedLabel.Text = "0 km/h";
        vbox.AddChild(_speedLabel);
        _distLabel = new Label();
        _distLabel.AddThemeFontSizeOverride("font_size", 16);
        _distLabel.Text = "0 m";
        vbox.AddChild(_distLabel);
        _timerLabel = new Label();
        _timerLabel.AddThemeFontSizeOverride("font_size", 16);
        _timerLabel.Text = "0:00.0";
        vbox.AddChild(_timerLabel);
        panel.AddChild(vbox);
        canvas.AddChild(panel);

        // Title
        _titleLabel = new Label();
        _titleLabel.HorizontalAlignment = HorizontalAlignment.Center;
        _titleLabel.AddThemeFontSizeOverride("font_size", 36);
        _titleLabel.Text = "VERY LONG BOARDS";
        _titleLabel.Modulate = new Color(1f, 0.18f, 0.61f);
        canvas.AddChild(_titleLabel);
        _titleLabel.AnchorLeft = 0.5f; _titleLabel.AnchorTop = 0.08f;
        _titleLabel.AnchorRight = 0.5f; _titleLabel.AnchorBottom = 0.08f;
        _titleLabel.OffsetLeft = -240; _titleLabel.OffsetRight = 240;

        // Subtitle: "A Carl Spatski Game"
        _subtitleLabel = new Label();
        _subtitleLabel.HorizontalAlignment = HorizontalAlignment.Center;
        _subtitleLabel.AddThemeFontSizeOverride("font_size", 16);
        _subtitleLabel.Text = "A Carl Spatski Game";
        _subtitleLabel.Modulate = new Color(0.7f, 0.7f, 0.8f);
        canvas.AddChild(_subtitleLabel);
        _subtitleLabel.AnchorLeft = 0.5f; _subtitleLabel.AnchorTop = 0.15f;
        _subtitleLabel.AnchorRight = 0.5f; _subtitleLabel.AnchorBottom = 0.15f;
        _subtitleLabel.OffsetLeft = -140; _subtitleLabel.OffsetRight = 140;

        // Character select
        _charLabel = new Label();
        _charLabel.HorizontalAlignment = HorizontalAlignment.Center;
        _charLabel.AddThemeFontSizeOverride("font_size", 14);
        _charLabel.Text = "< Office Carl >";
        _charLabel.Modulate = new Color(0.9f, 0.85f, 0.6f);
        canvas.AddChild(_charLabel);
        _charLabel.AnchorLeft = 0.5f; _charLabel.AnchorTop = 0.22f;
        _charLabel.AnchorRight = 0.5f; _charLabel.AnchorBottom = 0.22f;
        _charLabel.OffsetLeft = -120; _charLabel.OffsetRight = 120;

        // Prompt
        _promptLabel = new Label();
        _promptLabel.HorizontalAlignment = HorizontalAlignment.Center;
        _promptLabel.AddThemeFontSizeOverride("font_size", 18);
        _promptLabel.Text = "Press \u2191 to kick off";
        _promptLabel.Modulate = new Color(1f, 0.88f, 0.23f);
        canvas.AddChild(_promptLabel);
        _promptLabel.AnchorLeft = 0.5f; _promptLabel.AnchorTop = 0.8f;
        _promptLabel.AnchorRight = 0.5f; _promptLabel.AnchorBottom = 0.8f;
        _promptLabel.OffsetLeft = -140; _promptLabel.OffsetRight = 140;
    }

    // ── Camera ───────────────────────────────────

    private void UpdateCamera()
    {
        float groundY = HillAt(_playerDistance);
        float slope = (HillAt(_playerDistance + 3f) - HillAt(_playerDistance)) / 3f;
        float sf = Mathf.Clamp(-slope / 2f, 0f, 1f);

        float camH = 4.5f + sf * 3f;
        float camD = 6f + sf * 2f;
        _cameraMount.Position = new Vector3(0, camH, -camD);

        var cam = _cameraMount.GetNode<Camera3D>("Camera3D");
        float curve = CurveAt(_playerDistance);
        cam.LookAt(new Vector3(_playerX + curve * 30f, groundY + 0.5f, 12f), Vector3.Up);
    }

    // ── Game Loop ────────────────────────────────

    public override void _PhysicsProcess(double delta)
    {
        float dt = (float)delta;
        float steer = 0f;
        if (Input.IsActionPressed("move_left")) steer += 1f;
        if (Input.IsActionPressed("move_right")) steer -= 1f;
        bool braking = Input.IsActionPressed("brake");

        switch (_state)
        {
            case GameState.Title:
                UpdateCamera();
                if (Input.IsActionJustPressed("move_left"))
                {
                    _carl = (CarlType)(((int)_carl + 2) % 3);
                    _charLabel.Text = $"< {CarlNames[(int)_carl]} >";
                }
                if (Input.IsActionJustPressed("move_right"))
                {
                    _carl = (CarlType)(((int)_carl + 1) % 3);
                    _charLabel.Text = $"< {CarlNames[(int)_carl]} >";
                }
                if (Input.IsActionJustPressed("kick_off"))
                {
                    GD.Print($"KICK OFF as {CarlNames[(int)_carl]}!");
                    _state = GameState.Riding;
                    _kicked = true;
                    _playerSpeed = 0.3f;
                    _timer = 0f;
                    _titleLabel.Text = "";
                    _subtitleLabel.Text = "";
                    _charLabel.Text = "";
                    _promptLabel.Text = "";
                }
                break;

            case GameState.Riding:
                _timer += dt;
                UpdatePhysics(dt, steer, braking);
                UpdateTerrain();
                UpdateSceneryPositions();
                UpdateCamera();
                UpdateHUD();

                if (_playerDistance >= CourseLength)
                {
                    _state = GameState.Finished;
                    _finishTime = _timer;
                    if (_bestTime <= 0f || _finishTime < _bestTime)
                        _bestTime = _finishTime;
                    ShowFinish();
                }
                break;

            case GameState.Finished:
                UpdateCamera();
                if (Input.IsActionJustPressed("kick_off"))
                    ResetGame();
                break;
        }
    }

    private void UpdatePhysics(float dt, float steer, bool braking)
    {
        float slope = (HillAt(_playerDistance + 3f) - HillAt(_playerDistance)) / 3f;
        _playerSpeed += -slope * Gravity * dt * 60f;
        _playerSpeed *= Mathf.Pow(Friction, dt * 60f);

        if (braking)
            _playerSpeed *= Mathf.Pow(0.97f, dt * 60f);

        _lean = Mathf.Lerp(_lean, steer, 6f * dt);
        _playerX += steer * Handling * dt * 60f * (0.3f + _playerSpeed * 0.5f);
        _playerX = Mathf.Clamp(_playerX, -3.5f, 3.5f);
        _playerSpeed = Mathf.Clamp(_playerSpeed, 0.02f, MaxSpeed);

        _playerDistance += _playerSpeed * dt * 60f;
        _scrollOffset = _playerDistance;

        float groundY = HillAt(_playerDistance);
        _player.Position = new Vector3(_playerX, groundY + 0.1f, 0);

        if (_skaterRoot != null)
            _skaterRoot.Rotation = new Vector3(0, _lean * 0.12f, _lean * 0.18f);
    }

    private void UpdateHUD()
    {
        float kmh = _playerSpeed * 18f;
        _speedLabel.Text = $"{kmh:F0} km/h";
        _distLabel.Text = $"{_playerDistance:F0} m";
        int mins = (int)(_timer / 60f);
        float secs = _timer % 60f;
        _timerLabel.Text = $"{mins}:{secs:00.0}";
    }

    private void ShowFinish()
    {
        _titleLabel.Modulate = new Color(0.22f, 1f, 0.43f);
        _titleLabel.Text = "FINISH!";
        int mins = (int)(_finishTime / 60f);
        float secs = _finishTime % 60f;
        _promptLabel.Text = $"Time: {mins}:{secs:00.0}   |   Press \u2191 to ride again";
    }

    private void ResetGame()
    {
        _state = GameState.Title;
        _kicked = false;
        _playerSpeed = 0f;
        _playerX = 0f;
        _playerDistance = 0f;
        _scrollOffset = 0f;
        _timer = 0f;
        _lean = 0f;
        _player.Position = new Vector3(0, 0.1f, 0);
        if (_skaterRoot != null) _skaterRoot.Rotation = Vector3.Zero;
        _titleLabel.Modulate = new Color(1f, 0.18f, 0.61f);
        _titleLabel.Text = "VERY LONG BOARDS";
        _subtitleLabel.Text = "A Carl Spatski Game";
        _charLabel.Text = $"< {CarlNames[(int)_carl]} >";
        _promptLabel.Text = "Press \u2191 to kick off";
        UpdateTerrain();
        UpdateSceneryPositions();
        UpdateCamera();
    }
}
