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
| `CanResizeWithGrip` | Allows resizing with a resize grip. |

## Remarks
The Windows hosting backend maps these values to native window styles.

## Applies to
`Window.ResizeMode`.
