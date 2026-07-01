using Godot;

public class PlayerManager
{
    private Main _main;
    private Node3D _skaterRoot;
    private MeshInstance3D _shirtMesh;
    private MeshInstance3D _pantsMesh;
    private MeshInstance3D _deckMesh;
    private MeshInstance3D _gripMesh;

    public float Speed = 0f;
    public float PosX = 0f;
    public float Distance = 0f;
    public bool Kicked = false;
    public float Lean = 0f;

    private const float Gravity = 0.08f;
    private const float Friction = 0.998f;
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
        deckMat.AlbedoColor = Main.BoardDeckColors[(int)_main.Board];
        deckMat.Roughness = 0.7f;
        deckMat.SpecularMode = BaseMaterial3D.SpecularModeEnum.Disabled;

        _deckMesh = new MeshInstance3D();
        var deckMesh = new BoxMesh();
        deckMesh.Size = new Vector3(0.7f, 0.05f, 1.8f);
        _deckMesh.Mesh = deckMesh;
        _deckMesh.MaterialOverride = deckMat;
        _deckMesh.Position = new Vector3(0, 0.13f, 0);
        _skaterRoot.AddChild(_deckMesh);

        // Grip tape
        var gripMat = new StandardMaterial3D();
        gripMat.AlbedoColor = Main.BoardGripColors[(int)_main.Board];
        gripMat.Roughness = 0.95f;
        _gripMesh = new MeshInstance3D();
        var gripMesh = new BoxMesh();
        gripMesh.Size = new Vector3(0.65f, 0.02f, 1.6f);
        _gripMesh.Mesh = gripMesh;
        _gripMesh.MaterialOverride = gripMat;
        _gripMesh.Position = new Vector3(0, 0.165f, 0);
        _skaterRoot.AddChild(_gripMesh);

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
        truckMat.AlbedoColor = new Color(0.62f, 0.62f, 0.65f);
        truckMat.Metallic = 0.45f;
        truckMat.Roughness = 0.35f;
        AddBox(_skaterRoot, new Vector3(0.6f, 0.05f, 0.14f), truckMat, new Vector3(0, 0.08f, 0.55f));
        AddBox(_skaterRoot, new Vector3(0.6f, 0.05f, 0.14f), truckMat, new Vector3(0, 0.08f, -0.55f));

        // Wheels (rubber, smoother)
        var wheelMat = new StandardMaterial3D();
        wheelMat.AlbedoColor = new Color(0.12f, 0.12f, 0.12f);
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

        // ── Skater body (facing right, skating stance) ──
        var bodyGroup = new Node3D();
        bodyGroup.Rotation = new Vector3(0, Mathf.Pi / 2f, 0);
        bodyGroup.Position = new Vector3(0, 0.05f, 0); // slight forward offset
        _skaterRoot.AddChild(bodyGroup);

        // Skin material
        var skinMat = new StandardMaterial3D();
        skinMat.AlbedoColor = new Color(0.9f, 0.8f, 0.62f);
        skinMat.Roughness = 0.8f;
        skinMat.SpecularMode = BaseMaterial3D.SpecularModeEnum.Disabled;

        // Shoes (wider apart, on the board)
        var shoeMat = new StandardMaterial3D();
        shoeMat.AlbedoColor = new Color(0.16f, 0.16f, 0.16f);
        shoeMat.Roughness = 0.65f;
        // Front foot (left) - near nose
        AddCylinderTo(bodyGroup, 0.07f, 0.07f, 0.22f, shoeMat, new Vector3(-0.12f, 0.19f, -0.35f));
        // Back foot (right) - on tail
        AddCylinderTo(bodyGroup, 0.07f, 0.07f, 0.22f, shoeMat, new Vector3(0.12f, 0.19f, 0.3f));

        // Legs (bent knees - angled forward for skating stance)
        var pantsMat = new StandardMaterial3D();
        pantsMat.AlbedoColor = Main.CarlPantsColors[(int)_main.Carl];
        pantsMat.Roughness = 0.85f;
        pantsMat.SpecularMode = BaseMaterial3D.SpecularModeEnum.Disabled;
        // Front leg (more bent, forward)
        _pantsMesh = AddCylinderTo(bodyGroup, 0.07f, 0.065f, 0.3f, pantsMat, new Vector3(-0.12f, 0.36f, -0.28f));
        _pantsMesh.Rotation = new Vector3(0.35f, 0, 0);
        // Back leg (slightly bent)
        AddCylinderTo(bodyGroup, 0.07f, 0.065f, 0.3f, pantsMat, new Vector3(0.12f, 0.36f, 0.15f)).Rotation = new Vector3(-0.15f, 0, 0);

        // Torso (leaning forward slightly, wider at shoulders)
        var shirtMat = new StandardMaterial3D();
        shirtMat.AlbedoColor = Main.CarlShirtColors[(int)_main.Carl];
        shirtMat.Roughness = 0.75f;
        shirtMat.SpecularMode = BaseMaterial3D.SpecularModeEnum.Disabled;
        _shirtMesh = AddCylinderTo(bodyGroup, 0.22f, 0.18f, 0.42f, shirtMat, new Vector3(0, 0.68f, -0.12f));
        _shirtMesh.Rotation = new Vector3(0.15f, 0, 0); // lean forward

        // Arms (spread wide for balance, angled outward)
        AddCylinderTo(bodyGroup, 0.04f, 0.035f, 0.35f, skinMat, new Vector3(-0.3f, 0.72f, -0.1f)).Rotation = new Vector3(0.1f, 0, 0.6f);
        AddCylinderTo(bodyGroup, 0.04f, 0.035f, 0.35f, skinMat, new Vector3(0.3f, 0.72f, -0.1f)).Rotation = new Vector3(0.1f, 0, -0.6f);

        // Head (slightly forward, looking ahead)
        AddSphereTo(bodyGroup, 0.14f, skinMat, new Vector3(0, 1.0f, -0.15f));

        // Hair
        var hairMat = new StandardMaterial3D();
        hairMat.AlbedoColor = new Color(0.32f, 0.2f, 0.1f);
        hairMat.Roughness = 0.9f;
        AddCylinderTo(bodyGroup, 0.14f, 0.15f, 0.08f, hairMat, new Vector3(0, 1.14f, -0.15f));

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

        // Push off: only when going slow (realistic skateboarding)
        if (Input.IsActionJustPressed("kick_off") && Speed < 0.5f)
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

        // Carve animation — board points in direction of travel
        if (_skaterRoot != null)
        {
            float speedFactor = Mathf.Clamp(Speed / MaxSpeed, 0f, 1f);

            // Board Y rotation (yaw) — points the board when carving
            float carveAngle = Lean * 0.4f * (0.3f + speedFactor * 0.7f);

            // Board Z rotation (roll) — tilts into the turn
            float tiltAngle = Lean * 0.3f * (0.5f + speedFactor * 0.5f);

            // Body counter-rotates slightly (stays more upright than board)
            float bodyLean = -Lean * 0.1f;

            _skaterRoot.Rotation = new Vector3(0, carveAngle, tiltAngle);

            // Body group counter-rotates to stay facing forward-ish
            if (_skaterRoot.GetChildCount() > 0)
            {
                var bodyGroup = _skaterRoot.GetChild<Node3D>(0);
                if (bodyGroup != null)
                    bodyGroup.Rotation = new Vector3(0, Mathf.Pi / 2f + bodyLean, 0);
            }
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

    public void ApplyBoard()
    {
        if (_deckMesh != null)
        {
            var mat = new StandardMaterial3D();
            mat.AlbedoColor = Main.BoardDeckColors[(int)_main.Board];
            mat.Roughness = 0.7f;
            mat.SpecularMode = BaseMaterial3D.SpecularModeEnum.Disabled;
            _deckMesh.MaterialOverride = mat;
        }
        if (_gripMesh != null)
        {
            var mat = new StandardMaterial3D();
            mat.AlbedoColor = Main.BoardGripColors[(int)_main.Board];
            mat.Roughness = 0.95f;
            _gripMesh.MaterialOverride = mat;
        }
    }
}
