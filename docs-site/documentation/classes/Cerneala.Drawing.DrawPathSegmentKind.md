# DrawPathSegmentKind Enum

## Definition

Namespace: `Cerneala.Drawing`

Assembly/Project: `Cerneala`

Source: `Drawing/DrawPath.cs`

Identifies the typed operation stored by a `DrawPathSegment`.

```csharp
public enum DrawPathSegmentKind
```

## Examples

```csharp
bool closesContour = segment.Kind == DrawPathSegmentKind.Close;
```

## Remarks

Segments are produced by `DrawPathBuilder` or `DrawPathParser`; callers do not construct partially valid segment payloads.

## Fields

| Name | Description |
| --- | --- |
| `Move` | Begins a contour at `EndPoint`. |
| `Line` | Adds a straight segment ending at `EndPoint`. |
| `Quadratic` | Adds a quadratic Bezier using `Control1`. |
| `Cubic` | Adds a cubic Bezier using `Control1` and `Control2`. |
| `Arc` | Adds an SVG endpoint-form elliptical arc. |
| `Close` | Closes the contour back to its starting point. |

## Applies To

Typed path inspection and reusable geometry.

## See Also

- `DrawPathSegment`
- `DrawPathBuilder`
