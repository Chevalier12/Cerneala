using System.Runtime.CompilerServices;
using Cerneala.Drawing.Prism;
using Cerneala.Drawing.Prism.Catalog;
using Cerneala.Tests.UI.Motion.Core;
using Cerneala.UI.Elements;
using Cerneala.UI.Markup;
using Cerneala.UI.Motion.Core;
using Cerneala.UI.Motion.Properties;
using Cerneala.UI.Motion.Specs;
using Cerneala.UI.Prism.Definitions;
using Cerneala.UI.Prism.Runtime;

namespace Cerneala.Tests.UI.Prism;

public sealed class PrismAttachmentTests
{
    [Fact]
    public void AttachDetachAndReattachCreateFreshInstancesAndReconnectBindings()
    {
        UIRoot root = new();
        UIElement element = new();
        int connected = 0;
        int disconnected = 0;
        using IDisposable lifetime = GeneratedMarkup.AttachPrism(
            element,
            () => new PrismInstance(CreateDefinition()),
            new Func<PrismInstance, IDisposable>[]
            {
                _ =>
                {
                    connected++;
                    return new CallbackDisposable(() => disconnected++);
                }
            });

        Assert.False(GeneratedMarkup.TryGetPrismInstance(element, out _));

        ElementLifecycle.AttachSubtree(root, element);
        PrismInstance first = GeneratedMarkup.GetPrismInstance(element);
        Assert.True(
            PrismAttachment.TryGetRenderState(
                element,
                out _,
                out PrismCacheOwnerToken firstOwnerToken));
        Assert.Equal(1, connected);
        Assert.Equal(0, disconnected);

        ElementLifecycle.DetachSubtree(root, element);
        Assert.False(GeneratedMarkup.TryGetPrismInstance(element, out _));
        AssertOwnerInvalidation(
            root,
            firstOwnerToken);
        Assert.Equal(1, disconnected);

        ElementLifecycle.AttachSubtree(root, element);
        PrismInstance second = GeneratedMarkup.GetPrismInstance(element);
        Assert.True(
            PrismAttachment.TryGetRenderState(
                element,
                out _,
                out PrismCacheOwnerToken secondOwnerToken));
        Assert.NotSame(first, second);
        Assert.NotEqual(firstOwnerToken, secondOwnerToken);
        Assert.Equal(2, connected);

        lifetime.Dispose();
        Assert.False(GeneratedMarkup.TryGetPrismInstance(element, out _));
        AssertOwnerInvalidation(
            root,
            secondOwnerToken);
        Assert.Equal(2, disconnected);
    }

    [Fact]
    public void ReplacementDisposesThePreviousAttachmentWithoutDisturbingTheNewOne()
    {
        UIRoot root = new();
        UIElement element = new();
        int previousBindingDisposals = 0;
        IDisposable previous = GeneratedMarkup.AttachPrism(
            element,
            () => new PrismInstance(CreateDefinition()),
            new Func<PrismInstance, IDisposable>[]
            {
                _ => new CallbackDisposable(() => previousBindingDisposals++)
        });
        ElementLifecycle.AttachSubtree(root, element);
        PrismInstance first = GeneratedMarkup.GetPrismInstance(element);
        Assert.True(
            PrismAttachment.TryGetRenderState(
                element,
                out _,
                out PrismCacheOwnerToken firstOwnerToken));

        using IDisposable replacement = GeneratedMarkup.AttachPrism(
            element,
            () => new PrismInstance(CreateDefinition(opacity: 0.5f)));
        PrismInstance second = GeneratedMarkup.GetPrismInstance(element);
        Assert.True(
            PrismAttachment.TryGetRenderState(
                element,
                out _,
                out PrismCacheOwnerToken secondOwnerToken));

        Assert.NotSame(first, second);
        Assert.NotEqual(firstOwnerToken, secondOwnerToken);
        Assert.Equal(0.5f, second.GetLayerState(new PrismNodeId(1)).Opacity);
        Assert.Equal(1, previousBindingDisposals);
        AssertOwnerInvalidation(root, firstOwnerToken);

        previous.Dispose();
        Assert.Same(second, GeneratedMarkup.GetPrismInstance(element));

        ElementLifecycle.DetachSubtree(root, element);
        Assert.False(GeneratedMarkup.TryGetPrismInstance(element, out _));
        AssertOwnerInvalidation(root, secondOwnerToken);
    }

    [Fact]
    public void AttachmentsRemainIndependentAcrossDifferentRoots()
    {
        UIRoot firstRoot = new();
        UIRoot secondRoot = new();
        UIElement firstElement = new();
        UIElement secondElement = new();
        PrismCompositionDefinition definition = CreateDefinition();
        using IDisposable firstLifetime = GeneratedMarkup.AttachPrism(
            firstElement,
            () => new PrismInstance(definition));
        using IDisposable secondLifetime = GeneratedMarkup.AttachPrism(
            secondElement,
            () => new PrismInstance(definition));

        ElementLifecycle.AttachSubtree(firstRoot, firstElement);
        ElementLifecycle.AttachSubtree(secondRoot, secondElement);
        PrismInstance first = GeneratedMarkup.GetPrismInstance(firstElement);
        PrismInstance second = GeneratedMarkup.GetPrismInstance(secondElement);

        Assert.NotSame(first, second);
        Assert.Same(definition, first.Definition);
        Assert.Same(definition, second.Definition);

        ElementLifecycle.DetachSubtree(firstRoot, firstElement);
        Assert.False(GeneratedMarkup.TryGetPrismInstance(firstElement, out _));
        Assert.Same(second, GeneratedMarkup.GetPrismInstance(secondElement));
    }

    [Theory]
    [InlineData(NonRenderableMode.IsVisibleFalse)]
    [InlineData(NonRenderableMode.Hidden)]
    [InlineData(NonRenderableMode.Collapsed)]
    public void NonRenderableAncestorSuspendsBindingsAndVisibleReturnReappliesCurrentBase(
        NonRenderableMode mode)
    {
        UIRoot root = new();
        UIElement container = new();
        UIElement element = new();
        container.VisualChildren.Add(element);
        FloatSource source = new(0.6f);
        int connected = 0;
        int disconnected = 0;
        using IDisposable lifetime = GeneratedMarkup.AttachPrism(
            element,
            () => new PrismInstance(CreateDefinition(opacity: 0.4f)),
            new Func<PrismInstance, IDisposable>[]
            {
                instance =>
                {
                    connected++;
                    return source.Bind(
                        value => instance.GetLayerState(new PrismNodeId(1)).Opacity = value,
                        () => disconnected++);
                }
            });
        root.VisualChildren.Add(container);
        root.ProcessFrame();
        PrismInstance first = GeneratedMarkup.GetPrismInstance(element);
        Assert.True(
            PrismAttachment.TryGetRenderState(
                element,
                out _,
                out PrismCacheOwnerToken firstOwnerToken));

        source.Value = 0.8f;
        SetRenderable(container, mode, renderable: false);
        PrismValueVersion hiddenVersion = first.ValueVersion;
        source.Value = 0.2f;

        Assert.Equal(1, connected);
        Assert.Equal(1, disconnected);
        Assert.Equal(hiddenVersion, first.ValueVersion);
        Assert.False(
            PrismAttachment.TryGetRenderState(
                element,
                out _,
                out _));
        AssertOwnerInvalidation(
            root,
            firstOwnerToken);

        SetRenderable(container, mode, renderable: true);
        PrismInstance resumed = GeneratedMarkup.GetPrismInstance(element);
        Assert.True(
            PrismAttachment.TryGetRenderState(
                element,
                out _,
                out PrismCacheOwnerToken resumedOwnerToken));

        Assert.NotSame(first, resumed);
        Assert.NotEqual(firstOwnerToken, resumedOwnerToken);
        Assert.Equal(2, connected);
        Assert.Equal(0.2f, resumed.GetLayerState(new PrismNodeId(1)).Opacity);
    }

    [Fact]
    public void FullStackTemplateRecyclingAcrossTenThousandCyclesRetainsNoLifecycleObjects()
    {
        AttachmentReferences references = ExerciseTenThousandCycles();

        CollectGarbage();

        Assert.False(references.Element.TryGetTarget(out _));
        Assert.DoesNotContain(
            references.Instances,
            reference => reference.TryGetTarget(out _));
        Assert.DoesNotContain(
            references.MotionHandles,
            reference => reference.TryGetTarget(out _));
        Assert.False(references.HadActiveMotionAfterDetach);
        Assert.Equal(10_000, references.BindingConnections);
        Assert.Equal(10_000, references.BindingDisconnections);
        Assert.True(references.Invalidations.Sum(queue => queue.Count) >= 10_000);
        HashSet<PrismCacheOwnerToken> invalidatedOwners = [];
        foreach (PrismCacheInvalidationQueue queue in references.Invalidations)
        {
            while (queue.TryDequeue(out PrismCacheInvalidation invalidation))
            {
                Assert.Equal(
                    PrismCacheInvalidationKind.Owner,
                    invalidation.Kind);
                invalidatedOwners.Add(invalidation.OwnerToken);
            }
        }
        Assert.Equal(10_000, invalidatedOwners.Count);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static AttachmentReferences ExerciseTenThousandCycles()
    {
        ManualMotionClock clock = new();
        UIRoot[] roots =
        [
            new UIRoot(motionClock: clock),
            new UIRoot(motionClock: clock)
        ];
        UIElement element = new();
        PrismCompositionDefinition definition = CreateFullStackDefinition();
        FloatSource source = new(0.6f);
        List<WeakReference<PrismInstance>> instances = new(10_000);
        List<WeakReference<MotionHandle>> motionHandles = new(10_000);
        int bindingConnections = 0;
        int bindingDisconnections = 0;
        bool hadActiveMotionAfterDetach = false;
        using IDisposable prismLifetime = GeneratedMarkup.AttachPrism(
            element,
            () =>
            {
                PrismInstance instance = new(definition);
                instances.Add(new WeakReference<PrismInstance>(instance));
                return instance;
            },
            new Func<PrismInstance, IDisposable>[]
            {
                instance =>
                {
                    bindingConnections++;
                    return source.Bind(
                        value => instance.GetLayerState(LifecycleLayerId).Opacity = value,
                        () => bindingDisconnections++);
                }
            });
        using IDisposable motionSession = GeneratedMarkup.AttachMotionSession(element);

        for (int cycle = 0; cycle < 10_000; cycle++)
        {
            UIRoot root = roots[cycle % roots.Length];
            source.Value = cycle % 2 == 0 ? 0.6f : 0.7f;
            ElementLifecycle.AttachSubtree(root, element);
            MotionHandle handle = GeneratedMarkup.StartPrismMotionProperty(
                motionSession,
                element,
                propertyId: 1_001,
                static instance => instance.GetLayerState(LifecycleLayerId).Opacity,
                static (instance, value) => instance.GetLayerState(LifecycleLayerId).Opacity = value,
                discrete: false,
                hasFrom: false,
                from: 0f,
                toCurrent: false,
                to: 0.1f,
                spec: new TweenSpec<float>(TimeSpan.FromSeconds(10), Easings.Linear),
                MotionPropertyStartOptions.Default);
            motionHandles.Add(new WeakReference<MotionHandle>(handle));
            root.Motion.Tick();
            ElementLifecycle.DetachSubtree(root, element);
            root.Motion.Tick();
            hadActiveMotionAfterDetach |= root.Motion.HasActiveMotion;
        }

        return new AttachmentReferences(
            new WeakReference<UIElement>(element),
            instances,
            motionHandles,
            roots.Select(root => root.PrismCacheInvalidations).ToArray(),
            bindingConnections,
            bindingDisconnections,
            hadActiveMotionAfterDetach || roots.Any(root => root.Motion.HasActiveMotion));
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

    private static void SetRenderable(
        UIElement element,
        NonRenderableMode mode,
        bool renderable)
    {
        switch (mode)
        {
            case NonRenderableMode.IsVisibleFalse:
                element.IsVisible = renderable;
                break;
            case NonRenderableMode.Hidden:
                element.Visibility = renderable
                    ? Visibility.Visible
                    : Visibility.Hidden;
                break;
            case NonRenderableMode.Collapsed:
                element.Visibility = renderable
                    ? Visibility.Visible
                    : Visibility.Collapsed;
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(mode));
        }
    }

    private static void AssertOwnerInvalidation(
        UIRoot root,
        PrismCacheOwnerToken expected)
    {
        Assert.True(
            root.PrismCacheInvalidations.TryDequeue(
                out PrismCacheInvalidation invalidation));
        Assert.Equal(
            PrismCacheInvalidationKind.Owner,
            invalidation.Kind);
        Assert.Equal(expected, invalidation.OwnerToken);
    }

    private static PrismCompositionDefinition CreateDefinition(float opacity = 1f)
    {
        return new PrismCompositionDefinition(
            "attachment",
            [
                new PrismLayerDefinition(
                    new PrismNodeId(1),
                    "content",
                    filters: [new PrismFilterDefinition(PrismFilterId.Blur)],
                    opacity: opacity)
            ]);
    }

    private static readonly PrismNodeId LifecycleLayerId = new(11);

    private static PrismCompositionDefinition CreateFullStackDefinition()
    {
        PrismMaskDefinition mask = new(new PrismResourceId("LifecycleMask"));
        return new PrismCompositionDefinition(
            "full-stack-lifecycle",
            [
                new PrismGroupDefinition(
                    new PrismNodeId(10),
                    "Composite",
                    [
                        new PrismLayerDefinition(
                            LifecycleLayerId,
                            "Content",
                            filters: [new PrismFilterDefinition(PrismFilterId.Blur)],
                            styles: [new PrismStyleDefinition(PrismStyleId.OuterGlow)],
                            mask: mask)
                    ]),
                new PrismBackdropDefinition(
                    new PrismNodeId(12),
                    "Backdrop",
                    filters: [new PrismFilterDefinition(PrismFilterId.Blur)],
                    mask: mask)
            ]);
    }

    private sealed class CallbackDisposable : IDisposable
    {
        private Action? callback;

        public CallbackDisposable(Action callback)
        {
            this.callback = callback;
        }

        public void Dispose()
        {
            Interlocked.Exchange(ref callback, null)?.Invoke();
        }
    }

    private sealed class FloatSource
    {
        private float value;

        public FloatSource(float value)
        {
            this.value = value;
        }

        public event Action<float>? Changed;

        public float Value
        {
            get => value;
            set
            {
                this.value = value;
                Changed?.Invoke(value);
            }
        }

        public IDisposable Bind(Action<float> update, Action disconnected)
        {
            update(value);
            Changed += update;
            return new CallbackDisposable(() =>
            {
                Changed -= update;
                disconnected();
            });
        }
    }

    public enum NonRenderableMode
    {
        IsVisibleFalse,
        Hidden,
        Collapsed
    }

    private readonly record struct AttachmentReferences(
        WeakReference<UIElement> Element,
        IReadOnlyList<WeakReference<PrismInstance>> Instances,
        IReadOnlyList<WeakReference<MotionHandle>> MotionHandles,
        IReadOnlyList<PrismCacheInvalidationQueue> Invalidations,
        int BindingConnections,
        int BindingDisconnections,
        bool HadActiveMotionAfterDetach);
}
