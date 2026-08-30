using Godot;

/// <summary>
/// Every number that decides what Carl looks like, lifted out of <see cref="CarlBuilder"/> so
/// it can be dragged in the Inspector instead of recompiled. The defaults here reproduce the
/// rig exactly as it was hand-written, so an unassigned slot still gives you the real Carl.
///
/// Edit <c>Resources/Design/Carl.tres</c>, or open <c>Scenes/CarlPreview.tscn</c> to watch him
/// rebuild live as you drag.
/// </summary>
[Tool]
[GlobalClass]
public partial class CarlDesign : Resource
{
    // ── Proportions ──────────────────────────────
    [ExportGroup("Proportions")]

    [Export(PropertyHint.Range, "0.05,0.4,0.005")] public float PelvisTopRadius { get; set; } = 0.14f;
    [Export(PropertyHint.Range, "0.05,0.4,0.005")] public float PelvisBottomRadius { get; set; } = 0.13f;
    [Export(PropertyHint.Range, "0.1,0.8,0.005")] public float PelvisHeight { get; set; } = 0.38f;

    // ── Torso ────────────────────────────────────
    [ExportGroup("Torso")]

    // The ribcage is three stacked cylinders, each tapering into the one above it: the waist
    // cylinder runs WaistBottom -> Waist, the chest runs Waist -> Chest, the shoulder cap runs
    // Chest -> Shoulder. Widen one radius and the segments either side follow.
    [Export(PropertyHint.Range, "0.05,0.4,0.005")] public float WaistBottomRadius { get; set; } = 0.11f;
    [Export(PropertyHint.Range, "0.05,0.4,0.005")] public float WaistRadius { get; set; } = 0.13f;
    [Export(PropertyHint.Range, "0.05,0.4,0.005")] public float ChestRadius { get; set; } = 0.15f;
    [Export(PropertyHint.Range, "0.05,0.4,0.005")] public float ShoulderRadius { get; set; } = 0.16f;

    /// <summary>
    /// Scales the three stacked ribcage cylinders together. 1.0 is the original build; raise it
    /// for a longer back without touching the arms or legs.
    /// </summary>
    [Export(PropertyHint.Range, "0.4,2.0,0.01")] public float TorsoHeight { get; set; } = 1f;

    /// <summary>How far the chest and head sit behind the waist. Gives him a bit of slouch.</summary>
    [Export(PropertyHint.Range, "-0.1,0.1,0.005")] public float TorsoLean { get; set; } = 0.02f;

    // ── Head ─────────────────────────────────────
    [ExportGroup("Head")]

    [Export(PropertyHint.Range, "0.01,0.15,0.002")] public float NeckTopRadius { get; set; } = 0.05f;
    [Export(PropertyHint.Range, "0.01,0.15,0.002")] public float NeckBottomRadius { get; set; } = 0.06f;
    [Export(PropertyHint.Range, "0.02,0.3,0.005")] public float NeckHeight { get; set; } = 0.08f;
    [Export(PropertyHint.Range, "0.04,0.35,0.005")] public float HeadRadius { get; set; } = 0.12f;

    /// <summary>Head centre above the neck joint. Raise it together with HeadRadius.</summary>
    [Export(PropertyHint.Range, "0.02,0.5,0.005")] public float HeadOffsetY { get; set; } = 0.14f;

    [Export(PropertyHint.Range, "0.02,0.35,0.005")] public float HairTopRadius { get; set; } = 0.11f;
    [Export(PropertyHint.Range, "0.02,0.35,0.005")] public float HairBottomRadius { get; set; } = 0.13f;
    [Export(PropertyHint.Range, "0.01,0.2,0.005")] public float HairHeight { get; set; } = 0.06f;

    /// <summary>Hair cylinder centre above the neck joint — the cap sits on top of the skull.</summary>
    [Export(PropertyHint.Range, "0.02,0.6,0.005")] public float HairOffsetY { get; set; } = 0.24f;

    // ── Arms ─────────────────────────────────────
    [ExportGroup("Arms")]

    [Export(PropertyHint.Range, "0.05,0.4,0.005")] public float ShoulderOffsetX { get; set; } = 0.17f;
    [Export(PropertyHint.Range, "0.01,0.12,0.002")] public float UpperArmTopRadius { get; set; } = 0.035f;
    [Export(PropertyHint.Range, "0.01,0.12,0.002")] public float UpperArmBottomRadius { get; set; } = 0.03f;
    [Export(PropertyHint.Range, "0.05,0.5,0.005")] public float UpperArmLength { get; set; } = 0.2f;
    [Export(PropertyHint.Range, "0.01,0.12,0.002")] public float ForearmTopRadius { get; set; } = 0.03f;
    [Export(PropertyHint.Range, "0.01,0.12,0.002")] public float ForearmBottomRadius { get; set; } = 0.025f;
    [Export(PropertyHint.Range, "0.05,0.5,0.005")] public float ForearmLength { get; set; } = 0.18f;
    [Export(PropertyHint.Range, "0.01,0.12,0.002")] public float HandRadius { get; set; } = 0.03f;

    // ── Legs ─────────────────────────────────────
    [ExportGroup("Legs")]

    [Export(PropertyHint.Range, "0.0,0.3,0.005")] public float HipOffsetX { get; set; } = 0.08f;
    [Export(PropertyHint.Range, "0.01,0.2,0.002")] public float ThighTopRadius { get; set; } = 0.06f;
    [Export(PropertyHint.Range, "0.01,0.2,0.002")] public float ThighBottomRadius { get; set; } = 0.055f;
    [Export(PropertyHint.Range, "0.05,0.6,0.005")] public float ThighLength { get; set; } = 0.22f;
    [Export(PropertyHint.Range, "0.01,0.2,0.002")] public float ShinTopRadius { get; set; } = 0.05f;
    [Export(PropertyHint.Range, "0.01,0.2,0.002")] public float ShinBottomRadius { get; set; } = 0.045f;
    [Export(PropertyHint.Range, "0.05,0.6,0.005")] public float ShinLength { get; set; } = 0.2f;
    [Export] public Vector3 ShoeSize { get; set; } = new Vector3(0.1f, 0.06f, 0.22f);
    [Export(PropertyHint.Range, "0.005,0.08,0.002")] public float SoleThickness { get; set; } = 0.02f;

    /// <summary>How far his shoes sit forward of the leg centreline.</summary>
    [Export(PropertyHint.Range, "-0.15,0.15,0.005")] public float ShoeForward { get; set; } = -0.02f;

    // ── Palette ──────────────────────────────────
    [ExportGroup("Palette")]

    [Export] public Color SkinColor { get; set; } = new Color(0.9f, 0.78f, 0.6f);
    [Export] public Color HairColor { get; set; } = new Color(0.3f, 0.18f, 0.08f);
    [Export] public Color ShoeColor { get; set; } = new Color(0.14f, 0.14f, 0.14f);
    [Export] public Color SoleColor { get; set; } = new Color(0.08f, 0.08f, 0.08f);

    // ── Outfits ──────────────────────────────────
    // Index-parallel with Main.CarlType: Office, Party, Dark. There is only one Carl Spatski —
    // these are what he is wearing.
    [ExportGroup("Outfits")]

    [Export] public Color[] ShirtColors { get; set; } = {
        new Color(0.65f, 0.22f, 0.22f),   // Office: red
        new Color(0.28f, 0.2f, 0.7f),     // Party: purple
        new Color(0.15f, 0.15f, 0.18f)    // Dark: black
    };
    [Export] public Color[] PantsColors { get; set; } = {
        new Color(0.25f, 0.28f, 0.38f),   // Office: slacks
        new Color(0.95f, 0.45f, 0.15f),   // Party: orange
        new Color(0.12f, 0.12f, 0.15f)    // Dark: black
    };

    // ── Mesh detail ──────────────────────────────
    // The chunkiness knob. Dropping these is how you trade smoothness for polygon count the
    // way the hardware this game imitates actually had to.
    [ExportGroup("Mesh detail")]

    [Export(PropertyHint.Range, "3,24,1")] public int CylinderSegments { get; set; } = 8;
    [Export(PropertyHint.Range, "3,24,1")] public int SphereSegments { get; set; } = 8;
    [Export(PropertyHint.Range, "2,16,1")] public int SphereRings { get; set; } = 6;

    // ── Standing pose ────────────────────────────
    // The relaxed garage stance. The riding rig overwrites all of this every frame from
    // PlayerManager.Animate, so these only show up on the podium and in the garage.
    [ExportGroup("Standing pose")]

    [Export] public Vector3 PoseSpine { get; set; } = new Vector3(-0.04f, 0f, 0f);
    [Export] public Vector3 PoseNeck { get; set; } = new Vector3(0.04f, 0f, 0f);
    [Export] public Vector3 PoseArmL { get; set; } = new Vector3(0.05f, 0f, -0.18f);
    [Export] public Vector3 PoseArmR { get; set; } = new Vector3(0.05f, 0f, 0.18f);
    [Export] public Vector3 PoseForearmL { get; set; } = new Vector3(-0.25f, 0f, 0f);
    [Export] public Vector3 PoseForearmR { get; set; } = new Vector3(-0.25f, 0f, 0f);
    [Export] public Vector3 PoseLegL { get; set; } = new Vector3(0.06f, 0f, 0f);
    [Export] public Vector3 PoseLegR { get; set; } = new Vector3(0.06f, 0f, 0f);
    [Export] public Vector3 PoseKneeL { get; set; } = new Vector3(-0.12f, 0f, 0f);
    [Export] public Vector3 PoseKneeR { get; set; } = new Vector3(-0.12f, 0f, 0f);

    // ── Derived ──────────────────────────────────

    /// <summary>
    /// Sole-to-hip height, derived rather than exported so his soles land on y=0 whatever you
    /// do to his legs. Callers use it to reason about where his centre of mass sits.
    /// </summary>
    public float HipHeight { get { return ThighLength + ShinLength + ShoeSize.Y; } }

    /// <summary>Safe lookup — an outfit array short of a CarlType still yields a colour.</summary>
    public Color ShirtFor(int index)
    {
        if (ShirtColors == null || ShirtColors.Length == 0) return Colors.Gray;
        return ShirtColors[Mathf.Clamp(index, 0, ShirtColors.Length - 1)];
    }

    public Color PantsFor(int index)
    {
        if (PantsColors == null || PantsColors.Length == 0) return Colors.DarkGray;
        return PantsColors[Mathf.Clamp(index, 0, PantsColors.Length - 1)];
    }
}
