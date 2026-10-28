using Cerneala.Drawing;
using Cerneala.UI.Controls;
using Cerneala.UI.Controls.Templates;
using Cerneala.UI.Elements;
using Cerneala.UI.Invalidation;
using Cerneala.UI.Layout;

namespace Cerneala.Tests.Controls;

// As in WPF, content that is not an element and that no template handles is shown as the text of its ToString(), by
// every ContentPresenter and by the default presentation of an untemplated ContentControl (#1157).
public sealed class NonElementContentTextTests
{
    public static TheoryData<object, string> NonStringContent => new()
    {
        { 5, "5" },
        { DayOfWeek.Monday, "Monday" },
        { new Person("Ana"), "Person { Name = Ana }" }
    };

    [Theory]
    [MemberData(nameof(NonStringContent))]
    public void ContentPresenterShowsTheTextOfContentThatIsNotAStringOrAnElement(object content, string expected)
    {
        UIRoot root = new(200, 200);
        ContentPresenter presenter = new() { Content = content };
        root.VisualChildren.Add(presenter);

        root.ProcessFrame();

        TextBlock text = Assert.IsType<TextBlock>(presenter.PresentedChild);
        Assert.Equal(expected, text.Text);
        Assert.True(presenter.DesiredSize.Width > 0);
    }

    [Fact]
    public void ContentPresenterReusesItsGeneratedTextBlockWhenTheContentChangesToAnotherValue()
    {
        UIRoot root = new(200, 200);
        ContentPresenter presenter = new() { Content = 5 };
        root.VisualChildren.Add(presenter);
        root.ProcessFrame();
        TextBlock first = Assert.IsType<TextBlock>(presenter.PresentedChild);

        presenter.Content = DayOfWeek.Monday;
        root.ProcessFrame();

        Assert.Same(first, presenter.PresentedChild);
        Assert.Equal("Monday", first.Text);
    }

    [Fact]
    public void TemplateForTheContentTypeStillWinsOverTheText()
    {
        ContentTemplateRegistry registry = new();
        registry.Register(new ContentTemplate<int>("number", key: null, priority: 0,
            context => new TextBlock { Text = "#" + context.Data }));
        UIRoot root = new(200, 200);
        ContentPresenter presenter = new() { Content = 5, LocalTemplateRegistry = registry };
        root.VisualChildren.Add(presenter);

        root.ProcessFrame();

        Assert.Equal("#5", Assert.IsType<TextBlock>(presenter.PresentedChild).Text);
    }

    [Fact]
    public void NullContentStillGivesNoChild()
    {
        UIRoot root = new(200, 200);
        ContentPresenter presenter = new() { Content = 5 };
        ContentControl control = new() { Content = 5 };
        root.VisualChildren.Add(presenter);
        root.VisualChildren.Add(control);
        root.ProcessFrame();

        presenter.Content = null;
        control.Content = null;
        root.ProcessFrame();

        Assert.Null(presenter.PresentedChild);
        Assert.Empty(Texts(control));
        Assert.Empty(control.VisualChildren);
    }

    [Theory]
    [InlineData("hello", "hello")]
    [InlineData(5, "5")]
    [InlineData(DayOfWeek.Monday, "Monday")]
    public void UntemplatedContentControlShowsTheTextOfContentThatIsNotAnElement(object content, string expected)
    {
        UIRoot root = new(200, 200);
        ContentControl control = new() { Content = content };
        root.VisualChildren.Add(control);

        root.ProcessFrame();

        TextBlock text = Assert.Single(Texts(control));
        Assert.Equal(expected, text.Text);
        Assert.True(control.DesiredSize.Width > 0);
    }

    [Fact]
    public void UntemplatedContentControlHostsAnElementThatReplacesTextContentDirectly()
    {
        UIRoot root = new(200, 200);
        ContentControl control = new() { Content = 5 };
        root.VisualChildren.Add(control);
        root.ProcessFrame();
        Border element = new() { Width = 30, Height = 10 };

        control.Content = element;
        root.ProcessFrame();

        Assert.Same(element, Assert.Single(control.VisualChildren));
        Assert.Same(element, Assert.Single(control.LogicalChildren));
        Assert.Empty(Texts(control));
    }

    [Fact]
    public void ContentControlWithAComponentTemplatePresentsTextContentOnlyThroughItsTemplate()
    {
        UIRoot root = new(200, 200);
        ContentControl control = new() { Content = 5 };
        root.VisualChildren.Add(control);
        root.ProcessFrame();

        control.ComponentTemplate = new ComponentTemplate<ContentControl>("Host", context =>
            new ContentPresenter { Content = context.Owner.Content });
        root.ProcessFrame();

        TextBlock text = Assert.Single(Texts(control));
        Assert.Equal("5", text.Text);
        ContentPresenter templatePresenter = Assert.IsType<ContentPresenter>(Assert.Single(control.VisualChildren));
        Assert.Same(text, templatePresenter.PresentedChild);
    }

    // An untemplated Button draws its text content itself: content that is not a string is drawn once, as its text,
    // and the Button gets no generated presenter beside its own drawing.
    [Fact]
    public void UntemplatedButtonDrawsTheTextOfContentThatIsNotAStringOnce()
    {
        UIRoot root = new();
        Button button = new()
        {
            Content = 5,
            Background = new Cerneala.UI.Media.SolidColorBrush(Color.White),
            Foreground = new Cerneala.UI.Media.SolidColorBrush(Color.Black)
        };
        root.VisualChildren.Add(button);
        button.Arrange(new ArrangeContext(new LayoutRect(0, 0, 40, 20)));
        root.Invalidate(InvalidationFlags.Render | InvalidationFlags.Subtree, "test");
        root.ProcessFrame();

        DrawCommandList commands = root.RetainedRenderer.Commit(root);

        Assert.Empty(button.VisualChildren);
        Assert.Single(commands, command => command.Kind == DrawCommandKind.DrawText);
        float emptyWidth = new Button().Measure(new MeasureContext(new LayoutSize(200, 40))).Width;
        Assert.True(button.Measure(new MeasureContext(new LayoutSize(200, 40))).Width > emptyWidth);
    }

    [Fact]
    public void TabControlShowsTheTextOfHeadersThatAreNotStrings()
    {
        UIRoot root = new(400, 300);
        TabControl tabs = new();
        tabs.SetItems(new object[] { 1, DayOfWeek.Monday });
        tabs.SelectedIndex = 0;
        root.VisualChildren.Add(tabs);

        root.ProcessFrame();

        List<string> texts = Texts(tabs).Select(text => text.Text).ToList();
        Assert.Contains("1", texts);
        Assert.Contains("Monday", texts);
    }

    [Fact]
    public void MenuShowsTheTextOfItemsThatAreNotStrings()
    {
        UIRoot root = new(400, 300);
        Menu menu = new();
        menu.SetItems(new object[] { 1, DayOfWeek.Monday });
        root.VisualChildren.Add(menu);

        root.ProcessFrame();

        Assert.Equal(["1", "Monday"], Texts(menu).Select(text => text.Text));
    }

    private static List<TextBlock> Texts(UIElement element)
    {
        List<TextBlock> texts = [];
        Collect(element, texts);
        return texts;
    }

    private static void Collect(UIElement element, List<TextBlock> texts)
    {
        if (element is TextBlock text)
        {
            texts.Add(text);
        }

        foreach (UIElement child in element.VisualChildren)
        {
            Collect(child, texts);
        }
    }

    private sealed record Person(string Name);
}
