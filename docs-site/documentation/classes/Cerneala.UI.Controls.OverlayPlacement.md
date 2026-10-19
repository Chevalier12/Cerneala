# OverlayPlacement Enum

## Definition
Namespace: `Cerneala.UI.Controls`

Assembly/Project: `Cerneala`

Source: `UI/Controls/OverlayPlacement.cs`

Specifies how an `Overlay` is positioned relative to its target.

```csharp
public enum OverlayPlacement
```

## Examples

```csharp
using Cerneala.UI.Controls;

Overlay overlay = new();
overlay.Placement = OverlayPlacement.AutoHorizontal;
```

## Fields

| Name | Description |
| --- | --- |
| `Auto` | Prefer below, flip above when needed, or use the side with more space. |
| `Bottom` | Place below and clamp to available viewport space. |
| `Top` | Place above and clamp to available viewport space. |
| `AutoHorizontal` | Prefer the target's right side, fall back to its left side, and clamp to the viewport when neither side can contain the overlay. |

## Remarks

All modes clamp projected geometry to the current `UIRoot` viewport. `Overlay.MaxHeight` limits only auto-sized overlays; when `Overlay.Height` is set, that height is used and `MaxHeight` is not applied. `AutoHorizontal` measures the content at its natural width, with an infinite available width, or at the target width clamped to the viewport when `Overlay.MatchTargetWidth` is `true`. It measures the content a second time, at the final width and height, only when the natural width is wider than the viewport, when `Overlay.Height` is set, or when `Overlay.MaxHeight` or the viewport height limits the natural height. For example, in a 120 × 80 viewport, content with a natural size of 30 × 15 is measured once at (∞, ∞) and keeps width 30, while content with a natural size of 200 × 15 is measured at (∞, ∞) and again at (120, 15). The overlay uses the same width on either side; it does not remeasure against the chosen side's space, so content wider than that side is clamped back into the viewport and can overlap the target.

## Applies to

Project: `Cerneala`

## See also

- `Overlay`
