# Scene2DDiagnosticSeverity Enum

## Definition

Namespace: `Cerneala.UI.Controls`

Assembly/Project: `Cerneala`

Source: `UI/Controls/Scene2DModelValidator.cs`

Classifies scene validation and import diagnostics.

```csharp
public enum Scene2DDiagnosticSeverity
```

## Remarks

Only Warning permits publication. Error, Fatal, and Unsupported all make a validation/import result unsuccessful; Unsupported never means “silently omit this feature.”

## Fields

| Name | Value | Meaning |
| --- | ---: | --- |
| `Warning` | 0 | Known optional information is unused. |
| `Error` | 1 | Invalid data or reference. |
| `Fatal` | 2 | Required input cannot be obtained or interpreted. |
| `Unsupported` | 3 | Recognized or unknown construction outside the supported contract. |

