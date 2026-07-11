# WindowClosingEventArgs Class

## Definition
Namespace: `Cerneala.UI.Controls`  
Assembly/Project: `Cerneala`  
Source: `UI/Controls/WindowClosingEventArgs.cs`

Event arguments used to cancel a window close request.

```csharp
public sealed class WindowClosingEventArgs : EventArgs
```

## Examples
```csharp
void OnClosing(object? sender, WindowClosingEventArgs args)
{
    args.Cancel = HasUnsavedChanges;
}
```

## Properties
| Name | Description |
| --- | --- |
| `Cancel` | Set to `true` to prevent closing. |

## Applies to
Window close lifecycle events.
