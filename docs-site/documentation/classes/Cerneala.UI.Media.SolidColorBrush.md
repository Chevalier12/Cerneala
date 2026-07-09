# SolidColorBrush Class

## Definition
Namespace: `Cerneala.UI.Media`

Assembly/Project: `Cerneala`

Source: `UI/Media/SolidColorBrush.cs`

Represents a brush that paints with a single `DrawColor`.

```csharp
public sealed record SolidColorBrush(DrawColor Color) : Brush
```

Inheritance:
`object` -> `Brush` -> `SolidColorBrush`

## Examples

Create a solid brush and read the resolved solid color through the base `Brush` API.

```csharp
using Cerneala.Drawing;
using Cerneala.UI.Media;

SolidColorBrush brush = new(DrawColor.White);

DrawColor? color = brush.SolidColor;
```

## Remarks

`SolidColorBrush` is the concrete `Brush` implementation for a uniform color. Its primary constructor stores the supplied `DrawColor` in the `Color` property, and its `SolidColor` override returns that same value.

Because this type is a sealed record, equality and hashing use record value semantics. Two `SolidColorBrush` instances with the same `Color` compare as equal.

## Constructors

| Name | Description |
| --- | --- |
| `SolidColorBrush(DrawColor Color)` | Initializes a brush that paints with the specified `DrawColor`. |

## Properties

| Name | Type | Description |
| --- | --- | --- |
| `Color` | `DrawColor` | Gets the color supplied to the primary constructor. |
| `SolidColor` | `DrawColor?` | Gets `Color`, exposing the brush as a solid color through the base `Brush` contract. |

## Methods

| Name | Description |
| --- | --- |
| `Deconstruct(out DrawColor Color)` | Deconstructs the record into its `Color` component. |

## Applies to

`Cerneala` UI media brushes.

## See also

- `Cerneala.UI.Media.Brush`
- `Cerneala.Drawing.DrawColor`
