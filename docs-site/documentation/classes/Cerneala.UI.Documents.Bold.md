# Bold Class

## Definition
Namespace: `Cerneala.UI.Documents`

Assembly/Project: `Cerneala`

Source: `UI/Documents/Bold.cs`

Represents a `Span` whose text is bold.

```csharp
public sealed class Bold : Span
```

Inheritance:
`object` -> `UiObject` -> `TextElement` -> `Inline` -> `Span` -> `Bold`

## Examples

Bold one word, in C#. `Text` of the `TextBlock` becomes `"Hello world"`:

```csharp
using Cerneala.UI.Controls;
using Cerneala.UI.Documents;

TextBlock text = new();
text.Inlines.Add(new Run("Hello "));
text.Inlines.Add(new Bold(new Run("world")));
```

The same in markup:

```xml
<TextBlock>Hello <Bold>world</Bold></TextBlock>
```

An explicit weight on the `Bold` wins over its default:

```csharp
using Cerneala.Drawing;
using Cerneala.UI.Documents;

Bold bold = new();
// bold.FontWeight is FontWeight.Bold

bold.FontWeight = FontWeight.Normal;
// bold.FontWeight is FontWeight.Normal
```

## Remarks

`Bold` is a `Span` with a framework default of `FontWeight.Bold` for `FontWeight`. An inline inside can set its own weight: a `Run` in a `Bold` with `FontWeight = FontWeight.Normal` is drawn with the normal weight. The weight on the `Bold` itself replaces the default too.

`Bold` sets only the weight. The family, size and the other properties come from the `TextBlock` or from the `Span` above, as for any `Span`. With the default `FontFamily` `"Default"`, the text is drawn with the bold face of the default family.

The weight selects the closest real face that the font source returns; no bold is synthesized. See `FontWeight`.

## Constructors

| Name | Description |
| --- | --- |
| `Bold()` | Creates an empty bold span. |
| `Bold(Inline childInline)` | Creates a bold span that holds `childInline`. Throws `ArgumentNullException` when it is `null`. |

## Applies to

Project: `Cerneala`

## See also

- `Cerneala.UI.Documents.Span`
- `Cerneala.UI.Documents.Italic`
- `Cerneala.Drawing.FontWeight`
- `Cerneala.UI.Controls.TextBlock`
