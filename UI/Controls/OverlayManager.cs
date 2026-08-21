using Cerneala.UI.Elements;
using Cerneala.UI.Input;
using Cerneala.UI.Invalidation;
using Cerneala.UI.Layout;

namespace Cerneala.UI.Controls;

internal sealed class OverlayManager
{
    private readonly UIRoot root;
    private readonly OverlayLayer layer;
    private readonly List<Overlay> openOverlays = [];
    private bool layerAttached;

    public OverlayManager(UIRoot root)
    {
        this.root = root ?? throw new ArgumentNullException(nameof(root));
        layer = new OverlayLayer(root);
        root.VisualChildren.Changed += OnRootVisualChildrenChanged;
        root.Handlers.AddHandler(InputEvents.PreviewMouseDownEvent, OnPreviewMouseDown, handledEventsToo: true);
        root.Handlers.AddHandler(InputEvents.PreviewLostKeyboardFocusEvent, OnPreviewLostKeyboardFocus, handledEventsToo: true);
    }

    public void Show(Overlay overlay)
    {
        ArgumentNullException.ThrowIfNull(overlay);
        if (!ReferenceEquals(overlay.Root, root))
        {
            return;
        }

        EnsureLayer();
        if (openOverlays.Remove(overlay))
        {
            layer.Remove(overlay);
        }

        layer.Add(overlay);
        openOverlays.Add(overlay);
        overlay.SetProjected(true);
    }

    public void Hide(Overlay overlay)
    {
        ArgumentNullException.ThrowIfNull(overlay);
        if (!openOverlays.Remove(overlay))
        {
            return;
        }

        layer.Remove(overlay);
        overlay.SetProjected(false);
        if (openOverlays.Count == 0)
        {
            layerAttached = false;
            root.VisualChildren.Remove(layer);
        }
    }

    public void InvalidatePlacement(Overlay overlay)
    {
        if (openOverlays.Contains(overlay))
        {
            layer.InvalidatePlacement();
        }
    }

    public void OnElementArranged(UIElement element)
    {
        foreach (Overlay overlay in openOverlays)
        {
            if (ReferenceEquals(overlay.EffectivePlacementTarget, element))
            {
                layer.InvalidatePlacement();
                return;
            }
        }
    }

    public void OnViewportChanged()
    {
        if (openOverlays.Count > 0)
        {
            layer.InvalidatePlacement();
        }
    }

    private void EnsureLayer()
    {
        if (!layerAttached)
        {
            root.VisualChildren.Add(layer);
            layerAttached = true;
        }

        BringLayerToFront();
    }

    private void OnRootVisualChildrenChanged(object? sender, ElementTreeChange change)
    {
        if (layerAttached && change.Kind == ElementTreeChangeKind.Added)
        {
            BringLayerToFront();
        }
    }

    private void BringLayerToFront()
    {
        int layerIndex = -1;
        for (int index = 0; index < root.VisualChildren.Count; index++)
        {
            if (ReferenceEquals(root.VisualChildren[index], layer))
            {
                layerIndex = index;
                break;
            }
        }

        int lastIndex = root.VisualChildren.Count - 1;
        if (layerIndex >= 0 && layerIndex != lastIndex)
        {
            root.VisualChildren.Move(layerIndex, lastIndex);
        }
    }

    private void OnPreviewMouseDown(UiElementId _, RoutedEventArgs args)
    {
        Overlay? overlay = TopmostLightDismissOverlay();
        UIElement? source = ResolveElement(args.OriginalSource);
        if (overlay is null || IsWithinDismissDomain(source, overlay))
        {
            return;
        }

        Dismiss(overlay);
    }

    private void OnPreviewLostKeyboardFocus(UiElementId _, RoutedEventArgs args)
    {
        Overlay? overlay = TopmostLightDismissOverlay();
        if (overlay is null || args is not KeyboardFocusChangedEventArgs focusArgs)
        {
            return;
        }

        UIElement? next = ResolveElement(focusArgs.NewFocus);
        if (!IsWithinDismissDomain(next, overlay))
        {
            Dismiss(overlay);
        }
    }

    private bool IsWithinDismissDomain(UIElement? candidate, Overlay overlay)
    {
        OverlayDismissScope? scope = overlay.DismissScope;
        if (scope?.Contains(candidate) == true)
        {
            return true;
        }

        foreach (Overlay member in openOverlays)
        {
            if ((scope is null && !ReferenceEquals(member, overlay)) ||
                (scope is not null && !ReferenceEquals(member.DismissScope, scope)))
            {
                continue;
            }

            if (IsWithin(candidate, member.ProjectedPresenter) ||
                IsWithin(candidate, member.EffectivePlacementTarget))
            {
                return true;
            }
        }

        return false;
    }

    private static void Dismiss(Overlay overlay)
    {
        if (overlay.DismissScope is OverlayDismissScope scope)
        {
            scope.Dismiss();
        }
        else
        {
            overlay.IsOpen = false;
        }
    }

    private Overlay? TopmostLightDismissOverlay()
    {
        for (int index = openOverlays.Count - 1; index >= 0; index--)
        {
            if (openOverlays[index].IsLightDismissEnabled)
            {
                return openOverlays[index];
            }
        }

        return null;
    }

    private UIElement? ResolveElement(object? value)
    {
        if (value is UIElement element)
        {
            return element;
        }

        return value is UiElementId id && root.ElementIds.TryGetElement(id, out UIElement? resolved)
            ? resolved
            : null;
    }

    private static bool IsWithin(UIElement? candidate, UIElement ancestor)
    {
        for (UIElement? current = candidate; current is not null; current = current.VisualParent)
        {
            if (ReferenceEquals(current, ancestor))
            {
                return true;
            }
        }

        for (UIElement? current = candidate; current is not null; current = current.LogicalParent)
        {
            if (ReferenceEquals(current, ancestor))
            {
                return true;
            }
        }

        return false;
    }

    private sealed class OverlayLayer : global::Cerneala.UI.Layout.Panels.Panel
    {
        private readonly UIRoot root;
        private readonly Dictionary<ContentPresenter, Overlay> overlays = new(ReferenceEqualityComparer.Instance);

        public OverlayLayer(UIRoot root)
        {
            this.root = root;
        }

        public void Add(Overlay overlay)
        {
            ContentPresenter presenter = overlay.ProjectedPresenter;
            overlays.Add(presenter, overlay);
            VisualChildren.Add(presenter);
            InvalidatePlacement();
        }

        public void Remove(Overlay overlay)
        {
            ContentPresenter presenter = overlay.ProjectedPresenter;
            VisualChildren.Remove(presenter);
            overlays.Remove(presenter);
            InvalidatePlacement();
        }

        public void InvalidatePlacement()
        {
            IncrementLayoutVersion();
            IncrementRenderVersion();
            Invalidate(
                InvalidationFlags.Measure | InvalidationFlags.Arrange |
                InvalidationFlags.Render | InvalidationFlags.HitTest,
                "Overlay placement changed");
        }

        protected override LayoutSize MeasureCore(MeasureContext context)
        {
            LayoutSize viewport = ViewportSize(context.AvailableSize);
            foreach (UIElement child in VisualChildren)
            {
                Overlay overlay = overlays[(ContentPresenter)child];
                MeasureOverlay(overlay, viewport, context.Rounding);
            }

            return viewport;
        }

        protected override LayoutRect ArrangeCore(ArrangeContext context)
        {
            LayoutSize viewport = ViewportSize(context.FinalRect.Size);
            foreach (UIElement child in VisualChildren)
            {
                Overlay overlay = overlays[(ContentPresenter)child];
                LayoutRect rect = MeasureOverlay(overlay, viewport, context.Rounding);
                child.Arrange(new ArrangeContext(rect, context.Rounding));
            }

            return context.FinalRect;
        }

        private LayoutRect MeasureOverlay(Overlay overlay, LayoutSize viewport, LayoutRounding rounding)
        {
            return overlay.Placement == OverlayPlacement.AutoHorizontal
                ? MeasureHorizontalOverlay(overlay, viewport, rounding)
                : MeasureVerticalOverlay(overlay, viewport, rounding);
        }

        private static LayoutRect MeasureVerticalOverlay(
            Overlay overlay,
            LayoutSize viewport,
            LayoutRounding rounding)
        {
            UIElement target = overlay.EffectivePlacementTarget;
            LayoutRect targetBounds = target.ArrangedBounds;
            float targetBottom = targetBounds.Y + targetBounds.Height;
            float below = MathF.Max(0, viewport.Height - targetBottom);
            float above = MathF.Max(0, targetBounds.Y);
            float initialLimit = overlay.Placement switch
            {
                OverlayPlacement.Bottom => below,
                OverlayPlacement.Top => above,
                _ => MathF.Max(below, above)
            };
            bool hasExplicitHeight = !float.IsNaN(overlay.Height);
            float naturalWidthLimit = overlay.MatchTargetWidth
                ? MathF.Min(targetBounds.Width, viewport.Width)
                : float.PositiveInfinity;
            float initialHeightLimit = hasExplicitHeight
                ? MathF.Min(initialLimit, overlay.Height)
                : float.PositiveInfinity;

            ContentPresenter presenter = overlay.ProjectedPresenter;
            LayoutSize desired = presenter.Measure(new MeasureContext(
                new LayoutSize(naturalWidthLimit, initialHeightLimit),
                rounding));
            float width = overlay.MatchTargetWidth
                ? MathF.Min(targetBounds.Width, viewport.Width)
                : MathF.Min(desired.Width, viewport.Width);

            if (width != naturalWidthLimit)
            {
                desired = presenter.Measure(new MeasureContext(
                    new LayoutSize(width, initialHeightLimit),
                    rounding));
            }

            float requestedHeight = hasExplicitHeight
                ? overlay.Height
                : MathF.Min(desired.Height, overlay.MaxHeight);

            bool placeBelow = overlay.Placement switch
            {
                OverlayPlacement.Bottom => true,
                OverlayPlacement.Top => false,
                _ when requestedHeight <= below => true,
                _ when requestedHeight <= above => false,
                _ => below >= above
            };

            float sideLimit = placeBelow ? below : above;
            float height = MathF.Min(requestedHeight, sideLimit);
            if (hasExplicitHeight || desired.Height > height || width != naturalWidthLimit)
            {
                presenter.Measure(new MeasureContext(new LayoutSize(width, height), rounding));
            }

            float x = Math.Clamp(targetBounds.X, 0, MathF.Max(0, viewport.Width - width));
            float y = placeBelow
                ? Math.Clamp(targetBottom, 0, MathF.Max(0, viewport.Height - height))
                : Math.Clamp(targetBounds.Y - height, 0, MathF.Max(0, viewport.Height - height));
            return new LayoutRect(x, y, width, height);
        }

        private static LayoutRect MeasureHorizontalOverlay(
            Overlay overlay,
            LayoutSize viewport,
            LayoutRounding rounding)
        {
            LayoutRect targetBounds = overlay.EffectivePlacementTarget.ArrangedBounds;
            float targetRight = targetBounds.X + targetBounds.Width;
            float right = MathF.Max(0, viewport.Width - targetRight);
            float left = MathF.Max(0, targetBounds.X);
            bool hasExplicitHeight = !float.IsNaN(overlay.Height);
            float naturalWidthLimit = overlay.MatchTargetWidth
                ? MathF.Min(targetBounds.Width, viewport.Width)
                : viewport.Width;
            float naturalHeightLimit = hasExplicitHeight
                ? MathF.Min(overlay.Height, viewport.Height)
                : float.PositiveInfinity;

            ContentPresenter presenter = overlay.ProjectedPresenter;
            LayoutSize desired = presenter.Measure(new MeasureContext(
                new LayoutSize(naturalWidthLimit, naturalHeightLimit),
                rounding));
            float requestedWidth = overlay.MatchTargetWidth
                ? MathF.Min(targetBounds.Width, viewport.Width)
                : MathF.Min(desired.Width, viewport.Width);
            bool fitsRight = requestedWidth <= right;
            bool fitsLeft = requestedWidth <= left;
            bool placeRight = fitsRight;
            float width = requestedWidth;
            float requestedHeight = hasExplicitHeight
                ? overlay.Height
                : MathF.Min(desired.Height, overlay.MaxHeight);
            float height = MathF.Min(requestedHeight, viewport.Height);
            if (hasExplicitHeight || desired.Height > height)
            {
                presenter.Measure(new MeasureContext(new LayoutSize(width, height), rounding));
            }

            float x = placeRight ? targetRight : targetBounds.X - width;
            x = Math.Clamp(x, 0, MathF.Max(0, viewport.Width - width));
            float y = Math.Clamp(targetBounds.Y, 0, MathF.Max(0, viewport.Height - height));
            return new LayoutRect(x, y, width, height);
        }

        private LayoutSize ViewportSize(LayoutSize fallback)
        {
            float width = root.ViewportWidth > 0 ? root.ViewportWidth : fallback.Width;
            float height = root.ViewportHeight > 0 ? root.ViewportHeight : fallback.Height;
            return new LayoutSize(
                float.IsFinite(width) ? width : 0,
                float.IsFinite(height) ? height : 0);
        }
    }
}
