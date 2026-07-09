# RuntimeInputDiagnosticsSnapshot Record

## Definition
Namespace: `Cerneala.UI.Diagnostics`

Assembly/Project: `Cerneala`

Source: [`UI/Diagnostics/RuntimeDiagnostics.cs`](../../UI/Diagnostics/RuntimeDiagnostics.cs)

Represents the input-route cache state captured as part of a runtime diagnostics snapshot.

```csharp
public sealed record RuntimeInputDiagnosticsSnapshot(
    bool IsDirty,
    int RebuildCount,
    string LastInvalidationReason)
```

Inheritance:
`Object` -> `RuntimeInputDiagnosticsSnapshot`

Implements:
`IEquatable<RuntimeInputDiagnosticsSnapshot>`

## Examples

Read input cache diagnostics from a runtime snapshot:

```csharp
using Cerneala.UI.Diagnostics;
using Cerneala.UI.Elements;
using Cerneala.UI.Hosting;
using Cerneala.UI.Invalidation;

UIRoot root = new(100, 100);
root.InputCache.EnsureCurrent(root);

RuntimeDiagnosticsSnapshot snapshot = RuntimeDiagnostics.Capture(
    root,
    new UiViewport(100, 100),
    new FrameStats());

bool inputCacheNeedsRebuild = snapshot.Input.IsDirty;
int rebuilds = snapshot.Input.RebuildCount;
string reason = snapshot.Input.LastInvalidationReason;
```

## Remarks

`RuntimeInputDiagnosticsSnapshot` is the `Input` component of `RuntimeDiagnosticsSnapshot`. `RuntimeDiagnostics.Capture` creates it by copying `UIRoot.InputCache.IsDirty`, `UIRoot.InputCache.RebuildCount`, and `UIRoot.InputCache.LastInvalidationReason`.

`IsDirty` indicates whether the input route cache is marked for rebuild. `RebuildCount` is incremented by `ElementInputCache.Rebuild`, including rebuilds triggered through `EnsureCurrent`. `LastInvalidationReason` stores the most recent reason supplied to `ElementInputCache.Invalidate`; a new cache starts with `"Initial input cache"` and blank invalidation reasons are normalized to `"Input route changed"`.

The runtime formatted diagnostics line includes `IsDirty` and `RebuildCount` as `input dirty={IsDirty}` and `inputRebuilds={RebuildCount}`. It does not include `LastInvalidationReason`.

This record reports cache health for the retained runtime input route map. For per-event hit-target diagnostics, use `InputDiagnosticsSnapshot` instead.

## Constructors

| Name | Description |
| --- | --- |
| `RuntimeInputDiagnosticsSnapshot(bool isDirty, int rebuildCount, string lastInvalidationReason)` | Initializes the snapshot with input cache dirty state, rebuild count, and last invalidation reason. |

## Properties

| Name | Type | Description |
| --- | --- | --- |
| `IsDirty` | `bool` | Gets whether the retained input route cache is currently marked dirty. |
| `RebuildCount` | `int` | Gets the number of times the input route map has been rebuilt. |
| `LastInvalidationReason` | `string` | Gets the most recent input cache invalidation reason. |

## Methods

| Name | Return Type | Description |
| --- | --- | --- |
| `Deconstruct(out bool IsDirty, out int RebuildCount, out string LastInvalidationReason)` | `void` | Deconstructs the positional record into its public component values. |
| `ToString()` | `string` | Returns the compiler-generated positional record string for the snapshot. |

## Applies To

Cerneala retained UI runtime diagnostics.

## See Also

- [`RuntimeDiagnostics`](Cerneala.UI.Diagnostics.RuntimeDiagnostics.md)
- [`RuntimeDiagnosticsSnapshot`](Cerneala.UI.Diagnostics.RuntimeDiagnosticsSnapshot.md)
- [`InputDiagnosticsSnapshot`](Cerneala.UI.Diagnostics.InputDiagnosticsSnapshot.md)
- `Cerneala.UI.Input.ElementInputCache`
- `Cerneala.UI.Elements.UIRoot`
