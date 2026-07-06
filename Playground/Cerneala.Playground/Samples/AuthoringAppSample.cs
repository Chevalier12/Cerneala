#nullable enable

using Cerneala.UI.Controls;
using Cerneala.UI.Data;
using Cerneala.UI.Elements;
using Cerneala.UI.Input;
using Cerneala.UI.Layout;
using Cerneala.UI.Resources;
using PanelOrientation = Cerneala.UI.Layout.Panels.Orientation;

namespace Cerneala.Playground.Samples;

public sealed class AuthoringAppSample : IPlaygroundSample
{
    private readonly PlaygroundText text;
    private readonly IResourceProvider? resourceProvider;
    private readonly ResourceId<FontResource>? fontResourceId;

    public AuthoringAppSample(IResourceProvider? resourceProvider = null, ResourceId<FontResource>? fontResourceId = null)
    {
        this.resourceProvider = resourceProvider;
        this.fontResourceId = fontResourceId;
        text = new PlaygroundText(resourceProvider, fontResourceId);
        SubmitCommand = new ActionCommand(_ => Submit(), _ => !string.IsNullOrWhiteSpace(NameValue.Value));
        NameValue.ValueChanged += (_, args) =>
        {
            Status.Value = string.IsNullOrWhiteSpace(args.NewValue)
                ? "Type a name to enable submit."
                : $"Ready to add {args.NewValue}.";
            SubmitCommand.RaiseCanExecuteChanged();
        };
    }

    public string Name => "Authoring App";

    public ObservableValue<string> NameValue { get; } = new(string.Empty);

    public ObservableValue<string> Status { get; } = new("Type a name to enable submit.");

    public ObservableList<string> Items { get; } = new(["Ada", "Grace"]);

    public ActionCommand SubmitCommand { get; }

    public TextBox? NameTextBox { get; private set; }

    public Button? SubmitButton { get; private set; }

    public TextBlock? StatusText { get; private set; }

    public ListBox? ListBox { get; private set; }

    public UIElement? RootElement { get; private set; }

    public UIElement Build()
    {
        NameValue.Value = string.Empty;
        Status.Value = "Type a name to enable submit.";

        NameTextBox = new TextBox
        {
            Padding = new Thickness(6, 4, 6, 4),
            ResourceProvider = resourceProvider,
            FontResourceId = fontResourceId
        };
        NameTextBox.Bindings.Add(BindingOperations.BindTwoWay(NameTextBox, TextBoxBase.TextProperty, NameValue));

        SubmitButton = new Button
        {
            Content = text.Create("Add name", 14),
            Padding = new Thickness(12, 8, 12, 8),
            Command = SubmitCommand
        };

        StatusText = text.Create(string.Empty, 14);
        StatusText.Bindings.Add(BindingOperations.BindOneWay(StatusText, TextBlock.TextProperty, Status));

        ListBox = new ListBox { ItemsSource = Items };

        StackPanel root = new()
        {
            Margin = new Thickness(32, 24, 32, 24),
            Orientation = PanelOrientation.Vertical
        };
        root.VisualChildren.Add(text.Create("Authoring preview", 20));
        root.VisualChildren.Add(NameTextBox);
        root.VisualChildren.Add(SubmitButton);
        root.VisualChildren.Add(StatusText);
        root.VisualChildren.Add(ListBox);
        RootElement = root;
        return root;
    }

    private void Submit()
    {
        string value = NameValue.Value.Trim();
        if (value.Length == 0)
        {
            return;
        }

        Items.Add(value);
        Status.Value = $"Added {value}.";
        NameValue.Value = string.Empty;
        SubmitCommand.RaiseCanExecuteChanged();
    }
}
