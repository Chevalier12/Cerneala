# RoutedPropertyChangedEventArgs<T> Class

## Definition
Namespace: `Cerneala.UI.Input`  
Assembly/Project: `Cerneala`  
Source: `UI/Input/RoutedPropertyChangedEventArgs.cs`

Routed event arguments carrying an old and new typed property value.

```csharp
public class RoutedPropertyChangedEventArgs<T> : RoutedEventArgs
```

## Examples
```csharp
var args = new RoutedPropertyChangedEventArgs<float>(1, 2);
float delta = args.NewValue - args.OldValue;
```

## Constructors
| Name | Description |
| --- | --- |
| `RoutedPropertyChangedEventArgs(T, T)` | Creates args without a routed-event context. |
| `RoutedPropertyChangedEventArgs(RoutedEvent, object, T, T)` | Creates args with event and source context. |

## Properties
| Name | Description |
| --- | --- |
| `OldValue` | Previous value. |
| `NewValue` | New value. |

## Applies to
Typed routed property-change notifications.
