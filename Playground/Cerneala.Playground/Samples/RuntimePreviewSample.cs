#nullable enable

using Cerneala.Drawing;
using Cerneala.UI.Controls;
using Cerneala.UI.Data;
using Cerneala.UI.Diagnostics;
using Cerneala.UI.Elements;
using Cerneala.UI.Hosting;
using Cerneala.UI.Input;
using Cerneala.UI.Layout;
using Cerneala.UI.Resources;
using PanelOrientation = Cerneala.UI.Layout.Panels.Orientation;

namespace Cerneala.Playground.Samples;

public sealed class RuntimePreviewSample : IPlaygroundSample
{
    private readonly PlaygroundText text;
    private readonly IResourceProvider? resourceProvider;
    private readonly ResourceId<FontResource>? fontResourceId;
    private readonly ResourceId<ImageResource>? imageResourceId;
    private readonly ActionCommand appendCommand;

    public RuntimePreviewSample(
        IResourceProvider? resourceProvider = null,
        ResourceId<FontResource>? fontResourceId = null,
        ResourceId<ImageResource>? imageResourceId = null)
    {
        this.resourceProvider = resourceProvider;
        this.fontResourceId = fontResourceId;
        this.imageResourceId = imageResourceId;
        text = new PlaygroundText(resourceProvider, fontResourceId);
        appendCommand = new ActionCommand(_ => AppendItem(), _ => !string.IsNullOrWhiteSpace(InputValue.Value));
        InputValue.ValueChanged += (_, _) => appendCommand.RaiseCanExecuteChanged();
        Items = new ObservableList<string>(["Scaled viewport", "Cached image resource", "Clipboard-ready TextBox"]);
    }

    public string Name => "Runtime Preview";

    public ObservableValue<string> InputValue { get; } = new("copy paste here");

    public ObservableList<string> Items { get; }

    public Image? PreviewImage { get; private set; }

    public Image? Image => PreviewImage;

    public TextBox? InputTextBox { get; private set; }

    public Button? ActionButton { get; private set; }

    public Button? PrimaryButton => ActionButton;

    public ListBox? ItemsList { get; private set; }

    public ListBox? ListBox => ItemsList;

    public TextBlock? DiagnosticsText { get; private set; }

    public TextBlock? DiagnosticsOverlay => DiagnosticsText;

    public UIElement? RootElement { get; private set; }

    public UIElement Build()
    {
        InputValue.Value = "copy paste here";

        PreviewImage = new Image
        {
            UseIntrinsicSize = true,
            Foreground = DrawColor.White
        };

        if (imageResourceId is ResourceId<ImageResource> id)
        {
            PreviewImage.SourceResourceId = id;
            PreviewImage.ResourceProvider = resourceProvider;
        }
        else
        {
            PreviewImage.Source = new SampleImage(96, 42);
        }

        InputTextBox = new TextBox
        {
            Padding = new Thickness(8, 5, 8, 5),
            ResourceProvider = resourceProvider,
            FontResourceId = fontResourceId
        };
        InputTextBox.Bindings.Add(BindingOperations.BindTwoWay(InputTextBox, TextBoxBase.TextProperty, InputValue));

        DiagnosticsText = text.Create("runtime diagnostics: waiting for first frame", 13, new DrawColor(71, 85, 105));

        ActionButton = new Button
        {
            Content = text.Create("Append runtime item", 14, new DrawColor(28, 35, 48)),
            Padding = new Thickness(12, 8, 12, 8),
            Command = appendCommand
        };

        ItemsList = new ListBox { ItemsSource = Items };

        StackPanel content = new()
        {
            Margin = new Thickness(32, 24, 32, 24),
            Orientation = PanelOrientation.Vertical
        };
        content.VisualChildren.Add(text.Create("Runtime Preview", 24));
        content.VisualChildren.Add(text.Create("Scale, resources, input, cursor, clipboard, and retained no-work frames.", 14));
        content.VisualChildren.Add(BuildPreviewCard());
        content.VisualChildren.Add(DiagnosticsText);

        ScrollViewer scrollViewer = new()
        {
            Content = content,
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto
        };

        RootElement = scrollViewer;
        return scrollViewer;
    }

    public void UpdateFrame(UiFrame? frame)
    {
        if (frame is null || DiagnosticsText is null)
        {
            return;
        }

        UIRoot root = RootElement?.Root ?? new UIRoot(frame.Viewport.Width, frame.Viewport.Height, frame.Viewport.Scale);
        RuntimeDiagnosticsSnapshot snapshot = RuntimeDiagnostics.Capture(root, frame.Viewport, frame.Stats);
        DiagnosticsText.Text = RuntimeDiagnostics.Format(snapshot);
    }

    private UIElement BuildPreviewCard()
    {
        StackPanel panel = new()
        {
            Orientation = PanelOrientation.Vertical
        };
        panel.VisualChildren.Add(PreviewImage!);
        panel.VisualChildren.Add(InputTextBox!);
        panel.VisualChildren.Add(ActionButton!);
        panel.VisualChildren.Add(ItemsList!);

        return new Border
        {
            Margin = new Thickness(0, 12, 0, 12),
            Padding = new Thickness(14),
            BorderThickness = new Thickness(1),
            Child = panel
        };
    }

    private void AppendItem()
    {
        string value = InputValue.Value.Trim();
        if (value.Length == 0)
        {
            return;
        }

        Items.Add(value);
    }

    private sealed record SampleImage(int Width, int Height) : IDrawImage;
}
