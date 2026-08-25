using System.Numerics;
using Cerneala.Drawing;
using Cerneala.Drawing.MonoGame;
using Cerneala.UI.Controls;
using Cerneala.UI.Media;

namespace Cerneala.Tests.Drawing;

public sealed class DrawingTextLayoutTests
{
    private readonly TestFont font = new();
    private readonly SolidColorBrush black = new(Color.Black);

    [Fact]
    [Trait("PlanStage", "6")]
    public void BuilderCachesEquivalentContentAndKeepsStyledRunsImmutable()
    {
        SolidColorBrush accent = new(Color.Tomato);
        DrawTextLayoutBuilder builder = new();
        builder.AddSpan(new DrawTextSpan("styled ", font, 12, black));
        builder.AddSpan(new DrawTextSpan("text", font, 18, accent, opacity: 0.5f));
        DrawTextLayoutOptions options = new(maxWidth: 200, wrapping: DrawTextWrapping.Word);

        DrawTextLayout first = builder.Build(options);
        DrawTextLayout second = builder.Build(options);

        Assert.Same(first, second);
        Assert.Equal(first.StableId, second.StableId);
        Assert.Equal(2, first.Lines.Single().Runs.Count);
        Assert.Same(black, first.Lines[0].Runs[0].Brush);
        Assert.Same(accent, first.Lines[0].Runs[1].Brush);
        Assert.Equal(0.5f, first.Lines[0].Runs[1].Opacity);
    }

    [Fact]
    [Trait("PlanStage", "6")]
    public void WordAndCharacterWrappingNeverSplitCombiningClustersOrEmoji()
    {
        DrawTextLayout word = Layout(
            "one two",
            new DrawTextLayoutOptions(maxWidth: 15, wrapping: DrawTextWrapping.Word));
        DrawTextLayout clusters = Layout(
            "A\u0301🙂B",
            new DrawTextLayoutOptions(maxWidth: 5, wrapping: DrawTextWrapping.Character));

        Assert.Equal(["one", "two"], word.Lines.Select(static line => line.Text));
        Assert.Equal("A\u0301", clusters.Lines[0].Text);
        Assert.Equal("🙂", clusters.Lines[1].Text);
        Assert.Equal("B", clusters.Lines[2].Text);
    }

    [Fact]
    [Trait("PlanStage", "6")]
    public void EllipsisAndMaxLinesTrimAtUnicodeClusterBoundaries()
    {
        DrawTextLayout layout = Layout(
            "A\u0301 B C D",
            new DrawTextLayoutOptions(
                maxWidth: 10,
                wrapping: DrawTextWrapping.Word,
                maxLines: 1,
                trimming: DrawTextTrimming.CharacterEllipsis));

        Assert.Single(layout.Lines);
        Assert.True(layout.Lines[0].IsTrimmed);
        Assert.Equal("A\u0301…", layout.Lines[0].Text);
        Assert.DoesNotContain("\u0301…", layout.Lines[0].Runs.Select(static run => run.Text));
    }

    [Fact]
    [Trait("PlanStage", "6")]
    public void StartEndAndCenterAlignmentRespectResolvedRtlDirection()
    {
        DrawTextLayout start = Layout(
            "אבג",
            new DrawTextLayoutOptions(maxWidth: 40, alignment: DrawTextAlignment.Start));
        DrawTextLayout end = Layout(
            "אבג",
            new DrawTextLayoutOptions(maxWidth: 40, alignment: DrawTextAlignment.End));
        DrawTextLayout center = Layout(
            "אבג",
            new DrawTextLayoutOptions(maxWidth: 40, alignment: DrawTextAlignment.Center));

        Assert.Equal(DrawTextDirection.RightToLeft, start.Lines[0].Direction);
        Assert.True(start.Lines[0].Bounds.X > center.Lines[0].Bounds.X);
        Assert.True(center.Lines[0].Bounds.X > end.Lines[0].Bounds.X);
    }

    [Fact]
    [Trait("PlanStage", "6")]
    public void ContextRecordsOneLogicalCommandAndStateAnalyzerTransformsItsBounds()
    {
        DrawTextLayout layout = Layout(
            "layout",
            new DrawTextLayoutOptions(maxWidth: 30, wrapping: DrawTextWrapping.Word));
        DrawCommandList commands = new();
        DrawingContext drawing = new(commands);
        drawing.PushTransform(System.Numerics.Matrix3x2.CreateTranslation(5, 7));
        drawing.PushClip(new DrawRect(0, 0, 100, 100));
        drawing.DrawTextLayout(layout, new DrawPoint(2, 3));
        drawing.PopClip();
        drawing.PopTransform();

        DrawCommandStateAnalysis analysis = new DrawCommandStateAnalyzer().Analyze(commands);

        Assert.Equal(DrawCommandKind.DrawTextLayout, commands[2].Kind);
        Assert.Same(layout, commands[2].TextLayout);
        Assert.Equal(new DrawPoint(2, 3), commands[2].Position);
        Assert.Equal(5, commands.Count);
        Assert.Equal(7, analysis.Entries[2].Bounds!.Value.X);
        Assert.Equal(10, analysis.Entries[2].Bounds!.Value.Y);
        Assert.Equal(new DrawRect(5, 7, 100, 100), analysis.Entries[2].ClipBounds);
    }

    [Fact]
    [Trait("PlanStage", "6")]
    public void FrameDelegatesLayoutAndRejectsUseAfterCompletion()
    {
        DrawTextLayout layout = Layout("frame", new DrawTextLayoutOptions());
        DrawCommandList commands = new();
        RenderSurface2DFrame frame = new(
            commands,
            new DrawRect(0, 0, 100, 100),
            TimeSpan.Zero);

        frame.DrawTextLayout(layout, new DrawPoint(4, 6));
        frame.Complete();

        Assert.Single(commands);
        Assert.Equal(DrawCommandKind.DrawTextLayout, commands[0].Kind);
        Assert.Throws<ObjectDisposedException>(
            () => frame.DrawTextLayout(layout, default));
    }

    [Fact]
    [Trait("PlanStage", "6")]
    public void RetainedSessionReusesLayoutAndDamagesChangedLayoutBounds()
    {
        if (!OperatingSystem.IsWindows())
        {
            return;
        }

        using MonoGame.PrismGraphExecutorTests.WindowsDxFixture fixture = new();
        using MonoGameRenderSurface2DSession session = new(
            fixture.Session.GraphicsDevice,
            96,
            40);
        DrawTextLayout layout = Layout("stable", new DrawTextLayoutOptions(maxWidth: 36));

        void Draw(RenderSurface2DFrame frame) =>
            frame.DrawTextLayout(layout, new DrawPoint(2, 2));

        session.Render(Draw, Color.Black, TimeSpan.Zero);
        fixture.Session.GraphicsDevice.SetRenderTarget(null);
        session.Render(Draw, Color.Black, TimeSpan.FromMilliseconds(16));
        fixture.Session.GraphicsDevice.SetRenderTarget(null);

        Assert.Equal(1, session.RasterizedFrameCount);
        Assert.Null(session.LastDamageBounds);

        layout = Layout("changed text", new DrawTextLayoutOptions(maxWidth: 60));
        session.Render(Draw, Color.Black, TimeSpan.FromMilliseconds(32));

        Assert.Equal(2, session.RasterizedFrameCount);
        Assert.NotNull(session.LastDamageBounds);
    }

    [Theory]
    [Trait("PlanStage", "6")]
    [InlineData(-1, 10)]
    [InlineData(10, -1)]
    public void LayoutOptionsRejectNegativeConstraints(float width, float height)
    {
        Assert.Throws<ArgumentOutOfRangeException>(
            () => new DrawTextLayoutOptions(maxWidth: width, maxHeight: height));
    }

    private DrawTextLayout Layout(string text, DrawTextLayoutOptions options) =>
        new DrawTextLayoutBuilder()
            .AddSpan(new DrawTextSpan(text, font, 10, black))
            .Build(options);

    private sealed class TestFont : IDrawFont
    {
        public string FamilyName => "Stage6Test";

        public float Size => 10;
    }
}
