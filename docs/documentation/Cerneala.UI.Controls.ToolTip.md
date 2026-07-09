# ToolTip Class

## Definition
Namespace: `Cerneala.UI.Controls`

Assembly/Project: `Cerneala`

Source: `UI/Controls/ToolTip.cs`

Represents a popup-style control that hosts optional content through an internal `PopupRoot` while `IsOpen` is `true`.

```csharp
public class ToolTip : Control
```

Inheritance:
`object` -> `UiObject` -> `UIElement` -> `Control` -> `ToolTip`

## Examples
Create a tooltip, assign content, open it, then run layout so the internal popup root can host and arrange the content.

```csharp
using Cerneala.UI.Controls;
using Cerneala.UI.Layout;

ToolTip toolTip = new()
{
    Content = new TextBlock { Text = "Saved" },
    IsOpen = true
};

LayoutSize desired = toolTip.Measure(new MeasureContext(new LayoutSize(200, 80)));
toolTip.Arrange(new ArrangeContext(new LayoutRect(0, 0, desired.Width, desired.Height)));
```

Close a tooltip by setting `IsOpen` to `false`. The popup root is detached and its content is cleared.

```csharp
toolTip.IsOpen = false;
```

## Remarks
`ToolTip` does not host its `Content` directly. When `IsOpen` is `true`, the control attaches its `PopupRoot` as both a logical and visual child, assigns `Content` to that popup root, and delegates measure and arrange to the popup root. When `IsOpen` is `false`, measure returns `LayoutSize.Zero`; if a popup root was attached, it is removed from the tooltip's child collections and its content is reset to `null`.

Changing `IsOpen` refreshes the popup root. The `IsOpenProperty` metadata affects measure, render, and hit testing, so opening or closing a tooltip participates in retained layout and input invalidation.

Changing `Content` while the tooltip is open refreshes `PopupRoot.Content`. If assigning content fails, the previous `Content` value is restored; when the popup root was already attached, its content is restored too. For `UIElement` content, the popup root enforces the same ownership rules as `ContentControl`: an element that already has a logical or visual parent cannot be reparented into the tooltip.

The popup root overlay participates in hit testing and routed input while open. Tests cover mouse hit testing and routing through `PopupRoot` after the tooltip has been measured and arranged.

## Constructors
| Name | Description |
| --- | --- |
| `ToolTip()` | Initializes a new instance of `ToolTip`. |

## Fields
| Name | Type | Description |
| --- | --- | --- |
| `IsOpenProperty` | `UiProperty<bool>` | Identifies the `IsOpen` UI property. The default value is `false`; metadata affects measure, render, and hit testing. |

## Properties
| Name | Type | Description |
| --- | --- | --- |
| `Content` | `object?` | Gets or sets the value hosted by `PopupRoot` while the tooltip is open. |
| `IsOpen` | `bool` | Gets or sets whether the tooltip popup root is attached and participating in layout, rendering, hit testing, and input routing. |
| `PopupRoot` | `PopupRoot` | Gets the internal popup root used to host the tooltip content. |

## Methods
This class does not declare additional public methods.

## Events
This class does not declare additional public events.

## Property Information
| Property | Identifier field | Default value | Metadata/options |
| --- | --- | --- | --- |
| `IsOpen` | `IsOpenProperty` | `false` | `AffectsMeasure`, `AffectsRender`, `AffectsHitTest` |

## Applies To
Project: `Cerneala`

UI area: retained controls, popup-style content hosting, layout, hit testing, and routed input.

## See Also
- `Control`
- `PopupRoot`
- `ContentControl`
- `UIElement`
