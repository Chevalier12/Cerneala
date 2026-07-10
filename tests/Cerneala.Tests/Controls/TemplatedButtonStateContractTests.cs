using Cerneala.Drawing;
using Cerneala.UI.Controls;
using Cerneala.UI.Elements;
using Cerneala.UI.Layout;

namespace Cerneala.Tests.Controls;

public sealed class TemplatedButtonStateContractTests
{
    [Fact]
    public void TemplatedButtonUsesTemplateRootAndSkipsFallbackRender()
    {
        Button button = new()
        {
            Content = "Save",
            ComponentTemplate = Cerneala.UI.Theming.DefaultTheme.CreateButtonTemplate()
        };

        button.Measure(new MeasureContext(new LayoutSize(200, 40)));

        Assert.IsType<Border>(button.ComponentTemplateInstance!.Root);
    }

    [Fact]
    public void TemplatedButtonBindsContentToContentPresenter()
    {
        Button button = TemplatedButton("Save");
        button.Measure(new MeasureContext(new LayoutSize(200, 40)));

        ContentPresenter presenter = FindDescendant<ContentPresenter>(button);
        Assert.Equal("Save", presenter.Content);
        Assert.IsType<TextBlock>(presenter.PresentedChild);
    }

    [Fact]
    public void TemplatedButtonBindsChromePropertiesToBorder()
    {
        Button button = TemplatedButton("Save");
        button.Background = new DrawColor(1, 2, 3);
        button.BorderColor = new DrawColor(4, 5, 6);
        button.BorderThickness = new Thickness(2);
        button.Padding = new Thickness(3);

        button.Measure(new MeasureContext(new LayoutSize(200, 40)));
        Border border = Assert.IsType<Border>(button.ComponentTemplateInstance!.Root);

        Assert.Equal(button.Background, border.Background);
        Assert.Equal(button.BorderColor, border.BorderColor);
        Assert.Equal(button.BorderThickness, border.BorderThickness);
        Assert.Equal(button.Padding, border.Padding);
    }

    private static Button TemplatedButton(string content)
    {
        return new Button
        {
            Content = content,
            ComponentTemplate = Cerneala.UI.Theming.DefaultTheme.CreateButtonTemplate()
        };
    }

    private static T FindDescendant<T>(UIElement element)
        where T : UIElement
    {
        if (element is T typed)
        {
            return typed;
        }

        foreach (UIElement child in element.VisualChildren)
        {
            try
            {
                return FindDescendant<T>(child);
            }
            catch (InvalidOperationException)
            {
            }
        }

        throw new InvalidOperationException($"No descendant of type {typeof(T).Name}.");
    }
}
