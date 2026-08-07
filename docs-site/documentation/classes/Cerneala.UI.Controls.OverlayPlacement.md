# OverlayPlacement Enum

## Definition
Namespace: `Cerneala.UI.Controls`

Assembly/Project: `Cerneala`

Source: `UI/Controls/OverlayPlacement.cs`

Specifies how an `Overlay` is positioned vertically relative to its target.

```csharp
public enum OverlayPlacement
```

## Examples

```csharp
using Cerneala.UI.Controls;

Overlay overlay = new();
overlay.Placement = OverlayPlacement.Bottom;
```

## Fields

| Name | Description |
| --- | --- |
| `Auto` | Prefer below, flip above when needed, or use the side with more space. |
| `Bottom` | Place below and clamp to available viewport space. |
| `Top` | Place above and clamp to available viewport space. |

## Remarks

All modes clamp projected geometry to the current `UIRoot` viewport and respect `Overlay.MaxHeight`.

## Applies to

Project: `Cerneala`

## See also

- `Overlay`
