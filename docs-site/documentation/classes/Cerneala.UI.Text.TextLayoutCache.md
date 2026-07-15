# TextLayoutCache Class

## Definition
Namespace: `Cerneala.UI.Text`

Assembly/Project: `Cerneala`

Source: `UI/Text/TextLayoutCache.cs`

Caches text measurement results by `TextLayoutKey`.

```csharp
public sealed class TextLayoutCache
```

Inheritance:
`object` -> `TextLayoutCache`

## Examples

Cache a text measurement result:

```csharp
using Cerneala.UI.Text;

TextLayoutCache cache = new(capacity: 512);
TextLayoutKey key = CreateLayoutKey();

TextMeasureResult result = cache.GetOrAdd(
    key,
    layoutKey => Measure(layoutKey));

bool contains = cache.Contains(key);
```

## Remarks

`TextLayoutCache` stores `TextMeasureResult` instances by `TextLayoutKey`. Storage is bounded by `Capacity`; once full, adding a layout evicts the least recently used entry. A cache hit refreshes that entry's recency. The default capacity is `DefaultCapacity` (`512`). `GetOrAdd` increments `Hits` when a cached result is found and `Misses` when the factory is used.

The factory argument to `GetOrAdd` must be non-null. `Clear` removes all cached results and resets hit and miss counters to zero.

## Constructors

| Name | Description |
| --- | --- |
| `TextLayoutCache(int capacity = DefaultCapacity)` | Creates a bounded cache. Throws `ArgumentOutOfRangeException` when `capacity` is zero or negative. |

## Properties

| Name | Type | Description |
| --- | --- | --- |
| `DefaultCapacity` | `int` | Default cache capacity (`512`). |
| `Capacity` | `int` | Gets the maximum number of retained layouts. |
| `Count` | `int` | Gets the number of currently retained layouts. |
| `Hits` | `int` | Gets the number of cache hits. |
| `Misses` | `int` | Gets the number of cache misses. |

## Methods

| Name | Return Type | Description |
| --- | --- | --- |
| `GetOrAdd(TextLayoutKey key, Func<TextLayoutKey, TextMeasureResult> factory)` | `TextMeasureResult` | Returns an existing cached result or creates, stores, and returns a new one. |
| `Contains(TextLayoutKey key)` | `bool` | Returns whether the cache contains a result for `key`. |
| `Clear()` | `void` | Clears cached results and resets counters. |

## Applies To

Cerneala UI text measurement and layout caching.

## See Also

- `Cerneala.UI.Text.TextLayoutKey`
- `Cerneala.UI.Text.TextMeasureResult`
- `Cerneala.UI.Text.TextMeasurer`
