# ServoPoint Structure

## Definition
Namespace: `Cerneala.UI.Servo`
Assembly/Project: `Cerneala`
Source: `UI/Servo/ServoPoint.cs`

Represents a finite point in Servo client DIP coordinates.

```csharp
public readonly record struct ServoPoint
```

## Examples

```csharp
var destination = new ServoPoint(320, 180);
```

## Remarks

Both coordinates must be finite. NaN and infinity throw `ArgumentOutOfRangeException`.

## Constructors

| Name | Description |
| --- | --- |
| `ServoPoint(float, float)` | Creates a point from finite X and Y coordinates. |

## Properties

| Name | Description |
| --- | --- |
| `X` | Gets the horizontal client DIP coordinate. |
| `Y` | Gets the vertical client DIP coordinate. |

## Applies to

Servo pointer actions.
