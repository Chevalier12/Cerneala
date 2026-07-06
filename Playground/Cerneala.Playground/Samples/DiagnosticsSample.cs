#nullable enable

using Cerneala.Drawing;
using Cerneala.UI.Controls;
using Cerneala.UI.Diagnostics;
using Cerneala.UI.Elements;
using Cerneala.UI.Invalidation;
using Cerneala.UI.Layout;
using Cerneala.UI.Resources;
using PanelOrientation = Cerneala.UI.Layout.Panels.Orientation;

namespace Cerneala.Playground.Samples;

public sealed class DiagnosticsSample : IPlaygroundSample
{
    private readonly ResourceId<FontResource>? fontResourceId;
    private readonly IResourceProvider? resourceProvider;
    private readonly PlaygroundText text;

    public DiagnosticsSample(IResourceProvider? resourceProvider = null, ResourceId<FontResource>? fontResourceId = null)
    {
        this.resourceProvider = resourceProvider;
        this.fontResourceId = fontResourceId;
        text = new PlaygroundText(resourceProvider, fontResourceId);
    }

    public string Name => "Diagnostics";

    public UIElement Build()
    {
        StackPanel panel = new()
        {
            Margin = new Thickness(32, 24, 32, 0),
            Orientation = PanelOrientation.Vertical
        };

        TextBlock heading = text.Create("Diagnostics", 28, new DrawColor(28, 35, 48));
        TextBlock summary = text.Create("Frame, tree, dirty-state, render-cache, routed-event, and style traces are retained diagnostics.", 16, new DrawColor(71, 85, 105));
        DebugOverlay overlay = new(resourceProvider, fontResourceId)
        {
            Text = "Frame diagnostics: waiting for retained frame\nDirty tree: inspect root.Trace\nRender cache: inspect root.RetainedRenderCache"
        };

        DebugAdorner adorner = new()
        {
            Child = new Border
            {
                Padding = new Thickness(10),
                Background = new DrawColor(241, 245, 249),
                BorderColor = new DrawColor(148, 163, 184),
                BorderThickness = new Thickness(1),
                Child = text.Create("Adorner renders through retained UI.", 15, new DrawColor(30, 41, 59))
            }
        };

        Button invalidateButton = new()
        {
            Content = text.Create("Invalidate diagnostics sample", 14, new DrawColor(28, 35, 48)),
            Padding = new Thickness(12, 8, 12, 8),
            Command = new Cerneala.UI.Input.ActionCommand(_ =>
            {
                overlay.Text = "Frame diagnostics: overlay invalidated\n" +
                    "Dirty tree: overlay text changed\n" +
                    "Render cache: overlay subtree is render-dirty";
                overlay.Root.Invalidate(InvalidationFlags.Render, "Diagnostics sample manual invalidation");
            })
        };

        panel.VisualChildren.Add(heading);
        panel.VisualChildren.Add(summary);
        panel.VisualChildren.Add(overlay.Root);
        panel.VisualChildren.Add(adorner);
        panel.VisualChildren.Add(invalidateButton);
        return panel;
    }
}
