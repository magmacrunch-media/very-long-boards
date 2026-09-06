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

    private void AddBridge(float z)
    {
        var bridge = new Node3D();
        var woodMat = MeshKit.Mat(Colors.White, texture: TextureKit.Plank, uvScale: 4f);

        var railMat = new StandardMaterial3D();
        railMat.AlbedoColor = new Color(0.65f, 0.65f, 0.68f);

        // Bridge deck (wooden planks)
        MeshKit.Box(bridge, new Vector3(_design.RoadWidth + 1f, 0.12f, 6f), woodMat, new Vector3(0, -0.1f, 3f));
        // Plank lines
        for (int i = 0; i < 6; i++)
        {
            float zOff = i * 1f;
            MeshKit.Box(bridge, new Vector3(_design.RoadWidth + 0.8f, 0.01f, 0.04f), woodMat, new Vector3(0, -0.04f, zOff));
        }

        // Rails on both sides
        for (float side = -1f; side <= 1f; side += 2f)
        {
            for (int i = 0; i < 4; i++)
            {
                var post = MeshKit.Cylinder(null, 0.03f, 1f, railMat, segments: 6);
                post.Position = new Vector3(side * (_design.RoadWidth / 2f + 0.3f), 0.5f, i * 1.5f);
                bridge.AddChild(post);
            }
            var bar = MeshKit.Box(null, new Vector3(0.04f, 0.04f, 5.5f), railMat, new Vector3(side * (_design.RoadWidth / 2f + 0.3f), 0.8f, 2.5f));
            bridge.AddChild(bar);
        }

        _parent.AddChild(bridge);
        Items.Add(new SceneryItem { Node = bridge, WorldZ = z, OffsetX = 0f });
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
