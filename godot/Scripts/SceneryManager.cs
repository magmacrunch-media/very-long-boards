using Godot;
using System.Collections.Generic;

/// <summary>
/// Everything beside the road: trees, rocks, houses, the bridge, the finish line and the
/// wildlife. Populations and the fixed landmarks come from a <see cref="CourseDesign"/>.
///
/// Takes a plain parent node rather than <see cref="Main"/> so the editor preview at
/// <c>Scenes/CoursePreview.tscn</c> can dress a road without booting the game.
/// </summary>
public class SceneryManager
{
    private readonly Node3D _parent;
    private bool _created;
    private readonly TerrainManager _terrain;
    private CourseDesign _design;

    /// <summary>
    /// A scrolling prop. <see cref="Absolute"/> pins it to a fixed point on the course
    /// (distance markers); everything else recycles through the travelling band.
    /// </summary>
    public struct SceneryItem
    {
        public Node3D Node; public float WorldZ; public float OffsetX; public bool Absolute;
    }
    public List<SceneryItem> Items = new List<SceneryItem>();

    public struct Cloud { public Node3D Node; public float BaseX; public float BaseZ; public float Height; public float Speed; }
    public List<Cloud> Clouds = new List<Cloud>();

    // Butterflies
    public struct Butterfly { public Node3D Node; public float BaseX; public float BaseY; public float BaseZ; public float Phase; }
    public List<Butterfly> Butterflies = new List<Butterfly>();

    // Birds
    public struct Bird { public Node3D Node; public float BaseX; public float BaseY; public float BaseZ; public float Speed; public float Phase; }
    public List<Bird> Birds = new List<Bird>();

    // Squirrels
    public struct Squirrel { public Node3D Node; public float WorldZ; public float OffsetX; public float Phase; }
    public List<Squirrel> Squirrels = new List<Squirrel>();

    private Node3D _finishLine;

    // The bridge is placed piece by piece rather than as one rigid prop; see AddBridge.
    private const float BridgeLength = 6f;
    private const int BridgeBays = 4;
    private const float BridgeBay = BridgeLength / BridgeBays;

    /// <summary>A bridge piece, placed on the ribbon by its own z rather than the bridge's.</summary>
    private struct BridgePart { public Node3D Node; public float Z; public float X; public float Y; }

    /// <summary>A handrail run, stretched and aimed between the ribbon points at its ends.</summary>
    private struct BridgeRail { public Node3D Node; public float Z0; public float Z1; public float X; public float Y; }

    private Node3D _bridge;
    private MeshInstance3D _bridgeDeck;
    private float _bridgeZ;
    private readonly List<BridgePart> _bridgeParts = new List<BridgePart>();
    private readonly List<BridgeRail> _bridgeRails = new List<BridgeRail>();

    private readonly List<Node3D> _ridges = new List<Node3D>();
    private Node3D _sea;
    private Node3D _cliff;
    private Node3D _cliffFace;

    // ── The travelling band ──────────────────────
    // Props live in a fixed-length band that moves with the player and wrap around inside it,
    // so density stays constant for the whole course instead of running out partway. Sized to
    // the terrain window so a prop can never appear twice in one view.
    public float Band { get { return _design.Band; } }        // 1000 m by default
    public float Behind { get { return _design.Behind; } }    //  175 m by default

    // Population per band — these are densities, not totals, because everything recycles.
    // CourseDesign.Density scales the forest; pines are five meshes each, so that is the knob
    // which decides whether batching becomes necessary.
    private int N(int perBand) { return _design.Scaled(perBand); }

    /// <summary>Where a prop sits right now, wrapped into the band travelling with the player.</summary>
    private float WrapRel(float worldZ, float scrollOffset)
    {
        return Mathf.PosMod(worldZ - scrollOffset + Behind, Band) - Behind;
    }

    public SceneryManager(Node3D parent, TerrainManager terrain, CourseDesign design)
    {
        _parent = parent;
        _terrain = terrain;
        _design = design ?? new CourseDesign();
    }

    /// <summary>
    /// Throw the whole world away and populate a different course.
    ///
    /// Terrain can swap a design without rebuilding because it re-derives its ribbons every
    /// frame. Scenery cannot: every tree, rock and mailbox was placed once from the course's
    /// own populations and seed, so a new course means new props in new places.
    /// </summary>
    public void Rebuild(CourseDesign design)
    {
        Clear();
        _design = design ?? new CourseDesign();
        Create();
    }

    /// <summary>Free everything Create() made. Safe to call before anything has been made.</summary>
    private void Clear()
    {
        if (!_created) return;

        foreach (var item in Items) if (item.Node != null) item.Node.QueueFree();
        foreach (var c in Clouds) if (c.Node != null) c.Node.QueueFree();
        foreach (var b in Butterflies) if (b.Node != null) b.Node.QueueFree();
        foreach (var b in Birds) if (b.Node != null) b.Node.QueueFree();
        foreach (var s in Squirrels) if (s.Node != null) s.Node.QueueFree();
        if (_finishLine != null) { _finishLine.QueueFree(); _finishLine = null; }
        foreach (var r in _ridges) if (r != null) r.QueueFree();
        _ridges.Clear();
        if (_bridge != null) { _bridge.QueueFree(); _bridge = null; _bridgeDeck = null; }
        _bridgeParts.Clear();
        _bridgeRails.Clear();
        if (_sea != null) { _sea.QueueFree(); _sea = null; }
        if (_cliff != null) { _cliff.QueueFree(); _cliff = null; _cliffFace = null; }

        Items.Clear();
        Clouds.Clear();
        Butterflies.Clear();
        Birds.Clear();
        Squirrels.Clear();
        _created = false;
    }

    public void Create()
    {
        _created = true;
        CreateScenery();
        CreateStoneWalls();
        CreateRidges();
        CreateSea();
        CreateClouds();
        CreateFinishLine();
        CreateSunDisc();
        CreateButterflies();
        CreateBirds();
        CreateSquirrels();
    }

    private void CreateScenery()
    {
        var rng = new RandomNumberGenerator();
        rng.Seed = (ulong)_design.ScenerySeed;
        var terrain = _terrain;

        // Pine trees (close)
        for (int i = 0; i < N(_design.ClosePines); i++)
        {
            float z = rng.RandfRange(0f, Band);
            float side = rng.Randf() > 0.5f ? 1f : -1f;
            float offset = side * (5.5f + rng.RandfRange(0f, 12f));
            float h = 5f + rng.RandfRange(0f, 7f);
            AddTree(z, offset, h, true, rng);
        }

        // Pine trees (far)
        for (int i = 0; i < N(_design.FarPines); i++)
        {
            float z = rng.RandfRange(0f, Band);
            float side = rng.Randf() > 0.5f ? 1f : -1f;
            float offset = side * (18f + rng.RandfRange(0f, 25f));
            float h = 8f + rng.RandfRange(0f, 10f);
            AddTree(z, offset, h, true, rng);
        }

        // Deciduous
        for (int i = 0; i < N(_design.Deciduous); i++)
        {
            float z = rng.RandfRange(0f, Band);
            float side = rng.Randf() > 0.5f ? 1f : -1f;
            float offset = side * (7f + rng.RandfRange(0f, 18f));
            float h = 5f + rng.RandfRange(0f, 5f);
            AddTree(z, offset, h, false, rng);
        }

        // Rocks (roadside) — smoother spheres
        for (int i = 0; i < _design.Rocks; i++)
        {
            float z = rng.RandfRange(0f, Band);
            float side = rng.Randf() > 0.5f ? 1f : -1f;
            float offset = side * (4.5f + rng.RandfRange(0f, 4f));
            float size = 0.12f + rng.RandfRange(0f, 0.3f);
            var rockMat = new StandardMaterial3D();
            rockMat.AlbedoColor = new Color(0.95f + rng.RandfRange(0, 0.08f), 0.94f + rng.RandfRange(0, 0.06f), 0.92f + rng.RandfRange(0, 0.05f));
            rockMat.AlbedoTexture = TextureKit.Rock;
            rockMat.Uv1Scale = new Vector3(1.5f, 1.5f, 1f);
            rockMat.SpecularMode = BaseMaterial3D.SpecularModeEnum.Disabled;
            var rock = MeshKit.Sphere(null, size, rockMat, segments: 8, rings: 6);
            rock.Scale = new Vector3(1f, 0.6f + rng.RandfRange(0, 0.2f), 1f);
            rock.Rotation = new Vector3(rng.RandfRange(0, 0.3f), rng.RandfRange(0, 3f), 0);
            AddItem(z, offset, rock);
        }

        // Stumps
        for (int i = 0; i < _design.Stumps; i++)
        {
            float z = rng.RandfRange(0f, Band);
            float side = rng.Randf() > 0.5f ? 1f : -1f;
            float offset = side * (5f + rng.RandfRange(0f, 6f));
            AddItem(z, offset, MeshKit.Cylinder(null, 0.15f, 0.15f + rng.RandfRange(0f, 0.2f),
                MeshKit.Mat(Colors.White, texture: TextureKit.Bark, uvScale: 1.5f), segments: 8));
        }

        // Wildflowers — more variety and density
        for (int i = 0; i < _design.Wildflowers; i++)
        {
            float z = rng.RandfRange(0f, Band);
            float side = rng.Randf() > 0.5f ? 1f : -1f;
            float offset = side * (4.2f + rng.RandfRange(0f, 3f));
            var col = new[] {
                new Color(0.98f, 0.92f, 0.28f), new Color(0.98f, 0.48f, 0.58f),
                new Color(0.88f, 0.88f, 0.92f), new Color(0.68f, 0.48f, 0.88f),
                new Color(1f, 0.68f, 0.28f)
            }[rng.RandiRange(0, 4)];
            AddItem(z, offset, MeshKit.Sphere(null, 0.04f + rng.RandfRange(0, 0.02f), MeshKit.Mat(col), segments: 6));
        }

        // Ferns — low green fronds
        for (int i = 0; i < _design.Ferns; i++)
        {
            float z = rng.RandfRange(0f, Band);
            float side = rng.Randf() > 0.5f ? 1f : -1f;
            float offset = side * (4.5f + rng.RandfRange(0f, 3f));
            var fernMat = MeshKit.Mat(new Color(0.85f + rng.RandfRange(0, 0.2f),
                1f + rng.RandfRange(0, 0.15f), 0.8f + rng.RandfRange(0, 0.15f)),
                texture: TextureKit.Leaf, uvScale: 1f);
            var fern = new Node3D();
            // Multiple small flat ellipses for fern fronds
            for (int j = 0; j < 3; j++)
            {
                var frond = new MeshInstance3D();
                var fMesh = new SphereMesh();
                fMesh.Radius = 0.08f + rng.RandfRange(0, 0.04f);
                fMesh.Height = 0.04f;
                fMesh.RadialSegments = 8;
                frond.Mesh = fMesh;
                frond.MaterialOverride = fernMat;
                frond.Position = new Vector3(rng.RandfRange(-0.1f, 0.1f), 0.02f, rng.RandfRange(-0.1f, 0.1f));
                frond.Rotation = new Vector3(rng.RandfRange(-0.3f, 0.3f), rng.RandfRange(0, 3f), 0);
                fern.AddChild(frond);
            }
            AddItem(z, offset, fern);
        }

        // Bushes — multiple overlapping spheres for organic look
        for (int i = 0; i < _design.Bushes; i++)
        {
            float z = rng.RandfRange(0f, Band);
            float side = rng.Randf() > 0.5f ? 1f : -1f;
            float offset = side * (4.5f + rng.RandfRange(0f, 4f));
            float size = 0.15f + rng.RandfRange(0f, 0.25f);
            var bushMat = MeshKit.Mat(new Color(0.8f + rng.RandfRange(0, 0.15f),
                0.9f + rng.RandfRange(0, 0.12f), 0.75f + rng.RandfRange(0, 0.1f)),
                texture: TextureKit.Leaf, uvScale: 1.5f);

            var bush = new Node3D();
            bush.AddChild(MeshKit.Sphere(null, size, bushMat, segments: 8, rings: 6));
            var puff = MeshKit.Sphere(null, size * 0.7f, bushMat, segments: 8, rings: 6);
            puff.Position = new Vector3(size * 0.3f, size * 0.2f, 0);
            bush.AddChild(puff);
            AddItem(z, offset, bush);
        }

        // Logs
        for (int i = 0; i < _design.Logs; i++)
        {
            float z = rng.RandfRange(0f, Band);
            float side = rng.Randf() > 0.5f ? 1f : -1f;
            float offset = side * (5f + rng.RandfRange(0f, 5f));
            float length = 0.8f + rng.RandfRange(0f, 1.2f);
            var log = MeshKit.Cylinder(null, 0.06f, length,
                MeshKit.Mat(Colors.White, texture: TextureKit.Bark, uvScale: 2f), segments: 8);
            log.Rotation = new Vector3(0, rng.RandfRange(0, Mathf.Pi), Mathf.Pi / 2f);
            AddItem(z, offset, log);
        }

        // Mailboxes
        AddMailbox(60f, 1f);
        AddMailbox(350f, -1f);
        AddMailbox(700f, 1f);

        // Road signs
        for (int i = 0; i < _design.RoadSigns; i++)
        {
            float z = 80f + i * 220f + rng.RandfRange(0f, 40f);
            float side = rng.Randf() > 0.5f ? 1f : -1f;
            float offset = side * (5f + rng.RandfRange(0f, 2f));
            AddRoadSign(z, offset, rng);
        }

        // Houses
        for (int i = 0; i < _design.Houses; i++)
        {
            float z = 150f + i * 200f + rng.RandfRange(0f, 50f);
            float side = rng.Randf() > 0.5f ? 1f : -1f;
            float offset = side * (12f + rng.RandfRange(0f, 8f));
            AddHouse(z, offset, rng);
        }

        // Bridge
        AddBridge(800f);

        // Streams
        for (int i = 0; i < _design.Streams; i++)
        {
            float z = 250f + i * 400f + rng.RandfRange(0f, 100f);
            float side = rng.Randf() > 0.5f ? 1f : -1f;
            float offset = side * (8f + rng.RandfRange(0f, 5f));
            AddStream(z, offset, rng);
        }

        // Distance markers
        for (float mz = _design.MarkerSpacing; mz < _design.Length; mz += _design.MarkerSpacing)
            AddDistanceMarker(mz);

        UpdatePositions(0f);
    }

    /// <summary>
    /// A tree: solid trunk, foliage as crossed alpha-cut billboards.
    ///
    /// The billboards are what N64 forests actually were. Three quads at 60-degree intervals
    /// read as a leafy mass from any angle, cost fewer triangles than the faceted cones they
    /// replace, and — because the material is alpha-scissor — need no depth sorting.
    /// </summary>
    private void AddTree(float z, float offset, float h, bool isPine, RandomNumberGenerator rng)
    {
        var tree = new Node3D();

        // Trunk
        var trunkMat = MeshKit.Mat(new Color(1f, 0.95f + rng.RandfRange(0, 0.1f), 0.95f),
            texture: TextureKit.Bark, uvScale: 1.5f);
        var trunk = MeshKit.Cylinder(null, isPine ? 0.05f : 0.07f, h * 0.45f, trunkMat, segments: 8);
        trunk.Position = new Vector3(0, h * 0.22f, 0);
        tree.AddChild(trunk);

        // Per-tree tint so the treeline doesn't read as one clone repeated.
        var tint = new Color(0.88f + rng.RandfRange(0, 0.24f),
                             0.90f + rng.RandfRange(0, 0.20f),
                             0.85f + rng.RandfRange(0, 0.20f));
        var folMat = isPine
            ? MeshKit.CutoutMat(TextureKit.PineBillboard, tint)
            : MeshKit.CutoutMat(TextureKit.LeafBillboard, tint);

        // Conifers are tall and narrow; broadleaves are wide and sit higher up the trunk.
        float fw = isPine ? h * 0.52f : h * 0.66f;
        float fh = isPine ? h * 0.85f : h * 0.55f;
        float fy = isPine ? h * 0.52f : h * 0.68f;
        float yaw = rng.RandfRange(0f, Mathf.Pi);

        for (int j = 0; j < 3; j++)
        {
            var quad = MeshKit.Quad(null, new Vector2(fw, fh), folMat,
                new Vector3(0, fy, 0), new Vector3(0, yaw + j * Mathf.Pi / 3f, 0));
            tree.AddChild(quad);
        }

        _parent.AddChild(tree);
        Items.Add(new SceneryItem { Node = tree, WorldZ = z, OffsetX = offset });
    }

    private void AddMailbox(float z, float side)
    {
        var box = new Node3D();

        var postMat = new StandardMaterial3D();
        postMat.AlbedoColor = new Color(0.38f, 0.24f, 0.12f);

        var mailMat = new StandardMaterial3D();
        mailMat.AlbedoColor = new Color(0.3f, 0.3f, 0.8f);

        var flagMat = new StandardMaterial3D();
        flagMat.AlbedoColor = new Color(0.95f, 0.2f, 0.2f);

        // Post
        box.AddChild(MeshKit.Cylinder(null, 0.035f, 0.9f, postMat, new Vector3(0, 0.45f, 0), segments: 6));
        // Mailbox body
        box.AddChild(MeshKit.Box(null, new Vector3(0.22f, 0.18f, 0.35f), mailMat, new Vector3(0, 0.92f, 0)));
        // Flag
        box.AddChild(MeshKit.Box(null, new Vector3(0.025f, 0.14f, 0.025f), flagMat, new Vector3(0.13f, 0.98f, 0)));

        _parent.AddChild(box);
        Items.Add(new SceneryItem { Node = box, WorldZ = z, OffsetX = side * 5f });
    }

    private void AddRoadSign(float z, float offset, RandomNumberGenerator rng)
    {
        var sign = new Node3D();
        var postMat = new StandardMaterial3D();
        postMat.AlbedoColor = new Color(0.55f, 0.55f, 0.58f);

        var signColors = new[] {
            new Color(1f, 0.9f, 0.2f),      // yellow warning
            new Color(0.3f, 0.6f, 1f),       // blue info
            new Color(0.98f, 0.3f, 0.2f),    // red stop
        };
        var signMat = new StandardMaterial3D();
        signMat.AlbedoColor = signColors[rng.RandiRange(0, 2)];

        sign.AddChild(MeshKit.Cylinder(null, 0.03f, 1.5f, postMat, new Vector3(0, 0.75f, 0), segments: 6));
        sign.AddChild(MeshKit.Box(null, new Vector3(0.55f, 0.45f, 0.04f), signMat, new Vector3(0, 1.65f, 0)));

        _parent.AddChild(sign);
        Items.Add(new SceneryItem { Node = sign, WorldZ = z, OffsetX = offset });
    }

    private void AddHouse(float z, float offset, RandomNumberGenerator rng)
    {
        var house = new Node3D();
        var wallColors = new[] {
            new Color(0.98f, 0.96f, 0.9f),   // white clapboard
            new Color(0.95f, 0.85f, 0.7f),    // cream
            new Color(0.8f, 0.92f, 0.85f),    // sage green
            new Color(0.9f, 0.8f, 0.75f),     // beige
        };
        var roofColors = new[] {
            new Color(0.52f, 0.2f, 0.16f),    // dark red shingles
            new Color(0.35f, 0.28f, 0.24f),   // dark brown
            new Color(0.45f, 0.45f, 0.48f),   // grey
        };
        var w = wallColors[rng.RandiRange(0, 3)];
        var r = roofColors[rng.RandiRange(0, 2)];

        // Materials
        var wallMat = MeshKit.Mat(new Color(w.R * 1.8f, w.G * 1.8f, w.B * 1.8f),
            texture: TextureKit.Plank, uvScale: 3f);

        var roofMat = new StandardMaterial3D();
        roofMat.AlbedoColor = r;
        roofMat.SpecularMode = BaseMaterial3D.SpecularModeEnum.Disabled;

        var doorMat = new StandardMaterial3D();
        doorMat.AlbedoColor = new Color(0.48f, 0.3f, 0.18f);

        var winMat = new StandardMaterial3D();
        winMat.AlbedoColor = new Color(0.82f, 0.92f, 1f, 0.92f);
        winMat.Transparency = BaseMaterial3D.TransparencyEnum.Alpha;

        // Main body
        MeshKit.Box(house, new Vector3(2.5f, 1.8f, 2f), wallMat, new Vector3(0, 0.9f, 0));
        // Roof
        MeshKit.Box(house, new Vector3(2.8f, 0.15f, 2.3f), roofMat, new Vector3(0, 1.85f, 0));
        MeshKit.Box(house, new Vector3(2.8f, 0.6f, 0.12f), roofMat, new Vector3(0, 2.15f, 0));
        // Door
        MeshKit.Box(house, new Vector3(0.42f, 0.85f, 0.06f), doorMat, new Vector3(0, 0.42f, 1.02f));
        // Door handle
        var handleMat = new StandardMaterial3D();
        handleMat.AlbedoColor = new Color(0.7f, 0.65f, 0.3f);
        MeshKit.Box(house, new Vector3(0.04f, 0.04f, 0.04f), handleMat, new Vector3(0.12f, 0.45f, 1.06f));
        // Windows
        foreach (var wp in new[] { new Vector3(-0.7f, 1.15f, 1.02f), new Vector3(0.7f, 1.15f, 1.02f) })
        {
            MeshKit.Box(house, new Vector3(0.42f, 0.42f, 0.03f), winMat, wp);
            // Window frame
            var frameMat = new StandardMaterial3D();
            frameMat.AlbedoColor = new Color(0.9f, 0.88f, 0.8f);
            MeshKit.Box(house, new Vector3(0.48f, 0.03f, 0.04f), frameMat, wp + new Vector3(0, 0.22f, 0.01f));
            MeshKit.Box(house, new Vector3(0.48f, 0.03f, 0.04f), frameMat, wp + new Vector3(0, -0.22f, 0.01f));
        }

        _parent.AddChild(house);
        Items.Add(new SceneryItem { Node = house, WorldZ = z, OffsetX = offset });
    }

    /// <summary>
    /// The bridge, which is the one prop long enough for the road to move underneath it.
    ///
    /// Everything else here is a tree or a post: a metre wide, so a single height and a single
    /// lateral offset taken at its anchor are right for all of it. A bridge is 6 m of straight,
    /// flat, rigid structure, and over 6 m of Frogwood the road drops 54 cm and swings a metre
    /// sideways. Given one anchor point it therefore had to be wrong at one end or the other,
    /// and it was wrong at both: buried in the asphalt where it starts, and standing clear of
    /// the road on the diagonal where it ends. That is what was showing.
    ///
    /// So it is not a rigid prop any more. The deck is rebuilt every frame from HillAt and
    /// CurveAt exactly the way TerrainManager builds the road, which is the only way to get a
    /// trapezoid that matches a trapezoid, and the posts and handrails are placed one at a
    /// time by their own z rather than by the bridge's.
    /// </summary>
    private void AddBridge(float z)
    {
        _bridgeZ = z;
        _bridge = new Node3D();
        _parent.AddChild(_bridge);

        // Weathered rather than fresh-sawn. The deck used to sit UNDER the asphalt, where its
        // colour never mattered; on top of it, at full brightness, raw plank was the loudest
        // thing on screen after Carl's shirt.
        var woodMat = MeshKit.Mat(new Color(0.72f, 0.66f, 0.60f), texture: TextureKit.Plank, uvScale: 4f);
        var railMat = new StandardMaterial3D();
        railMat.AlbedoColor = new Color(0.65f, 0.65f, 0.68f);
        railMat.SpecularMode = BaseMaterial3D.SpecularModeEnum.Disabled;

        _bridgeDeck = new MeshInstance3D();
        _bridgeDeck.MaterialOverride = woodMat;
        _bridge.AddChild(_bridgeDeck);

        // Posts every 1.5 m down both kerbs, and a handrail between each pair.
        float rail = _design.RoadWidth / 2f + 0.3f;
        for (float side = -1f; side <= 1f; side += 2f)
        {
            for (int i = 0; i <= BridgeBays; i++)
            {
                var post = MeshKit.Cylinder(null, 0.03f, 1f, railMat, segments: 6);
                _bridge.AddChild(post);
                _bridgeParts.Add(new BridgePart {
                    Node = post, Z = i * BridgeBay, X = side * rail, Y = 0.5f });
            }
            for (int i = 0; i < BridgeBays; i++)
            {
                var bar = MeshKit.Box(null, new Vector3(0.04f, 0.04f, 1f), railMat);
                _bridge.AddChild(bar);
                _bridgeRails.Add(new BridgeRail {
                    Node = bar, Z0 = i * BridgeBay, Z1 = (i + 1) * BridgeBay, X = side * rail, Y = 0.85f });
            }
        }
    }

    /// <summary>Where the ribbon puts a point: the same arithmetic the road is drawn with.</summary>
    private Vector3 OnRibbon(float scroll, float lz, float xOff, float yOff)
    {
        float wz = scroll + lz;
        return new Vector3(_terrain.CurveAt(wz) * lz + xOff, _terrain.HillAt(wz) + yOff, lz);
    }

    /// <summary>
    /// Lay the bridge on the road wherever the road currently is.
    ///
    /// The deck is a mesh rather than a box because a box cannot be a trapezoid, and every
    /// quad of the road is one - the ribbon's edges converge and diverge as the heading
    /// changes. A box deck built to the road's width therefore sits on the road at one end
    /// and beside it at the other however carefully it is placed.
    /// </summary>
    private void UpdateBridge()
    {
        if (_bridge == null) return;

        float scroll = _terrain.ScrollOffset;
        float rel = WrapRel(_bridgeZ, scroll);

        _bridgeDeck.Mesh = BuildDeck(scroll, rel);

        foreach (var p in _bridgeParts)
            p.Node.Position = OnRibbon(scroll, rel + p.Z, p.X, p.Y);

        foreach (var r in _bridgeRails)
        {
            Vector3 a = OnRibbon(scroll, rel + r.Z0, r.X, r.Y);
            Vector3 b = OnRibbon(scroll, rel + r.Z1, r.X, r.Y);
            Vector3 dir = b - a;
            float len = dir.Length();
            if (len < 0.001f) continue;
            dir /= len;
            Vector3 right = Vector3.Up.Cross(dir).Normalized();
            var basis = new Basis(right, dir.Cross(right), dir).Scaled(new Vector3(1f, 1f, len));
            r.Node.Transform = new Transform3D(basis, (a + b) * 0.5f);
        }
    }

    /// <summary>Deck planking: a short ribbon on the road, with a beam down each side.</summary>
    private Mesh BuildDeck(float scroll, float rel)
    {
        float half = (_design.RoadWidth + 1f) / 2f;
        const float top = 0.02f;        // just proud of the asphalt: you ride across it
        const float thick = 0.16f;
        const int steps = 12;

        var st = new SurfaceTool();
        st.Begin(Mesh.PrimitiveType.Triangles);
        for (int i = 0; i < steps; i++)
        {
            float lz0 = rel + BridgeLength * i / steps;
            float lz1 = rel + BridgeLength * (i + 1) / steps;
            Vector3 a = OnRibbon(scroll, lz0, 0f, top);
            Vector3 b = OnRibbon(scroll, lz1, 0f, top);
            float v0 = (BridgeLength * i / steps) / 1.2f;
            float v1 = (BridgeLength * (i + 1) / steps) / 1.2f;

            Face(st, new Vector3(a.X - half, a.Y, lz0), new Vector3(a.X + half, a.Y, lz0),
                     new Vector3(b.X - half, b.Y, lz1), new Vector3(b.X + half, b.Y, lz1), v0, v1);
            for (float side = -1f; side <= 1f; side += 2f)
            {
                float ax = a.X + side * half, bx = b.X + side * half;
                Face(st, new Vector3(ax, a.Y, lz0), new Vector3(ax, a.Y - thick, lz0),
                         new Vector3(bx, b.Y, lz1), new Vector3(bx, b.Y - thick, lz1), v0, v1);
            }
        }
        st.GenerateNormals();
        return st.Commit();
    }

    /// <summary>One quad of the deck, as two triangles.</summary>
    private static void Face(SurfaceTool st, Vector3 a0, Vector3 a1, Vector3 b0, Vector3 b1,
                             float v0, float v1)
    {
        st.SetUV(new Vector2(0f, v0)); st.AddVertex(a0);
        st.SetUV(new Vector2(1f, v0)); st.AddVertex(a1);
        st.SetUV(new Vector2(0f, v1)); st.AddVertex(b0);
        st.SetUV(new Vector2(1f, v0)); st.AddVertex(a1);
        st.SetUV(new Vector2(1f, v1)); st.AddVertex(b1);
        st.SetUV(new Vector2(0f, v1)); st.AddVertex(b0);
    }

    private void AddStream(float z, float offset, RandomNumberGenerator rng)
    {
        var stream = new Node3D();

        // Water surface with transparency and emission
        var waterMat = new StandardMaterial3D();
        waterMat.AlbedoColor = new Color(0.38f, 0.65f, 0.88f, 0.82f);
        waterMat.Transparency = BaseMaterial3D.TransparencyEnum.Alpha;

        var water = new MeshInstance3D();
        var waterMesh = new CylinderMesh();
        waterMesh.TopRadius = 2f + rng.RandfRange(0f, 1f);
        waterMesh.BottomRadius = waterMesh.TopRadius;
        waterMesh.Height = 0.05f;
        water.Mesh = waterMesh;
        water.MaterialOverride = waterMat;
        water.Position = new Vector3(0, -0.15f, 0);
        stream.AddChild(water);

        // Rocks around the edge
        var rockMat = new StandardMaterial3D();
        rockMat.AlbedoColor = new Color(0.48f, 0.46f, 0.42f);
        for (int i = 0; i < 6; i++)
        {
            float angle = i * Mathf.Pi / 3f;
            float r = 1.8f + rng.RandfRange(0f, 0.5f);
            var rock = MeshKit.Sphere(null, 0.15f + rng.RandfRange(0f, 0.1f), rockMat, segments: 6);
            rock.Position = new Vector3(Mathf.Cos(angle) * r, -0.05f, Mathf.Sin(angle) * r);
            stream.AddChild(rock);
        }

        _parent.AddChild(stream);
        Items.Add(new SceneryItem { Node = stream, WorldZ = z, OffsetX = offset });
    }

    private void AddDistanceMarker(float z)
    {
        var marker = new Node3D();

        var postMat = new StandardMaterial3D();
        postMat.AlbedoColor = new Color(0.82f, 0.82f, 0.72f);

        var signMat = new StandardMaterial3D();
        signMat.AlbedoColor = new Color(1f, 1f, 0.92f);

        // Post
        marker.AddChild(MeshKit.Cylinder(null, 0.03f, 0.9f, postMat, new Vector3(0, 0.45f, 0), segments: 6));
        // Sign
        marker.AddChild(MeshKit.Box(null, new Vector3(0.35f, 0.25f, 0.04f), signMat, new Vector3(0, 0.92f, 0)));

        _parent.AddChild(marker);
        Items.Add(new SceneryItem { Node = marker, WorldZ = z, OffsetX = _design.RoadWidth / 2f + 0.5f, Absolute = true });
    }

    /// <summary>
    /// Field walls along the verge.
    ///
    /// Block Island is glacial moraine: the stone came out of the fields and went into the
    /// walls, and there are hundreds of miles of them. They are the first thing anyone
    /// notices about the island and the cheapest way to make a road read as that road rather
    /// than as a road. Frogwood leaves StoneWalls at zero and builds none.
    ///
    /// Each wall is one run of a few metres, built from stacked flattened spheres rather than
    /// a box, because a dry stone wall's whole character is that its edge is lumpy.
    /// </summary>
    private void CreateStoneWalls()
    {
        if (_design.StoneWalls <= 0) return;

        var rng = new RandomNumberGenerator();
        rng.Seed = (ulong)(_design.ScenerySeed + 977);

        for (int i = 0; i < _design.StoneWalls; i++)
        {
            float z = rng.RandfRange(0f, Band);
            float side = rng.Randf() > 0.5f ? 1f : -1f;
            float offset = side * (5.5f + rng.RandfRange(0f, 3.5f));
            float runLength = 6f + rng.RandfRange(0f, 14f);
            float height = 0.55f + rng.RandfRange(0f, 0.35f);

            var wall = new Node3D();
            // Stones along the run, in two courses, each one sitting a little proud of its
            // neighbours. Six segments and four rings keeps a whole wall cheap.
            for (float d = 0f; d < runLength; d += 0.42f)
            {
                int courses = height > 0.75f ? 2 : 1;
                for (int c = 0; c < courses; c++)
                {
                    float size = 0.20f + rng.RandfRange(0f, 0.10f);
                    var mat = new StandardMaterial3D();
                    float g = 0.62f + rng.RandfRange(0f, 0.16f);
                    mat.AlbedoColor = new Color(g, g * 0.99f, g * 0.95f);
                    mat.AlbedoTexture = TextureKit.Rock;
                    mat.Uv1Scale = new Vector3(2f, 2f, 1f);
                    mat.SpecularMode = BaseMaterial3D.SpecularModeEnum.Disabled;

                    var stone = MeshKit.Sphere(null, size, mat, segments: 6, rings: 4);
                    stone.Scale = new Vector3(1.15f, 0.72f, 1f);
                    stone.Position = new Vector3(
                        rng.RandfRange(-0.06f, 0.06f),
                        size * 0.6f + c * (height * 0.5f),
                        d + rng.RandfRange(-0.05f, 0.05f));
                    stone.Rotation = new Vector3(0f, rng.RandfRange(0f, 3.14f), 0f);
                    wall.AddChild(stone);
                }
            }
            AddItem(z, offset, wall);
        }
    }

    /// <summary>
    /// The skyline. Two or three silhouette bands standing well beyond the drawn world, each
    /// fainter than the one in front of it.
    ///
    /// They are placed past the terrain window rather than inside it, so a band can never cut
    /// across the road, and their material ignores the environment fog - at 1250 m the fog
    /// would take any colour to flat grey, so the recession is painted into CourseDesign's
    /// RidgeColors instead of being computed. That is how the era did backdrops, and it is
    /// also the only way to have one at these distances without turning the fog down.
    /// </summary>
    private void CreateRidges()
    {
        if (!_design.HasRidge || _design.RidgeColors == null || _design.RidgeColors.Length == 0)
            return;

        int bands = Mathf.Min(_design.RidgeBands, _design.RidgeColors.Length);
        for (int i = 0; i < bands; i++)
        {
            float dist = _design.RidgeNear * Mathf.Pow(_design.RidgeStep, i);

            var mat = new StandardMaterial3D();
            mat.AlbedoColor = Colors.White;
            mat.VertexColorUseAsAlbedo = true;
            // Vertex colours are taken as linear unless this says otherwise, while AlbedoColor
            // is taken as sRGB - so moving the ridge tint from one to the other silently
            // brightened every band (0.29 linear reads back as 0.57) and turned three
            // silhouettes into three washes.
            mat.VertexColorIsSrgb = true;
            mat.ShadingMode = BaseMaterial3D.ShadingModeEnum.Unshaded;
            mat.SpecularMode = BaseMaterial3D.SpecularModeEnum.Disabled;
            mat.DisableFog = true;
            mat.CullMode = BaseMaterial3D.CullModeEnum.Disabled;

            // Peaks grow with distance so every band subtends about the same angle - a far
            // band scaled like the near one is a bump on the horizon and reads as nothing.
            float height = _design.RidgeHeight * Mathf.Pow(_design.RidgeStep, i);

            // Haze pools in the valleys, so each band is washed toward the sky at its foot
            // and full strength at its peaks. Flat bands read as a green curtain once the
            // nearest one has risen far enough to hide the two behind it, which is what a
            // 178 m descent does to it by the finish.
            Color top = _design.RidgeColors[i];
            Color foot = top.Lerp(_design.SkyHorizon, 0.35f);

            var band = new MeshInstance3D();
            band.Mesh = BuildRidge(dist, height, _design.RidgeSeed + i * 17, top, foot);
            band.MaterialOverride = mat;
            _parent.AddChild(band);
            _ridges.Add(band);
        }
    }

    /// <summary>
    /// One band: a wall whose top edge is four sines summed, skirted far enough below its own
    /// foot that the bottom is never in shot whatever the road does.
    /// </summary>
    private static Mesh BuildRidge(float dist, float height, int seed, Color top, Color foot)
    {
        // Three times the distance covers the widest curve throw the ribbon can produce and
        // still leaves the ends off-screen.
        float half = dist * 1.5f;
        const int cols = 192;
        float wave = dist * 0.55f;

        var st = new SurfaceTool();
        st.Begin(Mesh.PrimitiveType.Triangles);
        for (int i = 0; i < cols; i++)
        {
            float x0 = Mathf.Lerp(-half, half, i / (float)cols);
            float x1 = Mathf.Lerp(-half, half, (i + 1) / (float)cols);
            float y0 = RidgeTop(x0, wave, seed) * height;
            float y1 = RidgeTop(x1, wave, seed) * height;
            const float floor = -900f;

            // Three rows, and the haze hangs from the SKYLINE rather than standing up from the
            // band's foot. Anchored to the foot it works at the start line and then vanishes:
            // by the finish the rider is 178 m lower, the foot is off the bottom of the screen,
            // and every band is a flat slab again. Hung from the ridge it is always in shot,
            // which is also how haze actually reads - thickest under the crest.
            float wash = height * 0.5f;

            Strip(st, x0, x1, floor, floor, foot, foot, y0 - wash, y1 - wash, foot, foot);
            Strip(st, x0, x1, y0 - wash, y1 - wash, foot, foot, y0, y1, top, top);
        }
        st.GenerateNormals();
        return st.Commit();
    }

    /// <summary>One quad of a ridge column, coloured per corner.</summary>
    private static void Strip(SurfaceTool st, float x0, float x1,
        float yl0, float yr0, Color cl0, Color cr0,
        float yl1, float yr1, Color cl1, Color cr1)
    {
        st.SetColor(cl0); st.AddVertex(new Vector3(x0, yl0, 0f));
        st.SetColor(cr0); st.AddVertex(new Vector3(x1, yr0, 0f));
        st.SetColor(cl1); st.AddVertex(new Vector3(x0, yl1, 0f));
        st.SetColor(cr0); st.AddVertex(new Vector3(x1, yr0, 0f));
        st.SetColor(cr1); st.AddVertex(new Vector3(x1, yr1, 0f));
        st.SetColor(cl1); st.AddVertex(new Vector3(x0, yl1, 0f));
    }

    /// <summary>Height of the skyline at <paramref name="x"/>, 0 to 1.</summary>
    private static float RidgeTop(float x, float wave, int seed)
    {
        float h = 0f, norm = 0f, amp = 1f, w = wave;
        for (int o = 0; o < 4; o++)
        {
            h += amp * Mathf.Sin(x / w * Mathf.Tau + RidgePhase(seed, o));
            norm += amp;
            amp *= 0.55f;
            w *= 0.43f;
        }
        float t = (h / norm + 1f) * 0.5f;
        return t * t * (3f - 2f * t);   // broad valleys, rounded tops
    }

    private static float RidgePhase(int seed, int octave)
    {
        uint h = (uint)(seed * 374761393 + octave * 668265263);
        h = (h ^ (h >> 13)) * 1274126177u;
        return ((h ^ (h >> 16)) & 0xFFFFFFu) / (float)0x1000000 * Mathf.Tau;
    }

    /// <summary>
    /// Hold the skyline still while the rider falls past it.
    ///
    /// The Y is the course's own RidgeFoot and never moves, which is the entire reason the
    /// bands exist: the world's heights are absolute and the camera descends through them, so
    /// hills that stay put are the only thing on screen that registers a 178 m drop. The X
    /// tracks the road's heading at the band's own distance, exactly as the ribbon and the
    /// props do, so the skyline swings across when the road turns and sits still when it
    /// does not.
    /// </summary>
    private void UpdateRidges()
    {
        if (_ridges.Count == 0) return;
        float z = _terrain.ScrollOffset;
        for (int i = 0; i < _ridges.Count; i++)
        {
            float dist = _design.RidgeNear * Mathf.Pow(_design.RidgeStep, i);
            float cx = _terrain.CurveAt(z + dist) * dist;
            _ridges[i].Position = new Vector3(cx, _design.RidgeFoot, dist);
        }
    }

    /// <summary>
    /// Open water beside the road, and the cliff that falls away to it.
    ///
    /// Both are single quads that follow the rider rather than props that recycle: the sea has
    /// no features to pass, so the only thing that would betray a fixed plane is its edge, and
    /// the edge is over the horizon. They are held apart from Items for that reason — nothing
    /// scrolls them, UpdateSea just keeps them under the rider.
    ///
    /// The cliff is deliberately not the Mohegan Bluffs in miniature. It is the ground ending,
    /// which is what you actually see from a road on top of them.
    /// </summary>
    private void CreateSea()
    {
        if (!_design.HasSea) return;

        var seaMat = new StandardMaterial3D();
        seaMat.AlbedoColor = _design.SeaColor;
        seaMat.SpecularMode = BaseMaterial3D.SpecularModeEnum.Disabled;
        seaMat.Metallic = 0.1f;

        _sea = new Node3D();
        var water = MeshKit.Box(null, new Vector3(4000f, 0.1f, 4000f), seaMat);
        water.Position = new Vector3(_design.SeaSide * 2000f, 0f, 0f);
        _sea.AddChild(water);
        _parent.AddChild(_sea);

        var cliffMat = new StandardMaterial3D();
        // The bluffs are clay, not granite - they are the reason the light had to be moved
        // back from them in 1993.
        cliffMat.AlbedoColor = new Color(0.52f, 0.42f, 0.33f);
        cliffMat.AlbedoTexture = TextureKit.Dirt;
        cliffMat.Uv1Scale = new Vector3(20f, 20f, 1f);
        cliffMat.SpecularMode = BaseMaterial3D.SpecularModeEnum.Disabled;

        // The skirt stands at the GROUND's edge, not somewhere short of it. Put it inside the
        // ground ribbon and it is simply buried in grass, with 40 cm of grey lip showing
        // along the verge where the land was supposed to fall away.
        // Built one unit tall and scaled each frame to exactly the drop, rather than made
        // enormous and left to poke out of things. A fixed 400 m face is a wall beside the
        // road, not a coast: the land is only 14 m above the water by the finish and 89 m at
        // the start, and the face has to be whichever of those it currently is.
        _cliff = new Node3D();
        _cliffFace = MeshKit.Box(null, new Vector3(0.6f, 1f, 4000f), cliffMat);
        _cliffFace.Position = new Vector3(_design.SeaSide * _design.ShoreDistance, 0f, 0f);
        _cliff.AddChild(_cliffFace);
        _parent.AddChild(_cliff);
    }

    /// <summary>
    /// Keep the water and the cliff under the rider. The sea sits at the course's own sea
    /// level, which for a measured course is where the real water is once the same vertical
    /// exaggeration has been applied to the drop.
    /// </summary>
    private void UpdateSea()
    {
        if (_sea == null) return;
        float z = _terrain.ScrollOffset;
        _sea.Position = new Vector3(0f, _design.SeaLevel, 0f);
        if (_cliffFace != null)
        {
            // Span exactly ground to water, wherever the road currently is.
            float top = _terrain.HillAt(z) - 0.4f;
            float drop = Mathf.Max(1f, top - _design.SeaLevel);
            _cliffFace.Scale = new Vector3(1f, drop, 1f);
            // Follows the shore in and out with the ground it edges.
            _cliffFace.Position = new Vector3(
                _design.SeaSide * _design.ShoreAt(z), top - drop / 2f, 0f);
        }
    }

    private void CreateClouds()
    {
        var rng = new RandomNumberGenerator();
        rng.Seed = 77;

        for (int i = 0; i < 18; i++)
        {
            var cloud = new Node3D();
            float w = rng.RandfRange(8f, 22f);
            float h = 0.3f + rng.RandfRange(0f, 0.2f);
            float d = rng.RandfRange(4f, 12f);

            // Cloud is flat overlapping boxes — N64 style
            var cloudMat = new StandardMaterial3D();
            cloudMat.AlbedoColor = new Color(0.99f, 0.99f, 1f, 0.88f);
            cloudMat.Transparency = BaseMaterial3D.TransparencyEnum.Alpha;
            cloudMat.SpecularMode = BaseMaterial3D.SpecularModeEnum.Disabled;

            // Main body
            var main = MeshKit.Box(null, new Vector3(w, h, d), cloudMat);
            cloud.AddChild(main);

            // Flat puff on top
            for (int j = 0; j < 2; j++)
            {
                float px = rng.RandfRange(-w * 0.3f, w * 0.3f);
                float pz = rng.RandfRange(-d * 0.3f, d * 0.3f);
                float puffW = rng.RandfRange(3f, 7f);
                float puffH = rng.RandfRange(0.5f, 1.5f);
                var puff = MeshKit.Box(null, new Vector3(puffW, puffH, puffW * 0.6f), cloudMat, new Vector3(px, h * 0.5f + puffH * 0.3f, pz));
                cloud.AddChild(puff);
            }

            float bx = rng.RandfRange(-120f, 120f);
            float bz = rng.RandfRange(0f, Band);
            float bh = 38f + rng.RandfRange(0f, 30f);
            cloud.Position = new Vector3(bx, bh, bz);
            _parent.AddChild(cloud);
            Clouds.Add(new Cloud { Node = cloud, BaseX = bx, BaseZ = bz, Height = bh, Speed = 0.3f + rng.RandfRange(0f, 0.8f) });
        }
    }

    private void CreateFinishLine()
    {
        _finishLine = new Node3D();

        var postMat = new StandardMaterial3D();
        postMat.AlbedoColor = new Color(0.95f, 0.2f, 0.2f);

        var bannerMat = new StandardMaterial3D();
        bannerMat.AlbedoColor = new Color(1f, 1f, 0.98f);

        // Posts
        _finishLine.AddChild(MeshKit.Cylinder(null, 0.08f, 3.5f, postMat, new Vector3(-_design.RoadWidth / 2f - 0.5f, 1.75f, 0), segments: 6));
        _finishLine.AddChild(MeshKit.Cylinder(null, 0.08f, 3.5f, postMat, new Vector3(_design.RoadWidth / 2f + 0.5f, 1.75f, 0), segments: 6));

        // Banner
        _finishLine.AddChild(MeshKit.Box(null, new Vector3(_design.RoadWidth + 1.5f, 0.7f, 0.06f), bannerMat, new Vector3(0, 3.2f, 0)));

        // Checkered pattern
        for (int i = 0; i < 14; i++)
        {
            float x = -_design.RoadWidth / 2f + 0.2f + i * (_design.RoadWidth / 14f);
            var checkMat = new StandardMaterial3D();
            checkMat.AlbedoColor = i % 2 == 0 ? new Color(0.12f, 0.12f, 0.12f) : new Color(0.95f, 0.2f, 0.2f);
            _finishLine.AddChild(MeshKit.Box(null, new Vector3(_design.RoadWidth / 14f - 0.04f, 0.18f, 0.07f), checkMat, new Vector3(x, 2.85f, 0)));
        }

        // "FINISH" text area (white box)
        var textMat = new StandardMaterial3D();
        textMat.AlbedoColor = new Color(0.98f, 0.98f, 0.95f);
        _finishLine.AddChild(MeshKit.Box(null, new Vector3(2f, 0.3f, 0.07f), textMat, new Vector3(0, 3.6f, 0)));

        _parent.AddChild(_finishLine);
    }

    private void CreateBirds()
    {
        var rng = new RandomNumberGenerator();
        rng.Seed = 456;
        var birdMat = new StandardMaterial3D();
        birdMat.AlbedoColor = new Color(0.15f, 0.12f, 0.1f);
        birdMat.SpecularMode = BaseMaterial3D.SpecularModeEnum.Disabled;

        for (int i = 0; i < 8; i++)
        {
            var bird = new Node3D();
            var body = new MeshInstance3D();
            var bodyMesh = new SphereMesh();
            bodyMesh.Radius = 0.06f;
            bodyMesh.Height = 0.12f;
            body.Mesh = bodyMesh;
            body.MaterialOverride = birdMat;
            bird.AddChild(body);

            var wingMesh = new BoxMesh();
            wingMesh.Size = new Vector3(0.2f, 0.01f, 0.08f);

            var wingL = new MeshInstance3D();
            wingL.Mesh = wingMesh;
            wingL.MaterialOverride = birdMat;
            wingL.Position = new Vector3(-0.1f, 0.02f, 0);
            bird.AddChild(wingL);

            var wingR = new MeshInstance3D();
            wingR.Mesh = wingMesh;
            wingR.MaterialOverride = birdMat;
            wingR.Position = new Vector3(0.1f, 0.02f, 0);
            bird.AddChild(wingR);

            float bx = rng.RandfRange(-30f, 30f);
            float by = 15f + rng.RandfRange(0f, 20f);
            float bz = rng.RandfRange(0f, Band);
            bird.Position = new Vector3(bx, by, bz);
            _parent.AddChild(bird);
            Birds.Add(new Bird { Node = bird, BaseX = bx, BaseY = by, BaseZ = bz, Speed = 3f + rng.RandfRange(0f, 5f), Phase = rng.RandfRange(0, 6f) });
        }
    }

    private void CreateSquirrels()
    {
        var rng = new RandomNumberGenerator();
        rng.Seed = 789;
        var squirrelMat = new StandardMaterial3D();
        squirrelMat.AlbedoColor = new Color(0.55f, 0.35f, 0.15f);
        squirrelMat.SpecularMode = BaseMaterial3D.SpecularModeEnum.Disabled;

        for (int i = 0; i < 6; i++)
        {
            var squirrel = new Node3D();
            // Body
            var body = new MeshInstance3D();
            var bodyMesh = new SphereMesh();
            bodyMesh.Radius = 0.08f;
            bodyMesh.Height = 0.16f;
            body.Mesh = bodyMesh;
            body.MaterialOverride = squirrelMat;
            squirrel.AddChild(body);

            // Head
            var head = new MeshInstance3D();
            var headMesh = new SphereMesh();
            headMesh.Radius = 0.05f;
            headMesh.Height = 0.1f;
            head.Mesh = headMesh;
            head.MaterialOverride = squirrelMat;
            head.Position = new Vector3(0, 0.06f, 0.06f);
            squirrel.AddChild(head);

            // Tail
            var tail = new MeshInstance3D();
            var tailMesh = new CylinderMesh();
            tailMesh.TopRadius = 0.01f;
            tailMesh.BottomRadius = 0.04f;
            tailMesh.Height = 0.15f;
            tail.Mesh = tailMesh;
            tail.MaterialOverride = squirrelMat;
            tail.Position = new Vector3(0, 0.08f, -0.1f);
            tail.Rotation = new Vector3(-0.5f, 0, 0);
            squirrel.AddChild(tail);

            float sz = rng.RandfRange(0f, Band);
            float side = rng.Randf() > 0.5f ? 1f : -1f;
            float offset = side * (5f + rng.RandfRange(0f, 3f));
            squirrel.Position = new Vector3(offset, 0.1f, sz);
            _parent.AddChild(squirrel);
            Squirrels.Add(new Squirrel { Node = squirrel, WorldZ = sz, OffsetX = offset, Phase = rng.RandfRange(0, 6f) });
        }
    }

    private void CreateSunDisc()
    {
        var sunMat = new StandardMaterial3D();
        sunMat.AlbedoColor = new Color(1f, 1f, 1f);
        sunMat.EmissionEnabled = true;
        sunMat.Emission = new Color(1f, 0.95f, 0.8f);
        sunMat.EmissionEnergyMultiplier = 4f;

        var sun = new MeshInstance3D();
        var sunMesh = new SphereMesh();
        sunMesh.Radius = 6f;
        sunMesh.Height = 12f;
        sun.Mesh = sunMesh;
        sun.MaterialOverride = sunMat;
        sun.Position = new Vector3(40f, 75f, 180f);
        _parent.AddChild(sun);
    }

    private void CreateButterflies()
    {
        var rng = new RandomNumberGenerator();
        rng.Seed = 123;
        var colors = new[] {
            new Color(0.98f, 0.88f, 0.28f),  // yellow
            new Color(0.98f, 0.48f, 0.68f),  // pink
            new Color(0.78f, 0.9f, 1f),      // blue
            new Color(1f, 1f, 0.98f),         // white
        };

        for (int i = 0; i < 15; i++)
        {
            var bf = new Node3D();
            var wingMat = new StandardMaterial3D();
            wingMat.AlbedoColor = colors[rng.RandiRange(0, 3)];

            // Two wings
            var wingL = new MeshInstance3D();
            var wMesh = new SphereMesh();
            wMesh.Radius = 0.04f;
            wMesh.Height = 0.02f;
            wingL.Mesh = wMesh;
            wingL.MaterialOverride = wingMat;
            wingL.Position = new Vector3(-0.03f, 0, 0);
            bf.AddChild(wingL);

            var wingR = new MeshInstance3D();
            wingR.Mesh = wMesh;
            wingR.MaterialOverride = wingMat;
            wingR.Position = new Vector3(0.03f, 0, 0);
            bf.AddChild(wingR);

            float bx = rng.RandfRange(-8f, 8f);
            float by = 1f + rng.RandfRange(0f, 3f);
            float bz = rng.RandfRange(0f, Band);
            bf.Position = new Vector3(bx, by, bz);
            _parent.AddChild(bf);
            Butterflies.Add(new Butterfly { Node = bf, BaseX = bx, BaseY = by, BaseZ = bz, Phase = rng.RandfRange(0, 6f) });
        }
    }

    public void UpdateAll(float dt)
    {
        UpdatePositions(_terrain.ScrollOffset);
        UpdateClouds(dt);
        UpdateFinishLine();
        UpdateRidges();
        UpdateBridge();
        UpdateSea();
        UpdateButterflies(dt);
        UpdateBirds(dt);
        UpdateSquirrels(dt);
    }

    public void SetItemsVisible(bool visible)
    {
        foreach (var item in Items)
            item.Node.Visible = visible;
        foreach (var cloud in Clouds)
            cloud.Node.Visible = visible;
        foreach (var bf in Butterflies)
            bf.Node.Visible = visible;
        foreach (var bird in Birds)
            bird.Node.Visible = visible;
        foreach (var sq in Squirrels)
            sq.Node.Visible = visible;
        foreach (var r in _ridges)
            r.Visible = visible;
        if (_bridge != null) _bridge.Visible = visible;
        _finishLine.Visible = visible;
    }

    public void UpdatePositions(float scrollOffset)
    {
        var terrain = _terrain;
        foreach (var item in Items)
        {
            float rel = item.Absolute
                ? item.WorldZ - scrollOffset
                : WrapRel(item.WorldZ, scrollOffset);

            // Where the prop actually is after wrapping. Height and curve have to come from
            // this, not from item.WorldZ — a recycled tree has to stand on the hill it lands
            // on, not the one it was first placed on.
            float worldZ = scrollOffset + rel;
            float cx = terrain.CurveAt(worldZ) * rel;
            float cy = terrain.HillAt(worldZ) - 0.4f;
            item.Node.Position = new Vector3(item.OffsetX + cx, cy, rel);
        }
    }

    public void UpdateClouds(float dt)
    {
        var terrain = _terrain;
        for (int i = 0; i < Clouds.Count; i++)
        {
            var c = Clouds[i];
            c.BaseX += c.Speed * dt;
            if (c.BaseX > 150f) c.BaseX -= 300f;
            float relZ = WrapRel(c.BaseZ, terrain.ScrollOffset);
            c.Node.Position = new Vector3(c.BaseX, c.Height, relZ);
        }
    }

    private void UpdateFinishLine()
    {
        var terrain = _terrain;
        float relZ = _design.Length - terrain.ScrollOffset;
        float cx = terrain.CurveAt(_design.Length) * relZ;
        float cy = terrain.HillAt(_design.Length);
        _finishLine.Position = new Vector3(cx, cy, relZ);
    }

    private void UpdateButterflies(float dt)
    {
        var terrain = _terrain;
        float time = (float)Time.GetTicksMsec() * 0.001f;
        for (int i = 0; i < Butterflies.Count; i++)
        {
            var bf = Butterflies[i];
            float x = bf.BaseX + Mathf.Sin(time * 0.8f + bf.Phase) * 2f;
            float y = bf.BaseY + Mathf.Sin(time * 1.2f + bf.Phase * 1.5f) * 0.5f;
            float relZ = WrapRel(bf.BaseZ, terrain.ScrollOffset);
            bf.Node.Position = new Vector3(x, y, relZ);
            bf.Node.Rotation = new Vector3(0, Mathf.Sin(time * 8f + bf.Phase) * 0.3f, 0);
        }
    }

    private void UpdateBirds(float dt)
    {
        var terrain = _terrain;
        float time = (float)Time.GetTicksMsec() * 0.001f;
        for (int i = 0; i < Birds.Count; i++)
        {
            var bird = Birds[i];
            float x = bird.BaseX + Mathf.Sin(time * 0.3f + bird.Phase) * 15f;
            float y = bird.BaseY + Mathf.Sin(time * 0.5f + bird.Phase) * 2f;
            float relZ = WrapRel(bird.BaseZ + time * bird.Speed, terrain.ScrollOffset);
            bird.Node.Position = new Vector3(x, y, relZ);
            bird.Node.Rotation = new Vector3(Mathf.Sin(time * 6f + bird.Phase) * 0.4f, 0, 0);
        }
    }

    private void UpdateSquirrels(float dt)
    {
        var terrain = _terrain;
        float time = (float)Time.GetTicksMsec() * 0.001f;
        for (int i = 0; i < Squirrels.Count; i++)
        {
            var sq = Squirrels[i];
            float relZ = WrapRel(sq.WorldZ, terrain.ScrollOffset);
            float worldZ = terrain.ScrollOffset + relZ;
            float cx = terrain.CurveAt(worldZ) * relZ;
            float cy = terrain.HillAt(worldZ) - 0.3f;
            // Squirrels twitch and look around
            float twitch = Mathf.Sin(time * 3f + sq.Phase) * 0.1f;
            sq.Node.Position = new Vector3(sq.OffsetX + cx, cy, relZ);
            sq.Node.Rotation = new Vector3(0, twitch, 0);
        }
    }

    // ── Helpers ──────────────────────────────────
    // Geometry comes from MeshKit; props are built as loose nodes (null parent) and
    // handed to AddItem once assembled.

    /// <summary>Parent a finished prop under Main and record it so UpdatePositions can scroll it.</summary>
    private void AddItem(float z, float offset, Node3D node, Vector3 rotation = default)
    {
        node.Rotation = rotation;
        _parent.AddChild(node);
        Items.Add(new SceneryItem { Node = node, WorldZ = z, OffsetX = offset });
    }
}
