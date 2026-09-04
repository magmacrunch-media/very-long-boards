using Godot;

/// <summary>
/// The one and only definition of a longboard mesh. Used by the player rig and by the
/// garage rack, so the deck you pick off the wall is literally the deck you ride.
///
/// Dimensions and colourways come from a <see cref="BoardDesign"/> — edit
/// <c>Resources/Design/Board.tres</c>. Pass null and you get the stock build.
///
/// Local space: deck sits at y=0, nose toward +Z, wheels hang below. Callers position the
/// returned node so the deck lands where they want it.
/// </summary>
public static class BoardBuilder
{
    public static Node3D Build(Main.BoardType type, BoardDesign design)
    {
        var d = design ?? new BoardDesign();
        var root = new Node3D();
        int i = (int)type;

        float deckWidth = d.WidthFor(type);
        float kickWidth = deckWidth * d.KickWidthRatio;

        var deckMat = MeshKit.Mat(d.DeckFor(i));
        if (type == Main.BoardType.Neon && d.NeonGlow > 0f)
        {
            deckMat.EmissionEnabled = true;
            deckMat.Emission = d.DeckFor(i);
            deckMat.EmissionEnergyMultiplier = d.NeonGlow;
        }

        var gripMat = MeshKit.Mat(d.GripFor(i), specular: true);
        var truckMat = MeshKit.Mat(d.TruckColor, specular: true);
        var axleMat = MeshKit.Mat(d.AxleColor, specular: true);
        var hubMat = MeshKit.Mat(d.HubColor, specular: true);
        var accentMat = MeshKit.Mat(d.AccentFor(i));
        var wheelMat = MeshKit.Mat(d.WheelFor(i), specular: true);

        // ── Deck: centre section, kicked nose, kicked tail ──
        float kickThickness = d.DeckThickness - 0.005f;
        MeshKit.Box(root, new Vector3(deckWidth, d.DeckThickness, d.DeckLength), deckMat, Vector3.Zero);
        MeshKit.Box(root, new Vector3(kickWidth, kickThickness, d.NoseLength), deckMat,
            new Vector3(0, 0, d.DeckLength / 2f + d.NoseLength / 2f));
        MeshKit.Box(root, new Vector3(kickWidth, kickThickness, d.TailLength), deckMat,
            new Vector3(0, 0, -(d.DeckLength / 2f + d.TailLength / 2f)));

        // ── Rail accent stripes down both edges ──
        float railX = deckWidth / 2f - d.RailWidth / 2f;
        var railSize = new Vector3(d.RailWidth, d.DeckThickness + 0.005f, d.DeckLength * 0.964f);
        MeshKit.Box(root, railSize, accentMat, new Vector3(-railX, 0, 0));
        MeshKit.Box(root, railSize, accentMat, new Vector3(railX, 0, 0));

        // ── Deck graphic, screen-printed on the underside ──
        //
        // A quad rather than a texture on the deck itself. Godot's BoxMesh packs its six
        // faces into a 3x2 atlas, so the underside would get one sixth of the image, rotated;
        // and the deck is three boxes, so it would get that sixth three times. A quad takes
        // the whole image once.
        //
        // A cutout, so the colourway shows around the ink and one graphic serves all four
        // boards. Inside the rails and short of the kicks, because those are narrower boxes
        // and a decal spanning the full 2.15m would hang over their edges.
        //
        // Rotation (PI/2, PI/2, 0) faces it down with u running from the nose to the tail,
        // which is the way the chevrons point.
        //
        // Depth is measured off the GRAIN, not off the deck. The grain slats are
        // DeckThickness + 0.001 tall so they stand proud on top, which also drops their
        // undersides half a millimetre below the deck's — sitting the decal on the deck
        // face left the two coplanar and the grain striped straight through the chevrons.
        var graphicMat = MeshKit.CutoutMat(DeckChevron.Texture, Colors.White);
        MeshKit.Quad(root,
            new Vector2(d.DeckLength * 0.95f, deckWidth * 0.85f),
            graphicMat,
            new Vector3(0, -(d.DeckThickness + 0.001f) / 2f - 0.0005f, 0),
            new Vector3(Mathf.Pi / 2f, Mathf.Pi / 2f, 0));

        // ── Wood grain, on the two wooden decks only ──
        if (type == Main.BoardType.Classic || type == Main.BoardType.Natural)
        {
            var accent = d.AccentFor(i);
            var grainMat = MeshKit.Mat(new Color(accent.R * 0.8f, accent.G * 0.8f, accent.B * 0.8f));
            for (int g = 0; g < 3; g++)
            {
                float x = -deckWidth * 0.2f + g * deckWidth * 0.2f;
                MeshKit.Box(root, new Vector3(0.01f, d.DeckThickness + 0.001f, d.DeckLength * 0.857f),
                    grainMat, new Vector3(x, 0.0f, 0.05f));
            }
        }

        // ── Grip tape ──
        MeshKit.Box(root, new Vector3(deckWidth * 0.93f, d.GripThickness, d.DeckLength * 0.929f),
            gripMat, new Vector3(0, d.GripTopY - d.GripThickness / 2f, 0));

        // ── Trucks, axles, wheels ──
        foreach (float z in new[] { d.TruckOffsetZ, -d.TruckOffsetZ })
        {
            MeshKit.Box(root, new Vector3(0.18f, 0.04f, 0.14f), truckMat, new Vector3(0, -0.05f, z));
            MeshKit.Cylinder(root, 0.015f, 0.015f, d.AxleLength, axleMat,
                new Vector3(0, -0.07f, z), new Vector3(0, 0, Mathf.Pi / 2f));

            foreach (float x in new[] { -d.AxleHalfWidth, d.AxleHalfWidth })
            {
                var pos = new Vector3(x, d.WheelY, z);
                MeshKit.Cylinder(root, d.WheelRadius, d.WheelRadius, d.WheelWidth, wheelMat, pos,
                    new Vector3(0, 0, Mathf.Pi / 2f), d.WheelSegments);
                MeshKit.Cylinder(root, d.WheelRadius * 0.455f, d.WheelRadius * 0.455f,
                    d.WheelWidth + 0.005f, hubMat, pos,
                    new Vector3(0, 0, Mathf.Pi / 2f), d.WheelSegments);
            }
        }

        return root;
    }
}
