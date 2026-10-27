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

As in SVG 1.1 (section 8.3.3), a command that follows `Z` or `z` without a new `M` or `m` starts a new contour at the initial point of the closed contour, and relative commands are relative to that point. For example, `"M0 0L10 0L10 10ZL20 20"` and `"M0 0L10 0L10 10zl20 20"` both parse into two contours, the closed triangle and an open contour from `(0, 0)` to `(20, 20)`, the same result as `"M0 0L10 0L10 10Z M0 0L20 20"`. A second `Z` directly after `Z`, as in `"M0 0L1 1ZZ"`, closes a new contour that has only its start point, so it throws `FormatException` like `"M0 0Z"`.

## Methods

| Name | Return type | Description |
| --- | --- | --- |
| `ParseSvg(string)` | `DrawPath` | Parses SVG data and preserves explicit open/closed contour state. |

## Exceptions

| Exception | Condition |
| --- | --- |
| `ArgumentNullException` | Input is `null`. |
| `ArgumentException` | Input is empty or whitespace. |
| `FormatException` | Input contains malformed or unsupported SVG path syntax, for example `"L1 1"` without a leading move command, `"M0 0"` with no drawable segment, or `"M0 0Z"`, which closes a contour that has only a move command. The message ends with the path offset, for example `Path offset: 5.` for `"M0 0Z"`. |
| `ArgumentOutOfRangeException` | An arc radius or rotation number is too large for `float` and parses as infinity, for example `"M0 0 A1e39 1 0 0 1 5 5"`. |

## Applies To

SVG compatibility and reusable typed geometry.

## See Also

- `DrawPath`
- `DrawPathBuilder`
