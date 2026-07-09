# FocusScope Class

## Definition
Namespace: `Cerneala.UI.Input`

Assembly/Project: `Cerneala`

Source: `UI/Input/FocusScope.cs`

Represents a focus scope associated with a non-null `UIElement` owner.

```csharp
public sealed class FocusScope
```

Inheritance:
`Object` -> `FocusScope`

## Examples

```csharp
using Cerneala.UI.Elements;
using Cerneala.UI.Input;

UIElement panel = new();
FocusScope scope = new(panel);

UIElement owner = scope.Owner;
```

## Remarks

`FocusScope` is a small immutable object that stores the `UIElement` that owns the scope. The owner is supplied at construction time and exposed through the `Owner` property.

The constructor rejects `null` owners with `ArgumentNullException`. This type does not expose APIs for moving keyboard focus, dispatching keyboard input, or changing focus state; those behaviors live in other input services such as `FocusManager` and `FocusPolicy`.

## Constructors

| Name | Description |
| --- | --- |
| `FocusScope(UIElement owner)` | Initializes a new focus scope for `owner`. Throws `ArgumentNullException` when `owner` is `null`. |

## Properties

| Name | Type | Description |
| --- | --- | --- |
| `Owner` | `UIElement` | Gets the non-null element that owns this focus scope. |

## Applies to

Cerneala retained UI input infrastructure.

## See also

- `Cerneala.UI.Input.FocusManager`
- `Cerneala.UI.Input.FocusPolicy`
- `Cerneala.UI.Elements.UIElement`
