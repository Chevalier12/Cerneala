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
    WindowStartupLocation = WindowStartupLocation.CenterScreen
};
```

## Remarks
Window dimensions and constraints participate in measure; native state and resize settings are translated by the Windows hosting backend. Closing can be cancelled through the closing event args.

The window renders its configured background and border before its content. `LastFrame` is assigned immediately after a frame is presented. The `FrameRendered` event is then raised on the window's UI thread, allowing diagnostic UI to inspect the completed frame without reaching into the hosting runtime.

`FrameRendered` and `ContentRendered` run inside the window relay synchronization context. Asynchronous event handlers therefore resume through the owning UI relay after an `await`, rather than continuing on an arbitrary thread-pool thread.

`SaveScreenshot` redraws the current retained command tree into a complete client framebuffer before encoding the PNG. This keeps captures complete even when the displayed frame is changing continuously under Motion. Call it from `FrameRendered` when the capture must correspond to a specific frame boundary.

## Properties
| Name | Description |
| --- | --- |
| `Title` | Native window title. |
| `Width`, `Height` | Requested dimensions. |
| `MinWidth`, `MinHeight`, `MaxWidth`, `MaxHeight` | Dimension constraints. |
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
| `SaveScreenshot(string path)` | Draws the current retained client frame and saves it as a PNG. |

## Applies to
Windows desktop hosting.
