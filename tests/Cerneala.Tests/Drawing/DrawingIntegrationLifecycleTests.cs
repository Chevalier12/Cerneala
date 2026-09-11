using Cerneala.Drawing;
using Cerneala.Drawing.Prism;
using Cerneala.Drawing.Prism.Graph;
using Cerneala.Tests.Drawing.Prism;
using Cerneala.UI.Controls;
using Cerneala.UI.Elements;
using Cerneala.UI.Media;

namespace Cerneala.Tests.Drawing;

public sealed class DrawingIntegrationLifecycleTests
{
    [Fact]
    public void FullyRevalidatedEquivalentCommandsShareTheImmutableStateSnapshot()
    {
        const int labels = 1_024;
        DrawCommandList commands = new();
        DrawTextRun run = new(new TestFont(), "unchanged", 10);
        PrismFrameAnalyzer analyzer = new();
        PrismFrameAnalysis Record()
        {
            commands.Clear();
            commands.Add(DrawCommand.PushClip(new DrawRect(0, 0, 100, 100)));
            commands.Add(DrawCommand.PushOpacity(0.5f));
            for (int index = 0; index < labels; index++)
            {
                commands.Add(DrawCommand.DrawText(run, new DrawPoint(index, 0), Color.White));
            }
            commands.Add(DrawCommand.PopOpacity());
            commands.Add(DrawCommand.PopClip());
            return analyzer.Analyze(commands);
        }

        PrismFrameAnalysis original = Record();
        for (int warmup = 0; warmup < 64; warmup++) { Record(); }
        long before = GC.GetAllocatedBytesForCurrentThread();
        PrismFrameAnalysis current = original;
        for (int index = 0; index < 64; index++) { current = Record(); }
        long bytesPerFrame = (GC.GetAllocatedBytesForCurrentThread() - before) / 64;
        Assert.True(bytesPerFrame <= 4_096, $"Equivalent state analysis allocated {bytesPerFrame:N0} bytes/frame.");
        Assert.Same(original.StateAnalysis.Entries, current.StateAnalysis.Entries);
        Assert.NotEqual(original.StateAnalysis.CommandListVersion, current.StateAnalysis.CommandListVersion);
        current.StateAnalysis.EnsureCurrent(commands);
        Assert.Throws<InvalidOperationException>(() => original.StateAnalysis.EnsureCurrent(commands));
        Assert.Throws<NotSupportedException>(() => ((IList<DrawCommandStateEntry>)current.StateAnalysis.Entries)[0] = default);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(3)]
    public void StateSnapshotRevalidationVisitsEveryBrushOnceWhenACommandChanges(int changedIndex)
    {
        int descriptions = 0;
        ImageTestBrush brush = new(new TestImage(16, 16), () => descriptions++);
        DrawTextRun run = new(new TestFont(), "unchanged", 10);
        DrawCommandList commands = new();
        PrismFrameAnalyzer analyzer = new();
        PrismFrameAnalysis Record(bool change)
        {
            commands.Clear();
            commands.Add(DrawCommand.PushClip(new DrawRect(0, 0, 100, 100)));
            commands.Add(DrawCommand.PushOpacity(0.5f));
            for (int index = 0; index < 4; index++)
            {
                commands.Add(DrawCommand.DrawText(run,
                    new DrawPoint(index + (change && index == changedIndex ? 10 : 0), 0), brush));
            }
            commands.Add(DrawCommand.PopOpacity());
            commands.Add(DrawCommand.PopClip());
            descriptions = 0;
            return analyzer.Analyze(commands);
        }

        PrismFrameAnalysis original = Record(false);
        DrawCommandStateEntry[] snapshot = original.StateAnalysis.Entries.ToArray();
        Assert.Equal(4, descriptions);
        PrismFrameAnalysis equivalent = Record(false);
        Assert.Equal(4, descriptions);
        PrismFrameAnalysis changed = Record(true);
        Assert.Equal(4, descriptions);
        Assert.Same(original.StateAnalysis.Entries, equivalent.StateAnalysis.Entries);
        Assert.NotSame(original.StateAnalysis.Entries, changed.StateAnalysis.Entries);
        Assert.Equal(snapshot, original.StateAnalysis.Entries);

        DrawCommandStateAnalysis fresh = new DrawCommandStateAnalyzer().Analyze(commands);
        for (int index = 0; index < commands.Count; index++)
        {
            DrawCommandStateEntry expected = fresh.Entries[index], actual = changed.StateAnalysis.Entries[index];
            Assert.Equal(expected with { Metadata = null }, actual with { Metadata = null });
        }
    }

    [Fact]
    public void MetadataRevalidationDoesNotAllocateUnusedDependencyOrGraphemeSnapshots()
    {
        DrawCommand command = DrawCommand.DrawText(
            new DrawTextRun(new TestFont(), "debug e\u0301 \U0001F469\u200D\U0001F4BB", 10), default, Color.White);
        DrawCommandMetadata original = DrawCommandMetadata.Create(command);
        for (int warmup = 0; warmup < 64; warmup++) { DrawCommandMetadata.Create(command, original); }
        long before = GC.GetAllocatedBytesForCurrentThread();
        for (int index = 0; index < 4_096; index++) { DrawCommandMetadata.Create(command, original); }
        long allocated = GC.GetAllocatedBytesForCurrentThread() - before;
        Assert.True(allocated <= 4_096, $"Equivalent metadata revalidation allocated {allocated:N0} bytes of unused snapshots.");
        Assert.Same(original, DrawCommandMetadata.Create(command, original));
    }

    [Theory]
    [InlineData("")]
    [InlineData("plain debug label")]
    [InlineData("e\u0301\u0308 A\r\nB")]
    [InlineData("\U0001F469\u200D\U0001F4BB \U0001F1F7\U0001F1F4 \U0001F44D\U0001F3FD")]
    [InlineData("\u0915\u094D\u0937\u093F")]
    public void TextMetadataBoundsPreserveRuntimeGraphemeSegmentation(string text)
    {
        DrawCommand command = DrawCommand.DrawText(new DrawTextRun(new TestFont(), text, 10), new DrawPoint(3, 5), Color.White);
        int elements = System.Globalization.StringInfo.ParseCombiningCharacters(text).Length;
        Assert.Equal(new DrawRect(3, 5, elements * 10, 15), DrawCommandMetadata.Create(command).Bounds);
        // Include malformed UTF-16 without depending on test-runner serialization.
        string malformed = string.Concat(text, '\uD800', 'x', '\uDC00');
        command = DrawCommand.DrawText(new DrawTextRun(new TestFont(), malformed, 10), new DrawPoint(3, 5), Color.White);
        elements = System.Globalization.StringInfo.ParseCombiningCharacters(malformed).Length;
        Assert.Equal(new DrawRect(3, 5, elements * 10, 15), DrawCommandMetadata.Create(command).Bounds);
    }

    [Fact]
    public void RerecordedCommandsReuseRevalidatedMetadataWithoutRetainingScopedState()
    {
        const int labels = 1_024;
        DrawTextRun run = new(new TestFont(), "unchanged label", 10);
        DrawCommandList commands = new();
        PrismFrameAnalyzer analyzer = new();
        PrismFrameAnalysis Record(int translation)
        {
            commands.Clear();
            commands.Add(DrawCommand.PushTransform(System.Numerics.Matrix3x2.CreateTranslation(translation, 0)));
            for (int index = 0; index < labels; index++)
            {
                commands.Add(DrawCommand.DrawText(run, new DrawPoint(index, 0), Color.White));
            }
            commands.Add(DrawCommand.PopTransform());
            return analyzer.Analyze(commands);
        }

        PrismFrameAnalysis original = Record(0);
        for (int warmup = 0; warmup < 8; warmup++) { Record(warmup); }
        long before = GC.GetAllocatedBytesForCurrentThread();
        PrismFrameAnalysis current = original;
        for (int index = 0; index < 16; index++) { current = Record(100 + index); }
        long bytesPerFrame = (GC.GetAllocatedBytesForCurrentThread() - before) / 16;
        long budget = (labels + 2L) * (System.Runtime.CompilerServices.Unsafe.SizeOf<DrawCommandStateEntry>() + 160) + 32_768;
        Assert.True(bytesPerFrame <= budget, $"Repeated analysis allocated {bytesPerFrame:N0} bytes/frame; budget {budget:N0}.");
        Assert.Same(original.StateAnalysis.Entries[1].Metadata, current.StateAnalysis.Entries[1].Metadata);
        Assert.NotSame(original.StateAnalysis.Entries[0].Metadata, current.StateAnalysis.Entries[0].Metadata);
        Assert.Equal(0, original.StateAnalysis.Entries[1].Transform.M31);
        Assert.Equal(115, current.StateAnalysis.Entries[1].Transform.M31);
        Assert.NotEqual(original.StateAnalysis.Entries[1].Bounds, current.StateAnalysis.Entries[1].Bounds);
    }

    [Fact]
    public void MetadataReuseRediscoversMutableResourcesAndPreservesPayloadIdentity()
    {
        TestImage first = new(16, 16);
        TestImage second = new(16, 16);
        int descriptions = 0;
        ImageTestBrush brush = new(first, () => descriptions++);
        DrawTextRun run = new(new TestFont(), "label", 10);
        DrawCommandList commands = new();
        PrismFrameAnalyzer analyzer = new();
        DrawCommandMetadata Record()
        {
            commands.Clear();
            commands.Add(DrawCommand.DrawText(run, default, brush));
            return analyzer.Analyze(commands).StateAnalysis.Entries[0].Metadata!;
        }

        DrawCommandMetadata original = Record();
        int previousDescriptions = descriptions;
        Assert.Same(original, Record());
        Assert.True(descriptions > previousDescriptions);
        brush.Image = second;
        DrawCommandMetadata replaced = Record();
        Assert.NotSame(original, replaced);
        Assert.Contains(original.Resources, resource => ReferenceEquals(resource, first));
        Assert.DoesNotContain(original.Resources, resource => ReferenceEquals(resource, second));
        Assert.Contains(replaced.Resources, resource => ReferenceEquals(resource, second));
        run = new DrawTextRun(run.Font, run.Text, run.Size);
        Assert.NotSame(replaced, Record());
    }

    [Fact]
    public void MetadataDoesNotAllocateGeneralPurposeCollectionsForTwoResources()
    {
        TestImage image = new(16, 16);
        DrawMesh2D mesh = Triangle(0, 0, image);
        DrawCommand command = DrawCommand.DrawMesh(mesh);
        for (int i = 0; i < 256; i++)
        {
            GC.KeepAlive(DrawCommandMetadata.Create(command));
        }

        const int iterations = 4096;
        long started = GC.GetAllocatedBytesForCurrentThread();
        for (int i = 0; i < iterations; i++)
        {
            GC.KeepAlive(DrawCommandMetadata.Create(command));
        }
        long bytesPerCommand = (GC.GetAllocatedBytesForCurrentThread() - started) / iterations;
        int budget = System.Runtime.CompilerServices.Unsafe.SizeOf<DrawCommand>() + 224;
        Assert.True(bytesPerCommand <= budget, $"Metadata allocated {bytesPerCommand} bytes; budget {budget}.");

        DrawCommandMetadata metadata = DrawCommandMetadata.Create(command);
        Assert.Equal(2, metadata.Resources.Count);
        Assert.Same(image, metadata.Resources[0]);
        Assert.Same(mesh, metadata.Resources[1]);
        Assert.Throws<NotSupportedException>(() => ((IList<object>)metadata.Resources)[1] = image);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void MetadataDoesNotAllocateGeneralPurposeCollectionsForZeroOrOneResource(bool hasResource)
    {
        DrawMesh2D mesh = Triangle(0, 0);
        DrawCommand command = hasResource
            ? DrawCommand.DrawMesh(mesh)
            : DrawCommand.FillRectangle(new DrawRect(0, 0, 16, 16), Color.White);
        for (int i = 0; i < 256; i++)
        {
            GC.KeepAlive(DrawCommandMetadata.Create(command));
        }

        const int iterations = 4096;
        long started = GC.GetAllocatedBytesForCurrentThread();
        for (int i = 0; i < iterations; i++)
        {
            GC.KeepAlive(DrawCommandMetadata.Create(command));
        }
        long bytesPerCommand = (GC.GetAllocatedBytesForCurrentThread() - started) / iterations;
        // The eager identity snapshot remains; resource discovery needs no list/hash set
        // for zero or one resource, only a read-only singleton in the latter case.
        int budget = System.Runtime.CompilerServices.Unsafe.SizeOf<DrawCommand>() + (hasResource ? 224 : 160);
        Assert.True(bytesPerCommand <= budget, $"Metadata allocated {bytesPerCommand} bytes; budget {budget}.");
    }

    [Fact]
    public void MetadataResourcesRemainReadOnlyAndDeduplicateByReference()
    {
        TestImage first = new(16, 16);
        TestImage second = new(16, 16);
        TestFont font = new();
        ImageTestBrush firstBrush = new(first);
        ImageTestBrush secondBrush = new(second);
        DrawTextLayout layout = new DrawTextLayoutBuilder()
            .AddSpan("first", font, 10, firstBrush)
            .AddSpan("repeat", font, 10, firstBrush)
            .AddSpan("equal but distinct", font, 10, secondBrush)
            .Build();
        DrawCommandMetadata metadata = DrawCommandMetadata.Create(
            DrawCommand.DrawTextLayout(layout, new DrawPoint(0, 0)));
        object[] expected = [layout, font, firstBrush, first, secondBrush, second];
        Assert.Equal(expected.Length, metadata.Resources.Count);
        for (int i = 0; i < expected.Length; i++)
        {
            Assert.Same(expected[i], metadata.Resources[i]);
        }
        Assert.Throws<NotSupportedException>(() => ((IList<object>)metadata.Resources)[0] = second);

        DrawMesh2D mesh = Triangle(0, 0);
        DrawCommandMetadata singleton = DrawCommandMetadata.Create(DrawCommand.DrawMesh(mesh));
        Assert.Same(mesh, Assert.Single(singleton.Resources));
        Assert.Throws<NotSupportedException>(() => ((IList<object>)singleton.Resources)[0] = second);
        Assert.Same(mesh, Assert.Single(singleton.Resources));
    }

    [Fact]
    [Trait("PlanStage", "7")]
    public void EveryCommandKindUsesCentralMetadataInsidePrismAndNestedSurfaces()
    {
        TestImage image = new(16, 16);
        TestFont font = new();
        SolidColorBrush brush = new(Color.White);
        DrawPath path = new DrawPathBuilder()
            .MoveTo(new DrawPoint(0, 0))
            .LineTo(new DrawPoint(8, 0))
            .LineTo(new DrawPoint(8, 8))
            .Close()
            .Build();
        DrawPen pen = new(brush, 1);
        DrawMesh2D mesh = Triangle(1, 1, image);
        DrawPointBatch points = new([new DrawPoint(2, 2)], Color.White, 2);
        DrawLineBatch lines = new([
            new DrawLineSegment2D(new DrawPoint(1, 1), new DrawPoint(5, 5), Color.White)
        ]);
        DrawSpriteBatch sprites = new(image, [new DrawSprite2D(new DrawRect(1, 1, 4, 4))]);
        DrawTextLayout layout = new DrawTextLayoutBuilder()
            .AddSpan("layout", font, 10, brush)
            .Build(new DrawTextLayoutOptions(maxWidth: 40));
        PrismDrawScope prismScope = PrismTestData.Scope(
            PrismTestData.Composition("Stage7", PrismTestData.Layer(1, "Content")),
            bounds: new DrawRect(0, 0, 64, 64));
        DrawCommandList commands = new();

        commands.Add(DrawCommand.BeginPrism(prismScope));
        commands.Add(DrawCommand.FillRectangle(new DrawRect(1, 1, 4, 4), Color.White));
        commands.Add(DrawCommand.DrawRectangle(new DrawRect(1, 1, 4, 4), pen));
        commands.Add(DrawCommand.FillRoundedRectangle(new DrawRect(1, 1, 4, 4), new DrawCornerRadius(1), Color.White));
        commands.Add(DrawCommand.DrawRoundedRectangle(new DrawRect(1, 1, 4, 4), new DrawCornerRadius(1), pen));
        commands.Add(DrawCommand.FillEllipse(new DrawRect(1, 1, 4, 4), Color.White));
        commands.Add(DrawCommand.DrawEllipse(new DrawRect(1, 1, 4, 4), pen));
        commands.Add(DrawCommand.DrawLine(new DrawPoint(1, 1), new DrawPoint(4, 4), pen));
        commands.Add(DrawCommand.FillPath(path, brush));
        commands.Add(DrawCommand.DrawPath(path, pen));
        commands.Add(DrawCommand.DrawText(new DrawTextRun(font, "text", 10), new DrawPoint(1, 1), brush));
        commands.Add(DrawCommand.DrawTextLayout(layout, new DrawPoint(1, 1)));
        commands.Add(DrawCommand.DrawImage(image, new DrawRect(1, 1, 4, 4), Color.White));
        commands.Add(DrawCommand.DrawImageQuad(
            image,
            new DrawPoint(1, 1),
            new DrawPoint(5, 1),
            new DrawPoint(5, 5),
            new DrawPoint(1, 5)));
        commands.Add(DrawCommand.DrawNineSlice(image, new DrawRect(1, 1, 8, 8), new DrawInsets(1)));
        commands.Add(DrawCommand.DrawMesh(mesh));
        commands.Add(DrawCommand.DrawPointBatch(points));
        commands.Add(DrawCommand.DrawLineBatch(lines));
        commands.Add(DrawCommand.DrawSpriteBatch(sprites));
        commands.Add(DrawCommand.RenderSurface2D(new TestSurface(), new DrawRect(1, 1, 8, 8), Color.White));
        commands.Add(DrawCommand.EndPrism());
        commands.Add(DrawCommand.PushClip(new DrawRect(0, 0, 10, 10)));
        commands.Add(DrawCommand.PopClip());
        commands.Add(DrawCommand.PushClip(path));
        commands.Add(DrawCommand.PopClip());
        commands.Add(DrawCommand.PushTransform(System.Numerics.Matrix3x2.Identity));
        commands.Add(DrawCommand.PopTransform());
        commands.Add(DrawCommand.PushOpacity(0.5f));
        commands.Add(DrawCommand.PopOpacity());
        commands.Add(DrawCommand.PushBlend(DrawBlendMode.Multiply));
        commands.Add(DrawCommand.PopBlend());
        commands.Add(DrawCommand.PushLayer(new DrawLayerOptions(0.5f, DrawBlendMode.Screen)));
        commands.Add(DrawCommand.PopLayer());

        DrawCommandStateAnalysis state = new DrawCommandStateAnalyzer().Analyze(commands);
        PrismFrameAnalysis prism = new PrismFrameAnalyzer().Analyze(commands);

        Assert.Equal(
            Enum.GetValues<DrawCommandKind>().OrderBy(static kind => kind),
            commands.Select(static command => command.Kind).Distinct().OrderBy(static kind => kind));
        Assert.All(state.Entries, static entry => Assert.NotNull(entry.Metadata));
        Assert.All(
            Enum.GetValues<DrawCommandKind>(),
            static kind => _ = DrawCommandMetadata.IsContextSensitiveKind(kind));
        Assert.Single(prism.Scopes);
        Assert.Contains(
            state.Entries[15].Metadata!.Resources,
            resource => ReferenceEquals(resource, image));
        Assert.Contains(
            state.Entries[19].Metadata!.Resources,
            resource => resource is TestSurface);
    }

    [Fact]
    [Trait("PlanStage", "7")]
    public void RetainedIdentitySnapshotsPrismValueVersions()
    {
        PrismDrawScope scope = PrismTestData.Scope(
            PrismTestData.Composition("Retained", PrismTestData.Layer(1, "Content")));
        DrawCommand first = DrawCommand.BeginPrism(scope);

        scope.Instance.GetLayerState(new Cerneala.UI.Prism.Definitions.PrismNodeId(1)).Opacity = 0.5f;
        DrawCommand second = DrawCommand.BeginPrism(scope);

        Assert.NotEqual(first.RetainedVersion, second.RetainedVersion);
        Assert.NotEqual(
            DrawCommandMetadata.Create(first).RetainedIdentity,
            DrawCommandMetadata.Create(second).RetainedIdentity);
    }

    [Fact]
    [Trait("PlanStage", "7")]
    public void FrameTracksAllImageResourcesThroughCentralMetadata()
    {
        TestImage image = new(16, 16);
        ImageTestBrush imageBrush = new(image);
        DrawMesh2D mesh = Triangle(0, 0, image);
        DrawSpriteBatch sprites = new(image, [new DrawSprite2D(new DrawRect(0, 0, 4, 4))]);
        DrawTextLayout layout = new DrawTextLayoutBuilder()
            .AddSpan("image brush", new TestFont(), 10, imageBrush)
            .Build();
        DrawCommandList commands = new();
        List<IDrawImage> tracked = [];
        RenderSurface2DFrame frame = new(
            commands,
            new DrawRect(0, 0, 64, 64),
            TimeSpan.Zero,
            tracked.Add);

        frame.DrawImage(image, new DrawRect(0, 0, 4, 4), Color.White);
        frame.DrawMesh(mesh);
        frame.DrawSpriteBatch(sprites);
        frame.DrawTextLayout(layout, new DrawPoint(0, 0));
        frame.Complete();

        Assert.Equal(4, tracked.Count);
        Assert.All(tracked, candidate => Assert.Same(image, candidate));
    }

    [Fact]
    public void FrameImageDependencyTrackingDoesNotAllocateDrawingIdentitySnapshots()
    {
        const int commandCount = 1000;
        DrawTextRun text = new(new TestFont(), "debug label", 10);
        DrawCommandList commands = new();
        for (int index = 0; index < commandCount; index++)
        {
            commands.Add(DrawCommand.DrawText(text, new DrawPoint(index, 20), Color.White));
        }
        Action<IDrawImage> track = static _ => throw new InvalidOperationException("Plain text has no image dependency.");
        for (int warmup = 0; warmup < 16; warmup++)
        {
            new RenderSurface2DFrame(commands, new DrawRect(0, 0, 1000, 40), TimeSpan.Zero, track).Complete();
        }
        RenderSurface2DFrame frame = new(commands, new DrawRect(0, 0, 1000, 40), TimeSpan.Zero, track);
        long before = GC.GetAllocatedBytesForCurrentThread();
        frame.Complete();
        long allocated = GC.GetAllocatedBytesForCurrentThread() - before;

        // Resource discovery may retain a read-only singleton for each font;
        // it does not need bounds or a copied drawing identity for each label.
        const long budget = commandCount * 128L + 4096;
        Assert.True(allocated <= budget, $"Image dependency tracking allocated {allocated:N0} bytes; budget {budget:N0}.");
    }

    [Fact]
    public void ImageDependencyTrackingDoesNotAllocateUnusedResourceSnapshots()
    {
        DrawCommand command = DrawCommand.DrawText(
            new DrawTextRun(new TestFont(), "unchanged debug label", 10),
            new DrawPoint(0, 0), Color.White);
        Action<IDrawImage> track = static _ => throw new InvalidOperationException("Plain text has no image dependency.");
        for (int warmup = 0; warmup < 64; warmup++)
        {
            DrawCommandMetadata.TrackImageDependencies(command, track);
        }

        long before = GC.GetAllocatedBytesForCurrentThread();
        for (int index = 0; index < 2_048; index++)
        {
            DrawCommandMetadata.TrackImageDependencies(command, track);
        }
        long allocated = GC.GetAllocatedBytesForCurrentThread() - before;

        Assert.True(allocated <= 4_096, $"Image-only dependency visits allocated {allocated:N0} bytes of unused resource snapshots.");
    }

    [Fact]
    public void ImageTrackingCompletesDiscoveryBeforeInvokingCallbacks()
    {
        TestImage first = new(16, 16);
        TestImage second = new(16, 16);
        int descriptions = 0;
        TestFont font = new();
        DrawTextLayout layout = new DrawTextLayoutBuilder()
            .AddSpan("first", font, 10, new ImageTestBrush(first, () => descriptions++))
            .AddSpan("second", font, 10, new ImageTestBrush(second, () => descriptions++))
            .Build();
        DrawCommand command = DrawCommand.DrawTextLayout(layout, new DrawPoint(0, 0));
        descriptions = 0;
        List<IDrawImage> tracked = [];

        DrawCommandMetadata.TrackImageDependencies(command, image =>
        {
            Assert.Equal(2, descriptions);
            tracked.Add(image);
        });

        Assert.Equal(2, tracked.Count);
        Assert.Same(first, tracked[0]);
        Assert.Same(second, tracked[1]);
    }

    [Fact]
    [Trait("PlanStage", "7")]
    public void DiagnosticsNameInvalidGeometryAndUnbalancedState()
    {
        ArgumentOutOfRangeException geometry = Assert.Throws<ArgumentOutOfRangeException>(
            () => new DrawMesh2D(
                [
                    new DrawVertex2D(new DrawPoint(0, 0), Color.White),
                    new DrawVertex2D(new DrawPoint(1, 0), Color.White),
                    new DrawVertex2D(new DrawPoint(0, 1), Color.White)
                ],
                [0, 1, 9]));
        DrawCommandList unbalanced = new();
        unbalanced.Add(DrawCommand.PushOpacity(0.5f));
        InvalidOperationException state = Assert.Throws<InvalidOperationException>(
            () => new DrawCommandStateAnalyzer().Analyze(unbalanced));

        Assert.Contains("index", geometry.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("command index 0", state.Message, StringComparison.OrdinalIgnoreCase);
    }

    private static DrawMesh2D Triangle(int x, int y, IDrawImage? image = null) =>
        new(
            [
                new DrawVertex2D(new DrawPoint(x, y), Color.White),
                new DrawVertex2D(new DrawPoint(x + 4, y), Color.White),
                new DrawVertex2D(new DrawPoint(x, y + 4), Color.White)
            ],
            [0, 1, 2],
            image: image);

    private sealed record TestImage(int Width, int Height) : IDrawImage;

    private sealed class TestFont : IDrawFont
    {
        public string FamilyName => "Stage7Test";

        public float Size => 10;
    }

    private sealed class ImageTestBrush(IDrawImage image, Action? describe = null) : IDrawBrush
    {
        public IDrawImage Image { get; set; } = image;

        public DrawBrushKind Kind => DrawBrushKind.Image;

        public float Opacity => 1;

        public Color? SolidColor => null;

        public DrawBrushDescriptor CreateDescriptor()
        {
            describe?.Invoke();
            return new ImageDrawBrushDescriptor(
                Image,
                SourceIdentity: null,
                DrawBrushStretch.Fill,
                DrawBrushAlignmentX.Center,
                DrawBrushAlignmentY.Center,
                Viewport: null,
                Viewbox: null,
                DrawTileMode.None,
                BrushOpacity: 1);
        }
    }

    private sealed class TestSurface : IRenderSurface2DSource;
}
