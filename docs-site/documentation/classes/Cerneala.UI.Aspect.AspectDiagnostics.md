# AspectDiagnostics Class

## Definition
Namespace: `Cerneala.UI.Aspect`

Assembly/Project: `Cerneala`

Source: `UI/Aspect/AspectDiagnostics.cs`

Provides the aspect engine diagnostic container type used to expose resolution, token, and counter snapshots.

```csharp
public static class AspectDiagnostics
```

Inheritance:
`Object` -> `AspectDiagnostics`

## Examples

Capture diagnostics after applying aspects to an element and pass them to the diagnostic trace formatter.

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

Create an empty snapshot when no element diagnostics are available.

```csharp
using Cerneala.UI.Aspect;

AspectDiagnostics.Snapshot diagnostics = new();

bool hasResolvedAspect = diagnostics.ResolvedAspect is not null;
int stepCount = diagnostics.ResolutionSteps.Count;
```

## Remarks

`AspectDiagnostics` is a static holder for the nested `Snapshot` type. The class itself has no static methods or state.

`AspectEngine.Apply` stores an `AspectDiagnostics.Snapshot` for the processed element. That snapshot includes the winning `ResolvedAspect`, ordered resolution steps for matched and rejected declarations, token traces for resolved token-backed values, and a copied `AspectEngineCounters` instance.

`AspectEngine.GetDiagnostics` returns the stored snapshot for an element, or a new empty snapshot when the element has not been processed by the engine. `AspectEngine.Clear` also resets the element diagnostics to an empty snapshot.

Empty snapshots have `ResolvedAspect` set to `null`, empty `ResolutionSteps` and `TokenTraces` collections, and a new `AspectEngineCounters` instance.

## Nested Types

| Name | Description |
| --- | --- |
| `AspectDiagnostics.Snapshot` | Immutable snapshot of aspect resolution diagnostics, token traces, and engine counters for an element. |

## Applies to

Cerneala UI aspect diagnostics produced by `AspectEngine`.

## See also

- `AspectEngine`
- `AspectEngineCounters`
- `AspectTrace`
- `ResolvedAspect`
