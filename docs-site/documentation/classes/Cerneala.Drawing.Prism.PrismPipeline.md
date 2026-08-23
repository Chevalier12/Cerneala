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

A pipeline can be shared by multiple `PrismImage` instances. Collection changes and operation-property changes are observed lazily on the next draw.

The Prism runtime represents filters and styles as separate ordered collections on a layer. Consequently, all filters execute in their insertion order before all styles execute in their insertion order, even if filters and styles are interleaved in this collection.

An empty pipeline cannot be applied to an image. Clearing an already applied pipeline causes the next draw to throw `InvalidOperationException` until an operation is added again.

## See Also

- `PrismOperation`
- `PrismFilter`
- `PrismStyle`
- `PrismImage`
