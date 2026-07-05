#nullable enable

using Cerneala.Drawing;
using Cerneala.UI.Controls;
using Cerneala.UI.Elements;
using Cerneala.UI.Input;
using Cerneala.UI.Layout;
using Cerneala.UI.Resources;
using PanelOrientation = Cerneala.UI.Layout.Panels.Orientation;

namespace Cerneala.Playground.Samples;

public sealed class RetainedAppSample : IPlaygroundSample
{
    private readonly PlaygroundText text;
    private int clickCount;

    public RetainedAppSample(IResourceProvider? resourceProvider = null, ResourceId<FontResource>? fontResourceId = null)
    {
        text = new PlaygroundText(resourceProvider, fontResourceId);
    }

    public string Name => "Retained App";

    public TextBlock? StatusText { get; private set; }

    public Button? PrimaryButton { get; private set; }

    public UIElement Build()
    {
        clickCount = 0;
        StatusText = text.Create("Ready. No retained work should run on unchanged frames.", 14, new DrawColor(51, 65, 85));
        PrimaryButton = new Button
        {
            Content = text.Create("Run retained command", 14, new DrawColor(15, 23, 42)),
            Padding = new Thickness(12, 8, 12, 8),
            Background = new DrawColor(248, 250, 252),
            BorderColor = new DrawColor(100, 116, 139),
            BorderThickness = new Thickness(1),
            Command = new ActionCommand(_ =>
            {
                clickCount++;
                StatusText.Text = $"Command executed {clickCount} time(s).";
            })
        };

        StackPanel root = new()
        {
            Margin = new Thickness(32, 24, 32, 24),
            Orientation = PanelOrientation.Vertical
        };

        root.VisualChildren.Add(text.Create("Cerneala retained app", 26, new DrawColor(15, 23, 42)));
        root.VisualChildren.Add(text.Create("Retained tree, invalidation-driven layout/render, explicit input.", 15, new DrawColor(71, 85, 105)));
        root.VisualChildren.Add(BuildInteractionCard());
        root.VisualChildren.Add(BuildListCard());
        return root;
    }

    private UIElement BuildInteractionCard()
    {
        StackPanel content = new()
        {
            Orientation = PanelOrientation.Vertical
        };
        content.VisualChildren.Add(text.Create("Interactive state", 18, new DrawColor(30, 41, 59)));
        content.VisualChildren.Add(BuildImagePreview());
        content.VisualChildren.Add(StatusText!);
        content.VisualChildren.Add(PrimaryButton!);

        return new Border
        {
            Margin = new Thickness(0, 16, 0, 0),
            Padding = new Thickness(14),
            Background = new DrawColor(241, 245, 249),
            BorderColor = new DrawColor(148, 163, 184),
            BorderThickness = new Thickness(1),
            Child = content
        };
    }

    private UIElement BuildImagePreview()
    {
        return new Border
        {
            Margin = new Thickness(0, 8, 0, 8),
            Padding = new Thickness(8),
            Background = new DrawColor(226, 232, 240),
            BorderColor = new DrawColor(148, 163, 184),
            BorderThickness = new Thickness(1),
            Child = new Image
            {
                Source = new SampleImage(96, 36),
                Foreground = new DrawColor(59, 130, 246)
            }
        };
    }

    private UIElement BuildListCard()
    {
        StackPanel list = new()
        {
            Orientation = PanelOrientation.Vertical
        };

        for (int i = 1; i <= 8; i++)
        {
            list.VisualChildren.Add(text.Row($"Retained row {i}", 14, new DrawColor(51, 65, 85), new Thickness(0, 4, 0, 4)));
        }

        return new Border
        {
            Margin = new Thickness(0, 12, 0, 0),
            Padding = new Thickness(14),
            Background = new DrawColor(255, 255, 255),
            BorderColor = new DrawColor(203, 213, 225),
            BorderThickness = new Thickness(1),
            Child = new ScrollViewer
            {
                Content = list,
                VerticalScrollBarVisibility = ScrollBarVisibility.Visible
            }
        };
    }

    private sealed record SampleImage(int Width, int Height) : IDrawImage;
}
