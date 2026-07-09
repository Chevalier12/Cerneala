#nullable enable

using System;
using Cerneala.Drawing;
using Cerneala.UI.Controls;
using Cerneala.UI.Elements;
using Cerneala.UI.Input;
using Cerneala.UI.Layout;
using Cerneala.UI.Media;
using Cerneala.UI.Motion.Layout;
using Cerneala.UI.Resources;
using PanelOrientation = Cerneala.UI.Layout.Orientation;
using MotionFactory = Cerneala.UI.Motion.Specs.Motion;

namespace Cerneala.Playground.Samples;

public sealed class LayoutMotionSample : IPlaygroundSample
{
    private readonly PlaygroundText text;

    public LayoutMotionSample(IResourceProvider? resourceProvider = null, ResourceId<FontResource>? fontResourceId = null)
    {
        text = new PlaygroundText(resourceProvider, fontResourceId);
    }

    public string Name => "Layout Motion";

    public UIElement Build()
    {
        StackPanel list = new() { Orientation = PanelOrientation.Vertical };
        TextBlock stats = text.Create(
            "measure=0 arrange=0 correction ticks are render-only",
            14,
            new DrawColor(71, 85, 105));
        Border expandable = Item("A", new DrawColor(219, 234, 254));
        Border second = Item("B", new DrawColor(220, 252, 231));
        Border third = Item("C", new DrawColor(254, 249, 195));
        list.VisualChildren.Add(expandable);
        list.VisualChildren.Add(second);
        list.VisualChildren.Add(third);

        StackPanel root = new()
        {
            Margin = new Thickness(32, 24, 32, 0),
            Orientation = PanelOrientation.Vertical
        };
        root.VisualChildren.Add(new Button
        {
            Content = text.Create("Reorder", 14, DrawColor.Black),
            Padding = new Thickness(12, 8, 12, 8),
            Command = new ActionCommand(_ =>
            {
                list.VisualChildren.Remove(third);
                list.VisualChildren.Remove(second);
                list.VisualChildren.Remove(expandable);
                list.VisualChildren.Add(third);
                list.VisualChildren.Add(expandable);
                list.VisualChildren.Add(second);
                stats.Text = "measure=1 arrange=1 correction ticks stay render-only";
            })
        });
        root.VisualChildren.Add(new Button
        {
            Content = text.Create("Expand", 14, DrawColor.Black),
            Padding = new Thickness(12, 8, 12, 8),
            Command = new ActionCommand(_ =>
            {
                expandable.Padding = expandable.Padding.Top > 12
                    ? new Thickness(12)
                    : new Thickness(12, 28, 12, 28);
                stats.Text = "measure=1 arrange=1 expansion then render-only correction";
            })
        });
        root.VisualChildren.Add(stats);
        root.VisualChildren.Add(list);
        return root;
    }

    private Border Item(string label, DrawColor color)
    {
        return new Border
        {
            Padding = new Thickness(12),
            Background = color,
            BorderColor = new DrawColor(148, 163, 184),
            BorderThickness = new Thickness(1),
            LayoutMotionId = $"playground-layout-{label}",
            LayoutMotion = LayoutMotionOptions.Spring(MotionFactory.Tween<Transform>(TimeSpan.FromMilliseconds(180))),
            Child = text.Create(label, 16, new DrawColor(30, 41, 59))
        };
    }
}
