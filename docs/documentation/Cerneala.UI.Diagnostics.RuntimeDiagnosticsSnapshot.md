# RuntimeDiagnosticsSnapshot Class

## Definition
Namespace: `Cerneala.UI.Diagnostics`

Assembly/Project: `Cerneala`

Source: `UI/Diagnostics/RuntimeDiagnostics.cs`

Represents an immutable aggregate of runtime diagnostics captured from a retained UI root, viewport, frame counters, input cache, render cache, resources, and platform services.

```csharp
public sealed record RuntimeDiagnosticsSnapshot(
    RuntimeViewportDiagnosticsSnapshot Viewport,
    FrameDiagnosticsSnapshot Frame,
    RuntimeInputDiagnosticsSnapshot Input,
    RuntimeRenderDiagnosticsSnapshot Render,
    RuntimeResourceDiagnosticsSnapshot Resources,
    RuntimePlatformDiagnosticsSnapshot Platform)
```

Inheritance:
`object` -> `RuntimeDiagnosticsSnapshot`

## Examples

Capture and format a runtime diagnostics snapshot for an existing UI root:

```csharp
using Cerneala.UI.Diagnostics;
using Cerneala.UI.Elements;
using Cerneala.UI.Hosting;
using Cerneala.UI.Invalidation;

UIRoot root = new(320, 180, 1.5f);
UiViewport viewport = new(320, 180, 1.5f);
FrameStats stats = new();

RuntimeDiagnosticsSnapshot snapshot = RuntimeDiagnostics.Capture(root, viewport, stats);

float scale = snapshot.Viewport.Scale;
bool hasFrameWork = snapshot.Frame.HasWork;
int rootCommands = snapshot.Render.RootCommandCount;
string line = snapshot.ToString();
```

## Remarks

`RuntimeDiagnosticsSnapshot` is the top-level value returned by `RuntimeDiagnostics.Capture`. It groups specialized snapshot records for viewport state, frame work, input cache reuse, retained render-cache state, image resource-cache availability, and optional platform-service availability.

Capturing a snapshot is read-only. The tested capture path does not rebuild retained render commands, does not invalidate the root, and does not create scheduler work. It reads the current root render-cache version and root command count as they already exist.

`ToString()` uses invariant culture and returns a compact single-line diagnostics summary. The formatted line includes viewport size and scale, frame phase counters, layout and render counters, motion counters, input dirty/rebuild state, retained root command count, image-cache load count or `none`, and selected platform-service flags.

The primary constructor does not validate component values. Code that constructs the record directly is responsible for passing meaningful nested snapshot instances.

## Constructors

| Name | Description |
| --- | --- |
| `RuntimeDiagnosticsSnapshot(RuntimeViewportDiagnosticsSnapshot, FrameDiagnosticsSnapshot, RuntimeInputDiagnosticsSnapshot, RuntimeRenderDiagnosticsSnapshot, RuntimeResourceDiagnosticsSnapshot, RuntimePlatformDiagnosticsSnapshot)` | Initializes the aggregate runtime diagnostics snapshot with explicit nested snapshot values. |

## Properties

| Name | Type | Description |
| --- | --- | --- |
| `Viewport` | `RuntimeViewportDiagnosticsSnapshot` | Gets the logical viewport width, logical viewport height, and scale. |
| `Frame` | `FrameDiagnosticsSnapshot` | Gets retained frame phase, layout, render-cache, hit-test, no-work, and motion counters. |
| `Input` | `RuntimeInputDiagnosticsSnapshot` | Gets input cache dirty state, rebuild count, and last invalidation reason. |
| `Render` | `RuntimeRenderDiagnosticsSnapshot` | Gets retained root render-cache validity, root version, and root command count. |
| `Resources` | `RuntimeResourceDiagnosticsSnapshot` | Gets image resource-cache presence and image-cache load count when available. |
| `Platform` | `RuntimePlatformDiagnosticsSnapshot` | Gets availability flags for clipboard, cursor, file dialogs, text input, DPI, and accessibility services. |

## Methods

| Name | Return Type | Description |
| --- | --- | --- |
| `ToString()` | `string` | Formats the aggregate runtime snapshot into a compact invariant-culture diagnostics line. |
| `Deconstruct(...)` | `void` | Deconstructs the positional record into its public component values. |

## Applies To

Cerneala retained UI runtime diagnostics.

## See Also

- `Cerneala.UI.Diagnostics.RuntimeDiagnostics`
- `Cerneala.UI.Diagnostics.FrameDiagnosticsSnapshot`
- `Cerneala.UI.Diagnostics.RuntimeViewportDiagnosticsSnapshot`
- `Cerneala.UI.Diagnostics.RuntimeInputDiagnosticsSnapshot`
- `Cerneala.UI.Diagnostics.RuntimeRenderDiagnosticsSnapshot`
- `Cerneala.UI.Diagnostics.RuntimeResourceDiagnosticsSnapshot`
- `Cerneala.UI.Diagnostics.RuntimePlatformDiagnosticsSnapshot`
- `Cerneala.UI.Elements.UIRoot`
- `Cerneala.UI.Hosting.UiFrame`
- `Cerneala.UI.Invalidation.FrameStats`
