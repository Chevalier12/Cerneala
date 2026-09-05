using System.Reflection;
using Cerneala.Drawing;
using Cerneala.Drawing.Prism;
using Cerneala.Tests.Drawing.Prism;
using Cerneala.Tests.UI.Motion.Core;
using Cerneala.UI.Aspect;
using Cerneala.UI.Controls;
using Cerneala.UI.Core;
using Cerneala.UI.Elements;
using Cerneala.UI.Markup;
using Cerneala.UI.Motion;
using Cerneala.UI.Motion.Core;
using Cerneala.UI.Motion.Properties;
using Cerneala.UI.Prism.Runtime;
using Cerneala.UI.Rendering;
using Cerneala.UI.Resources;
using MotionFactory = Cerneala.UI.Motion.Specs.Motion;

namespace Cerneala.Tests.Controls;

using Scene2D = global::Cerneala.UI.Controls.Scene2D;

public sealed class SpriteAnimationStageZeroContractTests
{
    private const string ControlsNamespace = "Cerneala.UI.Controls.";

    [Fact]
    [Trait("SpriteAnimationStage", "0")]
    public void PublicContractExtendsSpriteAndUsesImmutableDurationBasedDefinitions()
    {
        Assert.Null(Resolve("AnimatedSprite2D"));
        Type frame = RequireType("SpriteAnimationFrame");
        Type clip = RequireType("SpriteAnimationClip");
        Type set = RequireType("SpriteAnimationSet");
        Type mode = RequireType("SpriteAnimationStateChangeMode");

        Assert.True(frame.IsSealed);
        Assert.True(clip.IsSealed);
        Assert.True(set.IsSealed);
        RequireReadOnlyProperties(frame, "SourceRect", "Duration", "Flip");
        RequireReadOnlyProperties(clip, "Name", "Frames", "IsLooping", "Duration", "Version");
        RequireReadOnlyProperties(set, "Clips", "Version");
        Assert.Null(frame.GetProperty("FramesPerSecond"));
        Assert.Null(clip.GetProperty("FramesPerSecond"));
        Assert.Equal(["Restart", "Resume"], Enum.GetNames(mode));

        RequireAnimationSurface(typeof(Sprite2D));
        RequireAnimationSurface(typeof(TileInstance2D));
    }

    [Fact]
    [Trait("SpriteAnimationStage", "0")]
    public void SamplerUsesLeftClosedBoundariesAndConstantCostLargeJumps()
    {
        object looping = CreateClip(
            "Walk",
            isLooping: true,
            (new DrawRect(0, 0, 16, 16), TimeSpan.FromMilliseconds(100), RenderSurface2DSpriteFlip.None),
            (new DrawRect(16, 0, 16, 16), TimeSpan.FromMilliseconds(200), RenderSurface2DSpriteFlip.Horizontal),
            (new DrawRect(32, 0, 16, 16), TimeSpan.FromMilliseconds(300), RenderSurface2DSpriteFlip.None));

        AssertSample(looping, TimeSpan.Zero, 1, expectedFrame: 0, completed: false);
        AssertSample(looping, TimeSpan.FromTicks(TimeSpan.FromMilliseconds(100).Ticks - 1), 1, expectedFrame: 0, completed: false);
        AssertSample(looping, TimeSpan.FromMilliseconds(100), 1, expectedFrame: 1, completed: false);
        AssertSample(looping, TimeSpan.FromMilliseconds(300), 1, expectedFrame: 2, completed: false);
        AssertSample(looping, TimeSpan.FromMilliseconds(600), 1, expectedFrame: 0, completed: false);
        AssertSample(looping, TimeSpan.FromMilliseconds(600_000_100), 1, expectedFrame: 1, completed: false);
        AssertSample(looping, TimeSpan.FromMilliseconds(50), 2, expectedFrame: 1, completed: false);
        AssertSample(looping, TimeSpan.FromMilliseconds(500), 0, expectedFrame: 0, completed: false);

        object finite = CreateClip(
            "Attack",
            isLooping: false,
            (new DrawRect(0, 16, 16, 16), TimeSpan.FromMilliseconds(100), RenderSurface2DSpriteFlip.None),
            (new DrawRect(16, 16, 16, 16), TimeSpan.FromMilliseconds(100), RenderSurface2DSpriteFlip.None));
        AssertSample(finite, TimeSpan.FromMilliseconds(200), 1, expectedFrame: 1, completed: true);
        AssertSample(finite, TimeSpan.FromHours(1000), 1, expectedFrame: 1, completed: true);

        TargetInvocationException negative = Assert.Throws<TargetInvocationException>(
            () => Sample(looping, TimeSpan.FromMilliseconds(1), -1));
        Assert.IsType<ArgumentOutOfRangeException>(negative.InnerException);
    }

    [Fact]
    [Trait("SpriteAnimationStage", "0")]
    public void ActiveFrameOverridesSourceRectAndAnimationFlipXorsWithBaseFlip()
    {
        object animations = CreateSet(CreateClip(
            "Walk",
            isLooping: true,
            (new DrawRect(0, 0, 16, 16), TimeSpan.FromMilliseconds(100), RenderSurface2DSpriteFlip.None),
            (new DrawRect(16, 0, 16, 16), TimeSpan.FromMilliseconds(100), RenderSurface2DSpriteFlip.Horizontal)));
        Sprite2D sprite = new()
        {
            Source = new TestImage(64, 64),
            SourceRect = new DrawRect(48, 48, 8, 8),
            Destination = new DrawRect(2, 3, 16, 16),
            Flip = RenderSurface2DSpriteFlip.Horizontal
        };
        SetAnimation(sprite, animations, "Walk");
        RenderSurface2D surface = SurfaceWith(sprite, RenderSurface2DRedrawMode.OnDemand);
        UIRoot root = Attach(surface);
        try
        {
            DrawCommand initial = Assert.Single(Record(surface).Where(IsImage));
            Assert.Equal(new DrawRect(0, 0, 16, 16), initial.ImageSource);
            Assert.Equal(DrawImageFlip.Horizontal, initial.ImageFlip);

            Assert.True(Update(surface, TimeSpan.FromMilliseconds(100)));
            DrawCommand next = Assert.Single(Record(surface).Where(IsImage));
            Assert.Equal(new DrawRect(16, 0, 16, 16), next.ImageSource);
            Assert.Equal(DrawImageFlip.None, next.ImageFlip);
        }
        finally
        {
            ElementLifecycle.DetachSubtree(root, surface);
        }
    }

    [Fact]
    [Trait("SpriteAnimationStage", "0")]
    public void OnDemandInvalidatesOnlyPresentationChangesAndStopsForPauseOrCompletion()
    {
        object animations = CreateSet(CreateClip(
            "Attack",
            isLooping: false,
            (new DrawRect(0, 0, 16, 16), TimeSpan.FromMilliseconds(100), RenderSurface2DSpriteFlip.None),
            (new DrawRect(16, 0, 16, 16), TimeSpan.FromMilliseconds(100), RenderSurface2DSpriteFlip.None)));
        Sprite2D sprite = new() { Source = new TestImage(32, 16), Destination = new DrawRect(0, 0, 16, 16) };
        SetAnimation(sprite, animations, "Attack");
        RenderSurface2D surface = SurfaceWith(sprite, RenderSurface2DRedrawMode.OnDemand);
        UIRoot root = Attach(surface);
        try
        {
            long version = ((IRenderSurface2DFrameSource)surface).FrameVersion;
            Assert.False(Update(surface, TimeSpan.FromMilliseconds(50)));
            Assert.Equal(version, ((IRenderSurface2DFrameSource)surface).FrameVersion);
            Assert.True(Update(surface, TimeSpan.FromMilliseconds(50)));
            Assert.True(((IRenderSurface2DFrameSource)surface).FrameVersion > version);

            Set(sprite, "IsAnimationPaused", true);
            Assert.False(Update(surface, TimeSpan.FromSeconds(1)));
            Set(sprite, "IsAnimationPaused", false);
            Assert.False(Update(surface, TimeSpan.FromMilliseconds(100)));
            Assert.False(Update(surface, TimeSpan.FromSeconds(1)));

            surface.RedrawMode = RenderSurface2DRedrawMode.Continuous;
            Assert.True(Update(surface, TimeSpan.Zero));
        }
        finally
        {
            ElementLifecycle.DetachSubtree(root, surface);
        }
    }

    [Fact]
    [Trait("SpriteAnimationStage", "0")]
    public void LifecycleAtlasStateAndSharedDefinitionsHaveExplicitProgressPolicy()
    {
        object shared = CreateSet(
            CreateClip(
                "Idle",
                isLooping: true,
                (new DrawRect(0, 0, 16, 16), TimeSpan.FromMilliseconds(100), RenderSurface2DSpriteFlip.None),
                (new DrawRect(16, 0, 16, 16), TimeSpan.FromMilliseconds(100), RenderSurface2DSpriteFlip.None)),
            CreateClip(
                "Walk",
                isLooping: true,
                (new DrawRect(0, 16, 16, 16), TimeSpan.FromMilliseconds(100), RenderSurface2DSpriteFlip.None),
                (new DrawRect(16, 16, 16, 16), TimeSpan.FromMilliseconds(100), RenderSurface2DSpriteFlip.None)));
        Sprite2D first = AnimatedSprite(shared, "Idle");
        Sprite2D second = AnimatedSprite(shared, "Idle");
        Set(second, "IsAnimationPaused", true);
        Scene2D scene = new();
        scene.Children.Add(first);
        scene.Children.Add(second);
        RenderSurface2D surface = new() { Scene = scene, RedrawMode = RenderSurface2DRedrawMode.OnDemand };
        UIRoot root = Attach(surface);
        try
        {
            Update(surface, TimeSpan.FromMilliseconds(100));
            DrawRect[] sourceRects = Record(surface).Where(IsImage).Select(static command => command.ImageSource!.Value).ToArray();
            Assert.Equal([new DrawRect(16, 0, 16, 16), new DrawRect(0, 0, 16, 16)], sourceRects);

            first.Source = new TestImage(32, 32);
            Assert.Equal(new DrawRect(16, 0, 16, 16), Assert.Single(Record(surface).Where(IsImage).Take(1)).ImageSource);
            first.DataContext = new object();
            Assert.Equal(new DrawRect(16, 0, 16, 16), Record(surface).First(IsImage).ImageSource);

            Set(first, "AnimationStateChangeMode", Enum.Parse(RequireType("SpriteAnimationStateChangeMode"), "Resume"));
            Set(first, "AnimationState", "Walk");
            Update(surface, TimeSpan.FromMilliseconds(100));
            Set(first, "AnimationState", "Idle");
            Assert.Equal(new DrawRect(16, 0, 16, 16), Record(surface).First(IsImage).ImageSource);

            ElementLifecycle.DetachSubtree(root, surface);
            Assert.False(Update(surface, TimeSpan.FromSeconds(1)));
            ElementLifecycle.AttachSubtree(root, surface);
            Assert.Equal(new DrawRect(16, 0, 16, 16), Record(surface).First(IsImage).ImageSource);

            Set(first, "AnimationStateChangeMode", Enum.Parse(RequireType("SpriteAnimationStateChangeMode"), "Restart"));
            Set(first, "AnimationState", "Walk");
            Assert.Equal(new DrawRect(0, 16, 16, 16), Record(surface).First(IsImage).ImageSource);
        }
        finally
        {
            if (surface.IsAttached)
            {
                ElementLifecycle.DetachSubtree(root, surface);
            }
        }
    }

    [Fact]
    [Trait("SpriteAnimationStage", "0")]
    public void AspectMotionAndPrismKeepOneFrameOwnerAndMotionCancellationPreservesProgress()
    {
        object animations = CreateSet(CreateClip(
            "Walk",
            isLooping: true,
            (new DrawRect(0, 0, 16, 16), TimeSpan.FromMilliseconds(100), RenderSurface2DSpriteFlip.None),
            (new DrawRect(16, 0, 16, 16), TimeSpan.FromMilliseconds(100), RenderSurface2DSpriteFlip.None)));
        Sprite2D sprite = AnimatedSprite(animations, "Walk");
        UiProperty animationStateProperty = GetUiProperty(typeof(Sprite2D), "AnimationState");
        sprite.Aspect = new ElementAspect(
            [
                new ElementAspectValue(animationStateProperty, "Walk"),
                new ElementAspectValue(Sprite2D.TintProperty, Color.Red)
            ]);
        RenderSurface2D surface = SurfaceWith(sprite, RenderSurface2DRedrawMode.OnDemand);
        ManualMotionClock clock = new();
        UIRoot root = new(motionClock: clock);
        root.VisualChildren.Add(surface);
        root.ProcessFrame();
        using IDisposable prism = GeneratedMarkup.AttachPrism(
            sprite,
            () => new PrismInstance(PrismTestData.Composition("Animated", PrismTestData.Layer(1, "Content"))));
        MotionHandle motion = sprite.Motion()
            .Animate(UIElement.OpacityProperty)
            .To(0.25f)
            .With(MotionFactory.Tween<float>(TimeSpan.FromMilliseconds(200)));
        root.ProcessFrame(); // Establish Motion's initial sample before advancing its clock.
        try
        {
            Update(surface, TimeSpan.FromMilliseconds(100));
            clock.Advance(TimeSpan.FromMilliseconds(100));
            root.ProcessFrame();
            DrawCommandList commands = Record(surface);
            Assert.Equal(
                [DrawCommandKind.BeginPrism, DrawCommandKind.DrawImage, DrawCommandKind.EndPrism],
                commands.Select(static command => command.Kind));
            Assert.Equal(new DrawRect(16, 0, 16, 16), commands[1].ImageSource);
            Assert.Equal(Color.Red, sprite.Tint);
            Assert.InRange(sprite.Opacity, 0.25f, 0.99f);

            motion.Cancel(MotionCancelBehavior.Revert);
            root.ProcessFrame(); // Commit the reverted Motion sample through the property store.
            Assert.Equal(1f, sprite.Opacity);
            Assert.Equal(new DrawRect(16, 0, 16, 16), Record(surface).Single(IsImage).ImageSource);

            AnimatablePropertyRegistry registry = new();
            Assert.True(registry.TryGet(GetUiProperty(typeof(Sprite2D), "AnimationPlaybackRate"), out _));
            Assert.False(registry.TryGet(GetUiProperty(typeof(Sprite2D), "Animations"), out _));
            Assert.False(registry.TryGet(animationStateProperty, out _));
            Assert.False(registry.TryGet(Sprite2D.SourceRectProperty, out _));
            Assert.False(registry.TryGet(Sprite2D.FlipProperty, out _));
        }
        finally
        {
            motion.Dispose();
            ElementLifecycle.DetachSubtree(root, surface);
        }
    }

    [Fact]
    [Trait("SpriteAnimationStage", "0")]
    public void PromotedTileUsesTheSameDefinitionAndSamplerWithoutDisablingOtherBatching()
    {
        object animations = CreateSet(CreateClip(
            "Walk",
            isLooping: true,
            (new DrawRect(0, 0, 16, 16), TimeSpan.FromMilliseconds(100), RenderSurface2DSpriteFlip.None),
            (new DrawRect(16, 0, 16, 16), TimeSpan.FromMilliseconds(100), RenderSurface2DSpriteFlip.Horizontal)));
        ResourceId<ImageResource> atlasId = new("Atlas");
        TileMap2D map = new()
        {
            Model = new TileMap2DModel(
                new DrawSize(16, 16),
                [new TileSet2D("World", atlasId, [new TileDefinition2D(1, new DrawRect(0, 0, 16, 16))])],
                [new TileLayer2DModel("Ground", [new TileChunk2D(new TileCoordinate2D(0, 0), 3, 1, [new TileCell2D(1), new TileCell2D(1), new TileCell2D(1)])])],
                new TileMapBounds2D(0, 0, 3, 1))
        };
        TileInstance2D promoted = map.Promote(new TileCellKey2D("Ground", 1, 0));
        SetAnimation(promoted, animations, "Walk");
        Scene2D scene = new();
        scene.Children.Add(map);
        RenderSurface2D surface = new() { Scene = scene, RedrawMode = RenderSurface2DRedrawMode.OnDemand };
        surface.Resources.SetResource(atlasId, new ImageResource("atlas.png"));
        UIRoot root = new();
        root.SetImageLoader(new TestImageLoader(new TestImage(32, 16)));
        root.VisualChildren.Add(surface);
        using IDisposable prism = GeneratedMarkup.AttachPrism(
            promoted,
            () => new PrismInstance(PrismTestData.Composition("Tile", PrismTestData.Layer(1, "Content"))));
        try
        {
            Update(surface, TimeSpan.FromMilliseconds(100));
            DrawCommandList commands = Record(surface);
            DrawCommand promotedDraw = Assert.Single(commands.Where(static command => command.Kind == DrawCommandKind.DrawImage));
            Assert.Equal(new DrawRect(16, 0, 16, 16), promotedDraw.ImageSource);
            Assert.Equal(DrawImageFlip.Horizontal, promotedDraw.ImageFlip);
            Assert.Contains(commands, static command => command.Kind == DrawCommandKind.DrawSpriteBatch);
            Assert.Equal(1, map.GetDiagnosticsSnapshot().PromotedInstancesVisible);
        }
        finally
        {
            ElementLifecycle.DetachSubtree(root, surface);
        }
    }

    private static void RequireAnimationSurface(Type type)
    {
        foreach (string name in new[] { "Animations", "AnimationState", "AnimationPlaybackRate", "IsAnimationPaused", "AnimationStateChangeMode" })
        {
            Assert.NotNull(type.GetProperty(name, BindingFlags.Instance | BindingFlags.Public));
            Assert.NotNull(type.GetField(name + "Property", BindingFlags.Static | BindingFlags.Public));
        }
        Assert.Contains(type.GetMethods(BindingFlags.Instance | BindingFlags.Public), static method => method.Name == "RestartAnimation" && method.GetParameters().Length == 0);
    }

    private static void RequireReadOnlyProperties(Type type, params string[] names)
    {
        foreach (string name in names)
        {
            PropertyInfo? property = type.GetProperty(name, BindingFlags.Instance | BindingFlags.Public);
            Assert.NotNull(property);
            Assert.False(property.SetMethod?.IsPublic == true, $"{type.Name}.{name} must be immutable.");
        }
    }

    private static object CreateFrame(DrawRect sourceRect, TimeSpan duration, RenderSurface2DSpriteFlip flip)
    {
        Type type = RequireType("SpriteAnimationFrame");
        return Activator.CreateInstance(type, sourceRect, duration, flip)!;
    }

    private static object CreateClip(
        string name,
        bool isLooping,
        params (DrawRect Rect, TimeSpan Duration, RenderSurface2DSpriteFlip Flip)[] frames)
    {
        Type frameType = RequireType("SpriteAnimationFrame");
        Array typedFrames = Array.CreateInstance(frameType, frames.Length);
        for (int index = 0; index < frames.Length; index++)
        {
            typedFrames.SetValue(CreateFrame(frames[index].Rect, frames[index].Duration, frames[index].Flip), index);
        }
        return Activator.CreateInstance(RequireType("SpriteAnimationClip"), name, typedFrames, isLooping)!;
    }

    private static object CreateSet(params object[] clips)
    {
        Type clipType = RequireType("SpriteAnimationClip");
        Array typedClips = Array.CreateInstance(clipType, clips.Length);
        for (int index = 0; index < clips.Length; index++)
        {
            typedClips.SetValue(clips[index], index);
        }
        return Activator.CreateInstance(RequireType("SpriteAnimationSet"), typedClips)!;
    }

    private static object Sample(object clip, TimeSpan elapsed, double playbackRate)
    {
        Type sampler = RequireType("SpriteAnimationSampler");
        MethodInfo method = Assert.Single(sampler.GetMethods(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic).Where(static candidate => candidate.Name == "Sample"));
        return method.Invoke(null, [clip, elapsed, playbackRate])!;
    }

    private static void AssertSample(object clip, TimeSpan elapsed, double rate, int expectedFrame, bool completed)
    {
        object sample = Sample(clip, elapsed, rate);
        Assert.Equal(expectedFrame, Get<int>(sample, "FrameIndex"));
        Assert.Equal(completed, Get<bool>(sample, "IsCompleted"));
    }

    private static Sprite2D AnimatedSprite(object animations, string state)
    {
        Sprite2D sprite = new()
        {
            Source = new TestImage(32, 32),
            Destination = new DrawRect(0, 0, 16, 16)
        };
        SetAnimation(sprite, animations, state);
        return sprite;
    }

    private static RenderSurface2D SurfaceWith(SceneNode2D node, RenderSurface2DRedrawMode mode)
    {
        Scene2D scene = new();
        scene.Children.Add(node);
        return new RenderSurface2D { Scene = scene, RedrawMode = mode };
    }

    private static UIRoot Attach(RenderSurface2D surface)
    {
        UIRoot root = new();
        ElementLifecycle.AttachSubtree(root, surface);
        return root;
    }

    private static bool Update(RenderSurface2D surface, TimeSpan frameTime) =>
        ((ITimeSensitiveRenderElement)surface).UpdateRenderTime(frameTime);

    private static DrawCommandList Record(RenderSurface2D surface)
    {
        DrawCommandList commands = new();
        ((IRenderSurface2DFrameSource)surface).RecordFrame(commands, new DrawRect(0, 0, 128, 128));
        return commands;
    }

    private static bool IsImage(DrawCommand command) => command.Kind == DrawCommandKind.DrawImage;

    private static void SetAnimation(object target, object animations, string state)
    {
        Set(target, "Animations", animations);
        Set(target, "AnimationState", state);
    }

    private static void Set(object target, string propertyName, object? value)
    {
        try
        {
            PropertyInfo? property = target.GetType().GetProperty(propertyName, BindingFlags.Instance | BindingFlags.Public);
            Assert.NotNull(property);
            property.SetValue(target, value);
        }
        catch (TargetInvocationException exception) when (exception.InnerException is not null)
        {
            System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(exception.InnerException).Throw();
        }
    }

    private static T Get<T>(object target, string propertyName) =>
        Assert.IsType<T>(target.GetType().GetProperty(propertyName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)!.GetValue(target));

    private static UiProperty GetUiProperty(Type owner, string name) =>
        Assert.IsAssignableFrom<UiProperty>(owner.GetField(name + "Property", BindingFlags.Static | BindingFlags.Public)!.GetValue(null));

    private static Type RequireType(string name)
    {
        Type? type = Resolve(name);
        Assert.True(type is not null, $"RED: approved sprite-animation capability is absent: {name}");
        return type;
    }

    private static Type? Resolve(string name) => typeof(Sprite2D).Assembly.GetType(ControlsNamespace + name);

    private sealed class TestImage(int width, int height) : IDrawImage
    {
        public int Width { get; } = width;

        public int Height { get; } = height;
    }

    private sealed class TestImageLoader(IDrawImage image) : IImageLoader
    {
        public IDrawImage Load(string path) => image;
    }
}
