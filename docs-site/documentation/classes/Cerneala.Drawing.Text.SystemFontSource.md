# SystemFontSource Class

## Definition
Namespace: `Cerneala.Drawing.Text`

Assembly/Project: `Cerneala`

Source: `Drawing/Text/SystemFontSource.cs`

Loads draw fonts from the host system font manager for the Cerneala drawing text pipeline.

```csharp
public sealed class SystemFontSource : IFontSource
```

Inheritance:
`object` -> `SystemFontSource`

Implements:
`IFontSource`

## Examples
```csharp
using Cerneala.Drawing;
using Cerneala.Drawing.Text;

IFontSource fontSource = new SystemFontSource();
IDrawFont font = fontSource.LoadFont("Arial", 16);
```

## Remarks
`SystemFontSource` is the default `IFontSource` used by `DrawingContentServices` when no custom font source is supplied.

`LoadFont` first resolves the requested family name through `SKFontManager.Default.MatchFamily`. It also accepts common named-weight suffixes such as `SemiBold`, `ExtraBold`, and `Light`; for example, `Cascadia Mono SemiBold` resolves the `Cascadia Mono` family at weight 600. If neither the exact name nor a named-weight form resolves, the implementation falls back to `SKTypeface.Default` and still creates a `SkiaFont` with the requested family name and size.

The type exposes no caching or lifetime API, but it caches resolved typefaces in a process-wide static dictionary keyed by family name (case-insensitive). Entries are never removed. Each successful `LoadFont` call creates and returns a new `SkiaFont` instance over the cached typeface; for example, `LoadFont("Arial", 12)` and `LoadFont("arial", 20)` return two different `SkiaFont` objects that share one `SKTypeface`.

The styled overload `LoadFont(familyName, size, weight, style, stretch)` returns the plain `LoadFont(familyName, size)` result when the weight, style, and stretch are all `Normal`; the typeface is then the same instance. Otherwise it asks `SKFontManager.Default.MatchFamily` for the family with the requested weight (the number of the `FontWeight`), width (the number of the `FontStretch`), and slant (upright, italic, or oblique). The system returns the closest installed face; Cerneala does not synthesize bold or italic. If the system does not know the name as a family, the typeface that `LoadFont(familyName, size)` would use (a weighted name such as `Segoe UI Bold`, or the default typeface) supplies the family, and the requested style, stretch, and, unless it is `Normal`, weight apply to that family; a weight already in the name stays when the request leaves the weight at `Normal`. The returned `SkiaFont` still carries the requested family name. Styled typefaces are cached in a second process-wide static dictionary keyed by family name (case-insensitive), weight, style, and stretch, so `LoadFont("Segoe UI", 12, FontWeight.Bold, FontStyle.Italic, FontStretch.Normal)` and `LoadFont("Segoe UI", 20, FontWeight.Bold, FontStyle.Italic, FontStretch.Normal)` share one `SKTypeface`.

## Constructors
| Name | Description |
| --- | --- |
| `SystemFontSource()` | Initializes a new `SystemFontSource` instance. |

## Methods
| Name | Description |
| --- | --- |
| `LoadFont(string familyName, float size)` | Loads a system-backed draw font for the requested family name and text size. |
| `LoadFont(string familyName, float size, FontWeight weight, FontStyle style, FontStretch stretch)` | Loads a system-backed draw font for the requested family, size, weight, style, and stretch. |

## Method Details

### LoadFont
```csharp
public IDrawFont LoadFont(string familyName, float size)
```

Returns an `IDrawFont` backed by a `SkiaFont`.

Parameters:

| Name | Type | Description |
| --- | --- | --- |
| `familyName` | `string` | The system font family name, optionally followed by a supported named-weight suffix such as `SemiBold`, to resolve. |
| `size` | `float` | The text size. Must be positive, finite, and no greater than `16384`. |

Return value:

| Type | Description |
| --- | --- |
| `IDrawFont` | A `SkiaFont` containing the resolved Skia typeface, requested family name, and requested size. |

Exceptions:

| Exception | Condition |
| --- | --- |
| `ArgumentNullException` | `familyName` is `null`. |
| `ArgumentException` | `familyName` is empty or contains only white-space characters. |
| `ArgumentOutOfRangeException` | `size` is not positive, is not finite, or is greater than `16384`. |

### LoadFont with weight, style and stretch
```csharp
public IDrawFont LoadFont(string familyName, float size, FontWeight weight, FontStyle style, FontStretch stretch)
```

Returns an `IDrawFont` backed by a `SkiaFont` over the closest face of the family.

Example:

```csharp
using Cerneala.Drawing;
using Cerneala.Drawing.Text;

SystemFontSource source = new();
IDrawFont bold = source.LoadFont("Segoe UI", 16, FontWeight.Bold, FontStyle.Normal, FontStretch.Normal);
IDrawFont italic = source.LoadFont("Segoe UI", 16, FontWeight.Normal, FontStyle.Italic, FontStretch.Normal);
// When the system has Segoe UI Bold and Segoe UI Italic faces installed, `bold` uses the bold face
// and `italic` uses the italic face. When a face is missing, the closest installed face is used instead.
```

Parameters:

| Name | Type | Description |
| --- | --- | --- |
| `familyName` | `string` | The system font family name. |
| `size` | `float` | The text size. Must be positive, finite, and no greater than `16384`. |
| `weight` | `FontWeight` | The requested weight. |
| `style` | `FontStyle` | The requested slant. |
| `stretch` | `FontStretch` | The requested width. |

Exceptions:

| Exception | Condition |
| --- | --- |
| `ArgumentNullException` | `familyName` is `null`. |
| `ArgumentOutOfRangeException` | `weight`, `style`, or `stretch` is not a defined enum member, or `size` is not positive, not finite, or greater than `16384`. |
| `ArgumentException` | `familyName` is empty or white space, for a request where the weight, style, or stretch is not `Normal`. A request with all three `Normal` follows the rules of `LoadFont(familyName, size)`. |

## Applies To
Project: `Cerneala`

## See Also
- `Cerneala.Drawing.IFontSource`
- `Cerneala.Drawing.IDrawFont`
- `Cerneala.Drawing.Text.SkiaFont`
- `Cerneala.Drawing.FontWeight`
- `Cerneala.Drawing.FontStyle`
- `Cerneala.Drawing.FontStretch`
- `Cerneala.UI.Hosting.DrawingContentServices`
