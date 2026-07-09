# DebugAdorner Class

## Definition
Namespace: `Cerneala.UI.Diagnostics`  
Assembly/Project: `Cerneala`  
Source: `UI/Diagnostics/DebugAdorner.cs`

Draws a one-pixel diagnostic rectangle around its arranged bounds while hosting a single child element.

```csharp
public sealed class DebugAdorner : Decorator
```

Inheritance:  
`Object` -> `UiObject` -> `UIElement` -> `Control` -> `Decorator` -> `DebugAdorner`

## Examples

```csharp
using Cerneala.Drawing;
using Cerneala.UI.Controls;
using Cerneala.UI.Diagnostics;
using Cerneala.UI.Layout;

DebugAdorner adorner = new()
{
    AdornerColor = new DrawColor(255, 64, 64),
    Child = new Border
    {
        Padding = new Thickness(10),
        Child = new TextBlock { Text = "Layout area" }
    }
};
```

## Remarks

`DebugAdorner` is a retained UI diagnostic helper. It inherits `Decorator`, so it owns at most one `Child` and measures or arranges that child through the normal decorator layout behavior.

During rendering, the adorner reads `RenderContext.Bounds`. If either width or height is zero or negative, it skips drawing. Otherwise, it draws a rectangle over those bounds through the current `DrawingContext`, using `AdornerColor` and a stroke thickness of `1`.

Changing `AdornerColor` affects render invalidation because `AdornerColorProperty` is registered with `UiPropertyOptions.AffectsRender`.

## Constructors

| Name | Description |
| --- | --- |
| `DebugAdorner()` | Creates a new debug adorner with the default `AdornerColor` value. |

## Fields

| Name | Type | Description |
| --- | --- | --- |
| `AdornerColorProperty` | `UiProperty<DrawColor>` | Identifies the `AdornerColor` UI property. The default value is `new DrawColor(255, 64, 64)`, and the property affects rendering. |

## Properties

| Name | Type | Description |
| --- | --- | --- |
| `AdornerColor` | `DrawColor` | Gets or sets the rectangle stroke color used by the adorner. |
| `Child` | `UIElement?` | Inherited from `Decorator`. Gets or sets the single child element hosted by the adorner. |

## Protected Overrides

| Name | Description |
| --- | --- |
| `OnRender(RenderContext context)` | Draws the diagnostic rectangle when the render bounds have positive width and height. |

## Property Information

| Item | Value |
| --- | --- |
| Identifier field | `AdornerColorProperty` |
| Property type | `DrawColor` |
| Default value | `new DrawColor(255, 64, 64)` |
| Metadata/options | `UiPropertyOptions.AffectsRender` |

## Applies to

Cerneala retained UI diagnostics.

## See also

- `Cerneala.UI.Controls.Decorator`
- `Cerneala.UI.Diagnostics.DebugOverlay`
- `Cerneala.Drawing.DrawColor`
