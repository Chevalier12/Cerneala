# TextMeasurer Class

## Definition
Namespace: `Cerneala.UI.Text`

Assembly/Project: `Cerneala`

Source: `UI/Text/TextMeasurer.cs`

Measures text for layout by resolving a font, breaking text into lines, and caching the resulting layout measurement.

```csharp
public class TextMeasurer
```

Inheritance:
`object` -> `TextMeasurer`

## Examples

Measure wrapped text with the shared default measurer:

```csharp
using Cerneala.UI.Text;
using Cerneala.UI.Layout;

TextAspect aspect = new(
    "Arial",
    14,
    TextWrapping.Wrap,
    TextTrimming.WordEllipsis);
TextMeasureResult result = TextMeasurer.Default.Measure(
    "A short line of text for layout.",
    aspect,
    new LayoutSize(160, 40));

float desiredWidth = result.Size.Width;
float desiredHeight = result.Size.Height;
int lineCount = result.LineCount;
```

Use an explicit layout cache when the caller needs to inspect or clear cached measurements:

```csharp
using Cerneala.UI.Text;

TextLayoutCache cache = new();
TextMeasurer measurer = new(FontResolver.Default, LineBreakService.Default, cache);

TextAspect aspect = new("Arial", 12);
TextMeasureResult first = measurer.Measure("Cached text", aspect, float.PositiveInfinity);
TextMeasureResult second = measurer.Measure("Cached text", aspect, float.PositiveInfinity);

int hits = cache.Hits;
int misses = cache.Misses;
```

## Remarks

`TextMeasurer` is the layout-facing text measurement service used by controls such as text and button surfaces. It delegates font lookup to `FontResolver`, line creation to `LineBreakService`, and stores `TextMeasureResult` instances in a `TextLayoutCache`.

`Measure` normalizes the supplied width before it builds the cache key. `TextWrapping.NoWrap` with trimming `None` always uses an infinite width, whatever width is supplied; either ellipsis mode retains a finite width so overflowing no-wrap lines can be collapsed. Otherwise positive infinity remains unconstrained, while non-positive or `NaN` widths become `0`.

The `LayoutSize` overload also applies finite height, but only when an ellipsis mode is active and the text has more than one line. Then, when all lines do not fit, the result retains at least the first line and collapses the final visible line. A single line taller than the available height, or any text with trimming `None`, is returned unchanged. The width-only overload is equivalent to an unconstrained height.

The cache key includes the text, resolved font identity, font size, wrapping mode, normalized wrapping width, trimming mode, scale, and the effective visible-line count. The returned result contains the measured `LayoutSize`, visible line count, cache key, resolved font identity, and the exact lines to render. Height is based on measured line height when the text shaper can provide it, otherwise on `FontSize * Scale`.

Calls to `Measure` lock around cache access for the `TextMeasurer` instance. The exposed `LayoutCache` object can still be accessed directly by callers, so direct cache operations should be coordinated by the caller when shared across threads.

`MeasureParagraph` lays out a list of `TextSpan` values as one paragraph through the Drawing text layout engine (`DrawTextLayout`). It supports several fonts and sizes on one line, alignment, a line limit, a line height, and `TextWrapping.WrapWithOverflow`. This is the path of `TextBlock`. The paragraph layouts are cached in the same `TextLayoutCache` as the results of `Measure`, but in their own store, so the existing `Measure` overloads and their results are unchanged.

The key of a paragraph layout holds the wrapping, trimming, scale, normalized width and height, the options, and, for every span, its text, the identity of its resolved font, and its font size. The brush, the foreground, and the decorations of a span are not in the key, so paragraphs that differ only in them return the same `TextParagraphLayout` instance. A different weight, style, stretch, family, size, or a different split of the same text into spans gives a different layout.

`MeasureParagraph` normalizes the width before it builds the key. When the aspect is `NoWrap`, has no trimming, and the alignment is `Left`, the width is not used and every available width shares one layout. In every other case a positive finite width is kept, because it is the width the lines wrap, align, or trim to; positive infinity stays unconstrained, and a width that is not positive or is `NaN` becomes `0`. The available height is used only when trimming is not `None`; the paragraph keeps at least its first line, also when the height is smaller than one line. Without trimming, any height gives the same layout.

`TextMeasurer` does not render text. It prepares measurement data for layout and rendering code.

## Constructors

| Name | Description |
| --- | --- |
| `TextMeasurer()` | Creates a measurer that uses `FontResolver.Default`, `LineBreakService.Default`, and a new `TextLayoutCache`. |
| `TextMeasurer(FontResolver fontResolver, LineBreakService lineBreakService, TextLayoutCache layoutCache)` | Creates a measurer with explicit font resolution, line breaking, and cache dependencies. Throws `ArgumentNullException` for any `null` dependency. |

## Properties

| Name | Type | Description |
| --- | --- | --- |
| `Default` | `TextMeasurer` | Gets the shared default text measurer instance. |
| `LayoutCache` | `TextLayoutCache` | Gets the cache used by this measurer. |

## Methods

| Name | Return Type | Description |
| --- | --- | --- |
| `Measure(string text, TextAspect aspect, float availableWidth)` | `TextMeasureResult` | Measures text for the supplied text aspect and available width, using the layout cache when an equivalent measurement already exists. |
| `Measure(string text, TextAspect aspect, LayoutSize availableSize)` | `TextMeasureResult` | Measures text against width and height, including horizontal and final-visible-line ellipsis behavior. |
| `MeasureParagraph(IReadOnlyList<TextSpan> spans, TextAspect paragraphAspect, TextParagraphOptions options, LayoutSize availableSize)` | `TextParagraphLayout` | Lays out the spans as one paragraph, with alignment, line limit, line height and mixed fonts, and caches the layout. |

## Method Details

### Measure

```csharp
public virtual TextMeasureResult Measure(string text, TextAspect aspect, float availableWidth)
```

#### Parameters

| Name | Type | Description |
| --- | --- | --- |
| `text` | `string` | The text to measure. Cannot be `null`. |
| `aspect` | `TextAspect` | Font, wrapping, trimming, scale, and optional font resource information used for measurement. `Foreground` does not affect measurement. |
| `availableWidth` | `float` | The available width used for wrapping or ellipsis trimming. The overload leaves height unconstrained. |

#### Returns

`TextMeasureResult`

A measurement containing the requested size, line count, cache key, resolved font identity, and measured lines.

#### Exceptions

| Exception | Condition |
| --- | --- |
| `ArgumentNullException` | `text` or `aspect` is `null`. |
| `InvalidOperationException` | Font resolution requires a font resource provider, but the configured `FontResolver` cannot resolve font resources. |

### Measure with layout bounds

```csharp
public virtual TextMeasureResult Measure(string text, TextAspect aspect, LayoutSize availableSize)
```

`availableSize.Width` controls wrapping and horizontal collapse. `availableSize.Height` limits visible lines only when trimming is not `None`; at least the first line remains visible, matching the WPF `TextBlock` height-overflow contract.

### MeasureParagraph

```csharp
public virtual TextParagraphLayout MeasureParagraph(
    IReadOnlyList<TextSpan> spans,
    TextAspect paragraphAspect,
    TextParagraphOptions options,
    LayoutSize availableSize)
```

#### Parameters

| Name | Type | Description |
| --- | --- | --- |
| `spans` | `IReadOnlyList<TextSpan>` | The pieces of text, each with its own `TextAspect`. Each span resolves its own font from its aspect, using the scale of `paragraphAspect`; a span whose aspect has a `FontResourceId` ignores weight, style, and stretch. |
| `paragraphAspect` | `TextAspect` | Gives the wrapping, trimming, and scale of the whole paragraph. The wrapping and trimming of the aspects of the spans are not used. |
| `options` | `TextParagraphOptions` | Alignment, maximum lines, line height, and line stacking strategy. |
| `availableSize` | `LayoutSize` | The width the lines wrap, align, or trim to, and, with trimming, the height that limits the visible lines. |

#### Returns

`TextParagraphLayout`

The measured size (the widest line, the height of the lines), the line count, and the Drawing text layout. No spans give size `0`, `0` and no lines; one empty span gives width `0` and one line of the height of the font.

#### Exceptions

| Exception | Condition |
| --- | --- |
| `ArgumentNullException` | `spans` or `options` is `null`. |

#### Example

```csharp
using Cerneala.Drawing;
using Cerneala.UI.Layout;
using Cerneala.UI.Media;
using Cerneala.UI.Text;

SolidColorBrush black = new(Color.Black);
TextAspect regular = new("Segoe UI", 16, TextWrapping.Wrap, foreground: black);
TextAspect bold = new("Segoe UI", 16, TextWrapping.Wrap, foreground: black, fontWeight: FontWeight.Bold);

TextParagraphLayout paragraph = TextMeasurer.Default.MeasureParagraph(
    [new TextSpan("Hello ", regular), new TextSpan("world", bold)],
    regular,
    new TextParagraphOptions(TextAlignment.Center, MaxLines: 2),
    new LayoutSize(120, float.PositiveInfinity));

LayoutSize size = paragraph.Size;
int lines = paragraph.LineCount;
```

## Applies To

Cerneala UI text layout services in the `Cerneala` project.

## See Also

- `TextMeasureResult`
- `TextLayoutCache`
- `TextAspect`
- `LineBreakService`
- `FontResolver`
- `TextSpan`
- `TextParagraphOptions`
- `TextParagraphLayout`
