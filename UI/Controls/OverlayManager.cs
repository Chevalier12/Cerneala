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
        if (overlay is null || IsWithin(source, overlay.ProjectedPresenter) || IsWithin(source, overlay.EffectivePlacementTarget))
        {
            return;
        }

        overlay.IsOpen = false;
    }

    private void OnPreviewLostKeyboardFocus(UiElementId _, RoutedEventArgs args)
    {
        Overlay? overlay = TopmostLightDismissOverlay();
        if (overlay is null || args is not KeyboardFocusChangedEventArgs focusArgs)
        {
            return;
        }

        UIElement? next = ResolveElement(focusArgs.NewFocus);
        if (!IsWithin(next, overlay.ProjectedPresenter) && !IsWithin(next, overlay.EffectivePlacementTarget))
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
            initialLimit = MathF.Min(initialLimit, overlay.MaxHeight);
            float measureWidth = overlay.MatchTargetWidth
                ? MathF.Min(targetBounds.Width, viewport.Width)
                : viewport.Width;

            ContentPresenter presenter = overlay.ProjectedPresenter;
            LayoutSize desired = presenter.Measure(new MeasureContext(
                new LayoutSize(measureWidth, initialLimit),
                rounding));

            bool placeBelow = overlay.Placement switch
            {
                OverlayPlacement.Bottom => true,
                OverlayPlacement.Top => false,
                _ when desired.Height <= below => true,
                _ when desired.Height <= above => false,
                _ => below >= above
            };

            float sideLimit = MathF.Min(placeBelow ? below : above, overlay.MaxHeight);
            if (sideLimit != initialLimit)
            {
                desired = presenter.Measure(new MeasureContext(
                    new LayoutSize(measureWidth, sideLimit),
                    rounding));
            }

            float width = overlay.MatchTargetWidth
                ? targetBounds.Width
                : desired.Width;
            width = MathF.Min(width, viewport.Width);
            float height = MathF.Min(desired.Height, sideLimit);
            float x = Math.Clamp(targetBounds.X, 0, MathF.Max(0, viewport.Width - width));
            float y = placeBelow
                ? Math.Clamp(targetBottom, 0, MathF.Max(0, viewport.Height - height))
                : Math.Clamp(targetBounds.Y - height, 0, MathF.Max(0, viewport.Height - height));
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
