# TextBlock Class

## Definition
Namespace: `Cerneala.UI.Controls`

Assembly/Project: `Cerneala`

Source: `UI/Controls/TextBlock.cs`

Displays text as one paragraph, either a plain string or inline content (`Run`, `Span`, `Bold`, `Italic`, `LineBreak`), measures it through the paragraph layout engine, and renders it with the font, foreground, alignment, decoration, and line settings of the control and of its inlines.

```csharp
public class TextBlock : Control
```

Inheritance:
`object` -> `UiObject` -> `UIElement` -> `Control` -> `TextBlock`

## Examples

```csharp
using Cerneala.Drawing;
using Cerneala.UI.Controls;
using Cerneala.UI.Layout;
using Cerneala.UI.Media;
using Cerneala.UI.Text;

TextBlock label = new()
{
    Text = "Ready",
    FontFamily = "Default",
    FontSize = 18,
    Foreground = new SolidColorBrush(Color.White),
    TextWrapping = TextWrapping.NoWrap,
    TextTrimming = TextTrimming.WordEllipsis,
    Width = 200
};

LayoutSize desired = label.Measure(new MeasureContext(new LayoutSize(200, 40)));
```

Style the whole block: centered, bold, italic, underlined and struck through, at most 2 lines of 24 px, wrapping at spaces:

```csharp
using Cerneala.Drawing;
using Cerneala.UI.Controls;
using Cerneala.UI.Text;

TextBlock note = new()
{
    Text = "A long note that needs more than one line to show all of its words.",
    TextAlignment = TextAlignment.Center,
    FontWeight = FontWeight.Bold,
    FontStyle = FontStyle.Italic,
    TextDecorations = TextDecorations.Underline | TextDecorations.Strikethrough,
    MaxLines = 2,
    LineHeight = 24,
    LineStackingStrategy = LineStackingStrategy.BlockLineHeight,
    TextWrapping = TextWrapping.WrapWithOverflow,
    Width = 200
};
```

The same in markup:

```xml
<TextBlock TextAlignment="Center" FontWeight="Bold" FontStyle="Italic" TextDecorations="Underline, Strikethrough" MaxLines="2" LineHeight="24" LineStackingStrategy="BlockLineHeight" TextWrapping="WrapWithOverflow" Text="A long note that needs more than one line to show all of its words."/>
```

Mix styles inside one paragraph with inlines. `Text` becomes `"Hello world"`:

```csharp
using Cerneala.UI.Controls;
using Cerneala.UI.Documents;

TextBlock greeting = new();
greeting.Inlines.Add(new Run("Hello "));
greeting.Inlines.Add(new Bold(new Run("world")));
```

```xml
<TextBlock>Hello <Bold>world</Bold></TextBlock>
```

Other inline shapes in markup:

```xml
<TextBlock>one<LineBreak/>two</TextBlock>
<TextBlock><Italic>slanted</Italic></TextBlock>
<TextBlock><Span Foreground="Tomato"><Run>colored text</Run></Span></TextBlock>
<TextBlock><Run Text="x" FontStyle="Italic"/></TextBlock>
```

The first has the text `"one\ntwo"` and draws two lines.

## Remarks

`TextBlock` builds a `TextAspect` from `FontFamily`, `FontSize`, `FontWeight`, `FontStyle`, `FontStretch`, `Foreground`, `TextWrapping`, `TextTrimming`, and `FontResourceId`. It builds a `TextParagraphOptions` from `TextAlignment`, `MaxLines`, `LineHeight`, and `LineStackingStrategy`. It turns its inline tree into a list of `TextSpan` values, one for each piece of text, depth first, each with the style that its `Run`, `Span`, `Bold`, and `Italic` ancestors give it; a block with no text gets one empty span, so an empty `TextBlock` still measures one line. During measurement it passes the spans, the aspect, the options, and the available layout bounds to `TextMeasurer.MeasureParagraph`; during rendering it passes them to `TextRenderer.RenderParagraph`, which draws the visible lines at the render bounds origin. The desired size is `TextParagraphLayout.Size`.

### Text and Inlines

As in WPF, `Text` and `Inlines` are two views of the same content:

- Setting `Text` replaces all inlines with one `Run` that holds the text, or with no inline when the text is empty. For example, after `Inlines.Add(new Bold(new Run("old")))`, setting `Text = "Hello"` leaves one `Run` with the text `"Hello"`.
- Changing the inlines sets `Text` to the concatenated text of all inlines. A `LineBreak` is `"\n"`. For example `Run("ab")`, `LineBreak`, `Run("c")` gives `Text` equal to `"ab\nc"`. This also happens when the text of a `Run` changes, or when an inline under a `Span` is added or removed.
- A change of the inlines or of the text of a `Run` invalidates measure, render, and semantics. A change of the font properties of an inline invalidates measure and render. A change of the `Foreground` or the `TextDecorations` of an inline invalidates render only, and the text is not changed.

In `.crn` markup, the content of a `TextBlock` element is its inlines, and whitespace follows WPF. The rules apply once to the whole content of the `TextBlock`, across the `Run`, `Span`, `Bold`, and `Italic` elements inside it: a run of white space becomes one space, also when it crosses an element boundary; white space at the start and the end of the whole content is removed; and white space next to a `LineBreak` is removed. For example, `<TextBlock>Click <Bold> here </Bold>now</TextBlock>` has the text `"Click here now"`, and `<TextBlock><Bold>a</Bold> <Italic>b</Italic></TextBlock>` has the text `"a b"`. The value of a `Text` attribute is never changed: `<TextBlock><Run Text="  a  "/></TextBlock>` has the text `"  a  "`. A `TextBlock` with only text and no inline element sets `Text` directly.

Markup limits: an attribute of an inline element cannot use a binding or an application resource reference (`$...`); the generator reports `CERNEALAUI007`. Inline elements cannot be placed inside directive content (`@if`, `@when`, `@for`), and an inline element cannot hold directives; the generator reports `CERNEALAUI006`. An inline element outside a `TextBlock` or a `Span` reports `CERNEALAUI003`.

An inline takes each style property that it does not set from the nearest `Span` above it, and then from the `TextBlock`. Text decorations are different: the decorations of the `TextBlock`, of the `Span` above, and of the inline add up, and an explicit `None` does not remove an inherited decoration.

### Font weight, style and stretch

`FontWeight`, `FontStyle`, and `FontStretch` choose the face of the font family. The system returns the closest real face of the family; Cerneala never makes text bold or slanted by itself, so a family without a bold face is drawn with the closest face it has. A `FontResourceId` font is one exact face and ignores the three properties. With the default family `"Default"`, bold and italic also select the bold and italic faces. The three properties inherit through the UI property system, like `FontFamily` and `FontSize`.

### Alignment, line height, and line limit

`TextAlignment` places the lines inside the width that the block is arranged at, and it does not change the measured width. `Justify` does not stretch the last line of a paragraph, that is the line that ends the text or the line before a hard line break. `LineHeight` is `NaN` by default, which keeps the height of the content of each line; `LineStackingStrategy` says whether a given `LineHeight` is a lower limit (`MaxHeight`) or the exact height (`BlockLineHeight`). `MaxLines` limits the number of lines that are measured and drawn; `0` shows all lines.

`TextWrapping.WrapWithOverflow` breaks at the same places as `Wrap`, but a word that is wider than the available width stays whole on its line and the line overflows. The measured width can then be larger than the available width. For example, the single word `"Supercalifragilisticexpialidocious"` at an available width of 60 is split into several lines with `Wrap`, and stays one line wider than 60 with `WrapWithOverflow`.

### Decorations

`TextDecorations` draws a rectangle for each flag over the text of each span, in the foreground brush of the span. They do not change the layout, so changing them invalidates only render. A span without a foreground brush (`Foreground` of `null`) draws neither text nor decorations.

### Resources, trimming, and invalidation

The `Text` value is never stored as `null`. Assigning `null` through the property or `TextProperty` is coerced to `string.Empty`. Empty text still measures through the text measurer, but `OnRender` exits without emitting a text draw command.

By default the control uses `TextMeasurer.Default` and `TextRenderer.Default`. Supplying `FontResourceId` together with a local `ResourceProvider` or root resource provider makes the control use a resource-backed `FontResolver` and an internal text layout cache. When a font resource dependency is recorded through `ResourceDependencyTracker` or the root tracker, resource changes invalidate measure and render.

`TextWrapping.Wrap` allows measurement to respect the available width. `TextTrimming.CharacterEllipsis` collapses overflow at Unicode text-element boundaries, while `TextTrimming.WordEllipsis` prefers complete words and falls back to character trimming for an oversized first word. Both modes append `U+2026`, respect finite width, and collapse the last visible line when finite height hides later lines. `TextTrimming.None` preserves the uncollapsed text layout.

Changing text, wrapping, alignment, line height, line stacking strategy, maximum lines, font weight, style or stretch, font resource, resource provider, or text measurer invalidates measure and render. Changing `TextDecorations` invalidates render only. Changing the text renderer invalidates render only. Changing inherited `Foreground` invalidates render without changing the text layout identity.

## Constructors

| Name | Description |
| --- | --- |
| `TextBlock()` | Initializes a new `TextBlock` with default control and text property values. |

## Fields

| Name | Type | Description |
| --- | --- | --- |
| `TextProperty` | `UiProperty<string>` | Identifies the `Text` property. Defaults to `string.Empty`, coerces `null` to empty text, and affects measure, render, and semantics. |
| `TextWrappingProperty` | `UiProperty<TextWrapping>` | Identifies the `TextWrapping` property. Defaults to `TextWrapping.NoWrap`, validates enum values, and affects measure and render. |
| `TextTrimmingProperty` | `UiProperty<TextTrimming>` | Identifies the `TextTrimming` property. Defaults to `TextTrimming.None`, validates enum values, and affects measure and render. |
| `TextAlignmentProperty` | `UiProperty<TextAlignment>` | Identifies the `TextAlignment` property. Defaults to `TextAlignment.Left`, validates enum values, and affects measure and render. |
| `TextDecorationsProperty` | `UiProperty<TextDecorations>` | Identifies the `TextDecorations` property. Defaults to `TextDecorations.None`, rejects bits that are not defined flags, and affects render only. |
| `LineHeightProperty` | `UiProperty<float>` | Identifies the `LineHeight` property. Defaults to `float.NaN` (the height of the line content), accepts `NaN` or a finite value greater than zero, and affects measure and render. |
| `LineStackingStrategyProperty` | `UiProperty<LineStackingStrategy>` | Identifies the `LineStackingStrategy` property. Defaults to `LineStackingStrategy.MaxHeight`, validates enum values, and affects measure and render. |
| `MaxLinesProperty` | `UiProperty<int>` | Identifies the `MaxLines` property. Defaults to `0` (all lines), rejects negative values, and affects measure and render. |

## Properties

| Name | Type | Description |
| --- | --- | --- |
| `Text` | `string` | Gets or sets the displayed text. `null` assignments become `string.Empty`. |
| `Inlines` | `InlineCollection` | Gets the inline content. It is two views of the same content as `Text`: setting `Text` replaces the inlines with one `Run`, and changing the inlines sets `Text` to their concatenated text (`LineBreak` is `"\n"`). |
| `TextAlignment` | `TextAlignment` | Gets or sets how lines are placed horizontally: `Left` (default), `Right`, `Center`, or `Justify`. |
| `TextDecorations` | `TextDecorations` | Gets or sets the flags `Underline`, `Strikethrough`, `Overline`, and `Baseline` drawn over all the text. Inline decorations are added to it. |
| `LineHeight` | `float` | Gets or sets the height of each line box. `float.NaN` (default) keeps the height of the content. Zero, negative, and infinite values throw `ArgumentException`. |
| `LineStackingStrategy` | `LineStackingStrategy` | Gets or sets whether `LineHeight` is a lower limit (`MaxHeight`, default) or the exact line height (`BlockLineHeight`). |
| `MaxLines` | `int` | Gets or sets the most lines that are shown. `0` (default) shows all lines. A negative value throws `ArgumentException`. |
| `TextWrapping` | `TextWrapping` | Gets or sets whether text is measured as `NoWrap`, `Wrap`, or `WrapWithOverflow`. |
| `TextTrimming` | `TextTrimming` | Gets or sets `None`, `CharacterEllipsis`, or `WordEllipsis` overflow behavior. |
| `TextMeasurer` | `TextMeasurer` | Gets or sets the measurer used when no font resource provider path is active. Setting `null` throws `ArgumentNullException`; changing the instance invalidates measure and render. |
| `TextRenderer` | `TextRenderer` | Gets or sets the renderer used when no font resource provider path is active. Setting `null` throws `ArgumentNullException`; changing the instance invalidates render. |
| `FontResourceId` | `ResourceId<FontResource>?` | Gets or sets the optional font resource id used for resource-backed measurement and rendering. Changing it clears the internal resource text layout cache and invalidates measure and render. |
| `ResourceProvider` | `IResourceProvider?` | Gets or sets the optional local resource provider. If unset, the control can use `Root.ResourceProvider`. Changing it clears the internal resource text layout cache and invalidates measure and render. |
| `ResourceDependencyTracker` | `ResourceDependencyTracker?` | Gets or sets the optional local dependency tracker. If unset, the control can use `Root.ResourceDependencyTracker`. |

## Relevant Inherited Properties

| Name | Declared On | Description |
| --- | --- | --- |
| `FontFamily` | `Control` | Inherited text font family. Defaults to `"Default"`, inherits through the UI property system, and affects measure and render. |
| `FontSize` | `Control` | Inherited text size. Defaults to `16`, must be positive and finite, and affects measure and render. |
| `FontWeight` | `Control` | Inherited font weight. Defaults to `FontWeight.Normal` and affects measure and render. Selects the closest real face of the family. |
| `FontStyle` | `Control` | Inherited font style. Defaults to `FontStyle.Normal` and affects measure and render. Selects the closest real face of the family. |
| `FontStretch` | `Control` | Inherited font stretch. Defaults to `FontStretch.Normal` and affects measure and render. Selects the closest real face of the family. |
| `Foreground` | `Control` | Inherited foreground brush. Defaults to a solid brush with `Color.Black`, inherits through the UI property system, and affects render. |

## Methods

This class does not declare public methods.

## Events

This class does not declare public events.

## Applies to

`Cerneala.UI.Controls.TextBlock` in the `Cerneala` project.

## See also

- `Cerneala.UI.Controls.Control`
- `Cerneala.UI.Text.TextMeasurer`
- `Cerneala.UI.Text.TextRenderer`
- `Cerneala.UI.Text.TextWrapping`
- `Cerneala.UI.Text.TextTrimming`
- `Cerneala.UI.Text.TextAlignment`
- `Cerneala.UI.Text.TextDecorations`
- `Cerneala.UI.Text.LineStackingStrategy`
- `Cerneala.UI.Text.TextSpan`
- `Cerneala.UI.Text.TextParagraphOptions`
- `Cerneala.UI.Text.TextParagraphLayout`
- `Cerneala.UI.Documents.InlineCollection`
- `Cerneala.UI.Documents.Run`
- `Cerneala.UI.Documents.Span`
