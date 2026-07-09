# Cerneala.UI.Input.UiElementId Class

## Definition
Namespace: `Cerneala.UI.Input`

Assembly/Project: `Cerneala`

Source: `UI/Input/UiElementId.cs`

Represents a strongly typed identifier for a UI element in Cerneala input and routed-event infrastructure.

```csharp
public readonly record struct UiElementId(string Value)
```

Inheritance:
`Object` -> `ValueType` -> `UiElementId`

Implements:
`IEquatable<UiElementId>`

## Examples

The following example creates identifiers and uses them to register a parent-child route in a `UiInputTree`.

```csharp
using Cerneala.UI.Input;

UiInputTree tree = new();

UiElementId rootId = new("root");
UiElementId childId = new("root.child");

tree.Add(rootId, parentId: null);
tree.Add(childId, parentId: rootId);

IReadOnlyList<UiElementId> route = tree.GetRouteToRoot(childId);

string childName = route[0].ToString(); // "root.child"
```

## Remarks

`UiElementId` wraps the string stored in `Value` so input routing code can pass element identifiers as a distinct type instead of raw strings.

The type is a `readonly record struct`, so its generated equality and hash code behavior are value-based. Two `UiElementId` values with the same `Value` compare equal and can be used as dictionary keys by input infrastructure such as `UiInputTree` and `ElementInputRouteMap`.

`UiElementId` does not validate, normalize, or generate identifier strings. Callers provide the `Value` used by the input tree, route maps, routed-event targets, and routed-event sources.

## Constructors

| Name | Description |
| --- | --- |
| `UiElementId(String)` | Initializes a new `UiElementId` with the supplied string `Value`. |

## Properties

| Name | Type | Description |
| --- | --- | --- |
| `Value` | `String` | Gets the identifier text wrapped by this value. |

## Methods

| Name | Return Type | Description |
| --- | --- | --- |
| `ToString()` | `String` | Returns `Value`. |

## Applies to

Cerneala UI input routing.

## See also

- `UiInputTree`
- `UiInputElement`
- `ElementInputRouteMap`
- `RoutedEventRouter`
