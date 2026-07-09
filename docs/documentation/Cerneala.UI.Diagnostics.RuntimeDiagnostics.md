# RuntimeDiagnostics Class

## Definition
Namespace: `Cerneala.UI.Diagnostics`

Assembly/Project: `Cerneala`

Source: `UI/Diagnostics/RuntimeDiagnostics.cs`

Captures a read-only runtime diagnostics snapshot for a `UIRoot`, viewport, frame counters, input cache, retained render cache, resources, and platform services.

```csharp
public static class RuntimeDiagnostics
```

Inheritance:
`Object` -> `RuntimeDiagnostics`

## Examples
Capture and format runtime diagnostics for an existing frame:

```csharp
using Cerneala.UI.Diagnostics;
using Cerneala.UI.Elements;
using Cerneala.UI.Hosting;
using Cerneala.UI.Invalidation;

UIRoot root = new(320, 180, 1.5f);
UiViewport viewport = new(320, 180, 1.5f);
FrameStats stats = new();

RuntimeDiagnosticsSnapshot snapshot = RuntimeDiagnostics.Capture(root, viewport, stats);
string line = RuntimeDiagnostics.Format(snapshot);
```

The playground runtime preview captures diagnostics from the active `UiFrame` and assigns `RuntimeDiagnostics.Format(snapshot)` to a diagnostic text element.

## Remarks
`RuntimeDiagnostics` is a read-only diagnostics facade. `Capture` copies current values from the supplied `UIRoot`, `UiViewport`, and `FrameStats` into immutable snapshot records. It does not rebuild the input cache, rebuild retained render commands, invalidate the root, or load image resources.

The frame portion is captured through `FrameDiagnostics.Capture(stats)`. Runtime-specific values include viewport logical size and scale, input cache dirty/rebuild state, retained root render-cache validity/version/command count, optional image-cache availability and load count, and availability flags for optional platform services.

`Format` returns `snapshot.ToString()`. The snapshot string uses invariant culture and prints a compact single-line summary with fields such as `runtime viewport`, `scale`, `queuedMeasure`, `commands`, `imageCache`, `platform clipboard`, and `cursor`.

## Methods
| Name | Return Type | Description |
| --- | --- | --- |
| `Capture(UIRoot root, UiViewport viewport, FrameStats stats)` | `RuntimeDiagnosticsSnapshot` | Captures viewport, frame, input, render-cache, resource, and platform-service diagnostics from the current runtime state. |
| `Format(RuntimeDiagnosticsSnapshot snapshot)` | `string` | Returns `snapshot.ToString()` for a previously captured runtime snapshot. |

## Exceptions
| Method | Exception | Condition |
| --- | --- | --- |
| `Capture` | `ArgumentNullException` | `root` or `stats` is `null`. |
| `Format` | `ArgumentNullException` | `snapshot` is `null`. |

## Returned Snapshot Values
`Capture` returns a `RuntimeDiagnosticsSnapshot` record with these public values.

| Name | Type | Description |
| --- | --- | --- |
| `Viewport` | `RuntimeViewportDiagnosticsSnapshot` | Logical viewport width, height, and scale copied from the supplied `UiViewport`. |
| `Frame` | `FrameDiagnosticsSnapshot` | Frame phase, layout, render-cache, hit-test, no-work, and motion counters captured from `FrameStats`. |
| `Input` | `RuntimeInputDiagnosticsSnapshot` | Current input cache dirty flag, rebuild count, and last invalidation reason from the root input cache. |
| `Render` | `RuntimeRenderDiagnosticsSnapshot` | Retained root render-cache validity, root cache version, and root command count. |
| `Resources` | `RuntimeResourceDiagnosticsSnapshot` | Whether an image resource cache is present and, when present, its load count. |
| `Platform` | `RuntimePlatformDiagnosticsSnapshot` | Availability flags for clipboard, cursor, file dialogs, text input, DPI, and accessibility services. |

## Related Snapshot Records
| Type | Members | Description |
| --- | --- | --- |
| `RuntimeViewportDiagnosticsSnapshot` | `LogicalWidth`, `LogicalHeight`, `Scale` | Captures logical viewport dimensions and scale. |
| `RuntimeInputDiagnosticsSnapshot` | `IsDirty`, `RebuildCount`, `LastInvalidationReason` | Captures input cache reuse and invalidation state. |
| `RuntimeRenderDiagnosticsSnapshot` | `IsRootValid`, `RootVersion`, `RootCommandCount` | Captures retained root render-cache state without forcing a rebuild. |
| `RuntimeResourceDiagnosticsSnapshot` | `HasImageCache`, `ImageCacheLoadCount` | Captures optional image cache presence and load count. |
| `RuntimePlatformDiagnosticsSnapshot` | `HasClipboard`, `HasCursor`, `HasFileDialogs`, `HasTextInput`, `HasDpi`, `HasAccessibility` | Captures optional platform-service availability. |

## Applies To
Project: `Cerneala`

Runtime: `.NET 8`

## See Also
- `Cerneala.UI.Diagnostics.FrameDiagnostics`
- `Cerneala.UI.Diagnostics.RuntimeDiagnosticsSnapshot`
- `Cerneala.UI.Elements.UIRoot`
- `Cerneala.UI.Hosting.UiFrame`
- `Cerneala.UI.Invalidation.FrameStats`
