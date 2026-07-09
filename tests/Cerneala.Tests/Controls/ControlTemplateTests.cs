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

public sealed class ControlTemplateTests
{
    [Fact]
    public void TypedTemplateReceivesOwnerContextAndAttachesRetainedRoot()
    {
        Button button = new();
        Button? contextOwner = null;
        UIElement child = new();
        ControlTemplate<Button> template = new(context =>
        {
            contextOwner = context.Owner;
            return child;
        });

        button.Template = template;

        Assert.Same(button, contextOwner);
        Assert.Same(child, button.TemplateInstance!.Root);
        Assert.Same(button, child.LogicalParent);
        Assert.Same(button, child.VisualParent);
        Assert.Contains(child, button.LogicalChildren);
        Assert.Contains(child, button.VisualChildren);
    }

    [Fact]
    public void TypedTemplateRejectsIncompatibleOwner()
    {
        ControlTemplate<Button> template = new(_ => new UIElement());

        Assert.Throws<InvalidOperationException>(() => template.CreateInstance(new ContentControl()));
    }

    [Fact]
    public void TemplatedButtonContentPresenterHostsExistingElementContent()
    {
        Button button = new();
        UIElement child = new();
        ContentPresenter? presenter = null;
        button.Content = child;

        button.Template = new ControlTemplate<Button>(context =>
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
        ControlTemplate<Control> template = new(_ =>
        {
            created++;
            return new UIElement();
        });
        control.Template = template;
        UIElement root = control.TemplateInstance!.Root!;

        control.ApplyTemplate();
        control.Measure(new MeasureContext(new LayoutSize(100, 100)));

        Assert.Equal(1, created);
        Assert.Same(root, control.TemplateInstance!.Root);
    }

    [Fact]
    public void ReplacingTemplateDetachesOldRootAndAttachesNewRoot()
    {
        Control control = new();
        UIElement oldRoot = new();
        UIElement newRoot = new();
        control.Template = new ControlTemplate<Control>(_ => oldRoot);

        control.Template = new ControlTemplate<Control>(_ => newRoot);

        Assert.Null(oldRoot.LogicalParent);
        Assert.Null(oldRoot.VisualParent);
        Assert.Same(control, newRoot.LogicalParent);
        Assert.Same(control, newRoot.VisualParent);
    }

    [Fact]
    public void FailedTemplateAttachDetachesGeneratedRoot()
    {
        Control control = new();
        RejectingBindingTarget child = new();
        ControlTemplate<Control> template = new(context =>
        {
            context.Bind(Control.FontSizeProperty, child, RejectingBindingTarget.MinimumFontSizeProperty);
            return child;
        });

        Assert.Throws<ArgumentException>(() => control.Template = template);

        Assert.Null(control.TemplateInstance);
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
            Template = new ControlTemplate<Control>(_ => new UIElement())
        };

        IReadOnlyList<TemplatePartAttribute> parts = TemplatePartAttribute.GetParts(typeof(PartedControl));

        TemplatePartAttribute part = Assert.Single(parts);
        Assert.Equal("PART_Content", part.Name);
        Assert.Equal(typeof(ContentPresenter), part.Type);
        Assert.NotNull(control.TemplateInstance);
    }

    [Fact]
    public void TemplateReplacementQueuesRetainedInvalidationAndSameTemplateDoesNotDuplicateWork()
    {
        UIRoot root = new(40, 20);
        Control control = new();
        root.VisualChildren.Add(control);
        ControlTemplate<Control> template = new(_ => new UIElement());

        control.Template = template;
        int measureCount = root.LayoutQueue.MeasureCount;
        int arrangeCount = root.LayoutQueue.ArrangeCount;
        int renderCount = root.RenderQueue.Count;
        int hitTestCount = root.HitTestQueue.Count;
        control.Template = template;

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

        control.Template = new ControlTemplate<Control>(_ => child);
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
            Template = new ControlTemplate<Control>(_ => new RenderableElement(DrawColor.White))
        };
        root.VisualChildren.Add(control);
        control.Measure(new MeasureContext(new LayoutSize(100, 100)));
        control.Arrange(new ArrangeContext(new LayoutRect(0, 0, 40, 20)));
        RenderableElement child = Assert.IsType<RenderableElement>(control.TemplateInstance!.Root);
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

    [TemplatePart("PART_Content", typeof(ContentPresenter))]
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
