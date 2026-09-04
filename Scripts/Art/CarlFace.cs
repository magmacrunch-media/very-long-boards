using System.Collections.Generic;
using Godot;

/// <summary>
/// Carl's face, drawn in SPRITE//FORGE.
///
/// A TINT MAP, not a picture of his face: Godot multiplies albedo_color by
/// albedo_texture, so the field is white and comes back as exactly CarlDesign.SkinColor,
/// and the features are a neutral dark grey that darkens the skin without shifting its
/// hue. Recolour his skin on the slider and the brows and mouth follow. Painting the skin
/// tone into these texels instead would double-darken them and freeze the slider out.
///
/// 64x32 because Godot's SphereMesh unwraps equirectangularly: u covers a full 360 degrees
/// and v covers 180, so 2:1 is the only aspect that lands on it unstretched. The face is
/// drawn at the CENTRE of the image, where an artist naturally puts it, but u=0 is +Z,
/// which is Carl's forward - so CarlBuilder gives the material a Uv1Offset of 0.5 to line
/// the two up. Drawing the face split across the left and right edges would need no
/// offset, and is much harder to draw.
///
/// GENERATED from <c>Art/carl_face.forge</c> — re-export rather than editing
/// a row here by hand. 64x32, 2 colours.
/// </summary>
public static class CarlFace
{
    private static ImageTexture _texture;

    /// <summary>Built on first use and shared, the same way <see cref="TextureKit"/> does it.</summary>
    public static ImageTexture Texture => _texture ??= ForgeArt.FromRows(Rows, Key);

    private static readonly Dictionary<char, Color> Key = new()
    {
        ['a'] = new Color(0.227f, 0.227f, 0.227f),   // #3a3a3a
        ['n'] = new Color(1.000f, 1.000f, 1.000f),   // #ffffff
    };

    private static readonly string[] Rows =
    {
        "nnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnn",
        "nnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnn",
        "nnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnn",
        "nnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnn",
        "nnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnn",
        "nnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnn",
        "nnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnn",
        "nnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnn",
        "nnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnn",
        "nnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnn",
        "nnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnn",
        "nnnnnnnnnnnnnnnnnnnnnnnnnaaaaaannnaaaaaannnnnnnnnnnnnnnnnnnnnnnn",
        "nnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnn",
        "nnnnnnnnnnnnnnnnnnnnnnnnnnaaannnnnnnaaannnnnnnnnnnnnnnnnnnnnnnnn",
        "nnnnnnnnnnnnnnnnnnnnnnnnnnaaannnnnnnaaannnnnnnnnnnnnnnnnnnnnnnnn",
        "nnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnn",
        "nnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnannnnnnnnnnnnnnnnnnnnnnnnnnnnnnn",
        "nnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnannnnnnnnnnnnnnnnnnnnnnnnnnnnnnn",
        "nnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnaannnnnnnnnnnnnnnnnnnnnnnnnnnnnnn",
        "nnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnn",
        "nnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnn",
        "nnnnnnnnnnnnnnnnnnnnnnnnnnnnaaaaaaaaannnnnnnnnnnnnnnnnnnnnnnnnnn",
        "nnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnn",
        "nnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnn",
        "nnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnn",
        "nnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnn",
        "nnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnn",
        "nnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnn",
        "nnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnn",
        "nnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnn",
        "nnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnn",
        "nnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnn",
    };
}
