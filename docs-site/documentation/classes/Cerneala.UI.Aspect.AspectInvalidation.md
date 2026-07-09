# AspectInvalidation Class

## Definition
Namespace: `Cerneala.UI.Aspect`

Assembly/Project: `Cerneala`

Source: `UI/Aspect/AspectInvalidation.cs`

Provides a small tracking facade that applies a fixed `AspectCatalog` and `AspectEnvironment` to `UIElement` instances through an `AspectEngine`.

```csharp
public sealed class AspectInvalidation
```

Inheritance:
`object` -> `AspectInvalidation`

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
AspectInvalidation invalidation = new(engine, catalog, environment);

Button button = new();
AspectApplicationResult result = invalidation.Track(button);

if (result.Applied)
{
    ResolvedAspect resolved = result.ResolvedAspect;
}
```

## Remarks

`AspectInvalidation` stores the `AspectEngine`, `AspectCatalog`, and `AspectEnvironment` supplied to its constructor. Calling `Track` delegates directly to `AspectEngine.Apply(element, catalog, environment)`, so the element is resolved and updated with aspect-base values using those fixed dependencies.

This class does not build catalogs, synchronize token defaults, pass a theme provider, pass aspect variants, or manage a data context. Root-level processing that needs those inputs is handled by `AspectProcessor`. Use `AspectInvalidation` when code already owns the engine, catalog, and environment that should be reused for repeated tracking calls.

The constructor throws `ArgumentNullException` when `engine`, `catalog`, or `environment` is `null`. `Track` relies on `AspectEngine.Apply` for element validation, so a `null` element also results in `ArgumentNullException`.

## Constructors

| Name | Description |
| --- | --- |
| `AspectInvalidation(AspectEngine engine, AspectCatalog catalog, AspectEnvironment environment)` | Initializes a tracker that reuses the supplied engine, catalog, and environment for each `Track` call. |

## Methods

| Name | Return Type | Description |
| --- | --- | --- |
| `Track(UIElement element)` | `AspectApplicationResult` | Applies the stored catalog and environment to `element` through the stored `AspectEngine` and returns the engine result. |

## Applies to

Cerneala UI aspect application where a caller wants to reuse a specific `AspectEngine`, `AspectCatalog`, and `AspectEnvironment`.

## See also

- `AspectEngine`
- `AspectProcessor`
- `AspectCatalog`
- `AspectEnvironment`
- `AspectApplicationResult`
