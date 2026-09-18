# RetainedRenderCache Class

## Definition
Namespace: `Cerneala.UI.Rendering`

Assembly/Project: `Cerneala`

Source: `UI/Rendering/RetainedRenderCache.cs`

Stores retained rendering command lists and per-element render caches.

```csharp
public sealed class RetainedRenderCache : IDisposable
```

Inheritance:
`object` -> `RetainedRenderCache`

## Examples

Get an element cache and track when the root command list is rebuilt:

```csharp
using Cerneala.UI.Elements;
using Cerneala.UI.Rendering;

using RetainedRenderCache cache = new();
UIElement element = new();

ElementRenderCache elementCache = cache.GetElementCache(element);

cache.InvalidateRoot();
cache.MarkRootBuilt();
```

## Remarks

`RetainedRenderCache` owns the root `DrawCommandList` used by retained rendering, independent acquisitions for its recorded cache-managed images, and its per-element `ElementRenderCache` instances. It keeps element entries until explicitly released by root lifecycle processing or until the retained cache is disposed; entries are no longer weak-key entries whose image release responsibility could disappear during garbage collection.

`GetElementCache` creates an element cache on demand for the supplied element and throws `ArgumentNullException` when `element` is `null`. `InvalidateRoot` marks the root command list as invalid. `MarkRootBuilt` marks the root as valid and increments `Version`.

Dispose a retained cache that you create directly. The `UIRoot.RetainedRenderCache` instance belongs to the root; application code must not dispose it independently. Returned element caches are borrowed from their retained owner. `Dispose` clears root/local commands and releases every owned image acquisition, even if one release fails; release failures are reported together in an `AggregateException`. Repeated disposal is harmless. `GetElementCache` and `MarkRootBuilt` throw `ObjectDisposedException` after disposal. Invalidation does not itself discard commands or release their images. Use the cache on the rendering/UI thread.

The public command lists contain borrowed image references. Adding a command manually does not transfer ownership of its image to the cache; keep your own image acquisition for manually supplied commands. The retained rendering pipeline tracks acquisitions when it builds commands.

## Properties

| Name | Type | Description |
| --- | --- | --- |
| `RootCommands` | `DrawCommandList` | Gets the root draw command list owned by the cache. |
| `Version` | `int` | Gets the root cache version, incremented each time the root is marked built. |
| `IsRootValid` | `bool` | Gets whether the root command list is currently marked valid. |

## Methods

| Name | Return Type | Description |
| --- | --- | --- |
| `GetElementCache(UIElement element)` | `ElementRenderCache` | Gets or creates the render cache associated with an element. |
| `InvalidateRoot()` | `void` | Marks the root command list invalid. |
| `MarkRootBuilt()` | `void` | Marks the root command list valid and increments `Version`. |
| `Dispose()` | `void` | Disposes all owned element caches, clears root commands, and releases recorded image acquisitions. |

## Applies To

Cerneala retained UI rendering and diagnostics APIs.

## See Also

- `Cerneala.UI.Rendering.ElementRenderCache`
- `Cerneala.UI.Rendering.DrawCommandListBuilder`
- `Cerneala.UI.Rendering.RenderQueueProcessor`
- `Cerneala.UI.Elements.UIElement`
