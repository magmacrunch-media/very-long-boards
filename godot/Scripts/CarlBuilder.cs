using Godot;

/// <summary>
/// Joint handles for a built Carl, so the caller can drive the procedural animation.
/// The garage just ignores these and leaves him in the default pose.
/// </summary>
public class CarlJoints
{
    public Node3D BodyGroup;
    public Node3D Hip;
    public Node3D Spine;
    public Node3D Neck;
    public Node3D ArmL;
    public Node3D ForearmL;
    public Node3D ArmR;
    public Node3D ForearmR;
    public Node3D LegL;
    public Node3D KneeL;
    public Node3D LegR;
    public Node3D KneeR;
}

/// <summary>
/// The one and only definition of Carl Spatski. There's one Carl — the three
/// <see cref="Main.CarlType"/> values are just his outfit.
///
/// Every dimension and colour comes from a <see cref="CarlDesign"/>, so reshaping him is a
/// matter of dragging sliders in <c>Resources/Design/Carl.tres</c> rather than editing this
/// file. Pass null and you get the stock build.
///
/// Local space: the returned root's origin is at the soles of his shoes and it carries no
/// rotation, so callers can drop him on a floor (or a deck) with a plain Position and turn
/// him with a plain Rotation. The sideways skate stance lives on the inner body group.
/// </summary>
public static class CarlBuilder
{
    // The spine stack, in spine-local metres at TorsoHeight 1. These are ratios rather than
    // knobs: the design scales the whole stack together so the head and arms stay attached.
    private const float HipLocalY = 0.2f;      // hip joint above the body group origin
    private const float SpineLocalY = 0.35f;   // spine joint above the hip
    private const float PelvisLocalY = 0.19f;  // pelvis cylinder centre above the hip
    private const float WaistSegY = 0.09f;
    private const float WaistSegH = 0.18f;
    private const float ChestSegY = 0.29f;
    private const float ChestSegH = 0.22f;
    private const float ShoulderSegY = 0.42f;
    private const float ShoulderSegH = 0.06f;
    private const float NeckJointY = 0.46f;
    private const float ArmJointY = 0.4f;

    /// <summary>Gap between the end of the forearm and the centre of the hand sphere.</summary>
    private const float WristGap = 0.02f;

    public static Node3D Build(Main.CarlType carl, CarlDesign design, out CarlJoints joints)
    {
        var d = design ?? new CarlDesign();
        joints = new CarlJoints();

        var skinMat = MeshKit.Mat(d.SkinColor);
        // The head gets its own material because the face is a texture and the rest of him is
        // not — sharing skinMat would paint a face on his hands. It keeps d.SkinColor, because
        // AlbedoColor MULTIPLIES AlbedoTexture and CarlFace is a tint map: white where the
        // skin shows, dark grey for the features. So the slider still drives his complexion
        // and the brows and mouth darken with it, instead of the head being the one part of
        // him the Palette group cannot reach.
        var faceMat = MeshKit.Mat(d.SkinColor, texture: CarlFace.Texture);
        // u = 0 lands on +Z, which is Carl's forward, and the face is drawn mid-image. Half a
        // turn brings them together. See CarlFace for why it is drawn that way.
        faceMat.Uv1Offset = new Vector3(0.5f, 0f, 0f);
        var shoeMat = MeshKit.Mat(d.ShoeColor, specular: true);
        var soleMat = MeshKit.Mat(d.SoleColor, specular: true);
        var hairMat = MeshKit.Mat(d.HairColor, specular: true);
        var shirtMat = MeshKit.Mat(d.ShirtFor((int)carl));
        var pantsMat = MeshKit.Mat(d.PantsFor((int)carl));

        int cyl = d.CylinderSegments;
        float t = d.TorsoHeight;
        float lean = d.TorsoLean;

        // Root: origin at the soles, unrotated. Callers own placement and facing.
        var root = new Node3D();

        // Body group carries the sideways stance and lifts the rig so the soles land on y=0.
        var bodyGroup = new Node3D();
        bodyGroup.Position = new Vector3(0, d.HipHeight - HipLocalY, 0);
        bodyGroup.Rotation = new Vector3(0, -Mathf.Pi / 2f, 0);
        root.AddChild(bodyGroup);
        joints.BodyGroup = bodyGroup;

        var hip = new Node3D();
        hip.Position = new Vector3(0, HipLocalY, 0);
        bodyGroup.AddChild(hip);
        joints.Hip = hip;

        // Pelvis. The hip joint carried no geometry of its own, which left a bare 35cm gap
        // between the thigh tops and the bottom of the ribcage — invisible from the chase
        // camera, glaring when he's stood on a podium.
        MeshKit.Cylinder(hip, d.PelvisTopRadius, d.PelvisBottomRadius, d.PelvisHeight, pantsMat,
            new Vector3(0, PelvisLocalY, 0), Vector3.Zero, cyl);

        // ── Torso ──
        var spine = new Node3D();
        spine.Position = new Vector3(0, SpineLocalY, 0);
        hip.AddChild(spine);
        joints.Spine = spine;

        MeshKit.Cylinder(spine, d.WaistRadius, d.WaistBottomRadius, WaistSegH * t, shirtMat,
            new Vector3(0, WaistSegY * t, 0), Vector3.Zero, cyl);
        MeshKit.Cylinder(spine, d.ChestRadius, d.WaistRadius, ChestSegH * t, shirtMat,
            new Vector3(0, ChestSegY * t, -lean), Vector3.Zero, cyl);
        MeshKit.Cylinder(spine, d.ShoulderRadius, d.ChestRadius, ShoulderSegH * t, shirtMat,
            new Vector3(0, ShoulderSegY * t, -lean), Vector3.Zero, cyl);

        // ── Head ──
        var neck = new Node3D();
        neck.Position = new Vector3(0, NeckJointY * t, -lean);
        spine.AddChild(neck);
        joints.Neck = neck;

        MeshKit.Cylinder(neck, d.NeckTopRadius, d.NeckBottomRadius, d.NeckHeight, skinMat,
            new Vector3(0, d.NeckHeight / 2f, 0), Vector3.Zero, cyl);
        MeshKit.Sphere(neck, d.HeadRadius, faceMat, new Vector3(0, d.HeadOffsetY, -lean),
            d.SphereSegments, d.SphereRings);
        MeshKit.Cylinder(neck, d.HairTopRadius, d.HairBottomRadius, d.HairHeight, hairMat,
            new Vector3(0, d.HairOffsetY, -lean), Vector3.Zero, cyl);

        // ── Arms ──
        joints.ArmL = BuildArm(spine, skinMat, d, -d.ShoulderOffsetX, out joints.ForearmL);
        joints.ArmR = BuildArm(spine, skinMat, d, d.ShoulderOffsetX, out joints.ForearmR);

        // ── Legs ──
        joints.LegL = BuildLeg(hip, pantsMat, shoeMat, soleMat, d, -d.HipOffsetX, out joints.KneeL);
        joints.LegR = BuildLeg(hip, pantsMat, shoeMat, soleMat, d, d.HipOffsetX, out joints.KneeR);

        return root;
    }

    private static Node3D BuildArm(Node3D spine, StandardMaterial3D skinMat, CarlDesign d,
        float x, out Node3D forearm)
    {
        int cyl = d.CylinderSegments;

        var arm = new Node3D();
        arm.Position = new Vector3(x, ArmJointY * d.TorsoHeight, -d.TorsoLean);
        spine.AddChild(arm);
        MeshKit.Cylinder(arm, d.UpperArmTopRadius, d.UpperArmBottomRadius, d.UpperArmLength, skinMat,
            new Vector3(0, -d.UpperArmLength / 2f, 0), Vector3.Zero, cyl);

        forearm = new Node3D();
        forearm.Position = new Vector3(0, -d.UpperArmLength, 0);
        arm.AddChild(forearm);
        MeshKit.Cylinder(forearm, d.ForearmTopRadius, d.ForearmBottomRadius, d.ForearmLength, skinMat,
            new Vector3(0, -d.ForearmLength / 2f, 0), Vector3.Zero, cyl);
        MeshKit.Sphere(forearm, d.HandRadius, skinMat,
            new Vector3(0, -d.ForearmLength - WristGap, 0), d.SphereSegments, d.SphereRings);

        return arm;
    }

    private static Node3D BuildLeg(Node3D hip, StandardMaterial3D pantsMat, StandardMaterial3D shoeMat,
        StandardMaterial3D soleMat, CarlDesign d, float x, out Node3D knee)
    {
        int cyl = d.CylinderSegments;

        var leg = new Node3D();
        leg.Position = new Vector3(x, 0f, 0);
        hip.AddChild(leg);
        MeshKit.Cylinder(leg, d.ThighTopRadius, d.ThighBottomRadius, d.ThighLength, pantsMat,
            new Vector3(0, -d.ThighLength / 2f, 0), Vector3.Zero, cyl);

        knee = new Node3D();
        knee.Position = new Vector3(0, -d.ThighLength, 0);
        leg.AddChild(knee);
        MeshKit.Cylinder(knee, d.ShinTopRadius, d.ShinBottomRadius, d.ShinLength, pantsMat,
            new Vector3(0, -d.ShinLength / 2f, 0), Vector3.Zero, cyl);

        // Measured down from the knee so the sole lands exactly on the root's y=0 plane,
        // whatever the leg lengths are — HipHeight is derived from them for the same reason.
        float ankleToGround = d.ShinLength + d.ShoeSize.Y;
        float shoeY = d.SoleThickness / 2f + d.ShoeSize.Y / 2f - ankleToGround;
        float soleY = d.SoleThickness / 2f - ankleToGround;
        MeshKit.Box(knee, d.ShoeSize, shoeMat, new Vector3(0, shoeY, d.ShoeForward));
        MeshKit.Box(knee, new Vector3(d.ShoeSize.X, d.SoleThickness, d.ShoeSize.Z), soleMat,
            new Vector3(0, soleY, d.ShoeForward));

        return leg;
    }

    /// <summary>
    /// The relaxed standing pose used in the garage — arms loose at his sides, slight knee
    /// bend. The riding rig overwrites all of this every frame from PlayerManager.Animate.
    /// </summary>
    public static void PoseStanding(CarlJoints j, CarlDesign design)
    {
        var d = design ?? new CarlDesign();
        j.Spine.Rotation = d.PoseSpine;
        j.Neck.Rotation = d.PoseNeck;
        j.ArmL.Rotation = d.PoseArmL;
        j.ArmR.Rotation = d.PoseArmR;
        j.ForearmL.Rotation = d.PoseForearmL;
        j.ForearmR.Rotation = d.PoseForearmR;
        j.LegL.Rotation = d.PoseLegL;
        j.LegR.Rotation = d.PoseLegR;
        j.KneeL.Rotation = d.PoseKneeL;
        j.KneeR.Rotation = d.PoseKneeR;
    }
}
