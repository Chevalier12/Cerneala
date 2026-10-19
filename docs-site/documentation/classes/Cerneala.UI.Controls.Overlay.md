# Overlay Class

## Definition
Namespace: `Cerneala.UI.Controls`

Assembly/Project: `Cerneala`

Source: `UI/Controls/Overlay.cs`

Projects content into the topmost layer of the current `UIRoot` without creating a native window.

```csharp
public class Overlay : Control
```

## Examples

```csharp
using Cerneala.UI.Controls;

Button button = new() { Content = "Actions" };
Overlay menu = new()
{
    Content = new TextBlock { Text = "Actions" },
    PlacementTarget = button,
    Placement = OverlayPlacement.Auto,
    IsLightDismissEnabled = true,
    MatchTargetWidth = true,
    Height = 240,
    IsOpen = true
};
```

## Remarks

The content remains a logical descendant of the overlay but is visually projected into a root-owned layer. The inline overlay placeholder has `IsHitTestVisible` set to `false`; it remains logically visible so projected descendants can render, while only the projected presenter receives pointer input. The layer is attached only while overlays are open, renders last, and hit-tests projected children before ordinary content. Empty layer space is input-transparent.

Opening before attachment is deferred. Detaching closes the overlay. A `Closed` handler that throws does not leave the overlay half closed: the projected content is withdrawn, the layer is removed from the root when no other overlay is open, and a detached overlay has `IsOpen` set to `false`; the handler's exception then propagates to the caller of the close or the removal. For example, when the only open overlay has a throwing `Closed` handler, `overlay.IsOpen = false` throws that exception and the root no longer contains the layer. An `Unloaded` handler that throws does not leave it half closed either: removing the overlay's parent still closes it, raises `Closed` once, and withdraws the projected content, then the caller of the removal receives the handler's exception. `Auto` placement prefers below, flips above when needed, chooses the larger side when neither fits, and clamps the result to the viewport. Placement is refreshed for target layout and viewport changes.

Without an explicit `Height`, projected content is measured at its natural height and then limited by `MaxHeight`. Setting `Height` requests that exact projected height and overrides `MaxHeight`; the available viewport side remains the final safety limit. The overlay-specific `Height` does not enlarge the inline placeholder or its owner layout.

Without `MatchTargetWidth`, when the content asks for more height than the overlay gets (from `MaxHeight` or from the viewport side), the content is measured again at the height it gets, and the overlay width comes from that measure, as WPF measures `Popup` content within the screen. Content that scrolls vertically at that height therefore adds its vertical scrollbar to the overlay width instead of losing that width. For example, a `MenuBar` item with 30 submenu items of `Width = 140` and `Height = 34` (1020 tall) in an 820 tall root opens a submenu that is 140 + scrollbar width + 2 border wide, and each item is arranged 140 wide; a submenu that fits keeps its natural 142 width and shows no scrollbar.

Routed events raised on projected content follow the WPF `Popup` contract: the route goes from the content up to the projected presenter, then to the overlay, and then along the overlay's own ancestors to the `UIRoot`; it does not pass through the root-owned layer. Tunneling events run in the reverse order. For example, in a `Window` with a `MenuBar` holding `File > Open`, F5 pressed while `Open` has keyboard focus raises `Window.PreviewKeyDown`, `File.PreviewKeyDown`, the submenu overlay's `PreviewKeyDown`, `Open.PreviewKeyDown`, then `KeyDown` on `Open`, the overlay, `File`, and `Window`. A `Window.PreviewKeyDown` handler that sets `Handled` therefore stops the key before it reaches `Open`, and `InputBindings` and command bindings of the overlay's ancestors apply to the projected content. `Opened` and `Closed` are direct routed events, like the CLR events `Popup.Opened` and `Popup.Closed` in WPF: they run only the handlers of the overlay whose content is projected or withdrawn, and they never reach its ancestors. For example, when an open overlay holds a `ComboBox`, opening and closing the drop-down raises `Opened` and `Closed` on the drop-down's own overlay only; the enclosing overlay's `Opened` and `Closed` handlers do not run, because its content stays projected. State that describes where the pointer or the focus is stays visual: `IsPointerOver`, `MouseEnter`, `MouseLeave`, `IsKeyboardFocusWithin`, the resolved cursor, and the element that receives the focus, the pressed state, or the click command of a pointer press are resolved from the projected content's visual ancestors only. Hovering `Open` sets `Open.IsPointerOver` and leaves `File.IsPointerOver` unchanged, while the `MouseMove` event from `Open` still bubbles to `File`. Pointer hit testing and keyboard navigation order are unchanged.

Light-dismiss examines only the topmost eligible overlay. An exterior pointer press closes it without handling the event, so the same press continues to the underlying control. Owner and projected content form one focus domain.

## Fields and Properties

| Property | Type | Default | Description |
| --- | --- | --- | --- |
| `Content` | `object?` | `null` | Projected content. |
| `IsOpen` | `bool` | `false` | Requested projection state. |
| `PlacementTarget` | `UIElement?` | `null` | Anchor; the overlay itself is used when null. |
| `Placement` | `OverlayPlacement` | `Auto` | Placement policy: `Auto`, `Bottom`, and `Top` place the overlay vertically; `AutoHorizontal` places it to the right or left of the target. |
| `IsLightDismissEnabled` | `bool` | `false` | Enables exterior-click and focus-exit dismissal. |
| `MatchTargetWidth` | `bool` | `false` | Measures and arranges projected content at the target width, clamped to the viewport. |
| `Height` | `float` | `NaN` (automatic) | Exact projected height. Overrides `MaxHeight` and is clamped to available viewport space. |
| `MaxHeight` | `float` | Positive infinity | Maximum automatically sized projected height; accepts zero or a positive value. |

Each property has a same-named `UiProperty` identifier field.

## Events

| Name | Description |
| --- | --- |
| `Opened` | Raised when content is actually projected. Direct routed event (`OpenedEvent`): raised on this overlay only. |
| `Closed` | Raised when projected content is actually withdrawn. Direct routed event (`ClosedEvent`): raised on this overlay only. |

## Applies to

Project: `Cerneala`

## See also

- `OverlayPlacement`
- `UIRoot`
- `ToolTip`
- `ComboBox`
