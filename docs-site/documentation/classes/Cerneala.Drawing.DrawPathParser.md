# DrawPathParser Class

## Definition

Namespace: `Cerneala.Drawing`

Assembly/Project: `Cerneala`

Source: `Drawing/DrawPathParser.cs`

Parses SVG path data into immutable typed paths.

```csharp
public static class DrawPathParser
```

## Examples

```csharp
DrawPath icon = DrawPathParser.ParseSvg(
    "M0 0L24 0L24 24L0 24Z M6 6L18 6L18 18L6 18Z");

drawing.FillPath(icon, brush, DrawFillRule.EvenOdd);
```

## Remarks

`ParseSvg` supports absolute and relative `M`, `L`, `H`, `V`, `C`, `S`, `Q`, `T`, `A`, and `Z` commands. It is the single SVG grammar used by both typed paths and the legacy SVG fill overload. Arc rotation follows SVG degrees.

## Methods

| Name | Return type | Description |
| --- | --- | --- |
| `ParseSvg(string)` | `DrawPath` | Parses SVG data and preserves explicit open/closed contour state. |

## Exceptions

| Exception | Condition |
| --- | --- |
| `ArgumentException` | Input is null, empty, or whitespace. |
| `FormatException` | Input contains malformed or unsupported SVG path syntax. |

## Applies To

SVG compatibility and reusable typed geometry.

## See Also

- `DrawPath`
- `DrawPathBuilder`
