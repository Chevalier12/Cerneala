# AutoToolTipPlacement Enum

## Definition
Namespace: `Cerneala.UI.Controls.Primitives`

Assembly/Project: `Cerneala`

Source: `UI/Controls/Primitives/AutoToolTipPlacement.cs`

Specifies where a `Slider` shows the tooltip with its value while its thumb is dragged.

```csharp
public enum AutoToolTipPlacement
```

## Examples

```csharp
using Cerneala.UI.Controls;
using Cerneala.UI.Controls.Primitives;

Slider slider = new()
{
    Minimum = 0,
    Maximum = 10,
    AutoToolTipPlacement = AutoToolTipPlacement.TopLeft,
    AutoToolTipPrecision = 1
};
// While the user drags the thumb, a ToolTip centered above it shows the value, for example 4.7.
```

## Fields

| Name | Description |
| --- | --- |
| `None` | No tooltip is shown. This is the default of `Slider.AutoToolTipPlacement`. |
| `TopLeft` | The tooltip is centered above a horizontal slider's thumb, and left of a vertical slider's thumb, centered on it vertically. |
| `BottomRight` | The tooltip is centered below a horizontal slider's thumb, and right of a vertical slider's thumb, centered on it vertically. |

## Remarks

The tooltip opens when a thumb drag starts, follows the thumb, and closes when the drag ends. It stays inside the window's viewport. The values match WPF's `AutoToolTipPlacement`. See `Slider` for the text format.

## Applies to

Project: `Cerneala`

## See also

- `Cerneala.UI.Controls.Slider`
- `Cerneala.UI.Controls.ToolTip`
