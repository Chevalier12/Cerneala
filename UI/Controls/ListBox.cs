using Cerneala.UI.Controls.Primitives;
using Cerneala.UI.Controls.Templates;
using Cerneala.UI.Core;
using Cerneala.UI.Elements;
using Cerneala.UI.Input;
using Cerneala.UI.Invalidation;
using Cerneala.UI.Layout;

namespace Cerneala.UI.Controls;

[TemplatePart("PART_ScrollViewer", typeof(ScrollViewer))]
[TemplatePart("PART_ItemsPresenter", typeof(ItemsPresenter))]
public class ListBox : Selector
{
    public static readonly UiProperty<SelectionMode> SelectionModeProperty = UiProperty<SelectionMode>.Register(
        nameof(SelectionMode),
        typeof(ListBox),
        new UiPropertyMetadata<SelectionMode>(
            SelectionMode.Single,
            UiPropertyOptions.None,
            validateValue: value => Enum.IsDefined(value)));

    public static readonly UiProperty<bool> IsTextSearchEnabledProperty = UiProperty<bool>.Register(
        nameof(IsTextSearchEnabled),
        typeof(ListBox),
        new UiPropertyMetadata<bool>(true, UiPropertyOptions.AffectsInputVisual));

    public static readonly UiProperty<bool> IsTextSearchCaseSensitiveProperty = UiProperty<bool>.Register(
        nameof(IsTextSearchCaseSensitive),
        typeof(ListBox),
        new UiPropertyMetadata<bool>(false, UiPropertyOptions.AffectsInputVisual));

    private readonly ItemTextSearch textSearch = new();

    // The item that Ctrl+click or a plain click in Extended mode made the anchor, and its index then: Shift+click
    // selects the items from it to the clicked item, as WPF's ListBox.AnchorItem.
    private object? anchorItem;
    private int anchorIndex = -1;

    // The ScrollViewer of the template, which scrolls the rows.
    private ScrollViewer? scrollViewer;

    // The item that ScrollIntoView received before the rows had their position: it scrolls into view after the next
    // arrange.
    private object? pendingScrollItem;
    private bool hasPendingScrollItem;

    public ListBox()
    {
        ItemsPanel = new StackPanel();
        Handlers.AddHandler(InputEvents.KeyDownEvent, OnKeyDown);
        Handlers.AddHandler(InputEvents.TextInputEvent, OnTextInput);
        SetFrameworkDefault(ComponentTemplateProperty, ListBoxTemplates.Default);
    }

    public SelectionMode SelectionMode
    {
        get => GetValue(SelectionModeProperty);
        set => SetValue(SelectionModeProperty, value);
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

    // The selected items in selection order, as WPF's ListBox.SelectedItems. SelectedItem is the first of them.
    public IReadOnlyList<object?> SelectedItems =>
        [.. SelectionModel.SelectedIndices.Where(index => index < ItemCount).Select(GetItemAt)];

    // As WPF's ListBox.SelectAll, which a ListBox whose SelectionMode is Single does not support.
    public void SelectAll()
    {
        if (SelectionMode == SelectionMode.Single)
        {
            throw new NotSupportedException("SelectAll is not supported when SelectionMode is Single.");
        }

        if (ItemCount > 0)
        {
            SelectionModel.AddRange(0, ItemCount - 1);
        }
    }

    public void UnselectAll()
    {
        SelectionModel.Clear();
    }

    // As WPF's ListBox.ScrollIntoView: the template's ScrollViewer scrolls a row above the viewport until the row's top
    // is at the top of the viewport, and a row below it until the row's bottom is at the bottom. A row in the viewport
    // and an item that the ListBox does not hold change nothing. Before the rows have their position, the row scrolls
    // into view after the next arrange.
    public void ScrollIntoView(object? item)
    {
        int index = FindItem(item);
        if (index < 0)
        {
            return;
        }

        if (!DirtyState.Has(InvalidationFlags.Measure) &&
            !DirtyState.Has(InvalidationFlags.Arrange) &&
            ScrollRowIntoView(index))
        {
            return;
        }

        pendingScrollItem = item;
        hasPendingScrollItem = true;
        Invalidate(InvalidationFlags.Arrange, "ListBox scroll into view");
    }

    protected override Type DefaultContainerType => typeof(ListBoxItem);

    // The default template places the rows below BorderThickness and Padding, as the layout without a template does.
    internal override float ItemsTopInset =>
        TemplateChild is null || ReferenceEquals(ComponentTemplate, ListBoxTemplates.Default) ? Insets.Top : 0;

    protected internal override Type GetContainerTypeForItem(object? item)
    {
        return item is ListBoxItem ? item.GetType() : typeof(ListBoxItem);
    }

    protected internal override UIElement CreateItemContainer(int index, object? item)
    {
        return item is ListBoxItem element ? element : new ListBoxItem();
    }

    internal override bool IsItemItsOwnContainer(object? item)
    {
        return item is ListBoxItem;
    }

    // As WPF's ListBoxItem selects itself on a press only when its Focus() succeeds, a click selects a row only when the
    // row can take the keyboard focus: a row whose Focusable is false leaves the selection as it was (#675).
    internal override bool SelectsContainerOnClick(UIElement container, MouseButtonEventArgs args)
    {
        ElementInputRouteMap? routeMap = args.RouteMap ??
            (container.Root is UIRoot root ? root.InputCache.EnsureCurrent(root) : null);
        return routeMap is not null && FocusPolicy.CanFocus(container, routeMap);
    }

    internal override void SelectContainerOnClick(UIElement container, MouseButtonEventArgs args)
    {
        int index = ItemContainerGenerator.IndexFromContainer(container);
        if (index < 0 ||
            (SelectionMode == SelectionMode.Single && !(args.IsControlDown && SelectionModel.IsSelected(index))))
        {
            base.SelectContainerOnClick(container, args);
            return;
        }

        SelectRowByUser(index, args.IsControlDown, args.IsShiftDown);
    }

    protected override void OnTemplateApplied(ComponentTemplateInstance? instance)
    {
        scrollViewer = null;
        ActivateItemsPresenter(null);
        if (instance is null)
        {
            return;
        }

        ActivateItemsPresenter(GetOptionalTemplatePart<ItemsPresenter>("PART_ItemsPresenter"));
        scrollViewer = GetOptionalTemplatePart<ScrollViewer>("PART_ScrollViewer");
    }

    protected override LayoutRect ArrangeCore(ArrangeContext context)
    {
        LayoutRect bounds = base.ArrangeCore(context);
        if (hasPendingScrollItem)
        {
            object? item = pendingScrollItem;
            pendingScrollItem = null;
            hasPendingScrollItem = false;
            int index = FindItem(item);
            if (index >= 0)
            {
                ScrollRowIntoView(index);
            }
        }

        return bounds;
    }

    protected override void OnPropertyChanged(UiPropertyChangedEventArgs args)
    {
        base.OnPropertyChanged(args);
        if (ReferenceEquals(args.Property, SelectionModeProperty) &&
            SelectionMode == SelectionMode.Single &&
            SelectionModel.SelectedIndices.Count > 1)
        {
            SelectionModel.Select(SelectedIndex);
        }
        else if (ReferenceEquals(args.Property, TextSearch.TextPathProperty) ||
                 ReferenceEquals(args.Property, IsTextSearchEnabledProperty) ||
                 ReferenceEquals(args.Property, IsTextSearchCaseSensitiveProperty))
        {
            textSearch.Reset();
        }
    }

    // As WPF's ListBox selects on a click or on Space: Single selects the row, and with Ctrl deselects it when it is
    // selected. Multiple toggles the row. Extended selects only the row and makes it the anchor; with Ctrl it toggles
    // the row and makes it the anchor; with Shift it selects the rows from the anchor to it in place of the selection,
    // and with Ctrl+Shift adds them to the selection.
    private void SelectRowByUser(int index, bool control, bool shift)
    {
        switch (SelectionMode)
        {
            case SelectionMode.Single:
                if (control && SelectionModel.IsSelected(index))
                {
                    SelectionModel.Remove(index);
                }
                else
                {
                    SelectedIndex = index;
                }

                break;
            case SelectionMode.Multiple:
                Toggle(index);
                break;
            default:
                if (shift)
                {
                    SelectRangeFromAnchor(index, add: control);
                }
                else if (control)
                {
                    SetAnchor(index);
                    Toggle(index);
                }
                else
                {
                    SetAnchor(index);
                    SelectionModel.Select(index);
                }

                break;
        }
    }

    // As WPF's ListBox selects on keyboard navigation: Single selects the row the focus moves to, but not with Ctrl.
    // Multiple moves only the focus. Extended selects only that row and makes it the anchor; with Shift it selects the
    // rows from the anchor to it, with Ctrl+Shift adds them, and with Ctrl it moves only the focus.
    private void NavigateToRow(int index, bool control, bool shift)
    {
        ScrollRowIntoView(index);
        FocusRow(index);
        switch (SelectionMode)
        {
            case SelectionMode.Single:
                if (!control)
                {
                    SelectedIndex = index;
                }

                break;
            case SelectionMode.Extended:
                if (shift)
                {
                    SelectRangeFromAnchor(index, add: control);
                }
                else if (!control)
                {
                    SetAnchor(index);
                    SelectionModel.Select(index);
                }

                break;
        }
    }

    private void OnKeyDown(UiElementId _, RoutedEventArgs args)
    {
        if (args is not KeyEventArgs keyArgs || keyArgs.Handled || keyArgs.IsAltDown || !IsEnabled || ItemCount == 0)
        {
            return;
        }

        if (keyArgs.Key == InputKey.A && keyArgs.IsControlDown)
        {
            if (SelectionMode != SelectionMode.Single)
            {
                SelectAll();
                args.Handled = true;
            }

            return;
        }

        int focusedIndex = FindFocusedRowIndex();
        if (keyArgs.Key == InputKey.Space)
        {
            if (focusedIndex >= 0)
            {
                SelectRowByUser(focusedIndex, keyArgs.IsControlDown, keyArgs.IsShiftDown);
                args.Handled = true;
            }

            return;
        }

        int current = focusedIndex >= 0 ? focusedIndex : SelectedIndex;
        int last = ItemCount - 1;
        int target = keyArgs.Key switch
        {
            InputKey.Up => current < 0 ? 0 : Math.Max(0, current - 1),
            InputKey.Down => current < 0 ? 0 : Math.Min(last, current + 1),
            InputKey.Home => 0,
            InputKey.End => last,
            InputKey.PageDown => current < 0 ? 0 : GetPageTarget(current, down: true),
            InputKey.PageUp => current < 0 ? 0 : GetPageTarget(current, down: false),
            _ => -1
        };
        if (target < 0)
        {
            return;
        }

        // As WPF's line navigation, Up on the first row and Down on the last row find no row and change nothing.
        if (keyArgs.Key is InputKey.Up or InputKey.Down && target == focusedIndex)
        {
            args.Handled = true;
            return;
        }

        NavigateToRow(target, keyArgs.IsControlDown, keyArgs.IsShiftDown);
        args.Handled = true;
    }

    // As WPF's TextSearch, a typed text moves to the first row whose text starts with the letters typed so far, as Down
    // without a modifier moves to a row.
    private void OnTextInput(UiElementId _, RoutedEventArgs args)
    {
        if (args is not TextCompositionEventArgs textArgs || textArgs.Handled || !IsEnabled || !IsTextSearchEnabled)
        {
            return;
        }

        string input = TextInputCore.NormalizeSingleLineInput(textArgs.Text);
        if (input.Length == 0)
        {
            return;
        }

        int matchIndex = textSearch.Search(this, input, IsTextSearchCaseSensitive);
        if (matchIndex < 0)
        {
            return;
        }

        NavigateToRow(matchIndex, control: false, shift: false);
        textArgs.Handled = true;
    }

    // As WPF's page navigation: PageDown moves to the last fully visible row, and on that row it scrolls one viewport
    // down and moves to the last fully visible row there. PageUp mirrors it with the first fully visible row. A row out
    // of the viewport counts as scrolled into view first. Without a ScrollViewer the whole list is one page.
    private int GetPageTarget(int current, bool down)
    {
        if (scrollViewer is null ||
            !ItemContainerGenerator.RealizedContainers.TryGetValue(current, out UIElement? currentRow))
        {
            return down ? ItemCount - 1 : 0;
        }

        ScrollContentPresenter presenter = scrollViewer.Presenter;
        float viewportHeight = presenter.ViewportHeight;
        float maxOffset = MathF.Max(0, presenter.ExtentHeight - viewportHeight);
        float offset = GetOffsetShowingRow(currentRow, presenter);
        int edge = FindFullyVisibleEdgeRow(presenter, offset, viewportHeight, down);
        if (edge >= 0 && edge != current)
        {
            return edge;
        }

        offset = Math.Clamp(down ? offset + viewportHeight : offset - viewportHeight, 0, maxOffset);
        int target = FindFullyVisibleEdgeRow(presenter, offset, viewportHeight, down);
        if (target >= 0)
        {
            return target;
        }

        return down ? Math.Min(ItemCount - 1, current + 1) : Math.Max(0, current - 1);
    }

    // The last (down) or first fully visible row when the viewport starts at offset, or -1.
    private int FindFullyVisibleEdgeRow(ScrollContentPresenter presenter, float offset, float viewportHeight, bool down)
    {
        int edge = -1;
        foreach ((int index, UIElement row) in ItemContainerGenerator.RealizedContainers)
        {
            float top = GetRowTop(row, presenter);
            if (top < offset || top + row.ArrangedBounds.Height > offset + viewportHeight)
            {
                continue;
            }

            if (edge < 0 || (down ? index > edge : index < edge))
            {
                edge = index;
            }
        }

        return edge;
    }

    // Scrolls the realized row at index into view, as ScrollIntoView describes; false when the row is not realized.
    private bool ScrollRowIntoView(int index)
    {
        if (!ItemContainerGenerator.RealizedContainers.TryGetValue(index, out UIElement? row))
        {
            return false;
        }

        if (scrollViewer is not null)
        {
            ScrollContentPresenter presenter = scrollViewer.Presenter;
            float offset = GetOffsetShowingRow(row, presenter);
            if (offset != presenter.VerticalOffset)
            {
                presenter.SetVerticalOffset(offset);
            }
        }

        return true;
    }

    // The vertical offset that shows the row: the current offset when the row is in the viewport, else the offset that
    // puts its top at the top or its bottom at the bottom of the viewport.
    private static float GetOffsetShowingRow(UIElement row, ScrollContentPresenter presenter)
    {
        float top = GetRowTop(row, presenter);
        float bottom = top + row.ArrangedBounds.Height;
        float offset = presenter.VerticalOffset;
        if (top < offset)
        {
            return top;
        }

        return bottom > offset + presenter.ViewportHeight ? bottom - presenter.ViewportHeight : offset;
    }

    // The top of the row in the scrolled content, which does not change when the content scrolls.
    private static float GetRowTop(UIElement row, ScrollContentPresenter presenter)
    {
        return row.ArrangedBounds.Y - presenter.ArrangedBounds.Y + presenter.ArrangedScrollOffset.Y;
    }

    private void FocusRow(int index)
    {
        if (Root is UIRoot { ActiveFocusManager: { } focusManager } root &&
            ItemContainerGenerator.RealizedContainers.TryGetValue(index, out UIElement? row))
        {
            focusManager.Focus(row, root.InputCache.EnsureCurrent(root));
        }
    }

    // The index of the row that has the keyboard focus or holds the focused element, or -1.
    private int FindFocusedRowIndex()
    {
        if (Root is not UIRoot { ActiveFocusManager.FocusedElement: { } focused })
        {
            return -1;
        }

        for (UIElement? current = focused; current is not null && !ReferenceEquals(current, this); current = current.VisualParent)
        {
            int index = ItemContainerGenerator.IndexFromContainer(current);
            if (index >= 0)
            {
                return index;
            }
        }

        return -1;
    }

    private int FindItem(object? item)
    {
        for (int index = 0; index < ItemCount; index++)
        {
            if (Equals(GetItemAt(index), item))
            {
                return index;
            }
        }

        return -1;
    }

    private void Toggle(int index)
    {
        if (SelectionModel.IsSelected(index))
        {
            SelectionModel.Remove(index);
        }
        else
        {
            SelectionModel.Add(index);
        }
    }

    private void SelectRangeFromAnchor(int index, bool add)
    {
        int anchor = ResolveAnchorIndex();
        if (add)
        {
            SelectionModel.AddRange(anchor, index);
        }
        else
        {
            SelectionModel.SelectRange(anchor, index);
        }
    }

    private void SetAnchor(int index)
    {
        anchorIndex = index;
        anchorItem = GetItemAt(index);
    }

    // The anchor's index now, as its item may have moved. Without an anchor, as in WPF, the last selected item is the
    // anchor, or the first item when nothing is selected.
    private int ResolveAnchorIndex()
    {
        int index = anchorIndex >= 0 ? FindItemIndex(anchorItem, anchorIndex) : -1;
        if (index >= 0)
        {
            return index;
        }

        IReadOnlyList<int> selected = SelectionModel.SelectedIndices;
        return selected.Count > 0 ? selected[^1] : 0;
    }

    // The template goes to the row before its content, so the row presents the new content with it once.
    protected override void PrepareItemContent(UIElement container, int index, object? item)
    {
        if (container is ListBoxItem listBoxItem)
        {
            listBoxItem.SetItemPresentation(ItemTemplate, ItemTemplateKey, ContentTemplateRegistry, index);
        }

        base.PrepareItemContent(container, index, item);
    }

    protected internal override void ClearItemContainer(UIElement container)
    {
        base.ClearItemContainer(container);
        if (container is ListBoxItem listBoxItem)
        {
            listBoxItem.SetItemPresentation(null, null, null, -1);
        }
    }
}
