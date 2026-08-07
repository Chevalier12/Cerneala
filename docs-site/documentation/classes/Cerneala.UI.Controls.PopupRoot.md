# PopupRoot Class

## Definition
Namespace: `Cerneala.UI.Controls`

Assembly/Project: `Cerneala`

Source: `UI/Controls/PopupRoot.cs`

Represents the compatibility content root used by popup-style controls.

```csharp
public class PopupRoot : ContentControl
```

## Examples

```csharp
ToolTip toolTip = new() { Content = new TextBlock { Text = "Details" } };
PopupRoot root = toolTip.PopupRoot;
```

## Remarks

`PopupRoot` keeps its public API for compatibility. In `ToolTip`, it is hosted inside the `UIRoot` overlay layer rather than inserted inline under the tooltip. It does not create or own a native window.

Its measure and arrange behavior is inherited from `ContentControl`, including transactional ownership checks for `UIElement` content.

## Properties

| Name | Description |
| --- | --- |
| `Content` | Inherited popup payload. |
| `ComponentTemplate` | Inherited component template. |

## Applies to

Project: `Cerneala`

## See also

- `ToolTip`
- `Overlay`
- `ContentControl`
