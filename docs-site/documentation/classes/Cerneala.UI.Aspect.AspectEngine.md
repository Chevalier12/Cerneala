# AspectEngine Class

## Definition
Namespace: `Cerneala.UI.Aspect`

Assembly/Project: `Cerneala`

Source: `UI/Aspect/AspectEngine.cs`

Resolves aspect rules for a `UIElement`, applies the winning aspect values, and keeps per-element diagnostics and dependency tracking.

```csharp
public sealed class AspectEngine
```

Inheritance:
`object` -> `AspectEngine`

## Examples

Apply a catalog to a control:

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
}
```

Resolve without mutating the element:

```csharp
using Cerneala.UI.Aspect;
using Cerneala.UI.Controls;

Button button = new();
AspectCatalog catalog = new AspectRegistry()
    .Register(DefaultAspectPackage.Create())
    .BuildCatalog();

ResolvedAspect resolved = new AspectEngine()
    .Resolve(button, catalog, DefaultAspectPackage.CreateEnvironment());
```

## Remarks

Standalone engines capture their constructing thread. Root-owned engines use `UIRoot.Relay`; resolution, application, diagnostics state, dependency tracking, and clearing must occur on that owner thread and reject cross-thread calls before changing engine or element state.

`AspectEngine` is the low-level resolver and applier behind `AspectProcessor`. Most root-level UI processing goes through `AspectProcessor`, which owns an `AspectEngine`, builds catalogs from the root registry, synchronizes token defaults, and passes control variants and the root theme provider.

`Resolve` evaluates each `AspectRuleSet` in the catalog against an `AspectMatchContext` built from the target element, element state, optional variants, optional data context, optional slot path, and the current environment version. Matching declarations are resolved through an `AspectResolutionContext`. When multiple declarations target the same `UiProperty`, the engine keeps the declaration with the strongest cascade key, based on layer order, specificity, and declaration order. Rejected declarations are stored in the returned `ResolvedAspect`.

`Apply` calls `Resolve`, writes winning values to the element with the `UiPropertyValueSource.AspectBase` source, clears previously applied aspect values that are no longer present, updates diagnostics, and tracks the resolved dependency set. It returns `Applied` as `true` when the aspect value source or value changed on the element.

The engine stores state per `UIElement` in a `ConditionalWeakTable`. `Clear` removes the element's aspect-base values from the last resolved result, resets its diagnostics, and removes it from dependency tracking. `GetDiagnostics` and `GetDependencies` return empty snapshots or dependency sets for elements that have not been applied.

The `Counters` instance is cumulative for the engine instance. It records rule consideration, matches, declaration resolution, token lookups, and cache counters used by the aspect diagnostics and stress tests.

## Properties

| Name | Type | Description |
| --- | --- | --- |
| `Counters` | `AspectEngineCounters` | Gets cumulative resolution counters for this engine instance. |

## Methods

| Name | Return Type | Description |
| --- | --- | --- |
| `Apply(UIElement element, AspectCatalog catalog, AspectEnvironment environment, ThemeProvider? themeProvider = null, AspectVariantSet? variants = null, AspectDataContext? dataContext = null, AspectSlotPath? slotPath = null)` | `AspectApplicationResult` | Resolves the element against the catalog and applies the resulting aspect-base values to the element. Throws `ArgumentNullException` when `element`, `catalog`, or `environment` is `null`. |
| `Resolve(UIElement element, AspectCatalog catalog, AspectEnvironment environment, ThemeProvider? themeProvider = null, AspectVariantSet? variants = null, AspectDataContext? dataContext = null, AspectSlotPath? slotPath = null)` | `ResolvedAspect` | Resolves matching rules and winning declarations without applying values to the element. Throws `ArgumentNullException` when `element`, `catalog`, or `environment` is `null`. |
| `GetDiagnostics(UIElement element)` | `AspectDiagnostics.Snapshot` | Gets the latest diagnostics snapshot recorded by `Apply` for the element, or an empty snapshot if the element has no tracked state. Throws `ArgumentNullException` when `element` is `null`. |
| `GetDependencies(UIElement element)` | `AspectDependencySet` | Gets the dependency set tracked from the latest `Apply` call for the element, or an empty dependency set when none is tracked. Throws `ArgumentNullException` when `element` is `null`. |
| `Clear(UIElement element)` | `void` | Clears aspect-base values from the element based on the last resolved result, resets diagnostics, and untracks dependencies. Throws `ArgumentNullException` when `element` is `null`. |

## Applies to

Cerneala UI aspect resolution and application for `UIElement` instances.

## See also

- `AspectProcessor`
- `AspectCatalog`
- `AspectEnvironment`
- `ResolvedAspect`
- `AspectDependencySet`
