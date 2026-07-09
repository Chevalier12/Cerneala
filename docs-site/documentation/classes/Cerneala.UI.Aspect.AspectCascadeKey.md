# AspectCascadeKey Record Struct

## Definition
Namespace: `Cerneala.UI.Aspect`

Assembly/Project: `Cerneala`

Source: `UI/Aspect/AspectRuleSet.cs`

Represents the internal cascade comparison key used to choose the winning aspect declaration for a UI property.

```csharp
internal readonly record struct AspectCascadeKey(
    int LayerOrder,
    AspectSpecificity Specificity,
    int DeclarationOrder) : IComparable<AspectCascadeKey>
```

Inheritance:
`object` -> `ValueType` -> `AspectCascadeKey`

Implements:
`IComparable<AspectCascadeKey>`

## Examples

Create and compare cascade keys inside aspect resolution:

```csharp
AspectCascadeKey current = new(
    rule.Layer.Order,
    rule.Target.Specificity,
    rule.DeclarationOrder);

if (current.CompareTo(previous) > 0)
{
    winners[declaration.Property] = (current, declaration);
}
```

## Remarks

`AspectCascadeKey` is an internal implementation detail shared by `AspectRuleSet.ResolveDeclarations` and `AspectEngine.Resolve`. It captures the three values that decide which matching aspect declaration wins when multiple rules set the same `UiProperty`.

Comparison is lexicographic. `LayerOrder` is compared first, so a rule from a higher `AspectLayer.Order` wins before target specificity or declaration order are considered. If both keys have the same layer order, `Specificity` is compared next. If specificity is also equal, `DeclarationOrder` is the final tie-breaker.

Higher comparison results represent stronger cascade precedence. The engine stores the winning key with each resolved value internally so later matching declarations can be accepted or rejected with the same ordering logic.

## Constructors

| Name | Description |
| --- | --- |
| `AspectCascadeKey(int LayerOrder, AspectSpecificity Specificity, int DeclarationOrder)` | Initializes a cascade key from layer order, target specificity, and declaration order. |

## Properties

| Name | Type | Description |
| --- | --- | --- |
| `LayerOrder` | `int` | Gets the aspect layer order compared before specificity and declaration order. |
| `Specificity` | `AspectSpecificity` | Gets the target specificity compared when layer order is equal. |
| `DeclarationOrder` | `int` | Gets the declaration order compared when layer order and specificity are equal. |

## Methods

| Name | Return Type | Description |
| --- | --- | --- |
| `CompareTo(AspectCascadeKey other)` | `int` | Compares this key with another key by layer order, specificity, and declaration order. |

## Applies to

Cerneala UI aspect cascade resolution internals.

## See also

- `Cerneala.UI.Aspect.AspectRuleSet`
- `Cerneala.UI.Aspect.AspectEngine`
- `Cerneala.UI.Aspect.AspectLayer`
- `Cerneala.UI.Aspect.AspectSpecificity`
- `Cerneala.UI.Aspect.ResolvedAspectValue`
