using System.Numerics;
using Cerneala.Backends.SdlGpu;
using Cerneala.Drawing;
using Cerneala.Drawing.Prism;
using Cerneala.Drawing.Prism.Catalog;
using Cerneala.Tests.Drawing.Prism;
using Cerneala.Tests.SdlGpu;
using Cerneala.UI.Prism.Definitions;
using Cerneala.UI.Prism.Runtime;

namespace Cerneala.Tests.Drawing.SdlGpu;

[Collection(SdlNativeTestCollection.Name)]
public sealed class PrismAdjustmentResourceMigrationTests
{
    [SdlNativeFact]
    public void TypedCurvesResourceRendersAndInvalidatesItsRetainedLut()
    {
        using SdlDrawingFixture fixture = new(16, 16);
        PrismResourceId id = new("curves-cache");
        PrismCurvesResource first = new(red: [new(0, 0), new(1, .5f)]);
        PrismInstance instance = new(new PrismCompositionDefinition("Typed curves",
            [new PrismLayerDefinition(new(1), "Curves", filters: [new(PrismFilterId.Curves)])],
            workingColorProfile: PrismColorProfile.LinearSrgb));
        PrismFilterState state = Assert.Single(instance.GetLayerState(new(1)).Filters);
        state.SetValue(PrismCatalog.GetFilter(PrismFilterId.Curves).Parameters.Single(p => p.Name == "Curves"), id);

        DrawCommandList Commands(PrismCurvesResource resource, long identity, long version)
        {
            PrismDrawScope scope = new(instance, new(4411), new(0, 0, 16, 16), Matrix3x2.Identity, 1, 1,
                PrismDrawResources.Create([], [new PrismDrawCurvesResource(id, resource, version, identity)]));
            return PrismTestData.Commands(DrawCommand.BeginPrism(scope),
                DrawCommand.FillRectangle(new(0, 0, 16, 16), Color.White), DrawCommand.EndPrism());
        }

        DrawCommandList initial = Commands(first, 1, 1);
        Color initialPixel = fixture.RenderCenterPixel(initial);
        Assert.Equal(0, fixture.Backend.PrismDiagnostics.Count);
        Assert.InRange(initialPixel.R, 187, 189);
        Assert.Equal(255, initialPixel.G);
        Assert.Equal(255, initialPixel.B);
        Assert.Equal(255, initialPixel.A);
        Assert.Equal(initialPixel, fixture.RenderCenterPixel(initial));
        Assert.Equal(0, fixture.Backend.PrismDiagnostics.Counters.CaptureCount);

        Color changed = fixture.RenderCenterPixel(Commands(new PrismCurvesResource(), 2, 2));
        Assert.Equal(0, fixture.Backend.PrismDiagnostics.Count);
        Assert.Equal(Color.White, changed);
    }

    [SdlNativeTheory]
    [InlineData(2)]
    [InlineData(3)]
    public void HaldKernelUsesResourceDimensionsAndCubeSize(int level)
    {
        int cube = level * level, side = cube * level;
        Vector4[] lut = new Vector4[side * side];
        for (int b = 0; b < cube; b++)
        for (int g = 0; g < cube; g++)
        for (int r = 0; r < cube; r++)
            lut[r + cube * (g + cube * b)] = new(
                b / (float)(cube - 1), g / (float)(cube - 1), r / (float)(cube - 1), 1);

        using SdlPrismKernelFixture fixture = new();
        Vector4 source = new(.1f, .2f, .35f, .5f);
        Vector4 pixel = fixture.RunKernel(3, [source], 1, 1, uniforms =>
        {
            uniforms[23] = new(10, (int)PrismColorProfile.LinearSrgb, 0, cube);
            uniforms[34] = new(side, side, 3, 0);
        }, new(side, side, lut))[0];
        Vector4 expected = new(source.Z, source.Y, source.X, source.W);
        Assert.InRange(Vector4.Distance(pixel, expected), 0, .002f);
    }

    [SdlNativeTheory]
    [InlineData(1, 1)]
    [InlineData(8, 9)]
    [InlineData(9, 9)]
    public void InvalidHaldDimensionsReportFallbackAndPreserveSource(int width, int height)
    {
        using SdlDrawingFixture fixture = new(16, 16);
        byte[] pixels = new byte[width * height * 4];
        Array.Fill(pixels, (byte)255);
        using SdlGpuImage image = new(width, height, pixels);
        PrismResourceId id = new("InvalidHald");
        PrismLayerDefinition layer = new(new(1), "Lookup", filters: [new(PrismFilterId.ColorLookup)]);
        PrismInstance instance = new(PrismTestData.Composition("Invalid Hald", layer));
        PrismFilterState state = Assert.Single(instance.GetLayerState(new(1)).Filters);
        state.SetValue(PrismCatalog.GetFilter(PrismFilterId.ColorLookup).Parameters.Single(p => p.Name == "Lookup"), id);
        PrismDrawScope scope = new(instance, new(4401), new(0, 0, 16, 16), Matrix3x2.Identity, 1, 1,
            PrismDrawResources.Create([new PrismDrawImageResource(id, image)]));
        Color source = new(32, 128, 224);
        Color pixel = fixture.RenderCenterPixel(PrismTestData.Commands(DrawCommand.BeginPrism(scope),
            DrawCommand.FillRectangle(new(0, 0, 16, 16), source), DrawCommand.EndPrism()));
        Assert.Equal(1, fixture.Backend.PrismDiagnostics.Count);
        Assert.Contains("UnsupportedCapability", fixture.Backend.PrismDiagnostics.DumpExecutedGraph());
        Assert.InRange(Math.Abs(pixel.R - source.R), 0, 1);
        Assert.InRange(Math.Abs(pixel.G - source.G), 0, 1);
        Assert.InRange(Math.Abs(pixel.B - source.B), 0, 1);
        Assert.Equal(source.A, pixel.A);
    }
}
