#nullable enable

using System;
using Cerneala.Drawing;
using Cerneala.UI.Controls;
using Cerneala.UI.Elements;
using Cerneala.UI.Input;
using Cerneala.UI.Layout;
using Cerneala.UI.Motion.Core;
using Cerneala.UI.Motion.Presence;
using Cerneala.UI.Resources;
using PanelOrientation = Cerneala.UI.Layout.Orientation;
using MotionFactory = Cerneala.UI.Motion.Specs.Motion;

namespace Cerneala.Playground.Samples;

public sealed class PresenceMotionSample : IPlaygroundSample
{
    private readonly PlaygroundText text;

    public PresenceMotionSample(IResourceProvider? resourceProvider = null, ResourceId<FontResource>? fontResourceId = null)
    {
        text = new PlaygroundText(resourceProvider, fontResourceId);
    }

    public string Name => "Presence";

    public UIElement Build()
    {
        StackPanel items = new() { Orientation = PanelOrientation.Vertical };
        items.VisualChildren.Add(Item(1));

        StackPanel root = new()
        {
            Margin = new Thickness(32, 24, 32, 0),
            Orientation = PanelOrientation.Vertical
        };
        int next = 2;
        root.VisualChildren.Add(new Button
        {
            Content = text.Create("Add", 14, DrawColor.Black),
            Padding = new Thickness(12, 8, 12, 8),
            Command = new ActionCommand(_ => items.VisualChildren.Add(Item(next++)))
        });
        root.VisualChildren.Add(new Button
        {
            Content = text.Create("Remove", 14, DrawColor.Black),
            Padding = new Thickness(12, 8, 12, 8),
            Command = new ActionCommand(_ =>
            {
                if (items.VisualChildren.Count > 0)
                {
                    items.VisualChildren.Remove(items.VisualChildren[items.VisualChildren.Count - 1]);
                }
            })
        });
        root.VisualChildren.Add(new Button
        {
            Content = text.Create("Reduced motion", 14, DrawColor.Black),
            Padding = new Thickness(12, 8, 12, 8),
            Command = new ActionCommand(_ => root.Root?.Motion.ReducedMotion.SetMode(ReducedMotionMode.DisableNonEssential))
        });
        root.VisualChildren.Add(items);
        return root;
    }

    private Border Item(int index)
    {
        return new Border
        {
            Padding = new Thickness(12),
            Background = new DrawColor(241, 245, 249),
            BorderColor = new DrawColor(148, 163, 184),
            BorderThickness = new Thickness(1),
            Presence = PresenceOptions.FadeAndScale(
                MotionFactory.Tween<float>(TimeSpan.FromMilliseconds(120)),
                MotionFactory.Tween<float>(TimeSpan.FromMilliseconds(120))),
            Child = text.Create($"Item {index}", 16, new DrawColor(30, 41, 59))
        };
    }
}
