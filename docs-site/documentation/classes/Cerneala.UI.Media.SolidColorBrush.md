# SolidColorBrush Class

## Definition
Namespace: `Cerneala.UI.Media`

Assembly/Project: `Cerneala`

Source: `UI/Media/SolidColorBrush.cs`

Represents a brush that paints with a single `Color`.

```csharp
public sealed record SolidColorBrush(Color Color) : Brush
```

Inheritance:
`object` -> `Brush` -> `SolidColorBrush`

## Examples

Create a solid brush and read the resolved solid color through the base `Brush` API.

```csharp
using Cerneala.Drawing;
using Cerneala.UI.Media;

SolidColorBrush brush = new(Color.White);

Color? color = brush.SolidColor;
```

## Remarks

`SolidColorBrush` is the concrete `Brush` implementation for a uniform color. Its primary constructor stores the supplied `Color` in the `Color` property, and its `SolidColor` override returns that same value.

Because this type is a sealed record, equality and hashing use record value semantics. Two `SolidColorBrush` instances with the same `Color` compare as equal.

## Constructors

| Name | Description |
| --- | --- |
| `SolidColorBrush(Color Color)` | Initializes a brush that paints with the specified `Color`. |

## Properties

| Name | Type | Description |
| --- | --- | --- |
| `Color` | `Color` | Gets the color supplied to the primary constructor. |
| `SolidColor` | `Color?` | Gets `Color`, exposing the brush as a solid color through the base `Brush` contract. |

## Methods

| Name | Description |
| --- | --- |
| `Deconstruct(out Color Color)` | Deconstructs the record into its `Color` component. |

## Applies to

`Cerneala` UI media brushes.

## See also

- `Cerneala.UI.Media.Brush`
- `Cerneala.Drawing.Color`
