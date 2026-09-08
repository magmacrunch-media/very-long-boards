using Godot;

/// <summary>
/// The shape of a course: how the road rolls, how it turns, how wide it is, and how thickly
/// the forest sits beside it. Lifted out of <see cref="TerrainManager"/> and
/// <see cref="SceneryManager"/> so the whole thing can be dialled in the Inspector.
///
/// Edit <c>Resources/Design/Frogwood.tres</c>, or open <c>Scenes/CoursePreview.tscn</c> and drag
/// the Distance slider to fly down the road while you tune it.
///
/// A course does not have to be made of sine layers. <see cref="MeasuredCourse"/> overrides
/// the three methods below with a sampled profile taken off real survey data, which is how
/// Block Island is built; everything downstream asks this class the same three questions and
/// never learns the difference.
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

    /// <summary>
    /// Stone walls along the verge. Block Island is glacial moraine and the field walls are
    /// the first thing anyone notices about it; Frogwood has none and leaves this at zero.
    /// </summary>
    [Export(PropertyHint.Range, "0,200,1")] public int StoneWalls { get; set; } = 0;

    // ── Palette ──────────────────────────────────
    // What the course is made of, rather than how it is shaped. Two courses can share every
    // number above and still be nothing alike: Frogwood is a New Hampshire pine forest and
    // Block Island is open moraine over the Atlantic, and both of them are summer.
    [ExportGroup("Palette")]

    /// <summary>The two colours the ground texture is mottled between.</summary>
    [Export] public Color GroundLo { get; set; } = new Color(0.14f, 0.34f, 0.09f);
    [Export] public Color GroundHi { get; set; } = new Color(0.24f, 0.48f, 0.16f);

    /// <summary>The two colours the broadleaf canopy and the roadside scrub are mottled between.</summary>
    [Export] public Color FoliageLo { get; set; } = new Color(0.14f, 0.38f, 0.09f);
    [Export] public Color FoliageHi { get; set; } = new Color(0.30f, 0.58f, 0.18f);

    /// <summary>Roadside flowers. Frogwood's meadow mix; Block Island's beach rose and goldenrod.</summary>
    [Export] public Color[] FlowerColors { get; set; } = {
        new Color(0.95f, 0.85f, 0.30f),
        new Color(0.90f, 0.45f, 0.65f),
        new Color(0.85f, 0.90f, 0.95f),
        new Color(0.70f, 0.45f, 0.85f)
    };

    // ── Light ────────────────────────────────────
    [ExportGroup("Light")]

    [Export(PropertyHint.Range, "0,4,0.05")] public float SunEnergy { get; set; } = 1.75f;
    [Export] public Color SunColor { get; set; } = new Color(1f, 0.98f, 0.95f);

    /// <summary>
    /// Kept weak and near-neutral on purpose. A strong blue ambient turns the grass teal and
    /// the asphalt purple, which reads as dusk rather than as an afternoon.
    /// </summary>
    [Export(PropertyHint.Range, "0,2,0.05")] public float AmbientEnergy { get; set; } = 0.55f;
    [Export] public Color AmbientColor { get; set; } = new Color(0.74f, 0.76f, 0.78f);

    [Export] public Color FogColor { get; set; } = new Color(0.66f, 0.76f, 0.86f);
    [Export(PropertyHint.Range, "0,0.05,0.0005")] public float FogDensity { get; set; } = 0.010f;

    [Export] public Color SkyTop { get; set; } = new Color(0.18f, 0.38f, 0.8f);
    [Export] public Color SkyHorizon { get; set; } = new Color(0.48f, 0.65f, 0.88f);

    /// <summary>
    /// The sky's own ground hemisphere — what fills the view past where the ground ribbons
    /// stop, which is most of the middle distance.
    ///
    /// It has to match this course's ground or the horizon becomes a visible seam between two
    /// different-coloured fields. Frogwood never had to think about it because the stock sky
    /// is New Hampshire green and so is Frogwood; drop a tawnier moor in front of the same sky
    /// and the join is the first thing you see.
    /// </summary>
    [Export] public Color SkyGroundHorizon { get; set; } = new Color(0.32f, 0.48f, 0.30f);
    [Export] public Color SkyGroundBottom { get; set; } = new Color(0.06f, 0.12f, 0.04f);

    // ── The sea ──────────────────────────────────
    [ExportGroup("The sea")]

    /// <summary>
    /// Whether this course runs beside open water. Off for an inland course, and everything
    /// below is ignored when it is.
    /// </summary>
    [Export] public bool HasSea { get; set; } = false;

    /// <summary>
    /// Sea level, in the course's own height units — so it is negative for a road that starts
    /// above it. Written by the generator for a measured course, which knows the real height
    /// the start line sits at and applies the same exaggeration to the drop down to the water.
    /// </summary>
    [Export] public float SeaLevel { get; set; } = -100f;

    /// <summary>Which side the water is on: -1 for the rider's left, +1 for the right.</summary>
    [Export(PropertyHint.Range, "-1,1,2")] public int SeaSide { get; set; } = 1;

    /// <summary>How far out from the road edge the cliff falls away to the water.</summary>
    [Export(PropertyHint.Range, "10,400,5")] public float ShoreDistance { get; set; } = 90f;

    [Export] public Color SeaColor { get; set; } = new Color(0.16f, 0.34f, 0.46f);

    /// <summary>
    /// How far the water is at <paramref name="z"/>. Constant for a composed course; a
    /// measured one overrides it, because a real road wanders toward the coast and away again.
    /// </summary>
    public virtual float ShoreAt(float z)
    {
        return ShoreDistance;
    }

    // ── The horizon ──────────────────────────
    [ExportGroup("The horizon")]

    /// <summary>
    /// Distant landform behind the drawn world. Without it the skyline is a ruler-straight
    /// line - the sky material's ground hemisphere meeting its sky colour, with nothing
    /// standing against it - and a road that drops 178 m through hill country looks flat from
    /// inside it.
    ///
    /// The bands sit at a FIXED altitude while the rider descends past them, which is the
    /// whole point: the drop is invisible from the road itself, and the only place it can be
    /// read is against something that is not falling with you.
    ///
    /// Off for an island. Block Island really does have a flat horizon in most directions,
    /// and inventing hills over the Atlantic would be worse than the ruler.
    /// </summary>
    [Export] public bool HasRidge { get; set; } = true;

    /// <summary>How many silhouette bands, near to far. Each is fainter than the last.</summary>
    [Export(PropertyHint.Range, "1,4,1")] public int RidgeBands { get; set; } = 3;

    /// <summary>
    /// Distance to the nearest band. Beyond the terrain window (Segments x SegmentLength, 1000 m
    /// by default) on purpose, so a ridge can never cut in front of the road.
    /// </summary>
    [Export(PropertyHint.Range, "600,4000,50")] public float RidgeNear { get; set; } = 1250f;

    /// <summary>Each band sits this many times farther out than the one before it.</summary>
    [Export(PropertyHint.Range, "1.1,3,0.05")] public float RidgeStep { get; set; } = 1.7f;

    /// <summary>Height of the nearest band's peaks above its foot, in metres.</summary>
    [Export(PropertyHint.Range, "10,400,5")] public float RidgeHeight { get; set; } = 90f;

    /// <summary>
    /// Altitude of the ridge foot, in the course's own height units - so 0 is the height of
    /// the start line. Negative sinks the whole skyline.
    /// </summary>
    [Export(PropertyHint.Range, "-200,200,5")] public float RidgeFoot { get; set; } = -25f;

    /// <summary>
    /// Near band first. Haze is painted in rather than fogged in: at the distances these sit
    /// at, the environment fog would take any colour to flat grey, so the material ignores it
    /// and the recession lives entirely in these.
    /// </summary>
    [Export] public Color[] RidgeColors { get; set; } = {
        new Color(0.29f, 0.42f, 0.38f),
        new Color(0.44f, 0.56f, 0.60f),
        new Color(0.57f, 0.67f, 0.77f)
    };

    /// <summary>Which skyline you get. Any integer; nothing is better than any other.</summary>
    [Export] public int RidgeSeed { get; set; } = 9;

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
    public virtual float HillAt(float z)
    {
        if (z < HillFlatStart) return 0f;
        float adjustedZ = z - HillFlatStart;
        return Sum(HillLayers, adjustedZ) - adjustedZ * Grade;
    }

    /// <summary>Lateral drift per metre of look-ahead. Flat through the start straight.</summary>
    public virtual float CurveAt(float z)
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
    /// The worst climb the rider actually has to carry momentum through: the largest rise from
    /// a trough to a later crest anywhere on the course. Compare against <c>v^2/2g</c> at that
    /// rider's top speed — over that and he bogs down.
    ///
    /// This used to sum the hill layers' amplitudes and double them, which is a bound rather
    /// than a measurement and IGNORES THE GRADE — the single largest term in the height. On
    /// Frogwood it reported 24.4 m against a stated budget of 22 and had done for as long as
    /// anyone had looked. The sines really do rise 20.9 m across one long roll, but the road is
    /// descending at 8% underneath them the whole way, and what the rider climbs is what is
    /// left: 4.7 m, over 46 m, once. The course was never near its budget.
    ///
    /// Walking the curve costs a few thousand evaluations and answers the question that was
    /// being asked. It also works for any course shape, which is why <see cref="MeasuredCourse"/>
    /// no longer needs its own copy.
    /// </summary>
    public virtual float TotalRelief()
    {
        float worst = 0f;
        float trough = HillAt(0f);
        // Quarter-metre steps: the shortest hill layer here is a 66 m wavelength, so this is
        // two orders of magnitude finer than anything it can be asked to resolve.
        for (float z = 0f; z <= Length; z += 0.25f)
        {
            float h = HillAt(z);
            if (h < trough) trough = h;
            if (h - trough > worst) worst = h - trough;
        }
        return worst;
    }
}
