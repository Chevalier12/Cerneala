using Cerneala.UI.Elements;
using Cerneala.UI.Input;

namespace Cerneala.UI.Controls;

internal sealed class MenuSession
{
    private readonly Menu root;
    private readonly List<MenuItem> openPath = [];
    private MenuItem? activeItem;
    private MenuItem? restoreFocusTarget;
    private ItemsControl? pendingFocusOwner;
    private bool pendingFocusLast;
    private MenuItem? pendingFocusItem;
    private bool synchronizing;
    private bool rootAttached;

    public MenuSession(Menu root)
    {
        this.root = root ?? throw new ArgumentNullException(nameof(root));
        DismissScope = new OverlayDismissScope(
            () => CloseAll(restoreFocus: true),
            IsWithinRoot);
    }

    internal IReadOnlyList<MenuItem> OpenPath => openPath;

    internal OverlayDismissScope DismissScope { get; }

    internal void OnRootAttached()
    {
        rootAttached = true;
        TryApplyPendingFocus();
    }

    internal void OnRootDetached()
    {
        rootAttached = false;
        CloseAll(restoreFocus: false);
    }

    internal void OnContainerPrepared(ItemsControl owner, MenuItem item)
    {
        if (ReferenceEquals(pendingFocusOwner, owner))
        {
            TryResolvePendingBoundaryFocus();
        }

        if (ReferenceEquals(pendingFocusItem, item))
        {
            TryApplyPendingFocus();
        }
    }

    internal void OnItemAttached(MenuItem item)
    {
        if (ReferenceEquals(pendingFocusItem, item))
        {
            TryApplyPendingFocus();
        }
    }

    internal void OnItemArranged(MenuItem item)
    {
        if (ReferenceEquals(pendingFocusItem, item))
        {
            TryApplyPendingFocus();
        }
    }

    internal void OnItemDetached(MenuItem item)
    {
        if (synchronizing || !Owns(item))
        {
            return;
        }

        if (ContainsInOpenBranch(item) || ReferenceEquals(activeItem, item))
        {
            CloseAll(restoreFocus: false);
        }
    }

    internal void OnItemReleased(MenuItem item)
    {
        if (!Owns(item))
        {
            return;
        }

        if (ContainsInOpenBranch(item) || IsDescendantOf(activeItem, item))
        {
            CloseAll(restoreFocus: false);
        }

        if (ReferenceEquals(pendingFocusItem, item))
        {
            ClearPendingFocus();
        }
    }

    internal void OnRootItemsChanged()
    {
        CloseAll(restoreFocus: false);
    }

    internal void OnItemsChanged(MenuItem owner)
    {
        if (owner.IsSubmenuOpen || openPath.Contains(owner))
        {
            CloseFrom(owner);
        }
    }

    internal void OnItemUnavailable(MenuItem item)
    {
        if (openPath.Count > 0 && (ContainsInOpenBranch(item) || ReferenceEquals(activeItem, item)))
        {
            CloseAll(restoreFocus: false);
        }
    }

    internal void OnSubmenuStateRequested(MenuItem item, bool isOpen)
    {
        if (synchronizing || !Owns(item))
        {
            return;
        }

        if (isOpen)
        {
            if (!CanOpen(item))
            {
                SetOpen(item, false);
                return;
            }

            OpenBranch(item, focusFirstChild: false);
            return;
        }

        CloseFrom(item);
    }

    internal void ActivateParent(MenuItem item, bool focusFirstChild)
    {
        if (!CanOpen(item))
        {
            return;
        }

        activeItem = item;
        if (item.IsSubmenuOpen && !focusFirstChild)
        {
            CloseFrom(item);
            return;
        }

        OpenBranch(item, focusFirstChild);
    }

    internal void CompleteLeafActivation(MenuItem item)
    {
        if (Owns(item))
        {
            CloseAll(restoreFocus: true);
        }
    }

    internal void OnPointerEntered(MenuItem item)
    {
        if (!Owns(item) || !IsEligible(item))
        {
            return;
        }

        activeItem = item;
        if (item.ParentMenuItem is null && root is MenuBar)
        {
            if (openPath.Count == 0)
            {
                return;
            }

            if (item.HasSubmenu)
            {
                OpenBranch(item, focusFirstChild: false);
            }
            else
            {
                CloseAll(restoreFocus: false);
            }

            return;
        }

        if (item.HasSubmenu)
        {
            OpenBranch(item, focusFirstChild: false);
        }
        else
        {
            CloseBranchesAfter(item.ParentMenuItem);
        }
    }

    internal bool HandleKey(MenuItem item, KeyEventArgs args)
    {
        if (!Owns(item) || !IsEligible(item))
        {
            return false;
        }

        switch (args.Key)
        {
            case InputKey.Tab:
                if (openPath.Count > 0)
                {
                    CloseAll(restoreFocus: false);
                }

                return false;
            case InputKey.Escape:
                if (item.ParentMenuItem is MenuItem parent)
                {
                    CloseFrom(parent);
                    FocusItem(parent);
                    return true;
                }

                if (openPath.Count > 0)
                {
                    MenuItem target = TopLevelItem(item);
                    CloseAll(restoreFocus: false);
                    FocusItem(target);
                    return true;
                }

                return false;
            case InputKey.Home:
                return FocusBoundarySibling(item, last: false);
            case InputKey.End:
                return FocusBoundarySibling(item, last: true);
            case InputKey.Up:
                return item.ParentMenuItem is not null || root is not MenuBar
                    ? FocusRelativeSibling(item, -1, wrap: false)
                    : false;
            case InputKey.Down:
                if (item.ParentMenuItem is null && root is MenuBar)
                {
                    if (!item.HasSubmenu)
                    {
                        return false;
                    }

                    OpenBranch(item, focusFirstChild: true);
                    return true;
                }

                return FocusRelativeSibling(item, 1, wrap: false);
            case InputKey.Left:
                if (item.ParentMenuItem is MenuItem submenuParent)
                {
                    CloseFrom(submenuParent);
                    FocusItem(submenuParent);
                    return true;
                }

                return root is MenuBar && FocusRelativeSibling(item, -1, wrap: true, switchRootBranch: true);
            case InputKey.Right:
                if (item.ParentMenuItem is null && root is MenuBar)
                {
                    return FocusRelativeSibling(item, 1, wrap: true, switchRootBranch: true);
                }

                if (item.HasSubmenu)
                {
                    OpenBranch(item, focusFirstChild: true);
                    return true;
                }

                return false;
            case InputKey.Enter:
            case InputKey.Space:
                if (!item.HasSubmenu)
                {
                    return false;
                }

                OpenBranch(item, focusFirstChild: true);
                return true;
            default:
                return false;
        }
    }

    internal void CloseAll(bool restoreFocus)
    {
        if (synchronizing)
        {
            return;
        }

        MenuItem? focusTarget = null;
        if (restoreFocus)
        {
            focusTarget = restoreFocusTarget ?? openPath.FirstOrDefault();
            if (focusTarget is null && activeItem is MenuItem active)
            {
                focusTarget = TopLevelItem(active);
            }
        }
        synchronizing = true;
        try
        {
            for (int index = openPath.Count - 1; index >= 0; index--)
            {
                SetOpenCore(openPath[index], false);
            }

            openPath.Clear();
            ClearPendingFocus();
            activeItem = null;
            restoreFocusTarget = null;
        }
        finally
        {
            synchronizing = false;
        }

        if (focusTarget is not null && Owns(focusTarget) && IsEligible(focusTarget))
        {
            FocusItem(focusTarget);
        }
    }

    private void OpenBranch(MenuItem item, bool focusFirstChild)
    {
        List<MenuItem> requestedPath = BuildPath(item);
        int common = 0;
        while (common < requestedPath.Count &&
               common < openPath.Count &&
               ReferenceEquals(requestedPath[common], openPath[common]))
        {
            common++;
        }

        synchronizing = true;
        try
        {
            for (int index = openPath.Count - 1; index >= common; index--)
            {
                SetOpenCore(openPath[index], false);
                openPath.RemoveAt(index);
            }

            for (int index = common; index < requestedPath.Count; index++)
            {
                MenuItem pathItem = requestedPath[index];
                if (!CanOpen(pathItem))
                {
                    break;
                }

                SetOpenCore(pathItem, true);
                openPath.Add(pathItem);
            }
        }
        finally
        {
            synchronizing = false;
        }

        if (openPath.Count > 0)
        {
            restoreFocusTarget ??= openPath[0];
            activeItem = item;
        }

        if (focusFirstChild && item.IsSubmenuOpen)
        {
            FocusBoundaryChild(item, last: false);
        }
    }

    private void CloseFrom(MenuItem item)
    {
        int index = openPath.FindIndex(candidate => ReferenceEquals(candidate, item));
        if (index < 0)
        {
            return;
        }

        synchronizing = true;
        try
        {
            for (int current = openPath.Count - 1; current >= index; current--)
            {
                SetOpenCore(openPath[current], false);
                openPath.RemoveAt(current);
            }
        }
        finally
        {
            synchronizing = false;
        }

        ClearPendingFocus();
        activeItem = item.ParentMenuItem ?? item;
        if (openPath.Count == 0)
        {
            restoreFocusTarget = null;
        }
    }

    private void CloseBranchesAfter(MenuItem? parent)
    {
        int keepCount = parent is null
            ? 0
            : openPath.FindIndex(candidate => ReferenceEquals(candidate, parent)) + 1;
        if (keepCount < 0)
        {
            keepCount = 0;
        }

        synchronizing = true;
        try
        {
            for (int index = openPath.Count - 1; index >= keepCount; index--)
            {
                SetOpenCore(openPath[index], false);
                openPath.RemoveAt(index);
            }
        }
        finally
        {
            synchronizing = false;
        }

        if (openPath.Count == 0)
        {
            restoreFocusTarget = null;
        }
    }

    private bool FocusRelativeSibling(
        MenuItem item,
        int direction,
        bool wrap,
        bool switchRootBranch = false)
    {
        ItemsControl owner = item.ParentMenuItem is MenuItem parent ? parent : root;
        IReadOnlyList<MenuItem> siblings = EligibleChildren(owner);
        int current = IndexOfReference(siblings, item);
        if (current < 0 || siblings.Count == 0)
        {
            return false;
        }

        int next = current + direction;
        if (wrap)
        {
            next = (next + siblings.Count) % siblings.Count;
        }
        else if (next < 0 || next >= siblings.Count)
        {
            return false;
        }

        MenuItem target = siblings[next];
        if (switchRootBranch && openPath.Count > 0)
        {
            if (target.HasSubmenu)
            {
                OpenBranch(target, focusFirstChild: false);
            }
            else
            {
                CloseAll(restoreFocus: false);
            }
        }

        FocusItem(target);
        return true;
    }

    private bool FocusBoundarySibling(MenuItem item, bool last)
    {
        ItemsControl owner = item.ParentMenuItem is MenuItem parent ? parent : root;
        IReadOnlyList<MenuItem> siblings = EligibleChildren(owner);
        if (siblings.Count == 0)
        {
            return false;
        }

        FocusItem(last ? siblings[^1] : siblings[0]);
        return true;
    }

    private void FocusBoundaryChild(ItemsControl owner, bool last)
    {
        pendingFocusOwner = owner;
        pendingFocusLast = last;
        TryResolvePendingBoundaryFocus();
    }

    private void TryResolvePendingBoundaryFocus()
    {
        if (pendingFocusOwner is not ItemsControl owner)
        {
            return;
        }

        IReadOnlyList<MenuItem> children = EligibleChildren(owner, requireAttached: false);
        if (children.Count > 0)
        {
            pendingFocusOwner = null;
            FocusItem(pendingFocusLast ? children[^1] : children[0]);
            return;
        }

    }

    private void FocusItem(MenuItem item)
    {
        activeItem = item;
        pendingFocusItem = item;
        TryApplyPendingFocus();
    }

    private void TryApplyPendingFocus()
    {
        if (!rootAttached ||
            pendingFocusItem is not MenuItem item ||
            root.Root is not UIRoot uiRoot ||
            uiRoot.ActiveFocusManager is not FocusManager focusManager ||
            !item.IsAttached)
        {
            return;
        }

        ElementInputRouteMap routes = uiRoot.InputCache.EnsureCurrent(uiRoot);
        if (ReferenceEquals(focusManager.FocusedElement, item) || focusManager.Focus(item, routes))
        {
            pendingFocusItem = null;
        }
    }

    private IReadOnlyList<MenuItem> EligibleChildren(ItemsControl owner, bool requireAttached = true)
    {
        List<MenuItem> result = [];
        for (int index = 0; index < owner.ItemCount; index++)
        {
            if (owner.ItemContainerGenerator.RealizedContainers.TryGetValue(index, out UIElement? container) &&
                container is MenuItem menuItem &&
                Owns(menuItem) &&
                (requireAttached ? IsEligible(menuItem) : IsStateEligible(menuItem)))
            {
                result.Add(menuItem);
            }
        }

        return result;
    }

    private List<MenuItem> BuildPath(MenuItem item)
    {
        List<MenuItem> result = [];
        for (MenuItem? current = item; current is not null; current = current.ParentMenuItem)
        {
            result.Add(current);
        }

        result.Reverse();
        return result;
    }

    private bool ContainsInOpenBranch(MenuItem item)
    {
        return openPath.Any(candidate =>
            ReferenceEquals(candidate, item) || IsDescendantOf(candidate, item) || IsDescendantOf(item, candidate));
    }

    private static bool IsDescendantOf(MenuItem? item, MenuItem ancestor)
    {
        for (MenuItem? current = item?.ParentMenuItem; current is not null; current = current.ParentMenuItem)
        {
            if (ReferenceEquals(current, ancestor))
            {
                return true;
            }
        }

        return false;
    }

    private bool CanOpen(MenuItem item)
    {
        return Owns(item) && IsStateEligible(item) && item.HasSubmenu;
    }

    private bool IsEligible(MenuItem item)
    {
        return IsStateEligible(item) && item.IsAttached && UIElementVisibility.ParticipatesInInput(item);
    }

    private bool IsStateEligible(MenuItem item)
    {
        return root.IsEnabled && item.IsEnabled && item.Visibility == UI.Layout.Visibility.Visible;
    }

    private bool Owns(MenuItem item)
    {
        return ReferenceEquals(item.Session, this) && ReferenceEquals(item.MenuRoot, root);
    }

    private MenuItem TopLevelItem(MenuItem item)
    {
        for (MenuItem current = item; ; current = current.ParentMenuItem)
        {
            if (current.ParentMenuItem is null)
            {
                return current;
            }
        }
    }

    private void SetOpen(MenuItem item, bool value)
    {
        bool previous = synchronizing;
        synchronizing = true;
        try
        {
            SetOpenCore(item, value);
        }
        finally
        {
            synchronizing = previous;
        }
    }

    private void SetOpenCore(MenuItem item, bool value)
    {
        item.SetDismissScope(value ? DismissScope : item.Session is null ? null : DismissScope);
        item.SetSubmenuOpenFromSession(value);
    }

    private void ClearPendingFocus()
    {
        pendingFocusOwner = null;
        pendingFocusItem = null;
    }

    private bool IsWithinRoot(UIElement? candidate)
    {
        return IsWithin(candidate, root, useVisualParent: true) ||
            IsWithin(candidate, root, useVisualParent: false);
    }

    private static bool IsWithin(UIElement? candidate, UIElement ancestor, bool useVisualParent)
    {
        for (UIElement? current = candidate; current is not null; current = useVisualParent ? current.VisualParent : current.LogicalParent)
        {
            if (ReferenceEquals(current, ancestor))
            {
                return true;
            }
        }

        return false;
    }

    private static int IndexOfReference(IReadOnlyList<MenuItem> items, MenuItem candidate)
    {
        for (int index = 0; index < items.Count; index++)
        {
            if (ReferenceEquals(items[index], candidate))
            {
                return index;
            }
        }

        return -1;
    }
}
