using Cerneala.Drawing;
using Cerneala.Tests.Drawing.SdlGpu;
using Cerneala.UI.Media;

namespace Cerneala.Tests.SdlGpu;

[Collection(SdlNativeTestCollection.Name)]
public sealed class ThickStrokeHoleTests
{
    private static readonly Color Orange = new(255, 140, 26);

    [SdlNativeTheory]
    [InlineData(0, 1f, true)] [InlineData(0, 1.25f, true)] [InlineData(0, 1f, false)] [InlineData(0, 1.25f, false)]
    [InlineData(1, 1f, true)] [InlineData(1, 1.25f, true)] [InlineData(1, 1f, false)] [InlineData(1, 1.25f, false)]
    public void ThickClosedStrokeHasNoHolesInsideTheBand(int scene, float scale, bool msaa)
    {
        const float Thickness = 36;
        DrawRect rect = new(18, 18, 164, 84);
        int width = (int)MathF.Ceiling(200 * scale), height = (int)MathF.Ceiling(120 * scale);
        using SdlDrawingFixture fixture = new(width, height, useMultisampling: msaa, coordinateScale: scale);
        DrawCommandList commands = new();
        DrawingContext drawing = new(commands);
        if (scene == 0)
            drawing.DrawEllipse(rect, Orange, Thickness);
        else
            drawing.DrawPath(DrawPathFactory.RoundedRectangle(rect, new DrawCornerRadius(30)), new DrawPen(new SolidColorBrush(Orange), Thickness));

        Color[] pixels = fixture.Render(commands, Color.Black);
        int holes = 0;
        for (int py = 0; py < height; py++)
            for (int px = 0; px < width; px++)
            {
                float x = (px + 0.5f) / scale, y = (py + 0.5f) / scale;
                Color pixel = pixels[(py * width) + px];
                if (DeepInsideBand(scene, rect, x, y, Thickness / 2 - 2) && (pixel.R < 200 || pixel.G < 100))
                    holes++;
            }

        Assert.Equal(0, holes);
    }

    // Ellipse: between the ellipses with semi-axes (a ± margin, b ± margin), which lie inside the true band.
    // Rounded rectangle: |signed distance to the rounded box| <= margin.
    private static bool DeepInsideBand(int scene, DrawRect rect, float x, float y, float margin)
    {
        float cx = rect.X + rect.Width / 2, cy = rect.Y + rect.Height / 2;
        if (scene == 0)
        {
            float a = rect.Width / 2, b = rect.Height / 2;
            return N(x - cx, y - cy, a + margin, b + margin) <= 1 && N(x - cx, y - cy, a - margin, b - margin) >= 1;
        }

        const float R = 30;
        float qx = MathF.Abs(x - cx) - (rect.Width / 2 - R), qy = MathF.Abs(y - cy) - (rect.Height / 2 - R);
        float outside = MathF.Sqrt(MathF.Max(qx, 0) * MathF.Max(qx, 0) + MathF.Max(qy, 0) * MathF.Max(qy, 0));
        return MathF.Abs(outside + MathF.Min(MathF.Max(qx, qy), 0) - R) <= margin;
    }

    private static float N(float dx, float dy, float a, float b) => (dx * dx) / (a * a) + (dy * dy) / (b * b);
}
