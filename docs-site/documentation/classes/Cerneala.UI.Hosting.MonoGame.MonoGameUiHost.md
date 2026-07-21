# MonoGameUiHost Class

## Definition
Namespace: `Cerneala.UI.Hosting.MonoGame`

Assembly/Project: `Cerneala`

Source: `UI/Hosting/MonoGame/MonoGameUiHost.cs`

Hosts a Cerneala retained UI tree in a MonoGame application.

```csharp
public sealed class MonoGameUiHost : IDisposable
```

Implements:
`IDisposable`

## Examples

```csharp
using Cerneala.UI.Hosting;
using Cerneala.UI.Hosting.MonoGame;

MonoGameUiHost host = new(new MonoGameUiHostOptions
{
    SpriteBatch = spriteBatch,
    WhitePixel = whitePixel,
    Root = root,
    Viewport = new UiViewport(800, 600)
});

host.Update(new UiViewport(800, 600), elapsed);
host.Draw();

host.BackdropFrameSource = nextBackdropSource;
```

## Remarks

`MonoGameUiHost` composes `MonoGameInputSource`, `MonoGameContentServices`, `MonoGameDrawingBackend`, and the core `UiHost`. It wires image resource caching into the root and keeps the drawing backend coordinate scale aligned with the current viewport scale before drawing.

`Update(UiViewport, TimeSpan)` reads input through the configured input source after applying the viewport scale. `Update(InputFrame, UiViewport, TimeSpan)` lets callers supply an already constructed input frame. Both verify the root's Relay before pumping hosted windows and delegate the single root Relay drain to the core host update.

`Draw` delegates retained drawing to `MonoGameDrawingBackend`, which owns the
top-level sprite batch and restores the documented incoming graphics-device
state after success or failure. Callers must not begin the configured sprite
batch around `Draw`. When `MonoGameUiHostOptions.BackdropFrameSource` is set,
the core host acquires at most one lease from it after frame analysis and
disposes the lease after drawing, including exceptional exits.

The host borrows the configured `SpriteBatch`, white-pixel texture, and graphics
device. It owns the drawing backend it creates and disposes that backend once,
without disposing the borrowed graphics resources. The configured
`ContentServices` instance is host-owned even when supplied by the caller and is
disposed with the host.

The backdrop source and the already-rendered scene remain caller-owned. The
host does not dispose the source and does not retain a lease after `Draw`
returns. `BackdropFrameSource` can be replaced between frames or set to `null`.
Each assignment validates the candidate against the existing
`MonoGameDrawingBackend`; an incompatible candidate is rejected and the
previous provider remains configured. The WindowsDX scene source accepts any
live `MonoGameDrawingBackend` that uses its `GraphicsDevice`; compatibility does
not require the source and consumer to share the same backend instance. This
also keeps screenshot rendering through a temporary same-device backend on the
same backdrop path as on-screen drawing.

The host passes `MonoGameUiHostOptions.PrismRendererOptions` to its owned
drawing backend. `PrismDiagnostics` returns that backend's immutable diagnostic
snapshot, so applications can inspect hit, miss, promotion, eviction, memory,
and saved-work counters without taking ownership of cache resources.

## Constructors

| Name | Description |
| --- | --- |
| `MonoGameUiHost(MonoGameUiHostOptions)` | Validates the graphics resources, then initializes the MonoGame UI host from sprite batch, content, root, viewport, input, clock, and platform options. |

## Properties

| Name | Description |
| --- | --- |
| `InputSource` | Gets the MonoGame input source used by the host. |
| `ContentServices` | Gets the content services used for fonts, text, images, and image caching. |
| `Root` | Gets the current UI root, if one is attached. |
| `Relay` | Gets the current root's UI-thread Relay through the core host, or `null` while no root is attached. |
| `LastFrame` | Gets the last frame produced by the core UI host. |
| `PrismDiagnostics` | Gets an immutable snapshot of cumulative Prism retained-cache work and current surface usage from the owned drawing backend. |
| `BackdropFrameSource` | Gets or replaces the optional caller-owned source used for backdrop-aware frames. Set `null` to disable backdrop acquisition. |

## Methods

| Name | Description |
| --- | --- |
| `SetRoot(UIRoot)` | Replaces the current root and attaches content services to it. |
| `Update(UiViewport, TimeSpan)` | Updates input scaling and advances the UI frame using the input source. |
| `Update(InputFrame, UiViewport, TimeSpan)` | Advances the UI frame with an explicit input frame. |
| `QueueTextInput(string)` | Queues text input into the MonoGame input source. |
| `Draw()` | Draws the retained UI through the backend-owned sprite batch without requiring an external `Begin`/`End` pair. |
| `Dispose()` | Disposes the owned drawing backend and content services once, but not the borrowed sprite batch, white-pixel texture, or graphics device. |

## Exceptions

| Member | Exception | Condition |
| --- | --- | --- |
| Constructor | `ArgumentNullException` | `options`, `SpriteBatch`, or `WhitePixel` is `null`. |
| Constructor | `ObjectDisposedException` | A configured graphics resource or its graphics device is disposed. |
| Constructor | `ArgumentException` | `WhitePixel` belongs to a different graphics device than `SpriteBatch`. |
| Constructor | `ArgumentOutOfRangeException` | A configured Prism byte or entry limit is negative, or its retained soft limit exceeds its hard limit. |
| `BackdropFrameSource` setter | `ObjectDisposedException` | The host has already been disposed. |
| `BackdropFrameSource` setter | `InvalidOperationException` | The candidate source cannot supply leases consumable by the host's live MonoGame drawing backend. The previous source remains configured. |

## Applies to

Cerneala MonoGame UI hosting.

## See also

- `Cerneala.UI.Hosting.UiHost`
- `Cerneala.UI.Hosting.MonoGame.MonoGameUiHostOptions`
- `Cerneala.Drawing.MonoGame.MonoGameDrawingBackend`
- `Cerneala.Drawing.MonoGame.Prism.IMonoGameBackdropFrameLease`
- `Cerneala.Drawing.Prism.IBackdropFrameSource`
- `Cerneala.Drawing.Prism.PrismRendererDiagnostics`
- `Cerneala.Drawing.Prism.PrismRendererOptions`
- `Cerneala.UI.Hosting.MonoGame.MonoGameContentServices`
- `Cerneala.UI.Relay.UiRelay`
