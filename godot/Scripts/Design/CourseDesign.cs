using Godot;

/// <summary>
/// The shape of a course: how the road rolls, how it turns, how wide it is, and how thickly
/// the forest sits beside it. Lifted out of <see cref="TerrainManager"/> and
/// <see cref="SceneryManager"/> so the whole thing can be dialled in the Inspector.
///
/// Edit <c>Resources/Design/Frogwood.tres</c>, or open <c>Scenes/CoursePreview.tscn</c> and drag
/// the Distance slider to fly down the road while you tune it.
///
/// Two rules survive from the hand-written version and still apply:
/// <list type="bullet">
/// <item>Steepness is amplitude x frequency, not amplitude. A 0.9 m roll over a 66 m wavelength
/// is steeper than a 5.5 m roll over 1257 m.</item>
/// <item>Total relief has to stay under what a rider can climb on carried momentum
/// (v^2/2g, about 22 m at top speed), or he bogs down on every crest and the ride dies.
/// <c>physics_sim.py</c> reads this file and checks exactly that.</item>
/// </list>
/// </summary>
[Tool]
[GlobalClass]
public partial class CourseDesign : Resource
{
    // ── Hills ────────────────────────────────────
    [ExportGroup("Hills")]

    /// <summary>Metres of dead-flat road before the terrain starts moving.</summary>
    [Export(PropertyHint.Range, "0,200,1")] public float HillFlatStart { get; set; } = 20f;

    /// <summary>Net downhill grade. 0.08 is an 8% drop — this is what pays for the whole ride.</summary>
    [Export(PropertyHint.Range, "0,0.25,0.001")] public float Grade { get; set; } = 0.08f;

    /// <summary>Summed to make the rolling terrain. Four layers, long wavelength first.</summary>
    [Export] public SineLayer[] HillLayers { get; set; } = {
        new SineLayer(5.5f, 1256.6371f),   // landscape roll
        new SineLayer(4.0f, 392.6991f),    // long hills
        new SineLayer(1.8f, 149.5997f),    // rollers
        new SineLayer(0.9f, 66.1388f)      // sharp pitches
    };

    // ── Curves ───────────────────────────────────
    [ExportGroup("Curves")]

    /// <summary>Straight enough for the countdown and the first push, then it winds.</summary>
    [Export(PropertyHint.Range, "0,300,1")] public float CurveFlatStart { get; set; } = 60f;

    /// <summary>
    /// Lateral drift per metre of look-ahead, summed. Amplitudes here are a gradient, not
    /// metres — the road offsets by <c>CurveAt(z) * z</c>, so small numbers go a long way.
    /// </summary>
    [Export] public SineLayer[] CurveLayers { get; set; } = {
        new SineLayer(0.18f, 3141.5927f),
        new SineLayer(0.25f, 7853.9816f),
        new SineLayer(0.08f, 1256.6371f),
        new SineLayer(0.04f, 523.5988f)
    };

    // ── Road ─────────────────────────────────────
    [ExportGroup("Road")]

    [Export(PropertyHint.Range, "3,20,0.1")] public float RoadWidth { get; set; } = 8f;
    [Export(PropertyHint.Range, "20,600,1")] public float GroundWidth { get; set; } = 300f;
    [Export(PropertyHint.Range, "0.5,10,0.1")] public float ShoulderWidth { get; set; } = 2f;
    [Export(PropertyHint.Range, "0.02,0.5,0.01")] public float CenterLineWidth { get; set; } = 0.12f;
    [Export(PropertyHint.Range, "0.02,0.5,0.01")] public float EdgeLineWidth { get; set; } = 0.1f;

    /// <summary>How far in from the tarmac edge the white lines sit.</summary>
    [Export(PropertyHint.Range, "0,2,0.05")] public float EdgeLineInset { get; set; } = 0.3f;

    // ── Draw distance ────────────────────────────
    // Segments x SegmentLength is the visible window, and it also sizes the band the scenery
    // wraps inside — so a prop can never appear twice in one view. Changing these rescales
    // both the view and the prop recycling; raise Segments and the frame cost goes with it.
    [ExportGroup("Draw distance")]

    [Export(PropertyHint.Range, "50,1000,10")] public int Segments { get; set; } = 400;
    [Export(PropertyHint.Range, "0.5,10,0.1")] public float SegmentLength { get; set; } = 2.5f;
    [Export(PropertyHint.Range, "0,200,1")] public int SegmentsBehind { get; set; } = 70;

    // ── Course ───────────────────────────────────
    [ExportGroup("Course")]

    /// <summary>Metres from the start line to the finish banner.</summary>
    [Export(PropertyHint.Range, "200,20000,50")] public float Length { get; set; } = 2000f;

    // ── Scenery ──────────────────────────────────
    // Populations per band, not totals, because everything recycles as you ride.
    [ExportGroup("Scenery")]

    /// <summary>
    /// Scales the forest. Pines are five meshes each, so this is the knob that decides whether
    /// batching becomes necessary.
    /// </summary>
    [Export(PropertyHint.Range, "0,4,0.05")] public float Density { get; set; } = 1.2f;

    /// <summary>Fixed so the same course lays out the same way every run. Change it to reshuffle.</summary>
    [Export] public int ScenerySeed { get; set; } = 42;

    [Export(PropertyHint.Range, "0,400,1")] public int ClosePines { get; set; } = 120;
    [Export(PropertyHint.Range, "0,400,1")] public int FarPines { get; set; } = 50;
    [Export(PropertyHint.Range, "0,400,1")] public int Deciduous { get; set; } = 40;
    [Export(PropertyHint.Range, "0,200,1")] public int Rocks { get; set; } = 20;
    [Export(PropertyHint.Range, "0,200,1")] public int Stumps { get; set; } = 15;
    [Export(PropertyHint.Range, "0,500,1")] public int Wildflowers { get; set; } = 120;
    [Export(PropertyHint.Range, "0,200,1")] public int Ferns { get; set; } = 30;
    [Export(PropertyHint.Range, "0,200,1")] public int Bushes { get; set; } = 40;
    [Export(PropertyHint.Range, "0,100,1")] public int Logs { get; set; } = 10;
    [Export(PropertyHint.Range, "0,50,1")] public int RoadSigns { get; set; } = 8;
    [Export(PropertyHint.Range, "0,50,1")] public int Houses { get; set; } = 8;
    [Export(PropertyHint.Range, "0,20,1")] public int Streams { get; set; } = 3;

    /// <summary>How far apart the roadside distance markers stand, in metres.</summary>
    [Export(PropertyHint.Range, "50,2000,50")] public float MarkerSpacing { get; set; } = 500f;

    // ── Derived ──────────────────────────────────

    /// <summary>The visible window, and the length of the band the scenery wraps inside.</summary>
    public float Band { get { return Segments * SegmentLength; } }

    /// <summary>How far behind the rider the ribbons and the prop band extend.</summary>
    public float Behind { get { return SegmentsBehind * SegmentLength; } }

    /// <summary>A prop count scaled by <see cref="Density"/>.</summary>
    public int Scaled(int perBand) { return Mathf.RoundToInt(perBand * Density); }

    /// <summary>
    /// Road height in metres. Flat start area, then rolling hills on a net downhill grade.
    /// Mirrored by <c>physics_sim.py</c>, which parses this resource rather than copying it.
    /// </summary>
    public float HillAt(float z)
    {
        if (z < HillFlatStart) return 0f;
        float adjustedZ = z - HillFlatStart;
        return Sum(HillLayers, adjustedZ) - adjustedZ * Grade;
    }

    /// <summary>Lateral drift per metre of look-ahead. Flat through the start straight.</summary>
    public float CurveAt(float z)
    {
        if (z < CurveFlatStart) return 0f;
        return Sum(CurveLayers, z - CurveFlatStart);
    }

    private static float Sum(SineLayer[] layers, float z)
    {
        if (layers == null) return 0f;
        float total = 0f;
        foreach (var layer in layers)
        {
            if (layer == null) continue;
            total += Mathf.Sin(z * layer.AngularFrequency) * layer.Amplitude;
        }
        return total;
    }

    /// <summary>
    /// Worst-case climb between a trough and the next crest, ignoring the grade. Compare
    /// against v^2/2g (about 22 m at top speed) — over that and the rider bogs down.
    /// </summary>
    public float TotalRelief()
    {
        if (HillLayers == null) return 0f;
        float sum = 0f;
        foreach (var layer in HillLayers)
            if (layer != null) sum += layer.Amplitude;
        return sum * 2f;
    }
}
