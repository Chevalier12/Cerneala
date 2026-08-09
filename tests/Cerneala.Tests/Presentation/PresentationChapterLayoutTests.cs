using Cerneala.Presentation;
using Cerneala.UI.Controls;
using Cerneala.UI.Elements;
using Cerneala.UI.Layout;

namespace Cerneala.Tests.Presentation;

public sealed class PresentationChapterLayoutTests
{
    [Fact]
    public void WelcomeMentalModelFlowStaysInsideItsPanelAtNarrowWidth()
    {
        UIRoot root = new(538, 320);
        WelcomeChapterView view = new();
        root.VisualChildren.Add(view);

        root.ProcessFrame();

        TextBlock flow = Descendants(view)
            .OfType<TextBlock>()
            .Single(text => text.Text.StartsWith("input  ->", StringComparison.Ordinal));
        Border flowBorder = Assert.IsType<Border>(flow.VisualParent);
        Border panelBorder = Assert.IsType<Border>(flowBorder.VisualParent?.VisualParent);
        float overflow = Bottom(flowBorder.ArrangedBounds) - Bottom(panelBorder.ArrangedBounds);

        Assert.True(
            overflow <= 0.01f,
            $"Flow block overflows its panel by {overflow} pixels; panel={panelBorder.ArrangedBounds}; flow={flowBorder.ArrangedBounds}.");
    }

    private static float Bottom(LayoutRect rect) => rect.Y + rect.Height;

    private static IEnumerable<UIElement> Descendants(UIElement element)
    {
        foreach (UIElement child in element.VisualChildren)
        {
            yield return child;
            foreach (UIElement descendant in Descendants(child))
            {
                yield return descendant;
            }
        }
    }
}
