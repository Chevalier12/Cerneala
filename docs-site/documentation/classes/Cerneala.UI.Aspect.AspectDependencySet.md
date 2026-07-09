# AspectDependencySet Class

## Definition
Namespace: `Cerneala.UI.Aspect`

Assembly/Project: `Cerneala`

Source: `UI/Aspect/AspectDependencySet.cs`

Captures the aspect tokens, states, variants, UI properties, data dependencies, slot, and version stamps that a resolved aspect depends on.

```csharp
public sealed class AspectDependencySet
```

Inheritance:
`object` -> `AspectDependencySet`

## Examples

Read the dependencies recorded for an element after applying aspects:

```csharp
using Cerneala.UI.Aspect;
using Cerneala.UI.Controls;

AspectCatalog catalog = new AspectRegistry()
    .Register(DefaultAspectPackage.Create())
    .BuildCatalog();
AspectEnvironment environment = DefaultAspectPackage.CreateEnvironment();
AspectEngine engine = new();
Button button = new();

engine.Apply(button, catalog, environment);

AspectDependencySet dependencies = engine.GetDependencies(button);

bool usesButtonBackground = dependencies.Tokens.Contains(ButtonTokens.Background);
```

Create an explicit dependency set for code that tracks aspect invalidation inputs directly:

```csharp
using Cerneala.UI.Aspect;
using Cerneala.UI.Controls;

AspectDependencySet dependencies = new(
    tokens: [ButtonTokens.Background],
    states: [AspectState.Hover],
    catalogVersion: 3,
    environmentVersion: 7);
```

## Remarks

`AspectDependencySet` is a small data container used by the aspect resolution and invalidation pipeline. `AspectEngine` builds one from the dependencies discovered while matching aspect conditions and resolving declaration values, then stores it with the resolved aspect. `AspectInvalidationGraph` tracks the set per `UIElement`, and `AspectEngine.GetDependencies` returns the tracked set or an empty set when no dependencies are available.

Constructor arguments are optional. Passing `null` for any dependency list stores an empty list for that category. Non-null lists are stored as provided; callers that need stable snapshots should pass lists they will not mutate after construction.

`CatalogVersion` and `EnvironmentVersion` identify the aspect catalog and environment versions used during resolution. They are version stamps only; this class does not compare versions or perform invalidation by itself.

## Constructors

| Name | Description |
| --- | --- |
| `AspectDependencySet(IReadOnlyList<AspectToken>? tokens = null, IReadOnlyList<AspectState>? states = null, IReadOnlyList<AspectVariantKey>? variants = null, IReadOnlyList<UiProperty>? properties = null, IReadOnlyList<AspectDataDependency>? data = null, AspectSlot? slot = null, int catalogVersion = 0, int environmentVersion = 0)` | Initializes a dependency set, replacing `null` dependency lists with empty lists and storing the supplied slot and version stamps. |

## Properties

| Name | Type | Description |
| --- | --- | --- |
| `Tokens` | `IReadOnlyList<AspectToken>` | Gets aspect tokens used while resolving declaration values. |
| `States` | `IReadOnlyList<AspectState>` | Gets element states used by matched aspect conditions. |
| `Variants` | `IReadOnlyList<AspectVariantKey>` | Gets aspect variant keys used by matched aspect conditions. |
| `Properties` | `IReadOnlyList<UiProperty>` | Gets UI properties used by matched aspect conditions. |
| `Data` | `IReadOnlyList<AspectDataDependency>` | Gets data-context dependencies used by matched aspect conditions. |
| `Slot` | `AspectSlot?` | Gets the matched slot dependency, when aspect resolution was scoped to a slot path. |
| `CatalogVersion` | `int` | Gets the catalog version captured during aspect resolution. |
| `EnvironmentVersion` | `int` | Gets the environment version captured during aspect resolution. |

## Applies to

Cerneala UI aspect resolution, diagnostics, and invalidation tracking.

## See also

- `AspectEngine`
- `AspectInvalidationGraph`
- `ResolvedAspect`
- `AspectConditionDependency`
