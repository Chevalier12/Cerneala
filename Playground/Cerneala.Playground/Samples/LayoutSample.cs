#nullable enable

using Cerneala.Drawing;
using Cerneala.UI.Controls;
using Cerneala.UI.Elements;
using Cerneala.UI.Layout;
using Cerneala.UI.Resources;
using PanelOrientation = Cerneala.UI.Layout.Panels.Orientation;

namespace Cerneala.Playground.Samples;

public sealed class LayoutSample : IPlaygroundSample
{
    private readonly PlaygroundText text;

    public LayoutSample(IResourceProvider? resourceProvider = null, ResourceId<FontResource>? fontResourceId = null)
    {
        text = new PlaygroundText(resourceProvider, fontResourceId);
    }

    public string Name => "Layout";

    public UIElement Build()
    {
        StackPanel rows = new()
        {
            Margin = new Thickness(32),
            Orientation = PanelOrientation.Vertical
        };

        rows.VisualChildren.Add(Row("Layout sample", 22, new DrawColor(20, 28, 42), new Thickness(0, 0, 0, 8)));
        rows.VisualChildren.Add(Row("Vertical StackPanel with retained Border children", 15, new DrawColor(72, 80, 92), new Thickness(0, 0, 0, 10)));
        rows.VisualChildren.Add(Tile("Measure", "Desired size is cached until invalidated.", new DrawColor(224, 241, 255)));
        rows.VisualChildren.Add(Tile("Arrange", "Bounds update only when layout changes.", new DrawColor(229, 246, 236)));
        rows.VisualChildren.Add(Tile("Render", "Draw commands are regenerated on demand.", new DrawColor(252, 240, 219)));
        return rows;
    }

    private Border Tile(string title, string body, DrawColor background)
    {
        StackPanel content = new()
        {
            Orientation = PanelOrientation.Vertical
        };
        content.VisualChildren.Add(Text(title, 18, new DrawColor(28, 35, 48)));
        content.VisualChildren.Add(Text(body, 14, new DrawColor(62, 72, 86)));

        return new Border
        {
            Padding = new Thickness(12, 10, 12, 10),
            Background = background,
            BorderColor = new DrawColor(150, 162, 178),
            BorderThickness = new Thickness(1),
            Child = content
        };
    }

    private TextBlock Text(string value, float size, DrawColor color)
    {
        return text.Create(value, size, color);
    }

    private UIElement Row(string value, float size, DrawColor color, Thickness padding)
    {
        return text.Row(value, size, color, padding);
    }
}
