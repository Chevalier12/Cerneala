using Cerneala.Drawing;
using Cerneala.Tests.Drawing.SdlGpu;
using Cerneala.UI.Hosting;

namespace Cerneala.Tests.SdlGpu;

[Collection(SdlNativeTestCollection.Name)]
public sealed class RectanglePixelBoundaryMigrationTests
{
    [SdlNativeTheory]
    [InlineData(1f, 2.5f, 3.5f, 7f, 5f)]
    [InlineData(1.5f, 66f, 2f, 27f, 8f)]
    [InlineData(2f, 1.25f, 1.75f, 4f, 5f)]
    public void FilledRectangleRoundsEachPhysicalEdgeAwayFromZero(
        float scale, float x, float y, float width, float height)
    {
        using SdlDrawingFixture fixture = new(144, 96, coordinateScale: scale);
        DrawCommandList commands = new();
        commands.Add(DrawCommand.FillRectangle(new DrawRect(x, y, width, height), Color.White));
        Color[] pixels = fixture.Render(commands, Color.Black);
        int left = UiCoordinateMapper.LogicalToPhysicalPixel(x, scale);
        int top = UiCoordinateMapper.LogicalToPhysicalPixel(y, scale);
        int right = UiCoordinateMapper.LogicalToPhysicalPixel(x + width, scale);
        int bottom = UiCoordinateMapper.LogicalToPhysicalPixel(y + height, scale);
        for (int pixelY = 0; pixelY < 96; pixelY++)
        {
            for (int pixelX = 0; pixelX < 144; pixelX++)
            {
                Color expected = pixelX >= left && pixelX < right && pixelY >= top && pixelY < bottom
                    ? Color.White : Color.Black;
                Assert.True(pixels[pixelY * 144 + pixelX] == expected,
                    $"Scale {scale}: wrong coverage at ({pixelX},{pixelY}); expected {expected}, actual {pixels[pixelY * 144 + pixelX]}.");
            }
        }
    }
}
