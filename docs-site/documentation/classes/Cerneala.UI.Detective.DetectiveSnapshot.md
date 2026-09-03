# DetectiveSnapshot Record

Namespace: `Cerneala.UI.Detective`

Assembly: `Cerneala.dll`

Source: [`UI/Detective/Detective.cs`](https://github.com/Chevalier12/Cerneala/blob/master/UI/Detective/Detective.cs)

## Definition

Stores an immutable aggregate of root, frame, input, rendering, resource, platform, and Motion diagnostic state.

```csharp
public sealed record DetectiveSnapshot(
    ViewportDiagnosticsSnapshot Viewport,
    FrameDiagnosticsSnapshot Frame,
    RootInputDiagnosticsSnapshot Input,
    RootRenderDiagnosticsSnapshot Rendering,
    ResourceDiagnosticsSnapshot Resources,
    PlatformDiagnosticsSnapshot Platform,
    MotionGraphSnapshot Motion);
```

## Examples

```csharp
DetectiveSnapshot snapshot = root.Detective.Capture(root.ProcessFrame());
int renderedElements = snapshot.Frame.RenderedElements;
int rootCommands = snapshot.Rendering.RootCommandCount;
bool motionContinues = snapshot.Motion.NeedsAnotherFrame;
```

## Remarks

`Detective.Capture` creates the snapshot by copying current state. `ToString()` emits an invariant, compact line prefixed with `detective viewport=`.

## Properties

| Name | Type | Description |
| --- | --- | --- |
| `Frame` | `FrameDiagnosticsSnapshot` | Gets counters from the supplied frame statistics. |
| `Input` | `RootInputDiagnosticsSnapshot` | Gets root input-cache state. |
| `Motion` | `MotionGraphSnapshot` | Gets current Motion graph and activity state. |
| `Platform` | `PlatformDiagnosticsSnapshot` | Gets optional platform-service availability. |
| `Rendering` | `RootRenderDiagnosticsSnapshot` | Gets retained root render-cache state. |
| `Resources` | `ResourceDiagnosticsSnapshot` | Gets image-cache availability and load count. |
| `Viewport` | `ViewportDiagnosticsSnapshot` | Gets root viewport dimensions and scale. |

## Methods

| Name | Returns | Description |
| --- | --- | --- |
| `ToString()` | `string` | Formats the aggregate state as an invariant diagnostic line. |
