# Scene2DValidationOptions Class

## Definition

Namespace: `Cerneala.UI.Controls`

Assembly/Project: `Cerneala`

Source: `UI/Controls/Scene2DModelValidator.cs`

Defines trusted-caller aggregate validation and diagnostic retention budgets.

```csharp
public sealed class Scene2DValidationOptions
```

## Examples

```csharp
var options = new Scene2DValidationOptions { MaxDiagnostics = 16, MaxCells = 4096 };
```

## Remarks

All values are init-only and must be positive when validation starts. Defaults are used when options are null. A caller may lower or raise aggregate budgets; source content cannot change them. Per-component constructor caps remain in effect regardless of these options.

## Properties

| Name | Default | Description |
| --- | ---: | --- |
| `MaxDiagnostics` | 128 | Maximum retained diagnostics; failure state survives truncation. |
| `MaxCells` | 1,048,576 | Total decoded cells across the validated map/document. |
| `MaxChunks` | 65,536 | Total chunks. |
| `MaxLayers` | 4,096 | Total tile/layer models, including data-only layers. |
| `MaxEntities` | 65,536 | Total entities plus promotion references in a document. |

## See also

- [Scene2DModelValidator](Cerneala.UI.Controls.Scene2DModelValidator.md)

