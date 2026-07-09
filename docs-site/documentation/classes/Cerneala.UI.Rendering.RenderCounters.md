# RenderCounters Class

## Definition
Namespace: `Cerneala.UI.Rendering`

Assembly/Project: `Cerneala`

Source: `UI/Rendering/RenderCounters.cs`

Tracks retained-rendering cache and command emission counters.

```csharp
public sealed class RenderCounters
```

Inheritance:
`object` -> `RenderCounters`

## Examples

Record cache and composition work:

```csharp
using Cerneala.UI.Rendering;

RenderCounters counters = new();

counters.CountCacheHit();
counters.CountCacheMiss();
counters.CountLocalRebuild();
counters.CountComposedElement();
counters.CountEmittedCommands(3);
```

## Remarks

`RenderCounters` is a mutable counter container used by retained rendering components. It records cache hits, cache misses, local cache rebuilds, composed elements, and emitted draw commands.

Each `Count...` method increments the matching counter. `CountEmittedCommands` adds the supplied command count and throws `ArgumentOutOfRangeException` when `count` is negative.

## Properties

| Name | Type | Description |
| --- | --- | --- |
| `CacheHits` | `int` | Gets the number of retained render cache hits. |
| `CacheMisses` | `int` | Gets the number of retained render cache misses. |
| `LocalRebuilds` | `int` | Gets the number of local render cache rebuilds. |
| `ComposedElements` | `int` | Gets the number of elements composed into a root command list. |
| `EmittedCommands` | `int` | Gets the total number of emitted draw commands counted so far. |

## Methods

| Name | Return Type | Description |
| --- | --- | --- |
| `CountCacheHit()` | `void` | Increments `CacheHits`. |
| `CountCacheMiss()` | `void` | Increments `CacheMisses`. |
| `CountLocalRebuild()` | `void` | Increments `LocalRebuilds`. |
| `CountComposedElement()` | `void` | Increments `ComposedElements`. |
| `CountEmittedCommands(int count)` | `void` | Adds `count` to `EmittedCommands`; rejects negative counts. |

## Applies To

Cerneala retained UI rendering diagnostics and cache accounting.

## See Also

- `Cerneala.UI.Rendering.ElementRenderCache`
- `Cerneala.UI.Rendering.DrawCommandListBuilder`
- `Cerneala.UI.Rendering.RenderQueueProcessor`
