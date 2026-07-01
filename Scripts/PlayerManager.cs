using Godot;

public class PlayerManager
{
    private Main _main;
    private Node3D _skaterRoot;
    private MeshInstance3D _shirtMesh;
    private MeshInstance3D _pantsMesh;

    public float Speed = 0f;
    public float PosX = 0f;
    public float Distance = 0f;
    public bool Kicked = false;
    public float Lean = 0f;

    private const float Gravity = 0.03f;
    private const float Friction = 0.996f;
    public const float MaxSpeed = 5f;
    private const float Handling = 0.18f;

    // Particles
    private GpuParticles3D _dustParticles;
    private GpuParticles3D _confettiParticles;
    private GpuParticles3D _speedLines;

    public PlayerManager(Main main)
    {
        _main = main;
    }

    public void Create()
    {
        CreateMesh();
        CreateParticles();
    }

    private void CreateMesh()
    {
        _skaterRoot = new Node3D();
        _main.Player.AddChild(_skaterRoot);

        // ── Board ──
        // Deck with wood grain color
        var deckMat = new StandardMaterial3D();
        deckMat.AlbedoColor = new Color(0.45f, 0.22f, 0.06f);
        deckMat.Roughness = 0.7f;
        deckMat.SpecularMode = BaseMaterial3D.SpecularModeEnum.Disabled;

        var deck = new MeshInstance3D();
        var deckMesh = new BoxMesh();
        deckMesh.Size = new Vector3(0.7f, 0.05f, 1.8f);
        deck.Mesh = deckMesh;
        deck.MaterialOverride = deckMat;
        deck.Position = new Vector3(0, 0.13f, 0);
        _skaterRoot.AddChild(deck);

        // Grip tape
        var gripMat = new StandardMaterial3D();
        gripMat.AlbedoColor = new Color(0.12f, 0.12f, 0.12f);
        gripMat.Roughness = 0.95f;
        var grip = new MeshInstance3D();
        var gripMesh = new BoxMesh();
        gripMesh.Size = new Vector3(0.65f, 0.02f, 1.6f);
        grip.Mesh = gripMesh;
        grip.MaterialOverride = gripMat;
        grip.Position = new Vector3(0, 0.165f, 0);
        _skaterRoot.AddChild(grip);

        // Nose/tail kicks
        var kickMat = new StandardMaterial3D();
        kickMat.AlbedoColor = new Color(0.48f, 0.24f, 0.07f);
        kickMat.Roughness = 0.7f;
        var noseKick = new MeshInstance3D();
        var nkMesh = new BoxMesh();
        nkMesh.Size = new Vector3(0.55f, 0.04f, 0.2f);
        noseKick.Mesh = nkMesh;
        noseKick.MaterialOverride = kickMat;
        noseKick.Position = new Vector3(0, 0.18f, 0.9f);
        noseKick.Rotation = new Vector3(0.25f, 0, 0);
        _skaterRoot.AddChild(noseKick);

        var tailKick = new MeshInstance3D();
        var tkMesh = new BoxMesh();
        tkMesh.Size = new Vector3(0.55f, 0.04f, 0.2f);
        tailKick.Mesh = tkMesh;
        tailKick.MaterialOverride = kickMat;
        tailKick.Position = new Vector3(0, 0.18f, -0.9f);
        tailKick.Rotation = new Vector3(-0.25f, 0, 0);
        _skaterRoot.AddChild(tailKick);

        // Trucks (metal, smoother)
        var truckMat = new StandardMaterial3D();
        truckMat.AlbedoColor = new Color(0.58f, 0.58f, 0.62f);
        truckMat.Metallic = 0.4f;
        truckMat.Roughness = 0.35f;
        AddBox(_skaterRoot, new Vector3(0.6f, 0.05f, 0.14f), truckMat, new Vector3(0, 0.08f, 0.55f));
        AddBox(_skaterRoot, new Vector3(0.6f, 0.05f, 0.14f), truckMat, new Vector3(0, 0.08f, -0.55f));

        // Wheels (rubber, smoother)
        var wheelMat = new StandardMaterial3D();
        wheelMat.AlbedoColor = new Color(0.1f, 0.1f, 0.1f);
        wheelMat.Roughness = 0.6f;
        foreach (var pos in new[] {
            new Vector3(-0.32f, 0.04f, 0.55f), new Vector3(0.32f, 0.04f, 0.55f),
            new Vector3(-0.32f, 0.04f, -0.55f), new Vector3(0.32f, 0.04f, -0.55f) })
        {
            var wheel = new MeshInstance3D();
            var wMesh = new CylinderMesh();
            wMesh.TopRadius = 0.06f;
            wMesh.BottomRadius = 0.06f;
            wMesh.Height = 0.08f;
            wMesh.RadialSegments = 16;
            wheel.Mesh = wMesh;
            wheel.MaterialOverride = wheelMat;
            wheel.Position = pos;
            wheel.Rotation = new Vector3(0, 0, Mathf.Pi / 2f);
            _skaterRoot.AddChild(wheel);
        }

        // ── Skater body (facing right) ──
        var bodyGroup = new Node3D();
        bodyGroup.Rotation = new Vector3(0, Mathf.Pi / 2f, 0);
        _skaterRoot.AddChild(bodyGroup);

        // Skin material
        var skinMat = new StandardMaterial3D();
        skinMat.AlbedoColor = new Color(0.88f, 0.78f, 0.6f);
        skinMat.Roughness = 0.8f;
        skinMat.SpecularMode = BaseMaterial3D.SpecularModeEnum.Disabled;

        // Shoes (rounded)
        var shoeMat = new StandardMaterial3D();
        shoeMat.AlbedoColor = new Color(0.12f, 0.12f, 0.12f);
        shoeMat.Roughness = 0.7f;
        AddCylinderTo(bodyGroup, 0.06f, 0.06f, 0.28f, shoeMat, new Vector3(-0.1f, 0.19f, -0.3f));
        AddCylinderTo(bodyGroup, 0.06f, 0.06f, 0.28f, shoeMat, new Vector3(0.1f, 0.19f, 0.25f));

        // Legs (cylinders for smoother look)
        var pantsMat = new StandardMaterial3D();
        pantsMat.AlbedoColor = Main.CarlPantsColors[(int)_main.Carl];
        pantsMat.Roughness = 0.85f;
        pantsMat.SpecularMode = BaseMaterial3D.SpecularModeEnum.Disabled;
        _pantsMesh = AddCylinderTo(bodyGroup, 0.065f, 0.065f, 0.34f, pantsMat, new Vector3(-0.1f, 0.38f, -0.25f));
        _pantsMesh.Rotation = new Vector3(0.1f, 0, 0);
        AddCylinderTo(bodyGroup, 0.065f, 0.065f, 0.34f, pantsMat, new Vector3(0.1f, 0.38f, 0.2f)).Rotation = new Vector3(-0.1f, 0, 0);

        // Torso (rounded cylinder)
        var shirtMat = new StandardMaterial3D();
        shirtMat.AlbedoColor = Main.CarlShirtColors[(int)_main.Carl];
        shirtMat.Roughness = 0.8f;
        shirtMat.SpecularMode = BaseMaterial3D.SpecularModeEnum.Disabled;
        _shirtMesh = AddCylinderTo(bodyGroup, 0.18f, 0.16f, 0.44f, shirtMat, new Vector3(0, 0.72f, -0.05f));

        // Arms (cylinders)
        AddCylinderTo(bodyGroup, 0.04f, 0.04f, 0.32f, skinMat, new Vector3(-0.24f, 0.78f, -0.05f)).Rotation = new Vector3(0, 0, 0.2f);
        AddCylinderTo(bodyGroup, 0.04f, 0.04f, 0.32f, skinMat, new Vector3(0.24f, 0.78f, -0.05f)).Rotation = new Vector3(0, 0, -0.2f);

        // Head (sphere)
        AddSphereTo(bodyGroup, 0.14f, skinMat, new Vector3(0, 1.08f, -0.05f));

        // Hair (flattened sphere)
        var hairMat = new StandardMaterial3D();
        hairMat.AlbedoColor = new Color(0.3f, 0.18f, 0.08f);
        hairMat.Roughness = 0.9f;
        AddCylinderTo(bodyGroup, 0.14f, 0.15f, 0.08f, hairMat, new Vector3(0, 1.22f, -0.05f));

        // Collision
        var col = new CollisionShape3D();
        var shape = new BoxShape3D();
        shape.Size = new Vector3(0.7f, 1.3f, 1.8f);
        col.Shape = shape;
        col.Position = new Vector3(0, 0.65f, 0);
        _main.Player.AddChild(col);
    }

    private MeshInstance3D AddBox(Node3D parent, Vector3 size, StandardMaterial3D mat, Vector3 pos)
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

    private MeshInstance3D AddCylinderTo(Node3D parent, float topR, float bottomR, float height, StandardMaterial3D mat, Vector3 pos)
    {
        var m = new MeshInstance3D();
        var mesh = new CylinderMesh();
        mesh.TopRadius = topR;
        mesh.BottomRadius = bottomR;
        mesh.Height = height;
        mesh.RadialSegments = 12;
        m.Mesh = mesh;
        m.MaterialOverride = mat;
        m.Position = pos;
        parent.AddChild(m);
        return m;
    }

    private MeshInstance3D AddSphereTo(Node3D parent, float radius, StandardMaterial3D mat, Vector3 pos)
    {
        var m = new MeshInstance3D();
        var mesh = new SphereMesh();
        mesh.Radius = radius;
        mesh.Height = radius * 2f;
        mesh.Rings = 8;
        mesh.RadialSegments = 12;
        m.Mesh = mesh;
        m.MaterialOverride = mat;
        m.Position = pos;
        parent.AddChild(m);
        return m;
    }

    private void CreateParticles()
    {
        // Dust trail
        _dustParticles = new GpuParticles3D();
        _dustParticles.Amount = 60;
        _dustParticles.Lifetime = 1.0f;
        _dustParticles.Transform = new Transform3D(Basis.Identity, new Vector3(0, 0.05f, -0.9f));
        var dustMat = new ParticleProcessMaterial();
        dustMat.Direction = new Vector3(0, 0.3f, -1f);
        dustMat.Spread = 40f;
        dustMat.InitialVelocityMin = 0.3f;
        dustMat.InitialVelocityMax = 1.2f;
        dustMat.Gravity = new Vector3(0, -0.3f, 0);
        dustMat.ScaleMin = 0.03f;
        dustMat.ScaleMax = 0.12f;
        dustMat.Color = new Color(0.55f, 0.5f, 0.38f, 0.5f);
        _dustParticles.ProcessMaterial = dustMat;
        _dustParticles.Emitting = false;
        _main.Player.AddChild(_dustParticles);

        // Confetti celebration
        _confettiParticles = new GpuParticles3D();
        _confettiParticles.Amount = 300;
        _confettiParticles.Lifetime = 3f;
        _confettiParticles.OneShot = true;
        _confettiParticles.Emitting = false;
        var confMat = new ParticleProcessMaterial();
        confMat.Direction = new Vector3(0, 1, 0);
        confMat.Spread = 70f;
        confMat.InitialVelocityMin = 4f;
        confMat.InitialVelocityMax = 10f;
        confMat.Gravity = new Vector3(0, -2.5f, 0);
        confMat.ScaleMin = 0.02f;
        confMat.ScaleMax = 0.06f;
        confMat.Color = new Color(1f, 0.2f, 0.6f, 1f);
        _confettiParticles.ProcessMaterial = confMat;
        _main.AddChild(_confettiParticles);

        // Speed lines (at high speed)
        _speedLines = new GpuParticles3D();
        _speedLines.Amount = 30;
        _speedLines.Lifetime = 0.4f;
        _speedLines.Transform = new Transform3D(Basis.Identity, new Vector3(0, 0.5f, 2f));
        var speedMat = new ParticleProcessMaterial();
        speedMat.Direction = new Vector3(0, 0, 1f);
        speedMat.Spread = 15f;
        speedMat.InitialVelocityMin = 8f;
        speedMat.InitialVelocityMax = 15f;
        speedMat.ScaleMin = 0.01f;
        speedMat.ScaleMax = 0.02f;
        speedMat.Color = new Color(1f, 1f, 1f, 0.3f);
        _speedLines.ProcessMaterial = speedMat;
        _speedLines.Emitting = false;
        _main.Player.AddChild(_speedLines);
    }

    public void Update(float dt, float steer, bool braking)
    {
        var terrain = _main.Terrain;
        float slope = (terrain.HillAt(Distance + 3f) - terrain.HillAt(Distance)) / 3f;

        // Push off: gives a burst of speed
        if (Input.IsActionJustPressed("kick_off"))
        {
            Speed += 0.25f;
            Kicked = true;
        }

        // Slope acceleration
        Speed += -slope * Gravity * dt * 60f;
        Speed *= Mathf.Pow(Friction, dt * 60f);

        if (braking)
            Speed *= Mathf.Pow(0.97f, dt * 60f);

        Lean = Mathf.Lerp(Lean, steer, 6f * dt);
        PosX += steer * Handling * dt * 60f * (0.3f + Speed * 0.5f);
        PosX = Mathf.Clamp(PosX, -3.5f, 3.5f);
        Speed = Mathf.Clamp(Speed, 0.02f, MaxSpeed);

        Distance += Speed * dt * 60f;

        float groundY = terrain.HillAt(Distance);
        _main.Player.Position = new Vector3(PosX, groundY + 0.1f, 0);

        // Lean animation
        if (_skaterRoot != null)
        {
            float leanAngle = Lean * 0.25f;
            float bodyLean = Lean * 0.15f;
            float speedFactor = Mathf.Clamp(Speed / MaxSpeed, 0f, 1f);
            _skaterRoot.Rotation = new Vector3(0, bodyLean, leanAngle * (0.5f + speedFactor * 0.5f));
        }

        // Dust particles
        _dustParticles.Emitting = Speed > 0.3f && Kicked;
        var dustMat = _dustParticles.ProcessMaterial as ParticleProcessMaterial;
        if (dustMat != null)
        {
            float intensity = Mathf.Clamp(Speed / MaxSpeed, 0.1f, 1f);
            dustMat.Color = new Color(0.6f, 0.55f, 0.4f, 0.3f + intensity * 0.4f);
        }

        // Speed lines at high speed
        float spdFactor = Mathf.Clamp(Speed / MaxSpeed, 0f, 1f);
        _speedLines.Emitting = spdFactor > 0.7f && Kicked;
        var speedMat = _speedLines.ProcessMaterial as ParticleProcessMaterial;
        if (speedMat != null)
        {
            float lineIntensity = (spdFactor - 0.7f) / 0.3f;
            speedMat.Color = new Color(1f, 1f, 1f, 0.1f + lineIntensity * 0.3f);
        }
    }

    public void SpawnConfetti()
    {
        _confettiParticles.Position = _main.Player.Position + new Vector3(0, 2f, 0);
        _confettiParticles.Restart();
        _confettiParticles.Emitting = true;
    }

    public void Reset()
    {
        Speed = 0f;
        PosX = 0f;
        Distance = 0f;
        Kicked = false;
        Lean = 0f;
        _main.Player.Position = new Vector3(0, 0.1f, 0);
        if (_skaterRoot != null) _skaterRoot.Rotation = Vector3.Zero;
    }
}
