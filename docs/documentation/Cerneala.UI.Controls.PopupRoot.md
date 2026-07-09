# PopupRoot Class

## Definition
Namespace: `Cerneala.UI.Controls`

Assembly/Project: `Cerneala`

Source: `UI/Controls/PopupRoot.cs`

Represents a content host used as the root element for popup-style content.

```csharp
public class PopupRoot : ContentControl
```

Inheritance:
`object` -> `UiObject` -> `UIElement` -> `Control` -> `ContentControl` -> `PopupRoot`

## Examples
Create a popup root and assign retained UI content to it:

```csharp
using Cerneala.UI.Controls;

PopupRoot popupRoot = new()
{
    Content = new TextBlock
    {
        Text = "Saved"
    }
};
```

Access the popup root owned by a `ToolTip`:

```csharp
using Cerneala.UI.Controls;

ToolTip toolTip = new()
{
    Content = new TextBlock { Text = "Open details" },
    IsOpen = true
};

PopupRoot popupRoot = toolTip.PopupRoot;
```

## Remarks
`PopupRoot` is a specialized `ContentControl` with no additional public properties, fields, methods, or events of its own. Its layout overrides currently delegate directly to `ContentControl`, so untemplated `UIElement` content is measured and arranged using the inherited content-hosting behavior.

`ToolTip` owns a private `PopupRoot` instance and exposes it through `ToolTip.PopupRoot`. When the tooltip is open, the root receives the tooltip content and is added to the tooltip's logical and visual children. When the tooltip closes, the root content is cleared and the root is removed from those child collections.

Use the inherited `Content` property to assign the popup payload. Because `PopupRoot` inherits `ContentControl`, `UIElement` content follows the same ownership checks as other content controls: it cannot already have a logical or visual parent, cannot be the owner itself, cannot be an ancestor of the owner, and cannot belong to a different root.

## Constructors
| Name | Description |
| --- | --- |
| `PopupRoot()` | Initializes a new `PopupRoot` instance. |

## Public Members
`PopupRoot` declares no additional public members beyond its constructor.

## Relevant Inherited Properties
| Name | Type | Declared By | Description |
| --- | --- | --- | --- |
| `Content` | `object?` | `ContentControl` | Gets or sets the popup content value. `UIElement` content may become a logical and visual child when the root is untemplated. |
| `Template` | `ControlTemplate?` | `Control` | Gets or sets the classic control template used by inherited layout behavior. |
| `ComponentTemplate` | `ComponentTemplate?` | `Control` | Gets or sets the component template, which takes precedence over `Template`. |
| `Padding` | `Thickness` | `Control` | Gets or sets inherited padding used by `ContentControl` layout insets. |
| `BorderThickness` | `Thickness` | `Control` | Gets or sets inherited border thickness used by `ContentControl` layout insets. |

## Protected Methods
| Name | Return Type | Description |
| --- | --- | --- |
| `MeasureCore(MeasureContext context)` | `LayoutSize` | Delegates to `ContentControl.MeasureCore(context)`. |
| `ArrangeCore(ArrangeContext context)` | `LayoutRect` | Delegates to `ContentControl.ArrangeCore(context)`. |

## Applies To
Project: `Cerneala`

UI area: retained controls, content hosting, and tooltip popup composition.

## See Also
- `ContentControl`
- `ToolTip`
- `Control`
- `UIElement`
