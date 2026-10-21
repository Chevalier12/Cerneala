# Run Class

## Definition
Namespace: `Cerneala.UI.Documents`

Assembly/Project: `Cerneala`

Source: `UI/Documents/Run.cs`

Represents a piece of text with one style, inside the `Inlines` of a `TextBlock` or of a `Span`.

```csharp
public sealed class Run : Inline
```

Inheritance:
`object` -> `UiObject` -> `TextElement` -> `Inline` -> `Run`

## Examples

Build the content in C#. `Text` of the `TextBlock` becomes `"Hello world"`:

```csharp
using Cerneala.Drawing;
using Cerneala.UI.Controls;
using Cerneala.UI.Documents;

TextBlock text = new();
text.Inlines.Add(new Run("Hello "));
text.Inlines.Add(new Run("world") { FontStyle = FontStyle.Italic });
```

The same in markup. A `Run` takes its text from the `Text` attribute or from the content of the element:

```xml
<TextBlock>Hello <Run Text="world" FontStyle="Italic"/></TextBlock>

<TextBlock><Run>abc</Run></TextBlock>
```

The first `TextBlock` has the text `"Hello world"`, the second `"abc"`.

A run changes later, and the `TextBlock` follows:

```csharp
Run run = new("a");
TextBlock text = new();
text.Inlines.Add(run);
text.Inlines.Add(new Bold(new Run("b")));

run.Text = "aaaaaaaa";
// text.Text is now "aaaaaaaab", and the TextBlock measures and renders again.

run.Text = null!;
// run.Text is string.Empty.
```

## Remarks

A `Run` is the only inline that holds text. It uses the text properties of `TextElement` (font family, size, weight, style, stretch, foreground) and `Inline.TextDecorations`. What it does not set, it takes from its parent `Span` or from the `TextBlock`.

`Text` never holds `null`: assigning `null` stores `string.Empty`. A change of `Text` invalidates measure, render and semantics of the owning `TextBlock` and sets `TextBlock.Text` to the new concatenated text of all inlines.

Setting `TextBlock.Text` replaces all inlines with one new `Run` that holds the text (or with no inline when the text is empty). A `Run` that was in the `Inlines` is detached by that and can be added again.

In `.crn` markup, whitespace in the content of a `Run` follows WPF, with the rules applied once over the whole content of the `TextBlock` (see `TextBlock`): a run of white space becomes one space, white space at the start and at the end of the whole `TextBlock` content is removed, and white space next to a `LineBreak` is removed. So `<TextBlock>x<Run> abc </Run>y</TextBlock>` keeps the spaces and has the text `"x abc y"`, while `<TextBlock><Run>  a   b  </Run></TextBlock>` has the text `"a b"`, and `<TextBlock>one<LineBreak/>two</TextBlock>` has the text `"one\ntwo"`.

## Constructors

| Name | Description |
| --- | --- |
| `Run()` | Creates a run with empty text. |
| `Run(string text)` | Creates a run with the given text. `null` becomes `string.Empty`. |

## Fields

| Name | Type | Description |
| --- | --- | --- |
| `TextProperty` | `UiProperty<string>` | Identifies `Text`. Defaults to `string.Empty`, coerces `null` to empty, and affects measure and render. |

## Properties

| Name | Type | Description |
| --- | --- | --- |
| `Text` | `string` | Gets or sets the text of the run. |

## Applies to

Project: `Cerneala`

## See also

- `Cerneala.UI.Documents.Inline`
- `Cerneala.UI.Documents.Span`
- `Cerneala.UI.Documents.LineBreak`
- `Cerneala.UI.Documents.InlineCollection`
- `Cerneala.UI.Controls.TextBlock`
