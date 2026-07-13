# AspectInvalidation Class

## Definition
Namespace: `Cerneala.UI.Aspect`

Assembly/Project: `Cerneala`

Source: `UI/Aspect/AspectInvalidation.cs`

Tracks `UIElement` instances and reapplies a fixed aspect catalog when their aspect dependencies or token values change.

```csharp
public sealed class AspectInvalidation : IDisposable
```

Implements: `IDisposable`

## Examples

Track a button against a catalog and environment:

```csharp
using Cerneala.UI.Aspect;
using Cerneala.UI.Controls;

AspectCatalog catalog = new AspectRegistry()
    .Register(DefaultAspectPackage.Create())
    .BuildCatalog();

AspectEnvironment environment = DefaultAspectPackage.CreateEnvironment();
AspectEngine engine = new();
using AspectInvalidation invalidation = new(engine, catalog, environment);

Button button = new();
AspectApplicationResult result = invalidation.Track(button);

if (result.Applied)
{
    ResolvedAspect resolved = result.ResolvedAspect;
}
```

## Remarks

`AspectInvalidation` stores the `AspectEngine`, `AspectCatalog`, and `AspectEnvironment` supplied to its constructor. `Track` subscribes to the element's property changes, performs the initial application, and retains the element until `Untrack` or `Dispose` is called. Property changes marked with `AffectsAspect`, and properties recorded by the resolved aspect dependency set, cause the element to be recomputed.

Changes to environment tokens recompute only tracked elements whose resolved declarations depend on the changed token. Aspect data conditions receive the tracked element's current `DataContext`. `Recompute` forces an application without changing tracking state. `Untrack` removes subscriptions and clears the element's applied aspect state; `Dispose` removes all subscriptions and rejects later tracking or recompute calls.

This class does not build catalogs, synchronize token defaults, or pass aspect variants. Root-level processing that needs the root registry, theme bridge, and control variants is handled by `AspectProcessor`.

The constructor throws `ArgumentNullException` when `engine`, `catalog`, or `environment` is `null`. `Track` relies on `AspectEngine.Apply` for element validation, so a `null` element also results in `ArgumentNullException`.

## Constructors

| Name | Description |
| --- | --- |
| `AspectInvalidation(AspectEngine engine, AspectCatalog catalog, AspectEnvironment environment)` | Initializes a tracker that reuses the supplied engine, catalog, and environment for each `Track` call. |

## Methods

| Name | Return Type | Description |
| --- | --- | --- |
| `Track(UIElement element)` | `AspectApplicationResult` | Starts tracking `element`, applies the stored catalog and environment, and returns the engine result. Repeated calls recompute without duplicate subscriptions. |
| `Recompute(UIElement element)` | `AspectApplicationResult` | Reapplies the stored catalog and environment without changing tracking membership. |
| `Untrack(UIElement element)` | `bool` | Stops tracking the element, clears its engine-applied aspect state, and returns whether it was tracked. |
| `Dispose()` | `void` | Detaches environment and element subscriptions. Repeated calls are safe. |

## Applies to

Cerneala UI aspect application where a caller wants to reuse a specific `AspectEngine`, `AspectCatalog`, and `AspectEnvironment`.

## See also

- `AspectEngine`
- `AspectProcessor`
- `AspectCatalog`
- `AspectEnvironment`
- `AspectApplicationResult`
