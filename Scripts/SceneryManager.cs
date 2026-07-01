using Godot;
using System.Collections.Generic;

public class SceneryManager
{
    private Main _main;

    public struct SceneryItem { public Node3D Node; public float WorldZ; public float OffsetX; }
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

    public SceneryManager(Main main)
    {
        _main = main;
    }

    public void Create()
    {
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
        rng.Seed = 42;
        var terrain = _main.Terrain;

        // Pine trees (close)
        for (int i = 0; i < 120; i++)
        {
            float z = rng.RandfRange(-60f, 900f);
            float side = rng.Randf() > 0.5f ? 1f : -1f;
            float offset = side * (5.5f + rng.RandfRange(0f, 12f));
            float h = 5f + rng.RandfRange(0f, 7f);
            AddTree(z, offset, h, true, rng);
        }

        // Pine trees (far)
        for (int i = 0; i < 50; i++)
        {
            float z = rng.RandfRange(-80f, 900f);
            float side = rng.Randf() > 0.5f ? 1f : -1f;
            float offset = side * (18f + rng.RandfRange(0f, 25f));
            float h = 8f + rng.RandfRange(0f, 10f);
            AddTree(z, offset, h, true, rng);
        }

        // Deciduous
        for (int i = 0; i < 40; i++)
        {
            float z = rng.RandfRange(-80f, 900f);
            float side = rng.Randf() > 0.5f ? 1f : -1f;
            float offset = side * (7f + rng.RandfRange(0f, 18f));
            float h = 5f + rng.RandfRange(0f, 5f);
            AddTree(z, offset, h, false, rng);
        }

        // Rocks (roadside) — smoother spheres
        for (int i = 0; i < 20; i++)
        {
            float z = rng.RandfRange(-50f, 800f);
            float side = rng.Randf() > 0.5f ? 1f : -1f;
            float offset = side * (4.5f + rng.RandfRange(0f, 4f));
            float size = 0.12f + rng.RandfRange(0f, 0.3f);
            var rockMat = new StandardMaterial3D();
            rockMat.AlbedoColor = new Color(0.58f + rng.RandfRange(0, 0.04f), 0.56f + rng.RandfRange(0, 0.03f), 0.53f + rng.RandfRange(0, 0.02f));
            rockMat.Roughness = 0.95f;
            rockMat.SpecularMode = BaseMaterial3D.SpecularModeEnum.Disabled;
            var rock = MakeSphereWithMat(size, rockMat);
            rock.Scale = new Vector3(1f, 0.6f + rng.RandfRange(0, 0.2f), 1f);
            rock.Rotation = new Vector3(rng.RandfRange(0, 0.3f), rng.RandfRange(0, 3f), 0);
            AddItem(z, offset, rock);
        }

        // Stumps
        for (int i = 0; i < 15; i++)
        {
            float z = rng.RandfRange(-30f, 700f);
            float side = rng.Randf() > 0.5f ? 1f : -1f;
            float offset = side * (5f + rng.RandfRange(0f, 6f));
            AddItem(z, offset, MakeCylinder(0.15f, 0.15f + rng.RandfRange(0f, 0.2f), new Color(0.35f, 0.22f, 0.1f)));
        }

        // Wildflowers — more variety and density
        for (int i = 0; i < 120; i++)
        {
            float z = rng.RandfRange(-10f, 800f);
            float side = rng.Randf() > 0.5f ? 1f : -1f;
            float offset = side * (4.2f + rng.RandfRange(0f, 3f));
            var col = new[] {
                new Color(0.98f, 0.92f, 0.28f), new Color(0.98f, 0.48f, 0.58f),
                new Color(0.88f, 0.88f, 0.92f), new Color(0.68f, 0.48f, 0.88f),
                new Color(1f, 0.68f, 0.28f)
            }[rng.RandiRange(0, 4)];
            AddItem(z, offset, MakeSphere(0.04f + rng.RandfRange(0, 0.02f), col));
        }

        // Ferns — low green fronds
        for (int i = 0; i < 30; i++)
        {
            float z = rng.RandfRange(-20f, 700f);
            float side = rng.Randf() > 0.5f ? 1f : -1f;
            float offset = side * (4.5f + rng.RandfRange(0f, 3f));
            var fernMat = new StandardMaterial3D();
            fernMat.AlbedoColor = new Color(0.15f + rng.RandfRange(0, 0.06f), 0.45f + rng.RandfRange(0, 0.1f), 0.1f);
            fernMat.Roughness = 0.9f;
            fernMat.SpecularMode = BaseMaterial3D.SpecularModeEnum.Disabled;
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
        for (int i = 0; i < 40; i++)
        {
            float z = rng.RandfRange(-20f, 800f);
            float side = rng.Randf() > 0.5f ? 1f : -1f;
            float offset = side * (4.5f + rng.RandfRange(0f, 4f));
            float size = 0.15f + rng.RandfRange(0f, 0.25f);
            var bushMat = new StandardMaterial3D();
            bushMat.AlbedoColor = new Color(0.15f + rng.RandfRange(0, 0.04f), 0.38f + rng.RandfRange(0, 0.05f), 0.1f + rng.RandfRange(0, 0.02f));
            bushMat.Roughness = 0.92f;
            bushMat.SpecularMode = BaseMaterial3D.SpecularModeEnum.Disabled;

            var bush = new Node3D();
            bush.AddChild(MakeSphereWithMat(size, bushMat));
            var puff = MakeSphereWithMat(size * 0.7f, bushMat);
            puff.Position = new Vector3(size * 0.3f, size * 0.2f, 0);
            bush.AddChild(puff);
            AddItem(z, offset, bush);
        }

        // Logs
        for (int i = 0; i < 10; i++)
        {
            float z = rng.RandfRange(50f, 700f);
            float side = rng.Randf() > 0.5f ? 1f : -1f;
            float offset = side * (5f + rng.RandfRange(0f, 5f));
            float length = 0.8f + rng.RandfRange(0f, 1.2f);
            var log = MakeCylinder(0.06f, length, new Color(0.3f, 0.2f, 0.1f));
            log.Rotation = new Vector3(0, rng.RandfRange(0, Mathf.Pi), Mathf.Pi / 2f);
            AddItem(z, offset, log);
        }

        // Mailboxes
        AddMailbox(60f, 1f);
        AddMailbox(350f, -1f);
        AddMailbox(700f, 1f);

        // Guard rails
        for (int i = 0; i < 25; i++)
        {
            float z = 100f + i * 60f + rng.RandfRange(0f, 20f);
            float side = rng.Randf() > 0.5f ? 1f : -1f;
            float offset = side * (4.2f + rng.RandfRange(0f, 0.5f));
            AddGuardRail(z, offset);
        }

        // Road signs
        for (int i = 0; i < 8; i++)
        {
            float z = 80f + i * 220f + rng.RandfRange(0f, 40f);
            float side = rng.Randf() > 0.5f ? 1f : -1f;
            float offset = side * (5f + rng.RandfRange(0f, 2f));
            AddRoadSign(z, offset, rng);
        }

        // Houses
        for (int i = 0; i < 8; i++)
        {
            float z = 150f + i * 200f + rng.RandfRange(0f, 50f);
            float side = rng.Randf() > 0.5f ? 1f : -1f;
            float offset = side * (12f + rng.RandfRange(0f, 8f));
            AddHouse(z, offset, rng);
        }

        // Bridge
        AddBridge(800f);

        // Streams
        for (int i = 0; i < 3; i++)
        {
            float z = 250f + i * 400f + rng.RandfRange(0f, 100f);
            float side = rng.Randf() > 0.5f ? 1f : -1f;
            float offset = side * (8f + rng.RandfRange(0f, 5f));
            AddStream(z, offset, rng);
        }

        // Distance markers
        for (float mz = 500f; mz < Main.CourseLength; mz += 500f)
            AddDistanceMarker(mz);

        UpdatePositions(0f);
    }

    private void AddTree(float z, float offset, float h, bool isPine, RandomNumberGenerator rng)
    {
        var tree = new Node3D();

        // Trunk
        var trunkMat = new StandardMaterial3D();
        trunkMat.AlbedoColor = new Color(0.42f, 0.28f, 0.16f);
        trunkMat.Roughness = 0.88f;
        trunkMat.SpecularMode = BaseMaterial3D.SpecularModeEnum.Disabled;
        var trunk = MakeCylinderWithMat(0.04f, h * 0.45f, trunkMat);
        trunk.Position = new Vector3(0, h * 0.22f, 0);
        tree.AddChild(trunk);

        if (isPine)
        {
            // Pine: dark green layers
            for (int j = 0; j < 4; j++)
            {
                float t = j / 4f;
                float lh = h * 0.22f;
                float lr = (1f - t * 0.3f) * h * 0.22f;
                float green = 0.28f + rng.RandfRange(0, 0.06f);
                var folMat = new StandardMaterial3D();
                folMat.AlbedoColor = new Color(0.06f + rng.RandfRange(0, 0.03f), green, 0.04f + rng.RandfRange(0, 0.02f));
                folMat.Roughness = 0.9f;
                folMat.SpecularMode = BaseMaterial3D.SpecularModeEnum.Disabled;
                var foliage = MakeCylinderWithMat(lr, lh, folMat);
                foliage.Position = new Vector3(0, h * 0.3f + j * lh * 0.52f, 0);
                tree.AddChild(foliage);
            }
        }
        else
        {
            // Deciduous: bright summer green, multiple spheres
            float canopyR = h * 0.24f;
            var leafMat = new StandardMaterial3D();
            leafMat.AlbedoColor = new Color(0.18f + rng.RandfRange(0, 0.08f), 0.5f + rng.RandfRange(0, 0.08f), 0.1f + rng.RandfRange(0, 0.03f));
            leafMat.Roughness = 0.88f;
            leafMat.SpecularMode = BaseMaterial3D.SpecularModeEnum.Disabled;

            var main = MakeSphereWithMat(canopyR, leafMat);
            main.Position = new Vector3(0, h * 0.62f, 0);
            tree.AddChild(main);

            var left = MakeSphereWithMat(canopyR * 0.7f, leafMat);
            left.Position = new Vector3(-canopyR * 0.4f, h * 0.55f, canopyR * 0.2f);
            tree.AddChild(left);

            var right = MakeSphereWithMat(canopyR * 0.65f, leafMat);
            right.Position = new Vector3(canopyR * 0.35f, h * 0.58f, -canopyR * 0.15f);
            tree.AddChild(right);
        }

        _main.AddChild(tree);
        Items.Add(new SceneryItem { Node = tree, WorldZ = z, OffsetX = offset });
    }

    private void AddMailbox(float z, float side)
    {
        var box = new Node3D();

        var postMat = new StandardMaterial3D();
        postMat.AlbedoColor = new Color(0.38f, 0.24f, 0.12f);
        postMat.Roughness = 0.85f;

        var mailMat = new StandardMaterial3D();
        mailMat.AlbedoColor = new Color(0.28f, 0.28f, 0.78f);
        mailMat.Roughness = 0.5f;

        var flagMat = new StandardMaterial3D();
        flagMat.AlbedoColor = new Color(0.92f, 0.18f, 0.18f);
        flagMat.Roughness = 0.4f;

        // Post
        box.AddChild(MakeCylinderWithMat(0.035f, 0.9f, postMat, new Vector3(0, 0.45f, 0)));
        // Mailbox body
        box.AddChild(MakeBoxWithMat(new Vector3(0.22f, 0.18f, 0.35f), mailMat, new Vector3(0, 0.92f, 0)));
        // Flag
        box.AddChild(MakeBoxWithMat(new Vector3(0.025f, 0.14f, 0.025f), flagMat, new Vector3(0.13f, 0.98f, 0)));

        _main.AddChild(box);
        Items.Add(new SceneryItem { Node = box, WorldZ = z, OffsetX = side * 5f });
    }

    private void AddGuardRail(float z, float offset)
    {
        var rail = new Node3D();
        var postMat = new StandardMaterial3D();
        postMat.AlbedoColor = new Color(0.68f, 0.68f, 0.72f);
        postMat.Metallic = 0.25f;
        postMat.Roughness = 0.45f;

        var barMat = new StandardMaterial3D();
        barMat.AlbedoColor = new Color(0.68f, 0.68f, 0.72f);
        barMat.Metallic = 0.35f;
        barMat.Roughness = 0.35f;

        for (int i = 0; i < 3; i++)
        {
            var post = MakeCylinderWithMat(0.025f, 0.7f, postMat);
            post.Position = new Vector3(0, 0.35f, i * 2f);
            rail.AddChild(post);
        }
        var bar = MakeBoxWithMat(new Vector3(0.04f, 0.04f, 5f), barMat, new Vector3(0, 0.55f, 2.5f));
        rail.AddChild(bar);
        _main.AddChild(rail);
        Items.Add(new SceneryItem { Node = rail, WorldZ = z, OffsetX = offset });
    }

    private void AddRoadSign(float z, float offset, RandomNumberGenerator rng)
    {
        var sign = new Node3D();
        var postMat = new StandardMaterial3D();
        postMat.AlbedoColor = new Color(0.55f, 0.55f, 0.58f);
        postMat.Metallic = 0.2f;

        var signColors = new[] {
            new Color(1f, 0.9f, 0.2f),      // yellow warning
            new Color(0.3f, 0.6f, 1f),       // blue info
            new Color(0.98f, 0.3f, 0.2f),    // red stop
        };
        var signMat = new StandardMaterial3D();
        signMat.AlbedoColor = signColors[rng.RandiRange(0, 2)];
        signMat.Roughness = 0.6f;

        sign.AddChild(MakeCylinderWithMat(0.03f, 1.5f, postMat, new Vector3(0, 0.75f, 0)));
        sign.AddChild(MakeBoxWithMat(new Vector3(0.55f, 0.45f, 0.04f), signMat, new Vector3(0, 1.65f, 0)));

        _main.AddChild(sign);
        Items.Add(new SceneryItem { Node = sign, WorldZ = z, OffsetX = offset });
    }

    private void AddHouse(float z, float offset, RandomNumberGenerator rng)
    {
        var house = new Node3D();
        var wallColors = new[] {
            new Color(0.96f, 0.94f, 0.88f),  // white clapboard
            new Color(0.92f, 0.82f, 0.68f),   // cream
            new Color(0.78f, 0.9f, 0.82f),    // sage green
            new Color(0.88f, 0.78f, 0.72f),   // beige
        };
        var roofColors = new[] {
            new Color(0.48f, 0.18f, 0.14f),   // dark red shingles
            new Color(0.32f, 0.26f, 0.22f),   // dark brown
            new Color(0.42f, 0.42f, 0.45f),   // grey
        };
        var w = wallColors[rng.RandiRange(0, 3)];
        var r = roofColors[rng.RandiRange(0, 2)];

        // Materials
        var wallMat = new StandardMaterial3D();
        wallMat.AlbedoColor = w;
        wallMat.Roughness = 0.85f;
        wallMat.SpecularMode = BaseMaterial3D.SpecularModeEnum.Disabled;

        var roofMat = new StandardMaterial3D();
        roofMat.AlbedoColor = r;
        roofMat.Roughness = 0.9f;
        roofMat.SpecularMode = BaseMaterial3D.SpecularModeEnum.Disabled;

        var doorMat = new StandardMaterial3D();
        doorMat.AlbedoColor = new Color(0.45f, 0.28f, 0.16f);
        doorMat.Roughness = 0.65f;

        var winMat = new StandardMaterial3D();
        winMat.AlbedoColor = new Color(0.78f, 0.9f, 1f, 0.9f);
        winMat.Transparency = BaseMaterial3D.TransparencyEnum.Alpha;
        winMat.EmissionEnabled = true;
        winMat.Emission = new Color(0.1f, 0.08f, 0.04f);

        // Main body
        AddBoxTo(house, new Vector3(2.5f, 1.8f, 2f), wallMat, new Vector3(0, 0.9f, 0));
        // Roof
        AddBoxTo(house, new Vector3(2.8f, 0.15f, 2.3f), roofMat, new Vector3(0, 1.85f, 0));
        AddBoxTo(house, new Vector3(2.8f, 0.6f, 0.12f), roofMat, new Vector3(0, 2.15f, 0));
        // Door
        AddBoxTo(house, new Vector3(0.42f, 0.85f, 0.06f), doorMat, new Vector3(0, 0.42f, 1.02f));
        // Door handle
        var handleMat = new StandardMaterial3D();
        handleMat.AlbedoColor = new Color(0.7f, 0.65f, 0.3f);
        handleMat.Metallic = 0.4f;
        AddBoxTo(house, new Vector3(0.04f, 0.04f, 0.04f), handleMat, new Vector3(0.12f, 0.45f, 1.06f));
        // Windows
        foreach (var wp in new[] { new Vector3(-0.7f, 1.15f, 1.02f), new Vector3(0.7f, 1.15f, 1.02f) })
        {
            AddBoxTo(house, new Vector3(0.42f, 0.42f, 0.03f), winMat, wp);
            // Window frame
            var frameMat = new StandardMaterial3D();
            frameMat.AlbedoColor = new Color(0.9f, 0.88f, 0.8f);
            AddBoxTo(house, new Vector3(0.48f, 0.03f, 0.04f), frameMat, wp + new Vector3(0, 0.22f, 0.01f));
            AddBoxTo(house, new Vector3(0.48f, 0.03f, 0.04f), frameMat, wp + new Vector3(0, -0.22f, 0.01f));
        }

        _main.AddChild(house);
        Items.Add(new SceneryItem { Node = house, WorldZ = z, OffsetX = offset });
    }

    private void AddBridge(float z)
    {
        var bridge = new Node3D();
        var woodMat = new StandardMaterial3D();
        woodMat.AlbedoColor = new Color(0.48f, 0.38f, 0.28f);
        woodMat.Roughness = 0.85f;
        woodMat.SpecularMode = BaseMaterial3D.SpecularModeEnum.Disabled;

        var railMat = new StandardMaterial3D();
        railMat.AlbedoColor = new Color(0.62f, 0.62f, 0.65f);
        railMat.Metallic = 0.28f;
        railMat.Roughness = 0.42f;

        // Bridge deck (wooden planks)
        AddBoxTo(bridge, new Vector3(TerrainManager.RoadW + 1f, 0.12f, 6f), woodMat, new Vector3(0, -0.1f, 3f));
        // Plank lines
        for (int i = 0; i < 6; i++)
        {
            float zOff = i * 1f;
            AddBoxTo(bridge, new Vector3(TerrainManager.RoadW + 0.8f, 0.01f, 0.04f), woodMat, new Vector3(0, -0.04f, zOff));
        }

        // Rails on both sides
        for (float side = -1f; side <= 1f; side += 2f)
        {
            for (int i = 0; i < 4; i++)
            {
                var post = MakeCylinderWithMat(0.03f, 1f, railMat);
                post.Position = new Vector3(side * (TerrainManager.RoadW / 2f + 0.3f), 0.5f, i * 1.5f);
                bridge.AddChild(post);
            }
            var bar = MakeBoxWithMat(new Vector3(0.04f, 0.04f, 5.5f), railMat, new Vector3(side * (TerrainManager.RoadW / 2f + 0.3f), 0.8f, 2.5f));
            bridge.AddChild(bar);
        }

        _main.AddChild(bridge);
        Items.Add(new SceneryItem { Node = bridge, WorldZ = z, OffsetX = 0f });
    }

    private void AddStream(float z, float offset, RandomNumberGenerator rng)
    {
        var stream = new Node3D();

        // Water surface with transparency and emission
        var waterMat = new StandardMaterial3D();
        waterMat.AlbedoColor = new Color(0.35f, 0.62f, 0.85f, 0.8f);
        waterMat.Transparency = BaseMaterial3D.TransparencyEnum.Alpha;
        waterMat.EmissionEnabled = true;
        waterMat.Emission = new Color(0.06f, 0.12f, 0.18f);
        waterMat.Roughness = 0.15f;
        waterMat.Metallic = 0.15f;

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
        rockMat.Roughness = 0.9f;
        for (int i = 0; i < 6; i++)
        {
            float angle = i * Mathf.Pi / 3f;
            float r = 1.8f + rng.RandfRange(0f, 0.5f);
            var rock = MakeSphereWithMat(0.15f + rng.RandfRange(0f, 0.1f), rockMat);
            rock.Position = new Vector3(Mathf.Cos(angle) * r, -0.05f, Mathf.Sin(angle) * r);
            stream.AddChild(rock);
        }

        _main.AddChild(stream);
        Items.Add(new SceneryItem { Node = stream, WorldZ = z, OffsetX = offset });
    }

    private void AddDistanceMarker(float z)
    {
        var marker = new Node3D();

        var postMat = new StandardMaterial3D();
        postMat.AlbedoColor = new Color(0.82f, 0.82f, 0.72f);
        postMat.Roughness = 0.7f;

        var signMat = new StandardMaterial3D();
        signMat.AlbedoColor = new Color(1f, 1f, 0.92f);
        signMat.EmissionEnabled = true;
        signMat.Emission = new Color(0.15f, 0.15f, 0.12f);

        // Post
        marker.AddChild(MakeCylinderWithMat(0.03f, 0.9f, postMat, new Vector3(0, 0.45f, 0)));
        // Sign
        marker.AddChild(MakeBoxWithMat(new Vector3(0.35f, 0.25f, 0.04f), signMat, new Vector3(0, 0.92f, 0)));

        _main.AddChild(marker);
        Items.Add(new SceneryItem { Node = marker, WorldZ = z, OffsetX = TerrainManager.RoadW / 2f + 0.5f });
    }

    private void CreateClouds()
    {
        var rng = new RandomNumberGenerator();
        rng.Seed = 77;

        for (int i = 0; i < 25; i++)
        {
            var cloud = new Node3D();
            float w = rng.RandfRange(8f, 22f);
            float h = 0.3f + rng.RandfRange(0f, 0.2f);
            float d = rng.RandfRange(4f, 12f);

            // Cloud is multiple overlapping boxes for fluffy look
            var cloudMat = new StandardMaterial3D();
            cloudMat.AlbedoColor = new Color(0.99f, 0.99f, 1f, 0.88f);
            cloudMat.Transparency = BaseMaterial3D.TransparencyEnum.Alpha;
            cloudMat.Roughness = 1f;
            cloudMat.SpecularMode = BaseMaterial3D.SpecularModeEnum.Disabled;

            // Main body
            var main = MakeBoxWithMat(new Vector3(w, h, d), cloudMat);
            cloud.AddChild(main);

            // Puffs on top
            for (int j = 0; j < 3; j++)
            {
                float px = rng.RandfRange(-w * 0.3f, w * 0.3f);
                float pz = rng.RandfRange(-d * 0.3f, d * 0.3f);
                float puffSize = rng.RandfRange(2f, 5f);
                var puff = MakeSphereWithMat(puffSize, cloudMat);
                puff.Position = new Vector3(px, h * 0.4f + puffSize * 0.3f, pz);
                cloud.AddChild(puff);
            }

            float bx = rng.RandfRange(-120f, 120f);
            float bz = rng.RandfRange(-60f, 500f);
            float bh = 38f + rng.RandfRange(0f, 30f);
            cloud.Position = new Vector3(bx, bh, bz);
            _main.AddChild(cloud);
            Clouds.Add(new Cloud { Node = cloud, BaseX = bx, BaseZ = bz, Height = bh, Speed = 0.3f + rng.RandfRange(0f, 0.8f) });
        }
    }

    private void CreateFinishLine()
    {
        _finishLine = new Node3D();

        var postMat = new StandardMaterial3D();
        postMat.AlbedoColor = new Color(0.92f, 0.18f, 0.18f);
        postMat.Roughness = 0.6f;

        var bannerMat = new StandardMaterial3D();
        bannerMat.AlbedoColor = new Color(1f, 1f, 0.98f);
        bannerMat.EmissionEnabled = true;
        bannerMat.Emission = new Color(0.25f, 0.25f, 0.18f);

        // Posts
        _finishLine.AddChild(MakeCylinderWithMat(0.08f, 3.5f, postMat, new Vector3(-TerrainManager.RoadW / 2f - 0.5f, 1.75f, 0)));
        _finishLine.AddChild(MakeCylinderWithMat(0.08f, 3.5f, postMat, new Vector3(TerrainManager.RoadW / 2f + 0.5f, 1.75f, 0)));

        // Banner
        _finishLine.AddChild(MakeBoxWithMat(new Vector3(TerrainManager.RoadW + 1.5f, 0.7f, 0.06f), bannerMat, new Vector3(0, 3.2f, 0)));

        // Checkered pattern
        for (int i = 0; i < 14; i++)
        {
            float x = -TerrainManager.RoadW / 2f + 0.2f + i * (TerrainManager.RoadW / 14f);
            var checkMat = new StandardMaterial3D();
            checkMat.AlbedoColor = i % 2 == 0 ? new Color(0.08f, 0.08f, 0.08f) : new Color(0.92f, 0.18f, 0.18f);
            _finishLine.AddChild(MakeBoxWithMat(new Vector3(TerrainManager.RoadW / 14f - 0.04f, 0.18f, 0.07f), checkMat, new Vector3(x, 2.85f, 0)));
        }

        // "FINISH" text area (white box)
        var textMat = new StandardMaterial3D();
        textMat.AlbedoColor = new Color(0.98f, 0.98f, 0.95f);
        textMat.EmissionEnabled = true;
        textMat.Emission = new Color(0.15f, 0.15f, 0.12f);
        _finishLine.AddChild(MakeBoxWithMat(new Vector3(2f, 0.3f, 0.07f), textMat, new Vector3(0, 3.6f, 0)));

        _main.AddChild(_finishLine);
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
            float bz = rng.RandfRange(-50f, 200f);
            bird.Position = new Vector3(bx, by, bz);
            _main.AddChild(bird);
            Birds.Add(new Bird { Node = bird, BaseX = bx, BaseY = by, BaseZ = bz, Speed = 3f + rng.RandfRange(0f, 5f), Phase = rng.RandfRange(0, 6f) });
        }
    }

    private void CreateSquirrels()
    {
        var rng = new RandomNumberGenerator();
        rng.Seed = 789;
        var squirrelMat = new StandardMaterial3D();
        squirrelMat.AlbedoColor = new Color(0.55f, 0.35f, 0.15f);
        squirrelMat.Roughness = 0.8f;
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

            float sz = rng.RandfRange(50f, 600f);
            float side = rng.Randf() > 0.5f ? 1f : -1f;
            float offset = side * (5f + rng.RandfRange(0f, 3f));
            squirrel.Position = new Vector3(offset, 0.1f, sz);
            _main.AddChild(squirrel);
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
        _main.AddChild(sun);
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
            wingMat.EmissionEnabled = true;
            wingMat.Emission = wingMat.AlbedoColor * 0.2f;

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
            float bz = rng.RandfRange(-20f, 100f);
            bf.Position = new Vector3(bx, by, bz);
            _main.AddChild(bf);
            Butterflies.Add(new Butterfly { Node = bf, BaseX = bx, BaseY = by, BaseZ = bz, Phase = rng.RandfRange(0, 6f) });
        }
    }

    public void UpdateAll(float dt)
    {
        UpdatePositions(_main.Terrain.ScrollOffset);
        UpdateClouds(dt);
        UpdateFinishLine();
        UpdateButterflies(dt);
        UpdateBirds(dt);
        UpdateSquirrels(dt);
    }

    public void UpdatePositions(float scrollOffset)
    {
        var terrain = _main.Terrain;
        foreach (var item in Items)
        {
            float relZ = item.WorldZ - scrollOffset;
            float cx = terrain.CurveAt(item.WorldZ) * relZ;
            float cy = terrain.HillAt(item.WorldZ) - 0.4f;
            item.Node.Position = new Vector3(item.OffsetX + cx, cy, relZ);
        }
    }

    public void UpdateClouds(float dt)
    {
        var terrain = _main.Terrain;
        for (int i = 0; i < Clouds.Count; i++)
        {
            var c = Clouds[i];
            c.BaseX += c.Speed * dt;
            if (c.BaseX > 150f) c.BaseX -= 300f;
            float relZ = c.BaseZ - terrain.ScrollOffset;
            c.Node.Position = new Vector3(c.BaseX, c.Height, relZ);
        }
    }

    private void UpdateFinishLine()
    {
        var terrain = _main.Terrain;
        float relZ = Main.CourseLength - terrain.ScrollOffset;
        float cx = terrain.CurveAt(Main.CourseLength) * relZ;
        float cy = terrain.HillAt(Main.CourseLength);
        _finishLine.Position = new Vector3(cx, cy, relZ);
    }

    private void UpdateButterflies(float dt)
    {
        var terrain = _main.Terrain;
        float time = (float)Time.GetTicksMsec() * 0.001f;
        for (int i = 0; i < Butterflies.Count; i++)
        {
            var bf = Butterflies[i];
            float x = bf.BaseX + Mathf.Sin(time * 0.8f + bf.Phase) * 2f;
            float y = bf.BaseY + Mathf.Sin(time * 1.2f + bf.Phase * 1.5f) * 0.5f;
            float relZ = bf.BaseZ - terrain.ScrollOffset;
            bf.Node.Position = new Vector3(x, y, relZ);
            bf.Node.Rotation = new Vector3(0, Mathf.Sin(time * 8f + bf.Phase) * 0.3f, 0);
        }
    }

    private void UpdateBirds(float dt)
    {
        var terrain = _main.Terrain;
        float time = (float)Time.GetTicksMsec() * 0.001f;
        for (int i = 0; i < Birds.Count; i++)
        {
            var bird = Birds[i];
            float x = bird.BaseX + Mathf.Sin(time * 0.3f + bird.Phase) * 15f;
            float y = bird.BaseY + Mathf.Sin(time * 0.5f + bird.Phase) * 2f;
            float relZ = bird.BaseZ - terrain.ScrollOffset + time * bird.Speed;
            if (relZ > 300f) relZ -= 400f;
            bird.Node.Position = new Vector3(x, y, relZ);
            bird.Node.Rotation = new Vector3(Mathf.Sin(time * 6f + bird.Phase) * 0.4f, 0, 0);
        }
    }

    private void UpdateSquirrels(float dt)
    {
        var terrain = _main.Terrain;
        float time = (float)Time.GetTicksMsec() * 0.001f;
        for (int i = 0; i < Squirrels.Count; i++)
        {
            var sq = Squirrels[i];
            float relZ = sq.WorldZ - terrain.ScrollOffset;
            float cx = terrain.CurveAt(sq.WorldZ) * relZ;
            float cy = terrain.HillAt(sq.WorldZ) - 0.3f;
            // Squirrels twitch and look around
            float twitch = Mathf.Sin(time * 3f + sq.Phase) * 0.1f;
            sq.Node.Position = new Vector3(sq.OffsetX + cx, cy, relZ);
            sq.Node.Rotation = new Vector3(0, twitch, 0);
        }
    }

    // Helpers
    private MeshInstance3D MakeBox(Vector3 size, Color color, Vector3 pos = default)
    {
        var m = new MeshInstance3D();
        var mesh = new BoxMesh();
        mesh.Size = size;
        m.Mesh = mesh;
        var mat = new StandardMaterial3D();
        mat.AlbedoColor = color;
        mat.SpecularMode = BaseMaterial3D.SpecularModeEnum.Disabled;
        m.MaterialOverride = mat;
        m.Position = pos;
        return m;
    }

    private MeshInstance3D MakeCylinder(float topR, float height, Color color, Vector3 pos = default)
    {
        var m = new MeshInstance3D();
        var mesh = new CylinderMesh();
        mesh.TopRadius = topR;
        mesh.BottomRadius = topR;
        mesh.Height = height;
        mesh.RadialSegments = 12;
        m.Mesh = mesh;
        var mat = new StandardMaterial3D();
        mat.AlbedoColor = color;
        mat.SpecularMode = BaseMaterial3D.SpecularModeEnum.Disabled;
        m.MaterialOverride = mat;
        m.Position = pos;
        return m;
    }

    private MeshInstance3D MakeSphere(float radius, Color color)
    {
        var m = new MeshInstance3D();
        var mesh = new SphereMesh();
        mesh.Radius = radius;
        mesh.Height = radius * 2f;
        mesh.Rings = 8;
        mesh.RadialSegments = 12;
        m.Mesh = mesh;
        var mat = new StandardMaterial3D();
        mat.AlbedoColor = color;
        mat.SpecularMode = BaseMaterial3D.SpecularModeEnum.Disabled;
        m.MaterialOverride = mat;
        return m;
    }

    private void AddItem(float z, float offset, Node3D node, Vector3 rotation = default)
    {
        node.Rotation = rotation;
        _main.AddChild(node);
        Items.Add(new SceneryItem { Node = node, WorldZ = z, OffsetX = offset });
    }

    private MeshInstance3D AddBoxTo(Node3D parent, Vector3 size, StandardMaterial3D mat, Vector3 pos)
    {
        var m = new MeshInstance3D();
        var mesh = new BoxMesh();
        mesh.Size = size;
        m.Mesh = mesh;
        m.MaterialOverride = mat;
        m.Position = pos;
        parent.AddChild(m);
        return m;
    }

    private MeshInstance3D MakeCylinderWithMat(float topR, float height, StandardMaterial3D mat, Vector3 pos = default)
    {
        var m = new MeshInstance3D();
        var mesh = new CylinderMesh();
        mesh.TopRadius = topR;
        mesh.BottomRadius = topR;
        mesh.Height = height;
        mesh.RadialSegments = 12;
        m.Mesh = mesh;
        m.MaterialOverride = mat;
        m.Position = pos;
        return m;
    }

    private MeshInstance3D MakeBoxWithMat(Vector3 size, StandardMaterial3D mat, Vector3 pos = default)
    {
        var m = new MeshInstance3D();
        var mesh = new BoxMesh();
        mesh.Size = size;
        m.Mesh = mesh;
        m.MaterialOverride = mat;
        m.Position = pos;
        return m;
    }

    private MeshInstance3D MakeSphereWithMat(float radius, StandardMaterial3D mat)
    {
        var m = new MeshInstance3D();
        var mesh = new SphereMesh();
        mesh.Radius = radius;
        mesh.Height = radius * 2f;
        mesh.Rings = 8;
        mesh.RadialSegments = 12;
        m.Mesh = mesh;
        m.MaterialOverride = mat;
        return m;
    }
}
