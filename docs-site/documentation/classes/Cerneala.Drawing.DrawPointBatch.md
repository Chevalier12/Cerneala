# DrawPointBatch Class

## Definition

Namespace: `Cerneala.Drawing`

Assembly/Project: `Cerneala`

Source: `Drawing/DrawBatches.cs`

Stores an immutable collection of same-sized colored points as one reusable draw payload.

```csharp
public sealed class DrawPointBatch
```

## Examples

```csharp
DrawPointBatch batch = new(points, Color.CornflowerBlue, diameter: 2);
drawing.DrawPointBatch(batch);
```

## Remarks

Construction copies the input points and builds one triangle mesh. Recording the same batch again retains the same `Version` and avoids one command per point. Create a new batch when the points, color, or diameter changes.

## Constructors

| Name | Description |
| --- | --- |
| `DrawPointBatch(IEnumerable<DrawPoint> points, Color color, float diameter = 1)` | Copies a non-empty point sequence and builds its immutable mesh. |

## Properties

| Name | Type | Description |
| --- | --- | --- |
| `Points` | `IReadOnlyList<DrawPoint>` | Gets the copied points. |
| `Color` | `Color` | Gets the shared point color. |
| `Diameter` | `float` | Gets the positive shared point diameter. |
| `Version` | `long` | Gets the stable immutable-payload version. |
| `Bounds` | `DrawRect` | Gets bounds including point diameter. |

## Applies To

High-volume point drawing through one command and one primitive submission.
