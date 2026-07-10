using Cerneala.Drawing;
using Cerneala.UI.Controls;
using Cerneala.UI.Controls.Templates;
using Cerneala.UI.Core;
using Cerneala.UI.Elements;
using Cerneala.UI.Input;
using Cerneala.UI.Invalidation;
using Cerneala.UI.Layout;
using Cerneala.UI.Theming;

namespace Cerneala.Tests.Controls;

public sealed class ComponentTemplateLifecycleTests
{
    [Fact]
    public void TypedTemplateReceivesOwnerContextAndAttachesRetainedRoot()
    {
        Button button = new();
        Button? contextOwner = null;
        UIElement child = new();
        ComponentTemplate<Button> template = new("test", context =>
        {
            contextOwner = context.Owner;
            return child;
        });

        button.ComponentTemplate = template;

        Assert.Same(button, contextOwner);
        Assert.Same(child, button.ComponentTemplateInstance!.Root);
        Assert.Same(button, child.LogicalParent);
        Assert.Same(button, child.VisualParent);
        Assert.Contains(child, button.LogicalChildren);
        Assert.Contains(child, button.VisualChildren);
    }

    [Fact]
    public void TypedTemplateRejectsIncompatibleOwner()
    {
        ComponentTemplate<Button> template = new("test", _ => new UIElement());

        ContentControl incompatibleOwner = new();
        Assert.Throws<InvalidOperationException>(() => template.CreateInstance(
            incompatibleOwner,
            new ComponentTemplateContext(incompatibleOwner, new Cerneala.UI.Aspect.AspectEnvironment("test"))));
    }

    [Fact]
    public void TemplatedButtonContentPresenterHostsExistingElementContent()
    {
        Button button = new();
        UIElement child = new();
        ContentPresenter? presenter = null;
        button.Content = child;

        button.ComponentTemplate = new ComponentTemplate<Button>("test", context =>
        {
            presenter = new ContentPresenter { Content = context.Owner.Content };
            return presenter;
        });
        button.Measure(new MeasureContext(new LayoutSize(100, 100)));

        Assert.Same(child, presenter!.PresentedChild);
        Assert.Same(presenter, child.LogicalParent);
        Assert.Same(presenter, child.VisualParent);
        Assert.DoesNotContain(child, button.LogicalChildren);
        Assert.DoesNotContain(child, button.VisualChildren);
        Assert.Contains(presenter, button.LogicalChildren);
        Assert.Contains(presenter, button.VisualChildren);
    }

    [Fact]
    public void ApplyingSameTemplateReusesGeneratedRoot()
    {
        Control control = new();
        int created = 0;
        ComponentTemplate<Control> template = new("test", _ =>
        {
            created++;
            return new UIElement();
        });
        control.ComponentTemplate = template;
        UIElement root = control.ComponentTemplateInstance!.Root!;

        control.ApplyTemplate();
        control.Measure(new MeasureContext(new LayoutSize(100, 100)));

        Assert.Equal(1, created);
        Assert.Same(root, control.ComponentTemplateInstance!.Root);
    }

    [Fact]
    public void ReplacingTemplateDetachesOldRootAndAttachesNewRoot()
    {
        Control control = new();
        UIElement oldRoot = new();
        UIElement newRoot = new();
        control.ComponentTemplate = new ComponentTemplate<Control>("old", _ => oldRoot);

        control.ComponentTemplate = new ComponentTemplate<Control>("new", _ => newRoot);

        Assert.Null(oldRoot.LogicalParent);
        Assert.Null(oldRoot.VisualParent);
        Assert.Same(control, newRoot.LogicalParent);
        Assert.Same(control, newRoot.VisualParent);
    }

    [Fact]
    public void ReplacingTemplateDisposesRegisteredTemplateLifetimes()
    {
        Control control = new();
        TrackingDisposable lifetime = new();
        control.ComponentTemplate = new ComponentTemplate<Control>("old", context =>
        {
            context.RegisterLifetime(lifetime);
            return new UIElement();
        });

        control.ComponentTemplate = new ComponentTemplate<Control>("new", _ => new UIElement());

        Assert.Equal(1, lifetime.DisposeCount);
    }

    [Fact]
    public void FailedTemplateFactoryDisposesRegisteredTemplateLifetimes()
    {
        Control control = new();
        TrackingDisposable lifetime = new();
        ComponentTemplate<Control> template = new("broken", context =>
        {
            context.RegisterLifetime(lifetime);
            throw new InvalidOperationException("broken");
        });

        Assert.Throws<InvalidOperationException>(() => control.ComponentTemplate = template);

        Assert.Equal(1, lifetime.DisposeCount);
    }

    [Fact]
    public void FailedTemplateAttachDetachesGeneratedRoot()
    {
        Control control = new();
        RejectingBindingTarget child = new();
        ComponentTemplate<Control> template = new("test", context =>
        {
            context.Bind(Control.FontSizeProperty, child, RejectingBindingTarget.MinimumFontSizeProperty);
            return child;
        });

        Assert.Throws<ArgumentException>(() => control.ComponentTemplate = template);

        Assert.Null(control.ComponentTemplateInstance);
        Assert.Null(child.LogicalParent);
        Assert.Null(child.VisualParent);
        Assert.DoesNotContain(child, control.LogicalChildren);
        Assert.DoesNotContain(child, control.VisualChildren);
    }

    [Fact]
    public void TemplatePartMetadataIsDiagnosticOnly()
    {
        Control control = new()
        {
            ComponentTemplate = new ComponentTemplate<Control>("test", _ => new UIElement())
        };
        TemplatePartAttribute.Register<PartedControl>("PART_Content", typeof(ContentPresenter));

        IReadOnlyList<TemplatePartAttribute> parts = TemplatePartAttribute.GetParts(typeof(PartedControl));

        TemplatePartAttribute part = Assert.Single(parts);
        Assert.Equal("PART_Content", part.Name);
        Assert.Equal(typeof(ContentPresenter), part.Type);
        Assert.NotNull(control.ComponentTemplateInstance);
    }

    [Fact]
    public void TemplateReplacementQueuesRetainedInvalidationAndSameTemplateDoesNotDuplicateWork()
    {
        UIRoot root = new(40, 20);
        Control control = new();
        root.VisualChildren.Add(control);
        ComponentTemplate<Control> template = new("test", _ => new UIElement());

        control.ComponentTemplate = template;
        int measureCount = root.LayoutQueue.MeasureCount;
        int arrangeCount = root.LayoutQueue.ArrangeCount;
        int renderCount = root.RenderQueue.Count;
        int hitTestCount = root.HitTestQueue.Count;
        control.ComponentTemplate = template;

        Assert.Contains(control, root.LayoutQueue.SnapshotMeasure());
        Assert.Contains(control, root.LayoutQueue.SnapshotArrange());
        Assert.Contains(control, root.RenderQueue.Snapshot());
        Assert.Contains(control, root.HitTestQueue.Snapshot());
        Assert.Equal(measureCount, root.LayoutQueue.MeasureCount);
        Assert.Equal(arrangeCount, root.LayoutQueue.ArrangeCount);
        Assert.Equal(renderCount, root.RenderQueue.Count);
        Assert.Equal(hitTestCount, root.HitTestQueue.Count);
    }

    [Fact]
    public void TemplateRootAttachesToExistingRootAndKeepsStableIdentity()
    {
        UIRoot root = new(40, 20);
        Control control = new();
        root.VisualChildren.Add(control);
        LifecycleElement child = new();

        control.ComponentTemplate = new ComponentTemplate<Control>("test", _ => child);
        UiElementId id = child.ElementId!.Value;
        control.ApplyTemplate();

        Assert.Same(root, child.Root);
        Assert.Equal(1, child.AttachedCount);
        Assert.Equal(id, child.ElementId!.Value);
    }

    [Fact]
    public void TemplateChildParticipatesInLayoutRenderingHitTestingInputAndAspect()
    {
        UIRoot root = new(40, 20);
        Control control = new()
        {
            ComponentTemplate = new ComponentTemplate<Control>("test", _ => new RenderableElement(DrawColor.White))
        };
        root.VisualChildren.Add(control);
        control.Measure(new MeasureContext(new LayoutSize(100, 100)));
        control.Arrange(new ArrangeContext(new LayoutRect(0, 0, 40, 20)));
        RenderableElement child = Assert.IsType<RenderableElement>(control.ComponentTemplateInstance!.Root);
        root.Invalidate(InvalidationFlags.Render | InvalidationFlags.Subtree, "test");
        root.ProcessFrame();

        DrawCommandList commands = root.RetainedRenderer.Commit(root);
        HitTestResult? hit = new HitTestService().HitTest(root, 10, 10);
        bool routed = false;
        child.Handlers.AddHandler(InputEvents.MouseDownEvent, (_, _) => routed = true);
        ElementInputRouteMap map = new ElementInputRouteBuilder().Build(root);
        RoutedEventRouter.Raise(map.InputTree, child.ElementId!.Value, new MouseButtonEventArgs(InputEvents.MouseDownEvent, child.ElementId.Value, InputMouseButton.Left, 10, 10, 1));
        child.SetValue(UIElement.IsEnabledProperty, false, UiPropertyValueSource.AspectBase);

        Assert.Equal(new LayoutSize(20, 10), child.DesiredSize);
        Assert.Equal(new LayoutRect(0, 0, 40, 20), child.ArrangedBounds);
        Assert.Single(commands);
        Assert.Same(child, hit!.Element);
        Assert.True(routed);
        Assert.False(child.IsEnabled);
    }

    private sealed class LifecycleElement : UIElement
    {
        public int AttachedCount { get; private set; }

        protected override void OnAttached()
        {
            AttachedCount++;
        }
    }

    private sealed class TrackingDisposable : IDisposable
    {
        public int DisposeCount { get; private set; }

        public void Dispose()
        {
            DisposeCount++;
        }
    }

    private sealed class RejectingBindingTarget : UIElement
    {
        public static readonly UiProperty<float> MinimumFontSizeProperty = UiProperty<float>.Register(
            nameof(MinimumFontSize),
            typeof(RejectingBindingTarget),
            new UiPropertyMetadata<float>(128, validateValue: value => value >= 100));

        public float MinimumFontSize
        {
            get => GetValue(MinimumFontSizeProperty);
            set => SetValue(MinimumFontSizeProperty, value);
        }
    }

    private sealed class PartedControl : Control
    {
    }

    private sealed class RenderableElement(DrawColor color) : UIElement
    {
        protected override LayoutSize MeasureCore(MeasureContext context)
        {
            return new LayoutSize(20, 10);
        }

        protected override void OnRender(Cerneala.UI.Rendering.RenderContext context)
        {
            context.DrawingContext.FillRectangle(new DrawRect(context.Bounds.X, context.Bounds.Y, 1, 1), color);
        }
    }
}
