using System.Numerics;
using Cerneala.Backends.SdlGpu;
using Cerneala.Drawing;
using Cerneala.Drawing.Prism;
using Cerneala.Drawing.Prism.Catalog;
using Cerneala.Drawing.Prism.Graph;
using Cerneala.Tests.Drawing.Prism;
using Cerneala.Tests.SdlGpu;
using Cerneala.UI.Prism.Definitions;
using Cerneala.UI.Prism.Runtime;

namespace Cerneala.Tests.Drawing.SdlGpu;

[Collection(SdlNativeTestCollection.Name)]
public sealed class PrismRetainedDependencyMigrationTests
{
    [Theory]
    [InlineData("profile")]
    [InlineData("format")]
    [InlineData("capabilities")]
    [InlineData("shader")]
    public void RasterContextChangesCannotHitThePreviousRetainedKey(string mutation)
    {
        DrawCommandList commands = Commands(92001, new(220, 40, 70));
        PrismFrameAnalysis analysis = new PrismFrameAnalyzer().Analyze(commands);
        PrismGraphExecutionPlan plan = new PrismGraphOptimizer().Optimize(new PrismGraphBuilder().Build(analysis));
        PrismGraphNodeId output = plan.ExecutionOrder[plan.RootOutputExecutionIndices[0]];
        PrismRetainedRasterContext baseline = new(16, 16, PrismColorProfile.Srgb,
            BackdropPixelFormat.Rgba16Float, PrismSampling.Linear, analysis.RequiredCapabilities, 57);
        PrismRetainedRasterContext changed = new(16, 16,
            mutation == "profile" ? PrismColorProfile.LinearSrgb : PrismColorProfile.Srgb,
            mutation == "format" ? BackdropPixelFormat.Rgba8Unorm : BackdropPixelFormat.Rgba16Float,
            PrismSampling.Linear,
            mutation == "capabilities" ? analysis.RequiredCapabilities | PrismGraphCapabilities.BackdropInput : analysis.RequiredCapabilities,
            mutation == "shader" ? 58 : 57);
        Assert.True(PrismRetainedCacheKey.TryCreate(plan, output, baseline, out PrismRetainedCacheKey first));
        Assert.True(PrismRetainedCacheKey.TryCreate(plan, output, changed, out PrismRetainedCacheKey second));
        Assert.NotEqual(first, second);
        Dictionary<PrismRetainedCacheKey, int> retained = new() { [first] = 1 };
        Assert.False(retained.ContainsKey(second));
    }

    [SdlNativeFact]
    public void DifferentControlOwnersNeverShareCapturedPixels()
    {
        using SdlDrawingFixture fixture = new(16, 16);
        DrawCommandList first = Commands(92001, new(220, 40, 70));
        DrawCommandList second = Commands(92002, new(30, 210, 95));
        Color[] firstPixels = fixture.Render(first, Color.Transparent);
        Color[] secondPixels = fixture.Render(second, Color.Transparent);
        Assert.False(firstPixels.SequenceEqual(secondPixels));
        Assert.Equal(1, fixture.Backend.PrismDiagnostics.Counters.CaptureCount);
        Assert.Equal(fixture.Backend.PrismDiagnostics.Counters.PlannedPassCount,
            fixture.Backend.PrismDiagnostics.Counters.PassCount);
        Assert.Equal(0, fixture.Backend.PrismDiagnostics.Count);
        Assert.Equal(firstPixels, fixture.Render(first, Color.Transparent));
        Assert.Equal(0, fixture.Backend.PrismDiagnostics.Counters.CaptureCount);
        Assert.Equal(secondPixels, fixture.Render(second, Color.Transparent));
        Assert.Equal(0, fixture.Backend.PrismDiagnostics.Counters.CaptureCount);
    }

    [SdlNativeTheory]
    [InlineData(false)]
    [InlineData(true)]
    public void BackdropAndLowerUiVersionsInvalidateOnlyTheirDependentWork(bool lowerUi)
    {
        using SdlDrawingFixture fixture = new(16, 16);
        PrismBackdropSourceToken sourceToken = PrismBackdropSourceToken.CreateUnique();
        PrismCompositionDefinition definition = PrismTestData.Composition("Backdrop mutation",
            PrismTestData.Layer(1, "Foreground"), PrismTestData.BackdropLayer(2, "Backdrop"));
        DrawCommandList Scene(long version)
        {
            PrismDrawScope scope = new(new PrismInstance(definition), new(91001), new(0, 0, 16, 16),
                Matrix3x2.Identity, 1, 1, PrismDrawResources.Empty, version);
            return PrismTestData.Commands(DrawCommand.BeginPrism(scope),
                DrawCommand.FillRectangle(new(0, 0, 16, 16), new(230, 70, 86, 208)), DrawCommand.EndPrism());
        }
        byte[] Render(DrawCommandList commands, long contentVersion)
        {
            PrismFrameAnalysis analysis = new PrismFrameAnalyzer().Analyze(commands);
            fixture.Session.BeginFrame(new(38, 112, 210));
            try
            {
                using IBackdropFrameLease native = fixture.Session.AcquireFrame(new(16, 16, 1, analysis.BackdropRequirement));
                using StableBackdropLease lease = new((ISdlGpuBackdropFrameLease)native, contentVersion);
                DrawingFrameContext frame = new(analysis, lease, sourceToken);
                fixture.Session.DrawingBackend.Render(commands, in frame);
            }
            finally { fixture.Session.CompleteFrame(present: false); }
            return fixture.Session.CapturePresentedFrame().Pixels;
        }

        DrawCommandList baseline = Scene(10);
        byte[] first = Render(baseline, 20);
        Assert.Equal(first, Render(baseline, 20));
        Assert.Equal(1, fixture.Backend.PrismDiagnostics.Counters.PassCount);
        Assert.Equal(0, fixture.Backend.PrismDiagnostics.Counters.CaptureCount);
        DrawCommandList changed = Scene(lowerUi ? 11 : 10);
        byte[] actual = Render(changed, lowerUi ? 20 : 21);
        Assert.True(fixture.Backend.PrismDiagnostics.Counters.PassCount > 1);
        Assert.Equal(0, fixture.Backend.PrismDiagnostics.Counters.CaptureCount);
        fixture.Session.DrawingResources.PrismResources.Invalidate(PrismCacheInvalidation.All);
        byte[] fresh = Render(changed, lowerUi ? 20 : 21);
        Assert.Equal(fresh.Length, actual.Length);
        for (int index = 0; index < actual.Length; index++)
            Assert.InRange(Math.Abs(actual[index] - fresh[index]), 0, 1);
        Assert.Equal(0, fixture.Backend.PrismDiagnostics.Count);
    }

    [SdlNativeFact]
    public void CaptureExceptionReleasesLeasesAndCannotReuseThePreviousOwnerPixels()
    {
        using SdlDrawingFixture fixture = new(16, 16);
        fixture.Render(Commands(92001, new(220, 35, 60)), Color.Transparent);
        SdlGpuPrismDeviceResources resources = fixture.Session.DrawingResources.PrismResources;
        Assert.True(resources.RetainedCount > 0);
        FaultingBrush brush = new() { Fail = false };
        PrismDrawScope scope = PrismTestData.Scope(PrismTestData.Composition("Owner isolation", PrismTestData.Layer(1, "Layer")),
            ownerToken: 92001, bounds: new(0, 0, 16, 16), visualContentVersion: 2);
        DrawCommandList changed = PrismTestData.Commands(DrawCommand.BeginPrism(scope),
            DrawCommand.FillRectangle(new(0, 0, 16, 16), brush), DrawCommand.EndPrism());
        PrismFrameAnalysis analysis = new PrismFrameAnalyzer().Analyze(changed);
        DrawingFrameContext frame = new(analysis);
        brush.Fail = true;
        fixture.Session.BeginFrame(Color.Transparent);
        try
        {
            Assert.Throws<InjectedCaptureException>(() => fixture.Backend.Render(changed, in frame));
        }
        finally { fixture.Session.CompleteFrame(present: false); }
        Assert.Equal(0, resources.RetainedCount);
        Assert.Equal(resources.TotalBytes, resources.FreeBytes);
        brush.Fail = false;
        Color[] actual = fixture.Render(changed, Color.Transparent);
        resources.Invalidate(PrismCacheInvalidation.All);
        Assert.Equal(actual, fixture.Render(changed, Color.Transparent));
        Assert.Equal(0, fixture.Backend.PrismDiagnostics.Count);
    }

    private static DrawCommandList Commands(long owner, Color color)
    {
        PrismDrawScope scope = PrismTestData.Scope(PrismTestData.Composition("Owner isolation", PrismTestData.Layer(1, "Layer")),
            ownerToken: owner, bounds: new(0, 0, 16, 16));
        return PrismTestData.Commands(DrawCommand.BeginPrism(scope),
            DrawCommand.FillRectangle(new(0, 0, 16, 16), color), DrawCommand.EndPrism());
    }

    private sealed class StableBackdropLease(ISdlGpuBackdropFrameLease inner, long version) : ISdlGpuBackdropFrameLease
    {
        public nint Texture => inner.Texture;
        public BackdropFrameMetadata Metadata
        {
            get
            {
                BackdropFrameMetadata value = inner.Metadata;
                return new(value.PixelWidth, value.PixelHeight, value.PixelScale, value.ColorProfile,
                    value.PixelFormat, value.AlphaMode, value.CoordinateTransform, version);
            }
        }
        public void Dispose() => inner.Dispose();
    }

    private sealed class FaultingBrush : IDrawBrush
    {
        public bool Fail = true;
        public DrawBrushKind Kind => DrawBrushKind.SolidColor;
        public float Opacity => 1;
        public Color? SolidColor => null;
        public DrawBrushDescriptor CreateDescriptor() => Fail
            ? throw new InjectedCaptureException()
            : new SolidDrawBrushDescriptor(new Color(25, 205, 90), 1);
    }

    private sealed class InjectedCaptureException : Exception { }
}
