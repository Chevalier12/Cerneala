using Cerneala.Drawing;
using Cerneala.UI.Controls;
using Cerneala.UI.Elements;
using Cerneala.UI.Layout;
using Cerneala.UI.Media;
using Cerneala.UI.Rendering;

namespace Cerneala.Tests.Controls;

public sealed class RenderSurface2DTests
{
    [Fact]
    public void ContentUsesContentControlLayoutContract()
    {
        RenderSurface2D surface = new()
        {
            Padding = new Thickness(2),
            BorderThickness = new Thickness(1)
        };
        FixedElement child = new(new LayoutSize(10, 5));
        surface.Content = child;

        LayoutSize desired = surface.Measure(
            new MeasureContext(new LayoutSize(100, 100)));
        surface.Arrange(new ArrangeContext(new LayoutRect(0, 0, 30, 20)));

        Assert.Equal(new LayoutSize(16, 11), desired);
        Assert.Equal(new LayoutRect(3, 3, 24, 14), child.ArrangedBounds);
    }

    [Fact]
    public void SurfaceRendersBetweenBackgroundAndRetainedContent()
    {
        RenderSurface2D surface = new()
        {
            Background = new SolidColorBrush(new Color(1, 2, 3)),
            BorderBrush = new SolidColorBrush(new Color(4, 5, 6)),
            BorderThickness = new Thickness(1),
            Content = new RenderingElement(new Color(7, 8, 9))
        };
        surface.Draw += (_, _) => { };
        surface.Measure(new MeasureContext(new LayoutSize(40, 30)));
        surface.Arrange(new ArrangeContext(new LayoutRect(0, 0, 40, 30)));
        RetainedRenderCache cache = PreparedCache(surface);

        new DrawCommandListBuilder().Build(
            surface,
            cache,
            new RenderCounters());

        Assert.Collection(
            cache.RootCommands,
            command => Assert.Equal(DrawCommandKind.FillRectangle, command.Kind),
            command => Assert.Equal(DrawCommandKind.RenderSurface2D, command.Kind),
            command => Assert.Equal(DrawCommandKind.DrawRectangle, command.Kind),
            command =>
            {
                Assert.Equal(DrawCommandKind.FillRectangle, command.Kind);
                Assert.Equal(new Color(7, 8, 9), command.Color);
            });
    }

    [Fact]
    public void EventAndOverrideActivateDrawing()
    {
        RenderSurface2D eventSurface = new();
        RenderSurface2DDrawEventHandler handler = (_, _) => { };

        Assert.False(eventSurface.IsDrawingActiveForTests);
        eventSurface.Draw += handler;
        Assert.True(eventSurface.IsDrawingActiveForTests);
        eventSurface.Draw -= handler;
        Assert.False(eventSurface.IsDrawingActiveForTests);
        Assert.True(new OverriddenSurface().IsDrawingActiveForTests);
    }

    [Fact]
    public void ManagedSurfaceParticipatesInTheCernealaFrameLoop()
    {
        RenderSurface2D surface = new();
        surface.Draw += (_, _) => { };
        int renderVersion = surface.RenderVersion;

        ITimeSensitiveRenderElement timeSensitive = surface;
        bool firstFrameChanged = timeSensitive.UpdateRenderTime(TimeSpan.FromMilliseconds(16));
        int firstFrameVersion = surface.RenderVersion;
        bool secondFrameChanged = timeSensitive.UpdateRenderTime(TimeSpan.FromMilliseconds(32));

        Assert.True(firstFrameChanged);
        Assert.True(firstFrameVersion > renderVersion);
        Assert.True(secondFrameChanged);
        Assert.True(surface.RenderVersion > firstFrameVersion);
    }

    [Fact]
    public void OnDemandSurfaceOnlyRedrawsWhenInvalidated()
    {
        RenderSurface2D surface = new()
        {
            RedrawMode = RenderSurface2DRedrawMode.OnDemand
        };
        surface.Draw += (_, _) => { };
        int renderVersion = surface.RenderVersion;

        bool changed = ((ITimeSensitiveRenderElement)surface)
            .UpdateRenderTime(TimeSpan.FromMilliseconds(16));

        Assert.False(changed);
        Assert.Equal(renderVersion, surface.RenderVersion);

        surface.InvalidateFrame();

        Assert.True(surface.RenderVersion > renderVersion);
    }

    [Fact]
    public void PublicApiDoesNotExposeRawOrExternalSurfaceEscapeHatches()
    {
        Type surfaceType = typeof(RenderSurface2D);
        Type frameType = typeof(RenderSurface2DFrame);

        Assert.Null(surfaceType.GetProperty("Surface"));
        Assert.Null(surfaceType.GetMethod("Present"));
        Assert.Null(surfaceType.GetMethod("ClearSurface"));
        Assert.Null(surfaceType.GetMethod("RefreshSurface"));
        Assert.Null(surfaceType.GetMethod("UpdateRenderTime"));
        Assert.Null(frameType.GetProperty("SpriteBatch"));
        Assert.Null(frameType.GetProperty("GraphicsDevice"));
        Assert.Null(frameType.GetMethod("Begin"));
        Assert.Null(frameType.GetMethod("End"));
    }

    [Fact]
    public void FrameRecordsTheGeneralCernealaDrawingCommands()
    {
        DrawCommandList commands = new();
        RenderSurface2DFrame frame = new(
            commands,
            new DrawRect(0, 0, 64, 48),
            TimeSpan.FromMilliseconds(16));
        SolidColorBrush brush = new(new Color(20, 40, 60));
        DrawTextRun text = new(new TestFont(), "Snake", 12);
        TestImage image = new();

        frame.FillRectangle(new DrawRect(1, 1, 8, 6), brush);
        frame.DrawRectangle(new DrawRect(2, 2, 9, 7), Color.White, 1);
        frame.FillEllipse(new DrawRect(3, 3, 10, 8), Color.HotPink);
        frame.DrawEllipse(new DrawRect(4, 4, 11, 9), brush, 2);
        frame.DrawLine(new DrawPoint(0, 0), new DrawPoint(12, 8), Color.Black, 1);
        frame.FillPath(
            "M0 0L8 0L8 8Z",
            new DrawRect(0, 0, 8, 8),
            new DrawRect(5, 5, 8, 8),
            brush);
        frame.DrawText(text, new DrawPoint(6, 16), Color.White);
        frame.DrawImage(image, new DrawRect(7, 7, 12, 10), Color.White);
        frame.DrawSprite(
            image,
            new DrawRect(8, 8, 12, 10),
            source: null,
            Color.White,
            rotation: 0.2f,
            origin: new DrawPoint(1, 1),
            RenderSurface2DSpriteFlip.Horizontal,
            layerDepth: 0.5f);
        frame.PushClip(new DrawRect(0, 0, 32, 24));
        frame.PopClip();
        frame.Complete();

        Assert.Equal(
            [
                DrawCommandKind.FillRectangle,
                DrawCommandKind.DrawRectangle,
                DrawCommandKind.FillEllipse,
                DrawCommandKind.DrawEllipse,
                DrawCommandKind.DrawLine,
                DrawCommandKind.FillPath,
                DrawCommandKind.DrawText,
                DrawCommandKind.DrawImage,
                DrawCommandKind.DrawImage,
                DrawCommandKind.PushClip,
                DrawCommandKind.PopClip
            ],
            commands.Select(command => command.Kind));

        DrawCommand spriteCommand = commands[8];
        Assert.Equal(0.2f, spriteCommand.ImageRotation);
        Assert.Equal(new DrawPoint(1, 1), spriteCommand.ImageOrigin);
        Assert.Equal(DrawImageFlip.Horizontal, spriteCommand.ImageFlip);
        Assert.Equal(0.5f, spriteCommand.LayerDepth);
    }

    private static RetainedRenderCache PreparedCache(UIElement root)
    {
        RetainedRenderCache cache = new();
        RenderCounters counters = new();
        PrepareSubtree(root, cache, counters);
        return cache;
    }

    private static void PrepareSubtree(
        UIElement element,
        RetainedRenderCache cache,
        RenderCounters counters)
    {
        cache.GetElementCache(element).Ensure(
            element,
            counters,
            forceRebuild: true);
        foreach (UIElement child in element.VisualChildren)
        {
            PrepareSubtree(child, cache, counters);
        }
    }

    private sealed class FixedElement(LayoutSize size) : UIElement
    {
        protected override LayoutSize MeasureCore(MeasureContext context) => size;
    }

    private sealed class RenderingElement(Color color) : UIElement
    {
        protected override void OnRender(RenderContext context)
        {
            context.DrawingContext.FillRectangle(
                Border.ToDrawRect(context.Bounds),
                color);
        }
    }

    private sealed class OverriddenSurface : RenderSurface2D
    {
        protected override void OnDraw(RenderSurface2DFrame frame)
        {
        }
    }

    private sealed class TestFont : IDrawFont
    {
        public string FamilyName => "Test";

        public float Size => 12;
    }

    private sealed class TestImage : IDrawImage
    {
        public int Width => 16;

        public int Height => 16;
    }
}
