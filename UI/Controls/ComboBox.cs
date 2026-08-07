using Cerneala.Drawing;
using Cerneala.UI.Aspect;
using Cerneala.UI.Controls.Items;
using Cerneala.UI.Controls.Primitives;
using Cerneala.UI.Controls.Templates;
using Cerneala.UI.Core;
using Cerneala.UI.Elements;
using Cerneala.UI.Input;
using Cerneala.UI.Layout;
using Cerneala.UI.Layout.Panels;
using Cerneala.UI.Media;

namespace Cerneala.UI.Controls;

[TemplatePart("PART_SelectionPresenter", typeof(ContentPresenter))]
[TemplatePart("PART_EditableTextBox", typeof(TextBox))]
[TemplatePart("PART_DropDownToggle", typeof(ToggleButton))]
[TemplatePart("PART_DropDownOverlay", typeof(Overlay))]
[TemplatePart("PART_ItemsPresenter", typeof(ItemsPresenter))]
public class ComboBox : Selector
{
    private static readonly ElementAspect DefaultItemContainerAspect = new(
    [
        new ElementAspectValue(Control.PaddingProperty, new Thickness(6))
    ]);

    private ContentPresenter? selectionPresenter;
    private TextBox? editableTextBox;
    private ToggleButton? dropDownToggle;
    private Overlay? dropDownOverlay;
    private bool synchronizingParts;
    private bool preserveTextWhileDeselecting;

    public ComboBox()
    {
        Focusable = true;
        IsTabStop = true;
        ItemsPanel = new ItemsPanelTemplate(() => new StackPanel());
        Handlers.AddHandler(InputEvents.KeyDownEvent, OnKeyDown);
        SetValue(BackgroundProperty, new SolidColorBrush(Color.White), UiPropertyValueSource.AspectBase);
        SetValue(
            BorderBrushProperty,
            new SolidColorBrush(new Color(120, 130, 145)),
            UiPropertyValueSource.AspectBase);
        SetValue(ItemContainerAspectProperty, DefaultItemContainerAspect, UiPropertyValueSource.AspectBase);
        SetValue(ComponentTemplateProperty, ComboBoxTemplates.Default, UiPropertyValueSource.AspectBase);
    }

    public static readonly RoutedEvent DropDownOpenedEvent = RoutedEventRegistry.Register(
        nameof(DropDownOpened),
        typeof(ComboBox),
        RoutingStrategy.Bubble,
        typeof(RoutedEventArgs));

    public static readonly RoutedEvent DropDownClosedEvent = RoutedEventRegistry.Register(
        nameof(DropDownClosed),
        typeof(ComboBox),
        RoutingStrategy.Bubble,
        typeof(RoutedEventArgs));

    public static readonly UiProperty<bool> IsDropDownOpenProperty = UiProperty<bool>.Register(
        nameof(IsDropDownOpen),
        typeof(ComboBox),
        new UiPropertyMetadata<bool>(false, UiPropertyOptions.AffectsRender | UiPropertyOptions.AffectsInputVisual));

    public static readonly UiProperty<bool> IsEditableProperty = UiProperty<bool>.Register(
        nameof(IsEditable),
        typeof(ComboBox),
        new UiPropertyMetadata<bool>(
            false,
            UiPropertyOptions.AffectsMeasure | UiPropertyOptions.AffectsRender | UiPropertyOptions.AffectsInputVisual));

    public static readonly UiProperty<string> TextProperty = UiProperty<string>.Register(
        nameof(Text),
        typeof(ComboBox),
        new UiPropertyMetadata<string>(
            string.Empty,
            UiPropertyOptions.AffectsMeasure | UiPropertyOptions.AffectsRender | UiPropertyOptions.AffectsSemantics,
            coerceValue: (_, value) => value ?? string.Empty));

    public static readonly UiProperty<float> MaxDropDownHeightProperty = UiProperty<float>.Register(
        nameof(MaxDropDownHeight),
        typeof(ComboBox),
        new UiPropertyMetadata<float>(
            float.PositiveInfinity,
            UiPropertyOptions.AffectsMeasure | UiPropertyOptions.AffectsArrange,
            validateValue: value => value > 0 && !float.IsNaN(value)));

    public event RoutedEventHandler DropDownOpened
    {
        add => AddHandler(DropDownOpenedEvent, value);
        remove => RemoveHandler(DropDownOpenedEvent, value);
    }

    public event RoutedEventHandler DropDownClosed
    {
        add => AddHandler(DropDownClosedEvent, value);
        remove => RemoveHandler(DropDownClosedEvent, value);
    }

    public bool IsDropDownOpen
    {
        get => GetValue(IsDropDownOpenProperty);
        set => SetValue(IsDropDownOpenProperty, value);
    }

    public bool IsEditable
    {
        get => GetValue(IsEditableProperty);
        set => SetValue(IsEditableProperty, value);
    }

    public string Text
    {
        get => GetValue(TextProperty);
        set => SetValue(TextProperty, value ?? string.Empty);
    }

    public float MaxDropDownHeight
    {
        get => GetValue(MaxDropDownHeightProperty);
        set => SetValue(MaxDropDownHeightProperty, value);
    }

    protected override Type DefaultContainerType => typeof(ComboBoxItem);

    protected internal override Type GetContainerTypeForItem(object? item)
    {
        return item is ComboBoxItem ? item.GetType() : typeof(ComboBoxItem);
    }

    protected internal override UIElement CreateItemContainer(int index, object? item)
    {
        return item is ComboBoxItem element ? element : new ComboBoxItem();
    }

    protected internal override void PrepareItemContainer(UIElement container, int index, object? item)
    {
        base.PrepareItemContainer(container, index, item);
    }

    protected override void PrepareItemContent(UIElement container, int index, object? item)
    {
        if (container is not ComboBoxItem comboBoxItem)
        {
            base.PrepareItemContent(container, index, item);
            return;
        }

        ContentPresenter? presenter = comboBoxItem.Content as ContentPresenter;
        if (presenter is null)
        {
            comboBoxItem.Content = null;
            presenter = new ContentPresenter();
        }

        presenter.Content = ItemTemplate is null ? GetItemDisplayText(item) : item;
        presenter.ContentTemplate = ItemTemplate;
        presenter.ContentTemplateKey = ItemTemplateKey;
        presenter.LocalTemplateRegistry = ContentTemplateRegistry;
        presenter.ContentIndex = index;
        presenter.Foreground = Foreground;
        presenter.FontFamily = FontFamily;
        presenter.FontSize = FontSize;
        comboBoxItem.Content = presenter;
    }

    protected internal override void ClearItemContainer(UIElement container)
    {
        if (container is ComboBoxItem { Content: ContentPresenter presenter })
        {
            presenter.Content = null;
            presenter.ContentTemplate = null;
            presenter.ContentTemplateKey = null;
            presenter.LocalTemplateRegistry = null;
            presenter.ContentIndex = -1;
        }

        base.ClearItemContainer(container);
    }

    protected override void SelectContainer(UIElement container)
    {
        base.SelectContainer(container);
        IsDropDownOpen = false;
    }

    protected override void OnTemplateApplied(ComponentTemplateInstance? instance)
    {
        DetachTemplateParts();
        ActivateItemsPresenter(null);
        if (instance is null)
        {
            return;
        }

        selectionPresenter = GetRequiredTemplatePart<ContentPresenter>("PART_SelectionPresenter");
        editableTextBox = GetRequiredTemplatePart<TextBox>("PART_EditableTextBox");
        dropDownToggle = GetRequiredTemplatePart<ToggleButton>("PART_DropDownToggle");
        dropDownOverlay = GetRequiredTemplatePart<Overlay>("PART_DropDownOverlay");
        ItemsPresenter presenter = GetRequiredTemplatePart<ItemsPresenter>("PART_ItemsPresenter");

        ActivateItemsPresenter(presenter);
        dropDownOverlay.PlacementTarget = this;
        dropDownOverlay.IsLightDismissEnabled = true;
        dropDownOverlay.MatchTargetWidth = true;
        dropDownOverlay.MaxHeight = MaxDropDownHeight;
        editableTextBox.TextChanged += OnEditableTextChanged;
        dropDownToggle.Checked += OnDropDownToggleChecked;
        dropDownToggle.Unchecked += OnDropDownToggleUnchecked;
        dropDownOverlay.Opened += OnOverlayOpened;
        dropDownOverlay.Closed += OnOverlayClosed;
        SynchronizeTemplateParts();
    }

    protected override void OnPropertyChanged(UiPropertyChangedEventArgs args)
    {
        base.OnPropertyChanged(args);
        if (ReferenceEquals(args.Property, SelectedIndexProperty))
        {
            if (!preserveTextWhileDeselecting)
            {
                SetCurrentTextFromSelection();
            }

            SynchronizeSelectionPresenter();
        }
        else if (ReferenceEquals(args.Property, TextProperty))
        {
            SynchronizeEditorText();
            if (!synchronizingParts &&
                SelectedIndex >= 0 &&
                !string.Equals(Text, GetItemDisplayText(SelectedItem), StringComparison.Ordinal))
            {
                preserveTextWhileDeselecting = true;
                try
                {
                    SelectedIndex = -1;
                }
                finally
                {
                    preserveTextWhileDeselecting = false;
                }
            }

            SynchronizeSelectionPresenter();
        }
        else if (ReferenceEquals(args.Property, IsDropDownOpenProperty))
        {
            ApplyTemplate();
            SynchronizeDropDownState();
        }
        else if (ReferenceEquals(args.Property, IsEditableProperty))
        {
            SynchronizeEditingMode();
        }
        else if (ReferenceEquals(args.Property, MaxDropDownHeightProperty))
        {
            if (dropDownOverlay is not null)
            {
                dropDownOverlay.MaxHeight = MaxDropDownHeight;
            }
        }
        else if (ReferenceEquals(args.Property, DisplayMemberPathProperty) ||
                 ReferenceEquals(args.Property, ItemTemplateProperty) ||
                 ReferenceEquals(args.Property, ItemTemplateKeyProperty))
        {
            SetCurrentTextFromSelection();
            SynchronizeSelectionPresenter();
        }
        else if (ReferenceEquals(args.Property, IsEnabledProperty) && !IsEnabled)
        {
            IsDropDownOpen = false;
        }
    }

    protected override void OnDetached()
    {
        IsDropDownOpen = false;
        base.OnDetached();
    }

    private void DetachTemplateParts()
    {
        if (editableTextBox is not null)
        {
            editableTextBox.TextChanged -= OnEditableTextChanged;
        }

        if (dropDownToggle is not null)
        {
            dropDownToggle.Checked -= OnDropDownToggleChecked;
            dropDownToggle.Unchecked -= OnDropDownToggleUnchecked;
        }

        if (dropDownOverlay is not null)
        {
            dropDownOverlay.Opened -= OnOverlayOpened;
            dropDownOverlay.Closed -= OnOverlayClosed;
            dropDownOverlay.IsOpen = false;
        }

        selectionPresenter = null;
        editableTextBox = null;
        dropDownToggle = null;
        dropDownOverlay = null;
    }

    private void SynchronizeTemplateParts()
    {
        SynchronizeEditingMode();
        SynchronizeEditorText();
        SynchronizeSelectionPresenter();
        SynchronizeDropDownState();
    }

    private void SynchronizeEditingMode()
    {
        if (selectionPresenter is not null)
        {
            selectionPresenter.Visibility = IsEditable
                ? global::Cerneala.UI.Layout.Visibility.Collapsed
                : global::Cerneala.UI.Layout.Visibility.Visible;
        }

        if (editableTextBox is not null)
        {
            editableTextBox.Visibility = IsEditable
                ? global::Cerneala.UI.Layout.Visibility.Visible
                : global::Cerneala.UI.Layout.Visibility.Collapsed;
        }
    }

    private void SynchronizeEditorText()
    {
        if (editableTextBox is null || string.Equals(editableTextBox.Text, Text, StringComparison.Ordinal))
        {
            return;
        }

        synchronizingParts = true;
        try
        {
            editableTextBox.Text = Text;
        }
        finally
        {
            synchronizingParts = false;
        }
    }

    private void SynchronizeSelectionPresenter()
    {
        if (selectionPresenter is null)
        {
            return;
        }

        selectionPresenter.Content = ItemTemplate is null ? Text : SelectedItem;
        selectionPresenter.ContentTemplate = ItemTemplate;
        selectionPresenter.ContentTemplateKey = ItemTemplateKey;
        selectionPresenter.LocalTemplateRegistry = ContentTemplateRegistry;
        selectionPresenter.ContentIndex = SelectedIndex;
    }

    private void SynchronizeDropDownState()
    {
        if (dropDownToggle is not null && dropDownToggle.IsChecked != IsDropDownOpen)
        {
            dropDownToggle.IsChecked = IsDropDownOpen;
        }

        if (dropDownOverlay is not null)
        {
            dropDownOverlay.MaxHeight = MaxDropDownHeight;
            dropDownOverlay.IsOpen = IsDropDownOpen && IsEnabled;
        }
    }

    private void SetCurrentTextFromSelection()
    {
        string value = SelectedIndex >= 0 ? GetItemDisplayText(SelectedItem) : string.Empty;
        if (string.Equals(Text, value, StringComparison.Ordinal))
        {
            return;
        }

        synchronizingParts = true;
        try
        {
            Text = value;
        }
        finally
        {
            synchronizingParts = false;
        }
    }

    private void OnEditableTextChanged(object? sender, TextChangedEventArgs args)
    {
        if (!synchronizingParts && ReferenceEquals(sender, editableTextBox))
        {
            Text = args.NewText;
        }
    }

    private void OnDropDownToggleChecked(UiElementId _, RoutedEventArgs args)
    {
        if (!synchronizingParts)
        {
            IsDropDownOpen = true;
        }
    }

    private void OnDropDownToggleUnchecked(UiElementId _, RoutedEventArgs args)
    {
        if (!synchronizingParts)
        {
            IsDropDownOpen = false;
        }
    }

    private void OnOverlayOpened(UiElementId _, RoutedEventArgs args)
    {
        if (!IsDropDownOpen)
        {
            SetValue(IsDropDownOpenProperty, true);
        }

        RaiseEvent(new RoutedEventArgs(DropDownOpenedEvent, this));
    }

    private void OnOverlayClosed(UiElementId _, RoutedEventArgs args)
    {
        if (IsDropDownOpen)
        {
            SetValue(IsDropDownOpenProperty, false);
        }

        RaiseEvent(new RoutedEventArgs(DropDownClosedEvent, this));
    }

    private void OnKeyDown(UiElementId _, RoutedEventArgs args)
    {
        if (args is not KeyEventArgs keyArgs || !IsEnabled)
        {
            return;
        }

        if (keyArgs.Key == InputKey.F4 || (keyArgs.IsAltDown && keyArgs.Key == InputKey.Down))
        {
            IsDropDownOpen = !IsDropDownOpen;
            args.Handled = true;
            return;
        }

        if (keyArgs.IsAltDown && keyArgs.Key == InputKey.Up)
        {
            IsDropDownOpen = false;
            args.Handled = true;
            return;
        }

        if (keyArgs.Key == InputKey.Escape && IsDropDownOpen)
        {
            IsDropDownOpen = false;
            args.Handled = true;
            return;
        }

        if (keyArgs.Key == InputKey.Enter && IsDropDownOpen)
        {
            IsDropDownOpen = false;
            args.Handled = true;
            return;
        }

        if ((IsEditable && !IsDropDownOpen) ||
            keyArgs.Key is not (InputKey.Up or InputKey.Down or InputKey.Home or InputKey.End) ||
            ItemCount == 0)
        {
            return;
        }

        SelectedIndex = keyArgs.Key switch
        {
            InputKey.Home => 0,
            InputKey.End => ItemCount - 1,
            InputKey.Up => Math.Max(0, SelectedIndex < 0 ? 0 : SelectedIndex - 1),
            _ => Math.Min(ItemCount - 1, SelectedIndex + 1)
        };
        args.Handled = true;
    }
}
