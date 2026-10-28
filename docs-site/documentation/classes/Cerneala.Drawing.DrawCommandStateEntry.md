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

`Bounds` is `null` (unknown) when the conservative bounds do not fit in a `DrawRect`, for example a `FillRectangle(new DrawRect(0, 0, 5000, 5000), ...)` under `PushTransform(Matrix3x2.CreateScale(1e6f))`, or a `DrawRectangle` with a pen 1,000,000,000 pixels thick. A scope's push and pop entries have `null` bounds when any drawing inside the scope has unknown bounds, or when the union of its drawing does not fit in a `DrawRect`, such as two fills at x = -1.9e9 and x = 1.9e9 in one `PushOpacity` scope. Such a scope stays unknown even when drawing with known bounds follows. Analysis does not throw for these commands.

A `DrawRectangle`, `DrawRoundedRectangle`, or `DrawEllipse` stroke drawn with a color or brush and a thickness, without a `DrawPen`, is drawn with `DrawStrokeStyle.Default`. Its `Bounds` are therefore the bounds of the same stroke drawn with `new DrawPen(brush, thickness)`: for `DrawRectangle(new DrawRect(10, 10, 20, 20), Color.White, 6)` they contain the stroke from `(7, 7)` to `(33, 33)`.

`ClipBounds` is `null` only when no clip is active; a clip is never unknown. When a clip's world-space bounds do not fit in a `DrawRect`, `ClipBounds` is the part of that clip inside the largest `DrawRect` centered on the origin, from `(-1e9, -1e9)` to `(1e9, 1e9)`. Every render target lies inside that square, so drawing is clipped exactly as by the full clip. For example, `PushClip(new DrawRect(0, 0, 5000, 5000))` under `PushTransform(Matrix3x2.CreateScale(1e6f))` has the world clip `(0, 0)` to `(5e9, 5e9)`, and its `ClipBounds` is `DrawRect(0, 0, 1e9, 1e9)`.

## Constructors

| Name | Description |
| --- | --- |
| `DrawCommandStateEntry(DrawRect?, Matrix3x2, DrawRect?, float, DrawBlendMode, bool, int)` | Creates one resolved command-state snapshot. |

## Properties

| Name | Type | Description |
| --- | --- | --- |
| `Bounds` | `DrawRect?` | Gets conservative world-space bounds, or `null` when bounds are unknown. |
| `Transform` | `System.Numerics.Matrix3x2` | Gets the accumulated transform active before the command. |
| `ClipBounds` | `DrawRect?` | Gets the world-space clip intersection, or `null` when no clip is active. |
| `Opacity` | `float` | Gets the accumulated scope opacity. |
| `BlendMode` | `DrawBlendMode` | Gets the active blend mode. |
| `IsContextSensitive` | `bool` | Gets whether the command is a state or Prism delimiter: `true` for `PushClip`, `PushPathClip`, `PopClip`, `BeginPrism`, `EndPrism`, and the push and pop commands for transform, opacity, blend, and layer; `false` for drawing commands. Context-sensitive commands are excluded from scope bounds. |
| `MatchingCommandIndex` | `int` | Gets the matching state command index, or `-1`. |

## Applies To

Drawing backends, damage tracking, and Prism analysis.
