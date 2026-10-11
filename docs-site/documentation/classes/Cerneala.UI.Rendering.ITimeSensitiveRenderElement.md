# ITimeSensitiveRenderElement Interface

## Definition
Namespace: `Cerneala.UI.Rendering`

Assembly/Project: `Cerneala`

Source: `UI/Rendering/ITimeSensitiveRenderElement.cs`

Lets an element update time-based drawing, such as an animation or a blinking caret, on every host update.

```csharp
public interface ITimeSensitiveRenderElement
```

## Examples

An element that adds up the frame times it receives:

```csharp
using Cerneala.UI.Elements;
using Cerneala.UI.Rendering;

UIRoot root = new(100, 100);
Ticker ticker = new();
root.VisualChildren.Add(ticker);

TimeSensitiveRenderInvalidator.Invalidate(root, TimeSpan.FromMilliseconds(16));
TimeSensitiveRenderInvalidator.Invalidate(root, TimeSpan.FromMilliseconds(16));
Console.WriteLine(ticker.Elapsed.TotalMilliseconds);
// 32

sealed class Ticker : UIElement, ITimeSensitiveRenderElement
{
    public TimeSpan Elapsed { get; private set; }

    public bool UpdateRenderTime(TimeSpan frameTime)
    {
        Elapsed += frameTime;
        return false;
    }
}
```

## Remarks

`TimeSensitiveRenderInvalidator.Invalidate(root, frameTime)` visits every element under `root` and calls `UpdateRenderTime` on each one that implements this interface. It does not skip clean elements. It skips the `Scene` child of a `RenderSurface2D`, because the surface advances its own scene. `UiHost` calls it during every update that advances time.

`frameTime` is the time elapsed since the previous update, not the total time: the host passes the elapsed time given to `Update`, or the host clock's elapsed time, or `TimeSpan.Zero`.

The return value means "my drawing changed". The implementation must invalidate its own render; the invalidator ignores the returned value.

Cerneala implementations:

| Type | Behavior |
| --- | --- |
| `RenderSurface2D` | Advances its active and scheduled sprite animations. Returns `true` when an animation changed, or when `RedrawMode` is continuous and drawing is active. Throws `ArgumentOutOfRangeException` for a negative time. |
| `RenderSurface3D` | Returns `true` and invalidates its frame only when it has a draw handler and `RedrawMode` is continuous. Throws `ArgumentOutOfRangeException` for a negative time. |
| `TextBox`, `PasswordBox` | Advance the caret blink. Return `true` and invalidate render when the caret turns on or off. A zero or negative time is ignored. |

## Methods

| Name | Return Type | Description |
| --- | --- | --- |
| `UpdateRenderTime(TimeSpan frameTime)` | `bool` | Advances time-based state by `frameTime`. Returns `true` when the drawing changed. |

## Applies to

`RenderSurface2D`, `RenderSurface3D`, `TextBox`, and `PasswordBox`.

## See also

- `Cerneala.UI.Rendering.TimeSensitiveRenderInvalidator`
- `Cerneala.UI.Hosting.UiHost`
- `Cerneala.UI.Controls.RenderSurface2D`
