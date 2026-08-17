# SvgPath Class

## Definition
Namespace: `Cerneala.UI.Controls.Shapes`
Assembly/Project: `Cerneala`
Source: `UI/Controls/Shapes/SvgPath.cs`

Represents a shape control that renders SVG path data inside a declared view box.

```csharp
public sealed class SvgPath : Shape
```

Inheritance:
`object` -> `UiObject` -> `UIElement` -> `Control` -> `Shape` -> `SvgPath`

## Examples

```xml
<SvgPath
    Width="16"
    Height="16"
    Data="M4 12L10 18L20 6Z"
    ViewBox="0 0 24 24"
    Fill="#FF000000" />
```

## Remarks

`SvgPath` converts the `Data` and `ViewBox` strings into cached `SvgGeometry` data. The geometry is rebuilt only when either string changes.

`ViewBox` accepts four invariant-culture floating-point values separated by whitespace or commas: `x`, `y`, `width`, and `height`. Width and height must be greater than zero. Invalid values are rejected when assigned.

The inherited `Shape` renderer scales the SVG geometry from `ViewBox` into the arranged bounds of the control. A visible `Fill` brush and positive arranged width and height are required for a fill command to be emitted. Empty or whitespace-only `Data` produces no geometry and no drawing command.

## Constructors

| Name | Description |
| --- | --- |
| `SvgPath()` | Initializes a path with empty `Data` and a `0 0 1 1` view box. |

## Fields

| Name | Type | Description |
| --- | --- | --- |
| `DataProperty` | `UiProperty<string>` | Identifies the `Data` UI property. Changes affect measure and render. |
| `ViewBoxProperty` | `UiProperty<string>` | Identifies the validated `ViewBox` UI property. Changes affect measure and render. |

## Properties

| Name | Type | Default | Description |
| --- | --- | --- | --- |
| `Data` | `string` | Empty string | Gets or sets the SVG path-data string. |
| `ViewBox` | `string` | `0 0 1 1` | Gets or sets the SVG source coordinate rectangle. |

## Relevant Inherited Shape Properties

| Name | Type | Default | Description |
| --- | --- | --- | --- |
| `Fill` | `Brush?` | `null` | Provides the brush used to fill the SVG path. |
| `Opacity` | `float` | `1` | Controls the rendered opacity. |
| `RenderTransform` | `Transform` | `Transform.Identity` | Applies a render transform inherited from `Shape`. |

## Applies To

Cerneala retained UI shape controls and generated `.crn` markup in the `Cerneala` project.

## See Also

- `UI/Controls/Shapes/Shape.cs`
- `UI/Media/SvgGeometry.cs`
- `UI/Controls/Shapes/Path.cs`
