# RenderContext Class

## Definition
Namespace: `Cerneala.UI.Rendering`

Assembly/Project: `Cerneala`

Source: `UI/Rendering/RenderContext.cs`

Carries the current element, drawing surface, layout bounds, render layer, and counters for one render pass.

```csharp
public sealed class RenderContext
```

Inheritance:
`object` -> `RenderContext`

## Examples
Use `RenderContext` inside an element's `OnRender` override to inspect the arranged bounds and emit draw commands through the current `DrawingContext`.

```csharp
using Cerneala.Drawing;
using Cerneala.UI.Elements;
using Cerneala.UI.Rendering;

public sealed class SwatchElement : UIElement
{
    protected override void OnRender(RenderContext context)
    {
        DrawRect rect = new(
            context.Bounds.X,
            context.Bounds.Y,
            context.Bounds.Width,
            context.Bounds.Height);

        context.DrawingContext.FillRectangle(rect, new DrawColor(40, 120, 200));
    }
}
```

## Remarks
`RenderContext` is created by the retained rendering pipeline and passed to `UIElement.Render(RenderContext)`, which delegates to `OnRender(RenderContext)`. The context is immutable after construction: all public properties are get-only references or values supplied by the caller.

The default element render cache creates a context with the element being rebuilt, a `DrawingContext` backed by that element's command list, the element's `ArrangedBounds`, `RenderLayer.Default`, and the current `RenderCounters`.

The constructor requires non-null `element`, `drawingContext`, and `counters` arguments. Passing `null` for any of those arguments throws `ArgumentNullException`. `Bounds` and `Layer` are value types and are stored as provided.

## Constructors
| Name | Description |
| --- | --- |
| `RenderContext(UIElement element, DrawingContext drawingContext, LayoutRect bounds, RenderLayer layer, RenderCounters counters)` | Initializes a render context for an element render pass. Throws `ArgumentNullException` when `element`, `drawingContext`, or `counters` is `null`. |

## Properties
| Name | Type | Description |
| --- | --- | --- |
| `Element` | `UIElement` | Gets the element associated with this render pass. |
| `DrawingContext` | `DrawingContext` | Gets the drawing context used to append draw commands. |
| `Bounds` | `LayoutRect` | Gets the layout bounds supplied for rendering, typically the element's arranged bounds. |
| `Layer` | `RenderLayer` | Gets the render layer data supplied for the pass, including opacity through `RenderLayer.Opacity`. |
| `Counters` | `RenderCounters` | Gets the counters object used by rendering code to record cache, rebuild, composition, and emitted-command counts. |

## Applies to
`Cerneala` UI rendering pipeline.

## See also
- `Cerneala.UI.Elements.UIElement`
- `Cerneala.Drawing.DrawingContext`
- `Cerneala.UI.Rendering.RenderLayer`
- `Cerneala.UI.Rendering.RenderCounters`
