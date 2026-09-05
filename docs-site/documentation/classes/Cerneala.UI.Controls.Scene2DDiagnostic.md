# Scene2DDiagnostic Record

## Definition

Namespace: `Cerneala.UI.Controls`

Assembly/Project: `Cerneala`

Source: `UI/Controls/Scene2DModelValidator.cs`

Carries a stable code, severity, message, and optional source location.

```csharp
public sealed record Scene2DDiagnostic(
    string Code, Scene2DDiagnosticSeverity Severity, string Message,
    string FilePath = "", string JsonPath = "$");
```

## Remarks

Core validation supplies deterministic codes; external importers supply file and JSON locations where available. This data record does not validate, open, or interpret its location strings. Equal records compare by value. `Scene2DDiagnosticCollector` and `Scene2DModelValidator.GetDiagnostic` bound each message, file and JSON-path string to 4,096 UTF-16 characters; constructing this record directly does not truncate it.

## Properties

| Name | Description |
| --- | --- |
| `Code` | Stable machine-readable category. |
| `Severity` | Warning, Error, Fatal, or Unsupported. |
| `Message` | Actionable problem description. |
| `FilePath` | Source file when available; otherwise empty. |
| `JsonPath` | Source/model location; defaults to `$`. |

## See also

- [Scene2DDiagnosticSeverity](Cerneala.UI.Controls.Scene2DDiagnosticSeverity.md)
- [Scene2DModelValidator](Cerneala.UI.Controls.Scene2DModelValidator.md)
