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

All modes clamp projected geometry to the current `UIRoot` viewport. `Overlay.MaxHeight` limits only auto-sized overlays; when `Overlay.Height` is set, that height is used and `MaxHeight` is not applied. `AutoHorizontal` measures the content once with a width limited to the viewport and uses the same width on either side; it does not remeasure against the chosen side's space, so content wider than that side is clamped back into the viewport and can overlap the target.

## Applies to

Project: `Cerneala`

## See also

- `Overlay`
