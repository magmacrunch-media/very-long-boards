using Godot;

/// <summary>
/// Procedural surface textures, generated in code and shared. Sibling to <see cref="MeshKit"/>:
/// MeshKit makes the shapes, TextureKit gives them a surface — nothing is loaded from disk, so
/// the whole art pipeline stays in the repo as code.
///
/// Everything here is 32x32. The N64 could hold roughly 4 KB of texture at a time, and its look
/// comes from tiny maps tiled hard and smeared by trilinear filtering. Godot's default
/// linear+mipmap filtering *is* that smear, so materials leave TextureFilter alone.
///
/// The noise is lattice-wrapped at the octave period, so every texture tiles seamlessly — a
/// visible repeat line across 300 m of ground would be far more obvious than the tiling itself.
///
/// Each texture is built on first use and cached; every material shares the one instance.
/// </summary>
public static class TextureKit
{
    private const int Size = 32;

    private static ImageTexture _asphalt, _grass, _dirt, _bark, _pine, _leaf, _rock, _plank;

    /// <summary>Worn tarmac — the road surface.</summary>
    public static ImageTexture Asphalt => _asphalt ??= Fbm(
        new Color(0.24f, 0.24f, 0.26f), new Color(0.33f, 0.33f, 0.35f), 8, 3, 11);

    /// <summary>Summer grass — the verges and the ground plane.</summary>
    public static ImageTexture Grass => _grass ??= Fbm(
        new Color(0.14f, 0.34f, 0.09f), new Color(0.24f, 0.48f, 0.16f), 6, 3, 23);

    /// <summary>Dry dirt — the shoulder either side of the tarmac.</summary>
    public static ImageTexture Dirt => _dirt ??= Fbm(
        new Color(0.38f, 0.31f, 0.22f), new Color(0.52f, 0.44f, 0.33f), 6, 3, 37);

    /// <summary>Tree bark — streaked along the trunk.</summary>
    public static ImageTexture Bark => _bark ??= Streaks(
        new Color(0.30f, 0.21f, 0.13f), new Color(0.48f, 0.35f, 0.22f), 7, 53, vertical: true);

    /// <summary>Pine needle mass — dark and mottled.</summary>
    public static ImageTexture Pine => _pine ??= Fbm(
        new Color(0.05f, 0.16f, 0.06f), new Color(0.12f, 0.30f, 0.12f), 5, 2, 67);

    /// <summary>Deciduous canopy — brighter, coarser clumps.</summary>
    public static ImageTexture Leaf => _leaf ??= Fbm(
        new Color(0.14f, 0.38f, 0.09f), new Color(0.30f, 0.58f, 0.18f), 4, 2, 83);

    /// <summary>Granite — New Hampshire's whole personality.</summary>
    public static ImageTexture Rock => _rock ??= Fbm(
        new Color(0.44f, 0.43f, 0.41f), new Color(0.64f, 0.63f, 0.60f), 5, 3, 97);

    /// <summary>Sawn planks — bridge decks, houses, fences.</summary>
    public static ImageTexture Plank => _plank ??= Streaks(
        new Color(0.34f, 0.24f, 0.14f), new Color(0.50f, 0.37f, 0.23f), 6, 113, vertical: false);

    // ═══════════════════════════════════════════
    //  CUTOUTS
    // ═══════════════════════════════════════════
    // Foliage silhouettes for crossed billboard quads — what N64 trees actually were.
    // Unlike everything above these must NOT tile: they are a shape, not a surface, so the
    // noise rags the edge rather than wrapping.

    private const int CutSize = 64;
    private static ImageTexture _pineCut, _leafCut;

    /// <summary>Conifer silhouette — tapered, ragged, alpha-cut.</summary>
    public static ImageTexture PineBillboard => _pineCut ??= Cutout(
        new Color(0.06f, 0.19f, 0.07f), new Color(0.14f, 0.34f, 0.14f), conifer: true, seed: 131);

    /// <summary>Broadleaf canopy silhouette — a ragged blob.</summary>
    public static ImageTexture LeafBillboard => _leafCut ??= Cutout(
        new Color(0.15f, 0.40f, 0.10f), new Color(0.32f, 0.60f, 0.20f), conifer: false, seed: 149);

    /// <summary>
    /// Build a silhouette: a shape mask raggedised by noise, thresholded into alpha. Colour
    /// varies across the clump so the mass doesn't read as one flat cut-out.
    /// </summary>
    private static ImageTexture Cutout(Color lo, Color hi, bool conifer, int seed)
    {
        var img = Image.CreateEmpty(CutSize, CutSize, false, Image.Format.Rgba8);
        for (int y = 0; y < CutSize; y++)
        {
            for (int x = 0; x < CutSize; x++)
            {
                float u = x / (float)(CutSize - 1);          // 0..1 across
                float v = y / (float)(CutSize - 1);          // 0 at the top
                float dx = Mathf.Abs(u - 0.5f) * 2f;         // 0 centre, 1 edge

                // Conifers taper to a point; canopies are round.
                float mask = conifer
                    ? 1f - dx / Mathf.Max(0.12f, v * 1.05f)
                    : 1f - Mathf.Sqrt(dx * dx + Mathf.Pow((v - 0.45f) * 2.1f, 2f));

                // Rag the edge with the same noise the tiling textures use.
                float n = Fractal(x, y, 6, 3, seed, 1f);
                float alpha = mask - (1f - n) * 0.55f;

                var c = lo.Lerp(hi, Fractal(x, y, 3, 2, seed + 7, 1f));
                c.A = alpha > 0.5f ? 1f : 0f;                // hard cut — alpha scissor, not blend
                img.SetPixel(x, y, c);
            }
        }
        return ImageTexture.CreateFromImage(img);
    }

    // ═══════════════════════════════════════════
    //  GENERATORS
    // ═══════════════════════════════════════════

    /// <summary>Fractal value noise between two colours. The organic surfaces.</summary>
    private static ImageTexture Fbm(Color lo, Color hi, int period, int octaves, int seed)
    {
        var img = Image.CreateEmpty(Size, Size, false, Image.Format.Rgb8);
        for (int y = 0; y < Size; y++)
        {
            for (int x = 0; x < Size; x++)
                img.SetPixel(x, y, lo.Lerp(hi, Fractal(x, y, period, octaves, seed, 1f)));
        }
        return ImageTexture.CreateFromImage(img);
    }

    /// <summary>
    /// Noise stretched along one axis, which is all bark and planks really are. Vertical
    /// streaks run up a trunk; horizontal ones read as sawn boards.
    /// </summary>
    private static ImageTexture Streaks(Color lo, Color hi, int period, int seed, bool vertical)
    {
        var img = Image.CreateEmpty(Size, Size, false, Image.Format.Rgb8);
        for (int y = 0; y < Size; y++)
        {
            for (int x = 0; x < Size; x++)
            {
                // Squash the lattice on the streak axis so features smear along it.
                float n = vertical
                    ? Fractal(x, y, period, 2, seed, 0.25f)
                    : Fractal(y, x, period, 2, seed, 0.25f);
                img.SetPixel(x, y, lo.Lerp(hi, n));
            }
        }
        return ImageTexture.CreateFromImage(img);
    }

    /// <summary>Sum octaves of wrapped value noise, normalised to 0..1.</summary>
    private static float Fractal(int px, int py, int period, int octaves, int seed, float squash)
    {
        float sum = 0f, norm = 0f, amp = 1f;
        for (int o = 0; o < octaves; o++)
        {
            int p = period << o;
            float fx = px / (float)Size * p;
            float fy = py / (float)Size * p * squash;
            sum += Value(fx, fy, p, seed + o) * amp;
            norm += amp;
            amp *= 0.5f;
        }
        return sum / norm;
    }

    /// <summary>
    /// Value noise on a lattice that wraps at <paramref name="period"/>, so the tile is seamless.
    /// </summary>
    private static float Value(float x, float y, int period, int seed)
    {
        int x0 = Mathf.FloorToInt(x), y0 = Mathf.FloorToInt(y);
        float fx = x - x0, fy = y - y0;

        int xa = Wrap(x0, period), xb = Wrap(x0 + 1, period);
        int ya = Wrap(y0, period), yb = Wrap(y0 + 1, period);

        // Smoothstep the interpolation so the lattice doesn't show as a grid.
        float u = fx * fx * (3f - 2f * fx);
        float v = fy * fy * (3f - 2f * fy);

        return Mathf.Lerp(
            Mathf.Lerp(Hash(xa, ya, seed), Hash(xb, ya, seed), u),
            Mathf.Lerp(Hash(xa, yb, seed), Hash(xb, yb, seed), u), v);
    }

    private static int Wrap(int v, int period)
    {
        int m = v % period;
        return m < 0 ? m + period : m;
    }

    /// <summary>Deterministic 0..1 hash of a lattice point.</summary>
    private static float Hash(int x, int y, int seed)
    {
        unchecked
        {
            int h = x * 374761393 + y * 668265263 + seed * 1274126177;
            h = (h ^ (h >> 13)) * 1274126177;
            h ^= h >> 16;
            return (h & 0x7fffffff) / (float)0x7fffffff;
        }
    }
}
