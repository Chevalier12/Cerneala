# RoutedEventHandler<TArgs> Delegate

## Definition
Namespace: `Cerneala.UI.Input`

Assembly/Project: `Cerneala`

Source: `UI/Input/UiInputTree.cs`

Represents a method that handles a routed event on one element of the route and receives the event data as `TArgs`, so the handler needs no cast.

```csharp
public delegate void RoutedEventHandler<in TArgs>(object sender, TArgs args)
    where TArgs : RoutedEventArgs;
```

Type parameters:

| Name | Description |
| --- | --- |
| `TArgs` | The event data type. It is `in` (contravariant) and must derive from `RoutedEventArgs`. |

## Examples

A `MouseDown` handler reads `ChangedButton` directly. The compiler infers `MouseButtonEventArgs` from the event.

```csharp
using Cerneala.UI.Elements;
using Cerneala.UI.Input;

UIElement element = new();

element.MouseDown += (sender, args) => Console.WriteLine(args.ChangedButton);

element.RaiseEvent(new MouseButtonEventArgs(
    UIElement.MouseDownEvent,
    element,
    InputMouseButton.Right,
    x: 0,
    y: 0,
    clickCount: 1));
// Right
```

Register a typed handler for a routed event identifier with `UIElement.AddHandler<TArgs>`. `MouseDownEvent` raises `MouseButtonEventArgs`, so `TArgs` can be `MouseButtonEventArgs`, `MouseEventArgs`, or `RoutedEventArgs`:

```csharp
RoutedEventHandler<MouseEventArgs> onMouse = (sender, args) => Console.WriteLine($"{args.X}, {args.Y}");

element.AddHandler(UIElement.MouseDownEvent, onMouse);
element.RemoveHandler(UIElement.MouseDownEvent, onMouse);
```

`element.AddHandler<KeyEventArgs>(UIElement.MouseDownEvent, (_, _) => { })` throws `ArgumentException`, because `MouseDownEvent` never raises `KeyEventArgs`.

## Remarks

`RoutedEventHandler<TArgs>` is the typed form of [RoutedEventHandler](Cerneala.UI.Input.RoutedEventHandler.md). The two delegates differ only in the type of `args`. `sender` is the element whose handler runs, as an `object`, in both.

Routed CLR events whose `RoutedEvent` raises a type more specific than `RoutedEventArgs` are declared with this delegate and that exact type, for example `MouseDown` is `event RoutedEventHandler<MouseButtonEventArgs>` and `KeyDown` is `event RoutedEventHandler<KeyEventArgs>`. Events whose args type is exactly `RoutedEventArgs`, such as `Click`, `Loaded`, and `GotFocus`, keep `RoutedEventHandler`.

A registered handler is stored as a `RoutedEventHandler` adapter that casts `args` to `TArgs`. The cast cannot fail for a registered handler: `UIElement.AddHandler<TArgs>` refuses an event whose `ArgsType` is not assignable to `TArgs`, and both `UIElement.RaiseEvent` and `RoutedEventRouter.Raise` refuse args that are not the event's `ArgsType`.

Because `TArgs` is contravariant, a `RoutedEventHandler<RoutedEventArgs>` can be used where a `RoutedEventHandler<MouseButtonEventArgs>` is expected.

In markup, the code-behind method named by an event attribute takes the element as its first parameter and the event data as its second. `<Sprite2D MouseDown="OnOpen" />` binds `OnOpen(Sprite2D sender, MouseButtonEventArgs args)`. The second parameter may be the event's args type or a base type such as `RoutedEventArgs`. Any other type, such as `KeyEventArgs` for `MouseDown`, is rejected with `CERNEALAUI009`.

## Parameters

| Name | Type | Description |
| --- | --- | --- |
| `sender` | `object` | The element whose handler is running. Cast it to `UIElement` or the element type. |
| `args` | `TArgs` | The routed event data, already of the event's args type. |

## Applies to

`UIElement.AddHandler<TArgs>`, `UIElement.RemoveHandler<TArgs>`, and the routed CLR events of `UIElement` and `Control` whose args type is more specific than `RoutedEventArgs`.

## See also

- `Cerneala.UI.Input.RoutedEventHandler`
- `Cerneala.UI.Input.RoutedEventArgs`
- `Cerneala.UI.Input.RoutedEvent`
- `Cerneala.UI.Input.RoutedEventRouter`
- `Cerneala.UI.Elements.UIElement`
