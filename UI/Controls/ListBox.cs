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
public class ListBox : Selector, ITabOnceScope
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

    // The item that Ctrl+click or a plain click in Extended mode made the anchor, and its index: Shift+click selects the
    // items from it to the clicked item, as WPF's ListBox.AnchorItem. The index follows its occurrence when items are
    // inserted, removed, or moved, as the selected entries do, so an equal item before it does not take its place
    // (#925); only a reset finds the item again by value.
    private object? anchorItem;
    private int anchorIndex = -1;

    // The ScrollViewer of the template, which scrolls the rows.
    private ScrollViewer? scrollViewer;

    // The item that ScrollIntoView received before the rows had their position: it scrolls into view after the next
    // arrange.
    private object? pendingScrollItem;
    private bool hasPendingScrollItem;

    // The row that keyboard navigation or ScrollIntoView scrolled to by the items panel's estimate because it had no
    // container: after the next arrange, which realizes it, it scrolls to its exact place, and gets the focus when
    // focusPendingRow is true.
    private int pendingScrollIndex = -1;
    private bool focusPendingRow;

    public ListBox()
    {
        SetFrameworkDefault(ItemsPanelProperty, new StackPanel());
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

    // As WPF's ListBox.ScrollIntoView, which brings the row into view: the template's ScrollViewer, and then each
    // ScrollViewer around the ListBox, scrolls a row above the viewport until the row's top is at the top of the
    // viewport, and a row below it until the row's bottom is at the bottom. A row in the viewport and an item that the
    // ListBox does not hold change nothing. Before the rows have their position, the row scrolls into view after the next
    // arrange.
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
        pendingScrollIndex = -1;
        focusPendingRow = false;
        Invalidate(InvalidationFlags.Arrange, "ListBox scroll into view");
    }

    protected override Type DefaultContainerType => typeof(ListBoxItem);

    // As a WPF ListBox, whose TabNavigation is Once, the Tab key enters the list once, on the focused row, else the
    // selected row, else the first row; the rows are no Tab stops, so the next Tab leaves the list (#905).
    UIElement? ITabOnceScope.GetTabOnceElement(ElementInputRouteMap routeMap) =>
        FindTabOnceContainer(routeMap, ItemContainerGenerator.RealizedContainers.GetValueOrDefault(SelectedIndex));

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
        // As WPF's BringIndexIntoView, which scrolls until the item is in view, the adjustment repeats after the next
        // arrange while it moves the viewport (see BringRowIntoView). An adjustment that does not move the viewport ends
        // the pending row; a row that still has no container then keeps the focus where it is.
        if (hasPendingScrollItem || pendingScrollIndex >= 0)
        {
            int index = hasPendingScrollItem ? FindItem(pendingScrollItem) : pendingScrollIndex;
            bool focus = focusPendingRow;
            pendingScrollItem = null;
            hasPendingScrollItem = false;
            pendingScrollIndex = -1;
            focusPendingRow = false;
            if (index >= 0 && index < ItemCount)
            {
                BringRowIntoView(index, focus);
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
        BringRowIntoView(index, focus: true);
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

    private void OnKeyDown(object _, RoutedEventArgs args)
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
    private void OnTextInput(object _, RoutedEventArgs args)
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
        (float currentTop, float currentBottom) = GetRowBounds(current, currentRow, presenter)!.Value;
        float offset = GetOffsetShowing(currentTop, currentBottom, presenter);
        int edge = FindFullyVisibleEdgeRow(presenter, offset, viewportHeight, down);
        if (edge >= 0 && edge != current)
        {
            return edge;
        }

        offset = Math.Clamp(down ? offset + viewportHeight : offset - viewportHeight, 0, maxOffset);
        int target = FindFullyVisibleEdgeRow(presenter, offset, viewportHeight, down);
        // A VirtualizingStackPanel has realized only the rows of the current viewport, so the edge row one viewport
        // further is found with the panel's estimate when that row has no container.
        int estimated = EstimateFullyVisibleEdgeRow(presenter, offset, viewportHeight, down);
        if (estimated >= 0 && !ItemContainerGenerator.RealizedContainers.ContainsKey(estimated))
        {
            target = estimated;
        }

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

    // The last (down) or first fully visible row by the estimate of a VirtualizingStackPanel when the viewport starts
    // at offset, or -1. The estimated row tops and bottoms grow with the index, so one walk over the rows finds the last
    // row that ends in the viewport (down) or the first row that starts in it (up).
    private int EstimateFullyVisibleEdgeRow(ScrollContentPresenter presenter, float offset, float viewportHeight, bool down)
    {
        float viewportEnd = offset + viewportHeight;
        int found = -1;
        float foundTop = 0;
        float foundBottom = 0;
        int index = 0;
        foreach ((float top, float bottom) in EnumerateEstimatedRowBounds(ItemCount, presenter))
        {
            if (down ? bottom > viewportEnd : top >= offset)
            {
                if (!down)
                {
                    (found, foundTop, foundBottom) = (index, top, bottom);
                }

                break;
            }

            if (down)
            {
                (found, foundTop, foundBottom) = (index, top, bottom);
            }

            index++;
        }

        return found >= 0 && foundTop >= offset && foundBottom <= viewportEnd ? found : -1;
    }

    // As WPF's ListBox, which brings a row without a container into view through VirtualizingStackPanel's
    // BringIndexIntoView before it focuses the row: a realized row scrolls into view and gets the focus now; a row
    // without a container scrolls to the panel's estimate and gets the focus in the first later arrange that finds it
    // realized. As BringIndexIntoView, the row scrolls again after each arrange that follows a scroll, until a scroll
    // moves no viewport: the measure at the new place records the heights of the rows it realizes, which correct the
    // estimate of the rows before them. The scroll can move the template's ScrollViewer and the ScrollViewers around the
    // ListBox, for example an outer ScrollViewer around a ListBox without a fixed height, so each of them counts.
    // This ends: with the panel's records unchanged, the row is at the place the last scroll showed in each
    // ScrollViewer, so the next scroll moves none of them. Each record holds the height of one item, so with rows whose
    // height does not change, the records change only as long as rows that were never measured are realized, which
    // happens at most once per item.
    // As WPF's ListBox, which lays out before it focuses the row, a row that was realized in this layout pass and that
    // the panel has not arranged yet gets the focus in a later arrange too: focusing a row brings it into view (#1158),
    // and before its arrange the row has no place to bring into view.
    private void BringRowIntoView(int index, bool focus)
    {
        float[] offsetsBefore = GetScrollOffsetsAroundRows();
        bool realized = ScrollRowIntoView(index);
        bool arranged = realized && !ItemContainerGenerator.RealizedContainers[index].DirtyState.Has(InvalidationFlags.Arrange);
        if (arranged && focus)
        {
            FocusRow(index);
        }

        if (!offsetsBefore.AsSpan().SequenceEqual(GetScrollOffsetsAroundRows()) || (focus && realized && !arranged))
        {
            pendingScrollItem = null;
            hasPendingScrollItem = false;
            pendingScrollIndex = index;
            focusPendingRow = focus && !arranged;
            Invalidate(InvalidationFlags.Arrange, "ListBox bring row into view");
        }
    }

    // Scrolls the row at index into view, as ScrollIntoView describes, through BringIntoView as WPF's ListBox, so the
    // template's ScrollViewer scrolls first and then each ScrollViewer around the ListBox; false when the row is not
    // realized. The items panel asks to bring the row's place into view, not the row: ScrollIntoView is not input, as
    // in WPF it works also for a disabled row, and a disabled row is not in the input route, so its request would reach
    // no ScrollViewer, while the panel, a part of the template, is in it. A VirtualizingStackPanel gives the place of
    // every row, realized or not; with another panel a realized row gives its arranged place, and a row without a
    // container does not scroll.
    private bool ScrollRowIntoView(int index)
    {
        bool realized = ItemContainerGenerator.RealizedContainers.TryGetValue(index, out UIElement? row);
        if (ItemsPresenter.LayoutPanelRoot is not { } panel || GetRowPlaceInPanel(panel, index, row) is not { } place)
        {
            return realized;
        }

        panel.BringIntoView(place);
        return realized;
    }

    // The place of the row in the panel's coordinates, from the last arrange: for a VirtualizingStackPanel its estimate,
    // with the left and width of the row when it is realized and of the panel otherwise; for another panel the arranged
    // bounds of a realized row; null for a row that has no place yet.
    private static LayoutRect? GetRowPlaceInPanel(Layout.Panels.Panel panel, int index, UIElement? row)
    {
        LayoutRect panelBounds = panel.ArrangedBounds;
        if (panel is Layout.Panels.VirtualizingStackPanel virtualizingPanel &&
            EstimatePanelRowBounds(virtualizingPanel, index) is (float top, float bottom))
        {
            (float left, float width) = row is null
                ? (0, panelBounds.Width)
                : (row.ArrangedBounds.X - panelBounds.X, row.ArrangedBounds.Width);
            return new LayoutRect(left, top, width, bottom - top);
        }

        return row is null
            ? null
            : row.ArrangedBounds with { X = row.ArrangedBounds.X - panelBounds.X, Y = row.ArrangedBounds.Y - panelBounds.Y };
    }

    // The vertical offsets of the ScrollContentPresenters around the rows, innermost first: the one of the template's
    // ScrollViewer and those of the ScrollViewers around the ListBox, which bringing a row into view can scroll.
    private float[] GetScrollOffsetsAroundRows()
    {
        List<float> offsets = [];
        for (UIElement? current = ItemsPresenter; current is not null; current = current.VisualParent)
        {
            if (current is ScrollContentPresenter presenter)
            {
                offsets.Add(presenter.VerticalOffset);
            }
        }

        return [.. offsets];
    }

    // The top and bottom of the row in the scrolled content. The offsets of a VirtualizingStackPanel come from its
    // measure and are where it arranges each item, so they hold before the panel is arranged at a new scroll offset;
    // the arranged bounds of a row hold only after that arrange.
    private (float Top, float Bottom)? GetRowBounds(int index, UIElement? row, ScrollContentPresenter presenter)
    {
        if (EstimateRowBounds(index, presenter) is { } panelBounds)
        {
            return panelBounds;
        }

        if (row is null)
        {
            return null;
        }

        float top = GetRowTop(row, presenter);
        return (top, top + row.ArrangedBounds.Height);
    }

    // The vertical offset that shows the row, by the rule of MakeVisible, which ScrollIntoView applies.
    private static float GetOffsetShowing(float top, float bottom, ScrollContentPresenter presenter)
    {
        return ScrollContentPresenter.ComputeOffsetShowing(presenter.VerticalOffset, presenter.ViewportHeight, top, bottom);
    }

    // The top and bottom of the row at index in the scrolled content, from the offsets of the VirtualizingStackPanel
    // that presents the rows (measured heights, and the estimate for the rows it has not measured), or null when no
    // such panel virtualizes them.
    private (float Top, float Bottom)? EstimateRowBounds(int index, ScrollContentPresenter presenter)
    {
        (float Top, float Bottom)? bounds = null;
        foreach ((float Top, float Bottom) rowBounds in EnumerateEstimatedRowBounds(index + 1, presenter))
        {
            bounds = rowBounds;
        }

        return bounds;
    }

    // The top and bottom of the row at index in the panel, by its estimate, or null when it does not virtualize.
    private static (float Top, float Bottom)? EstimatePanelRowBounds(Layout.Panels.VirtualizingStackPanel panel, int index)
    {
        (float Top, float Bottom)? bounds = null;
        foreach ((float Top, float Bottom) rowBounds in panel.EnumerateAutomaticItemBounds(index + 1))
        {
            bounds = rowBounds;
        }

        return bounds;
    }

    // The top and bottom of each of the first count rows in the scrolled content, by the estimate of the
    // VirtualizingStackPanel that presents the rows, in one walk; no bounds when no such panel virtualizes them.
    private IEnumerable<(float Top, float Bottom)> EnumerateEstimatedRowBounds(int count, ScrollContentPresenter presenter)
    {
        if (ItemsPresenter.LayoutPanelRoot is not Layout.Panels.VirtualizingStackPanel panel)
        {
            yield break;
        }

        float panelTop = GetRowTop(panel, presenter);
        foreach ((float top, float bottom) in panel.EnumerateAutomaticItemBounds(count))
        {
            yield return (panelTop + top, panelTop + bottom);
        }
    }

    // The top of the row in the scrolled content, which does not change when the content scrolls.
    private static float GetRowTop(UIElement row, ScrollContentPresenter presenter)
    {
        return presenter.GetContentPosition(row).Y;
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

    // The anchor follows its item before the selection does; a removed or replaced anchor item leaves no anchor.
    internal override void OnItemsReplaced(int index, int removedCount, int addedCount)
    {
        if (anchorIndex >= index)
        {
            MoveAnchorForItems(anchorIndex < index + removedCount ? -1 : anchorIndex - removedCount + addedCount);
        }

        base.OnItemsReplaced(index, removedCount, addedCount);
    }

    internal override void OnItemsMoved(int oldIndex, int newIndex, int count)
    {
        if (anchorIndex >= 0)
        {
            MoveAnchorForItems(GetIndexAfterMove(anchorIndex, oldIndex, newIndex, count));
        }

        base.OnItemsMoved(oldIndex, newIndex, count);
    }

    internal override void OnItemsReset()
    {
        if (anchorIndex >= 0)
        {
            MoveAnchorForItems(FindItemIndex(anchorItem, anchorIndex));
        }

        base.OnItemsReset();
    }

    private void MoveAnchorForItems(int index)
    {
        anchorIndex = index;
        if (index < 0)
        {
            anchorItem = null;
        }
    }

    // The anchor's index. Without an anchor, as in WPF, the last selected item is the anchor, or the first item when
    // nothing is selected.
    private int ResolveAnchorIndex()
    {
        if (anchorIndex >= 0 && anchorIndex < ItemCount)
        {
            return anchorIndex;
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
