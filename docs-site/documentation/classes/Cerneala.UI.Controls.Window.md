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

`LastFrame` is assigned immediately after a frame is presented. The `FrameRendered` event is then raised on the window's UI thread, allowing diagnostic UI to inspect the completed frame without reaching into the hosting runtime.

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

## Applies to
Windows desktop hosting.
