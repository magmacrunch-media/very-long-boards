using Godot;
using System.Collections.Generic;

public class SceneryManager
{
    private Main _main;

    public struct SceneryItem { public Node3D Node; public float WorldZ; public float OffsetX; }
    public List<SceneryItem> Items = new List<SceneryItem>();

    public struct Cloud { public MeshInstance3D Node; public float BaseX; public float BaseZ; public float Height; public float Speed; }
    public List<Cloud> Clouds = new List<Cloud>();

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
    }

    private void CreateScenery()
    {
        var rng = new RandomNumberGenerator();
        rng.Seed = 42;
        var terrain = _main.Terrain;

        // Pine trees (close)
        for (int i = 0; i < 100; i++)
        {
            float z = rng.RandfRange(-80f, 900f);
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

        // Rocks
        for (int i = 0; i < 20; i++)
        {
            float z = rng.RandfRange(-50f, 800f);
            float side = rng.Randf() > 0.5f ? 1f : -1f;
            float offset = side * (4.5f + rng.RandfRange(0f, 4f));
            float size = 0.12f + rng.RandfRange(0f, 0.3f);
            AddItem(z, offset, MakeSphere(size, new Color(0.45f, 0.43f, 0.4f)),
                new Vector3(rng.RandfRange(0, 0.3f), rng.RandfRange(0, 3f), 0));
        }

        // Stumps
        for (int i = 0; i < 15; i++)
        {
            float z = rng.RandfRange(-30f, 700f);
            float side = rng.Randf() > 0.5f ? 1f : -1f;
            float offset = side * (5f + rng.RandfRange(0f, 6f));
            AddItem(z, offset, MakeCylinder(0.15f, 0.15f + rng.RandfRange(0f, 0.2f), new Color(0.35f, 0.22f, 0.1f)));
        }

        // Wildflowers
        for (int i = 0; i < 60; i++)
        {
            float z = rng.RandfRange(-30f, 800f);
            float side = rng.Randf() > 0.5f ? 1f : -1f;
            float offset = side * (4.2f + rng.RandfRange(0f, 3f));
            var col = new[] {
                new Color(0.9f, 0.85f, 0.2f), new Color(0.9f, 0.4f, 0.5f),
                new Color(0.8f, 0.8f, 0.85f), new Color(0.6f, 0.4f, 0.8f)
            }[rng.RandiRange(0, 3)];
            AddItem(z, offset, MakeSphere(0.04f, col));
        }

        // Bushes
        for (int i = 0; i < 40; i++)
        {
            float z = rng.RandfRange(-20f, 800f);
            float side = rng.Randf() > 0.5f ? 1f : -1f;
            float offset = side * (4.5f + rng.RandfRange(0f, 4f));
            float size = 0.15f + rng.RandfRange(0f, 0.25f);
            var col = new Color(0.18f + rng.RandfRange(0, 0.08f), 0.42f + rng.RandfRange(0, 0.1f), 0.12f);
            AddItem(z, offset, MakeSphere(size, col));
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

        _main.AddChild(tree);
        Items.Add(new SceneryItem { Node = tree, WorldZ = z, OffsetX = offset });
    }

    private void AddMailbox(float z, float side)
    {
        var box = new Node3D();
        box.AddChild(MakeCylinder(0.03f, 0.8f, new Color(0.35f, 0.22f, 0.1f), new Vector3(0, 0.4f, 0)));
        box.AddChild(MakeBox(new Vector3(0.2f, 0.15f, 0.3f), new Color(0.2f, 0.2f, 0.7f), new Vector3(0, 0.85f, 0)));
        box.AddChild(MakeBox(new Vector3(0.02f, 0.12f, 0.02f), new Color(0.8f, 0.1f, 0.1f), new Vector3(0.12f, 0.9f, 0)));
        _main.AddChild(box);
        Items.Add(new SceneryItem { Node = box, WorldZ = z, OffsetX = side * 5f });
    }

    private void AddGuardRail(float z, float offset)
    {
        var rail = new Node3D();
        for (int i = 0; i < 3; i++)
        {
            var post = MakeCylinder(0.025f, 0.7f, new Color(0.6f, 0.6f, 0.6f));
            post.Position = new Vector3(0, 0.35f, i * 2f);
            rail.AddChild(post);
        }
        var bar = MakeBox(new Vector3(0.04f, 0.04f, 5f), new Color(0.65f, 0.65f, 0.65f), new Vector3(0, 0.55f, 2.5f));
        rail.AddChild(bar);
        _main.AddChild(rail);
        Items.Add(new SceneryItem { Node = rail, WorldZ = z, OffsetX = offset });
    }

    private void AddRoadSign(float z, float offset, RandomNumberGenerator rng)
    {
        var sign = new Node3D();
        sign.AddChild(MakeCylinder(0.03f, 1.5f, new Color(0.5f, 0.5f, 0.5f), new Vector3(0, 0.75f, 0)));
        var colors = new[] { new Color(0.9f, 0.8f, 0.1f), new Color(0.2f, 0.5f, 0.9f), new Color(0.85f, 0.2f, 0.1f) };
        sign.AddChild(MakeBox(new Vector3(0.5f, 0.4f, 0.04f), colors[rng.RandiRange(0, 2)], new Vector3(0, 1.6f, 0)));
        _main.AddChild(sign);
        Items.Add(new SceneryItem { Node = sign, WorldZ = z, OffsetX = offset });
    }

    private void AddHouse(float z, float offset, RandomNumberGenerator rng)
    {
        var house = new Node3D();
        var walls = new[] { new Color(0.9f, 0.88f, 0.8f), new Color(0.85f, 0.75f, 0.6f), new Color(0.7f, 0.85f, 0.75f) };
        var roofs = new[] { new Color(0.4f, 0.12f, 0.1f), new Color(0.25f, 0.2f, 0.15f) };
        var w = walls[rng.RandiRange(0, 2)];
        var r = roofs[rng.RandiRange(0, 1)];

        house.AddChild(MakeBox(new Vector3(2.5f, 1.8f, 2f), w, new Vector3(0, 0.9f, 0)));
        house.AddChild(MakeBox(new Vector3(2.8f, 0.15f, 2.3f), r, new Vector3(0, 1.85f, 0)));
        house.AddChild(MakeBox(new Vector3(0.4f, 0.8f, 0.05f), new Color(0.35f, 0.2f, 0.1f), new Vector3(0, 0.4f, 1.02f)));
        house.AddChild(MakeBox(new Vector3(0.4f, 0.4f, 0.03f), new Color(0.7f, 0.85f, 0.95f), new Vector3(-0.7f, 1.1f, 1.02f)));
        house.AddChild(MakeBox(new Vector3(0.4f, 0.4f, 0.03f), new Color(0.7f, 0.85f, 0.95f), new Vector3(0.7f, 1.1f, 1.02f)));

        _main.AddChild(house);
        Items.Add(new SceneryItem { Node = house, WorldZ = z, OffsetX = offset });
    }

    private void AddBridge(float z)
    {
        var bridge = new Node3D();
        var railCol = new Color(0.5f, 0.5f, 0.5f);
        bridge.AddChild(MakeBox(new Vector3(TerrainManager.RoadW + 1f, 0.15f, 6f), new Color(0.4f, 0.3f, 0.2f), new Vector3(0, -0.1f, 3f)));

        for (float side = -1f; side <= 1f; side += 2f)
        {
            for (int i = 0; i < 4; i++)
            {
                var post = MakeCylinder(0.03f, 1f, railCol);
                post.Position = new Vector3(side * (TerrainManager.RoadW / 2f + 0.3f), 0.5f, i * 1.5f);
                bridge.AddChild(post);
            }
            var bar = MakeBox(new Vector3(0.04f, 0.04f, 5.5f), railCol, new Vector3(side * (TerrainManager.RoadW / 2f + 0.3f), 0.8f, 2.5f));
            bridge.AddChild(bar);
        }

        _main.AddChild(bridge);
        Items.Add(new SceneryItem { Node = bridge, WorldZ = z, OffsetX = 0f });
    }

    private void AddStream(float z, float offset, RandomNumberGenerator rng)
    {
        var stream = new Node3D();
        var water = new MeshInstance3D();
        var waterMesh = new CylinderMesh();
        waterMesh.TopRadius = 2f + rng.RandfRange(0f, 1f);
        waterMesh.BottomRadius = waterMesh.TopRadius;
        waterMesh.Height = 0.05f;
        water.Mesh = waterMesh;
        var waterMat = new StandardMaterial3D();
        waterMat.AlbedoColor = new Color(0.3f, 0.55f, 0.75f, 0.7f);
        waterMat.Transparency = BaseMaterial3D.TransparencyEnum.Alpha;
        water.MaterialOverride = waterMat;
        water.Position = new Vector3(0, -0.15f, 0);
        stream.AddChild(water);

        for (int i = 0; i < 6; i++)
        {
            float angle = i * Mathf.Pi / 3f;
            float r = 1.8f + rng.RandfRange(0f, 0.5f);
            var rock = MakeSphere(0.15f + rng.RandfRange(0f, 0.1f), new Color(0.45f, 0.43f, 0.4f));
            rock.Position = new Vector3(Mathf.Cos(angle) * r, -0.05f, Mathf.Sin(angle) * r);
            stream.AddChild(rock);
        }

        _main.AddChild(stream);
        Items.Add(new SceneryItem { Node = stream, WorldZ = z, OffsetX = offset });
    }

    private void AddDistanceMarker(float z)
    {
        var marker = new Node3D();
        marker.AddChild(MakeCylinder(0.03f, 0.8f, new Color(0.8f, 0.8f, 0.7f), new Vector3(0, 0.4f, 0)));
        marker.AddChild(MakeBox(new Vector3(0.3f, 0.2f, 0.03f), new Color(0.9f, 0.9f, 0.8f), new Vector3(0, 0.85f, 0)));
        _main.AddChild(marker);
        Items.Add(new SceneryItem { Node = marker, WorldZ = z, OffsetX = TerrainManager.RoadW / 2f + 0.5f });
    }

    private void CreateClouds()
    {
        var rng = new RandomNumberGenerator();
        rng.Seed = 77;
        var cloudMat = new StandardMaterial3D();
        cloudMat.AlbedoColor = new Color(0.95f, 0.95f, 0.97f, 0.7f);
        cloudMat.Transparency = BaseMaterial3D.TransparencyEnum.Alpha;

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
            cloud.Position = new Vector3(bx, bh, bz);
            _main.AddChild(cloud);
            Clouds.Add(new Cloud { Node = cloud, BaseX = bx, BaseZ = bz, Height = bh, Speed = 0.3f + rng.RandfRange(0f, 0.8f) });
        }
    }

    private void CreateFinishLine()
    {
        _finishLine = new Node3D();
        _finishLine.AddChild(MakeCylinder(0.08f, 3f, new Color(0.9f, 0.15f, 0.15f), new Vector3(-TerrainManager.RoadW / 2f - 0.5f, 1.5f, 0)));
        _finishLine.AddChild(MakeCylinder(0.08f, 3f, new Color(0.9f, 0.15f, 0.15f), new Vector3(TerrainManager.RoadW / 2f + 0.5f, 1.5f, 0)));
        _finishLine.AddChild(MakeBox(new Vector3(TerrainManager.RoadW + 1.5f, 0.6f, 0.05f), new Color(0.95f, 0.95f, 0.9f), new Vector3(0, 2.8f, 0)));

        for (int i = 0; i < 12; i++)
        {
            float x = -TerrainManager.RoadW / 2f + 0.3f + i * (TerrainManager.RoadW / 12f);
            var col = i % 2 == 0 ? new Color(0.1f, 0.1f, 0.1f) : new Color(0.9f, 0.15f, 0.15f);
            _finishLine.AddChild(MakeBox(new Vector3(TerrainManager.RoadW / 12f - 0.05f, 0.15f, 0.06f), col, new Vector3(x, 2.55f, 0)));
        }

        _main.AddChild(_finishLine);
    }

    public void UpdateAll(float dt)
    {
        UpdatePositions(_main.Terrain.ScrollOffset);
        UpdateClouds(dt);
        UpdateFinishLine();
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
        m.Mesh = mesh;
        var mat = new StandardMaterial3D();
        mat.AlbedoColor = color;
        mat.SpecularMode = BaseMaterial3D.SpecularModeEnum.Disabled;
        m.MaterialOverride = mat;
        return m;
    }

    private void AddItem(float z, float offset, MeshInstance3D mesh, Vector3 rotation = default)
    {
        mesh.Rotation = rotation;
        _main.AddChild(mesh);
        Items.Add(new SceneryItem { Node = mesh, WorldZ = z, OffsetX = offset });
    }
}
