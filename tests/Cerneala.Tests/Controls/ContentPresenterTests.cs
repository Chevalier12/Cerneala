using Cerneala.UI.Controls;
using Cerneala.UI.Elements;
using Cerneala.UI.Layout;

namespace Cerneala.Tests.Controls;

public sealed class ContentPresenterTests
{
    [Fact]
    public void UiElementContentIsHostedDirectly()
    {
        ContentPresenter presenter = new();
        UIElement child = new();

        presenter.Content = child;

        Assert.Same(child, presenter.PresentedChild);
        Assert.Same(presenter, child.LogicalParent);
        Assert.Same(presenter, child.VisualParent);
    }

    [Fact]
    public void ContentTemplateCreatesPresentedChildForNonElementContent()
    {
        ContentPresenter presenter = new()
        {
            Content = "hello",
            ContentTemplate = new ContentTemplate<string>("test", key: null, priority: 0, _ => new FixedElement(new LayoutSize(10, 5)))
        };

        presenter.Measure(new MeasureContext(new LayoutSize(100, 100)));

        FixedElement child = Assert.IsType<FixedElement>(presenter.PresentedChild);
        Assert.Same(presenter, child.LogicalParent);
        Assert.Equal(new LayoutSize(10, 5), presenter.DesiredSize);
    }

    [Fact]
    public void ContentTemplateCreatesPresentedChildForElementContent()
    {
        UIElement content = new();
        ContentPresenter presenter = new()
        {
            Content = content,
            ContentTemplate = new ContentTemplate<UIElement>("test", key: null, priority: 0, _ => new FixedElement(new LayoutSize(10, 5)))
        };

        presenter.Measure(new MeasureContext(new LayoutSize(100, 100)));

        FixedElement child = Assert.IsType<FixedElement>(presenter.PresentedChild);
        Assert.Same(presenter, child.LogicalParent);
        Assert.Null(content.LogicalParent);
        Assert.Null(content.VisualParent);
    }

    [Fact]
    public void ContentReplacementDetachesOldPresentedChild()
    {
        ContentPresenter presenter = new();
        UIElement oldChild = new();
        UIElement newChild = new();
        presenter.Content = oldChild;

        presenter.Content = newChild;

        Assert.Null(oldChild.LogicalParent);
        Assert.Null(oldChild.VisualParent);
        Assert.Same(presenter, newChild.LogicalParent);
        Assert.Same(presenter, newChild.VisualParent);
    }

    [Fact]
    public void ContentTemplateOutputIsRetainedAcrossMeasurePasses()
    {
        int created = 0;
        ContentPresenter presenter = new()
        {
            Content = "hello",
            ContentTemplate = new ContentTemplate<string>("test", key: null, priority: 0, _ =>
            {
                created++;
                return new FixedElement(new LayoutSize(10, 5));
            })
        };

        presenter.Measure(new MeasureContext(new LayoutSize(100, 100)));
        UIElement child = presenter.PresentedChild!;
        presenter.Measure(new MeasureContext(new LayoutSize(100, 100)));

        Assert.Equal(1, created);
        Assert.Same(child, presenter.PresentedChild);
    }

    [Fact]
    public void ValueEqualContentReplacementRematerializesPresentedChild()
    {
        EqualContent oldContent = new("same");
        EqualContent newContent = new("same");
        ContentPresenter presenter = new()
        {
            Content = oldContent,
            ContentTemplate = new ContentTemplate<EqualContent>("test", key: null, priority: 0, context => new ContentElement(context.Data!))
        };
        presenter.Measure(new MeasureContext(new LayoutSize(100, 100)));
        ContentElement oldChild = Assert.IsType<ContentElement>(presenter.PresentedChild);

        presenter.Content = newContent;

        ContentElement newChild = Assert.IsType<ContentElement>(presenter.PresentedChild);
        Assert.NotSame(oldChild, newChild);
        Assert.Same(newContent, newChild.Value);
        Assert.Null(oldChild.LogicalParent);
        Assert.Null(oldChild.VisualParent);
    }

    private sealed record EqualContent(string Value);

    private sealed class ContentElement(object value) : UIElement
    {
        public object Value { get; } = value;
    }

    private sealed class FixedElement(LayoutSize size) : UIElement
    {
        protected override LayoutSize MeasureCore(MeasureContext context)
        {
            return size;
        }
    }
}
