# DrawCommandStateAnalyzer Class

## Definition

Namespace: `Cerneala.Drawing`

Assembly/Project: `Cerneala`

Source: `Drawing/DrawState.cs`

Validates the common drawing-state stack and resolves command state and bounds.

```csharp
public sealed class DrawCommandStateAnalyzer
```

## Examples

```csharp
DrawCommandStateAnalysis analysis = new DrawCommandStateAnalyzer().Analyze(commands);
```

## Remarks

The analyzer treats transform, rectangular and path clips, opacity, blend, layer, and Prism scopes as one LIFO stack. An unmatched pop or unclosed push throws `InvalidOperationException` with the relevant command index.

## Constructors

| Name | Description |
| --- | --- |
| `DrawCommandStateAnalyzer()` | Creates an analyzer with no retained per-list state. |

## Methods

| Name | Return Type | Description |
| --- | --- | --- |
| `Analyze(DrawCommandList)` | `DrawCommandStateAnalysis` | Validates and analyzes the current list version. |

## Applies To

Drawing backends, retained damage, and Prism graph construction.
