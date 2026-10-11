# PrismPipeline Class

## Definition

Namespace: `Cerneala.Drawing.Prism`

Assembly/Project: `Cerneala`

Source: `Drawing/Prism/PrismPipeline.cs`

Stores an ordered, mutable collection of code-defined Prism operations.

```csharp
public sealed class PrismPipeline : Collection<PrismOperation>
```

## Constructors

| Signature | Description |
| --- | --- |
| `PrismPipeline()` | Creates an empty pipeline for collection-initializer syntax. |
| `PrismPipeline(IEnumerable<PrismOperation>)` | Creates a pipeline containing the supplied operations. |

## Remarks

A pipeline can be shared by multiple `PrismImage` instances. Collection changes and operation-property changes notify the images that use the pipeline immediately; each image rebuilds its native Prism definition on its next draw.

The Prism runtime represents filters and styles as separate ordered collections on a layer. Consequently, all filters execute in their insertion order before any style runs, even if filters and styles are interleaved in this collection. Styles are chained from the last inserted style to the first: for `[new DropShadowStyle(), new BlurFilter(), new OuterGlowStyle()]`, the blur runs first, then `OuterGlowStyle`, then `DropShadowStyle`.

An empty pipeline cannot be applied to an image. Clearing an already applied pipeline causes the next draw to throw `InvalidOperationException` until an operation is added again.

## Exceptions

| Member | Exception | Condition |
| --- | --- | --- |
| `PrismPipeline(IEnumerable<PrismOperation>)` | `ArgumentNullException` | `operations` is `null` or contains a `null` element. |
| `Add`, `Insert`, indexer setter | `ArgumentNullException` | The operation is `null`. |

## See Also

- `PrismOperation`
- `PrismFilter`
- `PrismStyle`
- `PrismImage`
