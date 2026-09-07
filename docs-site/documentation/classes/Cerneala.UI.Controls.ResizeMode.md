# ResizeMode Enum

## Definition
Namespace: `Cerneala.UI.Controls`  
Assembly/Project: `Cerneala`  
Source: `UI/Controls/ResizeMode.cs`

Controls which native window resize operations are available.

```csharp
public enum ResizeMode
```

## Members
| Name | Description |
| --- | --- |
| `NoResize` | Disables resizing. |
| `CanMinimize` | Allows minimizing. |
| `CanResize` | Allows native resizing. |
| `CanResizeWithGrip` | Allows native resizing and exposes a lower-right resize grip hit target in the client area. |

## Remarks
The SDL host maps these values to native window capabilities. `CanResize` supports resizing through the native frame. On Windows, `CanResizeWithGrip` additionally uses SDL hit-testing to map the system-sized lower-right client corner to `HTBOTTOMRIGHT` while the window is in its normal state, giving that area the standard diagonal resize cursor and drag behavior. Maximized and minimized windows do not expose the client grip. On other platforms, `CanResizeWithGrip` currently has the same behavior as `CanResize`.

## Applies to
`Window.ResizeMode`.
