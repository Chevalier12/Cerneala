# FontWeight Enum

## Definition

Namespace: `Cerneala.Drawing`

Assembly/Project: `Cerneala`

Source: `Drawing/FontWeight.cs`

Selects how heavy the strokes of a font face are. The numeric values are the OpenType weight classes.

```csharp
public enum FontWeight
```

## Examples

Show bold text in a `TextBlock`, in C#:

```csharp
using Cerneala.Drawing;
using Cerneala.UI.Controls;

TextBlock title = new()
{
    Text = "Settings",
    FontFamily = "Segoe UI",
    FontWeight = FontWeight.Bold
};
```

Show bold text in markup:

```xml
<TextBlock FontWeight="Bold" Text="Settings"/>
```

Ask a font source for the bold face directly:

```csharp
using Cerneala.Drawing;
using Cerneala.Drawing.Text;

IFontSource source = new SystemFontSource();
IDrawFont bold = source.LoadFont("Segoe UI", 16, FontWeight.Bold, FontStyle.Normal, FontStretch.Normal);
```

## Remarks

`FontWeight.Normal` (400) is the default everywhere the value is used. The number of a member is the weight that `SystemFontSource` passes to the system font manager, so `FontWeight.Bold` asks for weight 700. The system returns the closest installed face of the family. Cerneala does not make a face bolder by itself: if a family has no bold face, the text is drawn with the closest face that the system returns.

A font that comes from a `FontResourceId` is one exact face, so it ignores `FontWeight`.

Only the nine defined values are valid. A value such as `(FontWeight)450` is rejected: the `Control.FontWeight`, `TextElement.FontWeight` and `TextBlock` properties throw `ArgumentException`, and `TextAspect`, `FontResolver` and `SystemFontSource` throw `ArgumentOutOfRangeException`.

In `.crn` markup the value is the member name, matched without regard to case, for example `FontWeight="Bold"`.

## Values

| Name | Value | Description |
| --- | ---: | --- |
| `Thin` | `100` | The thinnest weight. |
| `ExtraLight` | `200` | Extra light. |
| `Light` | `300` | Light. |
| `Normal` | `400` | The regular weight. This is the default. |
| `Medium` | `500` | Medium. |
| `SemiBold` | `600` | Semi bold. |
| `Bold` | `700` | Bold. |
| `ExtraBold` | `800` | Extra bold. |
| `Black` | `900` | The heaviest weight. |

## Applies to

`Control.FontWeight`, `TextElement.FontWeight`, `TextAspect.FontWeight`, `IFontSource.LoadFont`, `FontResolver.Resolve` and `SystemFontSource.LoadFont`.

## See also

- `Cerneala.Drawing.FontStyle`
- `Cerneala.Drawing.FontStretch`
- `Cerneala.Drawing.IFontSource`
- `Cerneala.UI.Controls.Control`
- `Cerneala.UI.Documents.TextElement`
