# WindowStartupLocation Enum

## Definition
Namespace: `Cerneala.UI.Controls`  
Assembly/Project: `Cerneala`  
Source: `UI/Controls/WindowStartupLocation.cs`

Selects the initial native window placement.

```csharp
public enum WindowStartupLocation
```

## Members
| Name | Description |
| --- | --- |
| `Manual` | Uses the configured position. |
| `CenterScreen` | Centers on the screen. |
| `CenterOwner` | Centers relative to the owner window. |

## Remarks
`Manual` leaves placement to the configured position and native host.

## Applies to
`Window.WindowStartupLocation`.
