# DrawLineBatch Class

## Definition

Namespace: `Cerneala.Drawing`

Assembly/Project: `Cerneala`

Source: `Drawing/DrawBatches.cs`

Stores immutable colored line segments as one reusable draw payload.

```csharp
public sealed class DrawLineBatch
```

## Examples

```csharp
DrawLineBatch batch = new(
    [new DrawLineSegment2D(new DrawPoint(0, 0), new DrawPoint(80, 24), Color.White, 2)]);
drawing.DrawLineBatch(batch);
```

## Remarks

Construction copies the input segments and expands them into one indexed triangle mesh. Recording the same instance retains its `Version`; create a new batch when any segment changes.

## Constructors

| Name | Description |
| --- | --- |
| `DrawLineBatch(IEnumerable<DrawLineSegment2D> lines)` | Copies a non-empty segment sequence and builds its immutable mesh. |

## Properties

| Name | Type | Description |
| --- | --- | --- |
| `Lines` | `IReadOnlyList<DrawLineSegment2D>` | Gets the copied line segments. |
| `Version` | `long` | Gets the stable immutable-payload version. |
| `Bounds` | `DrawRect` | Gets the mesh bounds including line thickness. |

## Applies To

High-volume line drawing through one command and one primitive submission.
