using Godot;

/// <summary>
/// The mix. Six looping beds and a small pool of one-shot players, all fed by
/// <see cref="AudioKit"/> and all levelled by <see cref="AudioDesign"/>.
///
/// The beds are started once and never stopped — restarting a loop is what puts a click in it.
/// What changes is their volume and pitch, chased every frame toward a target read straight off
/// the ride: speed, wobble level, which surface a wheel is on, whether a foot is down. Nothing
/// else has to remember to tell the audio anything, which is the same hub-and-spoke deal every
/// other manager here has with <see cref="Main"/>.
///
/// One-shots are the exception, because a kick or a crash is an event and not a state. Those
/// are pushed in by whoever knows it happened.
/// </summary>
public class AudioManager
{
    /// <summary>Events, as opposed to the beds, which are conditions.</summary>
    public enum Sfx { Kick, Crash, Finish, Beep, Go, Move, Confirm, Back, Deny, Bird }

    /// <summary>How many one-shots can overlap. A crash lands on top of the last kick; four is plenty.</summary>
    private const int ShotVoices = 4;

    /// <summary>
    /// Linear gain below which a player is muted outright. LinearToDb(0) is negative infinity,
    /// and a bed left at -200 dB still costs a mix slot every frame.
    /// </summary>
    private const float Silence = 0.0008f;

    private readonly Main _main;
    private AudioDesign _mix;

    private AudioStreamPlayer _rumble, _gravel, _wind, _rattle, _scrub, _ambience;
    private AudioStreamPlayer[] _shots;
    private int _nextShot;

    // Current linear gains, chased toward the targets Update() computes.
    private float _gRumble, _gGravel, _gWind, _gRattle, _gScrub, _gAmbience;

    private readonly RandomNumberGenerator _rng = new RandomNumberGenerator();
    private float _birdTimer;

    public AudioManager(Main main)
    {
        _main = main;
    }

    public void Create()
    {
        _mix = _main.AudioMix ?? new AudioDesign();
        _rng.Randomize();

        _rumble = Bed(AudioKit.Rumble);
        _gravel = Bed(AudioKit.Gravel);
        _wind = Bed(AudioKit.Wind);
        _rattle = Bed(AudioKit.Rattle);
        _scrub = Bed(AudioKit.Scrub);
        _ambience = Bed(AudioKit.Ambience);

        _shots = new AudioStreamPlayer[ShotVoices];
        for (int i = 0; i < ShotVoices; i++)
        {
            _shots[i] = new AudioStreamPlayer();
            _main.AddChild(_shots[i]);
        }

        _birdTimer = NextBirdGap();
    }

    /// <summary>A bed: playing from the first frame, silent until the mix asks for it.</summary>
    private AudioStreamPlayer Bed(AudioStream stream)
    {
        var p = new AudioStreamPlayer();
        p.Stream = stream;
        p.VolumeDb = -80f;
        _main.AddChild(p);
        p.Play();
        return p;
    }

    // ═══════════════════════════════════════════
    //  MIX
    // ═══════════════════════════════════════════

    /// <summary>
    /// Called once a frame from <see cref="Main"/>, before the state machine runs. Everything
    /// here is read, not pushed: the mix is a view of the game state rather than a thing the
    /// game state has to maintain.
    /// </summary>
    public void Update(float dt)
    {
        var p = _main.PlayerMgr;
        var state = _main.State;

        // Two different questions, and they part company on the finish screen.
        //
        // OnRoad is "which world am I in", and decides the ambience. Rolling is "is Carl
        // actually moving", and decides the wheels, the wind and everything else the ride
        // makes. Crossing the line freezes the world without touching Speed — PlayerManager
        // simply stops being updated — so a mix that read Speed in the Finished state would
        // hold the wheels at whatever you crossed the line doing, howling under the jingle,
        // for as long as you left the screen up. Crashing does not show it: that path zeroes
        // Speed on the way out.
        bool onRoad = state == Main.GameState.Countdown || state == Main.GameState.Riding ||
                      state == Main.GameState.Paused || state == Main.GameState.Finished;
        bool rolling = state == Main.GameState.Countdown || state == Main.GameState.Riding ||
                       state == Main.GameState.Paused;

        // Speed as a fraction of this Carl's top speed — the same number the HUD, the camera
        // and the particles all read, so the ride sounds as fast as it looks.
        float v = rolling ? Mathf.Clamp(p.Speed / Mathf.Max(0.01f, p.MaxSpeed), 0f, 1f) : 0f;
        if (v < _mix.WheelSilenceBelow) v = 0f;

        // Wheels. Rolling noise is broadly linear in speed, but its loudness is not: the square
        // root gets the wheels audible the moment Carl is moving instead of leaving the first
        // fifty metres of every run silent.
        float wheels = Mathf.Sqrt(v);
        float wheelPitch = Mathf.Lerp(_mix.WheelPitchLow, _mix.WheelPitchHigh, v);

        // Tarmac or shoulder. Crossfaded rather than switched, because PosX slides across the
        // boundary and a hard cut there would click on every drift.
        float offRoad = rolling && p.OnShoulder ? 1f : 0f;

        float rumbleTarget = wheels * (1f - offRoad) * Db(_mix.RumbleDb);
        float gravelTarget = wheels * offRoad * Db(_mix.GravelDb);

        // Wind on speed squared — the same v^2 the drag term takes it out of.
        float windTarget = v * v * Db(_mix.WindDb);
        float windPitch = Mathf.Lerp(_mix.WindPitchLow, _mix.WindPitchHigh, v);

        // Rattle. Deliberately steeper than linear so the warning arrives late and then fast,
        // which is what makes it a warning rather than a background hum.
        float wob = rolling ? p.WobbleLevel : 0f;
        float rattleTarget = wob * wob * Db(_mix.RattleDb);
        float rattlePitch = Mathf.Lerp(_mix.RattlePitchLow, _mix.RattlePitchHigh, wob);

        // Scrub is a foot brake or a carve hard enough to break the wheels loose. Whichever is
        // louder wins; they are the same sound and doubling it just makes it twice as loud.
        float carve = p.SteerSmooth * p.SteerSmooth * v;
        float scrub = rolling ? Mathf.Max(p.BrakeBite * (0.3f + 0.7f * v), carve * 0.55f) : 0f;
        float scrubTarget = scrub * Db(_mix.ScrubDb);

        // The afternoon. Full outside the garage, gone on the road, and back at half once you
        // have stopped at the bottom — you are still standing on a country road.
        float ambienceTarget = Db(_mix.AmbienceDb) *
            (state == Main.GameState.Finished ? 0.55f : onRoad ? 0f : 1f);

        if (state == Main.GameState.Paused)
        {
            float duck = _mix.PauseDuck;
            rumbleTarget *= duck; gravelTarget *= duck; windTarget *= duck;
            rattleTarget *= duck; scrubTarget *= duck; ambienceTarget *= duck;
        }

        float ride = 1f - Mathf.Exp(-_mix.RideFadeRate * dt);
        float air = 1f - Mathf.Exp(-_mix.AmbienceFadeRate * dt);

        _gRumble = Mathf.Lerp(_gRumble, rumbleTarget, ride);
        _gGravel = Mathf.Lerp(_gGravel, gravelTarget, ride);
        _gWind = Mathf.Lerp(_gWind, windTarget, ride);
        _gRattle = Mathf.Lerp(_gRattle, rattleTarget, ride);
        _gScrub = Mathf.Lerp(_gScrub, scrubTarget, ride);
        _gAmbience = Mathf.Lerp(_gAmbience, ambienceTarget, air);

        Set(_rumble, _gRumble, wheelPitch);
        Set(_gravel, _gGravel, wheelPitch);
        Set(_wind, _gWind, windPitch);
        Set(_rattle, _gRattle, rattlePitch);
        Set(_scrub, _gScrub, 1f);
        Set(_ambience, _gAmbience, 1f);

        UpdateBirds(dt);
    }

    /// <summary>
    /// A bird every few seconds while the afternoon is actually audible. Gated on the faded
    /// gain rather than on the game state so the birds stop as you ride away, not the instant
    /// the road appears.
    /// </summary>
    private void UpdateBirds(float dt)
    {
        if (_gAmbience < Db(_mix.AmbienceDb) * 0.5f)
            return;

        _birdTimer -= dt;
        if (_birdTimer > 0f)
            return;

        _birdTimer = NextBirdGap();
        Play(Sfx.Bird, _rng.RandfRange(_mix.BirdPitchLow, _mix.BirdPitchHigh),
             Db(_mix.BirdDb) / Mathf.Max(1e-6f, Db(_mix.SfxDb)));
    }

    private float NextBirdGap()
    {
        return _rng.RandfRange(_mix.BirdGapMin, Mathf.Max(_mix.BirdGapMin, _mix.BirdGapMax));
    }

    /// <summary>Apply a linear gain and a pitch, muting outright below the noise floor.</summary>
    private void Set(AudioStreamPlayer player, float gain, float pitch)
    {
        float g = gain * Db(_mix.MasterDb);
        if (g < Silence)
        {
            player.VolumeDb = -80f;
            return;
        }
        player.VolumeDb = Mathf.LinearToDb(g);
        player.PitchScale = Mathf.Max(0.05f, pitch);
    }

    /// <summary>
    /// Stop every player before the engine tears the audio server down.
    ///
    /// A bed is started once and never stopped, which is what keeps a loop from clicking — but
    /// a stream still playing at exit keeps its playback alive, and the playback keeps the
    /// AudioStreamWav alive through the static cache AudioKit holds it in. Both are then still
    /// referenced when Godot sweeps ObjectDB, and it reports twelve leaked instances: six beds
    /// and their six playbacks. Nothing accumulates while the game is running; this is purely
    /// about the order in which things shut down.
    /// </summary>
    public void Shutdown()
    {
        Stop(_rumble); Stop(_gravel); Stop(_wind);
        Stop(_rattle); Stop(_scrub); Stop(_ambience);
        if (_shots != null)
            for (int i = 0; i < _shots.Length; i++) Stop(_shots[i]);
    }

    private static void Stop(AudioStreamPlayer p)
    {
        if (p != null && p.Playing) p.Stop();
    }

    // ═══════════════════════════════════════════
    //  ONE-SHOTS
    // ═══════════════════════════════════════════

    /// <summary>
    /// Fire an event sound. Voices are handed out round-robin, so a fifth overlapping sound
    /// cuts the oldest rather than being dropped — a crash you cannot hear is worse than a
    /// menu blip you cannot hear.
    /// </summary>
    public void Play(Sfx sfx, float pitch = 1f, float gain = 1f)
    {
        if (_shots == null) return;

        var player = _shots[_nextShot];
        _nextShot = (_nextShot + 1) % ShotVoices;

        player.Stream = Stream(sfx);
        player.PitchScale = Mathf.Max(0.05f, pitch);

        float g = gain * Db(_mix.SfxDb) * Db(_mix.MasterDb);
        if (_main.State == Main.GameState.Paused) g *= _mix.PauseDuck;
        player.VolumeDb = g < Silence ? -80f : Mathf.LinearToDb(g);

        player.Play();
    }

    private static AudioStream Stream(Sfx sfx)
    {
        switch (sfx)
        {
            case Sfx.Kick: return AudioKit.Kick;
            case Sfx.Crash: return AudioKit.Crash;
            case Sfx.Finish: return AudioKit.Finish;
            case Sfx.Beep: return AudioKit.Beep;
            case Sfx.Go: return AudioKit.Go;
            case Sfx.Confirm: return AudioKit.Confirm;
            case Sfx.Back: return AudioKit.Back;
            case Sfx.Deny: return AudioKit.Deny;
            case Sfx.Bird: return AudioKit.Bird;
            default: return AudioKit.Move;
        }
    }

    /// <summary>Decibels to a linear gain, the way every level in AudioDesign is written.</summary>
    private static float Db(float db)
    {
        return Mathf.DbToLinear(db);
    }
}
