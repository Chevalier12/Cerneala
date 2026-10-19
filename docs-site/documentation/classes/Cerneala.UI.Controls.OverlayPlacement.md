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

All modes clamp projected geometry to the current `UIRoot` viewport. `Overlay.MaxHeight` limits only auto-sized overlays; when `Overlay.Height` is set, that height is used and `MaxHeight` is not applied. `AutoHorizontal` measures the content at its natural width, with an infinite available width, or at the target width clamped to the viewport when `Overlay.MatchTargetWidth` is `true`. Without `MatchTargetWidth`, when the content asks for more height than the overlay gets, it measures the content again at that height, still with an infinite available width, and takes the width from that measure, so a vertical scrollbar is included in the width (see `Overlay`). It measures the content a last time, at the final width and height, only when `Overlay.Height` is set or when the size from the previous measure is wider than the final width or taller than the final height, for example when the natural width is wider than the viewport, or when `MatchTargetWidth` is `true` and the content is wider than the target. For example, in a 120 × 80 viewport, content with a natural size of 30 × 15 is measured once at (∞, ∞) and keeps width 30, content with a natural size of 200 × 15 is measured at (∞, ∞) and again at (120, 15), and content with a natural size of 30 × 200 that does not scroll is measured at (∞, ∞), at (∞, 80), and at (30, 80). The overlay uses the same width on either side; it does not remeasure against the chosen side's space, so content wider than that side is clamped back into the viewport and can overlap the target.

## Applies to

Project: `Cerneala`

## See also

- `Overlay`
