using Godot;
using System.Collections.Generic;

public partial class Main : Node3D
{
    private enum GameState { Title, Riding, Paused, Finished }
    private enum CarlType { Office, Party, Dark }
    private GameState _state = GameState.Title;
    private CarlType _carl = CarlType.Office;
    private static readonly string[] CarlNames = { "Office Carl", "Party Carl", "Dark Carl" };
    private static readonly string CourseName = "New Hampshire Summer";
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

    // Scenery
    private struct SceneryItem { public Node3D Node; public float WorldZ; public float OffsetX; }
    private List<SceneryItem> _scenery = new List<SceneryItem>();

    // Clouds
    private struct Cloud { public MeshInstance3D Node; public float BaseX; public float BaseZ; public float Height; public float Speed; }
    private List<Cloud> _clouds = new List<Cloud>();

    // Finish line
    private Node3D _finishLine;

    // Sound
    private AudioStreamPlayer3D _windSound;
    private AudioStreamPlayer3D _wheelSound;
    private float _windPitch = 0.8f;

    // Particles
    private GpuParticles3D _dustParticles;
    private GpuParticles3D _confettiParticles;

    // Camera
    private float _cameraTilt = 0f;

    // Pause
    private Label _pauseLabel;

    // UI
    private Label _speedLabel;
    private Label _distLabel;
    private Label _timerLabel;
    private Label _promptLabel;
    private Label _titleLabel;
    private Label _subtitleLabel;
    private Label _charLabel;
    private Label _bestLabel;
    private Label _courseLabel;

    public override void _Ready()
    {
        _player = GetNode<CharacterBody3D>("Player");
        _cameraMount = GetNode<Node3D>("Player/CameraMount");

        CreateTerrain();
        CreatePlayerMesh();
        CreateScenery();
        CreateClouds();
        CreateFinishLine();
        CreateDustParticles();
        CreateConfettiParticles();
        CreateSound();
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

        // Guard rails (along steep sections)
        for (int i = 0; i < 25; i++)
        {
            float z = 100f + i * 60f + rng.RandfRange(0f, 20f);
            float side = rng.Randf() > 0.5f ? 1f : -1f;
            float offset = side * (4.2f + rng.RandfRange(0f, 0.5f));
            AddGuardRail(z, offset, side);
        }

        // Road signs
        for (int i = 0; i < 8; i++)
        {
            float z = 80f + i * 220f + rng.RandfRange(0f, 40f);
            float side = rng.Randf() > 0.5f ? 1f : -1f;
            float offset = side * (5f + rng.RandfRange(0f, 2f));
            AddRoadSign(z, offset, rng);
        }

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
        var post = MakeCylinder(0.03f, 0.8f, new Color(0.35f, 0.22f, 0.1f));
        post.Position = new Vector3(0, 0.4f, 0);
        box.AddChild(post);
        var mail = MakeBox(new Vector3(0.2f, 0.15f, 0.3f), new Color(0.2f, 0.2f, 0.7f));
        mail.Position = new Vector3(0, 0.85f, 0);
        box.AddChild(mail);
        var flag = MakeBox(new Vector3(0.02f, 0.12f, 0.02f), new Color(0.8f, 0.1f, 0.1f));
        flag.Position = new Vector3(0.12f, 0.9f, 0);
        box.AddChild(flag);
        AddChild(box);
        _scenery.Add(new SceneryItem { Node = box, WorldZ = z, OffsetX = side * 5f });
    }

    private void AddGuardRail(float z, float offset, float side)
    {
        var rail = new Node3D();
        // Posts
        for (int i = 0; i < 3; i++)
        {
            float dz = i * 2f;
            var post = MakeCylinder(0.025f, 0.7f, new Color(0.6f, 0.6f, 0.6f));
            post.Position = new Vector3(0, 0.35f, dz);
            rail.AddChild(post);
        }
        // Rail bar
        var bar = MakeBox(new Vector3(0.04f, 0.04f, 5f), new Color(0.65f, 0.65f, 0.65f));
        bar.Position = new Vector3(0, 0.55f, 2.5f);
        rail.AddChild(bar);
        AddChild(rail);
        _scenery.Add(new SceneryItem { Node = rail, WorldZ = z, OffsetX = offset });
    }

    private void AddRoadSign(float z, float offset, RandomNumberGenerator rng)
    {
        var sign = new Node3D();
        // Post
        var post = MakeCylinder(0.03f, 1.5f, new Color(0.5f, 0.5f, 0.5f));
        post.Position = new Vector3(0, 0.75f, 0);
        sign.AddChild(post);
        // Sign board
        var signColors = new[] {
            new Color(0.9f, 0.8f, 0.1f),  // yellow warning
            new Color(0.2f, 0.5f, 0.9f),  // blue info
            new Color(0.85f, 0.2f, 0.1f),  // red stop
        };
        var col = signColors[rng.RandiRange(0, 2)];
        var board = MakeBox(new Vector3(0.5f, 0.4f, 0.04f), col);
        board.Position = new Vector3(0, 1.6f, 0);
        sign.AddChild(board);
        AddChild(sign);
        _scenery.Add(new SceneryItem { Node = sign, WorldZ = z, OffsetX = offset });
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

    // ── Clouds ───────────────────────────────────

    private void CreateClouds()
    {
        var rng = new RandomNumberGenerator();
        rng.Seed = 77;
        var cloudMat = new StandardMaterial3D();
        cloudMat.AlbedoColor = new Color(0.95f, 0.95f, 0.97f);
        cloudMat.Transparency = BaseMaterial3D.TransparencyEnum.Alpha;
        cloudMat.AlbedoColor = new Color(0.95f, 0.95f, 0.97f, 0.7f);

        for (int i = 0; i < 20; i++)
        {
            var cloud = new MeshInstance3D();
            var mesh = new BoxMesh();
            mesh.Size = new Vector3(rng.RandfRange(8f, 20f), 0.3f, rng.RandfRange(4f, 10f));
            cloud.Mesh = mesh;
            cloud.MaterialOverride = cloudMat;

            float bx = rng.RandfRange(-100f, 100f);
            float bz = rng.RandfRange(-50f, 500f);
            float bh = 35f + rng.RandfRange(0f, 25f);
            float speed = 0.3f + rng.RandfRange(0f, 0.8f);

            cloud.Position = new Vector3(bx, bh, bz);
            AddChild(cloud);
            _clouds.Add(new Cloud { Node = cloud, BaseX = bx, BaseZ = bz, Height = bh, Speed = speed });
        }
    }

    private void UpdateClouds(float dt)
    {
        for (int i = 0; i < _clouds.Count; i++)
        {
            var c = _clouds[i];
            c.BaseX += c.Speed * dt;
            if (c.BaseX > 150f) c.BaseX -= 300f;
            float relZ = c.BaseZ - _scrollOffset;
            c.Node.Position = new Vector3(c.BaseX, c.Height, relZ);
        }
    }

    // ── Finish Line ──────────────────────────────

    private void CreateFinishLine()
    {
        _finishLine = new Node3D();

        var postMat = new StandardMaterial3D();
        postMat.AlbedoColor = new Color(0.9f, 0.15f, 0.15f);

        var bannerMat = new StandardMaterial3D();
        bannerMat.AlbedoColor = new Color(0.95f, 0.95f, 0.9f);

        // Left post
        var postL = MakeCylinder(0.08f, 3f, new Color(0.9f, 0.15f, 0.15f));
        postL.Position = new Vector3(-RoadW / 2f - 0.5f, 1.5f, 0);
        _finishLine.AddChild(postL);

        // Right post
        var postR = MakeCylinder(0.08f, 3f, new Color(0.9f, 0.15f, 0.15f));
        postR.Position = new Vector3(RoadW / 2f + 0.5f, 1.5f, 0);
        _finishLine.AddChild(postR);

        // Banner
        var banner = MakeBox(new Vector3(RoadW + 1.5f, 0.6f, 0.05f), new Color(0.95f, 0.95f, 0.9f));
        banner.Position = new Vector3(0, 2.8f, 0);
        _finishLine.AddChild(banner);

        // Checkered pattern (simple alternating boxes)
        for (int i = 0; i < 12; i++)
        {
            float x = -RoadW / 2f + 0.3f + i * (RoadW / 12f);
            var check = MakeBox(new Vector3(RoadW / 12f - 0.05f, 0.15f, 0.06f),
                i % 2 == 0 ? new Color(0.1f, 0.1f, 0.1f) : new Color(0.9f, 0.15f, 0.15f));
            check.Position = new Vector3(x, 2.55f, 0);
            _finishLine.AddChild(check);
        }

        AddChild(_finishLine);
    }

    private void UpdateFinishLine()
    {
        float relZ = CourseLength - _scrollOffset;
        float cx = CurveAt(CourseLength) * relZ;
        float cy = HillAt(CourseLength);
        _finishLine.Position = new Vector3(cx, cy, relZ);
    }

    // ── Sound ────────────────────────────────────

    private void CreateSound()
    {
        _windSound = new AudioStreamPlayer3D();
        _windSound.MaxDistance = 5f;
        _player.AddChild(_windSound);
    }

    private void UpdateSound(float dt)
    {
        if (_windSound != null)
        {
            float targetVol = Mathf.Clamp(_playerSpeed / MaxSpeed, 0f, 0.3f);
            _windSound.VolumeDb = Mathf.LinearToDb(targetVol);
        }
    }

    // ── Dust Particles ───────────────────────────

    private void CreateDustParticles()
    {
        _dustParticles = new GpuParticles3D();
        _dustParticles.Amount = 40;
        _dustParticles.Lifetime = 0.8f;
        _dustParticles.Transform = new Transform3D(Basis.Identity, new Vector3(0, 0.1f, -0.8f));

        var mat = new ParticleProcessMaterial();
        mat.Direction = new Vector3(0, 0.5f, -1f);
        mat.Spread = 30f;
        mat.InitialVelocityMin = 0.5f;
        mat.InitialVelocityMax = 1.5f;
        mat.Gravity = new Vector3(0, -0.5f, 0);
        mat.ScaleMin = 0.05f;
        mat.ScaleMax = 0.15f;
        mat.Color = new Color(0.6f, 0.55f, 0.4f, 0.6f);

        _dustParticles.ProcessMaterial = mat;
        _dustParticles.Emitting = false;
        _player.AddChild(_dustParticles);
    }

    private void UpdateDustParticles()
    {
        if (_dustParticles != null)
        {
            _dustParticles.Emitting = _playerSpeed > 0.3f && _kicked;
            var mat = _dustParticles.ProcessMaterial as ParticleProcessMaterial;
            if (mat != null)
            {
                float intensity = Mathf.Clamp(_playerSpeed / MaxSpeed, 0.1f, 1f);
                mat.Color = new Color(0.6f, 0.55f, 0.4f, 0.3f + intensity * 0.4f);
            }
        }
    }

    // ── Confetti ─────────────────────────────────

    private void CreateConfettiParticles()
    {
        _confettiParticles = new GpuParticles3D();
        _confettiParticles.Amount = 200;
        _confettiParticles.Lifetime = 2.5f;
        _confettiParticles.OneShot = true;
        _confettiParticles.Emitting = false;

        var mat = new ParticleProcessMaterial();
        mat.Direction = new Vector3(0, 1, 0);
        mat.Spread = 60f;
        mat.InitialVelocityMin = 3f;
        mat.InitialVelocityMax = 8f;
        mat.Gravity = new Vector3(0, -3f, 0);
        mat.ScaleMin = 0.03f;
        mat.ScaleMax = 0.08f;
        mat.Color = new Color(1f, 0.2f, 0.6f, 1f);

        _confettiParticles.ProcessMaterial = mat;
        AddChild(_confettiParticles);
    }

    private void SpawnConfetti()
    {
        if (_confettiParticles != null)
        {
            _confettiParticles.Position = _player.Position + new Vector3(0, 2f, 0);
            _confettiParticles.Restart();
            _confettiParticles.Emitting = true;
        }
    }

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

        _bestLabel = new Label();
        _bestLabel.AddThemeFontSizeOverride("font_size", 12);
        _bestLabel.Text = "";
        _bestLabel.Modulate = new Color(0.7f, 0.85f, 1f);
        vbox.AddChild(_bestLabel);

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

        // Course name
        _courseLabel = new Label();
        _courseLabel.HorizontalAlignment = HorizontalAlignment.Center;
        _courseLabel.AddThemeFontSizeOverride("font_size", 12);
        _courseLabel.Text = CourseName;
        _courseLabel.Modulate = new Color(0.5f, 0.65f, 0.5f);
        canvas.AddChild(_courseLabel);
        _courseLabel.AnchorLeft = 0.5f; _courseLabel.AnchorTop = 0.27f;
        _courseLabel.AnchorRight = 0.5f; _courseLabel.AnchorBottom = 0.27f;
        _courseLabel.OffsetLeft = -100; _courseLabel.OffsetRight = 100;

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

        // Pause label
        _pauseLabel = new Label();
        _pauseLabel.HorizontalAlignment = HorizontalAlignment.Center;
        _pauseLabel.AddThemeFontSizeOverride("font_size", 28);
        _pauseLabel.Text = "";
        _pauseLabel.Modulate = new Color(0.8f, 0.6f, 1f);
        canvas.AddChild(_pauseLabel);
        _pauseLabel.AnchorLeft = 0.5f; _pauseLabel.AnchorTop = 0.4f;
        _pauseLabel.AnchorRight = 0.5f; _pauseLabel.AnchorBottom = 0.4f;
        _pauseLabel.OffsetLeft = -100; _pauseLabel.OffsetRight = 100;
    }

    // ── Camera ───────────────────────────────────

    private void UpdateCamera()
    {
        float groundY = HillAt(_playerDistance);
        float slope = (HillAt(_playerDistance + 3f) - HillAt(_playerDistance)) / 3f;
        float sf = Mathf.Clamp(-slope / 2f, 0f, 1f);
        float speedFactor = Mathf.Clamp(_playerSpeed / MaxSpeed, 0f, 1f);

        // Camera gets slightly closer and higher at speed
        float camH = 4.5f + sf * 3f - speedFactor * 0.5f;
        float camD = 6f + sf * 2f - speedFactor * 1f;
        _cameraMount.Position = new Vector3(0, camH, -camD);

        var cam = _cameraMount.GetNode<Camera3D>("Camera3D");
        float curve = CurveAt(_playerDistance);
        cam.LookAt(new Vector3(_playerX + curve * 30f, groundY + 0.5f, 12f), Vector3.Up);

        // Tilt camera into curves
        float targetTilt = -curve * 15f;
        _cameraTilt = Mathf.Lerp(_cameraTilt, targetTilt, 3f * (float)GetPhysicsProcessDeltaTime());
        cam.Rotation = new Vector3(cam.Rotation.X, cam.Rotation.Y, _cameraTilt);

        // Slight FOV increase at high speed
        cam.Fov = 65f + speedFactor * 5f;
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
                    _courseLabel.Text = "";
                    _promptLabel.Text = "";
                }
                break;

            case GameState.Riding:
                _timer += dt;
                UpdatePhysics(dt, steer, braking);
                UpdateTerrain();
                UpdateSceneryPositions();
                UpdateClouds(dt);
                UpdateFinishLine();
                UpdateDustParticles();
                UpdateSound(dt);
                UpdateCamera();
                UpdateHUD();

                if (Input.IsActionJustPressed("pause"))
                {
                    _state = GameState.Paused;
                    _pauseLabel.Text = "PAUSED";
                    _promptLabel.Text = "Press Esc to resume";
                    break;
                }

                if (_playerDistance >= CourseLength)
                {
                    _state = GameState.Finished;
                    _finishTime = _timer;
                    if (_bestTime <= 0f || _finishTime < _bestTime)
                        _bestTime = _finishTime;
                    SpawnConfetti();
                    ShowFinish();
                }
                break;

            case GameState.Paused:
                UpdateCamera();
                if (Input.IsActionJustPressed("pause"))
                {
                    _state = GameState.Riding;
                    _pauseLabel.Text = "";
                    _promptLabel.Text = "";
                }
                break;

            case GameState.Finished:
                UpdateClouds(dt);
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

        // Lean animation — board tilts, body leans into turn
        if (_skaterRoot != null)
        {
            float leanAngle = _lean * 0.25f;  // board tilt (Z rotation)
            float bodyLean = _lean * 0.15f;    // body lean (Y rotation)
            float speedFactor = Mathf.Clamp(_playerSpeed / MaxSpeed, 0f, 1f);
            _skaterRoot.Rotation = new Vector3(0, bodyLean, leanAngle * (0.5f + speedFactor * 0.5f));
        }
    }

    private void UpdateHUD()
    {
        float kmh = _playerSpeed * 18f;
        _speedLabel.Text = $"{kmh:F0} km/h";
        _distLabel.Text = $"{_playerDistance:F0} m";
        int mins = (int)(_timer / 60f);
        float secs = _timer % 60f;
        _timerLabel.Text = $"{mins}:{secs:00.0}";
        if (_bestTime > 0f)
        {
            int bMins = (int)(_bestTime / 60f);
            float bSecs = _bestTime % 60f;
            _bestLabel.Text = $"Best: {bMins}:{bSecs:00.0}";
        }
    }

    private void ShowFinish()
    {
        _titleLabel.Modulate = new Color(0.22f, 1f, 0.43f);
        _titleLabel.Text = "FINISH!";
        int mins = (int)(_finishTime / 60f);
        float secs = _finishTime % 60f;
        string bestText = "";
        if (_bestTime > 0f)
        {
            int bMins = (int)(_bestTime / 60f);
            float bSecs = _bestTime % 60f;
            bestText = $"  |  Best: {bMins}:{bSecs:00.0}";
        }
        _promptLabel.Text = $"Time: {mins}:{secs:00.0}{bestText}   |   Press \u2191 to ride again";
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
        _courseLabel.Text = CourseName;
        _promptLabel.Text = "Press \u2191 to kick off";
        UpdateTerrain();
        UpdateSceneryPositions();
        UpdateCamera();
    }
}
