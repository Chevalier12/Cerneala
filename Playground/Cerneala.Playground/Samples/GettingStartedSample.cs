#nullable enable

using Cerneala.UI.Controls;
using Cerneala.UI.Data;
using Cerneala.UI.Elements;
using Cerneala.UI.Input;
using Cerneala.UI.Layout;
using Cerneala.UI.Layout.Panels;
using Cerneala.UI.Resources;

namespace Cerneala.Playground.Samples;

public sealed class GettingStartedSample : IPlaygroundSample
{
    private readonly PlaygroundText text;
    private readonly IResourceProvider? resourceProvider;
    private readonly ResourceId<FontResource>? fontResourceId;

    public GettingStartedSample(IResourceProvider? resourceProvider = null, ResourceId<FontResource>? fontResourceId = null)
    {
        this.resourceProvider = resourceProvider;
        this.fontResourceId = fontResourceId;
        text = new PlaygroundText(resourceProvider, fontResourceId);
        AddCommand = new ActionCommand(_ => AddItem(), _ => !string.IsNullOrWhiteSpace(EntryText.Value));
        EntryText.ValueChanged += (_, args) =>
        {
            StatusText.Value = string.IsNullOrWhiteSpace(args.NewValue)
                ? "Type a value to enable add."
                : $"Ready to add {args.NewValue}.";
            AddCommand.RaiseCanExecuteChanged();
        };
    }

    public string Name => "Getting Started";

    public ObservableValue<string> EntryText { get; } = new(string.Empty);

    public ObservableValue<string> StatusText { get; } = new("Type a value to enable add.");

    public ObservableList<string> Items { get; } = new(["First item", "Second item"]);

    public ActionCommand AddCommand { get; }

    public TextBox? EntryTextBox { get; private set; }

    public Button? AddButton { get; private set; }

    public TextBlock? StatusBlock { get; private set; }

    public ListBox? ListBox { get; private set; }

    public Grid? LayoutGrid { get; private set; }

    public UIElement? RootElement { get; private set; }

    public UIElement Build()
    {
        EntryText.Value = string.Empty;
        StatusText.Value = "Type a value to enable add.";

        LayoutGrid = new Grid
        {
            Margin = new Thickness(32, 24, 32, 24)
        };
        LayoutGrid.RowDefinitions.Add(new RowDefinition(GridLength.Pixels(42)));
        LayoutGrid.RowDefinitions.Add(new RowDefinition(GridLength.Pixels(44)));
        LayoutGrid.RowDefinitions.Add(new RowDefinition(GridLength.Pixels(42)));
        LayoutGrid.RowDefinitions.Add(new RowDefinition(GridLength.Pixels(40)));
        LayoutGrid.RowDefinitions.Add(new RowDefinition(GridLength.Star));

        TextBlock title = text.Create("Getting started", 20);
        Grid.SetRow(title, 0);

        EntryTextBox = new TextBox
        {
            Padding = new Thickness(6, 4, 6, 4),
            ResourceProvider = resourceProvider,
            FontResourceId = fontResourceId
        };
        EntryTextBox.Bindings.Add(BindingOperations.BindTwoWay(EntryTextBox, TextBoxBase.TextProperty, EntryText));
        Grid.SetRow(EntryTextBox, 1);

        AddButton = new Button
        {
            Content = text.Create("Add item", 14),
            Padding = new Thickness(12, 8, 12, 8),
            Command = AddCommand
        };
        Grid.SetRow(AddButton, 2);

        StatusBlock = text.Create(string.Empty, 14);
        StatusBlock.Bindings.Add(BindingOperations.BindOneWay(StatusBlock, TextBlock.TextProperty, StatusText));
        Grid.SetRow(StatusBlock, 3);

        ListBox = new ListBox
        {
            ItemsSource = Items
        };
        Grid.SetRow(ListBox, 4);

        LayoutGrid.VisualChildren.Add(title);
        LayoutGrid.VisualChildren.Add(EntryTextBox);
        LayoutGrid.VisualChildren.Add(AddButton);
        LayoutGrid.VisualChildren.Add(StatusBlock);
        LayoutGrid.VisualChildren.Add(ListBox);
        RootElement = LayoutGrid;
        return LayoutGrid;
    }

    private void AddItem()
    {
        string value = EntryText.Value.Trim();
        if (value.Length == 0)
        {
            return;
        }

        Items.Add(value);
        EntryText.Value = string.Empty;
        StatusText.Value = $"Added {value}.";
        AddCommand.RaiseCanExecuteChanged();
    }
}
