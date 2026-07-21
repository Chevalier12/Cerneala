# IUiBackend Interface

## Definition
Namespace: `Cerneala.UI.Hosting`

Assembly/Project: `Cerneala`

Source: `UI/Hosting/IUiBackend.cs`

Exposes the input and drawing services that a `UiHost` can resolve from its current backend.

```csharp
public interface IUiBackend
```

## Examples

```csharp
using Cerneala.Drawing;
using Cerneala.Drawing.Prism;
using Cerneala.UI.Hosting;
using Cerneala.UI.Input;

static IUiBackend CreateBackend(
    IInputSource? inputSource,
    IDrawingBackend? drawingBackend) =>
    new HostBackend(inputSource, drawingBackend);

sealed class HostBackend : IUiBackend
{
    public HostBackend(
        IInputSource? inputSource,
        IDrawingBackend? drawingBackend)
    {
        InputSource = inputSource;
        DrawingBackend = drawingBackend;
    }

    public IInputSource? InputSource { get; }

    public IDrawingBackend? DrawingBackend { get; }

    public IBackdropFrameSource? BackdropFrameSource => null;
}
```

## Remarks

`UiHost.Update` uses `InputSource` when no explicit input frame or host-level input source is supplied. `UiHost.Draw` uses `DrawingBackend` when no explicit drawing backend is supplied.

The interface does not create `DrawingFrameContext` instances. `UiHost` reads the committed command list, analyzes it once for that draw, optionally acquires one backdrop lease from `BackdropFrameSource`, creates the frame context, and submits both through the resolved `IDrawingBackend`.

`BackdropFrameSource` has a default interface implementation that returns `null`, so existing backends and hosts that do not use Prism backdrop require no new implementation. When a source is supplied, assigning the backend to a host validates source/backend compatibility.

Any property may be `null`; missing input or drawing services make the corresponding implicit host operation require another source or throw `InvalidOperationException`. A missing backdrop source leaves the frame context lease empty so the drawing backend can apply the central fallback policy. The backend contract does not own or dispose the optional source; it only exposes the application-owned provider selected for the current host.

## Properties

| Name | Type | Description |
| --- | --- | --- |
| `InputSource` | `IInputSource?` | Gets the optional input source used by implicit host updates. |
| `DrawingBackend` | `IDrawingBackend?` | Gets the optional drawing backend used by implicit host draws. |
| `BackdropFrameSource` | `IBackdropFrameSource?` | Gets the optional application-owned backdrop frame provider. Defaults to `null`. |

## Applies to

Cerneala platform hosting integrations and `UiHost`.

## See also

- `Cerneala.UI.Hosting.UiHost`
- `Cerneala.UI.Input.IInputSource`
- `Cerneala.Drawing.IDrawingBackend`
- `Cerneala.Drawing.DrawingFrameContext`
- `Cerneala.Drawing.Prism.IBackdropFrameSource`
