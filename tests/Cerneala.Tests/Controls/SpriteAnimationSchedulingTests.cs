using Cerneala.Drawing;
using Cerneala.UI.Controls;
using Cerneala.UI.Elements;
using Cerneala.UI.Rendering;
using Cerneala.UI.Markup;
using Cerneala.UI.Prism.Runtime;
using Cerneala.Tests.Drawing.Prism;

namespace Cerneala.Tests.Controls;

using Scene2D = global::Cerneala.UI.Controls.Scene2D;

public sealed class SpriteAnimationSchedulingTests
{
    [Fact]
    [Trait("SpriteAnimationStage", "3")]
    public void NestedAnimationsAdvanceOnceAndCoalesceSurfaceInvalidation()
    {
        SpriteAnimationSet clips = Animations();
        Sprite2D first = Sprite(clips);
        Sprite2D second = Sprite(clips);
        Scene2D nested = new();
        nested.Children.Add(first);
        nested.Children.Add(second);
        Scene2D scene = new();
        scene.Children.Add(nested);
        RenderSurface2D surface = new() { Scene = scene, RedrawMode = RenderSurface2DRedrawMode.OnDemand };
        UIRoot root = new();
        root.VisualChildren.Add(surface);
        Assert.Equal(2, surface.ActiveAnimationCount);
        IRenderSurface2DFrameSource recorder = surface;
        long version = recorder.FrameVersion;

        TimeSensitiveRenderInvalidator.Invalidate(root, TimeSpan.FromMilliseconds(100));

        Assert.Equal(version + 1, recorder.FrameVersion);
        Assert.Equal([16f, 16f], Record(surface).Where(IsImage).Select(command => command.ImageSource!.Value.X));
        TimeSensitiveRenderInvalidator.Invalidate(root, TimeSpan.FromMilliseconds(25));
        Assert.Equal(version + 1, recorder.FrameVersion);
        first.IsAnimationPaused = true;
        second.AnimationPlaybackRate = 0;
        Assert.Equal(0, surface.ActiveAnimationCount);
        long pausedVersion = recorder.FrameVersion;
        for (int index = 0; index < 100; index++)
        {
            TimeSensitiveRenderInvalidator.Invalidate(root, TimeSpan.FromMilliseconds(16));
        }
        Assert.Equal(pausedVersion, recorder.FrameVersion);
        root.VisualChildren.Remove(surface);
        Assert.Equal(0, surface.ActiveAnimationCount);
    }

    [Fact]
    [Trait("SpriteAnimationStage", "3")]
    public void HiddenAndCulledNodesKeepProgressButDetachedNodesDoNot()
    {
        Sprite2D sprite = Sprite(Animations());
        Scene2D scene = new();
        scene.Children.Add(sprite);
        RenderSurface2D surface = new() { Scene = scene, RedrawMode = RenderSurface2DRedrawMode.OnDemand };
        UIRoot root = new();
        root.VisualChildren.Add(surface);
        sprite.IsVisible = false;
        TimeSensitiveRenderInvalidator.Invalidate(root, TimeSpan.FromMilliseconds(100));
        Assert.DoesNotContain(Record(surface), IsImage);
        sprite.IsVisible = true;
        Assert.Equal(16f, Record(surface).Single(IsImage).ImageSource!.Value.X);

        sprite.Destination = new DrawRect(1000, 1000, 16, 16);
        TimeSensitiveRenderInvalidator.Invalidate(root, TimeSpan.FromMilliseconds(100));
        Assert.DoesNotContain(Record(surface), IsImage);
        sprite.Destination = new DrawRect(0, 0, 16, 16);
        Assert.Equal(32f, Record(surface).Single(IsImage).ImageSource!.Value.X);

        scene.Children.Remove(sprite);
        Assert.False(((ITimeSensitiveRenderElement)surface).UpdateRenderTime(TimeSpan.FromMilliseconds(100)));
        scene.Children.Add(sprite);
        Assert.Equal(32f, Record(surface).Single(IsImage).ImageSource!.Value.X);
        TimeSensitiveRenderInvalidator.Invalidate(root, TimeSpan.FromMilliseconds(100));
        Assert.Equal(0f, Record(surface).Single(IsImage).ImageSource!.Value.X);
        root.VisualChildren.Remove(surface);
        Assert.False(((ITimeSensitiveRenderElement)surface).UpdateRenderTime(TimeSpan.FromMilliseconds(100)));
    }

    [Fact]
    [Trait("SpriteAnimationStage", "3")]
    public void NonLoopCompletionStopsAndRestartRegistersTimeAgain()
    {
        Sprite2D sprite = Sprite(Animations(loop: false));
        Scene2D scene = new();
        scene.Children.Add(sprite);
        RenderSurface2D surface = new() { Scene = scene, RedrawMode = RenderSurface2DRedrawMode.OnDemand };
        UIRoot root = new();
        root.VisualChildren.Add(surface);
        ITimeSensitiveRenderElement time = surface;

        Assert.True(time.UpdateRenderTime(TimeSpan.FromMilliseconds(500)));
        Assert.Equal(32f, Record(surface).Single(IsImage).ImageSource!.Value.X);
        Assert.Equal(0, surface.ActiveAnimationCount);
        Assert.False(time.UpdateRenderTime(TimeSpan.FromSeconds(1)));
        sprite.RestartAnimation();
        Assert.Equal(1, surface.ActiveAnimationCount);
        Assert.Equal(0f, Record(surface).Single(IsImage).ImageSource!.Value.X);
        Assert.True(time.UpdateRenderTime(TimeSpan.FromMilliseconds(100)));
        Assert.Equal(16f, Record(surface).Single(IsImage).ImageSource!.Value.X);
        root.VisualChildren.Remove(surface);
    }

    [Fact]
    [Trait("SpriteAnimationStage", "3")]
    public void RepeatedFinalPresentationStopsBeforeRedundantTailDurations()
    {
        Sprite2D sprite = Sprite(new SpriteAnimationSet([new SpriteAnimationClip("Walk", [
            new SpriteAnimationFrame(new DrawRect(0, 0, 16, 16), TimeSpan.FromMilliseconds(100)),
            new SpriteAnimationFrame(new DrawRect(16, 0, 16, 16), TimeSpan.FromMilliseconds(100)),
            new SpriteAnimationFrame(new DrawRect(16, 0, 16, 16), TimeSpan.FromDays(1))], isLooping: false)]));
        Scene2D scene = new();
        scene.Children.Add(sprite);
        RenderSurface2D surface = new() { Scene = scene, RedrawMode = RenderSurface2DRedrawMode.OnDemand };
        UIRoot root = new();
        root.VisualChildren.Add(surface);
        Assert.True(((ITimeSensitiveRenderElement)surface).UpdateRenderTime(TimeSpan.FromMilliseconds(100)));
        Assert.Equal(16f, Record(surface).Single(IsImage).ImageSource!.Value.X);
        Assert.Equal(0, surface.ActiveAnimationCount);
        root.VisualChildren.Remove(surface);
    }

    [Fact]
    [Trait("SpriteAnimationStage", "3")]
    public void SceneReplacementReattachAndOverlayUseOneUiClock()
    {
        Sprite2D sprite = Sprite(Animations());
        Scene2D scene = new();
        scene.Children.Add(sprite);
        ClockOverlay overlay = new();
        RenderSurface2D surface = new() { Scene = scene, Content = overlay, RedrawMode = RenderSurface2DRedrawMode.OnDemand };
        UIRoot root = new();
        root.VisualChildren.Add(surface);
        TimeSensitiveRenderInvalidator.Invalidate(root, TimeSpan.FromMilliseconds(100));
        Assert.Equal(1, overlay.Ticks);
        Assert.Equal(16f, Record(surface).Single(IsImage).ImageSource!.Value.X);
        root.VisualChildren.Remove(surface);
        Assert.Equal(0, surface.ActiveAnimationCount);
        root.VisualChildren.Add(surface);
        Assert.Equal(1, surface.ActiveAnimationCount);
        TimeSensitiveRenderInvalidator.Invalidate(root, TimeSpan.FromMilliseconds(100));
        Assert.Equal(32f, Record(surface).Single(IsImage).ImageSource!.Value.X);
        surface.Scene = new Scene2D();
        Assert.Equal(0, surface.ActiveAnimationCount);
        surface.Scene = scene;
        Assert.Equal(1, surface.ActiveAnimationCount);
        root.VisualChildren.Remove(surface);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    [Trait("SpriteAnimationStage", "3")]
    public void OffscreenPrismInputIsNotCulledAndFrameChangesInvalidateItsContent(bool groupPrism)
    {
        Sprite2D sprite = Sprite(Animations());
        sprite.Destination = new DrawRect(65, 0, 16, 16);
        Scene2D scene = new();
        scene.Children.Add(sprite);
        RenderSurface2D surface = new() { Scene = scene, RedrawMode = RenderSurface2DRedrawMode.OnDemand };
        UIRoot root = new();
        root.VisualChildren.Add(surface);
        using IDisposable prism = GeneratedMarkup.AttachPrism(groupPrism ? scene : sprite,
            () => new PrismInstance(PrismTestData.Composition("Offscreen", PrismTestData.Layer(1, "Content"))));
        DrawCommandList before = Record(surface);
        long version = before.Single(command => command.Kind == DrawCommandKind.BeginPrism).PrismScope!.Value.VisualContentVersion;
        TimeSensitiveRenderInvalidator.Invalidate(root, TimeSpan.FromMilliseconds(100));
        DrawCommandList after = Record(surface);
        Assert.Equal(16f, after.Single(IsImage).ImageSource!.Value.X);
        Assert.NotEqual(version, after.Single(command => command.Kind == DrawCommandKind.BeginPrism).PrismScope!.Value.VisualContentVersion);
        root.VisualChildren.Remove(surface);
    }

    private sealed class ClockOverlay : UIElement, ITimeSensitiveRenderElement
    {
        internal int Ticks { get; private set; }
        public bool UpdateRenderTime(TimeSpan frameTime) { Ticks++; return false; }
    }

    private static SpriteAnimationSet Animations(bool loop = true) => new([
        new SpriteAnimationClip("Walk", [
            new SpriteAnimationFrame(new DrawRect(0, 0, 16, 16), TimeSpan.FromMilliseconds(100)),
            new SpriteAnimationFrame(new DrawRect(16, 0, 16, 16), TimeSpan.FromMilliseconds(100)),
            new SpriteAnimationFrame(new DrawRect(32, 0, 16, 16), TimeSpan.FromMilliseconds(100))], loop)]);

    private static Sprite2D Sprite(SpriteAnimationSet clips) => new()
    {
        Source = new Image(), Destination = new DrawRect(0, 0, 16, 16),
        Animations = clips, AnimationState = "Walk"
    };

    private static DrawCommandList Record(RenderSurface2D surface)
    {
        DrawCommandList commands = new();
        ((IRenderSurface2DFrameSource)surface).RecordFrame(commands, new DrawRect(0, 0, 64, 64));
        return commands;
    }

    private static bool IsImage(DrawCommand command) => command.Kind == DrawCommandKind.DrawImage;

    private sealed class Image : IDrawImage
    {
        public int Width => 64;
        public int Height => 16;
    }
}
