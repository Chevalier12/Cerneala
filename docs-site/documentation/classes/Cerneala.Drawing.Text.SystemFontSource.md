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
`SystemFontSource` is the default `IFontSource` used by `MonoGameContentServices` when no custom font source is supplied.

`LoadFont` first resolves the requested family name through `SKFontManager.Default.MatchFamily`. It also accepts common named-weight suffixes such as `SemiBold`, `ExtraBold`, and `Light`; for example, `Cascadia Mono SemiBold` resolves the `Cascadia Mono` family at weight 600. If neither the exact name nor a named-weight form resolves, the implementation falls back to `SKTypeface.Default` and still creates a `SkiaFont` with the requested family name and size.

The type does not expose caching or lifetime management. Each successful `LoadFont` call creates and returns a new `SkiaFont` instance.

## Constructors
| Name | Description |
| --- | --- |
| `SystemFontSource()` | Initializes a new `SystemFontSource` instance. |

## Methods
| Name | Description |
| --- | --- |
| `LoadFont(string familyName, float size)` | Loads a system-backed draw font for the requested family name and text size. |

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

## Applies To
Project: `Cerneala`

## See Also
- `Cerneala.Drawing.IFontSource`
- `Cerneala.Drawing.IDrawFont`
- `Cerneala.Drawing.Text.SkiaFont`
- `Cerneala.UI.Hosting.MonoGame.MonoGameContentServices`
