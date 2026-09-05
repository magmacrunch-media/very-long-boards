using Godot;

/// <summary>
/// Every sound in the game, synthesized in code. Third sibling to <see cref="MeshKit"/> and
/// <see cref="TextureKit"/>: MeshKit makes the shapes, TextureKit gives them a surface, and
/// AudioKit gives them a voice — nothing is loaded from disk, so the whole art pipeline stays
/// in the repo as code you can diff.
///
/// Two kinds of stream come out of here:
///
///   Beds     loop forever and are ridden by volume and pitch — wheels, wind, truck rattle,
///            the shoulder, the summer afternoon outside the garage.
///   One-shots fire and finish — a kick, a crash, a menu blip.
///
/// The rate is 22050 Hz, which is what the N64's audio DSP actually ran most games at, and
/// nothing here is stereo: the mix is a single point of view sitting on the board.
///
/// Each stream is built on first use and cached; every player shares the one instance.
/// </summary>
public static class AudioKit
{
    /// <summary>Sample rate. Console-era, and plenty for noise beds and square-ish blips.</summary>
    public const int Rate = 22050;

    // ═══════════════════════════════════════════
    //  BEDS — looping, ridden by the mix
    // ═══════════════════════════════════════════

    private static AudioStreamWav _rumble, _gravel, _wind, _rattle, _scrub, _ambience;

    /// <summary>
    /// Urethane on tarmac. Low and broad so that pitching it up with speed reads as the wheels
    /// turning faster rather than as a filter sweep.
    /// </summary>
    public static AudioStreamWav Rumble => _rumble ??= BuildRumble();

    /// <summary>The shoulder — dirt and loose stone. Brighter, and chewed up by a fast flutter.</summary>
    public static AudioStreamWav Gravel => _gravel ??= BuildGravel();

    /// <summary>Air past your ears. Rides on speed squared, same as the drag term that earns it.</summary>
    public static AudioStreamWav Wind => _wind ??= BuildWind();

    /// <summary>
    /// Speed wobble in the trucks. Flutters at 11 Hz to match the lateral drift PlayerManager
    /// applies at <c>Sin(time * 11f)</c>, so the sound and the shimmy are the same event.
    /// </summary>
    public static AudioStreamWav Rattle => _rattle ??= BuildRattle();

    /// <summary>A sole dragged on tarmac — the foot brake, and the tyres letting go in a hard carve.</summary>
    public static AudioStreamWav Scrub => _scrub ??= BuildScrub();

    /// <summary>Outside the garage on a summer afternoon: a low bed with leaves moving over it.</summary>
    public static AudioStreamWav Ambience => _ambience ??= BuildAmbience();

    // ═══════════════════════════════════════════
    //  ONE-SHOTS
    // ═══════════════════════════════════════════

    private static AudioStreamWav _kick, _crash, _finish, _beep, _go;
    private static AudioStreamWav _blip, _confirm, _back, _deny, _bird;

    /// <summary>A foot shoving off the road. Scuff over a soft thump through the deck.</summary>
    public static AudioStreamWav Kick => _kick ??= BuildKick();

    /// <summary>Board and rider parting company: a low tumble under three clatters.</summary>
    public static AudioStreamWav Crash => _crash ??= BuildCrash();

    /// <summary>Finish line. A major arpeggio, four notes, up.</summary>
    public static AudioStreamWav Finish => _finish ??= BuildArpeggio(
        new float[] { 523.25f, 659.26f, 783.99f, 1046.5f }, 0.115f, 0.30f);

    /// <summary>Three, two, one.</summary>
    public static AudioStreamWav Beep => _beep ??= Wav(Blip(0.14f, 659.26f, 0.055f), false);

    /// <summary>Go.</summary>
    public static AudioStreamWav Go => _go ??= Wav(Blip(0.32f, 987.77f, 0.14f), false);

    /// <summary>Moving along a row of Carls, boards or posters.</summary>
    public static AudioStreamWav Move => _blip ??= Wav(Blip(0.06f, 880f, 0.020f), false);

    /// <summary>Taking a selection. Two notes, up.</summary>
    public static AudioStreamWav Confirm => _confirm ??= BuildArpeggio(
        new float[] { 659.26f, 987.77f }, 0.075f, 0.11f);

    /// <summary>Backing out. The same two notes, down.</summary>
    public static AudioStreamWav Back => _back ??= BuildArpeggio(
        new float[] { 659.26f, 440f }, 0.075f, 0.11f);

    /// <summary>A poster for a course that does not exist yet.</summary>
    public static AudioStreamWav Deny => _deny ??= Wav(Blip(0.20f, 155.56f, 0.075f), false);

    /// <summary>One bird, three chirps. Sprinkled over the ambience at random intervals.</summary>
    public static AudioStreamWav Bird => _bird ??= BuildBird();

    // ═══════════════════════════════════════════
    //  BUILDERS
    // ═══════════════════════════════════════════

    private static AudioStreamWav BuildRumble()
    {
        // 40-700 Hz, two poles either side. Wide enough that a 0.55x-1.6x pitch ride never
        // runs the band off the bottom of the speaker or up into hiss.
        float[] s = NoiseLoop(1.1f, 40f, 700f, 2, seed: 17);
        Gain(s, 0.85f);
        return Wav(s, true);
    }

    private static AudioStreamWav BuildGravel()
    {
        float[] s = NoiseLoop(1.1f, 180f, 5000f, 1, seed: 31);
        // 33 cycles over a 1.1 s loop is 30 Hz, and an integer count so the seam is silent.
        Flutter(s, cycles: 33, depth: 0.55f);
        Gain(s, 0.8f);
        return Wav(s, true);
    }

    private static AudioStreamWav BuildWind()
    {
        float[] s = NoiseLoop(1.4f, 500f, 6500f, 1, seed: 53);
        Flutter(s, cycles: 3, depth: 0.18f);   // a slow swell, so a held top speed doesn't sit flat
        Gain(s, 0.7f);
        return Wav(s, true);
    }

    private static AudioStreamWav BuildRattle()
    {
        // A narrow buzzy band — this is bearings and truck hardware, not road surface.
        float[] s = NoiseLoop(1.0f, 90f, 280f, 3, seed: 71);
        Flutter(s, cycles: 11, depth: 0.85f);  // 11 cycles / 1.0 s = the 11 Hz shimmy
        Gain(s, 0.9f);
        return Wav(s, true);
    }

    private static AudioStreamWav BuildScrub()
    {
        float[] s = NoiseLoop(0.8f, 900f, 5500f, 2, seed: 97);
        Gain(s, 0.75f);
        return Wav(s, true);
    }

    private static AudioStreamWav BuildAmbience()
    {
        const float len = 3f;
        float[] bed = NoiseLoop(len, 60f, 420f, 2, seed: 113);
        Gain(bed, 0.30f);

        float[] leaves = NoiseLoop(len, 1800f, 7000f, 1, seed: 127);
        Flutter(leaves, cycles: 2, depth: 0.7f);   // 2 cycles / 3 s — a breeze coming and going
        Gain(leaves, 0.14f);

        Mix(bed, leaves, 0, 1f);
        return Wav(bed, true);
    }

    private static AudioStreamWav BuildKick()
    {
        float[] s = new float[Mathf.RoundToInt(0.30f * Rate)];
        // The scuff: sole across tarmac, gone in a fifth of a second.
        Mix(s, NoiseBurst(0.22f, 400f, 4200f, seed: 149, attack: 0.004f, decay: 0.055f), 0, 0.55f);
        // The thump: the shove arriving through the deck.
        Mix(s, Blip(0.18f, 96f, 0.045f), 0, 0.45f);
        return Wav(s, false);
    }

    private static AudioStreamWav BuildCrash()
    {
        float[] s = new float[Mathf.RoundToInt(1.30f * Rate)];

        // The tumble — you and the board hitting the road together.
        Mix(s, Blip(0.55f, 74f, 0.16f), 0, 0.75f);
        Mix(s, NoiseBurst(0.45f, 90f, 1400f, seed: 163, attack: 0.002f, decay: 0.10f), 0, 0.65f);

        // Three clatters as the deck bounces away, each one lighter and higher than the last.
        Mix(s, NoiseBurst(0.30f, 700f, 6000f, seed: 167, attack: 0.001f, decay: 0.035f),
            Mathf.RoundToInt(0.02f * Rate), 0.60f);
        Mix(s, NoiseBurst(0.30f, 900f, 7000f, seed: 173, attack: 0.001f, decay: 0.030f),
            Mathf.RoundToInt(0.19f * Rate), 0.42f);
        Mix(s, NoiseBurst(0.30f, 1100f, 8000f, seed: 179, attack: 0.001f, decay: 0.025f),
            Mathf.RoundToInt(0.33f * Rate), 0.28f);

        // ...and then it slides to a stop.
        Mix(s, NoiseBurst(0.60f, 1200f, 5000f, seed: 181, attack: 0.05f, decay: 0.20f),
            Mathf.RoundToInt(0.40f * Rate), 0.22f);
        return Wav(s, false);
    }

    /// <summary>
    /// Notes struck in order, each one left ringing under the next. Normalised at the end
    /// rather than per note: four decaying blips overlapping sum past full scale, and where
    /// exactly they peak depends on the intervals, so a fixed per-note gain that does not
    /// clip this arpeggio would still clip the next one somebody writes.
    /// </summary>
    private static AudioStreamWav BuildArpeggio(float[] hz, float step, float decay)
    {
        float ring = decay * 4f;
        int n = Mathf.RoundToInt((step * (hz.Length - 1) + ring) * Rate);
        float[] s = new float[n];
        for (int i = 0; i < hz.Length; i++)
            Mix(s, Blip(ring, hz[i], decay), Mathf.RoundToInt(i * step * Rate), 1f);
        Normalise(s);
        Gain(s, 0.8f);
        return Wav(s, false);
    }

    private static AudioStreamWav BuildBird()
    {
        float[] s = new float[Mathf.RoundToInt(0.72f * Rate)];
        Mix(s, Chirp(0.11f, 2300f, 3500f, 2700f), 0, 0.45f);
        Mix(s, Chirp(0.09f, 2600f, 3700f, 2900f), Mathf.RoundToInt(0.20f * Rate), 0.40f);
        Mix(s, Chirp(0.13f, 2100f, 3200f, 2400f), Mathf.RoundToInt(0.42f * Rate), 0.34f);
        return Wav(s, false);
    }

    // ═══════════════════════════════════════════
    //  GENERATORS
    // ═══════════════════════════════════════════

    /// <summary>
    /// A seamless loop of band-limited noise.
    ///
    /// The white noise underneath has period <c>n</c> by construction, and a stable one-pole
    /// fed a periodic signal settles onto a periodic output — so running each filter twice
    /// around the buffer and keeping only the second lap leaves the last sample flowing into
    /// the first with no click. It is the audio version of the lattice wrap TextureKit uses to
    /// make its tiles seamless, and for the same reason: the repeat is bad enough without a
    /// join marking where it happens.
    /// </summary>
    private static float[] NoiseLoop(float seconds, float lowHz, float highHz, int poles, int seed)
    {
        int n = Mathf.RoundToInt(seconds * Rate);
        float[] s = new float[n];
        uint h = (uint)seed * 2654435761u + 1u;
        for (int i = 0; i < n; i++) s[i] = Rand(ref h) * 2f - 1f;

        for (int p = 0; p < poles; p++) HighPass(s, lowHz, circular: true);
        for (int p = 0; p < poles; p++) LowPass(s, highHz, circular: true);
        Normalise(s);
        return s;
    }

    /// <summary>Band-limited noise under an attack/decay envelope. Impacts, scuffs, clatter.</summary>
    private static float[] NoiseBurst(float seconds, float lowHz, float highHz, int seed,
                                      float attack, float decay)
    {
        int n = Mathf.RoundToInt(seconds * Rate);
        float[] s = new float[n];
        uint h = (uint)seed * 2654435761u + 1u;
        for (int i = 0; i < n; i++) s[i] = Rand(ref h) * 2f - 1f;

        HighPass(s, lowHz, circular: false);
        LowPass(s, highHz, circular: false);
        LowPass(s, highHz, circular: false);
        Normalise(s);

        for (int i = 0; i < n; i++) s[i] *= Env(i / (float)Rate, seconds, attack, decay);
        return s;
    }

    /// <summary>
    /// A pitched blip: fundamental plus two harmonics, decaying. Not a square wave — a square's
    /// upper harmonics alias badly at 22 kHz — but the first two partials get most of the way
    /// to that hard console-menu tone without the fizz.
    /// </summary>
    private static float[] Blip(float seconds, float hz, float decay)
    {
        int n = Mathf.RoundToInt(seconds * Rate);
        float[] s = new float[n];
        for (int i = 0; i < n; i++)
        {
            float t = i / (float)Rate;
            float w = Mathf.Tau * hz * t;
            float body = Mathf.Sin(w) + 0.40f * Mathf.Sin(w * 2f) + 0.18f * Mathf.Sin(w * 3f);
            s[i] = body * 0.63f * Env(t, seconds, 0.004f, decay);
        }
        return s;
    }

    /// <summary>A sine sweeping up to a peak and part-way back down. One bird syllable.</summary>
    private static float[] Chirp(float seconds, float startHz, float peakHz, float endHz)
    {
        int n = Mathf.RoundToInt(seconds * Rate);
        float[] s = new float[n];
        float phase = 0f;
        for (int i = 0; i < n; i++)
        {
            float t = i / (float)Rate;
            float u = t / seconds;
            // Up over the first third, down over the rest — the shape of a real chirp.
            float hz = u < 0.33f
                ? Mathf.Lerp(startHz, peakHz, u / 0.33f)
                : Mathf.Lerp(peakHz, endHz, (u - 0.33f) / 0.67f);
            phase += Mathf.Tau * hz / Rate;
            s[i] = (Mathf.Sin(phase) + 0.22f * Mathf.Sin(phase * 2f))
                   * Env(t, seconds, 0.012f, seconds * 0.7f);
        }
        return s;
    }

    // ═══════════════════════════════════════════
    //  FILTERS AND SHAPING
    // ═══════════════════════════════════════════

    /// <summary>
    /// One-pole lowpass, in place. <paramref name="circular"/> runs a warm-up lap first and
    /// throws it away, which is what makes a loop join seamlessly.
    /// </summary>
    private static void LowPass(float[] s, float cutoffHz, bool circular)
    {
        float a = 1f - Mathf.Exp(-Mathf.Tau * cutoffHz / Rate);
        float y = 0f;
        int passes = circular ? 2 : 1;
        for (int pass = 0; pass < passes; pass++)
        {
            bool keep = pass == passes - 1;
            for (int i = 0; i < s.Length; i++)
            {
                y += a * (s[i] - y);
                if (keep) s[i] = y;
            }
        }
    }

    /// <summary>One-pole highpass — the signal minus its own lowpass. In place.</summary>
    private static void HighPass(float[] s, float cutoffHz, bool circular)
    {
        float a = 1f - Mathf.Exp(-Mathf.Tau * cutoffHz / Rate);
        float y = 0f;
        int passes = circular ? 2 : 1;
        for (int pass = 0; pass < passes; pass++)
        {
            bool keep = pass == passes - 1;
            for (int i = 0; i < s.Length; i++)
            {
                float x = s[i];
                y += a * (x - y);
                if (keep) s[i] = x - y;
            }
        }
    }

    /// <summary>
    /// Amplitude modulation at a whole number of cycles across the buffer. Counting in cycles
    /// rather than hertz is deliberate: an integer count is guaranteed to land back where it
    /// started, so the modulation cannot be the thing that puts a click in a seamless loop.
    /// </summary>
    private static void Flutter(float[] s, int cycles, float depth)
    {
        for (int i = 0; i < s.Length; i++)
        {
            float u = i / (float)s.Length;
            s[i] *= 1f - depth * 0.5f * (1f - Mathf.Cos(Mathf.Tau * cycles * u));
        }
    }

    /// <summary>Attack ramp, exponential decay, and a 10 ms taper so a one-shot never clicks off.</summary>
    private static float Env(float t, float total, float attack, float decay)
    {
        float a = attack <= 0f ? 1f : Mathf.Min(1f, t / attack);
        float d = Mathf.Exp(-t / Mathf.Max(0.001f, decay));
        float tail = Mathf.Clamp((total - t) / 0.01f, 0f, 1f);
        return a * d * tail;
    }

    /// <summary>Add <paramref name="src"/> into <paramref name="dest"/> at a sample offset.</summary>
    private static void Mix(float[] dest, float[] src, int offset, float gain)
    {
        int n = Mathf.Min(src.Length, dest.Length - offset);
        for (int i = 0; i < n; i++) dest[offset + i] += src[i] * gain;
    }

    private static void Gain(float[] s, float g)
    {
        for (int i = 0; i < s.Length; i++) s[i] *= g;
    }

    /// <summary>Scale to full range, so band choice decides the colour and not the level.</summary>
    private static void Normalise(float[] s)
    {
        float peak = 0f;
        for (int i = 0; i < s.Length; i++) peak = Mathf.Max(peak, Mathf.Abs(s[i]));
        if (peak < 1e-6f) return;
        Gain(s, 1f / peak);
    }

    /// <summary>Deterministic 0..1. Same generator family as TextureKit's hash.</summary>
    private static float Rand(ref uint state)
    {
        unchecked
        {
            state ^= state << 13;
            state ^= state >> 17;
            state ^= state << 5;
            return (state & 0x7fffffff) / (float)0x7fffffff;
        }
    }

    // ═══════════════════════════════════════════
    //  PACKING
    // ═══════════════════════════════════════════

    /// <summary>Float samples to a mono 16-bit PCM stream, little-endian, optionally looping.</summary>
    private static AudioStreamWav Wav(float[] s, bool loop)
    {
        var data = new byte[s.Length * 2];
        for (int i = 0; i < s.Length; i++)
        {
            int v = Mathf.RoundToInt(Mathf.Clamp(s[i], -1f, 1f) * 32767f);
            data[i * 2] = (byte)(v & 0xff);
            data[i * 2 + 1] = (byte)((v >> 8) & 0xff);
        }

        var wav = new AudioStreamWav();
        wav.Format = AudioStreamWav.FormatEnum.Format16Bits;
        wav.MixRate = Rate;
        wav.Stereo = false;
        wav.Data = data;
        if (loop)
        {
            wav.LoopMode = AudioStreamWav.LoopModeEnum.Forward;
            wav.LoopBegin = 0;
            wav.LoopEnd = s.Length;
        }
        return wav;
    }
}
