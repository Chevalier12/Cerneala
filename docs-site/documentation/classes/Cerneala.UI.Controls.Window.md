# Window Class

## Definition
Namespace: `Cerneala.UI.Controls`  
Assembly/Project: `Cerneala`  
Source: `UI/Controls/Window.cs`

Top-level desktop window control with native size, position, state, and lifecycle properties.

```csharp
public class Window : ContentControl
```

## Examples
```csharp
var window = new Window
{
    Title = "Cerneala",
    Width = 1024,
    Height = 768,
    Focusable = true,
    WindowStartupLocation = WindowStartupLocation.CenterScreen
};
```

## Remarks
Window dimensions and constraints participate in measure; native state and resize settings are translated by the SDL hosting layer. Closing can be cancelled through the closing event args.

After a native viewport size change, the window and stretching content are arranged against the new viewport in the same UI frame. A retained `RenderSurface3D` child therefore receives its new raster size when its arranged bounds change; resizing the window is not a camera mutation.

`MaxWidth` and `MaxHeight` constrain the client area, including while the window is maximized. Both default to positive infinity, which means no explicit upper limit. Maximization does not temporarily remove a finite limit; a constrained maximized window may therefore be smaller than the monitor's work area.

Each maximum is independent: a finite `MaxWidth` still applies when `MaxHeight` is positive infinity, and vice versa. The Windows native runtime enforces this for bordered and borderless windows.

When both `Left` and `Top` are finite, that explicit position takes precedence over automatic startup centering. Maximizing or minimizing preserves the model's normal restore position instead of replacing it with the native maximized or minimized coordinates.

`Width` and `Height` follow a user resize, as `Left` and `Top` follow a user move. Example: a window created with `Width = 640` and `Height = 480`, which the user drags to 900x700 at display scale 1, then has `Width == 900` and `Height == 700`. The values are logical units: at display scale 1.25 the same drag reports 720x560. Setting `Title`, `Topmost`, `ShowInTaskbar`, or any other window property afterwards keeps the 900x700 size; only a new `Width` or `Height` value resizes the native window. While the window is maximized or minimized, `Width` and `Height` keep the normal size the window is restored to.

`ShowDialogAsync` returns a task that completes with `DialogResult` when the dialog closes. This also holds when the dialog closes during its first frame, for example when a `ContentRendered` handler sets `DialogResult`: the returned task is then already completed. A `Closed` handler that throws does not stop the close: the window is closed, the dialog task completes with `DialogResult`, the application's shutdown policy sees the close, and then the handler's exception propagates from the call that closed the window, such as `Close` or the `DialogResult` setter.

On Windows, the host preserves the application icon supplied by SDL. If neither the native window nor its class has an application icon, the host supplies the standard Windows application icon.

The window renders its configured background and border before its content. `LastFrame` is assigned immediately after a frame is presented. The `FrameRendered` event is then raised on the window's UI thread, allowing diagnostic UI to inspect the completed frame without reaching into the hosting runtime.

`FrameRendered` and `ContentRendered` run inside the window relay synchronization context. Asynchronous event handlers therefore resume through the owning UI relay after an `await`, rather than continuing on an arbitrary thread-pool thread.

`SaveScreenshot` redraws the current retained command tree into a complete client framebuffer before encoding the PNG. This keeps captures complete even when the displayed frame is changing continuously under Motion. Call it from `FrameRendered` when the capture must correspond to a specific frame boundary.

The capture is the last committed frame, which is the frame that was just presented. A change made before the call, in the same `FrameRendered` handler, is not in it yet: layout and the retained commit run in the next frame. For example, `status.Text = "changed"; SaveScreenshot("b.png");` in a `FrameRendered` handler saves the previous text, and a capture from a later `FrameRendered` shows `changed`. The PNG is encoded in memory and the file is written only when the capture succeeds, so a failed capture leaves no new or truncated file at `path`, and an existing file there keeps its content.

When the native platform activates a window and no retained element already has keyboard focus, a window with `Focusable` set to `true` receives keyboard focus automatically. Activation preserves an existing focused child instead of replacing it with the window.

## Properties
| Name | Description |
| --- | --- |
| `Title` | Native window title. |
| `Width`, `Height` | Client-area size in logical units. Setting them resizes the native window; a user resize writes the new normal size back to them. |
| `MinWidth`, `MinHeight`, `MaxWidth`, `MaxHeight` | Client-area dimension constraints; maximum limits also apply when maximized. They hide the `UIElement` layout limits of the same names and are separate properties: for `UIElement element = new Window { MinWidth = 640 }`, `element.MinWidth` is `0`. |
| `Left`, `Top` | Requested screen position. |
| `WindowState` | Normal, minimized, or maximized state. |
| `ResizeMode` | Native resize policy. |
| `WindowStartupLocation` | Initial placement policy. |
| `LastFrame` | Most recently presented `UiFrame`, or `null` before the first frame. |

## Events
| Name | Description |
| --- | --- |
| `FrameRendered` | Raised after each frame is presented and `LastFrame` has been updated. |
| `ContentRendered` | Raised once after the first frame is presented. |

## Methods
| Name | Description |
| --- | --- |
| `SaveScreenshot(string path)` | Draws the last committed retained client frame and saves it as a PNG. |

## Applies to
Desktop window hosting backends.
