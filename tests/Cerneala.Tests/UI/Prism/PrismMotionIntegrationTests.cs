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

    private static readonly PrismNodeId LayerId = new(1);

    private static MotionHandle StartOpacityMotion(
        MotionScenario scenario,
        float destination)
    {
        return GeneratedMarkup.StartPrismMotionProperty(
            scenario.Session,
            scenario.Element,
            propertyId: 101,
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

    private static TweenSpec<T> Tween<T>() =>
        new(TimeSpan.FromMilliseconds(100), Easings.Linear);

    private static MotionScenario CreateScenario()
    {
        ManualMotionClock clock = new();
        UIRoot root = new(motionClock: clock);
        UIElement element = new();
        _ = GeneratedMarkup.AttachPrism(
            element,
            () => new PrismInstance(CreateDefinition()));
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

    private static PrismCompositionDefinition CreateDefinition(float opacity = 1f)
    {
        return new PrismCompositionDefinition(
            "motion",
            [
                new PrismLayerDefinition(
                    LayerId,
                    "Glow",
                    filters: [new PrismFilterDefinition(PrismFilterId.Blur)],
                    opacity: opacity)
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
