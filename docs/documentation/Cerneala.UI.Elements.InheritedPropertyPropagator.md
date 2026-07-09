# InheritedPropertyPropagator Class

## Definition
Namespace: `Cerneala.UI.Elements`

Assembly/Project: `Cerneala`

Source: `UI/Elements/InheritedPropertyPropagator.cs`

Propagates inherited UI property values through a visual subtree.

```csharp
public sealed class InheritedPropertyPropagator
```

Inheritance:
`object` -> `InheritedPropertyPropagator`

## Examples

The following example propagates an inherited `Foreground` value from a parent control to a descendant.

```csharp
using Cerneala.Drawing;
using Cerneala.UI.Controls;
using Cerneala.UI.Core;
using Cerneala.UI.Elements;

UIRoot root = new();
Control parent = new() { Foreground = DrawColor.White };
TextBlock child = new() { Text = "child" };

parent.VisualChildren.Add(child);
root.VisualChildren.Add(parent);

int changed = root.InheritedPropertyPropagator.PropagateFrom(root);

DrawColor foreground = child.Foreground;
UiPropertyValueSource source = child.GetValueSource(Control.ForegroundProperty);
```

## Remarks

`InheritedPropertyPropagator` walks the visual children of the supplied root and applies registered UI properties whose metadata includes `UiPropertyOptions.Inherits`. The supplied root itself is not updated; it is used as the inherited-value source for its direct visual children.

For each parent-child pair, the propagator reads the parent's effective value. If the parent's source is `UiPropertyValueSource.Default`, it clears the child's inherited source for that property. Otherwise, it stores the parent's effective value on the child with the `UiPropertyValueSource.Inherited` source.

The class does not make inherited values stronger than local or other higher-priority value sources. Effective value precedence is handled by the UI property store, so a local child value continues to win over an inherited value.

`UIRoot` owns an `InheritedPropertyPropagator` instance and invokes it during the inherited-property frame phase. `ItemsPresenter` also uses it when processing realized subtrees, then clears inherited and aspect dirty state for the processed elements.

## Constructors

| Name | Description |
| --- | --- |
| `InheritedPropertyPropagator()` | Initializes a new propagator instance. |

## Methods

| Name | Description |
| --- | --- |
| `PropagateFrom(UIElement root)` | Propagates inherited property values from `root` to all descendants in the visual subtree and returns the number of effective child property values that changed. Throws `ArgumentNullException` when `root` is `null`. |

## Applies to

Cerneala retained UI element trees that use `UIElement.VisualChildren` and the `UiProperty` inheritance option.

## See also

- `Cerneala.UI.Elements.UIRoot`
- `Cerneala.UI.Elements.UIElement`
- `Cerneala.UI.Core.UiPropertyOptions`
- `Cerneala.UI.Core.UiPropertyValueSource`
