# RuntimeResourceDiagnosticsSnapshot Record

## Definition
Namespace: `Cerneala.UI.Diagnostics`

Assembly/Project: `Cerneala`

Source: [`UI/Diagnostics/RuntimeDiagnostics.cs`](../../UI/Diagnostics/RuntimeDiagnostics.cs)

Represents image resource-cache availability and load-count information captured during a runtime diagnostics snapshot.

```csharp
public sealed record RuntimeResourceDiagnosticsSnapshot(
    bool HasImageCache,
    int? ImageCacheLoadCount)
```

Inheritance:
`Object` -> `RuntimeResourceDiagnosticsSnapshot`

Implements:
`IEquatable<RuntimeResourceDiagnosticsSnapshot>`

## Examples

Capture resource diagnostics through `RuntimeDiagnostics.Capture`:

```csharp
using Cerneala.UI.Diagnostics;
using Cerneala.UI.Elements;
using Cerneala.UI.Hosting;
using Cerneala.UI.Invalidation;

UIRoot root = new(100, 100);
UiViewport viewport = new(100, 100);
FrameStats stats = new();

RuntimeDiagnosticsSnapshot snapshot = RuntimeDiagnostics.Capture(root, viewport, stats);

bool hasImageCache = snapshot.Resources.HasImageCache;
int? imageLoads = snapshot.Resources.ImageCacheLoadCount;
```

## Remarks

`RuntimeResourceDiagnosticsSnapshot` is the `Resources` component of `RuntimeDiagnosticsSnapshot`. `RuntimeDiagnostics.Capture` creates it from `UIRoot.ImageResourceCache`: `HasImageCache` is `true` when the root has an image resource cache, and `ImageCacheLoadCount` is copied from `ImageResourceCache.LoadCount` when the cache exists.

When no image resource cache is attached to the root, `HasImageCache` is `false` and `ImageCacheLoadCount` is `null`. `RuntimeDiagnosticsSnapshot.ToString()` formats the load count as `imageCache={count}` when present, or `imageCache=none` when the count is `null`.

`ImageResourceCache.LoadCount` increases when a path-backed `ImageResource` is loaded through the cache. Reusing an already cached image does not increase the count.

The type is a positional record. Its public constructor does not validate that `HasImageCache` and `ImageCacheLoadCount` are consistent, so direct construction should use `null` for `ImageCacheLoadCount` when no cache is available.

## Constructors

| Name | Description |
| --- | --- |
| `RuntimeResourceDiagnosticsSnapshot(bool hasImageCache, int? imageCacheLoadCount)` | Initializes the snapshot with image cache availability and an optional image cache load count. |

## Properties

| Name | Type | Description |
| --- | --- | --- |
| `HasImageCache` | `bool` | Gets whether the captured root had an `ImageResourceCache` instance. |
| `ImageCacheLoadCount` | `int?` | Gets the cache load count when an image resource cache exists; otherwise, `null`. |

## Methods

| Name | Return Type | Description |
| --- | --- | --- |
| `Deconstruct(out bool HasImageCache, out int? ImageCacheLoadCount)` | `void` | Deconstructs the positional record into its public component values. |
| `ToString()` | `string` | Returns the compiler-generated positional record string for the snapshot. |

## Applies To

Cerneala retained UI runtime diagnostics.

## See Also

- [`RuntimeDiagnostics`](Cerneala.UI.Diagnostics.RuntimeDiagnostics.md)
- [`RuntimeDiagnosticsSnapshot`](Cerneala.UI.Diagnostics.RuntimeDiagnosticsSnapshot.md)
- [`ImageResourceCache`](Cerneala.UI.Resources.ImageResourceCache.md)
- [`UIRoot`](Cerneala.UI.Elements.UIRoot.md)
