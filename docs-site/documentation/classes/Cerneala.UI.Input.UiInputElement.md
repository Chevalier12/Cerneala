# UiInputElement Class

## Definition
Namespace: `Cerneala.UI.Input`

Assembly/Project: `Cerneala`

Source: `UI/Input/UiInputElement.cs`

Represents one immutable node in a UI input tree.

```csharp
public sealed record UiInputElement(UiElementId Id, UiElementId? ParentId, bool IsEnabled);
```

Inheritance:
`object` -> `UiInputElement`

## Examples

```csharp
using System;
using Cerneala.UI.Input;

UiElementId rootId = new("root");
UiElementId childId = new("button");

UiInputElement root = new(rootId, ParentId: null, IsEnabled: true);
UiInputElement child = new(childId, ParentId: root.Id, IsEnabled: false);

Console.WriteLine(child.ParentId); // root
Console.WriteLine(child.IsEnabled); // False
```

## Remarks

`UiInputElement` is a sealed positional record used by `UiInputTree` to store the input identity, parent relationship, and enabled state for an element.

The type does not validate parent ordering or tree membership by itself. `UiInputTree.Add` validates that a parent id, when provided, has already been registered before it creates the `UiInputElement`.

`ParentId` is used by `UiInputTree.GetRouteToRoot` to walk from a target element toward the root. `IsEnabled` is consulted by `UiInputTree.GetHandlers`; disabled elements return no routed event handlers.

Because this is a record, instances have record value equality and can be deconstructed into their positional values.

## Constructors

| Name | Description |
| --- | --- |
| `UiInputElement(UiElementId Id, UiElementId? ParentId, bool IsEnabled)` | Initializes an input element with an id, optional parent id, and enabled state. |

## Properties

| Name | Type | Description |
| --- | --- | --- |
| `Id` | `UiElementId` | The unique input-tree id for this element. |
| `ParentId` | `UiElementId?` | The parent element id, or `null` when the element is a root. |
| `IsEnabled` | `bool` | Indicates whether routed event handlers for this element should be returned by the input tree. |

## Methods

| Name | Description |
| --- | --- |
| `Deconstruct(out UiElementId Id, out UiElementId? ParentId, out bool IsEnabled)` | Deconstructs the record into its positional values. |

## Applies to

Cerneala retained UI input routing infrastructure.

## See also

- `UiInputTree`
- `UiElementId`
- `RoutedEventRouter`
