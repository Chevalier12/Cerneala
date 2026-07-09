# AspectDiagnostics.Snapshot Class

## Definition
Namespace: `Cerneala.UI.Aspect`

Assembly/Project: `Cerneala`

Source: `UI/Aspect/AspectDiagnostics.cs`

Represents an immutable diagnostic snapshot for aspect resolution results, resolution steps, token traces, and engine counters.

```csharp
public sealed class AspectDiagnostics.Snapshot
```

Inheritance:
`object` -> `AspectDiagnostics.Snapshot`

## Examples

Read diagnostics for an element after applying aspects:

```csharp
using Cerneala.UI.Aspect;
using Cerneala.UI.Controls;
using Cerneala.UI.Diagnostics;

AspectCatalog catalog = new AspectRegistry()
    .Register(DefaultAspectPackage.Create())
    .BuildCatalog();

AspectEngine engine = new();
AspectEnvironment environment = DefaultAspectPackage.CreateEnvironment();
Button button = new();

engine.Apply(button, catalog, environment);

AspectDiagnostics.Snapshot diagnostics = engine.GetDiagnostics(button);
AspectTraceSnapshot trace = AspectTrace.Capture(button, Control.BackgroundProperty, diagnostics);
```

Create an empty snapshot when no resolved aspect is available:

```csharp
using Cerneala.UI.Aspect;

AspectDiagnostics.Snapshot diagnostics = new();

bool hasResolvedAspect = diagnostics.ResolvedAspect is not null;
int resolutionStepCount = diagnostics.ResolutionSteps.Count;
int tokenTraceCount = diagnostics.TokenTraces.Count;
```

## Remarks

`AspectDiagnostics.Snapshot` is the nested diagnostics payload used by `AspectEngine`. `AspectEngine.Apply` builds a snapshot from the resolved aspect, matched and rejected resolution steps, token traces, and the current `AspectEngineCounters` snapshot. `AspectEngine.GetDiagnostics` returns the stored snapshot for an element, or a new empty snapshot when the element has no stored aspect diagnostics.

The constructor substitutes empty read-only lists when `resolutionSteps` or `tokenTraces` is `null`. When `counters` is `null`, it creates a new `AspectEngineCounters` instance. `ResolvedAspect` remains nullable so an empty snapshot can represent the absence of aspect resolution data.

`AspectTrace.Capture` consumes this type to build human-readable diagnostic trace lines for a specific UI property.

## Constructors

| Name | Description |
| --- | --- |
| `Snapshot(ResolvedAspect? resolvedAspect = null, IReadOnlyList<AspectResolutionStep>? resolutionSteps = null, IReadOnlyList<AspectTokenTrace>? tokenTraces = null, AspectEngineCounters? counters = null)` | Initializes a snapshot with optional resolved aspect data, resolution steps, token traces, and counters. Null list arguments become empty lists, and a null counters argument becomes a new `AspectEngineCounters` instance. |

## Properties

| Name | Type | Description |
| --- | --- | --- |
| `ResolvedAspect` | `ResolvedAspect?` | Gets the resolved aspect data for the element, or `null` when no aspect diagnostics are available. |
| `ResolutionSteps` | `IReadOnlyList<AspectResolutionStep>` | Gets the matched and rejected aspect resolution steps captured for the element. |
| `TokenTraces` | `IReadOnlyList<AspectTokenTrace>` | Gets token trace entries for resolved values that depend on aspect tokens. |
| `Counters` | `AspectEngineCounters` | Gets the aspect engine counters captured with the snapshot. |

## Applies to

Cerneala UI aspect diagnostics produced and returned by `AspectEngine`.

## See also

- `AspectDiagnostics`
- `AspectEngine`
- `AspectEngineCounters`
- `AspectTrace`
- `ResolvedAspect`
