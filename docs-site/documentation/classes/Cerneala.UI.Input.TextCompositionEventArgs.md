# TextCompositionEventArgs Class

## Definition
Namespace: `Cerneala.UI.Input`  
Assembly/Project: `Cerneala`  
Source: `UI/Input/TextCompositionEventArgs.cs`

Provides routed event data for text composition input.

```csharp
public sealed class TextCompositionEventArgs : RoutedEventArgs
```

Inheritance:
`object` -> `RoutedEventArgs` -> `TextCompositionEventArgs`

## Examples

The following example reads the text payload from a text input routed event handler. `UIElement.TextInput` is a `RoutedEventHandler<TextCompositionEventArgs>`, so the handler needs no cast.

```csharp
using Cerneala.UI.Input;

void OnTextInput(object? sender, TextCompositionEventArgs args)
{
    if (!args.Handled)
    {
        string text = args.Text;
        args.Handled = true;
    }
}

element.TextInput += OnTextInput;
```

## Remarks

`TextCompositionEventArgs` carries the committed text payload for retained UI text input routing. The `Text` property is supplied at construction time and cannot be changed afterward.

`TextInputBridge` creates this type for both `InputEvents.PreviewTextInputEvent` and `InputEvents.TextInputEvent`. The preview event uses tunneling routing, and the text input event uses bubbling routing. If a preview handler sets the inherited `Handled` property, the bubble text input event is still raised with `Handled` set to `true`, so only handlers registered with `handledEventsToo: true` receive it.

`TextBox` and `PasswordBox` consume unhandled text input through their internal editing core, then set `Handled` to `true`.

The constructor throws `ArgumentNullException` when `routedEvent`, `originalSource`, or `text` is `null`.

## Constructors

| Name | Description |
| --- | --- |
| `TextCompositionEventArgs(RoutedEvent routedEvent, object originalSource, string text)` | Initializes a new instance for the specified routed event, original source, and text payload. |

## Properties

| Name | Type | Description |
| --- | --- | --- |
| `Text` | `string` | Gets the text payload associated with the composition input event. |
| `RoutedEvent` | `RoutedEvent` | Gets or sets the routed event associated with these arguments. Inherited from `RoutedEventArgs`. |
| `OriginalSource` | `object` | Gets the original source passed to the routed event arguments. Inherited from `RoutedEventArgs`. |
| `Source` | `object` | Gets or sets the current source while routing. Inherited from `RoutedEventArgs`. |
| `Handled` | `bool` | Gets or sets whether routing should treat the event as handled. Inherited from `RoutedEventArgs`. |

## Routed Event Usage

| Event | Routing strategy | Args type |
| --- | --- | --- |
| `InputEvents.PreviewTextInputEvent` | `Tunnel` | `TextCompositionEventArgs` |
| `InputEvents.TextInputEvent` | `Bubble` | `TextCompositionEventArgs` |

## Applies to

Cerneala retained UI text input routing.

## See also

- `InputEvents`
- `TextInputBridge`
- `RoutedEventArgs`
- `RoutedEvent`
