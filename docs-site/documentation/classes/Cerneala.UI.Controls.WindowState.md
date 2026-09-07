# WindowState Enum

## Definition
Namespace: `Cerneala.UI.Controls`  
Assembly/Project: `Cerneala`  
Source: `UI/Controls/WindowState.cs`

Describes the native display state of a window.

```csharp
public enum WindowState
```

## Members
| Name | Description |
| --- | --- |
| `Normal` | Restored window. |
| `Minimized` | Minimized window. |
| `Maximized` | Maximized window. |

## Remarks
State changes are applied by the native window hosting layer. `Maximized` does not override a window's finite `MaxWidth` or `MaxHeight`: those client-area limits remain in effect, so the maximized window may be smaller than the monitor's work area.

The limits apply independently, including when only one maximum is finite. See [Window](Cerneala.UI.Controls.Window.md).

## Applies to
`Window.WindowState`.
