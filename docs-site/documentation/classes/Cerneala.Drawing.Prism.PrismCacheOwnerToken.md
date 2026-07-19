# PrismCacheOwnerToken Struct

## Definition
Namespace: `Cerneala.Drawing.Prism`

Assembly/Project: `Cerneala`

Source: `Drawing/Prism/PrismDrawScope.cs`

Identifies the retained cache owner for a Prism scope without retaining a UI element.

```csharp
public readonly record struct PrismCacheOwnerToken
```

## Examples

```csharp
using Cerneala.Drawing.Prism;

PrismCacheOwnerToken token = new(42);
long numericValue = token.Value;
```

## Remarks

The token is an opaque, positive numeric identity. Rendering backends may use it as part of retained cache keys and invalidation records. It contains no reference back to the element that owns the Prism instance.

## Constructors

| Name | Description |
| --- | --- |
| `PrismCacheOwnerToken(long value)` | Creates a token from a positive numeric value. |

## Properties

| Name | Type | Description |
| --- | --- | --- |
| `Value` | `long` | Gets the positive numeric identity. |

## Exceptions

| Member | Exception | Condition |
| --- | --- | --- |
| `PrismCacheOwnerToken(long)` | `ArgumentOutOfRangeException` | `value` is zero or negative. |

## Applies to

Cerneala retained Prism analysis, composition graphs, and pixel caches.

## See also

- `Cerneala.Drawing.Prism.PrismDrawScope`
- `Cerneala.Drawing.DrawCommand`
