# SystemFontSource Class

## Definition
Namespace: `Cerneala.Drawing.Text`

Assembly/Project: `Cerneala`

Source: `UI/Drawing/Text/SystemFontSource.cs`

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

`LoadFont` resolves the requested family name through `SKFontManager.Default.MatchFamily`. If the system font manager does not return a matching typeface, the implementation falls back to `SKTypeface.Default` and still creates a `SkiaFont` with the requested family name and size.

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
| `familyName` | `string` | The system font family name to resolve. |
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
