# Unset.UnsetValue Class

## Definition
Namespace: `Cerneala.UI.Core`

Assembly/Project: `Cerneala`

Source: `UI/Core/Unset.cs`

Represents the private sentinel object type used by `Unset.Value`.

```csharp
private sealed class UnsetValue
```

Inheritance:
`Object` -> `Unset.UnsetValue`

Containing type:
`Unset`

Accessibility:
`private`

## Examples

```csharp
object value = Unset.Value;

bool isUnset = ReferenceEquals(value, Unset.Value);
string diagnosticText = value.ToString(); // "<unset>"
```

## Remarks

`Unset.UnsetValue` is a private nested implementation detail. Code outside `Unset` does not create this type directly; it receives the singleton instance exposed by `Unset.Value`.

The type overrides `ToString()` to return `"<unset>"`, which gives the sentinel a stable diagnostic string when it is logged or formatted. It does not declare additional state or public API surface.

## Methods

| Name | Description |
| --- | --- |
| `ToString()` | Returns the string `"<unset>"`. |

## Applies to

`Cerneala` project, `Cerneala.UI.Core` namespace.

## See also

- `Unset.Value`
