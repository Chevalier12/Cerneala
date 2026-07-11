# TextChangedEventArgs Class

## Definition
Namespace: `Cerneala.UI.Controls`  
Assembly/Project: `Cerneala`  
Source: `UI/Controls/TextChangedEventArgs.cs`

Routed event arguments carrying old and new text values.

```csharp
public sealed class TextChangedEventArgs : RoutedEventArgs
```

## Examples
```csharp
void OnTextChanged(object? sender, TextChangedEventArgs args)
{
    string current = args.NewText;
}
```

## Constructors
| Name | Description |
| --- | --- |
| `TextChangedEventArgs(RoutedEvent, object, string, string)` | Creates text-change args. |

## Properties
| Name | Description |
| --- | --- |
| `OldText` | Previous text. |
| `NewText` | Current text. |

## Applies to
TextBox and text-editing routed events.
