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
using Cerneala.UI.Layout.Virtualization;
using Cerneala.UI.Media;
using System.Globalization;
using System.Runtime.ExceptionServices;

namespace Cerneala.UI.Controls;

[TemplatePart("PART_SelectionPresenter", typeof(ContentPresenter))]
[TemplatePart("PART_EditableTextBox", typeof(TextBox))]
[TemplatePart("PART_DropDownToggle", typeof(ToggleButton))]
[TemplatePart("PART_DropDownOverlay", typeof(Overlay))]
[TemplatePart("PART_ItemsPresenter", typeof(ItemsPresenter))]
public class ComboBox : Selector
{
    private const float DefaultVirtualizedItemExtent = 28;
    private const int DefaultVirtualizationCacheItems = 1;

    private static readonly ElementAspect DefaultItemContainerAspect = new(
    [
        new ElementAspectValue(Control.PaddingProperty, new Thickness(6))
    ]);

    private readonly VirtualizingStackPanel defaultItemsPanel = new();
    private ContentPresenter? selectionPresenter;
    private TextBox? editableTextBox;
    private ToggleButton? dropDownToggle;
    private Overlay? dropDownOverlay;
    private ScrollViewer? dropDownScrollViewer;
    private bool synchronizingParts;
    private bool preserveTextWhileDeselecting;
    private bool updatingTextSearch;
    private readonly ItemTextSearch textSearch = new();
    private List<int>? filteredSourceIndices;
    private string filterText = string.Empty;
    private bool selectionTransactionActive;
    private int previewSelectedIndex = -1;

    // The item the open preview highlights. An item change moves the preview with its item, as WPF moves its
    // highlighted item. A preview index set before its item exists refers to no item.
    private object? previewItem;
    private bool hasPreviewItem;
    private string transactionText = string.Empty;
    private bool transactionTextMatchesPreview;
    private bool suppressAutocompleteForCurrentEdit;

    // True when Text was last set from a SelectedIndex whose item did not exist yet (#776).
    private bool textSetForMissingSelectedItem;

    public ComboBox()
    {
        SetFrameworkDefault(FocusableProperty, true);
        SetFrameworkDefault(IsTabStopProperty, true);
        SetFrameworkDefault(ItemsPanelProperty, defaultItemsPanel);
        Handlers.AddHandler(InputEvents.KeyDownEvent, OnKeyDown);
        Handlers.AddHandler(InputEvents.TextInputEvent, OnTextInput);
        Handlers.AddHandler(InputEvents.MouseUpEvent, OnMouseUp);
        SetFrameworkDefault(BackgroundProperty, new SolidColorBrush(Color.White));
        SetFrameworkDefault(ForegroundProperty, new SolidColorBrush(Color.Black));
        SetFrameworkDefault(BorderBrushProperty, new SolidColorBrush(new Color(120, 130, 145)));
        SetFrameworkDefault(ItemContainerAspectProperty, DefaultItemContainerAspect);
        SetFrameworkDefault(ComponentTemplateProperty, ComboBoxTemplates.Default);
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

    // A disabled ComboBox has no open drop-down: disabling closes it, and opening it while disabled leaves it closed,
    // so IsDropDownOpen, AspectState.Expanded, and the overlay always agree (#841).
    public static readonly UiProperty<bool> IsDropDownOpenProperty = UiProperty<bool>.Register(
        nameof(IsDropDownOpen),
        typeof(ComboBox),
        new UiPropertyMetadata<bool>(
            false,
            UiPropertyOptions.AffectsRender | UiPropertyOptions.AffectsInputVisual | UiPropertyOptions.AffectsAspect,
            coerceValue: (owner, value) => value && ((UIElement)owner).IsEnabled));

    public static readonly UiProperty<bool> IsEditableProperty = UiProperty<bool>.Register(
        nameof(IsEditable),
        typeof(ComboBox),
        new UiPropertyMetadata<bool>(
            true,
            UiPropertyOptions.AffectsMeasure | UiPropertyOptions.AffectsRender | UiPropertyOptions.AffectsInputVisual));

    public static readonly UiProperty<bool> IsReadOnlyProperty = UiProperty<bool>.Register(
        nameof(IsReadOnly),
        typeof(ComboBox),
        new UiPropertyMetadata<bool>(false, UiPropertyOptions.AffectsInputVisual | UiPropertyOptions.AffectsSemantics));

    public static readonly UiProperty<bool> IsTextSearchEnabledProperty = UiProperty<bool>.Register(
        nameof(IsTextSearchEnabled),
        typeof(ComboBox),
        new UiPropertyMetadata<bool>(true, UiPropertyOptions.AffectsInputVisual));

    public static readonly UiProperty<bool> IsTextSearchCaseSensitiveProperty = UiProperty<bool>.Register(
        nameof(IsTextSearchCaseSensitive),
        typeof(ComboBox),
        new UiPropertyMetadata<bool>(false, UiPropertyOptions.AffectsInputVisual));

    public static readonly UiProperty<bool> ShouldPreserveUserEnteredPrefixProperty = UiProperty<bool>.Register(
        nameof(ShouldPreserveUserEnteredPrefix),
        typeof(ComboBox),
        new UiPropertyMetadata<bool>(false, UiPropertyOptions.AffectsInputVisual));

    public static readonly UiProperty<bool> IsTextFilterEnabledProperty = UiProperty<bool>.Register(
        nameof(IsTextFilterEnabled),
        typeof(ComboBox),
        new UiPropertyMetadata<bool>(true, UiPropertyOptions.AffectsMeasure | UiPropertyOptions.AffectsRender));

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
            300,
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

    public bool IsReadOnly
    {
        get => GetValue(IsReadOnlyProperty);
        set => SetValue(IsReadOnlyProperty, value);
    }

    public bool IsTextSearchEnabled
    {
        get => GetValue(IsTextSearchEnabledProperty);
        set => SetValue(IsTextSearchEnabledProperty, value);
    }

    public bool IsTextSearchCaseSensitive
    {
        get => GetValue(IsTextSearchCaseSensitiveProperty);
        set => SetValue(IsTextSearchCaseSensitiveProperty, value);
    }

    public bool ShouldPreserveUserEnteredPrefix
    {
        get => GetValue(ShouldPreserveUserEnteredPrefixProperty);
        set => SetValue(ShouldPreserveUserEnteredPrefixProperty, value);
    }

    public bool IsTextFilterEnabled
    {
        get => GetValue(IsTextFilterEnabledProperty);
        set => SetValue(IsTextFilterEnabledProperty, value);
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

    // The non-editable field takes its press, as the WPF template's ToggleButton does (see OnMouseUp).
    internal override bool OwnsPointerClick => base.OwnsPointerClick || (!IsEditable && IsEnabled);

    internal override int ViewItemCount => filteredSourceIndices?.Count ?? base.ViewItemCount;

    internal override int GetSourceIndexForViewIndex(int viewIndex)
    {
        return filteredSourceIndices?[viewIndex] ?? base.GetSourceIndexForViewIndex(viewIndex);
    }

    protected internal override bool IsItemSelected(int index)
    {
        return selectionTransactionActive ? previewSelectedIndex == index : base.IsItemSelected(index);
    }

    protected internal override Type GetContainerTypeForItem(object? item)
    {
        return item is ComboBoxItem ? item.GetType() : typeof(ComboBoxItem);
    }

    protected internal override UIElement CreateItemContainer(int index, object? item)
    {
        return item is ComboBoxItem element ? element : new ComboBoxItem();
    }

    internal override bool IsItemItsOwnContainer(object? item)
    {
        return item is ComboBoxItem;
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

        // All five values go in before the presenter rebuilds its child on its next measure, so the row presents
        // its item once, with its index (#574).
        presenter.PrepareItemPresentation(
            ItemTemplate is null ? GetRowContent(item) : item,
            ItemTemplate,
            ItemTemplateKey,
            ContentTemplateRegistry,
            index);
        presenter.Foreground = Foreground;
        presenter.FontFamily = FontFamily;
        presenter.FontSize = FontSize;
        comboBoxItem.Content = presenter;
    }

    // As in WPF, an element item that is not a ComboBoxItem is the content of its generated row, so the row presents
    // the element itself (#606). Any other display value is presented as its text.
    private object GetRowContent(object? item)
    {
        object? value = GetItemDisplayValue(item);
        return value as UIElement ?? (object)(value?.ToString() ?? string.Empty);
    }

    protected internal override void ClearItemContainer(UIElement container)
    {
        if (container is ComboBoxItem { Content: ContentPresenter presenter } &&
            !ReferenceEquals(container, ItemContainerGenerator.GetItem(container)))
        {
            presenter.PrepareItemPresentation(null, null, null, null, -1);
        }

        base.ClearItemContainer(container);
    }

    protected override void SelectContainer(UIElement container)
    {
        int sourceIndex = ItemContainerGenerator.IndexFromContainer(container);
        if (sourceIndex < 0)
        {
            return;
        }

        if (!selectionTransactionActive)
        {
            base.SelectContainer(container);
            IsDropDownOpen = false;
            return;
        }

        // A failed re-preparation of the old or the clicked row does not stop the click: it still commits the
        // clicked item and closes the drop-down, and only then the first failure propagates.
        ExceptionDispatchInfo? preparationFailure = null;
        try
        {
            UpdatePreviewSelection(sourceIndex, updateEditor: true);
        }
        catch (Exception exception)
        {
            preparationFailure = ExceptionDispatchInfo.Capture(exception);
        }

        try
        {
            CommitSelectionTransactionAndCloseDropDown();
        }
        catch (Exception) when (preparationFailure is not null)
        {
            // The first failure propagates below.
        }

        preparationFailure?.Throw();
    }

    internal override void OnItemsViewSourceChanged()
    {
        RebuildFilteredView();
        if (previewSelectedIndex >= ItemCount ||
            (previewSelectedIndex >= 0 && !IsSourceIndexVisible(previewSelectedIndex)))
        {
            MovePreviewToFirstVisibleItem();
        }

        // A SelectedIndex set before its item exists keeps its value when the item arrives, so no SelectedIndex
        // change sets Text; Text and the selection box follow the item it now refers to (#776).
        if (textSetForMissingSelectedItem && SelectedIndex >= 0 && SelectedIndex < ItemCount)
        {
            SetCurrentTextFromSelection();
            SynchronizeSelectionPresenter();
        }

        ResetDropDownVirtualization();
    }

    // The preview follows its item before the selection does, as in WPF.
    internal override void OnItemsReplaced(int index, int removedCount, int addedCount)
    {
        if (hasPreviewItem && previewSelectedIndex >= index)
        {
            MovePreviewForItems(previewSelectedIndex < index + removedCount
                ? -1
                : previewSelectedIndex - removedCount + addedCount);
        }

        CapturePreviewItem();
        base.OnItemsReplaced(index, removedCount, addedCount);
    }

    internal override void OnItemsMoved(int oldIndex, int newIndex, int count)
    {
        if (hasPreviewItem)
        {
            MovePreviewForItems(GetIndexAfterMove(previewSelectedIndex, oldIndex, newIndex, count));
        }

        CapturePreviewItem();
        base.OnItemsMoved(oldIndex, newIndex, count);
    }

    internal override void OnItemsReset()
    {
        if (hasPreviewItem)
        {
            MovePreviewForItems(FindItemIndex(previewItem, previewSelectedIndex));
        }

        CapturePreviewItem();
        base.OnItemsReset();
    }

    internal override void OnItemsPresenterPanelChanged()
    {
        ResetDropDownVirtualization();
    }

    protected override void OnTemplateApplied(ComponentTemplateInstance? instance)
    {
        DetachTemplateParts();
        ActivateItemsPresenter(null);
        SetVirtualizationContext(null);
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
        dropDownScrollViewer = (dropDownOverlay.Content as Border)?.Child as ScrollViewer;
        editableTextBox.TextChanged += OnEditableTextChanged;
        editableTextBox.Handlers.AddHandler(InputEvents.PreviewKeyDownEvent, OnEditablePreviewKeyDown);
        editableTextBox.Handlers.AddHandler(InputEvents.PreviewKeyUpEvent, OnEditablePreviewKeyUp);
        dropDownToggle.Checked += OnDropDownToggleChecked;
        dropDownToggle.Unchecked += OnDropDownToggleUnchecked;
        dropDownOverlay.Opened += OnOverlayOpened;
        dropDownOverlay.Closed += OnOverlayClosed;
        if (dropDownScrollViewer is not null)
        {
            dropDownScrollViewer.ScrollChanged += OnDropDownScrollChanged;
        }

        ResetDropDownVirtualization();
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

            // An item change moves or clears the committed selection; the open preview stays the user's choice.
            if (selectionTransactionActive && !updatingTextSearch && !IsChangingSelectionForItems)
            {
                transactionText = Text;
                transactionTextMatchesPreview = SelectedIndex >= 0;
                UpdatePreviewSelection(SelectedIndex, updateEditor: SelectedIndex >= 0);
            }

            SynchronizeSelectionPresenter();
        }
        else if (ReferenceEquals(args.Property, TextProperty))
        {
            if (selectionTransactionActive && !synchronizingParts && !updatingTextSearch)
            {
                transactionText = Text;
            }

            SynchronizeEditorText();
            if (!synchronizingParts && !updatingTextSearch)
            {
                int exactMatch = IsTextSearchEnabled ? FindMatchingItem(Text, exactMatch: true) : -1;
                if (exactMatch >= 0)
                {
                    ApplyExactTextMatch(exactMatch);
                }
                else if (SelectedIndex >= 0 &&
                    !string.Equals(Text, GetItemText(SelectedItem), StringComparison.Ordinal))
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
            }

            SynchronizeSelectionPresenter();
        }
        else if (ReferenceEquals(args.Property, IsDropDownOpenProperty))
        {
            // A failed re-preparation does not stop the drop-down change: the overlay still follows
            // IsDropDownOpen before the failure propagates.
            try
            {
                if (IsDropDownOpen)
                {
                    BeginSelectionTransaction();
                }
                else
                {
                    TakeFocusFromDropDown();
                    CancelSelectionTransaction();
                }
            }
            finally
            {
                ApplyTemplate();
                SynchronizeDropDownState();
            }
        }
        else if (ReferenceEquals(args.Property, IsEditableProperty) ||
                 ReferenceEquals(args.Property, IsReadOnlyProperty))
        {
            if (!IsEditable)
            {
                ClearFilter();
            }

            SynchronizeEditingMode();
        }
        else if (ReferenceEquals(args.Property, MaxDropDownHeightProperty))
        {
            if (dropDownOverlay is not null)
            {
                dropDownOverlay.MaxHeight = MaxDropDownHeight;
            }

            ResetDropDownVirtualization();
        }
        else if (ReferenceEquals(args.Property, DisplayMemberPathProperty) ||
                 ReferenceEquals(args.Property, ItemTemplateProperty) ||
                 ReferenceEquals(args.Property, ItemTemplateKeyProperty))
        {
            RefreshFilterForTextPolicyChange();
            SetCurrentTextFromSelection();
            SynchronizeSelectionPresenter();
        }
        else if (ReferenceEquals(args.Property, ItemsPanelProperty))
        {
            ResetDropDownVirtualization();
        }
        else if (ReferenceEquals(args.Property, TextSearch.TextPathProperty))
        {
            ResetTextSearchPrefix();
            RefreshFilterForTextPolicyChange();
            SetCurrentTextFromSelection();
            SynchronizeSelectionPresenter();
        }
        else if (ReferenceEquals(args.Property, IsTextSearchEnabledProperty) ||
                 ReferenceEquals(args.Property, IsTextSearchCaseSensitiveProperty))
        {
            ResetTextSearchPrefix();
            RefreshFilterForTextPolicyChange();
        }
        else if (ReferenceEquals(args.Property, IsTextFilterEnabledProperty))
        {
            if (IsTextFilterEnabled && selectionTransactionActive)
            {
                SetFilterText(editableTextBox?.Text ?? transactionText);
            }
            else
            {
                ClearFilter();
            }
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

    // As WPF's ComboBox focuses itself when its drop-down closes with the keyboard focus inside the drop-down, an
    // element in the drop-down that took the focus, such as a focusable ListBoxItem given as an item (#662), does not
    // keep it in the closed drop-down.
    private void TakeFocusFromDropDown()
    {
        if (dropDownOverlay?.Content is not UIElement dropDownContent ||
            Root is not UIRoot { ActiveFocusManager: { FocusedElement: { } focused } focusManager } root)
        {
            return;
        }

        for (UIElement? current = focused; current is not null; current = current.VisualParent)
        {
            if (ReferenceEquals(current, dropDownContent))
            {
                focusManager.Focus(this, root.InputCache.EnsureCurrent(root));
                return;
            }
        }
    }

    private void DetachTemplateParts()
    {
        if (editableTextBox is not null)
        {
            editableTextBox.TextChanged -= OnEditableTextChanged;
            editableTextBox.Handlers.RemoveHandler(InputEvents.PreviewKeyDownEvent, OnEditablePreviewKeyDown);
            editableTextBox.Handlers.RemoveHandler(InputEvents.PreviewKeyUpEvent, OnEditablePreviewKeyUp);
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

        if (dropDownScrollViewer is not null)
        {
            dropDownScrollViewer.ScrollChanged -= OnDropDownScrollChanged;
        }

        selectionPresenter = null;
        editableTextBox = null;
        dropDownToggle = null;
        dropDownOverlay = null;
        dropDownScrollViewer = null;
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
            editableTextBox.IsReadOnly = IsReadOnly;
            editableTextBox.Visibility = IsEditable
                ? global::Cerneala.UI.Layout.Visibility.Visible
                : global::Cerneala.UI.Layout.Visibility.Collapsed;
        }
    }

    private void SynchronizeEditorText()
    {
        string value = selectionTransactionActive ? transactionText : Text;
        if (editableTextBox is null || string.Equals(editableTextBox.Text, value, StringComparison.Ordinal))
        {
            return;
        }

        bool wasSynchronizing = synchronizingParts;
        synchronizingParts = true;
        try
        {
            editableTextBox.Text = value;
        }
        finally
        {
            synchronizingParts = wasSynchronizing;
        }
    }

    private void SynchronizeSelectionPresenter()
    {
        if (selectionPresenter is null)
        {
            return;
        }

        int presentedIndex = selectionTransactionActive ? previewSelectedIndex : SelectedIndex;
        bool hasPresentedItem = presentedIndex >= 0 && presentedIndex < ItemCount;
        object? presentedItem = hasPresentedItem ? GetItemAt(presentedIndex) : null;
        string presentedText = selectionTransactionActive && presentedIndex >= 0
            ? GetItemText(presentedItem)
            : Text;

        // As WPF's ComboBox.UpdateSelectionBoxItem shows a missing item as text, the ItemTemplate presents only an
        // item: with no item, the selection box presents the text as it does without an ItemTemplate. All five
        // values go in before the presenter rebuilds its child on its next measure, so the selection box presents
        // its item once, with its index (#623).
        ContentTemplate? itemTemplate = hasPresentedItem ? ItemTemplate : null;
        selectionPresenter.PrepareItemPresentation(
            itemTemplate is null ? presentedText : presentedItem,
            itemTemplate,
            ItemTemplateKey,
            ContentTemplateRegistry,
            presentedIndex);
    }

    private void SynchronizeDropDownState()
    {
        if (dropDownToggle is not null && dropDownToggle.IsChecked != IsDropDownOpen)
        {
            SynchronizeDropDownToggleState();
        }

        if (dropDownOverlay is not null)
        {
            dropDownOverlay.MaxHeight = MaxDropDownHeight;
            dropDownOverlay.IsOpen = IsDropDownOpen && IsEnabled;
        }
    }

    private void SetCurrentTextFromSelection()
    {
        textSetForMissingSelectedItem = SelectedIndex >= ItemCount;
        string value = SelectedIndex >= 0 ? GetItemText(SelectedItem) : string.Empty;
        if (string.Equals(Text, value, StringComparison.Ordinal))
        {
            return;
        }

        bool wasSynchronizing = synchronizingParts;
        synchronizingParts = true;
        try
        {
            Text = value;
        }
        finally
        {
            synchronizingParts = wasSynchronizing;
        }
    }

    private void OnEditableTextChanged(object? sender, TextChangedEventArgs args)
    {
        if (synchronizingParts || updatingTextSearch || sender is not TextBox editor ||
            !ReferenceEquals(editor, editableTextBox))
        {
            return;
        }

        string enteredText = args.NewText;
        bool suppressAutocomplete = suppressAutocompleteForCurrentEdit;
        suppressAutocompleteForCurrentEdit = false;
        if (IsReadOnly)
        {
            return;
        }

        if (IsTextFilterEnabled && !IsDropDownOpen)
        {
            IsDropDownOpen = true;
        }

        if (IsTextFilterEnabled)
        {
            SetFilterText(enteredText);
        }

        bool canComplete = !suppressAutocomplete &&
            IsTextSearchEnabled && enteredText.Length > 0 &&
            editor.Selection.Active == enteredText.Length;
        int matchIndex = canComplete ? FindMatchingItem(enteredText, exactMatch: false) : -1;

        if (selectionTransactionActive)
        {
            transactionText = enteredText;
            transactionTextMatchesPreview = false;
            // Previewing the first filtered result is automatic matching, so with text search off typing previews
            // no item and a commit keeps the typed text (#942).
            // Without the filter there is no filtered result, so unmatched text previews no item (#840).
            int previewIndex = enteredText.Length == 0 || !IsTextSearchEnabled
                ? -1
                : matchIndex >= 0
                ? matchIndex
                : filteredSourceIndices is { Count: > 0 } ? filteredSourceIndices[0] : -1;
            UpdatePreviewSelection(previewIndex, updateEditor: false);
            if (matchIndex >= 0)
            {
                CompleteEditorText(editor, enteredText, matchIndex);
            }

            return;
        }

        if (matchIndex < 0)
        {
            Text = enteredText;
            return;
        }

        updatingTextSearch = true;
        try
        {
            string completedText = GetCompletedText(enteredText, matchIndex);
            SelectedIndex = matchIndex;
            Text = completedText;
            editor.Text = completedText;
            editor.Select(enteredText.Length, completedText.Length);
        }
        finally
        {
            updatingTextSearch = false;
        }
    }

    private void OnTextInput(object _, RoutedEventArgs args)
    {
        if (args is not TextCompositionEventArgs textArgs || textArgs.Handled ||
            !IsEnabled || IsEditable || !IsTextSearchEnabled)
        {
            return;
        }

        string input = TextInputCore.NormalizeSingleLineInput(textArgs.Text);
        if (input.Length == 0)
        {
            return;
        }

        int matchIndex = textSearch.Search(this, input, IsTextSearchCaseSensitive);
        if (matchIndex >= 0)
        {
            if (selectionTransactionActive)
            {
                UpdatePreviewSelection(matchIndex, updateEditor: true);
            }
            else
            {
                SelectedIndex = matchIndex;
            }
        }

        textArgs.Handled = true;
    }

    private void OnEditablePreviewKeyDown(object _, RoutedEventArgs args)
    {
        suppressAutocompleteForCurrentEdit = args is KeyEventArgs
        {
            Key: InputKey.Back or InputKey.Delete
        };
    }

    private void OnEditablePreviewKeyUp(object _, RoutedEventArgs args)
    {
        suppressAutocompleteForCurrentEdit = false;
    }

    private void OnDropDownToggleChecked(object _, RoutedEventArgs args)
    {
        if (!synchronizingParts)
        {
            ClearFilter();
            IsDropDownOpen = true;
        }
    }

    private void OnDropDownToggleUnchecked(object _, RoutedEventArgs args)
    {
        if (synchronizingParts)
        {
            return;
        }

        if (IsDropDownOpen && filterText.Length > 0)
        {
            ClearFilter();
            SynchronizeDropDownToggleState();
            return;
        }

        IsDropDownOpen = false;
    }

    private void SynchronizeDropDownToggleState()
    {
        if (dropDownToggle is null || dropDownToggle.IsChecked == IsDropDownOpen)
        {
            return;
        }

        bool wasSynchronizing = synchronizingParts;
        synchronizingParts = true;
        try
        {
            dropDownToggle.IsChecked = IsDropDownOpen;
        }
        finally
        {
            synchronizingParts = wasSynchronizing;
        }
    }

    // The overlay is already projected or withdrawn, so the event is raised even when starting or canceling the
    // selection transaction rethrows a re-preparation failure. The failure propagates after the event.
    private void OnOverlayOpened(object _, RoutedEventArgs args)
    {
        ResetDropDownVirtualization();
        try
        {
            if (!IsDropDownOpen)
            {
                SetValue(IsDropDownOpenProperty, true);
            }
        }
        finally
        {
            RaiseEvent(new RoutedEventArgs(DropDownOpenedEvent, this));
        }
    }

    private void OnOverlayClosed(object _, RoutedEventArgs args)
    {
        try
        {
            if (IsDropDownOpen)
            {
                SetValue(IsDropDownOpenProperty, false);
            }
        }
        finally
        {
            RaiseEvent(new RoutedEventArgs(DropDownClosedEvent, this));
        }
    }

    private void OnKeyDown(object _, RoutedEventArgs args)
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
            CommitSelectionTransactionAndCloseDropDown();
            args.Handled = true;
            return;
        }

        if (keyArgs.Key == InputKey.Tab && IsDropDownOpen)
        {
            CommitSelectionTransactionAndCloseDropDown();
            return;
        }

        if ((IsEditable && !IsDropDownOpen) ||
            keyArgs.Key is not (InputKey.Up or InputKey.Down or InputKey.Home or InputKey.End) ||
            ViewItemCount == 0)
        {
            return;
        }

        if (selectionTransactionActive)
        {
            NavigatePreview(keyArgs.Key);
            args.Handled = true;
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

    // A non-editable field acts as one drop-down button, as the WPF default template's ToggleButton covers the
    // whole field. Content in the field that the input bridge gives its own pointer input owns its click: a
    // command source, such as PART_DropDownToggle or a button that a hit-testable selection box shows, as the
    // input bridge resolves the click to it, and a pointer drag source, such as a TextBox or a Thumb there. The
    // projected list is not in the field's visual subtree. As the WPF ToggleButton handles the field's click, the
    // toggle marks the MouseUp handled, so an ancestor command source, such as a Button whose Content is this
    // ComboBox, does not also click or run its command. Inside such a command source the input bridge resolves the
    // press and the release to that command source, so the field also checks that the press hit the same element as
    // the release: a press on the outer Button followed by a release on the field clicks only the outer Button, as
    // the WPF ToggleButton never sees that press (#468). The click owner and the drag source are found with the route
    // map the input bridge routed the MouseUp with, so they stay the same when an earlier handler on the route removes
    // the clicked content from the field (#622).
    private void OnMouseUp(object _, RoutedEventArgs args)
    {
        if (IsEditable || !IsEnabled ||
            args is not MouseButtonEventArgs { ChangedButton: InputMouseButton.Left, ClickCount: > 0 } mouseArgs ||
            !ReferenceEquals(mouseArgs.PressOriginalSource, args.OriginalSource) ||
            !ReferenceEquals(FindClickOwner(this, args.OriginalSource, mouseArgs.RouteMap), this) ||
            IsInPointerDragSource(args.OriginalSource, mouseArgs.RouteMap))
        {
            return;
        }

        IsDropDownOpen = !IsDropDownOpen;
        args.Handled = true;
    }

    // With routeMap, source and its visual parents are those of the route map the input event was routed with, as in
    // Selector.FindClickOwner.
    private bool IsInPointerDragSource(object? source, ElementInputRouteMap? routeMap)
    {
        if (routeMap is not null)
        {
            UIElement? routed = source as UIElement;

            if (routed is null)
            {
                return false;
            }

            foreach (UIElement current in routeMap.GetVisualRouteToRoot(routed))
            {
                if (ReferenceEquals(current, this))
                {
                    return false;
                }

                if (current is IPointerDragSource)
                {
                    return true;
                }
            }

            return false;
        }

        UIElement? element = source as UIElement;

        for (UIElement? current = element; current is not null && !ReferenceEquals(current, this); current = current.VisualParent)
        {
            if (current is IPointerDragSource)
            {
                return true;
            }
        }

        return false;
    }

    private void ApplyExactTextMatch(int matchIndex)
    {
        updatingTextSearch = true;
        try
        {
            if (SelectedIndex != matchIndex)
            {
                SelectedIndex = matchIndex;
            }
            else
            {
                SetCurrentTextFromSelection();
            }
        }
        finally
        {
            updatingTextSearch = false;
        }

        if (selectionTransactionActive)
        {
            UpdatePreviewSelection(matchIndex, updateEditor: true);
        }
    }

    private int FindMatchingItem(string searchText, bool exactMatch)
    {
        return ItemTextSearch.FindMatchingItem(this, searchText, exactMatch, IsTextSearchCaseSensitive);
    }

    private string GetItemText(object? item)
    {
        return ItemTextSearch.GetItemText(this, item);
    }

    private void ResetTextSearchPrefix()
    {
        textSearch.Reset();
    }

    private void BeginSelectionTransaction()
    {
        if (selectionTransactionActive)
        {
            return;
        }

        selectionTransactionActive = true;
        previewSelectedIndex = SelectedIndex;
        CapturePreviewItem();
        transactionText = Text;
        transactionTextMatchesPreview = SelectedIndex >= 0;
        ExceptionDispatchInfo? preparationFailure = null;
        RefreshPreviewContainer(previewSelectedIndex, ref preparationFailure);
        SynchronizeSelectionPresenter();
        // A drop-down opened while SelectedIndex changes leaves the failure to that selection change.
        ThrowPreparationFailure(preparationFailure);
    }

    // Selector rethrows a failed re-preparation of the old or the new row from the SelectedIndex setter, when
    // SelectedIndex already holds the committed index. The commit then still finishes: the caller clears the filter
    // and synchronizes the editor, the caret, and the selection box, and only then rethrows the first failure.
    private void CommitSelectedIndex(int committedIndex, ref ExceptionDispatchInfo? preparationFailure)
    {
        try
        {
            if (committedIndex >= 0)
            {
                SelectedIndex = committedIndex;
            }
            else if (IsEditable)
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
        }
        catch (Exception exception) when (SelectedIndex == committedIndex)
        {
            preparationFailure ??= ExceptionDispatchInfo.Capture(exception);
        }
    }

    private void CommitSelectionTransaction()
    {
        if (!selectionTransactionActive)
        {
            return;
        }

        int committedIndex = previewSelectedIndex;
        string committedText = previewSelectedIndex >= 0 && !transactionTextMatchesPreview
            ? GetItemText(GetItemAt(previewSelectedIndex))
            : transactionText;
        int oldPreviewIndex = previewSelectedIndex;
        selectionTransactionActive = false;
        previewSelectedIndex = -1;
        CapturePreviewItem();
        transactionTextMatchesPreview = false;
        ExceptionDispatchInfo? preparationFailure = null;
        updatingTextSearch = true;
        try
        {
            CommitSelectedIndex(committedIndex, ref preparationFailure);
            if (IsEditable)
            {
                Text = committedText;
            }
        }
        finally
        {
            updatingTextSearch = false;
        }

        ClearFilter();
        RefreshPreviewContainer(oldPreviewIndex, ref preparationFailure);
        SynchronizeEditorText();
        editableTextBox?.MoveCaret(editableTextBox.Text.Length);
        SynchronizeSelectionPresenter();
        // A commit made while SelectedIndex changes leaves the failure to that selection change.
        ThrowPreparationFailure(preparationFailure);
    }

    // Enter, Tab, and item clicks commit the preview and close the drop-down. A failed re-preparation during the
    // commit does not stop the command: the drop-down still closes before the failure propagates, so an open
    // drop-down always has a selection transaction.
    private void CommitSelectionTransactionAndCloseDropDown()
    {
        try
        {
            CommitSelectionTransaction();
        }
        finally
        {
            IsDropDownOpen = false;
        }
    }

    private void CancelSelectionTransaction()
    {
        if (!selectionTransactionActive)
        {
            return;
        }

        int oldPreviewIndex = previewSelectedIndex;
        selectionTransactionActive = false;
        previewSelectedIndex = -1;
        CapturePreviewItem();
        transactionText = Text;
        transactionTextMatchesPreview = false;
        ClearFilter();
        ExceptionDispatchInfo? preparationFailure = null;
        RefreshPreviewContainer(oldPreviewIndex, ref preparationFailure);
        RefreshPreviewContainer(SelectedIndex, ref preparationFailure);
        SynchronizeEditorText();
        SynchronizeSelectionPresenter();
        // A drop-down closed while SelectedIndex changes leaves the failure to that selection change.
        ThrowPreparationFailure(preparationFailure);
    }

    private void NavigatePreview(InputKey key)
    {
        int currentViewIndex = GetViewIndexForSourceIndex(previewSelectedIndex);
        int nextViewIndex = key switch
        {
            InputKey.Home => 0,
            InputKey.End => ViewItemCount - 1,
            InputKey.Up => Math.Max(0, currentViewIndex < 0 ? 0 : currentViewIndex - 1),
            _ => Math.Min(ViewItemCount - 1, currentViewIndex + 1)
        };
        UpdatePreviewSelection(GetSourceIndexForViewIndex(nextViewIndex), updateEditor: true);
    }

    private void UpdatePreviewSelection(int sourceIndex, bool updateEditor)
    {
        if (!selectionTransactionActive)
        {
            return;
        }

        int oldIndex = previewSelectedIndex;
        previewSelectedIndex = sourceIndex;
        CapturePreviewItem();
        if (updateEditor && sourceIndex >= 0)
        {
            transactionText = GetItemText(GetItemAt(sourceIndex));
            transactionTextMatchesPreview = true;
            SynchronizeEditorText();
        }

        ExceptionDispatchInfo? preparationFailure = null;
        RefreshPreviewContainer(oldIndex, ref preparationFailure);
        RefreshPreviewContainer(sourceIndex, ref preparationFailure);
        SynchronizeSelectionPresenter();
        // A preview that follows a new SelectedIndex leaves the failure to that selection change.
        ThrowPreparationFailure(preparationFailure);
    }

    // The highlighted item is now at sourceIndex, or was removed when sourceIndex is -1.
    private void MovePreviewForItems(int sourceIndex)
    {
        previewSelectedIndex = sourceIndex;
        if (sourceIndex >= 0)
        {
            SynchronizeSelectionPresenter();
            return;
        }

        RebuildFilteredView();
        MovePreviewToFirstVisibleItem();
    }

    // The highlighted item was removed or the filter hides it. The editor keeps its text, which no longer
    // names the highlighted item, so a commit takes the text of the item highlighted instead.
    private void MovePreviewToFirstVisibleItem()
    {
        transactionTextMatchesPreview = false;
        UpdatePreviewSelection(ViewItemCount > 0 ? GetSourceIndexForViewIndex(0) : -1, updateEditor: false);
    }

    private void CapturePreviewItem()
    {
        hasPreviewItem = selectionTransactionActive && previewSelectedIndex >= 0 && previewSelectedIndex < ItemCount;
        previewItem = hasPreviewItem ? GetItemAt(previewSelectedIndex) : null;
    }

    // The preview state already holds its new value, so a failed re-preparation does not stop the rest of the
    // preview change. Each caller still refreshes the other container, synchronizes the editor and the selection
    // box, and only then rethrows the first failure, as Selector does for the committed selection.
    private void RefreshPreviewContainer(int sourceIndex, ref ExceptionDispatchInfo? preparationFailure)
    {
        RefreshSelectionContainer(sourceIndex, "ComboBox preview selection changed", ref preparationFailure);
    }

    private void CompleteEditorText(TextBox editor, string enteredText, int matchIndex)
    {
        string completedText = GetCompletedText(enteredText, matchIndex);
        transactionText = completedText;
        transactionTextMatchesPreview = true;
        updatingTextSearch = true;
        try
        {
            editor.Text = completedText;
            editor.Select(enteredText.Length, completedText.Length);
        }
        finally
        {
            updatingTextSearch = false;
        }
    }

    private string GetCompletedText(string enteredText, int matchIndex)
    {
        string matchedText = GetItemText(GetItemAt(matchIndex));
        return ShouldPreserveUserEnteredPrefix
            ? enteredText + matchedText[enteredText.Length..]
            : matchedText;
    }

    private void SetFilterText(string value)
    {
        if (string.Equals(filterText, value, StringComparison.Ordinal))
        {
            return;
        }

        filterText = value;
        RebuildFilteredView();
        RefreshItemsView("ComboBox text filter changed");
    }

    private void ClearFilter()
    {
        if (filterText.Length == 0 && filteredSourceIndices is null)
        {
            return;
        }

        filterText = string.Empty;
        filteredSourceIndices = null;
        RefreshItemsView("ComboBox text filter cleared");
    }

    private void RebuildFilteredView()
    {
        filteredSourceIndices = !IsTextFilterEnabled || filterText.Length == 0
            ? null
            : ComboBoxTextMatcher.Rank(
                ItemCount,
                index => GetItemText(GetItemAt(index)),
                filterText,
                CultureInfo.CurrentCulture,
                IsTextSearchCaseSensitive).ToList();
    }

    private void RefreshFilterForTextPolicyChange()
    {
        if (filteredSourceIndices is null)
        {
            return;
        }

        RebuildFilteredView();
        RefreshItemsView("ComboBox item text policy changed");
    }

    private void RefreshItemsView(string reason)
    {
        ItemContainerGenerator.Clear();
        ResetDropDownVirtualization();
        ItemsPresenter.MarkItemsDirty();
        InvalidateItems(reason);
    }

    private void OnDropDownScrollChanged(object? sender, ScrollChangedEventArgs args)
    {
        if (dropDownScrollViewer is null || !IsDropDownOpen || !UsesDropDownVirtualization())
        {
            return;
        }

        UpdateVirtualizationFromScrollInfo(
            dropDownScrollViewer.ScrollInfo,
            DefaultVirtualizedItemExtent,
            DefaultVirtualizationCacheItems);
    }

    private void ResetDropDownVirtualization()
    {
        if (!UsesDropDownVirtualization() || dropDownScrollViewer is null)
        {
            SetVirtualizationContext(null);
            return;
        }

        float estimatedContentExtent = ViewItemCount * DefaultVirtualizedItemExtent;
        float viewportExtent = MathF.Min(MaxDropDownHeight, estimatedContentExtent);

        float verticalOffset = dropDownScrollViewer?.ScrollInfo.VerticalOffset ?? 0;
        SetVirtualizationContext(new VirtualizationContext(
            ViewItemCount,
            DefaultVirtualizedItemExtent,
            viewportExtent,
            verticalOffset,
            DefaultVirtualizationCacheItems));
    }

    // Replacing ComboBox.ItemsPanel opts out of the drop-down virtualization policy. The policy
    // also needs a virtualizing panel in the tree, because a template's plain panel realizes every item.
    private bool UsesDropDownVirtualization()
    {
        return ReferenceEquals(ItemsPanel, defaultItemsPanel) &&
            ItemsPresenter.ResolveItemsPanel() is IItemsVirtualizingPanel;
    }

    private int GetViewIndexForSourceIndex(int sourceIndex)
    {
        return filteredSourceIndices?.IndexOf(sourceIndex) ?? sourceIndex;
    }

    private bool IsSourceIndexVisible(int sourceIndex)
    {
        return filteredSourceIndices?.Contains(sourceIndex) ?? sourceIndex < ItemCount;
    }
}
