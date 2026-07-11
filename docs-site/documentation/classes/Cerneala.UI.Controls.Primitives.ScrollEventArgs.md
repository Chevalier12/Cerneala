# ScrollEventArgs Class

## Definition
Namespace: `Cerneala.UI.Controls.Primitives`  
Assembly/Project: `Cerneala`  
Source: `UI/Controls/Primitives/ScrollEventArgs.cs`

Routed event arguments for a scroll action and its new value.

```csharp
public sealed class ScrollEventArgs : RoutedEventArgs
```

## Examples
```csharp
void OnScroll(object? sender, ScrollEventArgs args)
{
    float value = args.NewValue;
}
```

## Constructors
| Name | Description |
| --- | --- |
| `ScrollEventArgs(RoutedEvent, object, ScrollEventType, float)` | Creates scroll event args. |

## Properties
| Name | Description |
| --- | --- |
| `ScrollEventType` | Action that caused the event. |
| `NewValue` | New scroll value. |

## Applies to
Scroll bar and scroll viewer routed events.
