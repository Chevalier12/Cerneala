# DebugOverlay Class

## Definition
Namespace: `Cerneala.UI.Diagnostics`

Assembly/Project: `Cerneala`

Source: `UI/Diagnostics/DebugOverlay.cs`

Creates a retained UI overlay element that displays diagnostic text inside a styled border.

```csharp
public sealed class DebugOverlay
```

## Examples

```csharp
using Cerneala.UI.Diagnostics;

DebugOverlay overlay = new();
overlay.Text = "Frame: 42";

UIElement root = overlay.Root;
```

## Remarks

`DebugOverlay` builds a `Border` containing a `TextBlock`. The text block starts empty, uses white foreground, a 13-point font size, and can receive an optional resource provider and font resource id through the constructor.

Setting `Text` normalizes `null` to an empty string. When the new text matches the current text, no work is done. When it changes, the overlay updates the inner `TextBlock` and invalidates the root for measure and render with the reason `"Debug overlay text changed"`.

## Constructors

| Name | Description |
| --- | --- |
| `DebugOverlay(IResourceProvider?, ResourceId<FontResource>?)` | Initializes the overlay with optional font resource services. |

## Properties

| Name | Description |
| --- | --- |
| `Root` | Gets the retained UI element that should be inserted into a visual tree. |
| `Text` | Gets or sets the diagnostic text displayed by the overlay. |

## Applies to

Cerneala retained UI diagnostics.

## See also

- `Cerneala.UI.Controls.Border`
- `Cerneala.UI.Controls.TextBlock`
- `Cerneala.UI.Invalidation.InvalidationFlags`
