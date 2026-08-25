# DrawTextLayoutRun Class

## Definition

Namespace: `Cerneala.Drawing`

Assembly/Project: `Cerneala`

Source: `Drawing/DrawTextLayout.cs`

Describes one immutable shaped, styled, and positioned run.

```csharp
public sealed class DrawTextLayoutRun
```

## Examples

```csharp
DrawTextLayoutRun run = layout.Lines[0].Runs[0];
DrawPoint baseline = run.Position;
```

## Remarks

`Position` is the run baseline relative to the layout origin. `Bounds` is layout-local. The selected font may be a fallback chosen for a complete grapheme cluster.

## Properties

| Name | Type | Description |
| --- | --- | --- |
| `TextRun` | `DrawTextRun` | Gets the backend-compatible shaped text input. |
| `Text` | `string` | Gets run text. |
| `Font` | `IDrawFont` | Gets the selected primary or fallback font. |
| `Size` | `float` | Gets the scaled font size. |
| `Position` | `DrawPoint` | Gets the layout-relative baseline position. |
| `Brush` | `IDrawBrush` | Gets glyph paint. |
| `Opacity` | `float` | Gets run opacity. |
| `Direction` | `DrawTextDirection` | Gets resolved run direction. |
| `Bounds` | `DrawRect` | Gets conservative layout-local run bounds. |

## Applies To

Backend rendering and layout inspection.
