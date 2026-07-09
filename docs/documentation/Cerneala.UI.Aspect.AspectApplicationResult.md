# AspectApplicationResult Class

## Definition

Namespace: `Cerneala.UI.Aspect`

Assembly/Project: `Cerneala`

Source: `UI/Aspect/AspectEngine.cs`

Represents the result returned after an `AspectEngine` applies resolved aspect values to a `UIElement`.

```csharp
public sealed record AspectApplicationResult(bool Applied, ResolvedAspect ResolvedAspect);
```

Inheritance:
`object` -> `AspectApplicationResult`

## Examples

Apply aspects to a control and inspect whether the element changed:

```csharp
using Cerneala.UI.Aspect;
using Cerneala.UI.Controls;

AspectCatalog catalog = new AspectRegistry()
    .Register(DefaultAspectPackage.Create())
    .BuildCatalog();

AspectEnvironment environment = DefaultAspectPackage.CreateEnvironment();
AspectEngine engine = new();
Button button = new();

AspectApplicationResult result = engine.Apply(button, catalog, environment);

if (result.Applied)
{
    ResolvedAspect resolved = result.ResolvedAspect;
    Console.WriteLine(resolved.Values.Count);
}
```

Use the result returned through `AspectInvalidation.Track`:

```csharp
using Cerneala.UI.Aspect;
using Cerneala.UI.Controls;

AspectEngine engine = new();
AspectCatalog catalog = new AspectRegistry()
    .Register(DefaultAspectPackage.Create())
    .BuildCatalog();

AspectInvalidation invalidation = new(engine, catalog, DefaultAspectPackage.CreateEnvironment());
AspectApplicationResult result = invalidation.Track(new Button());

bool changed = result.Applied;
```

## Remarks

`AspectApplicationResult` is produced by `AspectEngine.Apply` and by `AspectInvalidation.Track`, which delegates to `AspectEngine.Apply`.

`Applied` is `true` when applying the resolved aspect changed the target element's aspect-base values. A change is reported when a previously applied aspect value is cleared, when a winning resolved value differs from the existing aspect-base value, or when the property's current value source is not `UiPropertyValueSource.AspectBase` and the engine writes the resolved value.

`Applied` is `false` when resolving and applying the catalog leaves the element's aspect-base values unchanged. Tests cover this stable case when the catalog, environment, and states do not change, and also when unrelated data context changes do not affect matched declarations.

`ResolvedAspect` contains the full resolution result used for the application pass, including winning values, matched rules, rejected declarations, and dependency data. It is returned even when `Applied` is `false`, so callers can still inspect the current resolution.

Because `AspectApplicationResult` is a record, it has value-based equality, deconstruction support, and compiler-generated members for record printing and copying. The source defines no custom validation on the primary constructor parameters.

## Constructors

| Name | Description |
| --- | --- |
| `AspectApplicationResult(bool Applied, ResolvedAspect ResolvedAspect)` | Initializes the application result with a change flag and the resolved aspect used by the application pass. |

## Properties

| Name | Type | Description |
| --- | --- | --- |
| `Applied` | `bool` | Gets whether applying the resolved aspect changed the element's aspect-base values. |
| `ResolvedAspect` | `ResolvedAspect` | Gets the resolved aspect result produced for the target element. |

## Methods

| Name | Return Type | Description |
| --- | --- | --- |
| `Deconstruct(out bool Applied, out ResolvedAspect ResolvedAspect)` | `void` | Deconstructs the record into its application flag and resolved aspect. |

## Applies to

Cerneala UI aspect application results returned by `AspectEngine` and `AspectInvalidation`.

## See also

- `Cerneala.UI.Aspect.AspectEngine`
- `Cerneala.UI.Aspect.AspectInvalidation`
- `Cerneala.UI.Aspect.ResolvedAspect`
- `Cerneala.UI.Core.UiPropertyValueSource`
