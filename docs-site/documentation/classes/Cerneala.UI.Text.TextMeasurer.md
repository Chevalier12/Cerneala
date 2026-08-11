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

`Measure` normalizes the supplied width before it builds the cache key. `TextWrapping.NoWrap` uses an infinite width only when trimming is `None`; either ellipsis mode retains a finite width so overflowing no-wrap lines can be collapsed. Positive infinity remains unconstrained, while non-positive or `NaN` widths become `0`.

The `LayoutSize` overload also applies finite height. When an ellipsis mode is active and all lines do not fit, the result retains at least the first line and collapses the final visible line. The width-only overload is equivalent to an unconstrained height.

The cache key includes the text, resolved font identity, font size, wrapping mode, normalized wrapping width, trimming mode, scale, and the effective visible-line count. The returned result contains the measured `LayoutSize`, visible line count, cache key, resolved font identity, and the exact lines to render. Height is based on measured line height when the text shaper can provide it, otherwise on `FontSize * Scale`.

Calls to `Measure` lock around cache access for the `TextMeasurer` instance. The exposed `LayoutCache` object can still be accessed directly by callers, so direct cache operations should be coordinated by the caller when shared across threads.

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

## Method Details

### Measure

```csharp
public virtual TextMeasureResult Measure(string text, TextAspect aspect, float availableWidth)
```

#### Parameters

| Name | Type | Description |
| --- | --- | --- |
| `text` | `string` | The text to measure. Cannot be `null`. |
| `aspect` | `TextAspect` | Font, wrapping, trimming, scale, color, and optional font resource information used for measurement. |
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

## Applies To

Cerneala UI text layout services in the `Cerneala` project.

## See Also

- `TextMeasureResult`
- `TextLayoutCache`
- `TextAspect`
- `LineBreakService`
- `FontResolver`
