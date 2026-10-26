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
`object` -> `UiObject` -> `UIElement` -> `Control` -> `Decorator`

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
`Decorator` is the base implementation for controls that wrap exactly one `UIElement`. Setting `Child` adds that element to both `LogicalChildren` and `VisualChildren`. Replacing the child, or setting `Child` to `null`, removes the old element from both child collections before attaching the new element.

The old element is removed from `LogicalChildren` first and from `VisualChildren` second. So an old child with `UIElement.Presence` set is still attached through the decorator when the visual removal starts its exit animation: on an attached decorator it stays an exiting visual child of the decorator (listed by `PresenceCoordinator.GetExitingVisualChildren`) until the exit ends, as `PresenceOptions` describes. For example, after `border.Child = null`, `root.Motion.Presence.GetState(oldChild)` is `PresenceState.Exiting` while the exit runs, and `PresenceState.Detached` after it ends. Conditional child content of a `Border` in markup is set through `Child`, so it exits the same way when its condition turns false.

A move is not an exit. When another owner takes the child out of the decorator, the decorator lets go of it at once: the element leaves both child collections without a presence exit, `Child` becomes `null`, and the new owner can add the element right away. This happens when a `ContentPresenter` takes its element content back out of the `Border` that its template created, for example when `ContentTemplate` changes from `context => new Border { Child = (UIElement?)context.Data }` to another template of the same shape: the element is then the child of the new `Border`, its state is `PresenceState.Present`, and `GetExitingVisualChildren` of the old `Border` is empty. A `ContentControl` or `TabItem` that takes its content or header element back out of its previous template does the same.

The assigned child must be unparented and cannot be the decorator itself, one of the decorator's ancestors, or an element already attached under a different root. Invalid assignments throw `InvalidOperationException`. If an assignment fails while replacing an existing child, the previous child is restored. The rejected element leaves both child collections at once, without a presence exit, because it never became the child.

A replacement raises `Unloaded` on the old child and `Loaded` on the new child while it runs, when the decorator is loaded. A `Child` change that one of these handlers makes wins, and the decorator still owns exactly one element, the one `Child` names. While the old child leaves, `Child` is `null`; while the new child is added, `Child` is the new child. For example, `border.Child` is `oldChild`, `oldChild.Unloaded += (_, _) => border.Child = other;`, and the application then sets `border.Child = value`. Afterward `border.Child` is `other`, `LogicalChildren` and `VisualChildren` both contain only `other`, and `value` has no parent. The same holds when `value.Loaded` sets `border.Child = other`.

Layout uses the inherited `Padding` and the inward part of `BorderThickness` from `Control` as a single inset: `Padding` plus `-t` on each side whose `BorderThickness` is negative (`-t`); a positive or zero side adds nothing. `MeasureCore` measures the child with the available size deflated by those insets, then returns the child's desired size inflated by the same insets. `ArrangeCore` arranges the child inside the final rectangle deflated by those insets.

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
| `BorderThickness` | `Control` | Its negative sides contribute to the inset used when measuring and arranging `Child`. |
| `Background` | `Control` | Inherited render property for the control background. |
| `BorderBrush` | `Control` | Inherited nullable brush property for the control border. |
| `ComponentTemplate` | `Control` | Inherited component template property. |

## Layout Behavior
| Operation | Behavior |
| --- | --- |
| Measure | Measures `Child` with `AvailableSize` reduced by `Padding` plus the negative sides of `BorderThickness`; returns the measured child size expanded by the same inset. |
| Arrange | Arranges `Child` in the final rectangle reduced by `Padding` plus the negative sides of `BorderThickness`; returns the original final rectangle. |
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
- `Cerneala.UI.Motion.Presence.PresenceOptions`
