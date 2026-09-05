using Godot;

/// <summary>
/// Every number that decides what a longboard looks like, lifted out of
/// <see cref="BoardBuilder"/>. One deck shape, four colourways — the rack in the garage and
/// the board under Carl read the same resource, so the deck you pick off the wall is still
/// literally the deck you ride.
///
/// Edit <c>Resources/Design/Board.tres</c>.
/// </summary>
[Tool]
[GlobalClass]
public partial class BoardDesign : Resource
{
    // ── Deck ─────────────────────────────────────
    [ExportGroup("Deck")]

    /// <summary>Deck top surface, relative to the board origin — where a rider's soles go.</summary>
    [Export(PropertyHint.Range, "0.0,0.2,0.0005")] public float GripTopY { get; set; } = 0.0375f;

    [Export(PropertyHint.Range, "0.3,1.2,0.005")] public float DeckWidth { get; set; } = 0.62f;

    /// <summary>The Natural deck is a wider cruiser. Applied to that colourway only.</summary>
    [Export(PropertyHint.Range, "0.3,1.2,0.005")] public float WideDeckWidth { get; set; } = 0.68f;

    [Export(PropertyHint.Range, "0.5,3.0,0.01")] public float DeckLength { get; set; } = 1.4f;
    [Export(PropertyHint.Range, "0.01,0.15,0.001")] public float DeckThickness { get; set; } = 0.045f;
    [Export(PropertyHint.Range, "0.1,1.0,0.005")] public float NoseLength { get; set; } = 0.4f;
    [Export(PropertyHint.Range, "0.1,1.0,0.005")] public float TailLength { get; set; } = 0.35f;

    /// <summary>Nose and tail taper to this fraction of the deck width.</summary>
    [Export(PropertyHint.Range, "0.3,1.0,0.01")] public float KickWidthRatio { get; set; } = 0.77f;

    [Export(PropertyHint.Range, "0.005,0.06,0.001")] public float RailWidth { get; set; } = 0.03f;
    [Export(PropertyHint.Range, "0.005,0.06,0.001")] public float GripThickness { get; set; } = 0.015f;

    // ── Trucks and wheels ────────────────────────
    [ExportGroup("Trucks and wheels")]

    /// <summary>Half the wheelbase — trucks sit at +/- this along the deck.</summary>
    [Export(PropertyHint.Range, "0.2,1.0,0.005")] public float TruckOffsetZ { get; set; } = 0.55f;

    [Export(PropertyHint.Range, "0.02,0.3,0.005")] public float WheelRadius { get; set; } = 0.055f;
    [Export(PropertyHint.Range, "0.02,0.3,0.005")] public float WheelWidth { get; set; } = 0.07f;

    /// <summary>Half the axle track — wheels sit at +/- this across the deck.</summary>
    [Export(PropertyHint.Range, "0.1,0.6,0.005")] public float AxleHalfWidth { get; set; } = 0.30f;

    [Export(PropertyHint.Range, "0.1,1.2,0.01")] public float AxleLength { get; set; } = 0.58f;
    [Export(PropertyHint.Range, "-0.3,0.0,0.005")] public float WheelY { get; set; } = -0.10f;
    [Export(PropertyHint.Range, "3,16,1")] public int WheelSegments { get; set; } = 6;

    // ── Colourways ───────────────────────────────
    // Index-parallel with Main.BoardType: Classic, Neon, Dark, Natural.
    [ExportGroup("Colourways")]

    [Export] public Color[] DeckColors { get; set; } = {
        new Color(0.52f, 0.26f, 0.1f),   // Classic brown
        new Color(0.95f, 0.25f, 0.95f),  // Neon pink
        new Color(0.12f, 0.12f, 0.15f),  // Dark black
        new Color(0.82f, 0.65f, 0.42f)   // Natural wood
    };
    [Export] public Color[] GripColors { get; set; } = {
        new Color(0.16f, 0.16f, 0.16f),  // Classic black
        new Color(0.15f, 0.15f, 0.45f),  // Neon blue
        new Color(0.35f, 0.05f, 0.55f),  // Dark purple
        new Color(0.55f, 0.45f, 0.3f)    // Natural tan
    };
    [Export] public Color[] AccentColors { get; set; } = {
        new Color(0.35f, 0.18f, 0.06f),  // Classic dark brown
        new Color(0.20f, 0.80f, 1.00f),  // Neon cyan
        new Color(0.50f, 0.10f, 0.70f),  // Dark purple
        new Color(0.65f, 0.50f, 0.30f)   // Natural light wood
    };
    [Export] public Color[] WheelColors { get; set; } = {
        new Color(0.12f, 0.12f, 0.12f),
        new Color(0.15f, 0.15f, 0.15f),
        new Color(0.10f, 0.10f, 0.12f),
        new Color(0.18f, 0.16f, 0.14f)
    };

    // ── Hardware ─────────────────────────────────
    [ExportGroup("Hardware")]

    [Export] public Color TruckColor { get; set; } = new Color(0.62f, 0.62f, 0.65f);
    [Export] public Color AxleColor { get; set; } = new Color(0.55f, 0.55f, 0.58f);
    [Export] public Color HubColor { get; set; } = new Color(0.45f, 0.45f, 0.48f);

    /// <summary>How hard the Neon deck glows. Zero turns the emission off entirely.</summary>
    [Export(PropertyHint.Range, "0,2,0.05")] public float NeonGlow { get; set; } = 0.3f;

    private static Color Pick(Color[] table, int index, Color fallback)
    {
        if (table == null || table.Length == 0) return fallback;
        return table[Mathf.Clamp(index, 0, table.Length - 1)];
    }

    public Color DeckFor(int index) { return Pick(DeckColors, index, new Color(0.52f, 0.26f, 0.1f)); }
    public Color GripFor(int index) { return Pick(GripColors, index, new Color(0.16f, 0.16f, 0.16f)); }
    public Color AccentFor(int index) { return Pick(AccentColors, index, new Color(0.35f, 0.18f, 0.06f)); }
    public Color WheelFor(int index) { return Pick(WheelColors, index, new Color(0.12f, 0.12f, 0.12f)); }

    /// <summary>Deck width for a colourway — Natural is the wide one.</summary>
    public float WidthFor(Main.BoardType type)
    {
        return type == Main.BoardType.Natural ? WideDeckWidth : DeckWidth;
    }
}
