# DrawTextLayoutBuilder Class

## Definition

Namespace: `Cerneala.Drawing`

Assembly/Project: `Cerneala`

Source: `Drawing/DrawTextLayout.cs`

Builds and caches immutable positioned text layouts from styled spans.

```csharp
public sealed class DrawTextLayoutBuilder
```

## Examples

```csharp
DrawTextLayout layout = new DrawTextLayoutBuilder()
    .AddSpan("Status: ", regularFont, 14, foreground)
    .AddSpan("Ready", boldFont, 14, Color.LimeGreen)
    .Build(new DrawTextLayoutOptions(maxWidth: 240, wrapping: DrawTextWrapping.Word));
```

## Remarks

`Build` looks up a static, process-wide cache of at most 256 layouts. The key contains every option value and, per span, the text, size, opacity, and the object identities (`RuntimeHelpers.GetHashCode`) of the font, brush, and fallback fonts. When the cache holds more than 256 entries, the oldest inserted entry is removed first; a cache hit does not refresh an entry. Rebuilding identical input returns the reusable layout rather than reshaping and reflowing it.

## Constructors

| Name | Description |
| --- | --- |
| `DrawTextLayoutBuilder()` | Creates an empty styled-span builder. |

## Methods

| Name | Returns | Description |
| --- | --- | --- |
| `AddSpan(DrawTextSpan)` | `DrawTextLayoutBuilder` | Appends a styled span. |
| `AddSpan(string, IDrawFont, float, IDrawBrush, float)` | `DrawTextLayoutBuilder` | Creates and appends a brush span. |
| `AddSpan(string, IDrawFont, float, Color, float)` | `DrawTextLayoutBuilder` | Creates and appends a color span. |
| `Build(DrawTextLayoutOptions?)` | `DrawTextLayout` | Returns a cached or newly measured immutable layout. |

## Exceptions

| Method | Exception | Condition |
| --- | --- | --- |
| `AddSpan(DrawTextSpan)` | `ArgumentNullException` | `span` is `null`. |
| `AddSpan(string, IDrawFont, float, IDrawBrush, float)` and `AddSpan(string, IDrawFont, float, Color, float)` | Same as the `DrawTextSpan` constructor | The arguments fail the [DrawTextSpan](Cerneala.Drawing.DrawTextSpan.md) validation. |

## Applies To

Reusable multi-line drawing through `DrawingContext.DrawTextLayout`.
