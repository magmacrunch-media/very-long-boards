using Godot;

/// <summary>
/// Editor-only change detection for the design resources.
///
/// The preview scenes subscribe to <see cref="Resource.Changed"/>, but that signal is not
/// reliably emitted when a scripted resource is edited through the Inspector — and it is
/// never emitted for an edit made to a nested resource, like one <see cref="SineLayer"/>
/// inside a <see cref="CourseDesign"/>. So the previews also poll: cheap enough at a few
/// dozen properties a frame, and it means a slider drag always redraws.
/// </summary>
public static class DesignWatcher
{
    private const int MaxDepth = 2;

    /// <summary>
    /// A hash over everything a resource would serialise, following nested resources so an
    /// edit to a SineLayer inside an array still moves the number.
    /// </summary>
    public static int Signature(Resource resource, int depth = 0)
    {
        if (resource == null || depth > MaxDepth) return 0;

        int hash = 17;
        foreach (var entry in resource.GetPropertyList())
        {
            var usage = (PropertyUsageFlags)entry["usage"].AsInt64();
            if ((usage & PropertyUsageFlags.Storage) == 0) continue;

            string name = entry["name"].AsString();
            hash = hash * 31 + name.GetHashCode();
            hash = hash * 31 + HashValue(resource.Get(name), depth);
        }
        return hash;
    }

    private static int HashValue(Variant value, int depth)
    {
        switch (value.VariantType)
        {
            // Nested resource — recurse, or its object id would be all we compared.
            case Variant.Type.Object:
                return Signature(value.As<Resource>(), depth + 1);

            // Typed arrays of resources arrive here; packed arrays (Color[]) do not, and
            // GD.Hash already walks those element by element.
            case Variant.Type.Array:
                int hash = 7;
                foreach (Variant item in value.AsGodotArray())
                    hash = hash * 31 + HashValue(item, depth);
                return hash;

            default:
                return GD.Hash(value);
        }
    }
}
