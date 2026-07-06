#nullable enable

using System;
using Cerneala.Drawing;
using Cerneala.UI.Controls;
using Cerneala.UI.Elements;
using Cerneala.UI.Input;
using Cerneala.UI.Layout;
using Cerneala.UI.Resources;
using PanelOrientation = Cerneala.UI.Layout.Panels.Orientation;

namespace Cerneala.Playground.Samples;

public sealed class RetainedButtonSample : IPlaygroundSample
{
    private readonly PlaygroundText text;

    public RetainedButtonSample(IResourceProvider? resourceProvider = null, ResourceId<FontResource>? fontResourceId = null)
    {
        text = new PlaygroundText(resourceProvider, fontResourceId);
    }

    public string Name => "Button";

    public UIElement Build()
    {
        TextBlock status = Text("Ready: retained button sample", 16, new DrawColor(42, 50, 64));

        Button button = new()
        {
            Content = Text("Click retained button", 16, new DrawColor(28, 35, 48), Thickness.Zero),
            Padding = new Thickness(12, 8, 12, 8),
            Command = new ActionCommand(_ => status.Text = $"Clicked at {DateTime.Now:HH:mm:ss}")
        };

        Border preview = new()
        {
            Padding = new Thickness(10),
            Background = new DrawColor(235, 240, 246),
            BorderColor = new DrawColor(160, 172, 188),
            BorderThickness = new Thickness(1),
            Child = Text("Border child uses retained text content", 14, new DrawColor(54, 64, 78))
        };

        StackPanel panel = new()
        {
            Margin = new Thickness(32),
            Orientation = PanelOrientation.Vertical
        };
        panel.VisualChildren.Add(Text("Retained controls", 22, new DrawColor(20, 28, 42)));
        panel.VisualChildren.Add(button);
        panel.VisualChildren.Add(new Border
        {
            Padding = new Thickness(0, 8, 0, 8),
            Background = DrawColor.Transparent,
            Child = status
        });
        panel.VisualChildren.Add(preview);
        return panel;
    }

    private TextBlock Text(string text, float size, DrawColor color)
    {
        return Text(text, size, color, Thickness.Zero);
    }

    private TextBlock Text(string value, float size, DrawColor color, Thickness margin)
    {
        return text.Create(value, size, color, margin);
    }
}
