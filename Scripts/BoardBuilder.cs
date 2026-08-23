using Godot;

/// <summary>
/// The one and only definition of a longboard mesh. Used by the player rig and by the
/// garage rack, so the deck you pick off the wall is literally the deck you ride.
///
/// Local space: deck sits at y=0, nose toward +Z, wheels hang below. Callers position the
/// returned node so the deck lands where they want it.
/// </summary>
public static class BoardBuilder
{
    /// <summary>Deck top surface, relative to the board origin — where a rider's soles go.</summary>
    public const float GripTopY = 0.0375f;

    public static Node3D Build(Main.BoardType type)
    {
        var root = new Node3D();

        float deckWidth = type == Main.BoardType.Natural ? 0.68f : 0.62f;

        var deckMat = MeshKit.Mat(Main.BoardDeckColors[(int)type]);
        if (type == Main.BoardType.Neon)
        {
            deckMat.EmissionEnabled = true;
            deckMat.Emission = Main.BoardDeckColors[(int)type];
            deckMat.EmissionEnergyMultiplier = 0.3f;
        }

        var gripMat = MeshKit.Mat(Main.BoardGripColors[(int)type], specular: true);
        var truckMat = MeshKit.Mat(new Color(0.62f, 0.62f, 0.65f), specular: true);
        var axleMat = MeshKit.Mat(new Color(0.55f, 0.55f, 0.58f), specular: true);
        var hubMat = MeshKit.Mat(new Color(0.45f, 0.45f, 0.48f), specular: true);
        var accentMat = MeshKit.Mat(AccentColor(type));
        var wheelMat = MeshKit.Mat(WheelColor(type), specular: true);

        // ── Deck: centre section, kicked nose, kicked tail ──
        MeshKit.Box(root, new Vector3(deckWidth, 0.045f, 1.4f), deckMat, Vector3.Zero);
        MeshKit.Box(root, new Vector3(deckWidth * 0.77f, 0.04f, 0.4f), deckMat, new Vector3(0, 0, 0.9f));
        MeshKit.Box(root, new Vector3(deckWidth * 0.77f, 0.04f, 0.35f), deckMat, new Vector3(0, 0, -0.88f));

        // ── Rail accent stripes down both edges ──
        float railX = deckWidth / 2f - 0.015f;
        MeshKit.Box(root, new Vector3(0.03f, 0.05f, 1.35f), accentMat, new Vector3(-railX, 0, 0));
        MeshKit.Box(root, new Vector3(0.03f, 0.05f, 1.35f), accentMat, new Vector3(railX, 0, 0));

        // ── Wood grain, on the two wooden decks only ──
        if (type == Main.BoardType.Classic || type == Main.BoardType.Natural)
        {
            var accent = AccentColor(type);
            var grainMat = MeshKit.Mat(new Color(accent.R * 0.8f, accent.G * 0.8f, accent.B * 0.8f));
            for (int i = 0; i < 3; i++)
            {
                float x = -deckWidth * 0.2f + i * deckWidth * 0.2f;
                MeshKit.Box(root, new Vector3(0.01f, 0.046f, 1.2f), grainMat, new Vector3(x, 0.0f, 0.05f));
            }
        }

        // ── Grip tape ──
        MeshKit.Box(root, new Vector3(deckWidth * 0.93f, 0.015f, 1.3f), gripMat, new Vector3(0, 0.03f, 0));

        // ── Trucks, axles, wheels ──
        foreach (float z in new[] { 0.55f, -0.55f })
        {
            MeshKit.Box(root, new Vector3(0.18f, 0.04f, 0.14f), truckMat, new Vector3(0, -0.05f, z));
            MeshKit.Cylinder(root, 0.015f, 0.015f, 0.58f, axleMat,
                new Vector3(0, -0.07f, z), new Vector3(0, 0, Mathf.Pi / 2f));

            foreach (float x in new[] { -0.30f, 0.30f })
            {
                var pos = new Vector3(x, -0.10f, z);
                MeshKit.Cylinder(root, 0.055f, 0.055f, 0.07f, wheelMat, pos,
                    new Vector3(0, 0, Mathf.Pi / 2f), segments: 6);
                MeshKit.Cylinder(root, 0.025f, 0.025f, 0.075f, hubMat, pos,
                    new Vector3(0, 0, Mathf.Pi / 2f), segments: 6);
            }
        }

        return root;
    }

    private static Color AccentColor(Main.BoardType type)
    {
        switch (type)
        {
            case Main.BoardType.Classic: return new Color(0.35f, 0.18f, 0.06f);  // dark brown
            case Main.BoardType.Neon:    return new Color(0.20f, 0.80f, 1.00f);  // cyan
            case Main.BoardType.Dark:    return new Color(0.50f, 0.10f, 0.70f);  // purple
            default:                     return new Color(0.65f, 0.50f, 0.30f);  // light wood
        }
    }

    private static Color WheelColor(Main.BoardType type)
    {
        switch (type)
        {
            case Main.BoardType.Classic: return new Color(0.12f, 0.12f, 0.12f);
            case Main.BoardType.Neon:    return new Color(0.15f, 0.15f, 0.15f);
            case Main.BoardType.Dark:    return new Color(0.10f, 0.10f, 0.12f);
            default:                     return new Color(0.18f, 0.16f, 0.14f);
        }
    }
}
