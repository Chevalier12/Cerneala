# UiInputElement Class

## Definition
Namespace: `Cerneala.UI.Input`

Assembly/Project: `Cerneala`

Source: `UI/Input/UiInputElement.cs`

Represents one immutable node in a UI input tree.

```csharp
public sealed record UiInputElement(UIElement Element, UIElement? Parent, bool IsEnabled);
```

Inheritance:
`object` -> `UiInputElement`

## Examples

```csharp
using System;
using Cerneala.UI.Controls;
using Cerneala.UI.Input;

Border rootElement = new();
Border childElement = new();

UiInputElement root = new(rootElement, Parent: null, IsEnabled: true);
UiInputElement child = new(childElement, Parent: root.Element, IsEnabled: false);

Console.WriteLine(ReferenceEquals(child.Parent, rootElement)); // True
Console.WriteLine(child.IsEnabled); // False
```

## Remarks

`UiInputElement` is a sealed positional record used by `UiInputTree` to store the element, its parent element, and its enabled state.

The type does not validate parent ordering or tree membership by itself. `UiInputTree.Add` validates that a parent, when provided, has already been registered before it creates the `UiInputElement`.

`Parent` is used by `UiInputTree.GetRouteToRoot` to walk from a target element toward the root. `IsEnabled` is consulted by `UiInputTree.GetHandlers`; disabled elements return no routed event handlers.

Because this is a record, instances have record value equality and can be deconstructed into their positional values.

## Constructors

| Name | Description |
| --- | --- |
| `UiInputElement(UIElement Element, UIElement? Parent, bool IsEnabled)` | Initializes an input element with the element, its optional parent element, and enabled state. |

## Properties

| Name | Type | Description |
| --- | --- | --- |
| `Element` | `UIElement` | The element this node stands for. |
| `Parent` | `UIElement?` | The parent element, or `null` when the element is a root. |
| `IsEnabled` | `bool` | Indicates whether routed event handlers for this element should be returned by the input tree. |

## Methods

| Name | Description |
| --- | --- |
| `Deconstruct(out UIElement Element, out UIElement? Parent, out bool IsEnabled)` | Deconstructs the record into its positional values. |
| `Equals(object?)` | Returns whether the object is a `UiInputElement` with equal `Element`, `Parent`, and `IsEnabled`. |
| `Equals(UiInputElement?)` | Returns whether the other record has equal `Element`, `Parent`, and `IsEnabled`. |
| `GetHashCode()` | Returns a hash code combined from the three positional values. |
| `ToString()` | Returns the record text: the type name followed by `Element`, `Parent`, and `IsEnabled` and their values. |

## Operators

| Name | Description |
| --- | --- |
| `operator ==(UiInputElement?, UiInputElement?)` | Returns whether both records are equal by value, or both are `null`. |
| `operator !=(UiInputElement?, UiInputElement?)` | Returns the negation of `==`. |

## Applies to

Cerneala retained UI input routing infrastructure.

## See also

- `UiInputTree`
- `Cerneala.UI.Elements.UIElement`
- `RoutedEventRouter`
