# ElementRenderDiagnosticsSnapshot Class

## Definition
Namespace: `Cerneala.UI.Diagnostics`

Assembly/Project: `Cerneala`

Source: `UI/Diagnostics/RenderDiagnostics.cs`

Captures the render-cache state for one `UIElement` at the moment `RenderDiagnostics.CaptureElement` reads it.

```csharp
public sealed record ElementRenderDiagnosticsSnapshot(
    string? ElementId,
    string ElementType,
    int ElementRenderVersion,
    RenderDependency ElementDependencies,
    bool IsCacheValid,
    int CacheRenderVersion,
    RenderDependency CacheDependencies,
    Cerneala.UI.Layout.LayoutRect ContentBounds,
    int CommandCount,
    bool IsStale)
```

Inheritance:
`object` -> `ElementRenderDiagnosticsSnapshot`

Implements:
`IEquatable<ElementRenderDiagnosticsSnapshot>`

## Examples

```csharp
using Cerneala.UI.Diagnostics;
using Cerneala.UI.Layout;
using Cerneala.UI.Rendering;

ElementRenderDiagnosticsSnapshot snapshot = new(
    ElementId: "42",
    ElementType: "TextBlock",
    ElementRenderVersion: 3,
    ElementDependencies: RenderDependency.None.WithTextVersion(1),
    IsCacheValid: true,
    CacheRenderVersion: 3,
    CacheDependencies: RenderDependency.None.WithTextVersion(1),
    ContentBounds: new LayoutRect(0, 0, 120, 24),
    CommandCount: 2,
    IsStale: false);

string diagnosticsLine = snapshot.ToString();
```

## Remarks

`ElementRenderDiagnosticsSnapshot` is a sealed positional record used by render diagnostics. `RenderDiagnostics.CaptureElement` builds it from an element and a `RetainedRenderCache` by reading:

- the element id, CLR type name, render version, and render dependency value from the `UIElement`;
- the matching `ElementRenderCache` validity, render version, dependencies, content bounds, command count, and stale state.

`IsStale` reflects `ElementRenderCache.IsStale(element)`, which is true when the cache is invalid, belongs to a different element instance, has a different render version, or has different render dependencies.

`ToString()` formats a single invariant-culture diagnostics line. If `ElementId` is null, the formatted element identity uses `unattached`.

## Constructors

| Name | Description |
| --- | --- |
| `ElementRenderDiagnosticsSnapshot(string? ElementId, string ElementType, int ElementRenderVersion, RenderDependency ElementDependencies, bool IsCacheValid, int CacheRenderVersion, RenderDependency CacheDependencies, LayoutRect ContentBounds, int CommandCount, bool IsStale)` | Initializes a render diagnostics snapshot with element-side and cache-side render state. |

## Properties

| Name | Type | Description |
| --- | --- | --- |
| `ElementId` | `string?` | The element identifier as text, or null when the source element has no id. |
| `ElementType` | `string` | The CLR type name of the source element. |
| `ElementRenderVersion` | `int` | The render version currently stored on the source element. |
| `ElementDependencies` | `RenderDependency` | The render dependency value currently stored on the source element. |
| `IsCacheValid` | `bool` | Indicates whether the element's render cache is marked valid. |
| `CacheRenderVersion` | `int` | The render version stored in the element render cache. |
| `CacheDependencies` | `RenderDependency` | The render dependency value stored in the element render cache. |
| `ContentBounds` | `LayoutRect` | The content bounds stored in the element render cache. |
| `CommandCount` | `int` | The number of draw commands currently stored in the element render cache. |
| `IsStale` | `bool` | Indicates whether the element render cache is stale for the source element. |

## Methods

| Name | Description |
| --- | --- |
| `ToString()` | Returns an invariant-culture diagnostics string containing element identity, cache validity, stale state, render versions, command count, bounds, and dependencies. |

## Applies to

Cerneala retained UI render diagnostics.

## See Also

- `RenderDiagnostics`
- `RenderCacheDumper`
- `ElementRenderCache`
- `RenderDependency`
