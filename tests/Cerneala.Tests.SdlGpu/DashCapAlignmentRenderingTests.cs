using Cerneala.Drawing;
using Cerneala.Tests.Drawing.SdlGpu;
using Cerneala.UI.Media;

namespace Cerneala.Tests.SdlGpu;

// Contract (2026-10-20): an Inside/Outside aligned stroke behaves like a centered
// stroke on the contour offset by T/2 toward the interior/exterior, so a dash's
// caps are built around the band's mid-line and never leave the band across the
// contour line.
[Collection(SdlNativeTestCollection.Name)]
public sealed class DashCapAlignmentRenderingTests
{
    private const int Width = 200;
    private const int Height = 120;

    // Rectangle (20,20,160,80), thickness 12, dashes [30,20], scale 1, no MSAA: the
    // second top-edge dash is x = 70..100. The Inside band is y = 20..32 (mid-line 26)
    // and the Outside band is y = 8..20 (mid-line 14). A round cap is a semicircle of
    // radius 6 around the mid-line, so 3.5 px beyond the dash end it spans
    // mid-line +- 4.87.
    [SdlNativeTheory]
    [InlineData(DrawStrokeAlignment.Inside, 0, 19, 23, 29)]
    [InlineData(DrawStrokeAlignment.Outside, 20, 45, 11, 17)]
    public void RoundDashCapsOfAlignedClosedStrokeStayInsideTheBand(
        DrawStrokeAlignment alignment,
        int forbiddenFirstRow,
        int forbiddenLastRow,
        int cappedFirstRow,
        int cappedLastRow)
    {
        using SdlDrawingFixture fixture = new(Width, Height, useMultisampling: false, coordinateScale: 1);
        DrawCommandList commands = new();
        DrawingContext drawing = new(commands);
        DrawStrokeStyle style = new(
            startCap: DrawLineCap.Round,
            endCap: DrawLineCap.Round,
            dashPattern: [30, 20],
            alignment: alignment);
        drawing.DrawRectangle(
            new DrawRect(20, 20, 160, 80),
            new DrawPen(new SolidColorBrush(new Color(255, 255, 255)), 12, style));

        Color[] pixels = fixture.Render(commands, Color.Black);

        for (int y = forbiddenFirstRow; y <= forbiddenLastRow; y++)
        {
            for (int x = 58; x <= 112; x++)
            {
                Assert.True(
                    pixels[(y * Width) + x].R < 64,
                    $"{alignment}: pixel ({x}, {y}) is painted (R = {pixels[(y * Width) + x].R}) on the wrong side of the contour line y = 20.");
            }
        }

        foreach (int x in new[] { 66, 103 })
        {
            for (int y = cappedFirstRow; y <= cappedLastRow; y++)
            {
                Assert.True(
                    pixels[(y * Width) + x].R >= 192,
                    $"{alignment}: cap pixel ({x}, {y}) is not painted (R = {pixels[(y * Width) + x].R}); the cap must cover the band around its mid-line.");
            }
        }
    }
}
