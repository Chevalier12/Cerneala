# PrismResourceId Struct

## Definition
Namespace: `Cerneala.UI.Prism.Definitions`

Assembly/Project: `Cerneala`

Source: `UI/Prism/Definitions/PrismResourceId.cs`

Represents a positive backend-neutral identifier for a Prism auxiliary resource.

```csharp
public readonly record struct PrismResourceId
```

## Remarks

The identifier does not own a texture, render target, or other GPU object. Resource providers and drawing backends resolve it through their own typed resource contracts.

## Constructors

| Name | Description |
| --- | --- |
| `PrismResourceId(int value)` | Initializes an identifier from a positive integer. |

## Properties

| Name | Type | Description |
| --- | --- | --- |
| `Value` | `int` | Gets the numeric identifier. |

## Methods

| Name | Return Type | Description |
| --- | --- | --- |
| `ToString()` | `string` | Returns the invariant-culture decimal representation. |

## Exceptions

| Member | Exception | Condition |
| --- | --- | --- |
| `PrismResourceId(int)` | `ArgumentOutOfRangeException` | `value` is zero or negative. |

## Applies to

Prism masks and catalog parameters that reference immutable auxiliary resources.
