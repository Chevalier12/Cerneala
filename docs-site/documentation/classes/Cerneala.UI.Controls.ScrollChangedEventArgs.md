# ScrollChangedEventArgs Class

## Definition
Namespace: `Cerneala.UI.Controls`  
Assembly/Project: `Cerneala`  
Source: `UI/Controls/ScrollChangedEventArgs.cs`

Routed event arguments describing old, new, and delta scroll offsets.

```csharp
public sealed class ScrollChangedEventArgs : RoutedEventArgs
```

## Examples
```csharp
void OnScroll(object? sender, ScrollChangedEventArgs args)
{
    float delta = args.HorizontalChange;
}
```

## Constructors
| Name | Description |
| --- | --- |
| `ScrollChangedEventArgs(RoutedEvent, object, float, float, float, float)` | Creates scroll-change args. |

## Properties
| Name | Description |
| --- | --- |
| `OldHorizontalOffset`, `OldVerticalOffset` | Previous offsets. |
| `HorizontalOffset`, `VerticalOffset` | Current offsets. |
| `HorizontalChange`, `VerticalChange` | Calculated deltas. |

## Applies to
Scroll viewer routed events.
