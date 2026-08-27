using Cerneala.Drawing;
using Cerneala.UI.Controls;
using Cerneala.UI.Media;

namespace Cerneala.SdlGpuSmoke;

public sealed class SmokeDrawingSurface : RenderSurface2D
{
    private static readonly SolidColorBrush BackgroundBrush = new(new Color(13, 22, 34));
    private static readonly SolidColorBrush CyanBrush = new(new Color(77, 240, 255));
    private static readonly SolidColorBrush PinkBrush = new(new Color(255, 62, 165));
    private static readonly SolidColorBrush LimeBrush = new(new Color(198, 255, 61));
    private static readonly DrawPen AccentPen = new(CyanBrush, 3);

    public SmokeDrawingSurface()
    {
        ClearColor = new Color(8, 13, 20);
        RedrawMode = RenderSurface2DRedrawMode.Continuous;
    }

    protected override void OnDraw(RenderSurface2DFrame frame)
    {
        frame.FillRectangle(frame.Bounds, BackgroundBrush);
        frame.FillRoundedRectangle(
            new DrawRect(22, 22, MathF.Max(0, frame.Bounds.Width - 44), MathF.Max(0, frame.Bounds.Height - 44)),
            new DrawCornerRadius(18),
            new LinearGradientBrush(
                new DrawPoint(0, 0),
                new DrawPoint(MathF.Max(1, frame.Bounds.Width), MathF.Max(1, frame.Bounds.Height)),
                [new GradientStop(0, new Color(25, 53, 78)), new GradientStop(1, new Color(48, 20, 67))]));
        frame.FillCircle(new DrawPoint(90, 96), 42, PinkBrush);
        frame.FillStar(new DrawPoint(178, 96), 47, 21, 7, LimeBrush, -MathF.PI / 2);
        frame.DrawLine(
            new DrawPoint(42, MathF.Max(36, frame.Bounds.Height - 54)),
            new DrawPoint(MathF.Max(44, frame.Bounds.Width - 42), MathF.Max(36, frame.Bounds.Height - 54)),
            AccentPen);
    }
}
