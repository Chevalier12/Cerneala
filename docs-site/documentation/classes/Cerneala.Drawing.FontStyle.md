# FontStyle Enum

## Definition

Namespace: `Cerneala.Drawing`

Assembly/Project: `Cerneala`

Source: `Drawing/FontStyle.cs`

Selects the slant of a font face: upright, italic, or oblique.

```csharp
public enum FontStyle
```

## Examples

Show italic text in a `TextBlock`, in C#:

```csharp
using Cerneala.Drawing;
using Cerneala.UI.Controls;

TextBlock note = new()
{
    Text = "Draft",
    FontFamily = "Segoe UI",
    FontStyle = FontStyle.Italic
};
```

Show italic text in markup, for the whole `TextBlock` or for one `Run`:

```xml
<TextBlock FontStyle="Italic" Text="Draft"/>

<TextBlock>Status: <Run Text="draft" FontStyle="Italic"/></TextBlock>
```

## Remarks

`FontStyle.Normal` is the default everywhere the value is used. `SystemFontSource` maps `Normal` to an upright slant, `Italic` to an italic slant and `Oblique` to an oblique slant, and asks the system font manager for that face of the family. The system returns the closest installed face. Cerneala does not slant a face by itself: if a family has no italic face, the text is drawn with the closest face that the system returns.

A font that comes from a `FontResourceId` is one exact face, so it ignores `FontStyle`.

Only the three defined values are valid. A value such as `(FontStyle)7` is rejected: the `Control.FontStyle`, `TextElement.FontStyle` and `TextBlock` properties throw `ArgumentException`, and `TextAspect`, `FontResolver` and `SystemFontSource` throw `ArgumentOutOfRangeException`.

In `.crn` markup the value is the member name, matched without regard to case, for example `FontStyle="Italic"`.

## Values

| Name | Value | Description |
| --- | ---: | --- |
| `Normal` | `0` | The upright face. This is the default. |
| `Italic` | `1` | The italic face. |
| `Oblique` | `2` | The oblique face. |

## Applies to

`Control.FontStyle`, `TextElement.FontStyle`, `TextAspect.FontStyle`, `IFontSource.LoadFont`, `FontResolver.Resolve` and `SystemFontSource.LoadFont`.

## See also

- `Cerneala.Drawing.FontWeight`
- `Cerneala.Drawing.FontStretch`
- `Cerneala.Drawing.IFontSource`
- `Cerneala.UI.Controls.Control`
- `Cerneala.UI.Documents.TextElement`
