# Scene2DValidationResult Class

## Definition

Namespace: `Cerneala.UI.Controls`

Assembly/Project: `Cerneala`

Source: `UI/Controls/Scene2DModelValidator.cs`

Exposes an immutable validation outcome and a bounded diagnostic snapshot.

```csharp
public sealed class Scene2DValidationResult
```

## Remarks

Created by `Scene2DModelValidator` or `Scene2DDiagnosticCollector`, not by a public constructor. Diagnostics are retained in deterministic traversal order. A truncated list is not an exhaustive error inventory. Always check Success even if all retained entries are warnings.

## Properties

| Name | Type | Description |
| --- | --- | --- |
| `Success` | `bool` | No fatal, error, or unsupported diagnostic occurred, including omitted entries. |
| `Diagnostics` | `IReadOnlyList<Scene2DDiagnostic>` | Read-only retained diagnostic snapshot. |
| `DiagnosticsTruncated` | `bool` | Additional diagnostics were omitted, or validation stopped after reaching the retention budget with a known failure. |
