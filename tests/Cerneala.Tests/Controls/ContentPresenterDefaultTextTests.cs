using Cerneala.Drawing;
using Cerneala.UI.Controls;
using Cerneala.UI.Elements;
using Cerneala.UI.Layout;
using Cerneala.UI.Media;

namespace Cerneala.Tests.Controls;

public sealed class ContentPresenterDefaultTextTests
{
    [Fact]
    public void ForegroundChangedAfterStringContentUpdatesGeneratedTextBlock()
    {
        ContentPresenter presenter = new() { Content = "Open 3 test windows" };
        TextBlock textBlock = Assert.IsType<TextBlock>(presenter.PresentedChild);

        SolidColorBrush foreground = new(Color.White);
        presenter.Foreground = foreground;

        Assert.Same(foreground, presenter.Foreground);
        Assert.Same(foreground, textBlock.Foreground);
    }

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
