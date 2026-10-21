# Italic Class

## Definition
Namespace: `Cerneala.UI.Documents`

Assembly/Project: `Cerneala`

Source: `UI/Documents/Italic.cs`

Represents a `Span` whose text is italic.

```csharp
public sealed class Italic : Span
```

Inheritance:
`object` -> `UiObject` -> `TextElement` -> `Inline` -> `Span` -> `Italic`

## Examples

Italicize one word, in C#. `Text` of the `TextBlock` becomes `"Hello world"`:

```csharp
using Cerneala.UI.Controls;
using Cerneala.UI.Documents;

TextBlock text = new();
text.Inlines.Add(new Run("Hello "));
text.Inlines.Add(new Italic(new Run("world")));
```

The same in markup:

```xml
<TextBlock>Hello <Italic>world</Italic></TextBlock>
```

Combine `Bold` and `Italic`:

```csharp
using Cerneala.UI.Documents;

Bold boldItalic = new(new Italic(new Run("both")));
// The run is drawn with FontWeight.Bold and FontStyle.Italic.
```

## Remarks

`Italic` is a `Span` with a framework default of `FontStyle.Italic` for `FontStyle`. An inline inside can set its own style, and an explicit `FontStyle` on the `Italic` itself replaces the default.

`Italic` sets only the style. The family, size and the other properties come from the `TextBlock` or from the `Span` above.

The style selects the closest real face that the font source returns; no slant is synthesized. See `FontStyle`.

## Constructors

| Name | Description |
| --- | --- |
| `Italic()` | Creates an empty italic span. |
| `Italic(Inline childInline)` | Creates an italic span that holds `childInline`. Throws `ArgumentNullException` when it is `null`. |

## Applies to

Project: `Cerneala`

## See also

- `Cerneala.UI.Documents.Span`
- `Cerneala.UI.Documents.Bold`
- `Cerneala.Drawing.FontStyle`
- `Cerneala.UI.Controls.TextBlock`
