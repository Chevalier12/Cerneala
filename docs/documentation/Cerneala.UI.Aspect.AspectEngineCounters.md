# AspectEngineCounters Class

## Definition
Namespace: `Cerneala.UI.Aspect`

Assembly/Project: `Cerneala`

Source: `UI/Aspect/AspectEngineCounters.cs`

Stores cumulative aspect engine resolution counters exposed by `AspectEngine` and copied into aspect diagnostics snapshots.

```csharp
public sealed class AspectEngineCounters
```

Inheritance:
`object` -> `AspectEngineCounters`

## Examples

Read counters after applying aspects:

```csharp
using Cerneala.UI.Aspect;
using Cerneala.UI.Controls;

AspectCatalog catalog = new AspectRegistry()
    .Register(DefaultAspectPackage.Create())
    .BuildCatalog();

AspectEngine engine = new();
AspectEnvironment environment = DefaultAspectPackage.CreateEnvironment();

engine.Apply(new Button(), catalog, environment);

int considered = engine.Counters.RulesConsidered;
int matched = engine.Counters.RulesMatched;
int declarations = engine.Counters.DeclarationsResolved;
```

Read the copied counters stored in diagnostics for an applied element:

```csharp
using Cerneala.UI.Aspect;
using Cerneala.UI.Controls;

Button button = new();
AspectEngine engine = new();
AspectCatalog catalog = new AspectRegistry()
    .Register(DefaultAspectPackage.Create())
    .BuildCatalog();

engine.Apply(button, catalog, DefaultAspectPackage.CreateEnvironment());

AspectEngineCounters counters = engine.GetDiagnostics(button).Counters;
int tokenLookups = counters.TokenLookups;
```

## Remarks

`AspectEngineCounters` is a mutable counter holder owned by `AspectEngine`. The public constructor creates a zero-valued counter set. The properties have public getters and internal setters, so callers can inspect values but only code inside the assembly updates them.

`AspectEngine.Resolve` increments `RulesConsidered` for every rule in the catalog, `RulesMatched` for rules whose target matches the current element context, `DeclarationsResolved` for declarations on matched rules, and `TokenLookups` by the number of token dependencies on each resolved declaration. These counters are cumulative for the owning engine instance; applying or resolving another element keeps increasing the same `Counters` object.

`AspectEngine.Apply` stores diagnostics with a copied counter snapshot. The copied `AspectEngineCounters` in `AspectDiagnostics.Snapshot.Counters` preserves the engine counter values at the time diagnostics were built for that element.

`CacheHits` and `CacheMisses` are part of the counter shape, but the current aspect engine implementation does not increment them.

## Constructors

| Name | Description |
| --- | --- |
| `AspectEngineCounters()` | Initializes a zero-valued counter set. |

## Properties

| Name | Type | Description |
| --- | --- | --- |
| `RulesConsidered` | `int` | Number of catalog rules evaluated by the engine. |
| `RulesMatched` | `int` | Number of evaluated rules whose target matched the current aspect match context. |
| `DeclarationsResolved` | `int` | Number of declarations resolved from matched rules. |
| `TokenLookups` | `int` | Number of token dependencies counted while resolving declarations. |
| `CacheHits` | `int` | Cache hit counter slot; currently not incremented by `AspectEngine`. |
| `CacheMisses` | `int` | Cache miss counter slot; currently not incremented by `AspectEngine`. |

## Applies to

Cerneala UI aspect resolution diagnostics and stress-budget checks.

## See also

- `AspectEngine`
- `AspectDiagnostics`
- `AspectCatalog`
- `AspectDeclaration`
