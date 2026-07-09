# Decorator Class

## Definition
Namespace: `Cerneala.UI.Controls`

Assembly/Project: `Cerneala`

Source: `UI/Controls/Decorator.cs`

Defines a single-child control that owns one `UIElement` in both the logical and visual trees and lays it out inside the inherited padding and border thickness.

```csharp
public class Decorator : Control
```

Inheritance:
`object` -> `UIElement` -> `Control` -> `Decorator`

Derived:
`Border`, `DebugAdorner`

## Examples
The following example creates a decorator with one child. During layout, the child is measured and arranged inside the decorator's combined `Padding` and `BorderThickness` insets.

```csharp
using Cerneala.UI.Controls;
using Cerneala.UI.Elements;
using Cerneala.UI.Layout;

internal static class DecoratorExample
{
    public static void Run()
    {
        Decorator decorator = new()
        {
            Padding = new Thickness(1),
            BorderThickness = new Thickness(2),
            Child = new FixedElement(new LayoutSize(10, 5))
        };

        LayoutSize desired = decorator.Measure(new MeasureContext(new LayoutSize(100, 100)));
        decorator.Arrange(new ArrangeContext(new LayoutRect(0, 0, 30, 20)));
    }

    private sealed class FixedElement(LayoutSize desiredSize) : UIElement
    {
        protected override LayoutSize MeasureCore(MeasureContext context)
        {
            return desiredSize;
        }
    }
}
```

## Remarks
`Decorator` is the base implementation for controls that wrap exactly one `UIElement`. Setting `Child` adds that element to both `LogicalChildren` and `VisualChildren`. Replacing the child removes the old element from both child collections before attaching the new element.

The assigned child must be unparented and cannot be the decorator itself, one of the decorator's ancestors, or an element already attached under a different root. Invalid assignments throw `InvalidOperationException`. If an assignment fails while replacing an existing child, the previous child is restored.

Layout uses the inherited `Padding` and `BorderThickness` from `Control` as a single inset. `MeasureCore` measures the child with the available size deflated by those insets, then returns the child's desired size inflated by the same insets. `ArrangeCore` arranges the child inside the final rectangle deflated by those insets.

Changing `Child` invalidates measure and render for the decorator.

## Constructors
| Name | Description |
| --- | --- |
| `Decorator()` | Initializes a new `Decorator` instance. |

## Properties
| Name | Type | Description |
| --- | --- | --- |
| `Child` | `UIElement?` | Gets or sets the single element owned by the decorator. The value is added as both a logical and visual child. |

## Relevant Inherited Properties
| Name | Declared By | Description |
| --- | --- | --- |
| `Padding` | `Control` | Contributes to the inset used when measuring and arranging `Child`. |
| `BorderThickness` | `Control` | Contributes to the inset used when measuring and arranging `Child`. |
| `Background` | `Control` | Inherited render property for the control background. |
| `BorderColor` | `Control` | Inherited render property for the control border color. |
| `Template` | `Control` | Inherited control template property. `Decorator` still owns its `Child` directly. |
| `ComponentTemplate` | `Control` | Inherited component template property. |

## Layout Behavior
| Operation | Behavior |
| --- | --- |
| Measure | Measures `Child` with `AvailableSize` reduced by `Padding + BorderThickness`; returns the measured child size expanded by the same inset. |
| Arrange | Arranges `Child` in the final rectangle reduced by `Padding + BorderThickness`; returns the original final rectangle. |
| No child | Measures as `LayoutSize.Zero` plus insets and arranges no child. |

## Exceptions
| Member | Exception | Condition |
| --- | --- | --- |
| `Child` setter | `InvalidOperationException` | The new child is the decorator itself, an ancestor of the decorator, already parented, or belongs to another root. |

## Applies To
Cerneala UI retained controls.

## See Also
- `Cerneala.UI.Controls.Control`
- `Cerneala.UI.Controls.Border`
- `Cerneala.UI.Controls.ContentControl`
