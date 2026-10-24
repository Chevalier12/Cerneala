# SizeChangedEventArgs Class

## Definition
Namespace: `Cerneala.UI.Elements`  
Assembly/Project: `Cerneala`  
Source: `UI/Elements/UIElement.Events.cs`

Routed event arguments raised when an element's arranged bounds change, including a move that keeps the same size.

```csharp
public sealed class SizeChangedEventArgs : RoutedEventArgs
```

## Examples
`UIElement.SizeChanged` is a `RoutedEventHandler<SizeChangedEventArgs>`, so the handler receives `SizeChangedEventArgs` without a cast:

```csharp
void OnSizeChanged(object sender, SizeChangedEventArgs args)
{
    Console.WriteLine(args.WidthChanged);
}

element.SizeChanged += OnSizeChanged;
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
