# RetainedRenderCache Class

## Definition
Namespace: `Cerneala.UI.Rendering`

Assembly/Project: `Cerneala`

Source: `UI/Rendering/RetainedRenderCache.cs`

Stores retained rendering command lists and per-element render caches.

```csharp
public sealed class RetainedRenderCache
```

Inheritance:
`object` -> `RetainedRenderCache`

## Examples

Get an element cache and track when the root command list is rebuilt:

```csharp
using Cerneala.UI.Elements;
using Cerneala.UI.Rendering;

RetainedRenderCache cache = new();
UIElement element = GetElement();

ElementRenderCache elementCache = cache.GetElementCache(element);

cache.InvalidateRoot();
cache.MarkRootBuilt();
```

## Remarks

`RetainedRenderCache` owns the root `DrawCommandList` used by retained rendering and keeps per-element `ElementRenderCache` instances in a `ConditionalWeakTable<UIElement, ElementRenderCache>`.

`GetElementCache` creates an element cache on demand for the supplied element and throws `ArgumentNullException` when `element` is `null`. `InvalidateRoot` marks the root command list as invalid. `MarkRootBuilt` marks the root as valid and increments `Version`.

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

## Applies To

Cerneala retained UI rendering and diagnostics APIs.

## See Also

- `Cerneala.UI.Rendering.ElementRenderCache`
- `Cerneala.UI.Rendering.DrawCommandListBuilder`
- `Cerneala.UI.Rendering.RenderQueueProcessor`
- `Cerneala.UI.Elements.UIElement`
