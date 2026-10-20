using Cerneala.UI.Controls;
using Cerneala.UI.Elements;
using Cerneala.UI.Layout;
using Cerneala.UI.Motion.Presence;
using Cerneala.Tests.UI.Motion.Core;
using MotionFactory = Cerneala.UI.Motion.Specs.Motion;

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
    public void ReplacingContentTemplateKeepsElementContentInTheContentPresenterThatTheTemplateReuses()
    {
        UIRoot root = new(200, 200);
        FixedElement content = new(new LayoutSize(10, 10));
        ContentPresenter cached = new();
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
        Assert.Same(content, cached.PresentedChild);
        Assert.Same(cached, content.VisualParent);
        Assert.Same(cached, content.LogicalParent);
        Assert.Single(cached.VisualChildren);
        Assert.Contains(content, cached.VisualChildren);
        Assert.Contains(content, cached.LogicalChildren);
    }

    [Fact]
    public void ReplacingContentTemplateReleasesElementContentThroughTheOldContentPresenterContent()
    {
        FixedElement content = new(new LayoutSize(10, 10));
        ContentPresenter presenter = new()
        {
            ContentTemplate = ContentPresenterTemplate("first"),
            Content = content
        };
        ContentPresenter oldContainer = Assert.IsType<ContentPresenter>(presenter.PresentedChild);
        Assert.Same(content, oldContainer.PresentedChild);

        presenter.ContentTemplate = ContentPresenterTemplate("second");

        ContentPresenter newContainer = Assert.IsType<ContentPresenter>(presenter.PresentedChild);
        Assert.NotSame(oldContainer, newContainer);
        Assert.Same(newContainer, content.VisualParent);
        Assert.Same(content, newContainer.PresentedChild);
        Assert.Null(oldContainer.Content);
        Assert.Null(oldContainer.PresentedChild);
        Assert.DoesNotContain(content, oldContainer.VisualChildren);
        Assert.DoesNotContain(content, oldContainer.LogicalChildren);
    }

    [Fact]
    public void ThrowingContentTemplateKeepsElementContentInsideThePreviousContentPresenter()
    {
        UIRoot root = new(200, 200);
        FixedElement content = new(new LayoutSize(10, 10));
        ContentPresenter presenter = new() { ContentTemplate = ContentPresenterTemplate("first"), Content = content };
        root.VisualChildren.Add(presenter);
        root.ProcessFrame();
        ContentPresenter oldContainer = Assert.IsType<ContentPresenter>(presenter.PresentedChild);

        Assert.Throws<InvalidOperationException>(() => presenter.ContentTemplate = ThrowingTemplate(takeContent: true));

        Assert.Same(oldContainer, presenter.PresentedChild);
        Assert.Same(content, oldContainer.Content);
        Assert.Same(content, oldContainer.PresentedChild);
        Assert.Same(oldContainer, content.VisualParent);
        Assert.Same(oldContainer, content.LogicalParent);
        Assert.Single(oldContainer.VisualChildren);
        Assert.Same(root, content.Root);
    }

    [Fact]
    public void ReplacingContentTemplateKeepsElementContentInTheTabItemHeaderThatTheTemplateReuses()
    {
        UIRoot root = new(200, 200);
        FixedElement content = new(new LayoutSize(10, 10));
        TabItem cached = new();
        Func<ContentTemplateContext, UIElement?> factory = context =>
        {
            cached.Header = context.Data;
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
        Assert.Same(content, cached.Header);
        Assert.Same(cached, content.VisualParent);
        Assert.Same(cached, content.LogicalParent);
        Assert.Same(root, content.Root);
        Assert.Single(cached.VisualChildren);
        Assert.Contains(content, cached.VisualChildren);
        Assert.Contains(content, cached.LogicalChildren);
    }

    [Fact]
    public void ReplacingContentTemplateReleasesElementContentThroughTheOldTabItemHeader()
    {
        FixedElement content = new(new LayoutSize(10, 10));
        ContentPresenter presenter = new()
        {
            ContentTemplate = TabItemHeaderTemplate("first"),
            Content = content
        };
        TabItem oldContainer = Assert.IsType<TabItem>(presenter.PresentedChild);
        Assert.Same(oldContainer, content.VisualParent);

        presenter.ContentTemplate = TabItemHeaderTemplate("second");

        TabItem newContainer = Assert.IsType<TabItem>(presenter.PresentedChild);
        Assert.NotSame(oldContainer, newContainer);
        Assert.Same(newContainer, content.VisualParent);
        Assert.Null(oldContainer.Header);
        Assert.DoesNotContain(content, oldContainer.VisualChildren);
        Assert.DoesNotContain(content, oldContainer.LogicalChildren);
    }

    [Fact]
    public void ThrowingContentTemplateKeepsElementContentInThePreviousTabItemHeader()
    {
        UIRoot root = new(200, 200);
        FixedElement content = new(new LayoutSize(10, 10));
        ContentPresenter presenter = new() { ContentTemplate = TabItemHeaderTemplate("first"), Content = content };
        root.VisualChildren.Add(presenter);
        root.ProcessFrame();
        TabItem oldContainer = Assert.IsType<TabItem>(presenter.PresentedChild);

        Assert.Throws<InvalidOperationException>(() => presenter.ContentTemplate = ThrowingTemplate(takeContent: true));

        Assert.Same(oldContainer, presenter.PresentedChild);
        Assert.Same(content, oldContainer.Header);
        Assert.Same(oldContainer, content.VisualParent);
        Assert.Same(oldContainer, content.LogicalParent);
        Assert.Single(oldContainer.VisualChildren);
        Assert.Same(root, content.Root);

        // Measured right after the failure: the next frame runs the failed refresh again (#531).
        oldContainer.Measure(new MeasureContext(new LayoutSize(200, 200)));
        Assert.Equal(new LayoutSize(10, 10), oldContainer.DesiredSize);
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

    [Fact]
    public void ThrowingContentTemplateGivesElementContentBackThroughThePreviousBorderChild()
    {
        UIRoot root = new(200, 200);
        Border content = new() { Width = 40, Height = 30 };
        ContentPresenter presenter = new() { ContentTemplate = WrapTemplate("wrap", key: null), Content = content };
        root.VisualChildren.Add(presenter);
        root.ProcessFrame();
        Border oldBorder = Assert.IsType<Border>(presenter.PresentedChild);

        Assert.Throws<InvalidOperationException>(() => presenter.ContentTemplate = ThrowingTemplate(takeContent: false));

        Assert.Same(oldBorder, presenter.PresentedChild);
        Assert.Same(content, oldBorder.Child);
        Assert.Same(content, Assert.Single(oldBorder.VisualChildren));
        content.Width = 80;
        oldBorder.Measure(new MeasureContext(new LayoutSize(200, 200)));
        Assert.Equal(new LayoutSize(80, 30), oldBorder.DesiredSize);
    }

    [Fact]
    public void ContentTemplateThatTakesElementContentAndThrowsGivesItBackThroughThePreviousBorderChild()
    {
        UIRoot root = new(200, 200);
        Border content = new() { Width = 40, Height = 30 };
        ContentPresenter presenter = new() { ContentTemplate = WrapTemplate("wrap", key: null), Content = content };
        root.VisualChildren.Add(presenter);
        root.ProcessFrame();
        Border oldBorder = Assert.IsType<Border>(presenter.PresentedChild);
        Border? failedBorder = null;
        ContentTemplate throwing = new("throws", typeof(object), key: null, priority: 0, context =>
        {
            failedBorder = new Border { Child = (UIElement?)context.Data };
            throw new InvalidOperationException("factory failed");
        });

        Assert.Throws<InvalidOperationException>(() => presenter.ContentTemplate = throwing);

        Assert.Same(content, oldBorder.Child);
        Assert.Same(oldBorder, content.VisualParent);
        Assert.Null(failedBorder!.Child);
        Assert.Empty(failedBorder.VisualChildren);
    }

    [Fact]
    public void TemplateChildThatCannotBePresentedNoLongerNamesElementContentAsChild()
    {
        UIRoot root = new(200, 200);
        Border content = new() { Width = 40, Height = 30 };
        ContentPresenter presenter = new() { Content = content };
        Border otherOwner = new();
        root.VisualChildren.Add(presenter);
        root.VisualChildren.Add(otherOwner);
        root.ProcessFrame();
        Border? wrapper = null;
        ContentTemplate parentedTemplate = new("parented", typeof(object), key: null, priority: 0, context =>
        {
            wrapper = new Border { Child = (UIElement?)context.Data };
            otherOwner.Child = wrapper;
            return wrapper;
        });

        Assert.Throws<InvalidOperationException>(() => presenter.ContentTemplate = parentedTemplate);

        Assert.Same(content, presenter.PresentedChild);
        Assert.Same(presenter, content.VisualParent);
        Assert.Null(wrapper!.Child);
        Assert.Empty(wrapper.VisualChildren);
        content.Width = 80;
        wrapper.Measure(new MeasureContext(new LayoutSize(200, 200)));
        Assert.Equal(LayoutSize.Zero, wrapper.DesiredSize);
    }

    [Fact]
    public void ThrowingContentTemplateGivesElementContentBackThroughThePreviousContentControlContent()
    {
        UIRoot root = new(200, 200);
        Border content = new() { Width = 40, Height = 30 };
        ContentPresenter presenter = new() { ContentTemplate = ContentControlTemplate("cc"), Content = content };
        root.VisualChildren.Add(presenter);
        root.ProcessFrame();
        ContentControl oldContainer = Assert.IsType<ContentControl>(presenter.PresentedChild);

        Assert.Throws<InvalidOperationException>(() => presenter.ContentTemplate = ThrowingTemplate(takeContent: false));

        Assert.Same(oldContainer, presenter.PresentedChild);
        Assert.Same(content, oldContainer.Content);
        Assert.Same(content, Assert.Single(oldContainer.VisualChildren));
        content.Width = 80;
        oldContainer.Measure(new MeasureContext(new LayoutSize(200, 200)));
        Assert.Equal(new LayoutSize(80, 30), oldContainer.DesiredSize);

        Border replacement = new() { Width = 5, Height = 5 };
        oldContainer.Content = replacement;
        Assert.Same(replacement, Assert.Single(oldContainer.VisualChildren));
        Assert.Null(content.VisualParent);
    }

    [Fact]
    public void ContentTemplateThatChangesThePreviousPanelAndThrowsGivesElementContentBackToThePanel()
    {
        UIRoot root = new(200, 200);
        Border content = new() { Width = 40, Height = 30 };
        Border sibling = new();
        StackPanel? oldPanel = null;
        ContentPresenter presenter = new()
        {
            ContentTemplate = new ContentTemplate("panel", typeof(object), key: null, priority: 0, context =>
            {
                if (context.Data is not UIElement element)
                {
                    return null;
                }

                oldPanel = new StackPanel();
                oldPanel.VisualChildren.Add(sibling);
                oldPanel.VisualChildren.Add(element);
                return oldPanel;
            }),
            Content = content
        };
        root.VisualChildren.Add(presenter);
        root.ProcessFrame();
        ContentTemplate throwing = new("throws", typeof(object), key: null, priority: 0, _ =>
        {
            oldPanel!.VisualChildren.Remove(sibling);
            throw new InvalidOperationException("factory failed");
        });

        InvalidOperationException thrown = Assert.Throws<InvalidOperationException>(() => presenter.ContentTemplate = throwing);

        Assert.Equal("factory failed", thrown.Message);
        Assert.Same(oldPanel, presenter.PresentedChild);
        Assert.Same(oldPanel, content.VisualParent);
        Assert.Same(content, Assert.Single(oldPanel!.VisualChildren));
        Assert.Same(root, content.Root);
    }

    [Fact]
    public void ThrowingContentTemplateGivesElementContentBackToThePreviousPanelInBothTrees()
    {
        UIRoot root = new(200, 200);
        Border content = new() { Width = 10, Height = 10 };
        Border sibling = new();
        ContentTemplate panelTemplate = new("panel", typeof(object), key: null, priority: 0, context =>
        {
            if (context.Data is not UIElement element)
            {
                return null;
            }

            StackPanel panel = new();
            panel.Children.Add(sibling);
            panel.VisualChildren.Add(element);
            panel.LogicalChildren.Add(element);
            return panel;
        });
        ContentPresenter presenter = new() { ContentTemplate = panelTemplate, Content = content };
        root.VisualChildren.Add(presenter);
        root.ProcessFrame();
        StackPanel oldPanel = Assert.IsType<StackPanel>(presenter.PresentedChild);

        InvalidOperationException thrown = Assert.Throws<InvalidOperationException>(
            () => presenter.ContentTemplate = ThrowingTemplate(takeContent: false));

        Assert.Equal("factory failed", thrown.Message);
        Assert.Same(oldPanel, presenter.PresentedChild);
        Assert.Same(oldPanel, content.VisualParent);
        Assert.Same(oldPanel, content.LogicalParent);
        Assert.Equal(1, oldPanel.VisualChildren.IndexOf(content));
        Assert.Equal(1, oldPanel.LogicalChildren.IndexOf(content));
        Assert.Same(root, content.Root);
    }

    [Fact]
    public void ThrowingContentTemplateKeepsTheGeneratedTextBlockGenerated()
    {
        UIRoot root = new(200, 200);
        ContentPresenter presenter = new() { Content = "hi" };
        root.VisualChildren.Add(presenter);
        root.ProcessFrame();
        TextBlock text = Assert.IsType<TextBlock>(presenter.PresentedChild);

        Assert.Throws<InvalidOperationException>(() => presenter.ContentTemplate = ThrowingTemplate(takeContent: false));
        Assert.Same(text, presenter.PresentedChild);

        Cerneala.UI.Media.SolidColorBrush foreground = new(Cerneala.Drawing.Color.White);
        presenter.FontSize = 37;
        presenter.FontFamily = "Mono";
        presenter.Foreground = foreground;
        Assert.Equal(37, text.FontSize);
        Assert.Equal("Mono", text.FontFamily);
        Assert.Same(foreground, text.Foreground);

        presenter.ContentTemplate = null;
        Assert.Same(text, presenter.PresentedChild);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void RefreshWhoseContentTemplateThrewIsRetriedOnTheNextFrame(bool invalidate)
    {
        UIRoot root = new(300, 200);
        bool fail = true;
        Border shown = new() { Width = 10, Height = 10 };
        ContentTemplate once = new("once", typeof(string), key: null, priority: 0,
            _ => fail ? throw new InvalidOperationException("boom") : shown);
        ContentPresenter presenter = new() { Content = "first" };
        root.VisualChildren.Add(presenter);
        root.ProcessFrame();
        TextBlock previous = Assert.IsType<TextBlock>(presenter.PresentedChild);

        Assert.Throws<InvalidOperationException>(() => presenter.ContentTemplate = once);
        Assert.Same(previous, presenter.PresentedChild);

        fail = false;
        if (invalidate)
        {
            presenter.Invalidate(
                Cerneala.UI.Invalidation.InvalidationFlags.Measure | Cerneala.UI.Invalidation.InvalidationFlags.Arrange,
                "test");
        }

        root.ProcessFrame();

        Assert.Same(shown, presenter.PresentedChild);
        Assert.Same(presenter, shown.VisualParent);
        Assert.Same(shown, Assert.Single(presenter.VisualChildren));
    }

    [Fact]
    public void RefreshWhoseContentTemplateKeepsThrowingThrowsAgainAndKeepsThePreviousPresentation()
    {
        UIRoot root = new(300, 200);
        ContentPresenter presenter = new() { Content = "first" };
        root.VisualChildren.Add(presenter);
        root.ProcessFrame();
        TextBlock previous = Assert.IsType<TextBlock>(presenter.PresentedChild);

        Assert.Throws<InvalidOperationException>(() => presenter.ContentTemplate = ThrowingTemplate(takeContent: false));

        for (int frame = 0; frame < 2; frame++)
        {
            InvalidOperationException thrown = Assert.Throws<InvalidOperationException>(() => root.ProcessFrame());
            Assert.Equal("factory failed", thrown.Message);
            Assert.Same(previous, presenter.PresentedChild);
            Assert.Same(previous, Assert.Single(presenter.VisualChildren));
        }
    }

    [Fact]
    public void ContentChangedByLoadedWhileAddingElementContentPresentsTheNewContent()
    {
        UIRoot root = new(200, 200);
        ContentPresenter presenter = new() { Content = "first" };
        root.VisualChildren.Add(presenter);
        root.ProcessFrame();
        FixedElement content = new(new LayoutSize(10, 10));
        SetContentOnFirstLoaded(content, presenter, "other");

        presenter.Content = content;
        root.ProcessFrame();

        Assert.Equal("other", presenter.Content);
        AssertPresentsOnlyText(presenter, "other");
        Assert.False(content.IsAttached);
        Assert.Null(content.VisualParent);
    }

    [Fact]
    public void ContentChangedByLoadedWhileAddingATemplateChildPresentsTheNewContent()
    {
        UIRoot root = new(200, 200);
        ContentPresenter presenter = new() { ContentTemplate = WrapOrTextTemplate("wrap"), Content = "first" };
        root.VisualChildren.Add(presenter);
        root.ProcessFrame();
        FixedElement content = new(new LayoutSize(10, 10));
        SetContentOnFirstLoaded(content, presenter, "other");

        presenter.Content = content;
        root.ProcessFrame();

        Assert.Equal("other", presenter.Content);
        AssertPresentsOnlyText(presenter, "other");
        Assert.False(content.IsAttached);
        Assert.Null(content.VisualParent);
        Assert.Null(content.LogicalParent);
    }

    // Issue #626: removing the visual child first and the logical child second canceled the exit at once.
    [Fact]
    public void ContentSetToNullKeepsTheExitOfADirectlyPresentedElementWithPresenceRunningUntilItEnds()
    {
        ManualMotionClock clock = new();
        UIRoot root = new(200, 200, motionClock: clock);
        ContentPresenter presenter = new();
        root.VisualChildren.Add(presenter);
        Border child = new() { Width = 50, Height = 20, Presence = CreatePresence() };
        presenter.Content = child;
        root.ProcessFrame();
        Assert.Same(presenter, child.VisualParent);

        presenter.Content = null;
        root.ProcessFrame();

        Assert.Null(presenter.PresentedChild);
        Assert.Equal(PresenceState.Exiting, root.Motion.Presence.GetState(child));
        Assert.Equal([child], root.Motion.Presence.GetExitingVisualChildren(presenter));
        Assert.True(child.IsAttached);
        Assert.Null(child.LogicalParent);
        Assert.Empty(presenter.VisualChildren);
        Assert.Empty(presenter.LogicalChildren);

        clock.Advance(TimeSpan.FromMilliseconds(120));
        root.ProcessFrame();

        Assert.Equal(PresenceState.Detached, root.Motion.Presence.GetState(child));
        Assert.Empty(root.Motion.Presence.GetExitingVisualChildren(presenter));
        Assert.False(child.IsAttached);
    }

    // Issue #626: a replaced directly presented element with presence exits while the new element becomes present.
    [Fact]
    public void ContentReplacedKeepsTheExitOfTheOldPresentedElementWithPresenceRunningAndTheNewElementIsPresent()
    {
        ManualMotionClock clock = new();
        UIRoot root = new(200, 200, motionClock: clock);
        ContentPresenter presenter = new();
        root.VisualChildren.Add(presenter);
        Border oldChild = new() { Width = 50, Height = 20, Presence = CreatePresence() };
        Border newChild = new() { Width = 50, Height = 20, Presence = CreatePresence() };
        presenter.Content = oldChild;
        root.ProcessFrame();

        presenter.Content = newChild;
        root.ProcessFrame();

        Assert.Same(newChild, presenter.PresentedChild);
        Assert.Equal(PresenceState.Exiting, root.Motion.Presence.GetState(oldChild));
        Assert.Equal([oldChild], root.Motion.Presence.GetExitingVisualChildren(presenter));
        Assert.True(oldChild.IsAttached);
        Assert.Equal(PresenceState.Present, root.Motion.Presence.GetState(newChild));
        Assert.Equal([newChild], presenter.VisualChildren);
        Assert.Equal([newChild], presenter.LogicalChildren);

        clock.Advance(TimeSpan.FromMilliseconds(120));
        root.ProcessFrame();

        Assert.Equal(PresenceState.Detached, root.Motion.Presence.GetState(oldChild));
        Assert.Empty(root.Motion.Presence.GetExitingVisualChildren(presenter));
        Assert.False(oldChild.IsAttached);
        Assert.Equal(PresenceState.Present, root.Motion.Presence.GetState(newChild));
    }

    // Issue #626: presenting the exiting element again cancels its exit and hosts it in both trees again.
    [Fact]
    public void ContentSetAgainWhileItsPresentedElementExitsCancelsTheExit()
    {
        ManualMotionClock clock = new();
        UIRoot root = new(200, 200, motionClock: clock);
        ContentPresenter presenter = new();
        root.VisualChildren.Add(presenter);
        Border child = new() { Width = 50, Height = 20, Presence = CreatePresence() };
        presenter.Content = child;
        root.ProcessFrame();
        presenter.Content = null;
        root.ProcessFrame();

        presenter.Content = child;
        root.ProcessFrame();

        Assert.Same(child, presenter.PresentedChild);
        Assert.Same(presenter, child.LogicalParent);
        Assert.Same(presenter, child.VisualParent);
        Assert.Equal(PresenceState.Present, root.Motion.Presence.GetState(child));
        Assert.Empty(root.Motion.Presence.GetExitingVisualChildren(presenter));
        Assert.True(child.IsAttached);
    }

    // Issue #626: a template that takes the directly presented element moves it at once; that move is not an exit.
    [Fact]
    public void TemplateThatTakesTheDirectlyPresentedElementWithPresenceDoesNotStartAnExit()
    {
        ManualMotionClock clock = new();
        UIRoot root = new(200, 200, motionClock: clock);
        ContentPresenter presenter = new();
        root.VisualChildren.Add(presenter);
        Border child = new() { Width = 50, Height = 20, Presence = CreatePresence() };
        presenter.Content = child;
        root.ProcessFrame();

        presenter.ContentTemplate = WrapTemplate("wrap", key: null);
        root.ProcessFrame();

        Border wrapper = Assert.IsType<Border>(presenter.PresentedChild);
        Assert.Same(wrapper, child.VisualParent);
        Assert.Same(wrapper, child.LogicalParent);
        Assert.NotEqual(PresenceState.Exiting, root.Motion.Presence.GetState(child));
        Assert.Empty(root.Motion.Presence.GetExitingVisualChildren(presenter));
    }

    // PR #652 review: a nested presenter that presents the element directly lets go of it for the next template; that
    // release is a move, not an exit, so the next template can add the element to the element it creates.
    [Fact]
    public void ElementWithPresenceReleasedByANestedPresenterForTheNextTemplateMovesWithoutAnExit()
    {
        ManualMotionClock clock = new();
        UIRoot root = new(200, 200, motionClock: clock);
        Border child = new() { Width = 50, Height = 20, Presence = CreatePresence() };
        ContentPresenter presenter = new() { Content = child, ContentTemplate = ContentPresenterTemplate("inner") };
        root.VisualChildren.Add(presenter);
        root.ProcessFrame();
        ContentPresenter inner = Assert.IsType<ContentPresenter>(child.VisualParent);

        presenter.ContentTemplate = WrapTemplate("wrap", key: null);
        root.ProcessFrame();

        Border wrapper = Assert.IsType<Border>(presenter.PresentedChild);
        Assert.Same(wrapper, child.VisualParent);
        Assert.Same(wrapper, child.LogicalParent);
        Assert.Null(inner.Content);
        Assert.Null(inner.PresentedChild);
        Assert.Equal(PresenceState.Present, root.Motion.Presence.GetState(child));
        Assert.Empty(root.Motion.Presence.GetExitingVisualChildren(inner));
    }

    // Issue #664: the Border of the previous template lets go of the element for the next template; that release is a
    // move, not an exit, so the next template can add the element to the Border it creates.
    [Fact]
    public void ElementWithPresenceReleasedByADecoratorForTheNextTemplateMovesWithoutAnExit()
    {
        ManualMotionClock clock = new();
        UIRoot root = new(200, 200, motionClock: clock);
        Border child = new() { Width = 50, Height = 20, Presence = CreatePresence() };
        ContentPresenter presenter = new() { Content = child, ContentTemplate = WrapTemplate("wrap1", key: null) };
        root.VisualChildren.Add(presenter);
        root.ProcessFrame();
        Border first = Assert.IsType<Border>(child.VisualParent);

        presenter.ContentTemplate = WrapTemplate("wrap2", key: null);
        root.ProcessFrame();

        Border wrapper = Assert.IsType<Border>(presenter.PresentedChild);
        Assert.NotSame(first, wrapper);
        Assert.Same(wrapper, child.VisualParent);
        Assert.Same(wrapper, child.LogicalParent);
        Assert.Null(first.Child);
        Assert.Equal(PresenceState.Present, root.Motion.Presence.GetState(child));
        Assert.Empty(root.Motion.Presence.GetExitingVisualChildren(first));
    }

    // Issue #626: an element whose presentation a Loaded handler supersedes never became the presented child, so it
    // leaves at once, without an exit.
    [Fact]
    public void ElementContentSupersededByLoadedWhileItIsAddedLeavesWithoutAnExit()
    {
        ManualMotionClock clock = new();
        UIRoot root = new(200, 200, motionClock: clock);
        ContentPresenter presenter = new() { Content = "first" };
        root.VisualChildren.Add(presenter);
        root.ProcessFrame();
        Border content = new() { Width = 50, Height = 20, Presence = CreatePresence() };
        SetContentOnFirstLoaded(content, presenter, "other");

        presenter.Content = content;
        root.ProcessFrame();

        AssertPresentsOnlyText(presenter, "other");
        Assert.Equal(PresenceState.Detached, root.Motion.Presence.GetState(content));
        Assert.Empty(root.Motion.Presence.GetExitingVisualChildren(presenter));
        Assert.False(content.IsAttached);
    }

    private static PresenceOptions CreatePresence()
    {
        return PresenceOptions.FadeAndScale(
            MotionFactory.Tween<float>(TimeSpan.FromMilliseconds(100)),
            MotionFactory.Tween<float>(TimeSpan.FromMilliseconds(100)));
    }

    private static void SetContentOnFirstLoaded(UIElement element, ContentPresenter presenter, object newContent)
    {
        bool changed = false;
        element.Loaded += (_, _) =>
        {
            if (!changed)
            {
                changed = true;
                presenter.Content = newContent;
            }
        };
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

    private static ContentTemplate ContentPresenterTemplate(string name)
    {
        return new ContentTemplate(name, typeof(object), key: null, priority: 0, context => new ContentPresenter { Content = context.Data });
    }

    private static ContentTemplate TabItemHeaderTemplate(string name)
    {
        return new ContentTemplate(name, typeof(object), key: null, priority: 0, context => new TabItem { Header = context.Data });
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
