using Cerneala.Drawing;
using Cerneala.Drawing.Prism;
using Cerneala.Drawing.Prism.Catalog;
using Cerneala.Tests.Drawing.Prism;
using Cerneala.Tests.SdlGpu;
using Cerneala.UI.Prism.Definitions;

namespace Cerneala.Tests.Drawing.SdlGpu;

[Collection(SdlNativeTestCollection.Name)]
public sealed class PrismTransformExecutionTests
{
    [SdlNativeFact]
    public void TransformRotationIsRigidInPixelsOnTheGpuForNonSquareLayers()
    {
        // A 60x10 px layer centered at (40, 30), turned by 30 degrees about its center. A rigid turn
        // covers pixel (16, 14): undoing the turn maps its center to (-28.1, -1.7) from the layer
        // center, inside the 60x10 layer. The execution extent is the union of the layer and the
        // turned bounds, 60 x 38.7 px; a turn in that extent's normalized uv maps the same pixel
        // 2.4 px beyond the layer's left end, so the pixel stays transparent.
        using SdlDrawingFixture fixture = new(80, 80);
        PrismLayerDefinition layer = new(new(1), "Transform", filters: [new(PrismFilterId.Transform)]);
        PrismDrawScope scope = PrismTestData.Scope(
            PrismTestData.Composition("Rigid transform", layer),
            bounds: new DrawRect(10, 25, 60, 10));
        scope.Instance.GetLayerState(new(1)).Filters.Single().SetValue(
            PrismCatalog.GetFilter(PrismFilterId.Transform).Parameters.Single(parameter => parameter.Name == "Rotation"),
            30f);
        DrawCommandList commands = PrismTestData.Commands(
            DrawCommand.BeginPrism(scope),
            DrawCommand.FillRectangle(new DrawRect(10, 25, 60, 10), Color.White),
            DrawCommand.EndPrism());

        Color[] pixels = fixture.Render(commands, Color.Transparent);

        Assert.Equal(0, fixture.Backend.PrismDiagnostics.Count);
        Assert.True(pixels[(14 * 80) + 16].A > 200, $"pixel (16, 14) alpha={pixels[(14 * 80) + 16].A}, expected opaque");
        Assert.True(pixels[(45 * 80) + 63].A > 200, $"pixel (63, 45) alpha={pixels[(45 * 80) + 63].A}, expected opaque");
        Assert.True(pixels[(14 * 80) + 64].A < 20, $"pixel (64, 14) alpha={pixels[(14 * 80) + 64].A}, expected transparent");
    }
}
