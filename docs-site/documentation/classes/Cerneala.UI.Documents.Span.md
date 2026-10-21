# Span Class

## Definition
Namespace: `Cerneala.UI.Documents`

Assembly/Project: `Cerneala`

Source: `UI/Documents/Span.cs`

Groups other inlines and gives them a shared style: font properties, foreground, and text decorations.

```csharp
public class Span : Inline
```

Inheritance:
`object` -> `UiObject` -> `TextElement` -> `Inline` -> `Span`

Derived:
`Bold`, `Italic`

## Examples

Color a group of runs red and underline them, in C#:

```csharp
using Cerneala.Drawing;
using Cerneala.UI.Controls;
using Cerneala.UI.Documents;
using Cerneala.UI.Media;
using Cerneala.UI.Text;

TextBlock text = new();
text.Inlines.Add(new Run("Warning: "));

Span warning = new()
{
    Foreground = new SolidColorBrush(Color.Red),
    TextDecorations = TextDecorations.Underline
};
warning.Inlines.Add(new Run("disk almost full"));
text.Inlines.Add(warning);
// text.Text is "Warning: disk almost full"
```

The same in markup:

```xml
<TextBlock>Warning: <Span Foreground="Tomato" TextDecorations="Underline"><Run>disk almost full</Run></Span></TextBlock>
```

## Remarks

A `Span` has no text of its own. Its text is the text of the inlines in `Inlines`, in order. Spans can be nested: an inline takes each property that it does not set from the nearest `Span` above it that sets it, and then from the `TextBlock`.

The text decorations of a `Span` are added to the decorations of the inlines inside it.

Changing `Inlines` (add, replace, remove, clear) invalidates the owning `TextBlock` and sets its `Text`. This works at any depth, for example `span.Inlines.RemoveAt(0)` on a `Span` that is in `textBlock.Inlines` updates `textBlock.Text`. While the `Span` is not in a `TextBlock`, its changes only change the collection.

A `Span` cannot hold itself or a `Span` above it, and a child can have only one parent. See `InlineCollection`.

## Constructors

| Name | Description |
| --- | --- |
| `Span()` | Creates an empty span. |
| `Span(Inline childInline)` | Creates a span that holds `childInline`. Throws `ArgumentNullException` when it is `null`, and `InvalidOperationException` when the inline already has a parent. |

## Properties

| Name | Type | Description |
| --- | --- | --- |
| `Inlines` | `InlineCollection` | Gets the inlines of the span. |

## Applies to

Project: `Cerneala`

## See also

- `Cerneala.UI.Documents.Inline`
- `Cerneala.UI.Documents.Bold`
- `Cerneala.UI.Documents.Italic`
- `Cerneala.UI.Documents.InlineCollection`
- `Cerneala.UI.Controls.TextBlock`
