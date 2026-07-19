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
}
```

## Remarks

`UiHost.Update` uses `InputSource` when no explicit input frame or host-level input source is supplied. `UiHost.Draw` uses `DrawingBackend` when no explicit drawing backend is supplied.

The interface does not create `DrawingFrameContext` instances. `UiHost` reads the committed command list, runs one `PrismFrameAnalyzer` analysis for that draw, creates the frame context, and submits both through the resolved `IDrawingBackend`.

Either property may be `null`; the corresponding implicit host operation then requires another source or throws `InvalidOperationException`.

## Properties

| Name | Type | Description |
| --- | --- | --- |
| `InputSource` | `IInputSource?` | Gets the optional input source used by implicit host updates. |
| `DrawingBackend` | `IDrawingBackend?` | Gets the optional drawing backend used by implicit host draws. |

## Applies to

Cerneala platform hosting integrations and `UiHost`.

## See also

- `Cerneala.UI.Hosting.UiHost`
- `Cerneala.UI.Input.IInputSource`
- `Cerneala.Drawing.IDrawingBackend`
- `Cerneala.Drawing.DrawingFrameContext`
