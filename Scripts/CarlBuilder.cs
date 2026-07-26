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
/// Local space: the returned root's origin is at the soles of his shoes and it carries no
/// rotation, so callers can drop him on a floor (or a deck) with a plain Position and turn
/// him with a plain Rotation. The sideways skate stance lives on the inner body group.
/// </summary>
public static class CarlBuilder
{
    /// <summary>Sole-to-hip height. Lets callers reason about where his centre of mass sits.</summary>
    public const float HipHeight = 0.48f;

    /// <summary>Sole to the top of his hair, for framing cameras on him.</summary>
    public const float StandingHeight = 1.56f;

    public static Node3D Build(Main.CarlType carl, out CarlJoints joints)
    {
        joints = new CarlJoints();

        var skinMat = MeshKit.Mat(new Color(0.9f, 0.78f, 0.6f));
        var shoeMat = MeshKit.Mat(new Color(0.14f, 0.14f, 0.14f), specular: true);
        var soleMat = MeshKit.Mat(new Color(0.08f, 0.08f, 0.08f), specular: true);
        var hairMat = MeshKit.Mat(new Color(0.3f, 0.18f, 0.08f), specular: true);
        var shirtMat = MeshKit.Mat(Main.CarlShirtColors[(int)carl]);
        var pantsMat = MeshKit.Mat(Main.CarlPantsColors[(int)carl]);

        // Root: origin at the soles, unrotated. Callers own placement and facing.
        var root = new Node3D();

        // Body group carries the sideways stance and lifts the rig so the soles land on y=0.
        var bodyGroup = new Node3D();
        bodyGroup.Position = new Vector3(0, HipHeight - 0.2f, 0);
        bodyGroup.Rotation = new Vector3(0, -Mathf.Pi / 2f, 0);
        root.AddChild(bodyGroup);
        joints.BodyGroup = bodyGroup;

        var hip = new Node3D();
        hip.Position = new Vector3(0, 0.2f, 0);
        bodyGroup.AddChild(hip);
        joints.Hip = hip;

        // Pelvis. The hip joint carried no geometry of its own, which left a bare 35cm gap
        // between the thigh tops and the bottom of the ribcage — invisible from the chase
        // camera, glaring when he's stood on a podium.
        MeshKit.Cylinder(hip, 0.14f, 0.13f, 0.38f, pantsMat, new Vector3(0, 0.19f, 0));

        // ── Torso ──
        var spine = new Node3D();
        spine.Position = new Vector3(0, 0.35f, 0);
        hip.AddChild(spine);
        joints.Spine = spine;

        MeshKit.Cylinder(spine, 0.13f, 0.11f, 0.18f, shirtMat, new Vector3(0, 0.09f, 0));
        MeshKit.Cylinder(spine, 0.15f, 0.13f, 0.22f, shirtMat, new Vector3(0, 0.29f, -0.02f));
        MeshKit.Cylinder(spine, 0.16f, 0.15f, 0.06f, shirtMat, new Vector3(0, 0.42f, -0.02f));

        // ── Head ──
        var neck = new Node3D();
        neck.Position = new Vector3(0, 0.46f, -0.02f);
        spine.AddChild(neck);
        joints.Neck = neck;

        MeshKit.Cylinder(neck, 0.05f, 0.06f, 0.08f, skinMat, new Vector3(0, 0.04f, 0));
        MeshKit.Sphere(neck, 0.12f, skinMat, new Vector3(0, 0.14f, -0.02f));
        MeshKit.Cylinder(neck, 0.11f, 0.13f, 0.06f, hairMat, new Vector3(0, 0.24f, -0.02f));

        // ── Arms ──
        joints.ArmL = BuildArm(spine, skinMat, -0.17f, out joints.ForearmL);
        joints.ArmR = BuildArm(spine, skinMat, 0.17f, out joints.ForearmR);

        // ── Legs ──
        joints.LegL = BuildLeg(hip, pantsMat, shoeMat, soleMat, -0.08f, out joints.KneeL);
        joints.LegR = BuildLeg(hip, pantsMat, shoeMat, soleMat, 0.08f, out joints.KneeR);

        return root;
    }

    private static Node3D BuildArm(Node3D spine, StandardMaterial3D skinMat, float x, out Node3D forearm)
    {
        var arm = new Node3D();
        arm.Position = new Vector3(x, 0.4f, -0.02f);
        spine.AddChild(arm);
        MeshKit.Cylinder(arm, 0.035f, 0.03f, 0.2f, skinMat, new Vector3(0, -0.1f, 0));

        forearm = new Node3D();
        forearm.Position = new Vector3(0, -0.2f, 0);
        arm.AddChild(forearm);
        MeshKit.Cylinder(forearm, 0.03f, 0.025f, 0.18f, skinMat, new Vector3(0, -0.09f, 0));
        MeshKit.Sphere(forearm, 0.03f, skinMat, new Vector3(0, -0.2f, 0));

        return arm;
    }

    private static Node3D BuildLeg(Node3D hip, StandardMaterial3D pantsMat, StandardMaterial3D shoeMat,
        StandardMaterial3D soleMat, float x, out Node3D knee)
    {
        var leg = new Node3D();
        leg.Position = new Vector3(x, 0f, 0);
        hip.AddChild(leg);
        MeshKit.Cylinder(leg, 0.06f, 0.055f, 0.22f, pantsMat, new Vector3(0, -0.11f, 0));

        knee = new Node3D();
        knee.Position = new Vector3(0, -0.22f, 0);
        leg.AddChild(knee);
        MeshKit.Cylinder(knee, 0.05f, 0.045f, 0.2f, pantsMat, new Vector3(0, -0.1f, 0));
        MeshKit.Box(knee, new Vector3(0.1f, 0.06f, 0.22f), shoeMat, new Vector3(0, -0.22f, -0.02f));
        MeshKit.Box(knee, new Vector3(0.1f, 0.02f, 0.22f), soleMat, new Vector3(0, -0.25f, -0.02f));

        return leg;
    }

    /// <summary>
    /// The relaxed standing pose used in the garage — arms loose at his sides, slight knee
    /// bend. The riding rig overwrites all of this every frame from PlayerManager.Animate.
    /// </summary>
    public static void PoseStanding(CarlJoints j)
    {
        j.Spine.Rotation = new Vector3(-0.04f, 0f, 0f);
        j.Neck.Rotation = new Vector3(0.04f, 0f, 0f);
        j.ArmL.Rotation = new Vector3(0.05f, 0f, -0.18f);
        j.ArmR.Rotation = new Vector3(0.05f, 0f, 0.18f);
        j.ForearmL.Rotation = new Vector3(-0.25f, 0f, 0f);
        j.ForearmR.Rotation = new Vector3(-0.25f, 0f, 0f);
        j.LegL.Rotation = new Vector3(0.06f, 0f, 0f);
        j.LegR.Rotation = new Vector3(0.06f, 0f, 0f);
        j.KneeL.Rotation = new Vector3(-0.12f, 0f, 0f);
        j.KneeR.Rotation = new Vector3(-0.12f, 0f, 0f);
    }
}
