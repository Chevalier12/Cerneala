# ElementInputCache Class

## Definition
Namespace: `Cerneala.UI.Input`

Assembly/Project: `Cerneala`

Source: `UI/Input/ElementInputCache.cs`

Caches the current retained input route map for a `UIRoot` and exposes hit testing against that map.

```csharp
public sealed class ElementInputCache
```

Inheritance:
`Object` -> `ElementInputCache`

## Examples

Use `EnsureCurrent` before input dispatch or hit testing to rebuild the route map only when it is dirty or when the root changes.

```csharp
using Cerneala.UI.Elements;
using Cerneala.UI.Input;

ElementInputCache cache = new();

ElementInputRouteMap routeMap = cache.EnsureCurrent(root);
HitTestResult? hit = cache.HitTest(root, pointerX, pointerY);
```

Invalidate the cache when retained input routing state changes.

```csharp
cache.Invalidate("Element tree changed");

ElementInputRouteMap routeMap = cache.EnsureCurrent(root);
```

## Remarks

`ElementInputCache` owns an `ElementInputRouteMap` and rebuilds it with `ElementInputRouteBuilder`. A new instance starts dirty with the invalidation reason set to `Initial input cache`.

`EnsureCurrent` returns the cached route map when it is clean for the same `UIRoot`. It calls `Rebuild` when the cache is dirty or when the supplied root differs from the last root used to build the cache.

`Invalidate` marks the cache dirty and records a reason. Empty or whitespace reasons are normalized to `Input route changed`.

`HitTest` ensures the route map is current before delegating to `HitTestService`.

## Constructors

| Name | Description |
| --- | --- |
| `ElementInputCache()` | Initializes an input cache with an empty route map and marks it dirty. |

## Properties

| Name | Type | Description |
| --- | --- | --- |
| `IsDirty` | `bool` | Gets whether the route map must be rebuilt before it is reused. |
| `LastInvalidationReason` | `string` | Gets the most recent invalidation reason. |
| `RebuildCount` | `int` | Gets the number of times the route map has been rebuilt. |
| `RouteMap` | `ElementInputRouteMap` | Gets the current cached route map. |

## Methods

| Name | Return Type | Description |
| --- | --- | --- |
| `EnsureCurrent(UIRoot)` | `ElementInputRouteMap` | Returns a current route map for the supplied root, rebuilding it when necessary. Throws if `root` is `null`. |
| `HitTest(UIRoot, float, float, HitTestFilter?)` | `HitTestResult?` | Ensures the route map is current, then hit-tests a point with an optional filter. Throws if `root` is `null`. |
| `Invalidate(string)` | `void` | Marks the cache dirty and stores an invalidation reason. |
| `Rebuild(UIRoot)` | `ElementInputRouteMap` | Rebuilds the route map for the supplied root, clears the dirty flag, increments `RebuildCount`, and returns the new map. Throws if `root` is `null`. |

## Applies to

- `Cerneala.UI.Input.ElementInputCache`

## See also

- `Cerneala.UI.Input.ElementInputRouteMap`
- `Cerneala.UI.Input.ElementInputRouteBuilder`
- `Cerneala.UI.Input.HitTestService`
