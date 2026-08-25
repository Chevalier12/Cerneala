# DrawTextLayoutLine Class

## Definition

Namespace: `Cerneala.Drawing`

Assembly/Project: `Cerneala`

Source: `Drawing/DrawTextLayout.cs`

Describes one immutable positioned line in a `DrawTextLayout`.

```csharp
public sealed class DrawTextLayoutLine
```

## Examples

```csharp
foreach (DrawTextLayoutLine line in layout.Lines)
    Console.WriteLine($"{line.Text}: {line.Bounds.Width}");
```

## Remarks

Run order is visual order for the resolved base direction. `IsTrimmed` reports an ellipsis produced by width, height, or line-count constraints.

## Properties

| Name | Type | Description |
| --- | --- | --- |
| `Runs` | `IReadOnlyList<DrawTextLayoutRun>` | Gets positioned styled runs. |
| `Bounds` | `DrawRect` | Gets line-local bounds within the layout. |
| `Baseline` | `float` | Gets the baseline in layout coordinates. |
| `Direction` | `DrawTextDirection` | Gets the resolved base direction. |
| `IsTrimmed` | `bool` | Gets whether the line ends in a generated ellipsis. |
| `Text` | `string` | Gets visual run text concatenated for inspection. |

## Applies To

Layout measurement and diagnostics.
