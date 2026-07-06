using Cerneala.UI.Controls;
using Cerneala.UI.Elements;
using Cerneala.UI.Layout;
using Cerneala.UI.Styling;

namespace Cerneala.Tests.UI.Styling;

public sealed class DefaultThemeTemplateTests
{
    [Fact]
    public void DefaultThemeProvidesButtonTemplateThroughStyle()
    {
        UIRoot root = StyledRoot();
        Button button = new() { Content = "Save" };
        root.VisualChildren.Add(button);

        root.ProcessFrame();

        Assert.NotNull(button.Template);
        Assert.NotNull(button.TemplateInstance);
    }

    [Fact]
    public void DefaultThemeButtonTemplateDisplaysStringContent()
    {
        UIRoot root = StyledRoot();
        Button button = new() { Content = "Save" };
        root.VisualChildren.Add(button);
        root.ProcessFrame();

        ContentPresenter presenter = FindDescendant<ContentPresenter>(button);

        Assert.IsType<TextBlock>(presenter.PresentedChild);
    }

    private static UIRoot StyledRoot()
    {
        UIRoot root = new(200, 80);
        root.SetThemeProvider(new ThemeProvider(DefaultTheme.Create()));
        root.SetStyleSheet(DefaultTheme.CreateStyleSheet());
        return root;
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
