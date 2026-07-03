#nullable enable

using System;
using Cerneala.Drawing;
using Cerneala.UI.Controls;
using Cerneala.UI.Elements;
using Cerneala.UI.Hosting;
using Cerneala.UI.Layout;
using Cerneala.UI.Resources;

namespace Cerneala.Playground.Samples;

public sealed class InvalidationStatsOverlay
{
    private readonly PlaygroundText playgroundText;
    private readonly TextBlock text;

    public InvalidationStatsOverlay(IResourceProvider? resourceProvider = null, ResourceId<FontResource>? fontResourceId = null)
    {
        playgroundText = new PlaygroundText(resourceProvider, fontResourceId);
        text = playgroundText.Create(Format(null), 13, DrawColor.White);

        Root = new Border
        {
            Margin = new Thickness(32, 8, 32, 0),
            Padding = new Thickness(10),
            Background = new DrawColor(24, 28, 36, 230),
            BorderColor = new DrawColor(74, 86, 104),
            BorderThickness = new Thickness(1),
            Child = text
        };
    }

    public UIElement Root { get; }

    public string Text => text.Text;

    public void Update(UiFrame? frame)
    {
        if (frame is null)
        {
            return;
        }

        text.Text = Format(frame);
    }

    public static string Format(UiFrame? frame)
    {
        if (frame is null)
        {
            return "Frame stats: waiting for first retained frame";
        }

        return string.Create(
            System.Globalization.CultureInfo.InvariantCulture,
            $"Frame stats: measured={frame.Stats.MeasuredElements}, arranged={frame.Stats.ArrangedElements}, renderCache={frame.Stats.RenderedElements}, hitTest={frame.Stats.HitTestElements}, reusedCaches={frame.Stats.ReusedCaches}, noWork={frame.Stats.NoWorkFrames}");
    }
}
