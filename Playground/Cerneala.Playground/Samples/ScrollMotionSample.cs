#nullable enable

using Cerneala.Drawing;
using Cerneala.UI.Controls;
using Cerneala.UI.Elements;
using Cerneala.UI.Layout;
using Cerneala.UI.Resources;
using PanelOrientation = Cerneala.UI.Layout.Panels.Orientation;

namespace Cerneala.Playground.Samples;

public sealed class ScrollMotionSample : IPlaygroundSample
{
    private readonly PlaygroundText text;

    public ScrollMotionSample(IResourceProvider? resourceProvider = null, ResourceId<FontResource>? fontResourceId = null)
    {
        text = new PlaygroundText(resourceProvider, fontResourceId);
    }

    public string Name => "Scroll Motion";

    public UIElement Build()
    {
        Border header = new()
        {
            Padding = new Thickness(12),
            Background = new DrawColor(14, 165, 233),
            Opacity = 0.72f,
            Child = text.Create("Header fade", 18, DrawColor.White)
        };
        Border parallax = new()
        {
            Padding = new Thickness(12),
            Background = new DrawColor(99, 102, 241),
            TranslateY = -18,
            Child = text.Create("Parallax", 16, DrawColor.White)
        };
        Border progress = new()
        {
            Padding = new Thickness(2),
            Background = new DrawColor(34, 197, 94),
            ScaleX = 0.35f,
            Child = text.Create("Progress", 12, DrawColor.White)
        };
        StackPanel content = new() { Orientation = PanelOrientation.Vertical };
        content.VisualChildren.Add(header);
        content.VisualChildren.Add(parallax);
        content.VisualChildren.Add(progress);
        for (int i = 0; i < 8; i++)
        {
            content.VisualChildren.Add(new Border
            {
                Padding = new Thickness(12),
                Background = i % 2 == 0 ? new DrawColor(241, 245, 249) : new DrawColor(226, 232, 240),
                Child = text.Create($"Scroll row {i + 1}", 16, new DrawColor(30, 41, 59))
            });
        }

        return new ScrollViewer
        {
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
            Content = content
        };
    }
}
