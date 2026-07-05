#nullable enable

using Cerneala.Drawing;
using Cerneala.UI.Controls;
using Cerneala.UI.Elements;
using Cerneala.UI.Layout;
using Cerneala.UI.Resources;

namespace Cerneala.Playground.Samples;

internal sealed class PlaygroundText
{
    private readonly IResourceProvider? resourceProvider;
    private readonly ResourceId<FontResource>? fontResourceId;

    public PlaygroundText(IResourceProvider? resourceProvider = null, ResourceId<FontResource>? fontResourceId = null)
    {
        this.resourceProvider = resourceProvider;
        this.fontResourceId = fontResourceId;
    }

    public TextBlock Create(string text, float size, DrawColor? color = null, Thickness margin = default)
    {
        TextBlock block = new()
        {
            Text = text,
            FontSize = size,
            Margin = margin,
            ResourceProvider = resourceProvider,
            FontResourceId = fontResourceId
        };

        if (color is DrawColor foreground)
        {
            block.Foreground = foreground;
        }

        return block;
    }

    public UIElement Row(string text, float size, DrawColor color, Thickness padding)
    {
        return new Border
        {
            Padding = padding,
            Background = DrawColor.Transparent,
            Child = Create(text, size, color)
        };
    }
}
