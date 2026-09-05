using Godot;

/// <summary>
/// The mix. Every level and every pitch range <see cref="AudioManager"/> rides, lifted out of
/// the manager the same way Carl's proportions are lifted out of his builder — so balancing the
/// game is dragging sliders in the Inspector rather than recompiling.
///
/// Levels are in decibels and 0 dB means "as loud as <see cref="AudioKit"/> generated it".
/// AudioKit normalises each stream to full scale, so nothing here starts at 0: the numbers
/// below are the mix, and they are negative because six beds summing at full scale would clip.
///
/// Pitch ranges are multipliers on the generated stream, mapped across the 0..1 range of
/// whatever drives them — speed for the wheels and the wind, wobble level for the rattle.
///
/// Edit <c>Resources/Design/Audio.tres</c>.
/// </summary>
[Tool]
[GlobalClass]
public partial class AudioDesign : Resource
{
    // ── Levels ───────────────────────────────────
    [ExportGroup("Levels")]

    /// <summary>Trim on everything at once, sound effects included. The volume knob.</summary>
    [Export(PropertyHint.Range, "-40,6,0.5")] public float MasterDb { get; set; } = 0f;

    /// <summary>Wheels on tarmac, at top speed.</summary>
    [Export(PropertyHint.Range, "-40,6,0.5")] public float RumbleDb { get; set; } = -5f;

    /// <summary>Wheels on the shoulder. Louder than tarmac — that is what being off the road sounds like.</summary>
    [Export(PropertyHint.Range, "-40,6,0.5")] public float GravelDb { get; set; } = -4f;

    /// <summary>Air past your ears, at top speed.</summary>
    [Export(PropertyHint.Range, "-40,6,0.5")] public float WindDb { get; set; } = -8.5f;

    /// <summary>Truck rattle, at the point where wobble crashes you.</summary>
    [Export(PropertyHint.Range, "-40,6,0.5")] public float RattleDb { get; set; } = -5f;

    /// <summary>Foot brake and hard carves.</summary>
    [Export(PropertyHint.Range, "-40,6,0.5")] public float ScrubDb { get; set; } = -10f;

    /// <summary>The summer afternoon behind the title and the garage.</summary>
    [Export(PropertyHint.Range, "-40,6,0.5")] public float AmbienceDb { get; set; } = -8.5f;

    /// <summary>Kicks, crashes, countdown and menu blips.</summary>
    [Export(PropertyHint.Range, "-40,6,0.5")] public float SfxDb { get; set; } = -6f;

    /// <summary>Birdsong over the ambience. Quiet — it is scenery, not an event.</summary>
    [Export(PropertyHint.Range, "-40,6,0.5")] public float BirdDb { get; set; } = -14f;

    // ── Ride ─────────────────────────────────────
    [ExportGroup("Ride")]

    /// <summary>Wheel pitch at a standstill and at top speed. Roughly linear in speed, as rolling noise is.</summary>
    [Export(PropertyHint.Range, "0.2,1.0,0.01")] public float WheelPitchLow { get; set; } = 0.55f;
    [Export(PropertyHint.Range, "1.0,3.0,0.01")] public float WheelPitchHigh { get; set; } = 1.60f;

    [Export(PropertyHint.Range, "0.2,1.0,0.01")] public float WindPitchLow { get; set; } = 0.80f;
    [Export(PropertyHint.Range, "1.0,3.0,0.01")] public float WindPitchHigh { get; set; } = 1.35f;

    /// <summary>
    /// Rattle pitch from first shimmy to crash. Deliberately a narrower ride than the visual
    /// wobble frequency's 8-22 Hz: pitching the bed that far takes the tone with it and it stops
    /// sounding like hardware.
    /// </summary>
    [Export(PropertyHint.Range, "0.2,1.0,0.01")] public float RattlePitchLow { get; set; } = 0.85f;
    [Export(PropertyHint.Range, "1.0,3.0,0.01")] public float RattlePitchHigh { get; set; } = 1.45f;

    /// <summary>
    /// Speed below which the wheels are silent, as a fraction of top speed. Not zero: the ride
    /// has a floor of 2.5 m/s and a bed that never quite fades out sounds like a stuck loop.
    /// </summary>
    [Export(PropertyHint.Range, "0.0,0.3,0.005")] public float WheelSilenceBelow { get; set; } = 0.03f;

    /// <summary>How fast the ride beds chase their target level, in units per second.</summary>
    [Export(PropertyHint.Range, "0.5,30,0.5")] public float RideFadeRate { get; set; } = 9f;

    /// <summary>How fast the ambience crosses in and out between worlds. Slower — it is weather.</summary>
    [Export(PropertyHint.Range, "0.1,10,0.1")] public float AmbienceFadeRate { get; set; } = 1.6f;

    /// <summary>What everything drops to while the game is paused.</summary>
    [Export(PropertyHint.Range, "0.0,1.0,0.01")] public float PauseDuck { get; set; } = 0.12f;

    // ── Ambience ─────────────────────────────────
    [ExportGroup("Ambience")]

    /// <summary>Seconds between birds, picked uniformly in this range.</summary>
    [Export(PropertyHint.Range, "0.5,30,0.1")] public float BirdGapMin { get; set; } = 3.5f;
    [Export(PropertyHint.Range, "0.5,60,0.1")] public float BirdGapMax { get; set; } = 11f;

    /// <summary>Pitch spread on each bird, so the same three chirps are not obviously the same bird.</summary>
    [Export(PropertyHint.Range, "0.5,1.0,0.01")] public float BirdPitchLow { get; set; } = 0.88f;
    [Export(PropertyHint.Range, "1.0,2.0,0.01")] public float BirdPitchHigh { get; set; } = 1.18f;
}
