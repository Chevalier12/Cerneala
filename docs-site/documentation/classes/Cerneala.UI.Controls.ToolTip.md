# ToolTip Class

## Definition
Namespace: `Cerneala.UI.Controls`

Assembly/Project: `Cerneala`

Source: `UI/Controls/ToolTip.cs`

Hosts tooltip content in an internal root overlay.

```csharp
public class ToolTip : Control
```

## Examples

```csharp
ToolTip toolTip = new()
{
    Content = new TextBlock { Text = "Saved" },
    IsOpen = true
};
```

## Remarks

`ToolTip` uses `Overlay` rather than an inline visual or native popup window. Its own desired size remains zero; open content is projected above ordinary content and participates in normal retained rendering, hit testing, and routed input.

`PopupRoot` remains public for compatibility and hosts `Content` inside the projected layer. Invalid `UIElement` content ownership is rejected transactionally without disturbing the previous value.

## Fields and Properties

| Name | Type | Description |
| --- | --- | --- |
| `IsOpenProperty` | `UiProperty<bool>` | Identifies `IsOpen`; default `false`. |
| `Content` | `object?` | Tooltip payload hosted by `PopupRoot`. |
| `IsOpen` | `bool` | Gets or sets the requested overlay state. |
| `PopupRoot` | `PopupRoot` | Compatibility host projected by the internal overlay. |

## Events

| Name | Description |
| --- | --- |
| `Opened` | Raised when the overlay is actually projected. |
| `Closed` | Raised when the overlay is actually withdrawn. |

## Applies to

Project: `Cerneala`

## See also

- `Overlay`
- `PopupRoot`
