using Godot;

public class PlayerManager
{
    private Main _main;
    private Node3D _skaterRoot;

    // Board meshes (for color swaps)
    private MeshInstance3D _deckMesh;
    private MeshInstance3D _deckNoseMesh;
    private MeshInstance3D _deckTailMesh;
    private MeshInstance3D _gripMesh;

    // Joint hierarchy — each is a Node3D we can rotate for animation
    private Node3D _bodyGroup;   // body counter-lean
    private Node3D _hip;         // root of skeleton
    private Node3D _spine;       // torso sway / forward lean
    private Node3D _neck;        // head look
    private Node3D _armL;        // left shoulder
    private Node3D _forearmL;    // left elbow
    private Node3D _armR;        // right shoulder
    private Node3D _forearmR;    // right elbow
    private Node3D _legL;        // left hip
    private Node3D _kneeL;       // left knee
    private Node3D _legR;        // right hip
    private Node3D _kneeR;       // right knee

    public float Speed = 0f;
    public float PosX = 0f;
    public float Distance = 0f;
    public bool Kicked = false;
    public float Lean = 0f;
    public bool Crashed = false;
    public float PushOffTimer = 0f;
    private float _prevSteer = 0f;
    private float _carveAngleSmooth = 0f;

    // Speed wobble system
    public float WobbleLevel = 0f;
    public float TimeSinceCarve = 0f;

    private const float Gravity = 0.08f;
    private const float Friction = 0.998f;
    public const float MaxSpeed = 5f;
    private const float Handling = 0.18f;
    private const float AnimLerp = 8f;

    // Wobble constants
    private const float WobbleSpeedThreshold = 0.6f;
    private const float BrakeSpeedThreshold = 0.7f;
    private const float BrakeCutoff = 0.85f;
    private const float WobbleBuildRate = 0.35f;
    private const float WobbleDecayRate = 0.8f;
    private const float CarveResetTime = 0.3f;
    private const float WobbleCrashLevel = 1f;

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
        CreateBoard();
        CreateBody();
        CreateParticles();
    }

    // ═══════════════════════════════════════════
    //  LONGBOARD
    // ═══════════════════════════════════════════

    private void CreateBoard()
    {
        _skaterRoot = new Node3D();
        _main.Player.AddChild(_skaterRoot);

        var deckMat = new StandardMaterial3D();
        deckMat.AlbedoColor = Main.BoardDeckColors[(int)_main.Board];
        deckMat.Roughness = 0.7f;
        deckMat.SpecularMode = BaseMaterial3D.SpecularModeEnum.Disabled;

        // ── Deck: center strip (flat) ──
        _deckMesh = new MeshInstance3D();
        var deckCenter = new BoxMesh();
        deckCenter.Size = new Vector3(0.62f, 0.045f, 1.4f);
        _deckMesh.Mesh = deckCenter;
        _deckMesh.MaterialOverride = deckMat;
        _deckMesh.Position = new Vector3(0, 0.13f, 0);
        _skaterRoot.AddChild(_deckMesh);

        // Nose extension (tapers narrower, flat)
        _deckNoseMesh = new MeshInstance3D();
        var deckNose = new BoxMesh();
        deckNose.Size = new Vector3(0.48f, 0.04f, 0.4f);
        _deckNoseMesh.Mesh = deckNose;
        _deckNoseMesh.MaterialOverride = deckMat;
        _deckNoseMesh.Position = new Vector3(0, 0.13f, 0.9f);
        _skaterRoot.AddChild(_deckNoseMesh);

        // Tail extension
        _deckTailMesh = new MeshInstance3D();
        var deckTail = new BoxMesh();
        deckTail.Size = new Vector3(0.48f, 0.04f, 0.35f);
        _deckTailMesh.Mesh = deckTail;
        _deckTailMesh.MaterialOverride = deckMat;
        _deckTailMesh.Position = new Vector3(0, 0.13f, -0.88f);
        _skaterRoot.AddChild(_deckTailMesh);

        // ── Grip tape ──
        var gripMat = new StandardMaterial3D();
        gripMat.AlbedoColor = Main.BoardGripColors[(int)_main.Board];
        gripMat.Roughness = 0.95f;
        _gripMesh = new MeshInstance3D();
        var gripMesh = new BoxMesh();
        gripMesh.Size = new Vector3(0.58f, 0.015f, 1.3f);
        _gripMesh.Mesh = gripMesh;
        _gripMesh.MaterialOverride = gripMat;
        _gripMesh.Position = new Vector3(0, 0.16f, 0);
        _skaterRoot.AddChild(_gripMesh);

        // ── Trucks ──
        var truckMat = new StandardMaterial3D();
        truckMat.AlbedoColor = new Color(0.62f, 0.62f, 0.65f);
        truckMat.Metallic = 0.5f;
        truckMat.Roughness = 0.3f;

        // Front truck: baseplate + axle
        AddBox(_skaterRoot, new Vector3(0.18f, 0.04f, 0.14f), truckMat, new Vector3(0, 0.08f, 0.55f));
        var axleMat = new StandardMaterial3D();
        axleMat.AlbedoColor = new Color(0.55f, 0.55f, 0.58f);
        axleMat.Metallic = 0.6f;
        axleMat.Roughness = 0.25f;
        AddCylinder(_skaterRoot, 0.015f, 0.015f, 0.58f, axleMat, new Vector3(0, 0.06f, 0.55f), new Vector3(0, 0, Mathf.Pi / 2f));

        // Rear truck
        AddBox(_skaterRoot, new Vector3(0.18f, 0.04f, 0.14f), truckMat, new Vector3(0, 0.08f, -0.55f));
        AddCylinder(_skaterRoot, 0.015f, 0.015f, 0.58f, axleMat, new Vector3(0, 0.06f, -0.55f), new Vector3(0, 0, Mathf.Pi / 2f));

        // ── Wheels ──
        var wheelMat = new StandardMaterial3D();
        wheelMat.AlbedoColor = new Color(0.12f, 0.12f, 0.12f);
        wheelMat.Roughness = 0.55f;

        var hubMat = new StandardMaterial3D();
        hubMat.AlbedoColor = new Color(0.45f, 0.45f, 0.48f);
        hubMat.Metallic = 0.4f;
        hubMat.Roughness = 0.3f;

        foreach (var pos in new[] {
            new Vector3(-0.30f, 0.03f, 0.55f), new Vector3(0.30f, 0.03f, 0.55f),
            new Vector3(-0.30f, 0.03f, -0.55f), new Vector3(0.30f, 0.03f, -0.55f) })
        {
            var wheel = new MeshInstance3D();
            var wMesh = new CylinderMesh();
            wMesh.TopRadius = 0.055f;
            wMesh.BottomRadius = 0.055f;
            wMesh.Height = 0.07f;
            wMesh.RadialSegments = 20;
            wheel.Mesh = wMesh;
            wheel.MaterialOverride = wheelMat;
            wheel.Position = pos;
            wheel.Rotation = new Vector3(0, 0, Mathf.Pi / 2f);
            _skaterRoot.AddChild(wheel);

            // Hub
            var hub = new MeshInstance3D();
            var hMesh = new CylinderMesh();
            hMesh.TopRadius = 0.025f;
            hMesh.BottomRadius = 0.025f;
            hMesh.Height = 0.075f;
            hMesh.RadialSegments = 12;
            hub.Mesh = hMesh;
            hub.MaterialOverride = hubMat;
            hub.Position = pos;
            hub.Rotation = new Vector3(0, 0, Mathf.Pi / 2f);
            _skaterRoot.AddChild(hub);
        }
    }

    // ═══════════════════════════════════════════
    //  SKATER BODY — joint hierarchy
    // ═══════════════════════════════════════════

    private void CreateBody()
    {
        // Materials
        var skinMat = new StandardMaterial3D();
        skinMat.AlbedoColor = new Color(0.9f, 0.78f, 0.6f);
        skinMat.Roughness = 0.75f;
        skinMat.SpecularMode = BaseMaterial3D.SpecularModeEnum.Disabled;

        var shoeMat = new StandardMaterial3D();
        shoeMat.AlbedoColor = new Color(0.14f, 0.14f, 0.14f);
        shoeMat.Roughness = 0.6f;

        var soleMat = new StandardMaterial3D();
        soleMat.AlbedoColor = new Color(0.08f, 0.08f, 0.08f);
        soleMat.Roughness = 0.7f;

        var pantsMat = new StandardMaterial3D();
        pantsMat.AlbedoColor = Main.CarlPantsColors[(int)_main.Carl];
        pantsMat.Roughness = 0.85f;
        pantsMat.SpecularMode = BaseMaterial3D.SpecularModeEnum.Disabled;

        var shirtMat = new StandardMaterial3D();
        shirtMat.AlbedoColor = Main.CarlShirtColors[(int)_main.Carl];
        shirtMat.Roughness = 0.75f;
        shirtMat.SpecularMode = BaseMaterial3D.SpecularModeEnum.Disabled;

        var hairMat = new StandardMaterial3D();
        hairMat.AlbedoColor = new Color(0.3f, 0.18f, 0.08f);
        hairMat.Roughness = 0.9f;

        // ── Body group — sideways stance (facing right) ──
        _bodyGroup = new Node3D();
        _bodyGroup.Position = new Vector3(0, 0.05f, 0);
        _bodyGroup.Rotation = new Vector3(0, -Mathf.Pi / 2f, 0); // face right (+X)
        _skaterRoot.AddChild(_bodyGroup);

        // ── Hip (skeleton root) ──
        _hip = new Node3D();
        _hip.Position = new Vector3(0, 0.2f, 0);
        _bodyGroup.AddChild(_hip);

        // ── Spine (torso) ──
        _spine = new Node3D();
        _spine.Position = new Vector3(0, 0.35f, 0);
        _hip.AddChild(_spine);

        // Waist (lower torso)
        AddCylinderTo(_spine, 0.13f, 0.11f, 0.18f, shirtMat, new Vector3(0, 0.09f, 0));
        // Chest (upper torso)
        AddCylinderTo(_spine, 0.15f, 0.13f, 0.22f, shirtMat, new Vector3(0, 0.29f, -0.02f));
        // Shoulders (wider cap)
        AddCylinderTo(_spine, 0.16f, 0.15f, 0.06f, shirtMat, new Vector3(0, 0.42f, -0.02f));

        // ── Neck ──
        _neck = new Node3D();
        _neck.Position = new Vector3(0, 0.46f, -0.02f);
        _spine.AddChild(_neck);
        AddCylinderTo(_neck, 0.05f, 0.06f, 0.08f, skinMat, new Vector3(0, 0.04f, 0));

        // Head
        var head = new MeshInstance3D();
        var headMesh = new SphereMesh();
        headMesh.Radius = 0.12f;
        headMesh.Height = 0.24f;
        headMesh.Rings = 10;
        headMesh.RadialSegments = 16;
        head.Mesh = headMesh;
        head.MaterialOverride = skinMat;
        head.Position = new Vector3(0, 0.14f, -0.02f);
        _neck.AddChild(head);

        // Hair (flat cap)
        var hair = new MeshInstance3D();
        var hairMesh = new CylinderMesh();
        hairMesh.TopRadius = 0.11f;
        hairMesh.BottomRadius = 0.13f;
        hairMesh.Height = 0.06f;
        hairMesh.RadialSegments = 16;
        hair.Mesh = hairMesh;
        hair.MaterialOverride = hairMat;
        hair.Position = new Vector3(0, 0.24f, -0.02f);
        _neck.AddChild(hair);

        // ── Left arm ──
        _armL = new Node3D();
        _armL.Position = new Vector3(-0.17f, 0.4f, -0.02f);
        _spine.AddChild(_armL);
        // Upper arm
        AddCylinderTo(_armL, 0.035f, 0.03f, 0.2f, skinMat, new Vector3(0, -0.1f, 0));
        // Forearm joint
        _forearmL = new Node3D();
        _forearmL.Position = new Vector3(0, -0.2f, 0);
        _armL.AddChild(_forearmL);
        AddCylinderTo(_forearmL, 0.03f, 0.025f, 0.18f, skinMat, new Vector3(0, -0.09f, 0));
        // Hand
        AddSphereTo(_forearmL, 0.03f, skinMat, new Vector3(0, -0.2f, 0));

        // ── Right arm ──
        _armR = new Node3D();
        _armR.Position = new Vector3(0.17f, 0.4f, -0.02f);
        _spine.AddChild(_armR);
        AddCylinderTo(_armR, 0.035f, 0.03f, 0.2f, skinMat, new Vector3(0, -0.1f, 0));
        _forearmR = new Node3D();
        _forearmR.Position = new Vector3(0, -0.2f, 0);
        _armR.AddChild(_forearmR);
        AddCylinderTo(_forearmR, 0.03f, 0.025f, 0.18f, skinMat, new Vector3(0, -0.09f, 0));
        AddSphereTo(_forearmR, 0.03f, skinMat, new Vector3(0, -0.2f, 0));

        // ── Left leg ──
        _legL = new Node3D();
        _legL.Position = new Vector3(-0.08f, 0.0f, 0);
        _hip.AddChild(_legL);
        // Upper leg (thigh)
        AddCylinderTo(_legL, 0.06f, 0.055f, 0.22f, pantsMat, new Vector3(0, -0.11f, 0));
        // Knee joint
        _kneeL = new Node3D();
        _kneeL.Position = new Vector3(0, -0.22f, 0);
        _legL.AddChild(_kneeL);
        // Lower leg (shin)
        AddCylinderTo(_kneeL, 0.05f, 0.045f, 0.2f, pantsMat, new Vector3(0, -0.1f, 0));
        // Shoe
        var shoeL = new MeshInstance3D();
        var shoeLMesh = new BoxMesh();
        shoeLMesh.Size = new Vector3(0.1f, 0.06f, 0.22f);
        shoeL.Mesh = shoeLMesh;
        shoeL.MaterialOverride = shoeMat;
        shoeL.Position = new Vector3(0, -0.22f, -0.02f);
        _kneeL.AddChild(shoeL);
        // Sole
        AddBoxTo(_kneeL, new Vector3(0.1f, 0.02f, 0.22f), soleMat, new Vector3(0, -0.25f, -0.02f));

        // ── Right leg ──
        _legR = new Node3D();
        _legR.Position = new Vector3(0.08f, 0.0f, 0);
        _hip.AddChild(_legR);
        AddCylinderTo(_legR, 0.06f, 0.055f, 0.22f, pantsMat, new Vector3(0, -0.11f, 0));
        _kneeR = new Node3D();
        _kneeR.Position = new Vector3(0, -0.22f, 0);
        _legR.AddChild(_kneeR);
        AddCylinderTo(_kneeR, 0.05f, 0.045f, 0.2f, pantsMat, new Vector3(0, -0.1f, 0));
        var shoeR = new MeshInstance3D();
        var shoeRMesh = new BoxMesh();
        shoeRMesh.Size = new Vector3(0.1f, 0.06f, 0.22f);
        shoeR.Mesh = shoeRMesh;
        shoeR.MaterialOverride = shoeMat;
        shoeR.Position = new Vector3(0, -0.22f, -0.02f);
        _kneeR.AddChild(shoeR);
        AddBoxTo(_kneeR, new Vector3(0.1f, 0.02f, 0.22f), soleMat, new Vector3(0, -0.25f, -0.02f));

        // ── Collision ──
        var col = new CollisionShape3D();
        var shape = new BoxShape3D();
        shape.Size = new Vector3(0.7f, 1.3f, 1.8f);
        col.Shape = shape;
        col.Position = new Vector3(0, 0.65f, 0);
        _main.Player.AddChild(col);

        // ── Initial riding stance ──
        ApplyStance(0f, 0f, false);
    }

    private void ApplyStance(float speedFactor, float lean, bool braking)
    {
        // Set joints to a relaxed riding pose
        float kneeBend = -0.25f - speedFactor * 0.15f;
        float spinePitch = -0.1f - speedFactor * 0.15f;
        float armOut = 0.7f - speedFactor * 0.3f;

        if (_hip != null) _hip.Rotation = new Vector3(0, 0, lean * 0.08f);
        if (_spine != null) _spine.Rotation = new Vector3(spinePitch, lean * 0.1f, 0);
        if (_neck != null) _neck.Rotation = new Vector3(0.1f, -lean * 0.15f, 0);
        if (_armL != null) _armL.Rotation = new Vector3(0.1f, 0, -armOut);
        if (_forearmL != null) _forearmL.Rotation = new Vector3(-0.3f, 0, 0);
        if (_armR != null) _armR.Rotation = new Vector3(0.1f, 0, armOut);
        if (_forearmR != null) _forearmR.Rotation = new Vector3(-0.3f, 0, 0);
        if (_legL != null) _legL.Rotation = new Vector3(0.15f + speedFactor * 0.1f, 0, 0);
        if (_kneeL != null) _kneeL.Rotation = new Vector3(kneeBend, 0, 0);
        if (_legR != null) _legR.Rotation = new Vector3(0.15f + speedFactor * 0.1f, 0, 0);
        if (_kneeR != null) _kneeR.Rotation = new Vector3(kneeBend, 0, 0);
    }

    // ═══════════════════════════════════════════
    //  PARTICLES
    // ═══════════════════════════════════════════

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

    // ═══════════════════════════════════════════
    //  UPDATE
    // ═══════════════════════════════════════════

    public void Update(float dt, float steer, bool braking)
    {
        var terrain = _main.Terrain;
        float slope = (terrain.HillAt(Distance + 3f) - terrain.HillAt(Distance)) / 3f;

        // Push off
        if (Input.IsActionJustPressed("kick_off") && Speed < 0.5f)
        {
            Speed += 0.25f;
            Kicked = true;
            PushOffTimer = 0.4f;
        }
        PushOffTimer = Mathf.Max(0f, PushOffTimer - dt);

        // Slope acceleration
        Speed += -slope * Gravity * dt * 60f;
        Speed *= Mathf.Pow(Friction, dt * 60f);

        float speedFactor = Mathf.Clamp(Speed / MaxSpeed, 0f, 1f);

        // Speed-dependent braking
        if (braking)
        {
            if (speedFactor < BrakeSpeedThreshold)
            {
                Speed *= Mathf.Pow(0.97f, dt * 60f);
            }
            else if (speedFactor < BrakeCutoff)
            {
                float brakeFade = 1f - (speedFactor - BrakeSpeedThreshold) / (BrakeCutoff - BrakeSpeedThreshold);
                Speed *= Mathf.Pow(Mathf.Lerp(1f, 0.97f, brakeFade), dt * 60f);
            }
        }

        // Shoulder drag
        if (Mathf.Abs(PosX) > 3.5f)
            Speed *= Mathf.Pow(0.94f, dt * 60f);

        // Wobble system — track carving and build wobble at high speed
        bool isCarving = Mathf.Abs(steer) > 0.1f;
        if (isCarving)
        {
            TimeSinceCarve = 0f;
            WobbleLevel -= WobbleDecayRate * dt;
        }
        else
        {
            TimeSinceCarve += dt;
            if (speedFactor >= WobbleSpeedThreshold && TimeSinceCarve > CarveResetTime)
            {
                WobbleLevel += WobbleBuildRate * dt * (0.5f + (speedFactor - WobbleSpeedThreshold) / (1f - WobbleSpeedThreshold) * 0.5f);
            }
            else
            {
                WobbleLevel -= WobbleDecayRate * 0.5f * dt;
            }
        }
        WobbleLevel = Mathf.Clamp(WobbleLevel, 0f, 1f);

        // Wobble crash
        if (WobbleLevel >= WobbleCrashLevel)
        {
            Crashed = true;
            Speed = 0f;
        }

        // Turn anticipation — brief counter-lean when steer changes from 0
        float leanTarget = steer;
        if (steer != 0f && _prevSteer == 0f)
            leanTarget = -steer * 0.4f;
        Lean = Mathf.Lerp(Lean, leanTarget, 12f * dt);
        if (Mathf.Abs(Lean) < 0.005f) Lean = 0f;
        float handlingScale = 1f - WobbleLevel * 0.4f;
        PosX += steer * Handling * handlingScale * dt * 60f * (0.3f + Speed * 0.5f);

        // Wobble drift — random lateral push when wobbling
        if (WobbleLevel > 0.2f)
        {
            float time = (float)Time.GetTicksMsec() * 0.001f;
            float drift = Mathf.Sin(time * 11f) * WobbleLevel * 0.12f * Speed * dt * 60f;
            PosX += drift;
        }

        PosX = Mathf.Clamp(PosX, -4.5f, 4.5f);
        Speed = Mathf.Clamp(Speed, 0.02f, MaxSpeed);

        Distance += Speed * dt * 60f;

        float groundY = terrain.HillAt(Distance);
        _main.Player.Position = new Vector3(PosX, groundY + 0.1f, 0);

        // Off-road crash check
        if (Mathf.Abs(PosX) >= TerrainManager.RoadW / 2f)
        {
            Crashed = true;
            Speed = 0f;
        }

        // Board carve — the board points in direction of travel
        if (_skaterRoot != null)
        {
            float targetCarve = Lean * 0.7f * (0.3f + speedFactor * 0.7f);
            _carveAngleSmooth = Mathf.Lerp(_carveAngleSmooth, targetCarve, 6f * dt);
            if (Mathf.Abs(_carveAngleSmooth) < 0.001f) _carveAngleSmooth = 0f;
            float tiltAngle = Lean * 0.35f * (0.5f + speedFactor * 0.5f);

            // Board nod during push-off — brief pitch dip and roll wobble
            float pushNod = 0f;
            float pushTilt = 0f;
            if (PushOffTimer > 0f)
            {
                float pt = PushOffTimer / 0.4f;
                pushNod = Mathf.Sin(pt * Mathf.Pi) * 0.06f;
                pushTilt = Mathf.Sin(pt * Mathf.Pi) * 0.03f;
            }

            // Dynamic wobble driven by WobbleLevel
            float time = (float)Time.GetTicksMsec() * 0.001f;
            float wobbleFreq = 6f + WobbleLevel * 18f;
            float wobbleAmp = 0.008f + WobbleLevel * 0.06f;
            float wobble = Mathf.Sin(time * wobbleFreq) * wobbleAmp * speedFactor;

            _skaterRoot.Rotation = new Vector3(pushNod, _carveAngleSmooth, tiltAngle + wobble + pushTilt);
        }

        // Body stays in sideways stance
        if (_bodyGroup != null)
            _bodyGroup.Rotation = new Vector3(0, -Mathf.Pi / 2f, 0);

        // Procedural animation
        Animate(dt, steer, braking);

        // Dust particles
        _dustParticles.Emitting = Speed > 0.3f && Kicked;
        var dustMat = _dustParticles.ProcessMaterial as ParticleProcessMaterial;
        if (dustMat != null)
        {
            float intensity = Mathf.Clamp(Speed / MaxSpeed, 0.1f, 1f);
            dustMat.Color = new Color(0.6f, 0.55f, 0.4f, 0.3f + intensity * 0.4f);
        }

        // Speed lines — pulse when wobbling
        float spdFactor = Mathf.Clamp(Speed / MaxSpeed, 0f, 1f);
        _speedLines.Emitting = spdFactor > 0.7f && Kicked;
        var speedMat = _speedLines.ProcessMaterial as ParticleProcessMaterial;
        if (speedMat != null)
        {
            float lineIntensity = (spdFactor - 0.7f) / 0.3f;
            float wobbleBoost = 1f + WobbleLevel * 0.5f;
            speedMat.Color = new Color(1f, 1f, 1f, (0.1f + lineIntensity * 0.3f) * wobbleBoost);
        }

        _prevSteer = steer;
    }

    // ═══════════════════════════════════════════
    //  PROCEDURAL ANIMATION
    // ═══════════════════════════════════════════

    private void Animate(float dt, float steer, bool braking)
    {
        float speedFactor = Mathf.Clamp(Speed / MaxSpeed, 0f, 1f);
        float time = (float)Time.GetTicksMsec() * 0.001f;

        // ── Defaults: relaxed riding stance (body faces right, board points forward) ──
        float hipYaw = 0f;
        float hipPitch = 0f;
        float hipRoll = Lean * 0.12f;

        float spinePitch = -0.1f - speedFactor * 0.15f; // lean forward at speed
        float spineRoll = Lean * 0.06f;
        float spineYaw = Lean * 0.15f; // twist into the turn

        float neckPitch = 0.1f; // look ahead
        float neckYaw = -Lean * 0.2f; // head looks into the turn (leads body)
        float neckRoll = 0f;

        // Arms: out for balance, tighter at speed
        float armOutBase = 0.8f - speedFactor * 0.3f;
        float armLPitch = 0.1f;
        float armRPitch = 0.1f;
        float forearmLBend = -0.3f;
        float forearmRBend = -0.3f;

        // Legs: bent knees
        float legLPitch = 0.15f + speedFactor * 0.1f;
        float legRPitch = 0.15f + speedFactor * 0.1f;
        float kneeLBend = -0.3f - speedFactor * 0.15f;
        float kneeRBend = -0.3f - speedFactor * 0.15f;

        // ── Idle sway (when slow/stopped) ──
        if (Speed < 0.3f)
        {
            float sway = Mathf.Sin(time * 1.2f) * 0.04f;
            hipRoll += sway;
            spineRoll += sway * 0.5f;
            armOutBase = 0.5f;
        }

        // ── Speed wobble — body shakes when not carving at high speed ──
        if (WobbleLevel > 0.2f)
        {
            float wobbleShake = (WobbleLevel - 0.2f) / 0.8f;
            float shakeX = Mathf.Sin(time * 13f) * 0.04f * wobbleShake;
            float shakeZ = Mathf.Cos(time * 17f) * 0.03f * wobbleShake;
            hipPitch += shakeX;
            hipRoll += shakeZ;
            spinePitch += shakeX * 0.6f;
            spineRoll += shakeZ * 0.5f;
            armOutBase += wobbleShake * 0.15f;
            armLPitch += Mathf.Sin(time * 15f) * 0.05f * wobbleShake;
            armRPitch -= Mathf.Sin(time * 15f + 1f) * 0.05f * wobbleShake;
        }

        // Arms adjust with steering — outside arm extends, inside arm tucks
        float leanAbs = Mathf.Abs(Lean);
        float turnArmFactor = leanAbs * speedFactor;
        // Left arm: outside when turning right (Lean < 0), inside when turning left (Lean > 0)
        armLPitch -= Lean * 0.12f;
        armRPitch += Lean * 0.12f;
        armOutBase += Lean * 0.08f * speedFactor;

        // Knee compression — inside knee bends more, outside straightens
        if (Lean > 0f)
        {
            kneeLBend -= leanAbs * 0.1f * speedFactor;
            kneeRBend += leanAbs * 0.05f * speedFactor;
        }
        else if (Lean < 0f)
        {
            kneeRBend -= leanAbs * 0.1f * speedFactor;
            kneeLBend += leanAbs * 0.05f * speedFactor;
        }

        // ── Braking ──
        if (braking && Speed > 0.3f)
        {
            // Back foot presses down, torso leans back
            legRPitch += 0.1f;
            kneeRBend -= 0.1f;
            spinePitch += 0.1f;
        }

        // ── Arm sway at speed ──
        float armSway = Mathf.Sin(time * 2.5f) * 0.05f * speedFactor;
        armLPitch += armSway;
        armRPitch -= armSway;

        // ── Lerp all joints toward targets ──
        float lerp = AnimLerp * dt;
        if (_hip != null)
            _hip.Rotation = LerpVec(_hip.Rotation, new Vector3(hipPitch, hipYaw, hipRoll), lerp);
        if (_spine != null)
            _spine.Rotation = LerpVec(_spine.Rotation, new Vector3(spinePitch, spineYaw, spineRoll), lerp);
        if (_neck != null)
            _neck.Rotation = LerpVec(_neck.Rotation, new Vector3(neckPitch, neckYaw, neckRoll), lerp);

        if (_armL != null)
            _armL.Rotation = LerpVec(_armL.Rotation, new Vector3(armLPitch, 0, -armOutBase), lerp);
        if (_forearmL != null)
            _forearmL.Rotation = LerpVec(_forearmL.Rotation, new Vector3(forearmLBend, 0, 0), lerp);
        if (_armR != null)
            _armR.Rotation = LerpVec(_armR.Rotation, new Vector3(armRPitch, 0, armOutBase), lerp);
        if (_forearmR != null)
            _forearmR.Rotation = LerpVec(_forearmR.Rotation, new Vector3(forearmRBend, 0, 0), lerp);

        if (_legL != null)
            _legL.Rotation = LerpVec(_legL.Rotation, new Vector3(legLPitch, 0, 0), lerp);
        if (_kneeL != null)
            _kneeL.Rotation = LerpVec(_kneeL.Rotation, new Vector3(kneeLBend, 0, 0), lerp);
        if (_legR != null)
            _legR.Rotation = LerpVec(_legR.Rotation, new Vector3(legRPitch, 0, 0), lerp);
        if (_kneeR != null)
            _kneeR.Rotation = LerpVec(_kneeR.Rotation, new Vector3(kneeRBend, 0, 0), lerp);
    }

    private static Vector3 LerpVec(Vector3 from, Vector3 to, float t)
    {
        return new Vector3(
            Mathf.Lerp(from.X, to.X, t),
            Mathf.Lerp(from.Y, to.Y, t),
            Mathf.Lerp(from.Z, to.Z, t));
    }

    // ═══════════════════════════════════════════
    //  CONFETTI / RESET / APPLY BOARD
    // ═══════════════════════════════════════════

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
        Crashed = false;
        PushOffTimer = 0f;
        WobbleLevel = 0f;
        TimeSinceCarve = 0f;
        _carveAngleSmooth = 0f;
        _main.Player.Position = new Vector3(0, 0.1f, 0);
        if (_skaterRoot != null) _skaterRoot.Rotation = Vector3.Zero;
        ApplyStance(0f, 0f, false);
        if (_kneeR != null) _kneeR.Rotation = Vector3.Zero;
    }

    public void ApplyBoard()
    {
        var deckMat = new StandardMaterial3D();
        deckMat.AlbedoColor = Main.BoardDeckColors[(int)_main.Board];
        deckMat.Roughness = 0.7f;
        deckMat.SpecularMode = BaseMaterial3D.SpecularModeEnum.Disabled;

        if (_deckMesh != null) _deckMesh.MaterialOverride = deckMat;
        if (_deckNoseMesh != null) _deckNoseMesh.MaterialOverride = deckMat;
        if (_deckTailMesh != null) _deckTailMesh.MaterialOverride = deckMat;

        if (_gripMesh != null)
        {
            var mat = new StandardMaterial3D();
            mat.AlbedoColor = Main.BoardGripColors[(int)_main.Board];
            mat.Roughness = 0.95f;
            _gripMesh.MaterialOverride = mat;
        }
    }

    // ═══════════════════════════════════════════
    //  MESH HELPERS
    // ═══════════════════════════════════════════

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

    private MeshInstance3D AddCylinder(Node3D parent, float topR, float bottomR, float height, StandardMaterial3D mat, Vector3 pos, Vector3 rot)
    {
        var m = new MeshInstance3D();
        var mesh = new CylinderMesh();
        mesh.TopRadius = topR;
        mesh.BottomRadius = bottomR;
        mesh.Height = height;
        mesh.RadialSegments = 16;
        m.Mesh = mesh;
        m.MaterialOverride = mat;
        m.Position = pos;
        m.Rotation = rot;
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
        mesh.RadialSegments = 16;
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
        mesh.Rings = 10;
        mesh.RadialSegments = 16;
        m.Mesh = mesh;
        m.MaterialOverride = mat;
        m.Position = pos;
        parent.AddChild(m);
        return m;
    }
}
