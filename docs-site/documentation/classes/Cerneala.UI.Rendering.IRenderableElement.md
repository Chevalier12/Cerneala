# IRenderableElement Interface

## Definition
Namespace: `Cerneala.UI.Rendering`

Assembly/Project: `Cerneala`

Source: `UI/Rendering/IRenderableElement.cs`

Defines how an element records its drawing into a retained render cache.

```csharp
public interface IRenderableElement
```

## Examples

Count how often a custom element records its drawing:

```csharp
using Cerneala.UI.Elements;
using Cerneala.UI.Invalidation;
using Cerneala.UI.Rendering;

UIRoot root = new(100, 100);
CountingElement element = new();
root.VisualChildren.Add(element);

root.ProcessFrame();
root.ProcessFrame();
Console.WriteLine($"{element.Renders} {element.RenderVersion}");
// 1 1

element.Invalidate(InvalidationFlags.Render, "demo");
root.ProcessFrame();
Console.WriteLine($"{element.Renders} {element.RenderVersion}");
// 2 1

sealed class CountingElement : UIElement
{
    public int Renders { get; private set; }

    protected override void OnRender(RenderContext context) => Renders++;
}
```

The second frame reuses the cached drawing, so `OnRender` runs once. A render invalidation forces a new recording without changing `RenderVersion`.

## Remarks

`UIElement` is the only implementation. A custom element draws by overriding `protected virtual void OnRender(RenderContext context)`; `Render` throws `ArgumentNullException` for a `null` context and then calls `OnRender`.

Each element has an `ElementRenderCache` that holds its recorded `DrawCommandList`. In the render-cache phase of a frame, the cache calls `Render` only when it is stale:

- the element is dirty for render, or
- the cache is invalid or belongs to another element, or
- `RenderVersion` or `RenderDependencies` differs from the values stored with the cache.

Otherwise the stored commands are reused. The cache calls `Render` only when the element's `Visibility` is `Visible` and `IsVisible` is `true`; otherwise the cache stays empty. If `OnRender` throws, the cache is cleared and the exception propagates.

`RenderVersion` increases when the element's render inputs change, for example when `RenderDependencies` changes. `RenderDependencies` lists the external inputs, such as resources, that the recording used; changing it increases `RenderVersion` and invalidates render.

`UiHost` draws the committed retained commands; it does not call `Render` during drawing. `VisualBrush` calls `Render` to record its source element.

## Properties

| Name | Type | Description |
| --- | --- | --- |
| `RenderVersion` | `int` | Gets a counter that increases when the element's render inputs change. |
| `RenderDependencies` | `RenderDependency` | Gets the external inputs the last recording depends on. |

## Methods

| Name | Return Type | Description |
| --- | --- | --- |
| `Render(RenderContext context)` | `void` | Records the element's drawing into `context.DrawingContext`. |

## Applies to

`UIElement`.

## See also

- `Cerneala.UI.Rendering.RenderContext`
- `Cerneala.UI.Rendering.ElementRenderCache`
- `Cerneala.UI.Rendering.RenderDependency`
- `Cerneala.UI.Elements.UIElement`
