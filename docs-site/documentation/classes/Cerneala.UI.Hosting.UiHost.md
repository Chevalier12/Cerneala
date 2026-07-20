# UiHost Class

## Definition

Namespace: `Cerneala.UI.Hosting`

Assembly/Project: `Cerneala`

Source: `UI/Hosting/UiHost.cs`

Coordinates a retained `UIRoot` and its UI-thread Relay with input, frame scheduling, motion invalidation, cursor publishing, and retained rendering.

```csharp
public sealed class UiHost
```

## Examples

```csharp
using Cerneala.UI.Elements;
using Cerneala.UI.Hosting;
using Cerneala.UI.Input;

UIRoot root = new();
UiHost host = new(new UiHostOptions
{
    Root = root,
    Viewport = new UiViewport(800, 600)
});

InputFrame input = new(
    PointerSnapshot.Empty,
    PointerSnapshot.Empty,
    KeyboardSnapshot.Empty,
    KeyboardSnapshot.Empty,
    Array.Empty<TextInputSnapshotEvent>());

UiFrame frame = host.Update(input, elapsedTime: TimeSpan.FromSeconds(1d / 60d));
```

## Remarks

`UiHost` is the core retained UI host. It owns the current `UIRoot` reference, exposes the root-owned Relay without taking separate ownership, applies viewport data to that root, dispatches `InputFrame` values through an `ElementInputBridge`, runs scheduled frame work, commits retained render data, and stores the last produced `UiFrame`.

The constructor can receive an initial root, viewport, input source, backend, clock, input bridge, and platform services through `UiHostOptions`. When a root is attached, the host applies platform services and viewport values to it. `SetRoot(UIRoot)` replaces the retained root, reapplies the same platform services and viewport, and forces the next update to invalidate the initial frame. Assigning `Backend` validates any optional backdrop source against its drawing backend.

`Update(UiViewport?, TimeSpan?)` reads input from `InputSource`, or from `Backend.InputSource` when `InputSource` is not set. `Update(InputFrame, UiViewport?, TimeSpan?)` uses an explicit input frame. Both update paths require a retained root and the root's owning UI thread. Each update drains one stable Relay snapshot before scheduler and input work, so Relay invalidations can be processed in that update while work posted during dispatch waits for the next update.

`Draw()` uses `Backend.DrawingBackend`. It analyzes the committed command list once, checks only that `PrismFrameAnalysis` for a backdrop requirement, and acquires at most one lease from `Backend.BackdropFrameSource`. Every qualifying Prism scope shares that lease through one `DrawingFrameContext`. The host skips acquisition when analysis reports no visible, non-empty backdrop, submits a null lease when no provider is configured, and disposes an acquired lease in a `finally` path after backend submission.

`Draw(IDrawingBackend)` submits to an explicit drawing backend. It uses the configured backdrop source only when the explicit backend is the same instance as `Backend.DrawingBackend`; unrelated explicit backends receive no configured lease. Drawing requires a retained root and a drawing backend, but it does not create a new `UiFrame`; frames are produced by `Update`.

If the root exposes a platform cursor service, `Update` resolves the cursor at the current pointer position and publishes it after committing retained render data.

## Constructors

| Name | Description |
| --- | --- |
| `UiHost(UiHostOptions?)` | Initializes the host from the provided options, or from default options when `null` is supplied. |

## Properties

| Name | Description |
| --- | --- |
| `Root` | Gets the current retained UI root, if one is attached. |
| `Relay` | Gets the current root's UI-thread Relay, or `null` while no root is attached. The host does not create or own a second Relay. |
| `InputSource` | Gets or sets the input source used by `Update(UiViewport?, TimeSpan?)`. |
| `Backend` | Gets or sets the backend used as a fallback input source and by `Draw()`. A supplied backdrop source must be compatible with its drawing backend. |
| `Clock` | Gets or sets the clock used when update elapsed time is not supplied explicitly. |
| `InputBridge` | Gets the bridge that dispatches input frames into the retained element tree. |
| `Viewport` | Gets the current logical viewport applied by the host. |
| `LastFrame` | Gets the most recent frame produced by `Update`, or `null` before the first update. |

## Methods

| Name | Description |
| --- | --- |
| `SetRoot(UIRoot)` | Verifies both root thread owners, replaces the retained root, applies platform services and the current viewport, and marks the next update as the initial frame for that root. |
| `Update(UiViewport?, TimeSpan?)` | Gets an input frame from `InputSource` or `Backend.InputSource`, advances the retained UI frame, commits retained render data, publishes cursor state, and returns the produced `UiFrame`. |
| `Update(InputFrame, UiViewport?, TimeSpan?)` | Advances the retained UI frame with an explicit input frame, optional viewport, and optional elapsed time. |
| `Draw()` | Analyzes and submits retained drawing commands through `Backend.DrawingBackend`, with at most one configured backdrop lease. |
| `Draw(IDrawingBackend)` | Submits retained drawing commands to the specified drawing backend and uses the configured backdrop source only when backend instances match. |

## Exceptions

| Member | Exception | Condition |
| --- | --- | --- |
| `SetRoot(UIRoot)` | `ArgumentNullException` | `newRoot` is `null`. |
| `SetRoot(UIRoot)` | `InvalidOperationException` | The call is not on the current or replacement root's owning UI thread. |
| `Update(UiViewport?, TimeSpan?)` | `InvalidOperationException` | No retained root is attached, or no input source is available from `InputSource` or `Backend.InputSource`. |
| `Update(InputFrame, UiViewport?, TimeSpan?)` | `ArgumentNullException` | `inputFrame` is `null`. |
| `Update(InputFrame, UiViewport?, TimeSpan?)` | `InvalidOperationException` | No retained root is attached, or the call is not on its owning UI thread. |
| `Draw()` | `InvalidOperationException` | No retained root is attached, or no drawing backend is available from `Backend.DrawingBackend`. |
| `Draw()` | Provider-defined exception | The configured backdrop frame source fails while acquiring a required frame. |
| `Backend` setter | `InvalidOperationException` | A backdrop source has no drawing backend or reports that it is incompatible with that drawing backend. |
| `Draw(IDrawingBackend)` | `ArgumentNullException` | `backend` is `null`. |
| `Draw(IDrawingBackend)` | `InvalidOperationException` | No retained root is attached, or the call is not on its owning UI thread. |

## Applies to

Cerneala retained UI hosting.

## See also

- `Cerneala.UI.Hosting.UiHostOptions`
- `Cerneala.UI.Hosting.UiFrame`
- `Cerneala.UI.Hosting.UiViewport`
- `Cerneala.UI.Elements.UIRoot`
- `Cerneala.UI.Relay.UiRelay`
- `Cerneala.UI.Input.InputFrame`
- `Cerneala.Drawing.IDrawingBackend`
