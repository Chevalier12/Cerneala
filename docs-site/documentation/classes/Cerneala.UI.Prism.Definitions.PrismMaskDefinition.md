# PrismMaskDefinition Class

## Definition
Namespace: `Cerneala.UI.Prism.Definitions`

Assembly/Project: `Cerneala`

Source: `UI/Prism/Definitions/PrismMaskDefinition.cs`

Defines an immutable image mask applied to the complete prepared contribution of a Prism scope.

```csharp
public sealed class PrismMaskDefinition : IEquatable<PrismMaskDefinition>
```

## Remarks

Definitions are immutable and may be shared by multiple Prism instances. A mask
is applied after filters and styles prepare the scope contribution and before the
scope opacity and blend operation. Mutable per-instance values live in
`PrismMaskState`.

## Constructors

| Name | Description |
| --- | --- |
| `PrismMaskDefinition(PrismResourceId image, PrismMaskChannel channel = Alpha, float feather = 0, float density = 1, bool invert = false)` | Initializes a mask using catalog-backed defaults. |

## Properties

| Name | Type | Description |
| --- | --- | --- |
| `Image` | `PrismResourceId` | Gets the backend-neutral mask-image identifier. |
| `Channel` | `PrismMaskChannel` | Gets the channel used to derive coverage. |
| `Feather` | `float` | Gets the nonnegative feather radius in device-independent pixels. |
| `Density` | `float` | Gets mask density from zero through one. |
| `Invert` | `bool` | Gets whether mask coverage is inverted. |

## Methods

| Name | Return Type | Description |
| --- | --- | --- |
| `Equals(PrismMaskDefinition? other)` | `bool` | Tests structural equality. |
| `GetHashCode()` | `int` | Returns the structural hash code. |

## Exceptions

| Member | Exception | Condition |
| --- | --- | --- |
| `PrismMaskDefinition(...)` | `ArgumentOutOfRangeException` | `feather` is negative or non-finite, or `density` is non-finite or outside zero through one. |

## Applies to

At most one mask on a Prism layer, group, or backdrop.
