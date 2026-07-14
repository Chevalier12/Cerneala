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
```

## Remarks

`MonoGameUiHost` composes `MonoGameInputSource`, `MonoGameContentServices`, `MonoGameDrawingBackend`, and the core `UiHost`. It wires image resource caching into the root and keeps the drawing backend coordinate scale aligned with the current viewport scale before drawing.

`Update(UiViewport, TimeSpan)` reads input through the configured input source after applying the viewport scale. `Update(InputFrame, UiViewport, TimeSpan)` lets callers supply an already constructed input frame. Both verify the root's Relay before pumping hosted windows and delegate the single root Relay drain to the core host update.

`Draw` begins a `SpriteBatch` with `SpriteSortMode.Immediate` and `MonoGameDrawingBackend.ScissorRasterizerState`, delegates retained drawing to the core host, and always ends the sprite batch in a `finally` block.

Disposal releases the drawing backend and content services once.

## Constructors

| Name | Description |
| --- | --- |
| `MonoGameUiHost(MonoGameUiHostOptions)` | Initializes the MonoGame UI host from sprite batch, content, root, viewport, input, clock, and platform options. |

## Properties

| Name | Description |
| --- | --- |
| `InputSource` | Gets the MonoGame input source used by the host. |
| `ContentServices` | Gets the content services used for fonts, text, images, and image caching. |
| `Root` | Gets the current UI root, if one is attached. |
| `Relay` | Gets the current root's UI-thread Relay through the core host, or `null` while no root is attached. |
| `LastFrame` | Gets the last frame produced by the core UI host. |

## Methods

| Name | Description |
| --- | --- |
| `SetRoot(UIRoot)` | Replaces the current root and attaches content services to it. |
| `Update(UiViewport, TimeSpan)` | Updates input scaling and advances the UI frame using the input source. |
| `Update(InputFrame, UiViewport, TimeSpan)` | Advances the UI frame with an explicit input frame. |
| `QueueTextInput(string)` | Queues text input into the MonoGame input source. |
| `Draw()` | Draws the retained UI through the MonoGame drawing backend. |
| `Dispose()` | Disposes drawing and content services once. |

## Applies to

Cerneala MonoGame UI hosting.

## See also

- `Cerneala.UI.Hosting.UiHost`
- `Cerneala.UI.Hosting.MonoGame.MonoGameUiHostOptions`
- `Cerneala.Drawing.MonoGame.MonoGameDrawingBackend`
- `Cerneala.UI.Hosting.MonoGame.MonoGameContentServices`
- `Cerneala.UI.Relay.UiRelay`
