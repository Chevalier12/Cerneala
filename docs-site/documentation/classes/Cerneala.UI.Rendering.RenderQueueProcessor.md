# RenderQueueProcessor Class

## Definition
Namespace: `Cerneala.UI.Rendering`

Assembly/Project: `Cerneala`

Source: `UI/Rendering/RenderQueueProcessor.cs`

Processes queued render work by ensuring an element's retained local render cache is current.

```csharp
public sealed class RenderQueueProcessor
```

Inheritance:
`object` -> `RenderQueueProcessor`

## Examples

Process one element against a retained render cache:

```csharp
using Cerneala.UI.Elements;
using Cerneala.UI.Rendering;

RetainedRenderCache renderCache = new();
RenderCounters counters = new();
RenderQueueProcessor processor = new(renderCache, counters);

UIElement element = GetElement();
processor.Process(element);
```

Use the processor owned by a root during frame processing:

```csharp
using Cerneala.UI.Elements;

UIRoot root = new();
UIElement element = GetElement();

root.RenderQueueProcessor.Process(element);
```

## Remarks

`RenderQueueProcessor` is the render-cache phase worker used by `UIRoot` through `FramePhaseProcessors.RenderCache`. It operates on one `UIElement` at a time, gets that element's `ElementRenderCache` from the shared `RetainedRenderCache`, and asks the cache to rebuild when the element is stale or has render dirtiness.

When an element has a render-scope-only invalidation, `Process` invalidates the root command cache. If the element cache is still current, processing stops there; otherwise the local element cache is rebuilt before composition can use it.

When the element has `InvalidationFlags.Render`, `Process` forces a local cache rebuild. When `ElementRenderCache.Ensure` reports that a rebuild occurred, the processor invalidates the root render cache so the retained root command list can be composed again.

`Process` validates null input, but it does not catch exceptions raised while rebuilding the element cache. Render failures propagate to the caller.

## Constructors

| Name | Description |
| --- | --- |
| `RenderQueueProcessor(RetainedRenderCache renderCache, RenderCounters counters)` | Initializes a processor that updates element caches in `renderCache` and records cache work in `counters`. Throws `ArgumentNullException` when either argument is `null`. |

## Methods

| Name | Return Type | Description |
| --- | --- | --- |
| `Process(UIElement element)` | `void` | Ensures the retained local render cache for `element` is current, invalidating the root render cache when scope-only state or a local rebuild requires recomposition. Throws `ArgumentNullException` when `element` is `null`. |

## Applies To

Cerneala retained rendering frame processing.

## See Also

- `Cerneala.UI.Elements.UIRoot`
- `Cerneala.UI.Rendering.ElementRenderCache`
- `Cerneala.UI.Rendering.RetainedRenderCache`
- `Cerneala.UI.Rendering.RenderCounters`
