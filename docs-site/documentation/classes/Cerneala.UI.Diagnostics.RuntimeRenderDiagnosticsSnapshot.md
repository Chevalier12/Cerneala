# RuntimeRenderDiagnosticsSnapshot Record

## Definition
Namespace: `Cerneala.UI.Diagnostics`

Assembly/Project: `Cerneala`

Source: [`UI/Diagnostics/RuntimeDiagnostics.cs`](../../UI/Diagnostics/RuntimeDiagnostics.cs)

Represents root retained render-cache state captured as part of a runtime diagnostics snapshot.

```csharp
public sealed record RuntimeRenderDiagnosticsSnapshot(
    bool IsRootValid,
    int RootVersion,
    int RootCommandCount)
```

Inheritance:
`Object` -> `RuntimeRenderDiagnosticsSnapshot`

Implements:
`IEquatable<RuntimeRenderDiagnosticsSnapshot>`

## Examples

Capture render-cache diagnostics through `RuntimeDiagnostics.Capture`:

```csharp
using Cerneala.UI.Diagnostics;
using Cerneala.UI.Elements;
using Cerneala.UI.Hosting;
using Cerneala.UI.Invalidation;

UIRoot root = new(100, 100);
UiViewport viewport = new(root.ViewportWidth, root.ViewportHeight, root.Scale);
FrameStats stats = new();

RuntimeDiagnosticsSnapshot snapshot = RuntimeDiagnostics.Capture(root, viewport, stats);

bool rootCacheIsValid = snapshot.Render.IsRootValid;
int rootCacheVersion = snapshot.Render.RootVersion;
int rootCommands = snapshot.Render.RootCommandCount;
```

## Remarks

`RuntimeRenderDiagnosticsSnapshot` is the `Render` component of `RuntimeDiagnosticsSnapshot`. `RuntimeDiagnostics.Capture` creates it from `UIRoot.RetainedRenderCache` by copying `IsRootValid`, `Version`, and the current `RootCommands.Count`.

The capture path is read-only for the render cache. Tests verify that runtime diagnostics capture reads the retained render-cache version and command count without rebuilding render commands or invalidating the root.

`RuntimeDiagnosticsSnapshot.ToString()` includes `RootCommandCount` in the formatted runtime line as `commands={RootCommandCount}`. The other render fields remain available through the `Render` property for structured diagnostics.

The type is a positional record, so it can also be constructed directly when a caller needs to represent explicit render-cache diagnostic values. The constructor does not perform validation.

## Constructors

| Name | Description |
| --- | --- |
| `RuntimeRenderDiagnosticsSnapshot(bool isRootValid, int rootVersion, int rootCommandCount)` | Initializes the snapshot with retained root render-cache validity, version, and root command count. |

## Properties

| Name | Type | Description |
| --- | --- | --- |
| `IsRootValid` | `bool` | Gets whether the retained root render cache is currently valid. |
| `RootVersion` | `int` | Gets the retained root render-cache version copied from `RetainedRenderCache.Version`. |
| `RootCommandCount` | `int` | Gets the number of root draw commands currently stored in `RetainedRenderCache.RootCommands`. |

## Methods

| Name | Return Type | Description |
| --- | --- | --- |
| `Deconstruct(out bool IsRootValid, out int RootVersion, out int RootCommandCount)` | `void` | Deconstructs the positional record into its public component values. |
| `ToString()` | `string` | Returns the compiler-generated positional record string for the snapshot. |

## Applies To

Cerneala retained UI runtime diagnostics.

## See Also

- [`RuntimeDiagnostics`](Cerneala.UI.Diagnostics.RuntimeDiagnostics.md)
- [`RuntimeDiagnosticsSnapshot`](Cerneala.UI.Diagnostics.RuntimeDiagnosticsSnapshot.md)
- [`RootRenderDiagnosticsSnapshot`](Cerneala.UI.Diagnostics.RootRenderDiagnosticsSnapshot.md)
- `Cerneala.UI.Rendering.RetainedRenderCache`
