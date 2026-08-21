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

All modes clamp projected geometry to the current `UIRoot` viewport and respect `Overlay.MaxHeight`. `AutoHorizontal` remeasures content against the selected lateral space when it fits on one side; oversized content is constrained to the viewport.

## Applies to

Project: `Cerneala`

## See also

- `Overlay`
