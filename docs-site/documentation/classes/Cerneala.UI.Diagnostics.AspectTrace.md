# AspectTrace Class

## Definition
Namespace: `Cerneala.UI.Diagnostics`  
Assembly/Project: `Cerneala`  
Source: `UI/Diagnostics/AspectTrace.cs`

Creates a line-based diagnostic snapshot that explains how an aspect value was resolved for a UI property on an element.

```csharp
public static class AspectTrace
```

Inheritance:  
`object` -> `AspectTrace`

## Examples

Capture the aspect trace for a button background after an `AspectEngine` has applied a catalog.

```csharp
using Cerneala.UI.Aspect;
using Cerneala.UI.Controls;
using Cerneala.UI.Diagnostics;

Button button = new();
AspectEngine engine = new();

// Apply an aspect catalog before capturing diagnostics.
engine.Apply(button, catalog, new AspectEnvironment("app"));

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

`AspectTrace` is a diagnostics helper for the modern aspect system. It does not resolve aspects itself and does not mutate the supplied element or property. Instead, `Capture` formats an existing `AspectDiagnostics.Snapshot` into ordered text lines.

The first line always names the traced property by using `UiProperty.DiagnosticName`. If the diagnostics snapshot is omitted or has no resolved aspect, the trace contains `No aspect diagnostics.` and returns immediately.

When diagnostics are available, the trace can include:

| Line kind | Source data |
| --- | --- |
| `winner` | The resolved value for the requested `UiProperty`, including the winning declaration diagnostic name or property name and the resolved value. |
| Resolution step | Each `AspectResolutionStep`, including package, rule, target, layer, specificity, declaration order, and outcome. |
| `rejected` | Each rejected declaration from `ResolvedAspect.RejectedDeclarations`, including its reason. |
| `token` | Each `AspectTokenTrace`, including token name, provider name, raw value, and resolved value. |
| `slot` | The resolved aspect dependency slot when one is present. |
| `variant` | Each variant dependency name recorded by the resolved aspect. |

`Capture` throws `ArgumentNullException` when `element` or `property` is `null`. The current implementation validates `element` but otherwise uses the supplied diagnostics snapshot; the element is not printed in the resulting lines.

## Methods

| Name | Return Type | Description |
| --- | --- | --- |
| `Capture(UIElement element, UiProperty property, AspectDiagnostics.Snapshot? diagnostics = null)` | `AspectTraceSnapshot` | Creates a textual trace for `property` from an optional aspect diagnostics snapshot. Throws when `element` or `property` is `null`. |

## Result Types

### AspectTraceSnapshot

```csharp
public sealed class AspectTraceSnapshot
```

Stores the immutable list reference of lines produced by `AspectTrace.Capture`.

| Member | Type | Description |
| --- | --- | --- |
| `AspectTraceSnapshot(IReadOnlyList<string> lines)` | Constructor | Creates a snapshot from the supplied line list. Throws when `lines` is `null`. |
| `Lines` | `IReadOnlyList<string>` | Gets the trace lines in the order produced by `Capture`. |

## Applies To

Cerneala UI aspect diagnostics.

## See Also

- `Cerneala.UI.Aspect.AspectDiagnostics`
- `Cerneala.UI.Aspect.AspectEngine`
- `Cerneala.UI.Aspect.ResolvedAspect`
- `Cerneala.UI.Diagnostics.AspectTraceSnapshot`
- Source: `UI/Diagnostics/AspectTrace.cs`
