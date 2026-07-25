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

    // Joint hierarchy
    private Node3D _bodyGroup;
    private Node3D _hip;
    private Node3D _spine;
    private Node3D _neck;
    private Node3D _armL;
    private Node3D _forearmL;
    private Node3D _armR;
    private Node3D _forearmR;
    private Node3D _legL;
    private Node3D _kneeL;
    private Node3D _legR;
    private Node3D _kneeR;

    // Public state (read by Main, UI, Camera)
    public float Speed = 0f;
    public float PosX = 0f;
    public float Distance = 0f;
    public bool Kicked = false;
    public bool Crashed = false;
    public float PushOffTimer = 0f;
    public float SteerSmooth => _steerSmooth;

    // Internal state
    private float _steerSmooth = 0f;
    private float _boardYaw = 0f;
    private float _boardRoll = 0f;
    private float _boardPitch = 0f;
    public float WobbleLevel = 0f;
    private float _timeSinceCarve = 0f;

    // Constants
    private const float Gravity = 0.05f;
    private const float Friction = 0.9997f;
    public const float MaxSpeed = 11f;
    private const float Handling = 0.15f;
    private const float AnimLerp = 8f;
    private const float CarvingDrag = 0.01f;

    private const float SteerSmoothRate = 12f;
    private const float YawPerSteer = 0.55f;
    private const float RollPerSteer = 0.35f;

    private const float WobbleSpeedThreshold = 0.50f;
    private const float BrakeSpeedThreshold = 0.7f;
    private const float BrakeCutoff = 0.85f;
    private const float WobbleBuildRate = 0.35f;
    private const float WobbleDecayRate = 1.0f;
    private const float CarveResetTime = 0.5f;
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
        deckMat.SpecularMode = BaseMaterial3D.SpecularModeEnum.Disabled;

        _deckMesh = new MeshInstance3D();
        var deckCenter = new BoxMesh();
        deckCenter.Size = new Vector3(0.62f, 0.045f, 1.4f);
        _deckMesh.Mesh = deckCenter;
        _deckMesh.MaterialOverride = deckMat;
        _deckMesh.Position = new Vector3(0, 0.13f, 0);
        _skaterRoot.AddChild(_deckMesh);

        _deckNoseMesh = new MeshInstance3D();
        var deckNose = new BoxMesh();
        deckNose.Size = new Vector3(0.48f, 0.04f, 0.4f);
        _deckNoseMesh.Mesh = deckNose;
        _deckNoseMesh.MaterialOverride = deckMat;
        _deckNoseMesh.Position = new Vector3(0, 0.13f, 0.9f);
        _skaterRoot.AddChild(_deckNoseMesh);

        _deckTailMesh = new MeshInstance3D();
        var deckTail = new BoxMesh();
        deckTail.Size = new Vector3(0.48f, 0.04f, 0.35f);
        _deckTailMesh.Mesh = deckTail;
        _deckTailMesh.MaterialOverride = deckMat;
        _deckTailMesh.Position = new Vector3(0, 0.13f, -0.88f);
        _skaterRoot.AddChild(_deckTailMesh);

        var gripMat = new StandardMaterial3D();
        gripMat.AlbedoColor = Main.BoardGripColors[(int)_main.Board];
        _gripMesh = new MeshInstance3D();
        var gripMesh = new BoxMesh();
        gripMesh.Size = new Vector3(0.58f, 0.015f, 1.3f);
        _gripMesh.Mesh = gripMesh;
        _gripMesh.MaterialOverride = gripMat;
        _gripMesh.Position = new Vector3(0, 0.16f, 0);
        _skaterRoot.AddChild(_gripMesh);

        var truckMat = new StandardMaterial3D();
        truckMat.AlbedoColor = new Color(0.62f, 0.62f, 0.65f);

        AddBox(_skaterRoot, new Vector3(0.18f, 0.04f, 0.14f), truckMat, new Vector3(0, 0.08f, 0.55f));
        var axleMat = new StandardMaterial3D();
        axleMat.AlbedoColor = new Color(0.55f, 0.55f, 0.58f);
        AddCylinder(_skaterRoot, 0.015f, 0.015f, 0.58f, axleMat, new Vector3(0, 0.06f, 0.55f), new Vector3(0, 0, Mathf.Pi / 2f));

        AddBox(_skaterRoot, new Vector3(0.18f, 0.04f, 0.14f), truckMat, new Vector3(0, 0.08f, -0.55f));
        AddCylinder(_skaterRoot, 0.015f, 0.015f, 0.58f, axleMat, new Vector3(0, 0.06f, -0.55f), new Vector3(0, 0, Mathf.Pi / 2f));

        var wheelMat = new StandardMaterial3D();
        wheelMat.AlbedoColor = new Color(0.12f, 0.12f, 0.12f);

        var hubMat = new StandardMaterial3D();
        hubMat.AlbedoColor = new Color(0.45f, 0.45f, 0.48f);

        foreach (var pos in new[] {
            new Vector3(-0.30f, 0.03f, 0.55f), new Vector3(0.30f, 0.03f, 0.55f),
            new Vector3(-0.30f, 0.03f, -0.55f), new Vector3(0.30f, 0.03f, -0.55f) })
        {
            var wheel = new MeshInstance3D();
            var wMesh = new CylinderMesh();
            wMesh.TopRadius = 0.055f;
            wMesh.BottomRadius = 0.055f;
            wMesh.Height = 0.07f;
            wMesh.RadialSegments = 6;
            wheel.Mesh = wMesh;
            wheel.MaterialOverride = wheelMat;
            wheel.Position = pos;
            wheel.Rotation = new Vector3(0, 0, Mathf.Pi / 2f);
            _skaterRoot.AddChild(wheel);

            var hub = new MeshInstance3D();
            var hMesh = new CylinderMesh();
            hMesh.TopRadius = 0.025f;
            hMesh.BottomRadius = 0.025f;
            hMesh.Height = 0.075f;
            hMesh.RadialSegments = 6;
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
        var skinMat = new StandardMaterial3D();
        skinMat.AlbedoColor = new Color(0.9f, 0.78f, 0.6f);
        skinMat.SpecularMode = BaseMaterial3D.SpecularModeEnum.Disabled;

        var shoeMat = new StandardMaterial3D();
        shoeMat.AlbedoColor = new Color(0.14f, 0.14f, 0.14f);

        var soleMat = new StandardMaterial3D();
        soleMat.AlbedoColor = new Color(0.08f, 0.08f, 0.08f);

        var pantsMat = new StandardMaterial3D();
        pantsMat.AlbedoColor = Main.CarlPantsColors[(int)_main.Carl];
        pantsMat.SpecularMode = BaseMaterial3D.SpecularModeEnum.Disabled;

        var shirtMat = new StandardMaterial3D();
        shirtMat.AlbedoColor = Main.CarlShirtColors[(int)_main.Carl];
        shirtMat.SpecularMode = BaseMaterial3D.SpecularModeEnum.Disabled;

        var hairMat = new StandardMaterial3D();
        hairMat.AlbedoColor = new Color(0.3f, 0.18f, 0.08f);

        _bodyGroup = new Node3D();
        _bodyGroup.Position = new Vector3(0, 0.05f, 0);
        _bodyGroup.Rotation = new Vector3(0, -Mathf.Pi / 2f, 0);
        _skaterRoot.AddChild(_bodyGroup);

        _hip = new Node3D();
        _hip.Position = new Vector3(0, 0.2f, 0);
        _bodyGroup.AddChild(_hip);

        _spine = new Node3D();
        _spine.Position = new Vector3(0, 0.35f, 0);
        _hip.AddChild(_spine);

        AddCylinderTo(_spine, 0.13f, 0.11f, 0.18f, shirtMat, new Vector3(0, 0.09f, 0));
        AddCylinderTo(_spine, 0.15f, 0.13f, 0.22f, shirtMat, new Vector3(0, 0.29f, -0.02f));
        AddCylinderTo(_spine, 0.16f, 0.15f, 0.06f, shirtMat, new Vector3(0, 0.42f, -0.02f));

        _neck = new Node3D();
        _neck.Position = new Vector3(0, 0.46f, -0.02f);
        _spine.AddChild(_neck);
        AddCylinderTo(_neck, 0.05f, 0.06f, 0.08f, skinMat, new Vector3(0, 0.04f, 0));

        var head = new MeshInstance3D();
        var headMesh = new SphereMesh();
        headMesh.Radius = 0.12f;
        headMesh.Height = 0.24f;
        headMesh.Rings = 6;
        headMesh.RadialSegments = 8;
        head.Mesh = headMesh;
        head.MaterialOverride = skinMat;
        head.Position = new Vector3(0, 0.14f, -0.02f);
        _neck.AddChild(head);

        var hair = new MeshInstance3D();
        var hairMesh = new CylinderMesh();
        hairMesh.TopRadius = 0.11f;
        hairMesh.BottomRadius = 0.13f;
        hairMesh.Height = 0.06f;
        hairMesh.RadialSegments = 8;
        hair.Mesh = hairMesh;
        hair.MaterialOverride = hairMat;
        hair.Position = new Vector3(0, 0.24f, -0.02f);
        _neck.AddChild(hair);

        _armL = new Node3D();
        _armL.Position = new Vector3(-0.17f, 0.4f, -0.02f);
        _spine.AddChild(_armL);
        AddCylinderTo(_armL, 0.035f, 0.03f, 0.2f, skinMat, new Vector3(0, -0.1f, 0));
        _forearmL = new Node3D();
        _forearmL.Position = new Vector3(0, -0.2f, 0);
        _armL.AddChild(_forearmL);
        AddCylinderTo(_forearmL, 0.03f, 0.025f, 0.18f, skinMat, new Vector3(0, -0.09f, 0));
        AddSphereTo(_forearmL, 0.03f, skinMat, new Vector3(0, -0.2f, 0));

        _armR = new Node3D();
        _armR.Position = new Vector3(0.17f, 0.4f, -0.02f);
        _spine.AddChild(_armR);
        AddCylinderTo(_armR, 0.035f, 0.03f, 0.2f, skinMat, new Vector3(0, -0.1f, 0));
        _forearmR = new Node3D();
        _forearmR.Position = new Vector3(0, -0.2f, 0);
        _armR.AddChild(_forearmR);
        AddCylinderTo(_forearmR, 0.03f, 0.025f, 0.18f, skinMat, new Vector3(0, -0.09f, 0));
        AddSphereTo(_forearmR, 0.03f, skinMat, new Vector3(0, -0.2f, 0));

        _legL = new Node3D();
        _legL.Position = new Vector3(-0.08f, 0.0f, 0);
        _hip.AddChild(_legL);
        AddCylinderTo(_legL, 0.06f, 0.055f, 0.22f, pantsMat, new Vector3(0, -0.11f, 0));
        _kneeL = new Node3D();
        _kneeL.Position = new Vector3(0, -0.22f, 0);
        _legL.AddChild(_kneeL);
        AddCylinderTo(_kneeL, 0.05f, 0.045f, 0.2f, pantsMat, new Vector3(0, -0.1f, 0));
        var shoeL = new MeshInstance3D();
        var shoeLMesh = new BoxMesh();
        shoeLMesh.Size = new Vector3(0.1f, 0.06f, 0.22f);
        shoeL.Mesh = shoeLMesh;
        shoeL.MaterialOverride = shoeMat;
        shoeL.Position = new Vector3(0, -0.22f, -0.02f);
        _kneeL.AddChild(shoeL);
        AddBoxTo(_kneeL, new Vector3(0.1f, 0.02f, 0.22f), soleMat, new Vector3(0, -0.25f, -0.02f));

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

        var col = new CollisionShape3D();
        var shape = new BoxShape3D();
        shape.Size = new Vector3(0.7f, 1.3f, 1.8f);
        col.Shape = shape;
        col.Position = new Vector3(0, 0.65f, 0);
        _main.Player.AddChild(col);

        Animate(0f, 0f, false);
    }

    // ═══════════════════════════════════════════
    //  PARTICLES
    // ═══════════════════════════════════════════

    private void CreateParticles()
    {
        _dustParticles = new GpuParticles3D();
        _dustParticles.Amount = 20;
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

        _confettiParticles = new GpuParticles3D();
        _confettiParticles.Amount = 50;
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

        _speedLines = new GpuParticles3D();
        _speedLines.Amount = 15;
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

        // ── Push off ──
        if (Input.IsActionJustPressed("kick_off") && Speed < 0.3f)
        {
            Speed += 0.5f;
            Kicked = true;
            PushOffTimer = 0.4f;
            _steerSmooth = 0f;
            _boardYaw = 0f;
            _boardRoll = 0f;
        }
        PushOffTimer = Mathf.Max(0f, PushOffTimer - dt);

        // ── Slope & friction ──
        Speed += -slope * Gravity * dt * 60f;
        Speed *= Mathf.Pow(Friction, dt * 60f);
        float speedFactor = Mathf.Clamp(Speed / MaxSpeed, 0f, 1f);

        // ── Braking ──
        if (braking)
        {
            if (speedFactor < BrakeSpeedThreshold)
                Speed *= Mathf.Pow(0.95f, dt * 60f);
            else if (speedFactor < BrakeCutoff)
            {
                float brakeFade = 1f - (speedFactor - BrakeSpeedThreshold) / (BrakeCutoff - BrakeSpeedThreshold);
                Speed *= Mathf.Pow(Mathf.Lerp(1f, 0.95f, brakeFade), dt * 60f);
            }
        }

        // ── Shoulder drag ──
        if (Mathf.Abs(PosX) > 3.5f)
            Speed *= Mathf.Pow(0.97f, dt * 60f);

        // ── Carving drag — steering bleeds speed (quadratic: gentle carving negligible, hard carving significant) ──
        float steerAmount = _steerSmooth * _steerSmooth;
        float carveDrag = steerAmount * CarvingDrag * speedFactor;
        Speed *= Mathf.Pow(1f - carveDrag, dt * 60f);

        // ── Steering ──
        float targetSteer = (PushOffTimer > 0f) ? 0f : steer;
        _steerSmooth = Mathf.Lerp(_steerSmooth, targetSteer, SteerSmoothRate * dt);
        if (Mathf.Abs(_steerSmooth) < 0.005f) _steerSmooth = 0f;

        float handlingScale = 1f - WobbleLevel * 0.4f;
        PosX += _steerSmooth * Handling * handlingScale * dt * 60f * (0.3f + Speed * 0.5f);

        // ── Wobble ──
        bool isSteering = Mathf.Abs(_steerSmooth) > 0.1f;
        if (isSteering)
        {
            _timeSinceCarve = 0f;
            WobbleLevel = Mathf.Max(0f, WobbleLevel - WobbleDecayRate * dt);
        }
        else
        {
            _timeSinceCarve += dt;
            if (speedFactor >= WobbleSpeedThreshold && _timeSinceCarve > CarveResetTime)
                WobbleLevel += WobbleBuildRate * dt * (0.5f + (speedFactor - WobbleSpeedThreshold) / (1f - WobbleSpeedThreshold) * 0.5f);
            else
                WobbleLevel = Mathf.Max(0f, WobbleLevel - WobbleDecayRate * 2f * dt);
        }
        WobbleLevel = Mathf.Clamp(WobbleLevel, 0f, 1f);

        if (WobbleLevel >= WobbleCrashLevel)
        {
            Crashed = true;
            Speed = 0f;
        }

        if (WobbleLevel > 0.3f)
        {
            float time = (float)Time.GetTicksMsec() * 0.001f;
            float drift = Mathf.Sin(time * 11f) * WobbleLevel * 0.08f * Speed * dt * 60f;
            PosX += drift;
        }

        PosX = Mathf.Clamp(PosX, -4.5f, 4.5f);
        Speed = Mathf.Clamp(Speed, 0.02f, MaxSpeed);
        Distance += Speed * dt * 60f;

        float groundY = terrain.HillAt(Distance);
        float playerY = groundY + 0.045f + Mathf.Sin(Mathf.Abs(_boardPitch)) * 0.55f + Mathf.Sin(Mathf.Abs(_boardRoll)) * 0.3f;
        _main.Player.Position = new Vector3(PosX, playerY, 0);

        if (Mathf.Abs(PosX) >= TerrainManager.RoadW / 2f)
        {
            Crashed = true;
            Speed = 0f;
        }

        // ── Board rotation ──
        if (_skaterRoot != null)
        {
            float time = (float)Time.GetTicksMsec() * 0.001f;

            // Board pitch tracks terrain slope — communicates acceleration to the player
            float pitchTarget = -slope * 1.2f;
            _boardPitch = Mathf.Lerp(_boardPitch, pitchTarget, 6f * dt);

            // Steering yaw & roll — responsive carving
            float yawTarget = _steerSmooth * YawPerSteer * (0.3f + speedFactor * 0.7f);
            float rollTarget = _steerSmooth * RollPerSteer * (0.3f + speedFactor * 0.7f);
            if (Mathf.Abs(_steerSmooth) < 0.05f)
            {
                _boardYaw = MoveToward(_boardYaw, 0f, 10f * dt);
                _boardRoll = MoveToward(_boardRoll, 0f, 12f * dt);
            }
            else
            {
                _boardYaw = Mathf.Lerp(_boardYaw, yawTarget, 15f * dt);
                _boardRoll = Mathf.Lerp(_boardRoll, rollTarget, 15f * dt);
            }
            if (Mathf.Abs(_boardYaw) < 0.001f) _boardYaw = 0f;
            if (Mathf.Abs(_boardRoll) < 0.001f) _boardRoll = 0f;

            // Wobble roll — more dramatic at high speed
            float wobbleRoll = 0f;
            if (WobbleLevel > 0.01f)
            {
                float wobbleFreq = 8f + WobbleLevel * 14f;
                float wobbleAmp = WobbleLevel * 0.08f;
                wobbleRoll = Mathf.Sin(time * wobbleFreq) * wobbleAmp * speedFactor;
            }

            // Road direction — board yaw aligns with the road's curve
            float roadYaw = terrain.CurveAt(Distance);
            float finalPitch = (PushOffTimer > 0f) ? 0f : _boardPitch;
            float finalYaw = (PushOffTimer > 0f) ? 0f : roadYaw + _boardYaw;
            float finalRoll = (PushOffTimer > 0f) ? 0f : _boardRoll + wobbleRoll;
            _skaterRoot.Rotation = new Vector3(finalPitch, finalYaw, finalRoll);
        }

        // Body sideways stance
        if (_bodyGroup != null)
            _bodyGroup.Rotation = new Vector3(0, -Mathf.Pi / 2f, 0);

        // Procedural animation
        Animate(dt, steer, braking);

        // Dust — always on when moving
        _dustParticles.Emitting = Speed > 0.3f;
        var dustMat2 = _dustParticles.ProcessMaterial as ParticleProcessMaterial;
        if (dustMat2 != null)
        {
            float intensity = Mathf.Clamp(Speed / MaxSpeed, 0.1f, 1f);
            dustMat2.Color = new Color(0.6f, 0.55f, 0.4f, 0.2f + intensity * 0.5f);
        }

        // Speed lines — kick in at 50% speed
        float spdFactor = Mathf.Clamp(Speed / MaxSpeed, 0f, 1f);
        _speedLines.Emitting = spdFactor > 0.5f;
        var speedMat2 = _speedLines.ProcessMaterial as ParticleProcessMaterial;
        if (speedMat2 != null)
        {
            float lineIntensity = Mathf.Clamp((spdFactor - 0.5f) / 0.5f, 0f, 1f);
            float wobbleBoost = 1f + WobbleLevel * 0.5f;
            speedMat2.Color = new Color(1f, 1f, 1f, (0.15f + lineIntensity * 0.35f) * wobbleBoost);
        }
    }

    private static float MoveToward(float current, float target, float maxDelta)
    {
        if (Mathf.Abs(target - current) <= maxDelta) return target;
        return current + Mathf.Sign(target - current) * maxDelta;
    }

    // ═══════════════════════════════════════════
    //  PROCEDURAL ANIMATION
    // ═══════════════════════════════════════════

    private void Animate(float dt, float steer, bool braking)
    {
        float speedFactor = Mathf.Clamp(Speed / MaxSpeed, 0f, 1f);
        float time = (float)Time.GetTicksMsec() * 0.001f;
        float lean = _steerSmooth;

        float hipPitch = 0f;
        float hipYaw = 0f;
        float hipRoll = lean * 0.1f;

        float spinePitch = -0.1f - speedFactor * 0.15f;
        float spineRoll = lean * 0.1f;
        float spineYaw = lean * 0.18f;

        float neckPitch = 0.1f;
        float neckYaw = -lean * 0.15f;
        float neckRoll = 0f;

        float armOutBase = 0.8f - speedFactor * 0.3f;
        float armLPitch = 0.1f;
        float armRPitch = 0.1f;
        float forearmLBend = -0.3f;
        float forearmRBend = -0.3f;

        float legLPitch = 0.15f + speedFactor * 0.1f;
        float legRPitch = 0.15f + speedFactor * 0.1f;
        float kneeLBend = -0.3f - speedFactor * 0.15f;
        float kneeRBend = -0.3f - speedFactor * 0.15f;

        // Idle sway — only when truly stopped
        if (Speed < 0.15f && WobbleLevel < 0.01f)
        {
            float sway = Mathf.Sin(time * 1.2f) * 0.01f;
            hipRoll += sway;
            armOutBase = 0.5f;
        }

        // Push-off kick animation — back leg extends, torso leans forward, arms swing
        if (PushOffTimer > 0f)
        {
            float pt = PushOffTimer / 0.4f;
            float kick = Mathf.Sin(pt * Mathf.Pi) * 0.45f;
            legRPitch += kick;
            kneeRBend += kick * 0.5f;
            spinePitch -= kick * 0.2f;
            // Arms swing forward during kick
            armLPitch += kick * 0.3f;
            armRPitch += kick * 0.3f;
            armOutBase -= kick * 0.2f;
        }

        // Wobble shake
        if (WobbleLevel > 0.3f)
        {
            float wobbleShake = (WobbleLevel - 0.3f) / 0.7f;
            hipPitch += Mathf.Sin(time * 13f) * 0.015f * wobbleShake;
            hipRoll += Mathf.Cos(time * 17f) * 0.01f * wobbleShake;
            spinePitch += Mathf.Sin(time * 13f) * 0.01f * wobbleShake;
            spineRoll += Mathf.Cos(time * 17f) * 0.008f * wobbleShake;
            armOutBase += wobbleShake * 0.08f;
            armLPitch += Mathf.Sin(time * 15f) * 0.02f * wobbleShake;
            armRPitch -= Mathf.Sin(time * 15f + 1f) * 0.02f * wobbleShake;
        }

        // Steering body adjustments
        float leanAbs = Mathf.Abs(lean);
        armLPitch -= lean * 0.18f;
        armRPitch += lean * 0.18f;
        armOutBase += lean * 0.12f * speedFactor;

        if (lean > 0f)
        {
            kneeLBend -= leanAbs * 0.14f * speedFactor;
            kneeRBend += leanAbs * 0.07f * speedFactor;
        }
        else if (lean < 0f)
        {
            kneeRBend -= leanAbs * 0.14f * speedFactor;
            kneeLBend += leanAbs * 0.07f * speedFactor;
        }

        // Braking
        if (braking && Speed > 0.3f)
        {
            legRPitch += 0.1f;
            kneeRBend -= 0.1f;
            spinePitch += 0.1f;
        }

        // Lerp joints
        float lerp = AnimLerp * dt;
        if (_hip != null) _hip.Rotation = LerpVec(_hip.Rotation, new Vector3(hipPitch, hipYaw, hipRoll), lerp);
        if (_spine != null) _spine.Rotation = LerpVec(_spine.Rotation, new Vector3(spinePitch, spineYaw, spineRoll), lerp);
        if (_neck != null) _neck.Rotation = LerpVec(_neck.Rotation, new Vector3(neckPitch, neckYaw, neckRoll), lerp);
        if (_armL != null) _armL.Rotation = LerpVec(_armL.Rotation, new Vector3(armLPitch, 0, -armOutBase), lerp);
        if (_forearmL != null) _forearmL.Rotation = LerpVec(_forearmL.Rotation, new Vector3(forearmLBend, 0, 0), lerp);
        if (_armR != null) _armR.Rotation = LerpVec(_armR.Rotation, new Vector3(armRPitch, 0, armOutBase), lerp);
        if (_forearmR != null) _forearmR.Rotation = LerpVec(_forearmR.Rotation, new Vector3(forearmRBend, 0, 0), lerp);
        if (_legL != null) _legL.Rotation = LerpVec(_legL.Rotation, new Vector3(legLPitch, 0, 0), lerp);
        if (_kneeL != null) _kneeL.Rotation = LerpVec(_kneeL.Rotation, new Vector3(kneeLBend, 0, 0), lerp);
        if (_legR != null) _legR.Rotation = LerpVec(_legR.Rotation, new Vector3(legRPitch, 0, 0), lerp);
        if (_kneeR != null) _kneeR.Rotation = LerpVec(_kneeR.Rotation, new Vector3(kneeRBend, 0, 0), lerp);
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
        Crashed = false;
        PushOffTimer = 0f;
        WobbleLevel = 0f;
        _timeSinceCarve = 0f;
        _steerSmooth = 0f;
        _boardYaw = 0f;
        _boardRoll = 0f;
        _boardPitch = 0f;
        _main.Player.Position = new Vector3(0, 0.1f, 0);
        if (_skaterRoot != null) _skaterRoot.Rotation = Vector3.Zero;
        Animate(0f, 0f, false);
    }

    public void ApplyBoard()
    {
        var deckMat = new StandardMaterial3D();
        deckMat.AlbedoColor = Main.BoardDeckColors[(int)_main.Board];
        deckMat.SpecularMode = BaseMaterial3D.SpecularModeEnum.Disabled;

        if (_deckMesh != null) _deckMesh.MaterialOverride = deckMat;
        if (_deckNoseMesh != null) _deckNoseMesh.MaterialOverride = deckMat;
        if (_deckTailMesh != null) _deckTailMesh.MaterialOverride = deckMat;

        if (_gripMesh != null)
        {
            var mat = new StandardMaterial3D();
            mat.AlbedoColor = Main.BoardGripColors[(int)_main.Board];
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
        mesh.RadialSegments = 8;
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
        mesh.RadialSegments = 8;
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
        mesh.Rings = 6;
        mesh.RadialSegments = 8;
        m.Mesh = mesh;
        m.MaterialOverride = mat;
        m.Position = pos;
        parent.AddChild(m);
        return m;
    }
}
