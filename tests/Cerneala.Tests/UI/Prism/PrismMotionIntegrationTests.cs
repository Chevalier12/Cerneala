using Cerneala.Drawing.Prism.Catalog;
using Cerneala.Tests.UI.Motion.Core;
using Cerneala.UI.Elements;
using Cerneala.UI.Markup;
using Cerneala.UI.Motion.Core;
using Cerneala.UI.Motion.Properties;
using Cerneala.UI.Motion.Specs;
using Cerneala.UI.Prism.Definitions;
using Cerneala.UI.Prism.Runtime;
using Cerneala.UI.Rendering;
using System.Runtime.CompilerServices;

namespace Cerneala.Tests.UI.Prism;

public sealed class PrismMotionIntegrationTests
{
    [Fact]
    public void NumericMotionChangesOnlyPrismValueStateAndPresentationStats()
    {
        MotionScenario scenario = CreateScenario();
        PrismInstance instance = scenario.Instance;
        PrismStructuralVersion structuralVersion = instance.StructuralVersion;
        ElementRenderCache renderCache = scenario.Root.RetainedRenderCache.GetElementCache(
            scenario.Element);
        renderCache.Ensure(scenario.Element, new RenderCounters());

        _ = StartOpacityMotion(scenario, 0.25f);
        scenario.Root.Motion.Tick();
        scenario.Clock.Advance(TimeSpan.FromMilliseconds(50));
        MotionFrameResult frame = scenario.Root.Motion.Tick();

        Assert.InRange(
            instance.GetLayerState(LayerId).Opacity,
            0.624f,
            0.626f);
        Assert.Equal(structuralVersion, instance.StructuralVersion);
        Assert.Equal(1, frame.MotionPropertyWrites);
        Assert.Equal(1, frame.MotionRenderInvalidations);
        Assert.Equal(0, frame.MotionLayoutInvalidations);
        Assert.True(renderCache.IsValid);
        Assert.Equal(1, scenario.Root.PrismCacheInvalidations.Count);
    }

    [Fact]
    public void DiscreteBooleanMotionWritesOnlyAtCompletion()
    {
        MotionScenario scenario = CreateScenario();
        PrismLayerState layer = scenario.Instance.GetLayerState(LayerId);
        MotionHandle handle = GeneratedMarkup.StartPrismMotionProperty(
            scenario.Session,
            scenario.Element,
            propertyId: 102,
            scope: null,
            static instance => instance.GetLayerState(LayerId).Visible,
            static (instance, value) => instance.GetLayerState(LayerId).Visible = value,
            discrete: true,
            hasFrom: false,
            from: false,
            toCurrent: false,
            to: false,
            spec: Tween<bool>(),
            new MotionPropertyStartOptions { HoldOnComplete = true });

        scenario.Root.Motion.Tick();
        scenario.Clock.Advance(TimeSpan.FromMilliseconds(50));
        scenario.Root.Motion.Tick();
        Assert.True(layer.Visible);
        Assert.True(handle.IsActive);

        scenario.Clock.Advance(TimeSpan.FromMilliseconds(50));
        scenario.Root.Motion.Tick();
        Assert.False(layer.Visible);
        Assert.True(handle.IsCompleted);
    }

    [Fact]
    public void DiscreteEnumMotionWritesOnlyAtCompletion()
    {
        MotionScenario scenario = CreateScenario();
        PrismLayerState layer = scenario.Instance.GetLayerState(LayerId);
        MotionHandle handle = GeneratedMarkup.StartPrismMotionProperty(
            scenario.Session,
            scenario.Element,
            propertyId: 103,
            scope: LayerId,
            static instance => instance.GetLayerState(LayerId).BlendMode,
            static (instance, value) => instance.GetLayerState(LayerId).BlendMode = value,
            discrete: true,
            hasFrom: false,
            from: PrismBlendMode.Normal,
            toCurrent: false,
            to: PrismBlendMode.Screen,
            spec: Tween<PrismBlendMode>(),
            new MotionPropertyStartOptions { HoldOnComplete = true });

        scenario.Root.Motion.Tick();
        scenario.Clock.Advance(TimeSpan.FromMilliseconds(50));
        scenario.Root.Motion.Tick();
        Assert.Equal(PrismBlendMode.Normal, layer.BlendMode);
        Assert.True(handle.IsActive);

        scenario.Clock.Advance(TimeSpan.FromMilliseconds(50));
        scenario.Root.Motion.Tick();
        Assert.Equal(PrismBlendMode.Screen, layer.BlendMode);
        Assert.True(handle.IsCompleted);
    }

    [Theory]
    [InlineData(Visibility.Visible, true)]
    [InlineData(Visibility.Hidden, false)]
    [InlineData(Visibility.Collapsed, false)]
    public void NonRenderableOwnerCancelsOnceWithoutAnotherPrismWrite(
        Visibility visibility,
        bool useIsVisible)
    {
        MotionScenario scenario = CreateScenario();
        MotionHandle handle = StartOpacityMotion(scenario, 0.25f);
        int canceled = 0;
        handle.Completed += (_, args) =>
        {
            if (args.IsCanceled)
            {
                canceled++;
            }
        };
        scenario.Root.Motion.Tick();
        scenario.Clock.Advance(TimeSpan.FromMilliseconds(25));
        scenario.Root.Motion.Tick();
        PrismValueVersion beforeHide = scenario.Instance.ValueVersion;

        if (useIsVisible)
        {
            scenario.Element.IsVisible = false;
        }
        else
        {
            scenario.Element.Visibility = visibility;
        }

        Assert.Equal(1, canceled);
        Assert.True(handle.IsCanceled);
        Assert.False(scenario.Root.Motion.HasActiveMotion);
        scenario.Clock.Advance(TimeSpan.FromMilliseconds(25));
        MotionFrameResult hiddenFrame = scenario.Root.Motion.Tick();
        scenario.Clock.Advance(TimeSpan.FromMilliseconds(25));
        scenario.Root.Motion.Tick();

        Assert.Equal(1, canceled);
        Assert.True(handle.IsCanceled);
        Assert.Equal(beforeHide, scenario.Instance.ValueVersion);
        Assert.Equal(0, hiddenFrame.MotionPropertyWrites);
        Assert.Equal(0, hiddenFrame.MotionRenderInvalidations);

        scenario.Element.IsVisible = true;
        scenario.Element.Visibility = Visibility.Visible;
        PrismInstance resumed = GeneratedMarkup.GetPrismInstance(scenario.Element);
        scenario.Clock.Advance(TimeSpan.FromMilliseconds(25));
        MotionFrameResult resumedFrame = scenario.Root.Motion.Tick();

        Assert.NotSame(scenario.Instance, resumed);
        Assert.Equal(1f, resumed.GetLayerState(LayerId).Opacity);
        Assert.True(handle.IsCanceled);
        Assert.False(resumedFrame.NeedsAnotherFrame);
    }

    [Fact]
    public void DetachCancelsOnceAndReleasesThePrismBinding()
    {
        MotionScenario scenario = CreateScenario();
        MotionHandle handle = StartOpacityMotion(scenario, 0.25f);
        int canceled = 0;
        handle.Completed += (_, args) =>
        {
            if (args.IsCanceled)
            {
                canceled++;
            }
        };
        scenario.Root.Motion.Tick();

        scenario.Root.VisualChildren.Remove(scenario.Element);
        scenario.Root.Motion.Tick();

        Assert.Equal(1, canceled);
        Assert.True(handle.IsCanceled);
        Assert.False(scenario.Root.Motion.HasActiveMotion);
    }

    [Fact]
    public void PrismReplacementCancelsTheOldTargetOnce()
    {
        MotionScenario scenario = CreateScenario();
        MotionHandle handle = StartOpacityMotion(scenario, 0.25f);
        int canceled = 0;
        handle.Completed += (_, args) =>
        {
            if (args.IsCanceled)
            {
                canceled++;
            }
        };
        scenario.Root.Motion.Tick();

        using IDisposable replacement = GeneratedMarkup.AttachPrism(
            scenario.Element,
            () => new PrismInstance(CreateDefinition(0.75f)));
        PrismInstance replacementInstance = GeneratedMarkup.GetPrismInstance(
            scenario.Element);
        scenario.Clock.Advance(TimeSpan.FromMilliseconds(25));
        scenario.Root.Motion.Tick();
        scenario.Clock.Advance(TimeSpan.FromMilliseconds(25));
        scenario.Root.Motion.Tick();

        Assert.NotSame(scenario.Instance, replacementInstance);
        Assert.Equal(1, canceled);
        Assert.True(handle.IsCanceled);
        Assert.Equal(0.75f, replacementInstance.GetLayerState(LayerId).Opacity);
    }

    [Fact]
    public void WritingTheCurrentValueDoesNotAdvancePrismVersions()
    {
        MotionScenario scenario = CreateScenario();
        PrismValueVersion valueVersion = scenario.Instance.ValueVersion;
        PrismStructuralVersion structuralVersion = scenario.Instance.StructuralVersion;

        MotionHandle handle = StartOpacityMotion(scenario, 1f);
        scenario.Root.Motion.Tick();
        scenario.Clock.Advance(TimeSpan.FromMilliseconds(100));
        MotionFrameResult frame = scenario.Root.Motion.Tick();

        Assert.True(handle.IsCompleted);
        Assert.Equal(valueVersion, scenario.Instance.ValueVersion);
        Assert.Equal(structuralVersion, scenario.Instance.StructuralVersion);
        Assert.Equal(0, frame.MotionPropertyWrites);
        Assert.Equal(0, frame.MotionRenderInvalidations);
        Assert.Equal(0, frame.MotionLayoutInvalidations);
    }

    [Fact]
    public void RepeatedChapterNavigationReleasesOwnersInstancesAndMotion()
    {
        NavigationReferences references = ExerciseNavigationCycles();

        CollectGarbage();

        Assert.False(references.HadActiveMotionAfterDetach);
        Assert.DoesNotContain(
            references.Elements,
            reference => reference.TryGetTarget(out _));
        Assert.DoesNotContain(
            references.Instances,
            reference => reference.TryGetTarget(out _));
    }

    [Fact]
    public void AnimatedPrismParameterAllocatesNoPerFrameClosuresOrCollectionsAfterWarmup()
    {
        MotionScenario scenario = CreateScenario();
        _ = GeneratedMarkup.StartPrismMotionProperty(
            scenario.Session,
            scenario.Element,
            propertyId: 104,
            scope: LayerId,
            static instance => instance.GetLayerState(LayerId).Opacity,
            static (instance, value) => instance.GetLayerState(LayerId).Opacity = value,
            discrete: false,
            hasFrom: false,
            from: 0f,
            toCurrent: false,
            to: 0f,
            spec: new TweenSpec<float>(
                TimeSpan.FromSeconds(10),
                Easings.Linear),
            MotionPropertyStartOptions.Default);
        scenario.Root.Motion.Tick();
        for (int frame = 0; frame < 8; frame++)
        {
            scenario.Clock.Advance(TimeSpan.FromMilliseconds(1));
            scenario.Root.Motion.Tick();
        }

        CollectGarbage();
        long before = GC.GetAllocatedBytesForCurrentThread();
        for (int frame = 0; frame < 32; frame++)
        {
            scenario.Clock.Advance(TimeSpan.FromMilliseconds(1));
            scenario.Root.Motion.Tick();
        }

        long allocated = GC.GetAllocatedBytesForCurrentThread() - before;
        Assert.Equal(0, allocated);
    }

    [Fact]
    public void LowerPriorityStartWithFromIsRejectedWithoutChangingTheActiveMotion()
    {
        MotionScenario scenario = CreateScenario();
        PrismLayerState layer = scenario.Instance.GetLayerState(LayerId);
        MotionHandle high = StartHalfwayReducedMotionOpacity(scenario);
        float current = layer.Opacity;

        MotionHandle rejected = GeneratedMarkup.StartPrismMotionProperty(
            scenario.Session,
            scenario.Element,
            propertyId: 101,
            scope: LayerId,
            static instance => instance.GetLayerState(LayerId).Opacity,
            static (instance, value) => instance.GetLayerState(LayerId).Opacity = value,
            discrete: false,
            hasFrom: true,
            from: 1f,
            toCurrent: false,
            to: 1f,
            spec: Tween<float>(),
            MotionPropertyStartOptions.Default);
        scenario.Root.Motion.Tick();

        Assert.True(rejected.IsCanceled);
        Assert.True(high.IsActive);
        Assert.Equal(current, layer.Opacity);
    }

    [Fact]
    public void LowerPriorityBoundStartWithFromIsRejectedWithoutChangingTheActiveMotion()
    {
        MotionScenario scenario = CreateScenario();
        PrismLayerState layer = scenario.Instance.GetLayerState(LayerId);
        MotionHandle high = StartHalfwayReducedMotionOpacity(scenario);
        float current = layer.Opacity;
        UIElement source = new() { Opacity = 1f };
        MarkupObservation observation = GeneratedMarkup.ObserveProperty(source, UIElement.OpacityProperty);

        MotionHandle rejected = GeneratedMarkup.StartBoundPrismMotionProperty(
            scenario.Session,
            scenario.Element,
            propertyId: 101,
            scope: LayerId,
            static instance => instance.GetLayerState(LayerId).Opacity,
            static (instance, value) => instance.GetLayerState(LayerId).Opacity = value,
            discrete: false,
            hasFrom: true,
            from: 1f,
            observation,
            Cerneala.UI.Data.BindingMode.OneWay,
            value => (float)value!,
            spec: Tween<float>(),
            MotionPropertyStartOptions.Default);
        scenario.Root.Motion.Tick();

        Assert.True(rejected.IsCanceled);
        Assert.True(high.IsActive);
        Assert.Equal(current, layer.Opacity);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void LayerVisibleFalseCancelsItsActiveMotionOnce(bool hideLayer)
    {
        MotionScenario scenario = CreateScenario();
        MotionHandle handle = StartOpacityMotion(scenario, 0.25f);
        int canceled = 0;
        handle.Completed += (_, args) =>
        {
            if (args.IsCanceled)
            {
                canceled++;
            }
        };
        scenario.Root.Motion.Tick();
        scenario.Clock.Advance(TimeSpan.FromMilliseconds(25));
        scenario.Root.Motion.Tick();

        if (hideLayer)
        {
            scenario.Instance.GetLayerState(LayerId).Visible = false;
        }

        scenario.Clock.Advance(TimeSpan.FromMilliseconds(25));
        MotionFrameResult frame = scenario.Root.Motion.Tick();

        Assert.Equal(hideLayer, handle.IsCanceled);
        Assert.Equal(hideLayer ? 1 : 0, canceled);
        Assert.Equal(hideLayer ? 0 : 1, frame.MotionPropertyWrites);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void GroupVisibleFalseCancelsItsActiveMotionOnce(bool hideGroup)
    {
        MotionScenario scenario = CreateScenario(CreateGroupDefinition);
        MotionHandle handle = GeneratedMarkup.StartPrismMotionProperty(
            scenario.Session,
            scenario.Element,
            propertyId: 106,
            scope: GroupId,
            static instance => instance.GetGroupState(GroupId).Opacity,
            static (instance, value) => instance.GetGroupState(GroupId).Opacity = value,
            discrete: false,
            hasFrom: false,
            from: 0f,
            toCurrent: false,
            to: 0.25f,
            spec: Tween<float>(),
            MotionPropertyStartOptions.Default);
        StrongBox<int> canceled = CountCancels(handle);
        scenario.Root.Motion.Tick();
        scenario.Clock.Advance(TimeSpan.FromMilliseconds(25));
        scenario.Root.Motion.Tick();

        if (hideGroup)
        {
            scenario.Instance.GetGroupState(GroupId).Visible = false;
        }

        scenario.Clock.Advance(TimeSpan.FromMilliseconds(25));
        MotionFrameResult frame = scenario.Root.Motion.Tick();

        Assert.Equal(hideGroup, handle.IsCanceled);
        Assert.Equal(hideGroup ? 1 : 0, canceled.Value);
        Assert.Equal(hideGroup ? 0 : 1, frame.MotionPropertyWrites);
    }

    [Fact]
    public void GroupVisibleFalseCancelsTheActiveMotionOfAChildLayer()
    {
        MotionScenario scenario = CreateScenario(CreateGroupDefinition);
        MotionHandle handle = StartChildOpacityMotion(scenario);
        StrongBox<int> canceled = CountCancels(handle);
        scenario.Root.Motion.Tick();
        scenario.Clock.Advance(TimeSpan.FromMilliseconds(25));
        scenario.Root.Motion.Tick();
        scenario.Instance.GetGroupState(GroupId).Visible = false;
        PrismValueVersion afterHide = scenario.Instance.ValueVersion;
        scenario.Clock.Advance(TimeSpan.FromMilliseconds(25));
        MotionFrameResult frame = scenario.Root.Motion.Tick();

        Assert.True(handle.IsCanceled);
        Assert.Equal(1, canceled.Value);
        Assert.Equal(0, frame.MotionPropertyWrites);
        Assert.Equal(afterHide, scenario.Instance.ValueVersion);
    }

    [Fact]
    public void LayerVisibleTrueAgainDoesNotRestartTheCanceledMotion()
    {
        MotionScenario scenario = CreateScenario();
        PrismLayerState layer = scenario.Instance.GetLayerState(LayerId);
        MotionHandle handle = StartOpacityMotion(scenario, 0.25f);
        StrongBox<int> canceled = CountCancels(handle);
        scenario.Root.Motion.Tick();
        scenario.Clock.Advance(TimeSpan.FromMilliseconds(25));
        scenario.Root.Motion.Tick();
        layer.Visible = false;
        scenario.Clock.Advance(TimeSpan.FromMilliseconds(25));
        scenario.Root.Motion.Tick();
        float opacityAtCancel = layer.Opacity;

        layer.Visible = true;
        scenario.Clock.Advance(TimeSpan.FromMilliseconds(25));
        MotionFrameResult frame = scenario.Root.Motion.Tick();
        scenario.Clock.Advance(TimeSpan.FromMilliseconds(100));
        scenario.Root.Motion.Tick();

        Assert.True(handle.IsCanceled);
        Assert.Equal(1, canceled.Value);
        Assert.Equal(0, frame.MotionPropertyWrites);
        Assert.Equal(opacityAtCancel, layer.Opacity);
        Assert.False(scenario.Root.Motion.HasActiveMotion);
    }

    [Fact]
    public void StartingAMotionOnAHiddenLayerCancelsItWithoutAWrite()
    {
        MotionScenario scenario = CreateScenario();
        PrismLayerState layer = scenario.Instance.GetLayerState(LayerId);
        layer.Visible = false;
        PrismValueVersion afterHide = scenario.Instance.ValueVersion;

        MotionHandle handle = StartOpacityMotion(scenario, 0.25f);
        scenario.Clock.Advance(TimeSpan.FromMilliseconds(50));
        MotionFrameResult frame = scenario.Root.Motion.Tick();

        Assert.True(handle.IsCanceled);
        Assert.Equal(0, frame.MotionPropertyWrites);
        Assert.Equal(afterHide, scenario.Instance.ValueVersion);
        Assert.Equal(1f, layer.Opacity);
    }

    [Fact]
    public void VisibleMotionOutsideTheLayerScopeShowsAHiddenLayerAgain()
    {
        MotionScenario scenario = CreateScenario();
        PrismLayerState layer = scenario.Instance.GetLayerState(LayerId);
        layer.Visible = false;

        MotionHandle handle = GeneratedMarkup.StartPrismMotionProperty(
            scenario.Session,
            scenario.Element,
            propertyId: 102,
            scope: null,
            static instance => instance.GetLayerState(LayerId).Visible,
            static (instance, value) => instance.GetLayerState(LayerId).Visible = value,
            discrete: true,
            hasFrom: false,
            from: false,
            toCurrent: false,
            to: true,
            spec: Tween<bool>(),
            new MotionPropertyStartOptions { HoldOnComplete = true });
        scenario.Root.Motion.Tick();
        scenario.Clock.Advance(TimeSpan.FromMilliseconds(100));
        scenario.Root.Motion.Tick();

        Assert.True(handle.IsCompleted);
        Assert.True(layer.Visible);
    }

    // `@to { Glow.Visible = false; Glow.Opacity = 0; }` and the same block with the two
    // lines swapped: the discrete Visible writes false in the frame the fade ends, so the
    // fade must still land on 0 whichever binding ticks first in that frame.
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void FadeOutAndHideInOneExecutionEndsAtTheFinalOpacityInEitherOrder(bool visibleFirst)
    {
        MotionScenario scenario = CreateScenario();
        PrismLayerState layer = scenario.Instance.GetLayerState(LayerId);
        MotionHandle hide = default!;
        if (visibleFirst)
        {
            hide = StartHideMotion(scenario);
        }

        MotionHandle fade = GeneratedMarkup.StartPrismMotionProperty(
            scenario.Session,
            scenario.Element,
            propertyId: 101,
            scope: LayerId,
            static instance => instance.GetLayerState(LayerId).Opacity,
            static (instance, value) => instance.GetLayerState(LayerId).Opacity = value,
            discrete: false,
            hasFrom: false,
            from: 0f,
            toCurrent: false,
            to: 0f,
            spec: Tween<float>(),
            new MotionPropertyStartOptions { HoldOnComplete = true });
        if (!visibleFirst)
        {
            hide = StartHideMotion(scenario);
        }

        scenario.Root.Motion.Tick();
        for (int frame = 0; frame < 4; frame++)
        {
            scenario.Clock.Advance(TimeSpan.FromMilliseconds(25));
            scenario.Root.Motion.Tick();
        }

        Assert.Equal(
            (true, true, false, 0f),
            (hide.IsCompleted, fade.IsCompleted, layer.Visible, layer.Opacity));
    }

    // `@from { Opacity = 0; } @to { Opacity = 1; }` on the hidden Glow is canceled at once
    // and writes nothing, so a later `@to { Opacity = 0.5; }` must start from the 1 that
    // the Prism store still holds, not from the 0 of the canceled start.
    [Fact]
    public void HiddenStartWithAFromValueDoesNotMoveTheNextMotionStart()
    {
        MotionScenario scenario = CreateScenario();
        PrismLayerState layer = scenario.Instance.GetLayerState(LayerId);
        layer.Visible = false;
        MotionHandle canceled = GeneratedMarkup.StartPrismMotionProperty(
            scenario.Session,
            scenario.Element,
            propertyId: 101,
            scope: LayerId,
            static instance => instance.GetLayerState(LayerId).Opacity,
            static (instance, value) => instance.GetLayerState(LayerId).Opacity = value,
            discrete: false,
            hasFrom: true,
            from: 0f,
            toCurrent: false,
            to: 1f,
            spec: Tween<float>(),
            MotionPropertyStartOptions.Default);
        layer.Visible = true;

        _ = StartOpacityMotion(scenario, 0.5f);
        scenario.Root.Motion.Tick();
        scenario.Clock.Advance(TimeSpan.FromMilliseconds(25));
        scenario.Root.Motion.Tick();

        Assert.Equal((true, 0.875f), (canceled.IsCanceled, layer.Opacity));
    }

    // `@animate { @to { $self.prism.Glow.Opacity = 0.8; } }` completes without hold, so the
    // Glow falls back to its opacity 0. A later `@to { $self.prism.Glow.Opacity = 1; }`
    // without `@from` must start from that visible 0, not from the 0.8 of the first end.
    [Fact]
    public void AnimateWithoutFromStartsFromTheShownValueAfterAnAnimationWithoutHold()
    {
        MotionScenario scenario = CreateScenario(static () => CreateDefinition(opacity: 0f));
        PrismLayerState layer = scenario.Instance.GetLayerState(LayerId);
        _ = StartOpacityMotion(scenario, 0.8f);
        scenario.Root.Motion.Tick();
        scenario.Clock.Advance(TimeSpan.FromMilliseconds(100));
        scenario.Root.Motion.Tick();
        float afterFirst = layer.Opacity;

        _ = StartOpacityMotion(scenario, 1f);
        scenario.Root.Motion.Tick();
        scenario.Clock.Advance(TimeSpan.FromMilliseconds(10));
        scenario.Root.Motion.Tick();

        Assert.Equal(0f, afterFirst);
        Assert.InRange(layer.Opacity, 0.099f, 0.101f);
    }

    // The bound form `@to { $self.prism.Glow.Opacity = {Binding ...}; }` follows the same rule.
    [Fact]
    public void BoundAnimateWithoutFromStartsFromTheShownValueAfterAnAnimationWithoutHold()
    {
        MotionScenario scenario = CreateScenario(static () => CreateDefinition(opacity: 0f));
        PrismLayerState layer = scenario.Instance.GetLayerState(LayerId);
        UIElement source = new() { Opacity = 1f };
        _ = StartOpacityMotion(scenario, 0.8f);
        scenario.Root.Motion.Tick();
        scenario.Clock.Advance(TimeSpan.FromMilliseconds(100));
        scenario.Root.Motion.Tick();
        float afterFirst = layer.Opacity;

        _ = GeneratedMarkup.StartBoundPrismMotionProperty(
            scenario.Session,
            scenario.Element,
            propertyId: 101,
            scope: LayerId,
            static instance => instance.GetLayerState(LayerId).Opacity,
            static (instance, value) => instance.GetLayerState(LayerId).Opacity = value,
            discrete: false,
            hasFrom: false,
            from: 0f,
            GeneratedMarkup.ObserveProperty(source, UIElement.OpacityProperty),
            Cerneala.UI.Data.BindingMode.OneWay,
            value => (float)value!,
            spec: Tween<float>(),
            MotionPropertyStartOptions.Default);
        scenario.Root.Motion.Tick();
        scenario.Clock.Advance(TimeSpan.FromMilliseconds(10));
        scenario.Root.Motion.Tick();

        Assert.Equal(0f, afterFirst);
        Assert.InRange(layer.Opacity, 0.099f, 0.101f);
    }

    private static readonly PrismNodeId LayerId = new(1);

    private static readonly PrismNodeId GroupId = new(2);

    private static readonly PrismNodeId ChildLayerId = new(3);

    private static StrongBox<int> CountCancels(MotionHandle handle)
    {
        StrongBox<int> canceled = new();
        handle.Completed += (_, args) =>
        {
            if (args.IsCanceled)
            {
                canceled.Value++;
            }
        };
        return canceled;
    }

    private static MotionHandle StartChildOpacityMotion(MotionScenario scenario)
    {
        return GeneratedMarkup.StartPrismMotionProperty(
            scenario.Session,
            scenario.Element,
            propertyId: 107,
            scope: ChildLayerId,
            static instance => instance.GetLayerState(ChildLayerId).Opacity,
            static (instance, value) => instance.GetLayerState(ChildLayerId).Opacity = value,
            discrete: false,
            hasFrom: false,
            from: 0f,
            toCurrent: false,
            to: 0.25f,
            spec: Tween<float>(),
            MotionPropertyStartOptions.Default);
    }

    private static MotionHandle StartHalfwayReducedMotionOpacity(MotionScenario scenario)
    {
        MotionHandle high = GeneratedMarkup.StartPrismMotionProperty(
            scenario.Session,
            scenario.Element,
            propertyId: 101,
            scope: LayerId,
            static instance => instance.GetLayerState(LayerId).Opacity,
            static (instance, value) => instance.GetLayerState(LayerId).Opacity = value,
            discrete: false,
            hasFrom: false,
            from: 0f,
            toCurrent: false,
            to: 0f,
            spec: Tween<float>(),
            new MotionPropertyStartOptions { HoldOnComplete = true, Priority = MotionPriority.ReducedMotion });
        scenario.Root.Motion.Tick();
        scenario.Clock.Advance(TimeSpan.FromMilliseconds(50));
        scenario.Root.Motion.Tick();
        Assert.InRange(scenario.Instance.GetLayerState(LayerId).Opacity, 0.499f, 0.501f);
        return high;
    }

    private static MotionHandle StartOpacityMotion(
        MotionScenario scenario,
        float destination)
    {
        return GeneratedMarkup.StartPrismMotionProperty(
            scenario.Session,
            scenario.Element,
            propertyId: 101,
            scope: LayerId,
            static instance => instance.GetLayerState(LayerId).Opacity,
            static (instance, value) => instance.GetLayerState(LayerId).Opacity = value,
            discrete: false,
            hasFrom: false,
            from: 0f,
            toCurrent: false,
            to: destination,
            spec: Tween<float>(),
            MotionPropertyStartOptions.Default);
    }

    private static MotionHandle StartHideMotion(MotionScenario scenario)
    {
        return GeneratedMarkup.StartPrismMotionProperty(
            scenario.Session,
            scenario.Element,
            propertyId: 102,
            scope: null,
            static instance => instance.GetLayerState(LayerId).Visible,
            static (instance, value) => instance.GetLayerState(LayerId).Visible = value,
            discrete: true,
            hasFrom: false,
            from: true,
            toCurrent: false,
            to: false,
            spec: Tween<bool>(),
            new MotionPropertyStartOptions { HoldOnComplete = true });
    }

    private static TweenSpec<T> Tween<T>() =>
        new(TimeSpan.FromMilliseconds(100), Easings.Linear);

    private static MotionScenario CreateScenario() =>
        CreateScenario(static () => CreateDefinition());

    private static MotionScenario CreateScenario(Func<PrismClipDefinition> createDefinition)
    {
        ManualMotionClock clock = new();
        UIRoot root = new(motionClock: clock);
        UIElement element = new();
        _ = GeneratedMarkup.AttachPrism(
            element,
            () => new PrismInstance(createDefinition()));
        IDisposable session = GeneratedMarkup.AttachMotionSession(element);
        root.VisualChildren.Add(element);
        root.ProcessFrame();
        PrismInstance instance = GeneratedMarkup.GetPrismInstance(element);
        return new MotionScenario(clock, root, element, instance, session);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static NavigationReferences ExerciseNavigationCycles()
    {
        ManualMotionClock clock = new();
        UIRoot root = new(motionClock: clock);
        List<WeakReference<UIElement>> elements = new(256);
        List<WeakReference<PrismInstance>> instances = new(256);
        for (int cycle = 0; cycle < 256; cycle++)
        {
            UIElement element = new();
            _ = GeneratedMarkup.AttachPrism(
                element,
                () => new PrismInstance(CreateDefinition()));
            IDisposable session = GeneratedMarkup.AttachMotionSession(element);
            root.VisualChildren.Add(element);
            root.ProcessFrame();
            PrismInstance instance = GeneratedMarkup.GetPrismInstance(element);
            _ = GeneratedMarkup.StartPrismMotionProperty(
                session,
                element,
                propertyId: 105,
                scope: LayerId,
                static target => target.GetLayerState(LayerId).Opacity,
                static (target, value) => target.GetLayerState(LayerId).Opacity = value,
                discrete: false,
                hasFrom: false,
                from: 0f,
                toCurrent: false,
                to: 0f,
                spec: new TweenSpec<float>(
                    TimeSpan.FromSeconds(10),
                    Easings.Linear),
                MotionPropertyStartOptions.Default);
            root.Motion.Tick();
            elements.Add(new WeakReference<UIElement>(element));
            instances.Add(new WeakReference<PrismInstance>(instance));

            root.VisualChildren.Remove(element);
            root.Motion.Tick();
        }

        return new NavigationReferences(
            elements,
            instances,
            root.Motion.HasActiveMotion);
    }

    private static void CollectGarbage()
    {
        for (int pass = 0; pass < 3; pass++)
        {
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();
        }
    }

    private static PrismClipDefinition CreateDefinition(float opacity = 1f)
    {
        return new PrismClipDefinition(
            "motion",
            [
                new PrismLayerDefinition(
                    LayerId,
                    "Glow",
                    filters: [new PrismFilterDefinition(PrismFilterId.Blur)],
                    opacity: opacity)
            ]);
    }

    private static PrismClipDefinition CreateGroupDefinition()
    {
        return new PrismClipDefinition(
            "motion-group",
            [
                new PrismGroupDefinition(
                    GroupId,
                    "Card",
                    [
                        new PrismLayerDefinition(
                            ChildLayerId,
                            "Face",
                            filters: [new PrismFilterDefinition(PrismFilterId.Blur)])
                    ])
            ]);
    }

    private sealed record MotionScenario(
        ManualMotionClock Clock,
        UIRoot Root,
        UIElement Element,
        PrismInstance Instance,
        IDisposable Session);

    private sealed record NavigationReferences(
        IReadOnlyList<WeakReference<UIElement>> Elements,
        IReadOnlyList<WeakReference<PrismInstance>> Instances,
        bool HadActiveMotionAfterDetach);
}
