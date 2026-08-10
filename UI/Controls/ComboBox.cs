using Cerneala.Drawing;
using Cerneala.UI.Aspect;
using Cerneala.UI.Controls.Items;
using Cerneala.UI.Controls.Primitives;
using Cerneala.UI.Controls.Templates;
using Cerneala.UI.Core;
using Cerneala.UI.Elements;
using Cerneala.UI.Input;
using Cerneala.UI.Invalidation;
using Cerneala.UI.Layout;
using Cerneala.UI.Layout.Panels;
using Cerneala.UI.Layout.Virtualization;
using Cerneala.UI.Media;
using System.Globalization;

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
    private static readonly ItemsPanelTemplate DefaultItemsPanelTemplate = new(
        () => new VirtualizingStackPanel());

    private ContentPresenter? selectionPresenter;
    private TextBox? editableTextBox;
    private ToggleButton? dropDownToggle;
    private Overlay? dropDownOverlay;
    private ScrollViewer? dropDownScrollViewer;
    private bool synchronizingParts;
    private bool preserveTextWhileDeselecting;
    private bool updatingTextSearch;
    private string textSearchPrefix = string.Empty;
    private long lastTextSearchInputTime;
    private List<int>? filteredSourceIndices;
    private string filterText = string.Empty;
    private bool selectionTransactionActive;
    private int previewSelectedIndex = -1;
    private string transactionText = string.Empty;
    private bool transactionTextMatchesPreview;
    private bool suppressAutocompleteForCurrentEdit;

    public ComboBox()
    {
        Focusable = true;
        IsTabStop = true;
        ItemsPanel = DefaultItemsPanelTemplate;
        Handlers.AddHandler(InputEvents.KeyDownEvent, OnKeyDown);
        Handlers.AddHandler(InputEvents.TextInputEvent, OnTextInput);
        SetValue(BackgroundProperty, new SolidColorBrush(Color.White), UiPropertyValueSource.AspectBase);
        SetValue(ForegroundProperty, new SolidColorBrush(Color.Black), UiPropertyValueSource.AspectBase);
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
        int sourceIndex = ItemContainerGenerator.GetItemIndex(container);
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

        UpdatePreviewSelection(sourceIndex, updateEditor: true);
        CommitSelectionTransaction();
        IsDropDownOpen = false;
    }

    internal override void OnItemsViewSourceChanged()
    {
        RebuildFilteredView();
        if (previewSelectedIndex >= ItemCount ||
            (previewSelectedIndex >= 0 && !IsSourceIndexVisible(previewSelectedIndex)))
        {
            UpdatePreviewSelection(ViewItemCount > 0 ? GetSourceIndexForViewIndex(0) : -1, updateEditor: false);
        }

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

            if (selectionTransactionActive && !updatingTextSearch)
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
            if (IsDropDownOpen)
            {
                BeginSelectionTransaction();
            }
            else
            {
                CancelSelectionTransaction();
            }

            ApplyTemplate();
            SynchronizeDropDownState();
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
        object? presentedItem = presentedIndex >= 0 && presentedIndex < ItemCount
            ? GetItemAt(presentedIndex)
            : null;
        string presentedText = selectionTransactionActive && presentedIndex >= 0
            ? GetItemText(presentedItem)
            : Text;
        selectionPresenter.Content = ItemTemplate is null ? presentedText : presentedItem;
        selectionPresenter.ContentTemplate = ItemTemplate;
        selectionPresenter.ContentTemplateKey = ItemTemplateKey;
        selectionPresenter.LocalTemplateRegistry = ContentTemplateRegistry;
        selectionPresenter.ContentIndex = presentedIndex;
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
            int previewIndex = enteredText.Length == 0
                ? -1
                : matchIndex >= 0
                ? matchIndex
                : ViewItemCount > 0 ? GetSourceIndexForViewIndex(0) : -1;
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

    private void OnTextInput(UiElementId _, RoutedEventArgs args)
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

        long now = Environment.TickCount64;
        if (lastTextSearchInputTime == 0 || now - lastTextSearchInputTime > 1000)
        {
            textSearchPrefix = string.Empty;
        }

        lastTextSearchInputTime = now;
        string candidatePrefix = textSearchPrefix + input;
        int matchIndex = FindMatchingItem(candidatePrefix, exactMatch: false);
        if (matchIndex < 0 && textSearchPrefix.Length > 0)
        {
            candidatePrefix = input;
            matchIndex = FindMatchingItem(candidatePrefix, exactMatch: false);
        }

        textSearchPrefix = candidatePrefix;
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

    private void OnEditablePreviewKeyDown(UiElementId _, RoutedEventArgs args)
    {
        suppressAutocompleteForCurrentEdit = args is KeyEventArgs
        {
            Key: InputKey.Back or InputKey.Delete
        };
    }

    private void OnEditablePreviewKeyUp(UiElementId _, RoutedEventArgs args)
    {
        suppressAutocompleteForCurrentEdit = false;
    }

    private void OnDropDownToggleChecked(UiElementId _, RoutedEventArgs args)
    {
        if (!synchronizingParts)
        {
            ClearFilter();
            IsDropDownOpen = true;
        }
    }

    private void OnDropDownToggleUnchecked(UiElementId _, RoutedEventArgs args)
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

    private void OnOverlayOpened(UiElementId _, RoutedEventArgs args)
    {
        ResetDropDownVirtualization();
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
            CommitSelectionTransaction();
            IsDropDownOpen = false;
            args.Handled = true;
            return;
        }

        if (keyArgs.Key == InputKey.Tab && IsDropDownOpen)
        {
            CommitSelectionTransaction();
            IsDropDownOpen = false;
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
        CompareInfo compareInfo = CultureInfo.CurrentCulture.CompareInfo;
        CompareOptions options = IsTextSearchCaseSensitive ? CompareOptions.None : CompareOptions.IgnoreCase;
        for (int index = 0; index < ItemCount; index++)
        {
            string itemText = GetItemText(GetItemAt(index));
            bool matches = exactMatch
                ? compareInfo.Compare(itemText, searchText, options) == 0
                : compareInfo.IsPrefix(itemText, searchText, options);
            if (matches)
            {
                return index;
            }
        }

        return -1;
    }

    private string GetItemText(object? item)
    {
        if (item is UiObject uiObject && uiObject.GetValueSource(TextSearch.TextProperty) != UiPropertyValueSource.Default)
        {
            return TextSearch.GetText(uiObject);
        }

        string textPath = TextSearch.GetTextPath(this);
        if (textPath.Length > 0)
        {
            return DisplayMemberPathAccessor.Resolve(item, textPath)?.ToString() ?? string.Empty;
        }

        return GetItemDisplayText(item);
    }

    private void ResetTextSearchPrefix()
    {
        textSearchPrefix = string.Empty;
        lastTextSearchInputTime = 0;
    }

    private void BeginSelectionTransaction()
    {
        if (selectionTransactionActive)
        {
            return;
        }

        selectionTransactionActive = true;
        previewSelectedIndex = SelectedIndex;
        transactionText = Text;
        transactionTextMatchesPreview = SelectedIndex >= 0;
        RefreshPreviewContainer(previewSelectedIndex);
        SynchronizeSelectionPresenter();
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
        transactionTextMatchesPreview = false;
        updatingTextSearch = true;
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
        RefreshPreviewContainer(oldPreviewIndex);
        SynchronizeEditorText();
        editableTextBox?.MoveCaret(editableTextBox.Text.Length);
        SynchronizeSelectionPresenter();
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
        transactionText = Text;
        transactionTextMatchesPreview = false;
        ClearFilter();
        RefreshPreviewContainer(oldPreviewIndex);
        RefreshPreviewContainer(SelectedIndex);
        SynchronizeEditorText();
        SynchronizeSelectionPresenter();
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
        if (updateEditor && sourceIndex >= 0)
        {
            transactionText = GetItemText(GetItemAt(sourceIndex));
            transactionTextMatchesPreview = true;
            SynchronizeEditorText();
        }

        RefreshPreviewContainer(oldIndex);
        RefreshPreviewContainer(sourceIndex);
        SynchronizeSelectionPresenter();
    }

    private void RefreshPreviewContainer(int sourceIndex)
    {
        if (sourceIndex < 0 || sourceIndex >= ItemCount ||
            !ItemContainerGenerator.RealizedContainers.TryGetValue(sourceIndex, out UIElement? container))
        {
            return;
        }

        PrepareItemContainer(container, sourceIndex, GetItemAt(sourceIndex));
        container.IncrementRenderVersion();
        container.Invalidate(
            InvalidationFlags.Render | InvalidationFlags.InputVisual,
            "ComboBox preview selection changed");
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
        if (dropDownScrollViewer is null || !IsDropDownOpen)
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
        if (!ReferenceEquals(ItemsPanel, DefaultItemsPanelTemplate) || dropDownScrollViewer is null)
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

    private int GetViewIndexForSourceIndex(int sourceIndex)
    {
        return filteredSourceIndices?.IndexOf(sourceIndex) ?? sourceIndex;
    }

    private bool IsSourceIndexVisible(int sourceIndex)
    {
        return filteredSourceIndices?.Contains(sourceIndex) ?? sourceIndex < ItemCount;
    }
}
