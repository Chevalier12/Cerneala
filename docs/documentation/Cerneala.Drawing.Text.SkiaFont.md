# SkiaFont Class

## Definition
Namespace: `Cerneala.Drawing.Text`

Assembly/Project: `Cerneala`

Source: [`UI/Drawing/Text/SkiaFont.cs`](../../UI/Drawing/Text/SkiaFont.cs)

Represents a Skia-backed font used by Cerneala drawing and text services.

```csharp
public sealed class SkiaFont : IDrawFont
```

Inheritance:
`Object` -> `SkiaFont`

Implements:
`IDrawFont`

## Examples

```csharp
using Cerneala.Drawing;
using Cerneala.Drawing.Text;
using SkiaSharp;

IDrawFont font = new SkiaFont(SKTypeface.Default, "Arial", 16);

Console.WriteLine(font.FamilyName);
Console.WriteLine(font.Size);
```

## Remarks

`SkiaFont` stores the `SKTypeface` that the Skia text pipeline needs, while also exposing the framework-level `IDrawFont` metadata through `FamilyName` and `Size`.

The constructor rejects a null `typeface`, a null, empty, or whitespace-only `familyName`, and invalid text sizes. A valid size is positive, finite, and no greater than `16384`.

`SystemFontSource.LoadFont` creates `SkiaFont` instances from operating-system fonts. `TextShaper` only shapes text runs whose font is a `SkiaFont`; lower-level Skia shaping and rasterization code expects the same concrete type.

## Constructors

| Name | Description |
| --- | --- |
| `SkiaFont(SKTypeface typeface, string familyName, float size)` | Initializes a font wrapper with a Skia typeface, a public family name, and a text size. |

## Properties

| Name | Type | Description |
| --- | --- | --- |
| `Typeface` | `SKTypeface` | Gets the Skia typeface used by shaping and rasterization. |
| `FamilyName` | `string` | Gets the font family name exposed through `IDrawFont`. |
| `Size` | `float` | Gets the font size exposed through `IDrawFont`. |

## Exceptions

| API | Exception | Condition |
| --- | --- | --- |
| `SkiaFont(SKTypeface, string, float)` | `ArgumentNullException` | `typeface` or `familyName` is null. |
| `SkiaFont(SKTypeface, string, float)` | `ArgumentException` | `familyName` is empty or whitespace-only. |
| `SkiaFont(SKTypeface, string, float)` | `ArgumentOutOfRangeException` | `size` is zero, negative, not finite, or greater than `16384`. |

## Applies to

Cerneala drawing text pipeline backed by SkiaSharp.

## See also

- [`IDrawFont`](../../UI/Drawing/IDrawFont.cs)
- [`SystemFontSource`](../../UI/Drawing/Text/SystemFontSource.cs)
- [`TextShaper`](../../UI/Drawing/Text/TextShaper.cs)
