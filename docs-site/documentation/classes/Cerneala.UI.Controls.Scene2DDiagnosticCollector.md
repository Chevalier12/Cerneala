# Scene2DDiagnosticCollector Class

## Definition

Namespace: `Cerneala.UI.Controls`

Assembly/Project: `Cerneala`

Source: `UI/Controls/Scene2DModelValidator.cs`

Collects bounded diagnostics using the same failure and truncation rules as core scene validation.

```csharp
public sealed class Scene2DDiagnosticCollector
```

## Examples

```csharp
var diagnostics = new Scene2DDiagnosticCollector(maxDiagnostics: 32);
diagnostics.Add(new Scene2DDiagnostic(
    "SCN2D001", Scene2DDiagnosticSeverity.Fatal, "Required map is missing.", "map.tmj"));
Scene2DValidationResult result = diagnostics.Complete();
```

## Remarks

Entries retain insertion order. Warning is the only non-failing severity. Errors added after the retention budget is full still make `Success` false. Message, file path and JSON path are each limited to 4,096 UTF-16 characters, ending in `...` when shortened. Codes are caller-supplied stable identifiers.

`Complete` returns an immutable snapshot; subsequent additions do not alter previous results. It does not reset or seal the collector. Instances are not thread-safe. The collector bounds retained output, not the caller's input parsing or iteration.

## Constructors

| Name | Description |
| --- | --- |
| `Scene2DDiagnosticCollector(int maxDiagnostics = 128)` | Sets a positive retained-entry limit; rejects zero or negative limits. |

## Methods

| Name | Description |
| --- | --- |
| `Add(Scene2DDiagnostic)` | Adds a non-null diagnostic, preserving failure state even when the entry is omitted. |
| `Complete()` | Returns the current `Scene2DValidationResult` snapshot. |

## See also

- [Scene2DDiagnostic](Cerneala.UI.Controls.Scene2DDiagnostic.md)
- [Scene2DValidationResult](Cerneala.UI.Controls.Scene2DValidationResult.md)
