# RetainedRenderer Class

## Definition
Namespace: `Cerneala.UI.Rendering`

Assembly/Project: `Cerneala`

Source: `UI/Rendering/RetainedRenderer.cs`

Commits, returns, and submits the retained root draw command list for a `UIRoot`.

```csharp
public sealed class RetainedRenderer
```

Inheritance:
`object` -> `RetainedRenderer`

## Examples

Commit a root before reading the retained draw commands:

```csharp
using Cerneala.Drawing;
using Cerneala.UI.Elements;

UIRoot root = new();

DrawCommandList committed = root.RetainedRenderer.Commit(root);
DrawCommandList rendered = root.RetainedRenderer.Render(root);

bool reusedCommandList = ReferenceEquals(committed, rendered);
```

Submit the committed command list to a drawing backend:

```csharp
using Cerneala.Drawing;
using Cerneala.Drawing.Prism.Graph;
using Cerneala.UI.Elements;

UIRoot root = new();
CapturingBackend backend = new();

DrawCommandList committed = root.RetainedRenderer.Commit(root);
DrawingFrameContext frameContext = new(
    new PrismFrameAnalyzer().Analyze(committed));
root.RetainedRenderer.Submit(root, backend, in frameContext);

bool backendReceivedCommittedList = ReferenceEquals(committed, backend.LastCommands);

private sealed class CapturingBackend : IDrawingBackend
{
    public DrawCommandList? LastCommands { get; private set; }

    public void Render(
        DrawCommandList commands,
        in DrawingFrameContext frameContext)
    {
        frameContext.EnsureCurrent(commands);
        LastCommands = commands;
    }
}
```

## Remarks

`RetainedRenderer` is the small public facade over retained rendering. `UIRoot` creates one using its `RetainedRenderCache`, `DrawCommandListBuilder`, and `RenderCounters`, then exposes it through `UIRoot.RetainedRenderer`.

`Commit` is the update-side operation. It checks whether the root command cache is valid and, when needed, asks `DrawCommandListBuilder` to rebuild `RetainedRenderCache.RootCommands`. It returns the cache-owned `DrawCommandList`.

`Render` is the draw-side read operation. It returns the already committed root command list without rebuilding it. If the root cache is invalid, `Render` throws `InvalidOperationException` with guidance to call `Commit` during update before rendering or submitting.

`Submit` validates the backend, calls `Render`, validates the supplied `DrawingFrameContext` against the committed list, and passes that same list and context directly to `IDrawingBackend.Render`. The command list is not copied, so backends must treat it as read-only during rendering.

`RetainedRenderer` does not analyze the command list itself. The caller creates the frame analysis once and supplies it through the context; `UiHost.Draw` performs that orchestration for hosted rendering.

## Constructors

| Name | Description |
| --- | --- |
| `RetainedRenderer(RetainedRenderCache renderCache, DrawCommandListBuilder builder, RenderCounters counters)` | Initializes a renderer using the retained root cache, command-list builder, and render counters. |

## Methods

| Name | Return Type | Description |
| --- | --- | --- |
| `Commit(UIRoot root)` | `DrawCommandList` | Builds the root command list when the retained root cache is invalid, then returns the cache-owned root commands. |
| `Render(UIRoot root)` | `DrawCommandList` | Returns the already committed root command list and throws if the root cache is invalid. |
| `Submit(UIRoot root, IDrawingBackend backend, in DrawingFrameContext frameContext)` | `void` | Validates the frame context and sends the committed root command list and that context to the specified drawing backend. |

## Exceptions

| Member | Exception | Condition |
| --- | --- | --- |
| `RetainedRenderer(RetainedRenderCache renderCache, DrawCommandListBuilder builder, RenderCounters counters)` | `ArgumentNullException` | `renderCache`, `builder`, or `counters` is `null`. |
| `Commit(UIRoot root)` | `ArgumentNullException` | `root` is `null`. |
| `Render(UIRoot root)` | `ArgumentNullException` | `root` is `null`. |
| `Render(UIRoot root)` | `InvalidOperationException` | The root command list is not committed because the retained root cache is invalid. |
| `Submit(UIRoot root, IDrawingBackend backend, in DrawingFrameContext frameContext)` | `ArgumentNullException` | `backend` or `root` is `null`. |
| `Submit(UIRoot root, IDrawingBackend backend, in DrawingFrameContext frameContext)` | `InvalidOperationException` | The root command list is not committed, the context is uninitialized, or its analysis does not match the committed command list and current Prism scope versions. |

## Applies To

Cerneala retained UI rendering, especially `UIRoot` update/draw flows and `UiHost` frame submission.

## See Also

- `Cerneala.UI.Elements.UIRoot`
- `Cerneala.UI.Rendering.RetainedRenderCache`
- `Cerneala.UI.Rendering.DrawCommandListBuilder`
- `Cerneala.Drawing.DrawCommandList`
- `Cerneala.Drawing.DrawingFrameContext`
- `Cerneala.Drawing.IDrawingBackend`
- `Cerneala.Drawing.Prism.Graph.PrismFrameAnalyzer`
