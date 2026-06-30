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

    private const float Gravity = 0.06f;
    private const float Friction = 0.998f;
    public const float MaxSpeed = 5f;
    private const float Handling = 0.18f;

    // Particles
    private GpuParticles3D _dustParticles;
    private GpuParticles3D _confettiParticles;

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

        // Board (long in Z - direction of motion)
        AddBox(new Vector3(0.7f, 0.05f, 1.8f), new Color(0.42f, 0.2f, 0.05f), new Vector3(0, 0.13f, 0));
        AddBox(new Vector3(0.65f, 0.02f, 1.6f), new Color(0.15f, 0.15f, 0.15f), new Vector3(0, 0.165f, 0)); // grip

        // Nose/tail kicks
        var noseKick = AddBox(new Vector3(0.55f, 0.04f, 0.2f), new Color(0.42f, 0.2f, 0.05f), new Vector3(0, 0.18f, 0.9f));
        noseKick.Rotation = new Vector3(0.25f, 0, 0);
        var tailKick = AddBox(new Vector3(0.55f, 0.04f, 0.2f), new Color(0.42f, 0.2f, 0.05f), new Vector3(0, 0.18f, -0.9f));
        tailKick.Rotation = new Vector3(-0.25f, 0, 0);

        // Trucks
        var truckCol = new Color(0.5f, 0.5f, 0.52f);
        AddBox(new Vector3(0.6f, 0.04f, 0.12f), truckCol, new Vector3(0, 0.08f, 0.55f));
        AddBox(new Vector3(0.6f, 0.04f, 0.12f), truckCol, new Vector3(0, 0.08f, -0.55f));

        // Wheels
        var wheelCol = new Color(0.12f, 0.12f, 0.12f);
        foreach (var pos in new[] {
            new Vector3(-0.32f, 0.04f, 0.55f), new Vector3(0.32f, 0.04f, 0.55f),
            new Vector3(-0.32f, 0.04f, -0.55f), new Vector3(0.32f, 0.04f, -0.55f) })
        {
            var wheel = AddCylinder(0.06f, 0.08f, wheelCol, pos);
            wheel.Rotation = new Vector3(0, 0, Mathf.Pi / 2f);
        }

        // Body group (facing right)
        var bodyGroup = new Node3D();
        bodyGroup.Rotation = new Vector3(0, Mathf.Pi / 2f, 0);
        _skaterRoot.AddChild(bodyGroup);

        // Shoes
        var shoeCol = new Color(0.15f, 0.15f, 0.15f);
        AddBoxTo(bodyGroup, new Vector3(0.14f, 0.06f, 0.25f), shoeCol, new Vector3(-0.1f, 0.19f, -0.3f));
        AddBoxTo(bodyGroup, new Vector3(0.14f, 0.06f, 0.25f), shoeCol, new Vector3(0.1f, 0.19f, 0.25f));

        // Legs
        var pantsCol = Main.CarlPantsColors[(int)_main.Carl];
        var legL = AddBoxTo(bodyGroup, new Vector3(0.14f, 0.32f, 0.14f), pantsCol, new Vector3(-0.1f, 0.38f, -0.25f));
        legL.Rotation = new Vector3(0.1f, 0, 0);
        _pantsMesh = legL;
        var legR = AddBoxTo(bodyGroup, new Vector3(0.14f, 0.32f, 0.14f), pantsCol, new Vector3(0.1f, 0.38f, 0.2f));
        legR.Rotation = new Vector3(-0.1f, 0, 0);

        // Torso
        _shirtMesh = AddBoxTo(bodyGroup, new Vector3(0.32f, 0.42f, 0.3f), Main.CarlShirtColors[(int)_main.Carl], new Vector3(0, 0.72f, -0.05f));

        // Arms
        var skinCol = new Color(0.88f, 0.78f, 0.6f);
        var armL = AddBoxTo(bodyGroup, new Vector3(0.08f, 0.3f, 0.08f), skinCol, new Vector3(-0.22f, 0.78f, -0.05f));
        armL.Rotation = new Vector3(0, 0, 0.2f);
        var armR = AddBoxTo(bodyGroup, new Vector3(0.08f, 0.3f, 0.08f), skinCol, new Vector3(0.22f, 0.78f, -0.05f));
        armR.Rotation = new Vector3(0, 0, -0.2f);

        // Head & hair
        AddBoxTo(bodyGroup, new Vector3(0.24f, 0.24f, 0.24f), skinCol, new Vector3(0, 1.06f, -0.05f));
        AddBoxTo(bodyGroup, new Vector3(0.26f, 0.07f, 0.26f), new Color(0.3f, 0.18f, 0.08f), new Vector3(0, 1.2f, -0.05f));

        // Collision
        var col = new CollisionShape3D();
        var shape = new BoxShape3D();
        shape.Size = new Vector3(0.7f, 1.3f, 1.8f);
        col.Shape = shape;
        col.Position = new Vector3(0, 0.65f, 0);
        _main.Player.AddChild(col);
    }

    private MeshInstance3D AddBox(Vector3 size, Color color, Vector3 pos)
    {
        return AddBoxTo(_skaterRoot, size, color, pos);
    }

    private MeshInstance3D AddBoxTo(Node3D parent, Vector3 size, Color color, Vector3 pos)
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
        parent.AddChild(m);
        return m;
    }

    private MeshInstance3D AddCylinder(float topR, float height, Color color, Vector3 pos)
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
        _skaterRoot.AddChild(m);
        return m;
    }

    private void CreateParticles()
    {
        // Dust
        _dustParticles = new GpuParticles3D();
        _dustParticles.Amount = 40;
        _dustParticles.Lifetime = 0.8f;
        _dustParticles.Transform = new Transform3D(Basis.Identity, new Vector3(0, 0.1f, -0.8f));
        var dustMat = new ParticleProcessMaterial();
        dustMat.Direction = new Vector3(0, 0.5f, -1f);
        dustMat.Spread = 30f;
        dustMat.InitialVelocityMin = 0.5f;
        dustMat.InitialVelocityMax = 1.5f;
        dustMat.Gravity = new Vector3(0, -0.5f, 0);
        dustMat.ScaleMin = 0.05f;
        dustMat.ScaleMax = 0.15f;
        dustMat.Color = new Color(0.6f, 0.55f, 0.4f, 0.6f);
        _dustParticles.ProcessMaterial = dustMat;
        _dustParticles.Emitting = false;
        _main.Player.AddChild(_dustParticles);

        // Confetti
        _confettiParticles = new GpuParticles3D();
        _confettiParticles.Amount = 200;
        _confettiParticles.Lifetime = 2.5f;
        _confettiParticles.OneShot = true;
        _confettiParticles.Emitting = false;
        var confMat = new ParticleProcessMaterial();
        confMat.Direction = new Vector3(0, 1, 0);
        confMat.Spread = 60f;
        confMat.InitialVelocityMin = 3f;
        confMat.InitialVelocityMax = 8f;
        confMat.Gravity = new Vector3(0, -3f, 0);
        confMat.ScaleMin = 0.03f;
        confMat.ScaleMax = 0.08f;
        confMat.Color = new Color(1f, 0.2f, 0.6f, 1f);
        _confettiParticles.ProcessMaterial = confMat;
        _main.AddChild(_confettiParticles);
    }

    public void Update(float dt, float steer, bool braking)
    {
        var terrain = _main.Terrain;
        float slope = (terrain.HillAt(Distance + 3f) - terrain.HillAt(Distance)) / 3f;
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
