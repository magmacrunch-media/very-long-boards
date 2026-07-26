using Godot;

public partial class TitlePainter : Control
{
    private float _time = 0f;

    public override void _Process(double delta)
    {
        _time += (float)delta;
        QueueRedraw();
    }

    public override void _Draw()
    {
        float w = Size.X;
        float h = Size.Y;

        // ── Back wall ──
        DrawRect(new Rect2(0, 0, w, h * 0.65f), new Color(0.23f, 0.23f, 0.23f));

        // Cinder block grooves — horizontal
        var grooveColor = new Color(0.18f, 0.18f, 0.18f);
        for (int row = 0; row < 8; row++)
        {
            float y = row * (h * 0.65f / 8f);
            DrawLine(new Vector2(0, y), new Vector2(w, y), grooveColor, 1f);
        }
        // Cinder block grooves — vertical (staggered)
        for (int col = 0; col < 16; col++)
        {
            float x = col * (w / 16f);
            int offset = col % 2;
            for (int row = 0; row < 8; row++)
            {
                float yStart = row * (h * 0.65f / 8f);
                float yEnd = yStart + (h * 0.65f / 8f);
                float xOff = (row % 2 == 0) ? 0f : (w / 32f);
                DrawLine(new Vector2(x + xOff, yStart), new Vector2(x + xOff, yEnd), grooveColor, 1f);
            }
        }

        // ── Floor ──
        float floorY = h * 0.65f;
        DrawRect(new Rect2(0, floorY, w, h - floorY), new Color(0.33f, 0.33f, 0.33f));

        // Floor perspective lines — horizontal
        var floorLineColor = new Color(0.28f, 0.28f, 0.28f);
        for (int i = 0; i < 8; i++)
        {
            float y = floorY + i * ((h - floorY) / 8f);
            DrawLine(new Vector2(0, y), new Vector2(w, y), floorLineColor, 1f);
        }
        // Floor perspective lines — vertical (converging)
        for (int i = 0; i < 14; i++)
        {
            float x = i * (w / 14f);
            float xEnd = x + (i - 7f) * 4f;
            DrawLine(new Vector2(x, floorY), new Vector2(xEnd, h), floorLineColor, 1f);
        }

        // ── Oil stain ──
        DrawEllipse(new Vector2(w * 0.55f, floorY + h * 0.12f), 25f, 10f, new Color(0.15f, 0.15f, 0.15f, 0.4f));

        // ── Workbench (left side) ──
        var woodColor = new Color(0.43f, 0.3f, 0.16f);
        var darkWoodColor = new Color(0.35f, 0.24f, 0.12f);
        float wbX = w * 0.03f;
        float wbW = w * 0.38f;
        float wbY = floorY - h * 0.22f;
        float wbH = h * 0.04f;
        float wbLegH = floorY - wbY - wbH;

        // Table top
        DrawRect(new Rect2(wbX, wbY, wbW, wbH), woodColor);
        // Legs
        float legW = wbW * 0.04f;
        DrawRect(new Rect2(wbX + wbW * 0.04f, wbY + wbH, legW, wbLegH), darkWoodColor);
        DrawRect(new Rect2(wbX + wbW * 0.92f, wbY + wbH, legW, wbLegH), darkWoodColor);
        // Shelf
        DrawRect(new Rect2(wbX + wbW * 0.04f, wbY + wbLegH * 0.55f, wbW * 0.92f, 4), darkWoodColor);

        // Items on workbench (proportional to table width)
        float itemY = wbY - h * 0.05f;
        float itemH = h * 0.045f;
        DrawRect(new Rect2(wbX + wbW * 0.05f, itemY, wbW * 0.08f, itemH), new Color(0.55f, 0.55f, 0.55f)); // box
        DrawRect(new Rect2(wbX + wbW * 0.18f, itemY + h * 0.01f, wbW * 0.05f, itemH * 0.7f), new Color(0.4f, 0.4f, 0.4f)); // can
        DrawRect(new Rect2(wbX + wbW * 0.30f, itemY + h * 0.008f, wbW * 0.16f, itemH * 0.85f), new Color(0.6f, 0.6f, 0.6f)); // toolbox
        DrawRect(new Rect2(wbX + wbW * 0.34f, itemY + h * 0.008f, wbW * 0.07f, itemH * 0.3f), new Color(1f, 0.42f, 0.21f)); // handle

        // ── Pegboard + tools (on back wall, center-left) ──
        float pbX = w * 0.33f;
        float pbY = h * 0.12f;
        float pbW = w * 0.18f;
        float pbH = h * 0.22f;
        DrawRect(new Rect2(pbX, pbY, pbW, pbH), new Color(0.28f, 0.28f, 0.28f));
        DrawRect(new Rect2(pbX, pbY, pbW, pbH), new Color(0.25f, 0.25f, 0.25f), false, 2f); // border

        // Hammer
        DrawRect(new Rect2(pbX + 12, pbY + 10, 3, 25), new Color(0.43f, 0.3f, 0.16f)); // handle
        DrawRect(new Rect2(pbX + 8, pbY + 6, 12, 5), new Color(0.55f, 0.55f, 0.55f)); // head
        // Wrench
        DrawRect(new Rect2(pbX + 35, pbY + 12, 3, 22), new Color(0.6f, 0.6f, 0.6f));
        DrawCircle(new Vector2(pbX + 36, pbY + 12), 4f, new Color(0.6f, 0.6f, 0.6f));
        // Screwdriver
        DrawRect(new Rect2(pbX + 55, pbY + 10, 3, 18), new Color(1f, 0.18f, 0.61f));
        DrawRect(new Rect2(pbX + 55, pbY + 5, 3, 7), new Color(0.8f, 0.8f, 0.8f));

        // ── Shelf (right side of back wall) ──
        float shX = w * 0.62f;
        float shY = h * 0.1f;
        float shW = w * 0.22f;
        DrawRect(new Rect2(shX, shY, shW, 5), darkWoodColor);
        DrawRect(new Rect2(shX, shY, 4, 25), darkWoodColor); // left support
        DrawRect(new Rect2(shX + shW - 4, shY, 4, 25), darkWoodColor); // right support

        // Items on shelf
        DrawRect(new Rect2(shX + 8, shY - 12, 10, 12), new Color(0.29f, 0.56f, 0.85f)); // blue bottle
        DrawRect(new Rect2(shX + 25, shY - 10, 8, 10), new Color(1f, 0.27f, 0.27f)); // red can
        DrawRect(new Rect2(shX + 40, shY - 11, 10, 11), new Color(0.27f, 0.8f, 0.27f)); // green bottle
        DrawRect(new Rect2(shX + 58, shY - 8, 14, 8), new Color(1f, 0.67f, 0f)); // orange box

        // ── Window (far right) ──
        float winX = w * 0.82f;
        float winY = h * 0.06f;
        float winW = w * 0.14f;
        float winH = h * 0.24f;
        DrawRect(new Rect2(winX, winY, winW, winH), new Color(0.48f, 0.68f, 0.8f, 0.85f));
        DrawRect(new Rect2(winX, winY, winW, winH), new Color(0.33f, 0.33f, 0.33f), false, 3f); // frame
        // Cross bars
        DrawLine(new Vector2(winX + winW / 2f, winY), new Vector2(winX + winW / 2f, winY + winH), new Color(0.33f, 0.33f, 0.33f), 3f);
        DrawLine(new Vector2(winX, winY + winH / 2f), new Vector2(winX + winW, winY + winH / 2f), new Color(0.33f, 0.33f, 0.33f), 3f);
        // Light glow on floor
        DrawColoredPolygon(new[] {
            new Vector2(winX, winY + winH),
            new Vector2(winX - 20, h),
            new Vector2(winX + winW + 20, h),
            new Vector2(winX + winW, winY + winH)
        }, new Color(0.48f, 0.68f, 0.8f, 0.06f));

        // ── Neon VLB sign ──
        float vlbX = w * 0.38f;
        float vlbY = h * 0.08f;
        // Glow layers (drawn back to front)
        DrawString(ThemeDB.FallbackFont, new Vector2(vlbX - 1, vlbY + 1), "VLB",
            HorizontalAlignment.Left, -1, 28, new Color(1f, 0.18f, 0.61f, 0.15f));
        DrawString(ThemeDB.FallbackFont, new Vector2(vlbX + 1, vlbY - 1), "VLB",
            HorizontalAlignment.Left, -1, 28, new Color(1f, 0.18f, 0.61f, 0.2f));
        DrawString(ThemeDB.FallbackFont, new Vector2(vlbX, vlbY), "VLB",
            HorizontalAlignment.Left, -1, 28, new Color(1f, 0.18f, 0.61f, 0.6f));
        DrawString(ThemeDB.FallbackFont, new Vector2(vlbX, vlbY), "VLB",
            HorizontalAlignment.Left, -1, 28, new Color(1f, 0.43f, 0.71f, 1f));

        // Tagline
        DrawString(ThemeDB.FallbackFont, new Vector2(vlbX + 2, vlbY + 20), "DOWNHILL SKATEBOARDS",
            HorizontalAlignment.Left, -1, 8, new Color(1f, 0.18f, 0.61f, 0.4f));

        // ── Leaning skateboard (right side, against wall) ──
        float sbX = w * 0.92f;
        float sbY = floorY;
        DrawRect(new Rect2(sbX - 3, sbY - 55, 6, 55), new Color(0.55f, 0.27f, 0.1f));
        // Grip tape
        DrawRect(new Rect2(sbX - 2, sbY - 52, 4, 48), new Color(0.16f, 0.16f, 0.16f));
        // Wheels
        DrawRect(new Rect2(sbX - 5, sbY - 4, 4, 3), new Color(0.12f, 0.12f, 0.12f));
        DrawRect(new Rect2(sbX + 1, sbY - 4, 4, 3), new Color(0.12f, 0.12f, 0.12f));
        DrawRect(new Rect2(sbX - 5, sbY - 48, 4, 3), new Color(0.12f, 0.12f, 0.12f));
        DrawRect(new Rect2(sbX + 1, sbY - 48, 4, 3), new Color(0.12f, 0.12f, 0.12f));

        // ── Ceiling ──
        DrawRect(new Rect2(0, 0, w, 3), new Color(0.2f, 0.2f, 0.2f));

        // ── Fluorescent light ──
        float lightX = w * 0.3f;
        DrawRect(new Rect2(lightX, 0, 60, 4), new Color(0.85f, 0.85f, 0.85f));
        // Light cone
        DrawColoredPolygon(new[] {
            new Vector2(lightX, 4),
            new Vector2(lightX - 15, floorY),
            new Vector2(lightX + 75, floorY),
            new Vector2(lightX + 60, 4)
        }, new Color(1f, 0.98f, 0.94f, 0.08f));

        // ── Title text with opaque backing ──
        // Backing behind title
        DrawRect(new Rect2(w * 0.15f, h * 0.42f, w * 0.7f, h * 0.22f), new Color(0.05f, 0.03f, 0.08f, 0.92f));

        // "VERY LONG BOARDS"
        DrawString(ThemeDB.FallbackFont, new Vector2(w * 0.5f - 75, h * 0.5f - 5), "VERY LONG BOARDS",
            HorizontalAlignment.Left, -1, 12, new Color(1f, 0.18f, 0.61f));

        // "A Carl Spatski Game"
        DrawString(ThemeDB.FallbackFont, new Vector2(w * 0.5f - 48, h * 0.5f + 12), "A Carl Spatski Game",
            HorizontalAlignment.Left, -1, 6, new Color(0.6f, 0.6f, 0.7f));

        // Backing behind prompt
        DrawRect(new Rect2(w * 0.25f, h * 0.68f, w * 0.5f, h * 0.08f), new Color(0.05f, 0.03f, 0.08f, 0.92f));

        // "PRESS ↑ TO START" — blink effect
        float blink = Mathf.Sin(_time * 3f) * 0.3f + 0.7f;
        DrawString(ThemeDB.FallbackFont, new Vector2(w * 0.5f - 55, h * 0.73f), "PRESS \u2191 TO START",
            HorizontalAlignment.Left, -1, 7, new Color(1f, 0.88f, 0.23f, blink));

        // Backing behind controls
        DrawRect(new Rect2(w * 0.08f, h * 0.88f, w * 0.84f, h * 0.08f), new Color(0.05f, 0.03f, 0.08f, 0.92f));

        // Controls
        DrawString(ThemeDB.FallbackFont, new Vector2(w * 0.5f - 80, h * 0.93f), "\u2190\u2192 STEER   SPACE BRAKE   \u2191/ENTER KICK",
            HorizontalAlignment.Left, -1, 5, new Color(0.4f, 0.4f, 0.5f));
    }

    private void DrawEllipse(Vector2 center, float rx, float ry, Color color)
    {
        var points = new Vector2[16];
        for (int i = 0; i < 16; i++)
        {
            float angle = i * Mathf.Pi * 2f / 16f;
            points[i] = new Vector2(
                center.X + Mathf.Cos(angle) * rx,
                center.Y + Mathf.Sin(angle) * ry);
        }
        DrawColoredPolygon(points, color);
    }
}
