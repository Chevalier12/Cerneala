# IDrawingBackend Interface

## Definition
Namespace: `Cerneala.Drawing`

Assembly/Project: `Cerneala`

Source: `Drawing/IDrawingBackend.cs`

Defines the backend-neutral entry point for rendering a retained command list with its current frame analysis.

```csharp
public interface IDrawingBackend
```

## Examples

```csharp
using Cerneala.Drawing;
using Cerneala.Drawing.Prism.Graph;

DrawCommandList commands = new();
DrawingFrameContext frameContext = new(
    new PrismFrameAnalyzer().Analyze(commands));
IDrawingBackend backend = new CapturingBackend();

backend.Render(commands, in frameContext);

sealed class CapturingBackend : IDrawingBackend
{
    public void Render(
        DrawCommandList commands,
        in DrawingFrameContext frameContext)
    {
        frameContext.EnsureCurrent(commands);
    }
}
```

## Remarks

Implementations must treat `commands` as read-only for the duration of `Render`. Retained UI can submit the same command-list instance across unchanged frames.

The supplied `DrawingFrameContext` carries the `PrismFrameAnalysis` produced for the same command list and can also carry an optional backend-neutral backdrop lease. Implementations should validate it with `DrawingFrameContext.EnsureCurrent` before using analyzed scope data.

The `in` modifier passes the readonly frame context by reference. A backend must not retain frame-scoped state beyond the rendering call.

## Methods

| Name | Return Type | Description |
| --- | --- | --- |
| `Render(DrawCommandList commands, in DrawingFrameContext frameContext)` | `void` | Renders the read-only command list using the analysis and optional backdrop lease for the current frame. |

## Applies to

Cerneala drawing backend implementations and retained UI frame submission.

## See also

- `Cerneala.Drawing.DrawCommandList`
- `Cerneala.Drawing.DrawingFrameContext`
- `Cerneala.Drawing.MonoGame.MonoGameDrawingBackend`
- `Cerneala.UI.Rendering.RetainedRenderer`
