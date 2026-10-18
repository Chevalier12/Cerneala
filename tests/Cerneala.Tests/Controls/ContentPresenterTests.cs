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

    [Fact]
    public void ReplacingContentTemplateWrapsElementContentInTheNewTemplate()
    {
        UIRoot root = new(200, 200);
        FixedElement content = new(new LayoutSize(10, 10));
        ContentPresenter presenter = new() { ContentTemplate = WrapTemplate("wrap", key: null), Content = content };
        root.VisualChildren.Add(presenter);
        root.ProcessFrame();
        Border oldBorder = Assert.IsType<Border>(content.VisualParent);

        presenter.ContentTemplate = WrapTemplate("wrap", key: null);
        root.ProcessFrame();

        Border newBorder = Assert.IsType<Border>(presenter.PresentedChild);
        Assert.NotSame(oldBorder, newBorder);
        Assert.Same(newBorder, content.VisualParent);
        Assert.Same(newBorder, content.LogicalParent);
        Assert.DoesNotContain(content, oldBorder.VisualChildren);
        Assert.DoesNotContain(content, oldBorder.LogicalChildren);
        Assert.Null(oldBorder.Child);
    }

    [Fact]
    public void ChangingContentTemplateKeyWrapsElementContentInTheResolvedTemplate()
    {
        ContentTemplateRegistry registry = new();
        registry.Register(WrapTemplate("first", "first"));
        registry.Register(WrapTemplate("second", "second"));
        FixedElement content = new(new LayoutSize(10, 10));
        ContentPresenter presenter = new()
        {
            LocalTemplateRegistry = registry,
            ContentTemplateKey = "first",
            Content = content
        };
        Border oldBorder = Assert.IsType<Border>(presenter.PresentedChild);
        Assert.Same(oldBorder, content.VisualParent);

        presenter.ContentTemplateKey = "second";

        Border newBorder = Assert.IsType<Border>(presenter.PresentedChild);
        Assert.NotSame(oldBorder, newBorder);
        Assert.Same(newBorder, content.VisualParent);
        Assert.DoesNotContain(content, oldBorder.VisualChildren);
    }

    [Fact]
    public void ReplacingLocalTemplateRegistryWrapsElementContentInTheNewTemplate()
    {
        ContentTemplateRegistry oldRegistry = new();
        oldRegistry.Register(WrapTemplate("old", key: null));
        ContentTemplateRegistry newRegistry = new();
        newRegistry.Register(WrapTemplate("new", key: null));
        FixedElement content = new(new LayoutSize(10, 10));
        ContentPresenter presenter = new() { LocalTemplateRegistry = oldRegistry, Content = content };
        Border oldBorder = Assert.IsType<Border>(presenter.PresentedChild);

        presenter.LocalTemplateRegistry = newRegistry;

        Border newBorder = Assert.IsType<Border>(presenter.PresentedChild);
        Assert.NotSame(oldBorder, newBorder);
        Assert.Same(newBorder, content.VisualParent);
        Assert.DoesNotContain(content, oldBorder.VisualChildren);
    }

    [Fact]
    public void RemovingContentTemplateHostsElementContentThatTheTemplateWrapped()
    {
        FixedElement content = new(new LayoutSize(10, 10));
        ContentPresenter presenter = new() { ContentTemplate = WrapTemplate("wrap", key: null), Content = content };
        Border oldBorder = Assert.IsType<Border>(presenter.PresentedChild);

        presenter.ContentTemplate = null;

        Assert.Same(content, presenter.PresentedChild);
        Assert.Same(presenter, content.VisualParent);
        Assert.Same(presenter, content.LogicalParent);
        Assert.DoesNotContain(content, oldBorder.VisualChildren);
    }

    [Fact]
    public void AddingContentTemplateWrapsElementContentThatWasHostedDirectly()
    {
        FixedElement content = new(new LayoutSize(10, 10));
        ContentPresenter presenter = new() { Content = content };
        Assert.Same(content, presenter.PresentedChild);

        presenter.ContentTemplate = WrapTemplate("wrap", key: null);

        Border border = Assert.IsType<Border>(presenter.PresentedChild);
        Assert.Same(border, content.VisualParent);
    }

    [Fact]
    public void ThrowingContentTemplateKeepsElementContentHostedDirectly()
    {
        UIRoot root = new(200, 200);
        FixedElement content = new(new LayoutSize(10, 10));
        ContentPresenter presenter = new() { Content = content };
        root.VisualChildren.Add(presenter);
        root.ProcessFrame();

        Assert.Throws<InvalidOperationException>(() => presenter.ContentTemplate = ThrowingTemplate(takeContent: false));

        Assert.Same(content, presenter.PresentedChild);
        Assert.Same(presenter, content.VisualParent);
        Assert.Same(presenter, content.LogicalParent);
        Assert.Same(root, content.Root);
    }

    [Fact]
    public void ThrowingContentTemplateKeepsElementContentInsideThePreviousTemplate()
    {
        UIRoot root = new(200, 200);
        FixedElement content = new(new LayoutSize(10, 10));
        ContentPresenter presenter = new() { ContentTemplate = WrapTemplate("wrap", key: null), Content = content };
        root.VisualChildren.Add(presenter);
        root.ProcessFrame();
        Border oldBorder = Assert.IsType<Border>(presenter.PresentedChild);

        Assert.Throws<InvalidOperationException>(() => presenter.ContentTemplate = ThrowingTemplate(takeContent: false));

        Assert.Same(oldBorder, presenter.PresentedChild);
        Assert.Same(oldBorder, content.VisualParent);
        Assert.Same(oldBorder, content.LogicalParent);
        Assert.Contains(content, oldBorder.VisualChildren);
        Assert.Same(root, content.Root);
    }

    [Fact]
    public void ContentTemplateThatTakesElementContentAndThrowsKeepsThePreviousTemplate()
    {
        UIRoot root = new(200, 200);
        FixedElement content = new(new LayoutSize(10, 10));
        ContentPresenter presenter = new() { ContentTemplate = WrapTemplate("wrap", key: null), Content = content };
        root.VisualChildren.Add(presenter);
        root.ProcessFrame();
        Border oldBorder = Assert.IsType<Border>(presenter.PresentedChild);

        Assert.Throws<InvalidOperationException>(() => presenter.ContentTemplate = ThrowingTemplate(takeContent: true));

        Assert.Same(oldBorder, presenter.PresentedChild);
        Assert.Same(oldBorder, content.VisualParent);
        Assert.Same(oldBorder, content.LogicalParent);
        Assert.Same(root, content.Root);
    }

    [Fact]
    public void TemplateChildThatCannotBePresentedKeepsElementContentHostedDirectly()
    {
        UIRoot root = new(200, 200);
        FixedElement content = new(new LayoutSize(10, 10));
        ContentPresenter presenter = new() { Content = content };
        root.VisualChildren.Add(presenter);
        root.ProcessFrame();
        Border otherOwner = new();
        ContentTemplate parentedTemplate = new("parented", typeof(object), key: null, priority: 0, context =>
        {
            Border wrapper = new() { Child = (UIElement?)context.Data };
            otherOwner.Child = wrapper;
            return wrapper;
        });

        Assert.Throws<InvalidOperationException>(() => presenter.ContentTemplate = parentedTemplate);

        Assert.Same(content, presenter.PresentedChild);
        Assert.Same(presenter, content.VisualParent);
        Assert.Same(presenter, content.LogicalParent);
        Assert.Same(root, content.Root);
    }

    [Fact]
    public void ReplacingContentTemplateKeepsElementContentInTheContainerThatTheTemplateReuses()
    {
        UIRoot root = new(200, 200);
        FixedElement content = new(new LayoutSize(10, 10));
        Border cached = new();
        Func<ContentTemplateContext, UIElement?> factory = context =>
        {
            cached.Child = (UIElement?)context.Data;
            return cached;
        };
        ContentPresenter presenter = new()
        {
            ContentTemplate = new ContentTemplate("cache1", typeof(object), key: null, priority: 0, factory),
            Content = content
        };
        root.VisualChildren.Add(presenter);
        root.ProcessFrame();
        Assert.Same(cached, content.VisualParent);

        presenter.ContentTemplate = new ContentTemplate("cache2", typeof(object), key: null, priority: 0, factory);
        root.ProcessFrame();

        Assert.Same(cached, presenter.PresentedChild);
        Assert.Same(content, cached.Child);
        Assert.Same(cached, content.VisualParent);
        Assert.Same(cached, content.LogicalParent);
        Assert.Contains(content, cached.VisualChildren);
        Assert.Contains(content, cached.LogicalChildren);
    }

    [Fact]
    public void ReplacingContentTemplateKeepsElementContentInTheContentControlThatTheTemplateReuses()
    {
        UIRoot root = new(200, 200);
        FixedElement content = new(new LayoutSize(10, 10));
        ContentControl cached = new();
        Func<ContentTemplateContext, UIElement?> factory = context =>
        {
            cached.Content = context.Data;
            return cached;
        };
        ContentPresenter presenter = new()
        {
            ContentTemplate = new ContentTemplate("cache1", typeof(object), key: null, priority: 0, factory),
            Content = content
        };
        root.VisualChildren.Add(presenter);
        root.ProcessFrame();
        Assert.Same(cached, content.VisualParent);

        presenter.ContentTemplate = new ContentTemplate("cache2", typeof(object), key: null, priority: 0, factory);
        root.ProcessFrame();

        Assert.Same(cached, presenter.PresentedChild);
        Assert.Same(content, cached.Content);
        Assert.Same(cached, content.VisualParent);
        Assert.Same(cached, content.LogicalParent);
        Assert.Contains(content, cached.VisualChildren);
        Assert.Contains(content, cached.LogicalChildren);
    }

    [Fact]
    public void ReplacingContentTemplateReleasesElementContentThroughTheOldContentControlContent()
    {
        FixedElement content = new(new LayoutSize(10, 10));
        ContentPresenter presenter = new()
        {
            ContentTemplate = ContentControlTemplate("first"),
            Content = content
        };
        ContentControl oldContainer = Assert.IsType<ContentControl>(presenter.PresentedChild);
        Assert.Same(oldContainer, content.VisualParent);

        presenter.ContentTemplate = ContentControlTemplate("second");

        ContentControl newContainer = Assert.IsType<ContentControl>(presenter.PresentedChild);
        Assert.NotSame(oldContainer, newContainer);
        Assert.Same(newContainer, content.VisualParent);
        Assert.Null(oldContainer.Content);
        Assert.DoesNotContain(content, oldContainer.VisualChildren);
        Assert.DoesNotContain(content, oldContainer.LogicalChildren);
    }

    [Fact]
    public void ReplacingPassThroughContentTemplateKeepsElementContentInPlace()
    {
        UIRoot root = new(200, 200);
        FixedElement content = new(new LayoutSize(10, 10));
        int loaded = 0;
        int unloaded = 0;
        content.Loaded += (_, _) => loaded++;
        content.Unloaded += (_, _) => unloaded++;
        ContentPresenter presenter = new() { ContentTemplate = PassThroughTemplate("first"), Content = content };
        root.VisualChildren.Add(presenter);
        root.ProcessFrame();
        Assert.Same(content, presenter.PresentedChild);

        presenter.ContentTemplate = PassThroughTemplate("second");
        root.ProcessFrame();

        Assert.Same(content, presenter.PresentedChild);
        Assert.Same(presenter, content.VisualParent);
        Assert.Same(presenter, content.LogicalParent);
        Assert.Equal(1, loaded);
        Assert.Equal(0, unloaded);
    }

    [Fact]
    public void ReplacingPassThroughContentTemplateWithPanelTemplateMovesElementContentIntoThePanel()
    {
        UIRoot root = new(200, 200);
        FixedElement content = new(new LayoutSize(10, 10));
        ContentPresenter presenter = new() { ContentTemplate = PassThroughTemplate("first"), Content = content };
        root.VisualChildren.Add(presenter);
        root.ProcessFrame();

        presenter.ContentTemplate = new ContentTemplate("panel", typeof(object), key: null, priority: 0, context =>
        {
            StackPanel panel = new();
            panel.VisualChildren.Add((UIElement)context.Data!);
            return panel;
        });
        root.ProcessFrame();

        StackPanel panel = Assert.IsType<StackPanel>(presenter.PresentedChild);
        Assert.Same(panel, content.VisualParent);
        Assert.Null(content.LogicalParent);
        Assert.DoesNotContain(content, presenter.VisualChildren);
        Assert.DoesNotContain(content, presenter.LogicalChildren);
        Assert.Same(root, content.Root);
    }

    [Fact]
    public void ContentChangedByUnloadedWhileTemplateTakesDirectlyHostedElementPresentsTheNewContent()
    {
        UIRoot root = new(200, 200);
        FixedElement content = new(new LayoutSize(10, 10));
        ContentPresenter presenter = new() { Content = content };
        root.VisualChildren.Add(presenter);
        root.ProcessFrame();
        SetContentOnFirstUnloaded(content, presenter, "other");

        presenter.ContentTemplate = WrapOrTextTemplate("wrap");
        root.ProcessFrame();

        AssertPresentsOnlyText(presenter, "other");
        Assert.False(content.IsAttached);
        Assert.Null(content.VisualParent);
        Assert.Null(content.LogicalParent);
    }

    [Fact]
    public void ContentChangedByUnloadedWhileReleasingTemplateWrappedElementPresentsTheNewContent()
    {
        UIRoot root = new(200, 200);
        FixedElement content = new(new LayoutSize(10, 10));
        ContentPresenter presenter = new() { ContentTemplate = WrapOrTextTemplate("first"), Content = content };
        root.VisualChildren.Add(presenter);
        root.ProcessFrame();
        Assert.IsType<Border>(presenter.PresentedChild);
        SetContentOnFirstUnloaded(content, presenter, "other");

        presenter.ContentTemplate = WrapOrTextTemplate("second");
        root.ProcessFrame();

        AssertPresentsOnlyText(presenter, "other");
        Assert.False(content.IsAttached);
        Assert.Null(content.VisualParent);
        Assert.Null(content.LogicalParent);
    }

    [Fact]
    public void ContentChangedByUnloadedOfThePreviousPresentedChildPresentsTheNewContent()
    {
        UIRoot root = new(200, 200);
        FixedElement previous = new(new LayoutSize(10, 10));
        ContentPresenter presenter = new()
        {
            ContentTemplate = new ContentTemplate("first", typeof(object), key: null, priority: 0, _ => previous),
            Content = "first"
        };
        root.VisualChildren.Add(presenter);
        root.ProcessFrame();
        Assert.Same(previous, presenter.PresentedChild);
        SetContentOnFirstUnloaded(previous, presenter, "other");

        presenter.ContentTemplate = WrapOrTextTemplate("second");
        root.ProcessFrame();

        AssertPresentsOnlyText(presenter, "other");
        Assert.False(previous.IsAttached);
    }

    [Fact]
    public void ContentChangedByUnloadedWhileContentReplacesDirectlyHostedElementPresentsTheLastContent()
    {
        UIRoot root = new(200, 200);
        FixedElement content = new(new LayoutSize(10, 10));
        ContentPresenter presenter = new() { Content = content };
        root.VisualChildren.Add(presenter);
        root.ProcessFrame();
        SetContentOnFirstUnloaded(content, presenter, "other");

        presenter.Content = "first";
        root.ProcessFrame();

        Assert.Equal("other", presenter.Content);
        AssertPresentsOnlyText(presenter, "other");
        Assert.False(content.IsAttached);
    }

    private static void SetContentOnFirstUnloaded(UIElement element, ContentPresenter presenter, object newContent)
    {
        bool changed = false;
        element.Unloaded += (_, _) =>
        {
            if (!changed)
            {
                changed = true;
                presenter.Content = newContent;
            }
        };
    }

    private static void AssertPresentsOnlyText(ContentPresenter presenter, string text)
    {
        TextBlock textBlock = Assert.IsType<TextBlock>(presenter.PresentedChild);
        Assert.Equal(text, textBlock.Text);
        Assert.Same(textBlock, Assert.Single(presenter.VisualChildren));
        Assert.Same(textBlock, Assert.Single(presenter.LogicalChildren));
        Assert.True(textBlock.IsAttached);
    }

    private static ContentTemplate WrapOrTextTemplate(string name)
    {
        return new ContentTemplate(name, typeof(object), key: null, priority: 0, context =>
            context.Data is UIElement element ? new Border { Child = element } : new TextBlock { Text = context.Data?.ToString() });
    }

    private static ContentTemplate PassThroughTemplate(string name)
    {
        return new ContentTemplate(name, typeof(object), key: null, priority: 0, context => context.Data as UIElement);
    }

    private static ContentTemplate WrapTemplate(string name, string? key)
    {
        return new ContentTemplate(name, typeof(object), key, priority: 0, context => new Border { Child = (UIElement?)context.Data });
    }

    private static ContentTemplate ContentControlTemplate(string name)
    {
        return new ContentTemplate(name, typeof(object), key: null, priority: 0, context => new ContentControl { Content = context.Data });
    }

    private static ContentTemplate ThrowingTemplate(bool takeContent)
    {
        return new ContentTemplate("throws", typeof(object), key: null, priority: 0, context =>
        {
            if (takeContent)
            {
                _ = new Border { Child = (UIElement?)context.Data };
            }

            throw new InvalidOperationException("factory failed");
        });
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
