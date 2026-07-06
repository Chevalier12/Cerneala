using Cerneala.UI.Controls;
using Cerneala.UI.Elements;
using Cerneala.UI.Layout;

namespace Cerneala.Tests.Controls;

public sealed class ContentPresenterDefaultTextTests
{
    [Fact]
    public void ContentPresenterCreatesTextBlockForStringContentWithoutTemplate()
    {
        ContentPresenter presenter = new() { Content = "Hello" };

        presenter.Measure(new MeasureContext(new LayoutSize(200, 40)));

        TextBlock textBlock = Assert.IsType<TextBlock>(presenter.PresentedChild);
        Assert.Equal("Hello", textBlock.Text);
    }

    [Fact]
    public void ContentPresenterReusesGeneratedTextBlockWhenStringContentUnchanged()
    {
        ContentPresenter presenter = new() { Content = "Hello" };
        presenter.Measure(new MeasureContext(new LayoutSize(200, 40)));
        UIElement first = presenter.PresentedChild!;

        presenter.Content = new string(['H', 'e', 'l', 'l', 'o']);
        presenter.Measure(new MeasureContext(new LayoutSize(200, 40)));

        Assert.Same(first, presenter.PresentedChild);
    }

    [Fact]
    public void ContentPresenterUpdatesGeneratedTextBlockTextWhenStringContentChanges()
    {
        ContentPresenter presenter = new() { Content = "Hello" };
        presenter.Measure(new MeasureContext(new LayoutSize(200, 40)));
        TextBlock first = Assert.IsType<TextBlock>(presenter.PresentedChild);

        presenter.Content = "World";
        presenter.Measure(new MeasureContext(new LayoutSize(200, 40)));

        Assert.Same(first, presenter.PresentedChild);
        Assert.Equal("World", first.Text);
    }

    [Fact]
    public void ContentPresenterDoesNotCreateChildForNullContent()
    {
        ContentPresenter presenter = new();

        presenter.Measure(new MeasureContext(new LayoutSize(200, 40)));

        Assert.Null(presenter.PresentedChild);
    }
}
