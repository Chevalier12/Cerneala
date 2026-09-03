# AspectDiagnostics Class

## Definition
Namespace: `Cerneala.UI.Detective`

Assembly/Project: `Cerneala`

Source: `UI/Detective/AspectDiagnostics.cs`

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
using Cerneala.UI.Detective;
using Cerneala.UI.Elements;

Button button = new();
UIRoot root = new();
root.VisualChildren.Add(button);
root.AspectProcessor.Process(button);

AspectDiagnostics.Snapshot diagnostics = root.Detective.CaptureAspect(button);
AspectTraceSnapshot trace = root.Detective.TraceAspect(button, Control.BackgroundProperty);
```

Create an empty snapshot when no element diagnostics are available.

```csharp
using Cerneala.UI.Detective;

AspectDiagnostics.Snapshot diagnostics = new();

bool hasResolvedAspect = diagnostics.ResolvedAspect is not null;
int stepCount = diagnostics.ResolutionSteps.Count;
```

## Remarks

`AspectDiagnostics` is a static holder for the nested `Snapshot` type. The class itself has no static methods or state.

`AspectEngine.Apply` stores a compact evaluation snapshot for the processed element. The first `Detective.CaptureAspect` call materializes an `AspectDiagnostics.Snapshot` containing the winning `ResolvedAspect`, one ordered resolution step for every considered rule (matched or structurally/conditionally rejected), token traces, and a copied `AspectEngineCounters` instance. Captured conditions are not reevaluated.

`Detective.CaptureAspect` returns the stored snapshot for an element, or a new empty snapshot when the element has not been processed by the root-owned engine. `AspectEngine.Clear` also resets the element diagnostics to an empty snapshot.

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
