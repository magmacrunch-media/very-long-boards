using Godot;

public class PlayerManager
{
    private Main _main;
    private Node3D _skaterRoot;

    // The board currently under Carl's feet — rebuilt wholesale when the selection changes.
    private Node3D _board;

    // Carl's rig and the joint handles Animate() drives.
    private Node3D _carl;
    private CarlJoints _joints;

    /// <summary>Board deck height above the skater root. Carl stands on the grip tape.</summary>
    private const float DeckY = 0.13f;

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
    private const float AnimLerp = 8f;
    private const float CarvingDrag = 0.01f;

    private const float SteerSmoothRate = 12f;
    private const float YawPerSteer = 0.55f;
    private const float RollPerSteer = 0.35f;

    private const float BrakeSpeedThreshold = 0.7f;
    private const float BrakeCutoff = 0.85f;
    private const float WobbleDecayRate = 1.0f;
    private const float CarveResetTime = 0.5f;
    private const float WobbleCrashLevel = 1f;

    // ── Per-rider handling ──────────────────────────────────────────────
    // Baselines for a hypothetical 3-pip rider; ApplyStats() scales them by the
    // selected Carl's CarlStat. Every derived field is seeded with its baseline so
    // MaxSpeed is never zero — speedFactor divides by it before a ride even starts.
    private const float BaseMaxSpeed = 11f;
    private const float BaseHandling = 0.15f;
    private const float BaseWobbleBuild = 0.35f;
    private const float BaseWobbleThreshold = 0.50f;

    /// <summary>Top speed for the current Carl. Read by the HUD and the chase camera.</summary>
    public float MaxSpeed { get; private set; } = BaseMaxSpeed;
    private float _handling = BaseHandling;
    private float _wobbleBuild = BaseWobbleBuild;
    private float _wobbleOnsetSpeed = BaseMaxSpeed * BaseWobbleThreshold;

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
        ApplyStats();
        CreateBoard();
        CreateBody();
        CreateParticles();
    }

    /// <summary>
    /// Turn the selected Carl's 1-5 pips into the numbers the ride actually uses.
    /// Called at startup and again whenever his look changes on the way into a run.
    /// </summary>
    private void ApplyStats()
    {
        var s = Main.CarlStats[(int)_main.Carl];

        MaxSpeed = BaseMaxSpeed * Pip(s.Speed, 0.82f, 1.18f);
        _handling = BaseHandling * Pip(s.Handling, 0.82f, 1.18f);

        // More trucks means wobble builds slower once it starts...
        _wobbleBuild = BaseWobbleBuild * Pip(s.Tracking, 1.35f, 0.70f);

        // ...and starts later. Note this is an ABSOLUTE speed, deliberately derived from
        // BaseMaxSpeed rather than this rider's MaxSpeed. Gate wobble on a fraction of the
        // rider's own top speed and a fast Carl reaches any given speed at a lower fraction,
        // so raising his SPD would quietly cancel his own TRK penalty.
        _wobbleOnsetSpeed = BaseMaxSpeed * BaseWobbleThreshold * Pip(s.Tracking, 0.85f, 1.15f);
    }

    /// <summary>Map a 1-5 stat pip onto a multiplier range. Three pips lands mid-range.</summary>
    private static float Pip(int pips, float atOne, float atFive)
    {
        return Mathf.Lerp(atOne, atFive, (Mathf.Clamp(pips, 1, 5) - 1) / 4f);
    }

    // ═══════════════════════════════════════════
    //  LONGBOARD
    // ═══════════════════════════════════════════

    private void CreateBoard()
    {
        _skaterRoot = new Node3D();
        _main.Player.AddChild(_skaterRoot);

        _board = BoardBuilder.Build(_main.Board);
        _board.Position = new Vector3(0, DeckY, 0);
        _skaterRoot.AddChild(_board);
    }

    // ═══════════════════════════════════════════
    //  SKATER BODY — joint hierarchy
    // ═══════════════════════════════════════════

    private void CreateBody()
    {
        _carl = CarlBuilder.Build(_main.Carl, out _joints);
        // Origin is at his soles, so this stands him on the grip tape rather than in it.
        _carl.Position = new Vector3(0, DeckY + BoardBuilder.GripTopY, 0);
        _skaterRoot.AddChild(_carl);

        Animate(0f, 0f, false);
    }

    /// <summary>Swap Carl's outfit without disturbing the board or the rig's placement.</summary>
    public void ApplyCarl()
    {
        ApplyStats();

        if (_carl != null)
        {
            _skaterRoot.RemoveChild(_carl);
            _carl.QueueFree();
        }
        CreateBody();
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
        PosX += _steerSmooth * _handling * handlingScale * dt * 60f * (0.3f + Speed * 0.5f);

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
            if (Speed >= _wobbleOnsetSpeed && _timeSinceCarve > CarveResetTime)
            {
                float over = (Speed - _wobbleOnsetSpeed) / Mathf.Max(0.01f, MaxSpeed - _wobbleOnsetSpeed);
                WobbleLevel += _wobbleBuild * dt * (0.5f + Mathf.Clamp(over, 0f, 1f) * 0.5f);
            }
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

        // Procedural animation (the sideways stance is baked into the rig by CarlBuilder)
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
        if (_joints == null) return;
        float lerp = AnimLerp * dt;
        var j = _joints;
        j.Hip.Rotation = LerpVec(j.Hip.Rotation, new Vector3(hipPitch, hipYaw, hipRoll), lerp);
        j.Spine.Rotation = LerpVec(j.Spine.Rotation, new Vector3(spinePitch, spineYaw, spineRoll), lerp);
        j.Neck.Rotation = LerpVec(j.Neck.Rotation, new Vector3(neckPitch, neckYaw, neckRoll), lerp);
        j.ArmL.Rotation = LerpVec(j.ArmL.Rotation, new Vector3(armLPitch, 0, -armOutBase), lerp);
        j.ForearmL.Rotation = LerpVec(j.ForearmL.Rotation, new Vector3(forearmLBend, 0, 0), lerp);
        j.ArmR.Rotation = LerpVec(j.ArmR.Rotation, new Vector3(armRPitch, 0, armOutBase), lerp);
        j.ForearmR.Rotation = LerpVec(j.ForearmR.Rotation, new Vector3(forearmRBend, 0, 0), lerp);
        j.LegL.Rotation = LerpVec(j.LegL.Rotation, new Vector3(legLPitch, 0, 0), lerp);
        j.KneeL.Rotation = LerpVec(j.KneeL.Rotation, new Vector3(kneeLBend, 0, 0), lerp);
        j.LegR.Rotation = LerpVec(j.LegR.Rotation, new Vector3(legRPitch, 0, 0), lerp);
        j.KneeR.Rotation = LerpVec(j.KneeR.Rotation, new Vector3(kneeRBend, 0, 0), lerp);
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

    /// <summary>
    /// Rebuild the board from the current selection. A full rebuild rather than a recolour,
    /// so shape differences (the Natural deck is wider) carry over from the garage rack too.
    /// </summary>
    public void ApplyBoard()
    {
        if (_board != null)
        {
            _skaterRoot.RemoveChild(_board);
            _board.QueueFree();
        }

        _board = BoardBuilder.Build(_main.Board);
        _board.Position = new Vector3(0, DeckY, 0);
        // Keep the board under Carl rather than in front of him in the child list.
        _skaterRoot.AddChild(_board);
        _skaterRoot.MoveChild(_board, 0);
    }

    /// <summary>
    /// Show/hide the rider without touching the Player node itself — CameraMount hangs off
    /// Player, and Godot disables a Camera3D that isn't visible in the tree.
    /// </summary>
    public void SetVisible(bool visible)
    {
        if (_skaterRoot != null) _skaterRoot.Visible = visible;
        if (_dustParticles != null) _dustParticles.Visible = visible;
        if (_speedLines != null) _speedLines.Visible = visible;
        if (_confettiParticles != null) _confettiParticles.Visible = visible;
    }
}
