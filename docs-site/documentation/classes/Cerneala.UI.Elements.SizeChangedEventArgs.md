# SizeChangedEventArgs Class

## Definition
Namespace: `Cerneala.UI.Elements`  
Assembly/Project: `Cerneala`  
Source: `UI/Elements/UIElement.Events.cs`

Routed event arguments for an element's arranged-size change.

```csharp
public sealed class SizeChangedEventArgs : RoutedEventArgs
```

## Examples
```csharp
void OnSizeChanged(object sender, RoutedEventArgs args)
{
    var change = (SizeChangedEventArgs)args;
    Console.WriteLine(change.WidthChanged);
}
```

## Constructors
| Name | Description |
| --- | --- |
| `SizeChangedEventArgs(RoutedEvent, object, LayoutSize, LayoutSize)` | Creates size-change args. |

## Properties
| Name | Description |
| --- | --- |
| `PreviousSize` | Size before arrange. |
| `NewSize` | New arranged size. |
| `WidthChanged` | Whether width differs. |
| `HeightChanged` | Whether height differs. |

## Applies to
`UIElement.SizeChanged` routed events.
