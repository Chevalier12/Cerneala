# PrismResourceId Struct

## Definition
Namespace: `Cerneala.UI.Prism.Definitions`

Assembly/Project: `Cerneala`

Source: `UI/Prism/Definitions/PrismResourceId.cs`

Represents a backend-neutral identifier for a Prism auxiliary resource.

```csharp
public readonly record struct PrismResourceId
```

## Examples

```csharp
PrismResourceId markupMask = new("MaskImage");
PrismResourceId numericResource = new(42);
```

## Remarks

The identifier does not own a texture, render target, or other GPU object.
Resource providers and drawing backends resolve it through their own typed
resource contracts.

The string constructor preserves the resource key and derives a stable positive
numeric value from it. This form is used for named markup resources so the
retained Prism draw scope can resolve the matching image snapshot. The integer
constructor remains available for explicitly assigned identifiers and leaves
`Key` as `null`.

## Constructors

| Name | Description |
| --- | --- |
| `PrismResourceId(int value)` | Initializes an identifier from a positive integer. |
| `PrismResourceId(string key)` | Initializes an identifier from a non-empty resource key and derives its stable positive numeric value. |

## Properties

| Name | Type | Description |
| --- | --- | --- |
| `Key` | `string?` | Gets the named resource key, or `null` for an identifier created from an integer. |
| `Value` | `int` | Gets the numeric identifier. |

## Methods

| Name | Return Type | Description |
| --- | --- | --- |
| `ToString()` | `string` | Returns the invariant-culture decimal representation. |

## Exceptions

| Member | Exception | Condition |
| --- | --- | --- |
| `PrismResourceId(int)` | `ArgumentOutOfRangeException` | `value` is zero or negative. |
| `PrismResourceId(string)` | `ArgumentException` | `key` is `null`, empty, or contains only whitespace. |

## Applies to

Prism masks and catalog parameters that reference immutable auxiliary resources.
