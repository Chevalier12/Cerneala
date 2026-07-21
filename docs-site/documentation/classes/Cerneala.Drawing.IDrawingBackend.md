# IDrawingBackend Interface

## Definition
Namespace: `Cerneala.Drawing`

Assembly/Project: `Cerneala`

Source: `Drawing/IDrawingBackend.cs`

Defines the backend-neutral entry point for rendering a retained command list with its current frame context.

```csharp
public interface IDrawingBackend
```

## Examples

```csharp
using Cerneala.Drawing;

sealed class BasicBackend : IDrawingBackend
{
    public List<DrawCommandKind> ExecutedKinds { get; } = [];

    public void Render(
        DrawCommandList commands,
        in DrawingFrameContext frameContext)
    {
        foreach (DrawCommand command in commands)
        {
            if (command.Kind is DrawCommandKind.BeginPrism or DrawCommandKind.EndPrism)
            {
                continue;
            }

            ExecutedKinds.Add(command.Kind);
        }

        // A backend that supports backdrop can inspect the frame-scoped lease.
        _ = frameContext.BackdropLease;
    }
}
```

## Remarks

Implementations must treat `commands` as read-only for the duration of `Render`. Retained UI can submit the same command-list instance across unchanged frames.

`UiHost` creates and validates the supplied `DrawingFrameContext` for the committed command list. Its public surface exposes only the optional backend-neutral backdrop lease; Prism scope analysis and freshness validation remain framework-owned implementation details.

A backend without Prism support ignores only `BeginPrism` and `EndPrism` and processes every command between them normally. Prism is presentation-only, so this fallback does not change layout, hit testing, or input routing and does not require a backdrop provider.

The `in` modifier passes the readonly frame context by reference. A backend must not retain frame-scoped state beyond the rendering call.

## Methods

| Name | Return Type | Description |
| --- | --- | --- |
| `Render(DrawCommandList commands, in DrawingFrameContext frameContext)` | `void` | Renders the read-only command list using the optional backdrop lease for the current frame. |

## Applies to

Cerneala drawing backend implementations and retained UI frame submission.

## See also

- `Cerneala.Drawing.DrawCommandList`
- `Cerneala.Drawing.DrawingFrameContext`
- `Cerneala.Drawing.MonoGame.MonoGameDrawingBackend`
- `Cerneala.UI.Rendering.RetainedRenderer`
