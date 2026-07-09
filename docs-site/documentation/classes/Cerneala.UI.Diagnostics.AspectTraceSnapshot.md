# AspectTraceSnapshot Class

## Definition
Namespace: `Cerneala.UI.Diagnostics`

Assembly/Project: `Cerneala`

Source: `UI/Diagnostics/AspectTrace.cs`

Represents the human-readable lines produced for an aspect resolution trace.

```csharp
public sealed class AspectTraceSnapshot
```

Inheritance:
`object` -> `AspectTraceSnapshot`

## Examples

Capture trace lines for a button background aspect:

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

AspectTraceSnapshot trace = AspectTrace.Capture(
    button,
    Control.BackgroundProperty,
    engine.GetDiagnostics(button));

foreach (string line in trace.Lines)
{
    Console.WriteLine(line);
}
```

## Remarks

`AspectTraceSnapshot` is returned by `AspectTrace.Capture`. The snapshot exposes the ordered diagnostic strings that describe a property aspect trace.

The first line names the traced property. When no resolved aspect diagnostics are available, the trace also contains `No aspect diagnostics.`. When diagnostics are present, `AspectTrace.Capture` can add lines for the winning declaration, each resolution step, rejected declarations and rejection reasons, token resolution, the active slot, and active variants.

The constructor requires a non-null `IReadOnlyList<string>`. It stores the supplied list reference as `Lines`; it does not clone the collection.

## Constructors

| Name | Description |
| --- | --- |
| `AspectTraceSnapshot(IReadOnlyList<string> lines)` | Initializes a trace snapshot with the supplied ordered diagnostic lines. Throws `ArgumentNullException` when `lines` is `null`. |

## Properties

| Name | Type | Description |
| --- | --- | --- |
| `Lines` | `IReadOnlyList<string>` | Gets the ordered diagnostic trace lines. |

## Applies to

Cerneala UI aspect diagnostics produced by `AspectTrace`.

## See also

- `AspectTrace`
- `Cerneala.UI.Aspect.AspectDiagnostics.Snapshot`
- `Cerneala.UI.Aspect.AspectEngine`
- `Cerneala.UI.Core.UiProperty`
