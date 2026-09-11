using System.Numerics;
using Cerneala.Backends.SdlGpu;
using Cerneala.Drawing;
using Cerneala.Drawing.Prism;
using Cerneala.Drawing.Prism.Catalog;
using Cerneala.Drawing.Prism.Graph;
using Cerneala.Tests.Drawing.Prism;
using Cerneala.Tests.SdlGpu;
using Cerneala.UI.Markup;
using Cerneala.UI.Prism.Definitions;
using Cerneala.UI.Prism.Runtime;

namespace Cerneala.Tests.Drawing.SdlGpu;

[Collection(SdlNativeTestCollection.Name)]
public sealed class PrismRetainedExecutionTests
{
    [SdlNativeFact]
    public void CroppedNestedScopesPreserveReferenceCoordinatesAndCachedPixels()
    {
        using SdlDrawingFixture fixture = new(256, 256);
        for (int scenario = 0; scenario < 4; scenario++)
        {
            DrawCommandList reference = Commands(cropped: false);
            DrawCommandList cropped = Commands(cropped: true);
            Color[] expected = fixture.Render(reference, Color.Transparent);
            Color[] actual = fixture.Render(cropped, Color.Transparent);
            Assert.Equal(0, fixture.Backend.PrismDiagnostics.Count);
            Assert.True(expected.SequenceEqual(actual), $"Cropped scenario {scenario} changed pixels.");
            Assert.Equal(actual, fixture.Render(cropped, Color.Transparent));

            DrawCommandList Commands(bool cropped)
            {
                DrawRect canvas = new(0, 0, 256, 256);
                DrawRect content = new(96, 100, 48, 32);
                PrismDrawScope parent = PrismTestData.Scope(new PrismCompositionDefinition("Reference canvas",
                    [new PrismLayerDefinition(new(1), "Blur", filters: [new(PrismFilterId.Blur)])]),
                    ownerToken: 94000 + scenario * 4 + (cropped ? 2 : 0), bounds: canvas);
                PrismLayerDefinition layer = scenario switch
                {
                    0 => new(new(2), "Noisy glow", styles: [new(PrismStyleId.OuterGlow)]),
                    1 => new(new(2), "Unaligned gradient", styles: [new(PrismStyleId.GradientOverlay)]),
                    2 => new(new(2), "Threshold", filters: [new(PrismFilterId.Threshold)]),
                    _ => new(new(2), "Dissolve", filters: [new(PrismFilterId.Invert)],
                        opacity: .6f, blendMode: PrismBlendMode.Dissolve)
                };
                PrismDrawScope child = PrismTestData.Scope(new PrismCompositionDefinition("Cropped child", [layer]),
                    ownerToken: 94001 + scenario * 4 + (cropped ? 2 : 0), bounds: cropped ? content : canvas);
                if (scenario is 0 or 1)
                {
                    PrismStyleState style = Assert.Single(child.Instance.GetLayerState(layer.Id).Styles);
                    PrismCatalogEntryDescriptor entry = PrismCatalogRuntime.GetEntry((int)(scenario == 0
                        ? PrismStyleId.OuterGlow : PrismStyleId.GradientOverlay));
                    if (scenario == 0)
                    {
                        SetNumber("Size", 8);
                        SetNumber("Noise", .5f);
                        SetNumber("Jitter", .4f);
                    }
                    else
                    {
                        GeneratedMarkup.SetPrismStyleBoolean(style, entry.StableId,
                            entry.Properties.Single(property => property.Name == "AlignWithLayer").TypeSlot, false);
                    }

                    void SetNumber(string name, float value) => GeneratedMarkup.SetPrismStyleNumber(style,
                        entry.StableId, entry.Properties.Single(property => property.Name == name).TypeSlot, value);
                }
                return PrismTestData.Commands(DrawCommand.BeginPrism(parent),
                    DrawCommand.FillRectangle(canvas, Color.Coral), DrawCommand.BeginPrism(child),
                    DrawCommand.FillRectangle(new(96, 100, 24, 32), new(42, 188, 126, 190)),
                    DrawCommand.FillRectangle(new(120, 100, 24, 32), new(224, 58, 92, 220)),
                    DrawCommand.EndPrism(), DrawCommand.EndPrism());
            }
        }
    }

    [SdlNativeTheory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    public void CachedSimpleMaskedGroupAndNestedScenesMatchFreshPixelsAndSkipCoveredWork(int scenario)
    {
        using SdlDrawingFixture fixture = new(16, 16);
        using SdlGpuImage mask = CreateMask();
        DrawCommandList commands = CreateScene(scenario, mask);
        Color[] original = fixture.Render(commands, Color.Transparent);
        Assert.Equal(0, fixture.Backend.PrismDiagnostics.Count);
        Color[] cached = fixture.Render(commands, Color.Transparent);
        AssertPixels(original, cached);
        Assert.Equal(0, fixture.Backend.PrismDiagnostics.Counters.CaptureCount);
        Assert.True(fixture.Backend.PrismDiagnostics.Counters.PassCount == 1,
            fixture.Backend.PrismDiagnostics.DumpExecutedGraph());
        Assert.Equal(0, fixture.Backend.PrismDiagnostics.Counters.ActiveSurfaceCount);
        fixture.Session.DrawingResources.PrismResources.Invalidate(PrismCacheInvalidation.All);
        Color[] fresh = fixture.Render(commands, Color.Transparent);
        AssertPixels(fresh, cached);
        Assert.True(fixture.Backend.PrismDiagnostics.Counters.CaptureCount > 0);
        Assert.Equal(0, fixture.Backend.PrismDiagnostics.Count);
    }

    [SdlNativeTheory]
    [InlineData("content")]
    [InlineData("structure")]
    [InlineData("parameter")]
    [InlineData("motion")]
    [InlineData("bounds")]
    [InlineData("scale")]
    [InlineData("resource")]
    public void PixelAffectingMutationMissesAndMatchesFreshOutput(string mutation)
    {
        using SdlDrawingFixture fixture = new(16, 16);
        using SdlGpuImage mask = CreateMask();
        PrismResourceId maskId = new("ContractMask");
        PrismCompositionDefinition definition = PrismTestData.Composition("Mutation",
            new PrismLayerDefinition(new(1), "Layer", filters: [new(PrismFilterId.GaussianBlur)],
                mask: mutation == "resource" ? new PrismMaskDefinition(maskId) : null));
        DrawCommandList baseline = Commands(false), changed = Commands(true);
        fixture.Render(baseline, Color.Transparent);
        Color[] actual = fixture.Render(changed, Color.Transparent);
        Assert.True(fixture.Backend.PrismDiagnostics.Counters.PassCount > 1,
            fixture.Backend.PrismDiagnostics.DumpExecutedGraph());
        if (mutation is "parameter" or "motion")
        {
            Assert.Equal(0, fixture.Backend.PrismDiagnostics.Counters.CaptureCount);
            Assert.True(fixture.Backend.PrismDiagnostics.Counters.PassCount <
                fixture.Backend.PrismDiagnostics.Counters.PlannedPassCount);
        }
        fixture.Session.DrawingResources.PrismResources.Invalidate(PrismCacheInvalidation.All);
        Color[] fresh = fixture.Render(changed, Color.Transparent);
        AssertPixels(fresh, actual);
        Assert.Equal(0, fixture.Backend.PrismDiagnostics.Count);

        DrawCommandList Commands(bool change)
        {
            PrismCompositionDefinition current = change && mutation == "structure"
                ? PrismTestData.Composition("Mutation changed", PrismTestData.Layer(1, "Layer"), PrismTestData.Layer(2, "Second"))
                : definition;
            PrismInstance instance = new(current);
            if (mutation is "parameter" or "motion")
                instance.GetLayerState(new(1)).Opacity = change ? mutation == "motion" ? .85f : .65f : .25f;
            DrawRect bounds = change && mutation == "bounds" ? new(0, 0, 14, 15) : new(0, 0, 16, 16);
            PrismDrawResources resources = mutation == "resource" ? PrismDrawResources.Create(
                [new PrismDrawImageResource(maskId, mask, Version: change ? 2 : 1, Identity: 93001)]) : PrismDrawResources.Empty;
            PrismDrawScope scope = new(instance, new(91001), bounds, Matrix3x2.Identity,
                change && mutation == "scale" ? 1.25f : 1, change && mutation == "content" ? 2 : 1, resources);
            Color color = mutation == "content" ? change ? new(35, 205, 95, 255) : new(220, 40, 70, 255) : Color.White;
            return PrismTestData.Commands(DrawCommand.BeginPrism(scope), DrawCommand.FillRectangle(bounds, color), DrawCommand.EndPrism());
        }
    }

    [SdlNativeTheory]
    [InlineData(false)]
    [InlineData(true)]
    public void OwnerInvalidationAndHiddenFramesReleaseEntriesWithoutStalePixels(bool hidden)
    {
        using SdlDrawingFixture fixture = new(16, 16);
        using SdlGpuImage mask = CreateMask();
        DrawCommandList commands = CreateScene(0, mask);
        Color[] fresh = fixture.Render(commands, Color.Transparent);
        SdlGpuPrismDeviceResources resources = fixture.Session.DrawingResources.PrismResources;
        Assert.True(resources.RetainedCount > 0);
        PrismCacheInvalidationQueue invalidations = new();
        invalidations.EnqueueOwner(new PrismCacheOwnerToken(8101));
        DrawCommandList empty = new();
        PrismFrameAnalysis emptyAnalysis = new PrismFrameAnalyzer().Analyze(empty);
        DrawingFrameContext frame = new(emptyAnalysis, null, default, invalidations);
        fixture.Session.BeginFrame(Color.Transparent);
        try { fixture.Session.DrawingBackend.Render(empty, in frame); }
        finally { fixture.Session.CompleteFrame(false); }
        Assert.Equal(0, resources.RetainedCount);
        Assert.Equal(resources.TotalBytes, resources.FreeBytes);
        Assert.Equal(0, fixture.Backend.PrismDiagnostics.Counters.PassCount);
        Assert.Equal(0, fixture.Backend.PrismDiagnostics.Counters.CaptureCount);
        if (hidden)
        {
            Color[] actual = fixture.Render(empty, Color.Transparent);
            Assert.All(actual, pixel => Assert.Equal(Color.Transparent, pixel));
        }
        else
        {
            Color[] actual = fixture.Render(commands, Color.Transparent);
            AssertPixels(fresh, actual);
            Assert.True(fixture.Backend.PrismDiagnostics.Counters.CaptureCount > 0);
        }
    }

    private static DrawCommandList CreateScene(int scenario, SdlGpuImage mask)
    {
        if (scenario == 0)
        {
            PrismDrawScope simple = PrismTestData.Scope(PrismTestData.Composition("Executor gate",
                PrismTestData.Layer(1, "Half opacity", opacity: .5f)), ownerToken: 8101, bounds: new(0, 0, 16, 16));
            return PrismTestData.Commands(DrawCommand.BeginPrism(simple),
                DrawCommand.FillRectangle(new(0, 0, 16, 16), Color.White), DrawCommand.EndPrism());
        }
        if (scenario == 1)
        {
            PrismResourceId id = new("RetainedMatrixMask");
            PrismDrawResources resources = PrismDrawResources.Create([new PrismDrawImageResource(id, mask, Version: 3, Identity: 81031)]);
            PrismLayerDefinition clipped = new(new(11), "Clipped masked screen",
                filters: [new(PrismFilterId.GaussianBlur)], styles: [new(PrismStyleId.ColorOverlay)],
                mask: new(id, density: .72f, feather: 1.25f), opacity: .82f, fill: .68f,
                blendMode: PrismBlendMode.Screen, clipToBelow: true);
            PrismLayerDefinition clipBase = new(new(12), "Multiply clip base",
                filters: [new(PrismFilterId.Invert)], blendMode: PrismBlendMode.Multiply);
            PrismGroupDefinition group = new(new(10), "Isolated group", [clipped, clipBase],
                filters: [new(PrismFilterId.Threshold)], opacity: .88f, blendMode: PrismBlendMode.Normal);
            PrismDrawScope complex = PrismTestData.Scope(PrismTestData.Composition("Complex retained matrix", group),
                ownerToken: 8103, bounds: new(0, 0, 16, 16), resources: resources);
            return PrismTestData.Commands(DrawCommand.BeginPrism(complex),
                DrawCommand.FillRectangle(new(1, 1, 12, 10), new(224, 58, 92, 220)),
                DrawCommand.FillRectangle(new(5, 4, 10, 10), new(43, 170, 224, 196)), DrawCommand.EndPrism());
        }
        PrismDrawScope outer = PrismTestData.Scope(PrismTestData.Composition("Retained outer",
            new PrismLayerDefinition(new(20), "Outer", filters: [new(PrismFilterId.Maximum)])),
            ownerToken: 8201, bounds: new(0, 0, 16, 16));
        PrismDrawScope inner = PrismTestData.Scope(PrismTestData.Composition("Retained inner",
            new PrismLayerDefinition(new(21), "Inner", filters: [new(PrismFilterId.GaussianBlur), new(PrismFilterId.Invert)])),
            ownerToken: 8202, bounds: new(2, 2, 12, 12));
        return PrismTestData.Commands(DrawCommand.BeginPrism(outer),
            DrawCommand.FillRectangle(new(0, 0, 16, 16), new(220, 48, 80, 210)), DrawCommand.BeginPrism(inner),
            DrawCommand.FillRectangle(new(2, 2, 12, 12), new(42, 188, 126, 190)), DrawCommand.EndPrism(), DrawCommand.EndPrism());
    }

    private static SdlGpuImage CreateMask()
    {
        byte[] pixels = new byte[16 * 16 * 4];
        for (int y = 0; y < 16; y++)
        for (int x = 0; x < 16; x++)
        {
            byte alpha = (byte)Math.Clamp(32 + x * 11 + y * 5, 0, 255);
            pixels.AsSpan((y * 16 + x) * 4, 4).Fill(alpha);
        }
        return new(16, 16, pixels);
    }

    private static void AssertPixels(Color[] expected, Color[] actual)
    {
        Assert.Equal(expected.Length, actual.Length);
        for (int i = 0; i < actual.Length; i++)
        {
            Assert.InRange(Math.Abs(expected[i].R - actual[i].R), 0, 1);
            Assert.InRange(Math.Abs(expected[i].G - actual[i].G), 0, 1);
            Assert.InRange(Math.Abs(expected[i].B - actual[i].B), 0, 1);
            Assert.InRange(Math.Abs(expected[i].A - actual[i].A), 0, 1);
        }
    }
}
