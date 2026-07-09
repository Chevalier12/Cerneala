# RenderDiagnostics Class

## Definition
Namespace: `Cerneala.UI.Diagnostics`

Assembly/Project: `Cerneala` (`net8.0`)

Source: `UI/Diagnostics/RenderDiagnostics.cs`

Provides static helpers that capture render-cache diagnostic snapshots for a retained UI root cache or a single `UIElement`.

```csharp
public static class RenderDiagnostics
```

Inheritance:
`Object` -> `RenderDiagnostics`

## Examples
Capture root-level and element-level render diagnostics from an existing retained UI root:

```csharp
using Cerneala.UI.Diagnostics;
using Cerneala.UI.Elements;

UIRoot root = GetRoot();
UIElement element = GetElement();

RootRenderDiagnosticsSnapshot rootSnapshot =
    RenderDiagnostics.CaptureRoot(root.RetainedRenderCache);

ElementRenderDiagnosticsSnapshot elementSnapshot =
    RenderDiagnostics.CaptureElement(element, root.RetainedRenderCache);

Console.WriteLine(rootSnapshot);
Console.WriteLine(elementSnapshot);
```

`RenderCacheDumper` uses the same API to print the root cache summary and then one render-cache snapshot for each element returned by `ElementTreeWalker.PreOrder`.

## Remarks
`RenderDiagnostics` is a read-only diagnostics entry point. It does not rebuild render caches, invalidate elements, or mutate render state directly.

`CaptureRoot` reports the retained root cache validity flag, root cache version, and number of commands currently stored in `RetainedRenderCache.RootCommands`.

`CaptureElement` retrieves the element-local cache with `RetainedRenderCache.GetElementCache(element)` and compares cache state against the current `UIElement`. The resulting snapshot includes element identity, element type name, element render version, element render dependencies, cache validity, cached render version, cached dependencies, cached content bounds, cached command count, and whether the cache is stale for that element.

Both methods throw `ArgumentNullException` for null arguments. `CaptureElement` can create an empty `ElementRenderCache` entry through `RetainedRenderCache.GetElementCache` when the cache has no entry yet for the supplied element.

## Methods
| Name | Return Type | Description |
| --- | --- | --- |
| `CaptureRoot(RetainedRenderCache cache)` | `RootRenderDiagnosticsSnapshot` | Captures root render-cache validity, version, and root command count. |
| `CaptureElement(UIElement element, RetainedRenderCache cache)` | `ElementRenderDiagnosticsSnapshot` | Captures the retained render-cache state associated with `element` and marks whether that cache is stale for the current element state. |

## Exceptions
| Method | Exception | Condition |
| --- | --- | --- |
| `CaptureRoot` | `ArgumentNullException` | `cache` is `null`. |
| `CaptureElement` | `ArgumentNullException` | `element` or `cache` is `null`. |

## Returned Snapshots
| Type | Members | Description |
| --- | --- | --- |
| `RootRenderDiagnosticsSnapshot` | `IsRootValid`, `Version`, `RootCommandCount` | Root-level retained render-cache state. Its `ToString()` uses invariant culture and prints root validity, version, and command count. |
| `ElementRenderDiagnosticsSnapshot` | `ElementId`, `ElementType`, `ElementRenderVersion`, `ElementDependencies`, `IsCacheValid`, `CacheRenderVersion`, `CacheDependencies`, `ContentBounds`, `CommandCount`, `IsStale` | Element-local retained render-cache state. Its `ToString()` uses invariant culture and prints element identity, cache validity, staleness, render versions, command count, bounds, and dependency values. |

## Applies To
Project: `Cerneala`

Runtime: `.NET 8`

## See Also
- `Cerneala.UI.Diagnostics.RenderCacheDumper`
- `Cerneala.UI.Rendering.RetainedRenderCache`
- `Cerneala.UI.Rendering.ElementRenderCache`
- `Cerneala.UI.Rendering.RenderDependency`
- `Cerneala.UI.Elements.UIElement`
