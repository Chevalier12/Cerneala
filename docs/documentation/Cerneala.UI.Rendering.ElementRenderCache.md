# ElementRenderCache Class

## Definition
Namespace: `Cerneala.UI.Rendering`

Assembly/Project: `Cerneala`

Source: `UI/Rendering/ElementRenderCache.cs`

Caches the local draw command list and render state for a single `UIElement`.

```csharp
public sealed class ElementRenderCache
```

Inheritance:
`object` -> `ElementRenderCache`

## Examples

Rebuild a local render cache and reuse it while the element remains unchanged:

```csharp
using Cerneala.UI.Elements;
using Cerneala.UI.Rendering;

UIElement element = GetElement();
ElementRenderCache cache = new();
RenderCounters counters = new();

cache.Ensure(element, counters, forceRebuild: true);

if (!cache.IsStale(element))
{
    DrawCommandList commands = cache.GetValidCommands(element);
}
```

## Remarks

`ElementRenderCache` stores a reusable `DrawCommandList` for one cached element together with the element render version, render dependencies, and arranged content bounds captured during rebuild.

`Ensure` returns `false` and counts a cache hit when the cache is still valid for the supplied element. Otherwise it clears the current commands, counts a cache miss and local rebuild, renders the visible element into the command list, then marks the cache valid for that element.

`GetValidCommands` throws `InvalidOperationException` when the cache is stale for the supplied element. `Invalidate` marks the cache invalid and clears the cached element reference without clearing the command list.

## Properties

| Name | Type | Description |
| --- | --- | --- |
| `Commands` | `DrawCommandList` | Gets the command list owned by this cache. |
| `IsValid` | `bool` | Gets whether the cache currently contains valid data for its cached element. |
| `RenderVersion` | `int` | Gets the render version captured during the last successful rebuild. |
| `Dependencies` | `RenderDependency` | Gets the render dependencies captured during the last successful rebuild. |
| `ContentBounds` | `LayoutRect` | Gets the arranged bounds captured during the last successful rebuild. |

## Methods

| Name | Return Type | Description |
| --- | --- | --- |
| `IsStale(UIElement element)` | `bool` | Returns whether the cache is invalid, belongs to a different element, or no longer matches the element render version or dependencies. |
| `GetValidCommands(UIElement element)` | `DrawCommandList` | Returns the cached command list, or throws when the cache is stale for `element`. |
| `Ensure(UIElement element, RenderCounters counters, bool forceRebuild = false)` | `bool` | Ensures the cache is valid for `element`; returns `true` when a rebuild happened. |
| `Invalidate()` | `void` | Marks the cache invalid and clears the cached element reference. |

## Applies To

Cerneala retained rendering and render cache internals.

## See Also

- `Cerneala.UI.Rendering.RetainedRenderCache`
- `Cerneala.UI.Rendering.RenderQueueProcessor`
- `Cerneala.UI.Rendering.DrawCommandListBuilder`
- `Cerneala.UI.Elements.UIElement`
