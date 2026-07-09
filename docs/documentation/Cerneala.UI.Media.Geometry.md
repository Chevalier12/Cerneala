# Geometry Class

## Definition
Namespace: `Cerneala.UI.Media`

Assembly/Project: `Cerneala`

Source: `UI/Media/Geometry.cs`

Defines the base type for UI geometry objects that expose drawing bounds.

```csharp
public abstract record Geometry
```

Inheritance:
`Object` -> `Geometry`

Derived:
`EllipseGeometry`, `PathGeometry`, `RectangleGeometry`

## Examples

Read geometry bounds through the base type.

```csharp
using Cerneala.Drawing;
using Cerneala.UI.Media;

DrawRect GetBounds(Geometry geometry)
{
    return geometry.Bounds;
}
```

## Remarks

`Geometry` is an abstract record used as the common base for concrete UI geometry shapes.

Derived types provide the `Bounds` rectangle used by drawing, clipping, hit testing, or layout-related geometry operations.

## Properties

| Name | Type | Description |
| --- | --- | --- |
| `Bounds` | `DrawRect` | Gets the bounding rectangle for the geometry. |

## Applies to

- `Cerneala.UI.Media.Geometry`

## See also

- `Cerneala.UI.Media.EllipseGeometry`
- `Cerneala.UI.Media.RectangleGeometry`
- `Cerneala.UI.Media.PathGeometry`
