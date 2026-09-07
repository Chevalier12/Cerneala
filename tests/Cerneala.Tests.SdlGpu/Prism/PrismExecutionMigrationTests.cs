using System.Numerics;
using Cerneala.Backends.SdlGpu;
using Cerneala.Drawing;
using Cerneala.Drawing.Prism;
using Cerneala.Drawing.Prism.Catalog;
using Cerneala.Drawing.Prism.Graph;
using Cerneala.Platforms.Sdl3;
using Cerneala.Tests.Drawing.Prism;
using Cerneala.Tests.SdlGpu;
using Cerneala.UI.Prism.Definitions;
using Cerneala.UI.Prism.Runtime;

namespace Cerneala.Tests.Drawing.SdlGpu;

[Collection(SdlNativeTestCollection.Name)]
public sealed class PrismExecutionMigrationTests
{
    [SdlNativeFact]
    public void StrokeGpuProducesSolidEuclideanOutsideBand()
    {
        const int size = 17, center = size / 2;
        using SdlPrismKernelFixture fixture = new();
        SdlGpuWindowGraphicsSession session = fixture.Session;
        SdlGpuPrismDeviceResources resources = session.DrawingResources.PrismResources;
        using SdlGpuPrismSurfaceLease a = resources.RentSurface(session.WindowIdentity, size, size, SdlGpuTextureFormat.R32G32B32A32Float, false);
        using SdlGpuPrismSurfaceLease b = resources.RentSurface(session.WindowIdentity, size, size, SdlGpuTextureFormat.R32G32B32A32Float, false);
        using SdlGpuPrismSurfaceLease output = resources.RentSurface(session.WindowIdentity, size, size, SdlGpuTextureFormat.R16G16B16A16Float, false);
        Vector4[] sourcePixels = new Vector4[size * size];
        sourcePixels[center * size + center] = Vector4.One;
        object sourceKey = new();
        session.BeginFrame(Color.Transparent);
        try
        {
            nint source = session.DrawingResources.GetOrCreateHalfVector4Texture(session, sourceKey, size, size, sourcePixels).Handle;
            SdlGpuPrismUniforms seed = SdlPrismKernelFixture.CreateUniforms(85, size, size);
            fixture.Draw(a.Target, source, source, source, seed, DrawSamplingMode.Point);
            SdlGpuRenderTarget read = a.Target, write = b.Target;
            foreach (int jump in new[] { 16, 8, 4, 2, 1, 1 })
            {
                SdlGpuPrismUniforms flood = SdlPrismKernelFixture.CreateUniforms(86, size, size);
                flood[9] = new(jump / (float)size, jump / (float)size, 0, 0);
                fixture.Draw(write, read.SampleTexture, source, source, flood, DrawSamplingMode.Point);
                (read, write) = (write, read);
            }
            SdlGpuPrismUniforms style = SdlPrismKernelFixture.CreateUniforms(82, size, size);
            style[10] = new(1, 0, 0, 1); style[12] = new(0, 0, 3, 0);
            style[14] = new(1, 0, 0, 0); style[16] = new(9, 0, 0, 0); style[17] = Vector4.Zero;
            fixture.Draw(output.Target, source, source, source, style, inputs: new Dictionary<uint, nint>
                { [4] = source, [5] = read.SampleTexture, [11] = read.SampleTexture });
        }
        finally
        {
            session.CompleteFrame(false);
            session.DrawingResources.InvalidateTexture(sourceKey);
        }
        Vector4[] pixels = fixture.ReadPixels(output.Target);
        Assert.InRange(Alpha(2, 0), .997f, 1);
        Assert.InRange(Alpha(2, 2), .997f, 1);
        Assert.InRange(Alpha(4, 1), 0, .003f);
        float Alpha(int x, int y) => pixels[(center + y) * size + center + x].W;
    }

    [SdlNativeFact]
    public void StrokeExecutorPreparesAndConsumesSignedDistanceField()
    {
        const int size = 17, center = size / 2;
        using SdlDrawingFixture fixture = new(size, size);
        PrismLayerDefinition layer = new(new(1), "Stroke", styles: [new(PrismStyleId.Stroke)]);
        PrismDrawScope scope = PrismTestData.Scope(PrismTestData.Composition("Stroke", layer), bounds: new(0, 0, size, size));
        Color[] pixels = fixture.Render(PrismTestData.Commands(DrawCommand.BeginPrism(scope),
            DrawCommand.FillRectangle(new(center, center, 1, 1), Color.White), DrawCommand.EndPrism()), Color.Transparent);
        Assert.InRange(Alpha(2, 0), 254, 255);
        Assert.InRange(Alpha(2, 2), 254, 255);
        Assert.InRange(Alpha(4, 1), 0, 1);
        Assert.Equal(0, fixture.Backend.PrismDiagnostics.Count);
        byte Alpha(int x, int y) => pixels[(center + y) * size + center + x].A;
    }

    [SdlNativeFact]
    public void TransformExecutionMinifiesCheckerboardWithoutAliasing()
    {
        const int size = 16;
        using SdlDrawingFixture fixture = new(size, size);
        PrismLayerDefinition layer = new(new(1), "Transform", filters: [new(PrismFilterId.Transform)]);
        PrismInstance instance = new(PrismTestData.Composition("Transform", layer));
        instance.GetLayerState(new(1)).Filters.Single().SetValue(
            PrismCatalog.GetFilter(PrismFilterId.Transform).Parameters.Single(p => p.Name == "Scale"), new Vector4(.25f, .25f, 0, 0));
        PrismDrawScope scope = new(instance, new(1), new(0, 0, size, size), Matrix3x2.Identity, 1, 1, PrismDrawResources.Empty);
        DrawCommandList commands = new();
        commands.Add(DrawCommand.BeginPrism(scope));
        for (int y = 0; y < size; y++)
        for (int x = 0; x < size; x++)
            commands.Add(DrawCommand.FillRectangle(new(x, y, 1, 1), (x + y) % 2 == 0 ? Color.White : Color.Black));
        commands.Add(DrawCommand.EndPrism());
        Color center = fixture.RenderCenterPixel(commands);
        Assert.Equal(0, fixture.Backend.PrismDiagnostics.Count);
        Assert.InRange(center.R, 70, 200);
        Assert.Equal(center.R, center.G);
        Assert.Equal(center.R, center.B);
    }

    [SdlNativeFact]
    public void StaticFinalHitSkipsCaptureAndCoveredPassesWithoutChangingPixels()
    {
        using SdlDrawingFixture fixture = new(16, 16);
        PrismDrawScope scope = PrismTestData.Scope(PrismTestData.Composition("Static alpha", PrismTestData.Layer(1, "Layer")), bounds: new(0, 0, 16, 16));
        DrawCommandList commands = PrismTestData.Commands(DrawCommand.BeginPrism(scope),
            DrawCommand.FillRectangle(new(0, 0, 16, 16), new(220, 40, 70, 160)), DrawCommand.EndPrism());
        Color[] fresh = fixture.Render(commands, Color.Transparent);
        Assert.Equal(1, fixture.Backend.PrismDiagnostics.Counters.CaptureCount);
        Color[] cached = fixture.Render(commands, Color.Transparent);
        Assert.Equal(fresh, cached);
        Assert.Equal(0, fixture.Backend.PrismDiagnostics.Count);
        Assert.Equal(0, fixture.Backend.PrismDiagnostics.Counters.CaptureCount);
        Assert.True(fixture.Backend.PrismDiagnostics.Counters.PassCount == 1,
            fixture.Backend.PrismDiagnostics.DumpExecutedGraph());
    }

    [SdlNativeFact]
    public void LiveLayerOpacityMutationChangesRetainedPixelsAndMatchesFreshOutput()
    {
        using SdlDrawingFixture fixture = new(16, 16);
        PrismLayerDefinition layer = new(new(1), "SignalPulse", styles: [new(PrismStyleId.OuterGlow)],
            opacity: .08f, blendMode: PrismBlendMode.Screen);
        PrismInstance instance = new(PrismTestData.Composition("Live opacity mutation", layer));
        PrismDrawScope scope = new(instance, new(91001), new(0, 0, 16, 16), Matrix3x2.Identity, 1, 1);
        DrawCommandList commands = PrismTestData.Commands(DrawCommand.BeginPrism(scope),
            DrawCommand.FillRectangle(new(4, 4, 16, 12), new(38, 157, 255, 255)), DrawCommand.EndPrism());
        Color[] baseline = fixture.Render(commands, Color.Transparent);
        instance.GetLayerState(new(1)).Opacity = 1;
        Color[] changed = fixture.Render(commands, Color.Transparent);
        fixture.Session.DrawingResources.PrismResources.Invalidate(PrismCacheInvalidation.All);
        Color[] fresh = fixture.Render(commands, Color.Transparent);
        Assert.False(baseline.SequenceEqual(changed));
        Assert.Equal(fresh, changed);
        Assert.Equal(0, fixture.Backend.PrismDiagnostics.Count);
    }
}
