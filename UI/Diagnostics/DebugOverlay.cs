using Cerneala.Drawing;
using Cerneala.UI.Controls;
using Cerneala.UI.Elements;
using Cerneala.UI.Invalidation;
using Cerneala.UI.Layout;
using Cerneala.UI.Resources;

namespace Cerneala.UI.Diagnostics;

public sealed class DebugOverlay
{
    private readonly TextBlock textBlock;

    public DebugOverlay(IResourceProvider? resourceProvider = null, ResourceId<FontResource>? fontResourceId = null)
    {
        textBlock = new TextBlock
        {
            Text = string.Empty,
            Foreground = Color.White,
            FontSize = 13,
            ResourceProvider = resourceProvider,
            FontResourceId = fontResourceId
        };

        Root = new Border
        {
            Padding = new Thickness(8),
            Background = new Color(18, 24, 32, 230),
            BorderColor = new Color(92, 107, 128),
            BorderThickness = new Thickness(1),
            Child = textBlock
        };
    }

    public UIElement Root { get; }

    public string Text
    {
        get => textBlock.Text;
        set
        {
            string next = value ?? string.Empty;
            if (textBlock.Text == next)
            {
                return;
            }

            textBlock.Text = next;
            Root.Invalidate(InvalidationFlags.Measure | InvalidationFlags.Render, "Debug overlay text changed");
        }
    }
}
