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
    MaxHeight = 320,
    IsOpen = true
};
```

## Remarks

The content remains a logical descendant of the overlay but is visually projected into a root-owned layer. The inline overlay placeholder has `IsHitTestVisible` set to `false`; it remains logically visible so projected descendants can render, while only the projected presenter receives pointer input. The layer is attached only while overlays are open, renders last, and hit-tests projected children before ordinary content. Empty layer space is input-transparent.

Opening before attachment is deferred. Detaching closes the overlay. `Auto` placement prefers below, flips above when needed, chooses the larger side when neither fits, and clamps the result to the viewport. Placement is refreshed for target layout and viewport changes.

Light-dismiss examines only the topmost eligible overlay. An exterior pointer press closes it without handling the event, so the same press continues to the underlying control. Owner and projected content form one focus domain.

## Fields and Properties

| Property | Type | Default | Description |
| --- | --- | --- | --- |
| `Content` | `object?` | `null` | Projected content. |
| `IsOpen` | `bool` | `false` | Requested projection state. |
| `PlacementTarget` | `UIElement?` | `null` | Anchor; the overlay itself is used when null. |
| `Placement` | `OverlayPlacement` | `Auto` | Vertical placement policy. |
| `IsLightDismissEnabled` | `bool` | `false` | Enables exterior-click and focus-exit dismissal. |
| `MatchTargetWidth` | `bool` | `false` | Measures and arranges projected content at the target width, clamped to the viewport. |
| `MaxHeight` | `float` | Positive infinity | Maximum projected height; accepts zero or a positive value. |

Each property has a same-named `UiProperty` identifier field.

## Events

| Name | Description |
| --- | --- |
| `Opened` | Raised when content is actually projected. |
| `Closed` | Raised when projected content is actually withdrawn. |

## Applies to

Project: `Cerneala`

## See also

- `OverlayPlacement`
- `UIRoot`
- `ToolTip`
- `ComboBox`
