# IInputActivatable Interface

## Definition
Namespace: `Cerneala.UI.Input`  
Assembly/Project: `Cerneala`  
Source: `UI/Input/IInputActivatable.cs`

Contract for controls that can be activated by an input bridge.

```csharp
public interface IInputActivatable
```

## Examples
```csharp
if (element is IInputActivatable activatable)
{
    activatable.Activate();
}
```

## Methods
| Name | Description |
| --- | --- |
| `Activate()` | Performs the control's input activation. |

## Applies to
Keyboard, pointer, and command-driven control activation.
