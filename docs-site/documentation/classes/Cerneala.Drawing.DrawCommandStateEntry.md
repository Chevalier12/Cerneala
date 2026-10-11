# DrawCommandStateEntry Struct

## Definition

Namespace: `Cerneala.Drawing`

Assembly/Project: `Cerneala`

Source: `Drawing/DrawState.cs`

Describes the resolved drawing state and world-space bounds at one command index.

```csharp
public readonly record struct DrawCommandStateEntry
```

## Examples

```csharp
DrawCommandStateAnalysis analysis = new DrawCommandStateAnalyzer().Analyze(commands);
DrawRect? worldBounds = analysis.Entries[commandIndex].Bounds;
```

## Remarks

`MatchingCommandIndex` links a push to its pop and a pop to its push, or is `-1` for ordinary drawing commands. Scope command bounds cover the drawing performed inside that scope.

## Constructors

| Name | Description |
| --- | --- |
| `DrawCommandStateEntry(DrawRect?, Matrix3x2, DrawRect?, float, DrawBlendMode, bool, int)` | Creates one resolved command-state snapshot. |

## Properties

| Name | Type | Description |
| --- | --- | --- |
| `Bounds` | `DrawRect?` | Gets conservative world-space bounds, or `null` when bounds are unknown. |
| `Transform` | `System.Numerics.Matrix3x2` | Gets the accumulated transform active before the command. |
| `ClipBounds` | `DrawRect?` | Gets the conservative world-space clip intersection. |
| `Opacity` | `float` | Gets the accumulated scope opacity. |
| `BlendMode` | `DrawBlendMode` | Gets the active blend mode. |
| `IsContextSensitive` | `bool` | Gets whether the command is a state or Prism delimiter: `true` for `PushClip`, `PushPathClip`, `PopClip`, `BeginPrism`, `EndPrism`, and the push and pop commands for transform, opacity, blend, and layer; `false` for drawing commands. Context-sensitive commands are excluded from scope bounds. |
| `MatchingCommandIndex` | `int` | Gets the matching state command index, or `-1`. |

## Applies To

Drawing backends, damage tracking, and Prism analysis.
