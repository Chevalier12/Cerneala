# FontStretch Enum

## Definition

Namespace: `Cerneala.Drawing`

Assembly/Project: `Cerneala`

Source: `Drawing/FontStretch.cs`

Selects how wide or narrow a font face is, from ultra condensed to ultra expanded.

```csharp
public enum FontStretch
```

## Examples

Ask for a condensed face in C#:

```csharp
using Cerneala.Drawing;
using Cerneala.UI.Controls;

TextBlock label = new()
{
    Text = "Narrow",
    FontFamily = "Segoe UI",
    FontStretch = FontStretch.Condensed
};
```

Ask a font source for the condensed face directly:

```csharp
using Cerneala.Drawing;
using Cerneala.Drawing.Text;

IFontSource source = new SystemFontSource();
IDrawFont condensed = source.LoadFont("Segoe UI", 16, FontWeight.Normal, FontStyle.Normal, FontStretch.Condensed);
```

## Remarks

`FontStretch.Normal` (5) is the default everywhere the value is used. The number of a member is the width class that `SystemFontSource` passes to the system font manager. The system returns the closest installed face of the family; when a family has no condensed or expanded face, the text is drawn with the closest face that the system returns. Cerneala does not squeeze or widen a face by itself.

A font that comes from a `FontResourceId` is one exact face, so it ignores `FontStretch`.

Only the nine defined values are valid. A value such as `(FontStretch)0` is rejected: the `Control.FontStretch`, `TextElement.FontStretch` and `TextBlock` properties throw `ArgumentException`, and `TextAspect`, `FontResolver` and `SystemFontSource` throw `ArgumentOutOfRangeException`.

## Values

| Name | Value | Description |
| --- | ---: | --- |
| `UltraCondensed` | `1` | The narrowest width. |
| `ExtraCondensed` | `2` | Extra condensed. |
| `Condensed` | `3` | Condensed. |
| `SemiCondensed` | `4` | Semi condensed. |
| `Normal` | `5` | The regular width. This is the default. |
| `SemiExpanded` | `6` | Semi expanded. |
| `Expanded` | `7` | Expanded. |
| `ExtraExpanded` | `8` | Extra expanded. |
| `UltraExpanded` | `9` | The widest width. |

## Applies to

`Control.FontStretch`, `TextElement.FontStretch`, `TextAspect.FontStretch`, `IFontSource.LoadFont`, `FontResolver.Resolve` and `SystemFontSource.LoadFont`.

## See also

- `Cerneala.Drawing.FontWeight`
- `Cerneala.Drawing.FontStyle`
- `Cerneala.Drawing.IFontSource`
- `Cerneala.UI.Controls.Control`
- `Cerneala.UI.Documents.TextElement`
