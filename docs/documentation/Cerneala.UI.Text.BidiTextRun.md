# BidiTextRun Class

## Definition
Namespace: `Cerneala.UI.Text`

Assembly/Project: `Cerneala`

Source: `UI/Text/BidiTextService.cs`

Represents a directional run within a bidirectional text string.

```csharp
public readonly record struct BidiTextRun(int Start, int Length, TextDirection Direction)
```

Inheritance:
`ValueType` -> `BidiTextRun`

## Examples

Inspect directional runs returned by `BidiTextService`:

```csharp
using Cerneala.UI.Text;

IReadOnlyList<BidiTextRun> runs =
    BidiTextService.Default.GetDirectionalRuns("Hello שלום");

foreach (BidiTextRun run in runs)
{
    int start = run.Start;
    int length = run.Length;
    TextDirection direction = run.Direction;
}
```

## Remarks

`BidiTextRun` stores the start index, run length, and normalized text direction for a segment produced by `BidiTextService.GetDirectionalRuns`.

The type does not validate indices or slice text by itself. It is a value object used to report directional segmentation.

## Properties

| Name | Type | Description |
| --- | --- | --- |
| `Start` | `int` | Gets the zero-based start index of the run. |
| `Length` | `int` | Gets the number of characters in the run. |
| `Direction` | `TextDirection` | Gets the text direction assigned to the run. |

## Applies To

Cerneala UI bidirectional text analysis.

## See Also

- `Cerneala.UI.Text.BidiTextService`
- `Cerneala.UI.Text.TextDirection`
