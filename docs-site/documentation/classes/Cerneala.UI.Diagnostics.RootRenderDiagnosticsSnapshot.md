# RootRenderDiagnosticsSnapshot Record

## Definition
Namespace: `Cerneala.UI.Diagnostics`

Assembly/Project: `Cerneala`

Source: `UI/Diagnostics/RenderDiagnostics.cs`

Captures root-level retained render cache state for diagnostics.

```csharp
public sealed record RootRenderDiagnosticsSnapshot(
    bool IsRootValid,
    int Version,
    int RootCommandCount)
```

## Examples

```csharp
using Cerneala.UI.Diagnostics;

RootRenderDiagnosticsSnapshot snapshot = RenderDiagnostics.CaptureRoot(renderCache);
string text = snapshot.ToString();
```

## Remarks

`RootRenderDiagnosticsSnapshot` is produced by `RenderDiagnostics.CaptureRoot`. It records whether the root cache is valid, the retained cache version, and how many root draw commands are currently stored.

`ToString` formats the snapshot with invariant culture as a compact diagnostic line containing validity, version, and command count.

## Constructors

| Name | Description |
| --- | --- |
| `RootRenderDiagnosticsSnapshot(bool, int, int)` | Initializes the root render diagnostics snapshot. |

## Properties

| Name | Description |
| --- | --- |
| `IsRootValid` | Gets whether the root retained render cache is valid. |
| `Version` | Gets the retained render cache version. |
| `RootCommandCount` | Gets the number of root draw commands in the cache. |

## Methods

| Name | Description |
| --- | --- |
| `ToString()` | Returns a compact root render cache diagnostic string. |

## Applies to

Cerneala retained UI render diagnostics.

## See also

- `Cerneala.UI.Diagnostics.RenderDiagnostics`
- `Cerneala.UI.Rendering.RetainedRenderCache`
