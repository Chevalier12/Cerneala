# LineBreak Class

## Definition
Namespace: `Cerneala.UI.Documents`

Assembly/Project: `Cerneala`

Source: `UI/Documents/LineBreak.cs`

Represents a line break inside the text of a `TextBlock`.

```csharp
public sealed class LineBreak : Inline
```

Inheritance:
`object` -> `UiObject` -> `TextElement` -> `Inline` -> `LineBreak`

## Examples

Two lines, in C#. `Text` of the `TextBlock` becomes `"one\ntwo"`:

```csharp
using Cerneala.UI.Controls;
using Cerneala.UI.Documents;

TextBlock text = new();
text.Inlines.Add(new Run("one"));
text.Inlines.Add(new LineBreak());
text.Inlines.Add(new Run("two"));
```

The same in markup:

```xml
<TextBlock>one<LineBreak/>two</TextBlock>
```

The text `"one"` is drawn on the first line and `"two"` on the second, below it.

## Remarks

A `LineBreak` contributes one `"\n"` to the text of its `TextBlock`, and so a `TextBlock` whose `Text` is set to `"one\ntwo"` breaks the line at the same place.

A line break ends a paragraph: with `TextAlignment.Justify`, the line before it is not stretched.

In `.crn` markup, white space next to a `LineBreak` is removed, so `<TextBlock>one <LineBreak/> two</TextBlock>` has the text `"one\ntwo"`.

## Constructors

| Name | Description |
| --- | --- |
| `LineBreak()` | Creates a line break. |

## Applies to

Project: `Cerneala`

## See also

- `Cerneala.UI.Documents.Inline`
- `Cerneala.UI.Documents.Run`
- `Cerneala.UI.Controls.TextBlock`
- `Cerneala.UI.Text.TextAlignment`
