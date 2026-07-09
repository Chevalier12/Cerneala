#nullable enable

using Cerneala.Drawing;
using Cerneala.UI.Controls;
using Cerneala.UI.Elements;
using Cerneala.UI.Layout;
using Cerneala.UI.Resources;
using PanelOrientation = Cerneala.UI.Layout.Orientation;

namespace Cerneala.Playground.Samples;

public sealed class TextSample : IPlaygroundSample
{
    private readonly PlaygroundText text;

    public TextSample(IResourceProvider? resourceProvider = null, ResourceId<FontResource>? fontResourceId = null)
    {
        text = new PlaygroundText(resourceProvider, fontResourceId);
    }

    public string Name => "Text";

    public UIElement Build()
    {
        StackPanel panel = new()
        {
            Margin = new Thickness(32),
            Orientation = PanelOrientation.Vertical
        };

        panel.VisualChildren.Add(Row("Text services", 24, new DrawColor(20, 28, 42), new Thickness(0, 0, 0, 8)));
        panel.VisualChildren.Add(Row("TextBlock uses retained measure/render services.", 16, new DrawColor(52, 62, 78), new Thickness(0, 0, 0, 6)));
        panel.VisualChildren.Add(Row("Short labels, body copy, and larger headings share the same retained tree.", 14, new DrawColor(72, 80, 92), new Thickness(0, 0, 0, 12)));
        panel.VisualChildren.Add(new Border
        {
            Padding = new Thickness(14, 12, 14, 12),
            Background = new DrawColor(241, 244, 248),
            BorderColor = new DrawColor(160, 172, 188),
            BorderThickness = new Thickness(1),
            Child = Text("No immediate drawing element is needed for this paragraph.", 15, new DrawColor(34, 44, 60))
        });

        return panel;
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
