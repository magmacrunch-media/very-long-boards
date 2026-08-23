using Godot;

/// <summary>
/// Primitive mesh helpers shared by the model builders, the garage set dressing and the
/// roadside scenery. Every piece of geometry in this game is a box, a cylinder or a sphere,
/// so these calls cover the whole art pipeline.
///
/// Pass <c>null</c> as the parent to get an unattached mesh back — the scenery builds props
/// as loose nodes and parents them itself once they're assembled.
/// </summary>
public static class MeshKit
{
    /// <summary>Flat-shaded material with specular off — the default look for everything.</summary>
    public static StandardMaterial3D Mat(Color color, bool specular = false)
    {
        var mat = new StandardMaterial3D();
        mat.AlbedoColor = color;
        if (!specular) mat.SpecularMode = BaseMaterial3D.SpecularModeEnum.Disabled;
        return mat;
    }

    /// <summary>Material that glows. Used for neon, poster highlights and podium rings.</summary>
    public static StandardMaterial3D EmissiveMat(Color color, Color emission, float energy = 1f)
    {
        var mat = Mat(color);
        mat.EmissionEnabled = true;
        mat.Emission = emission;
        mat.EmissionEnergyMultiplier = energy;
        return mat;
    }

    public static MeshInstance3D Box(Node3D parent, Vector3 size, StandardMaterial3D mat, Vector3 pos = default)
    {
        var m = new MeshInstance3D();
        var mesh = new BoxMesh();
        mesh.Size = size;
        m.Mesh = mesh;
        m.MaterialOverride = mat;
        m.Position = pos;
        if (parent != null) parent.AddChild(m);
        return m;
    }

    public static MeshInstance3D Box(Node3D parent, Vector3 size, Color color, Vector3 pos = default)
    {
        return Box(parent, size, Mat(color), pos);
    }

    public static MeshInstance3D Cylinder(Node3D parent, float topR, float bottomR, float height,
        StandardMaterial3D mat, Vector3 pos = default, Vector3 rot = default, int segments = 8)
    {
        var m = new MeshInstance3D();
        var mesh = new CylinderMesh();
        mesh.TopRadius = topR;
        mesh.BottomRadius = bottomR;
        mesh.Height = height;
        mesh.RadialSegments = segments;
        m.Mesh = mesh;
        m.MaterialOverride = mat;
        m.Position = pos;
        m.Rotation = rot;
        if (parent != null) parent.AddChild(m);
        return m;
    }

    /// <summary>Uniform-radius cylinder — trunks, posts, rails and the rest of the scenery.</summary>
    public static MeshInstance3D Cylinder(Node3D parent, float radius, float height,
        StandardMaterial3D mat, Vector3 pos = default, int segments = 8)
    {
        return Cylinder(parent, radius, radius, height, mat, pos, Vector3.Zero, segments);
    }

    public static MeshInstance3D Sphere(Node3D parent, float radius, StandardMaterial3D mat,
        Vector3 pos = default, int segments = 8)
    {
        var m = new MeshInstance3D();
        var mesh = new SphereMesh();
        mesh.Radius = radius;
        mesh.Height = radius * 2f;
        mesh.Rings = 6;
        mesh.RadialSegments = segments;
        m.Mesh = mesh;
        m.MaterialOverride = mat;
        m.Position = pos;
        if (parent != null) parent.AddChild(m);
        return m;
    }

    private const string BaseAlbedoMeta = "vlb_base_albedo";
    private const string BaseEmissionMeta = "vlb_base_emission";

    /// <summary>
    /// Recursively scale every material's brightness — used to dim unselected rack boards and
    /// posters. Idempotent: the first call caches each mesh's pristine colour in metadata, so
    /// repeated calls scale from the original rather than compounding.
    /// </summary>
    public static void Tint(Node node, float factor)
    {
        if (node is MeshInstance3D mi && mi.MaterialOverride is StandardMaterial3D mat)
        {
            if (!mi.HasMeta(BaseAlbedoMeta))
            {
                mi.SetMeta(BaseAlbedoMeta, mat.AlbedoColor);
                mi.SetMeta(BaseEmissionMeta, mat.EmissionEnergyMultiplier);
            }

            // Duplicate so meshes sharing a material instance aren't dimmed together.
            var copy = (StandardMaterial3D)mat.Duplicate();
            var b = (Color)mi.GetMeta(BaseAlbedoMeta);
            copy.AlbedoColor = new Color(b.R * factor, b.G * factor, b.B * factor, b.A);
            if (copy.EmissionEnabled)
                copy.EmissionEnergyMultiplier = (float)mi.GetMeta(BaseEmissionMeta) * factor;
            mi.MaterialOverride = copy;
        }
        foreach (var child in node.GetChildren())
            Tint(child, factor);
    }
}
