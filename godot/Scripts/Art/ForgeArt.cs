using System.Collections.Generic;
using Godot;

/// <summary>
/// Turns art authored in SPRITE//FORGE into a texture, without going through a file.
///
/// A .forge frame is rows of single characters indexing a shared colour key. That encoding
/// transliterates straight into C#, which is why hand-drawn art can arrive here as source
/// rather than as a PNG — <see cref="TextureKit"/>'s rule that nothing is loaded from disk
/// still holds, a recolour is still a one-line diff, and there is no .import file for Godot
/// to generate before the game will run.
///
/// The authoring source stays in <c>Art/</c> as the .forge it was saved as; the generated
/// data sits beside this file. Re-export rather than editing a generated row by hand.
///
/// '.' is transparent, exactly as it is in the .forge.
/// </summary>
public static class ForgeArt
{
    public const char Transparent = '.';

    /// <summary>
    /// Rows of key characters into an RGBA texture, one texel per character. The image is
    /// as wide as the first row and as tall as <paramref name="rows"/>.
    /// </summary>
    public static ImageTexture FromRows(string[] rows, Dictionary<char, Color> key)
    {
        int h = rows.Length;
        int w = rows[0].Length;
        var img = Image.CreateEmpty(w, h, false, Image.Format.Rgba8);

        for (int y = 0; y < h; y++)
        {
            string row = rows[y];
            // A short row would otherwise throw somewhere inside the loop with nothing saying
            // which row, and these are generated files that get re-exported.
            if (row.Length != w)
                GD.PushError($"ForgeArt: row {y} is {row.Length} characters, expected {w}");

            for (int x = 0; x < w && x < row.Length; x++)
            {
                char c = row[x];
                img.SetPixel(x, y, c == Transparent ? new Color(0, 0, 0, 0) : key[c]);
            }
        }

        return ImageTexture.CreateFromImage(img);
    }
}
