# DrawCommandStateAnalysis Class

## Definition

Namespace: `Cerneala.Drawing`

Assembly/Project: `Cerneala`

Source: `Drawing/DrawState.cs`

Contains the immutable state-stack analysis for one version of a command list.

```csharp
public sealed class DrawCommandStateAnalysis
```

## Examples

```csharp
DrawCommandStateAnalysis analysis = new DrawCommandStateAnalyzer().Analyze(commands);
foreach (DrawCommandStateEntry entry in analysis.Entries)
    Console.WriteLine(entry.Bounds);
```

## Remarks

The result is tied to the analyzed `DrawCommandList` reference and version. Cerneala shares it between frame analysis, backend validation, damage tracking, and Prism.

## Properties

| Name | Type | Description |
| --- | --- | --- |
| `Entries` | `IReadOnlyList<DrawCommandStateEntry>` | Gets one resolved entry per command. |
| `CommandListVersion` | `long` | Gets the command-list version captured by the analysis. |

## Applies To

Cerneala retained drawing submissions.
