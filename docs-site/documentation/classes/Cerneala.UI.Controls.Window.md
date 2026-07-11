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

## Applies to
Windows desktop hosting.
