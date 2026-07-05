using Cerneala.Playground.Samples;
using Cerneala.UI.Controls;
using Cerneala.UI.Core;
using Cerneala.UI.Elements;
using Cerneala.UI.Styling;

namespace Cerneala.Tests.Playground;

public sealed class RetainedAppStyleContractTests
{
    [Fact]
    public void RetainedAppSampleUsesThemeOrStyleForAtLeastCoreButtonAndTextVisuals()
    {
        RetainedAppSample sample = new();
        UIRoot root = new(400, 300);
        root.SetThemeProvider(new ThemeProvider(DefaultTheme.Create()));
        root.SetStyleSheet(DefaultTheme.CreateStyleSheet());
        root.VisualChildren.Add(sample.Build());

        root.ProcessFrame();

        Button primaryButton = Assert.IsType<Button>(sample.PrimaryButton);
        Assert.Equal(UiPropertyValueSource.StyleBase, primaryButton.GetValueSource(Control.BackgroundProperty));
        Assert.Equal(UiPropertyValueSource.StyleBase, primaryButton.GetValueSource(Control.BorderColorProperty));
        Assert.Equal(UiPropertyValueSource.StyleBase, primaryButton.GetValueSource(Control.ForegroundProperty));

        TextBlock primaryButtonText = Assert.IsType<TextBlock>(primaryButton.Content);
        UiPropertyValueSource textForegroundSource = primaryButtonText.GetValueSource(Control.ForegroundProperty);
        Assert.True(
            textForegroundSource is UiPropertyValueSource.StyleBase or UiPropertyValueSource.Inherited,
            $"Expected themed text foreground, got {textForegroundSource}.");

        Assert.Contains(Walk(root), element =>
            element is Border border &&
            border.GetValueSource(Control.BackgroundProperty) == UiPropertyValueSource.StyleBase);
    }

    private static IEnumerable<UIElement> Walk(UIElement element)
    {
        yield return element;
        foreach (UIElement child in element.VisualChildren)
        {
            foreach (UIElement descendant in Walk(child))
            {
                yield return descendant;
            }
        }
    }
}
