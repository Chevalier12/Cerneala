# RoutedEventHandler Delegate

## Definition
Namespace: `Cerneala.UI.Input`

Assembly/Project: `Cerneala`

Source: `UI/Input/UiInputTree.cs`

Represents a method that handles a routed event on one element of the route.

```csharp
public delegate void RoutedEventHandler(object sender, RoutedEventArgs args)
```

## Examples

The same handler runs on the button and on its parent. `sender` is the element whose handler runs. `args.Source` is updated to the same element before each handler runs.

```csharp
using Cerneala.UI.Controls;
using Cerneala.UI.Controls.Primitives;
using Cerneala.UI.Elements;
using Cerneala.UI.Input;

UIRoot root = new(200, 200);
UIElement panel = new();
Button button = new();
root.VisualChildren.Add(panel);
panel.VisualChildren.Add(button);
root.ProcessFrame();

RoutedEventHandler log = (sender, args) =>
    Console.WriteLine(
        $"{sender.GetType().Name} sourceIsSender={ReferenceEquals(args.Source, sender)} originalIsButton={ReferenceEquals(args.OriginalSource, button)}");
panel.AddHandler(ButtonBase.ClickEvent, log);
button.AddHandler(ButtonBase.ClickEvent, log);

button.RaiseEvent(new RoutedEventArgs(ButtonBase.ClickEvent, button));
// Button sourceIsSender=True originalIsButton=True
// UIElement sourceIsSender=True originalIsButton=True
```

## Remarks

`sender` is the element whose handler is running, as an `object`. It changes as the event moves along the route. To get the element that raised the event, read `args.OriginalSource`.

When `UIElement.RaiseEvent` calls only the element's own handlers, because the event is `Direct` or the element has no root, `sender` is that element, and `args.Source` is set to that element too. `args.OriginalSource` keeps the value the caller gave it.

In markup, an event attribute such as `<Button Click="OnSave" />` calls a code-behind method whose first parameter has the exact type of the element: `OnSave(Button sender, RoutedEventArgs args)`. The first parameter may also be a base type of the element (`UIElement` or `object`).

`RoutedEventHandler` receives plain `RoutedEventArgs`. Routed events that raise a more specific type use [RoutedEventHandler&lt;TArgs&gt;](Cerneala.UI.Input.RoutedEventHandler_TArgs_.md) instead, so the handler needs no cast: `MouseDown` is `event RoutedEventHandler<MouseButtonEventArgs>`. Events whose args type is exactly `RoutedEventArgs`, such as `Click`, `Loaded`, and `GotFocus`, use this delegate.

A handler runs only when `args.Handled` is `false`, unless it was added with `handledEventsToo: true`.

Register a handler with `UIElement.AddHandler`, `UiInputTree.AddHandler`, or a CLR event that wraps a routed event, such as `ButtonBase.Click`. Typed CLR events such as `EventHandler<TEventArgs>` use an internal adapter that passes the element object as `sender` instead.

## Parameters

| Name | Type | Description |
| --- | --- | --- |
| `sender` | `object` | The element whose handler is running. Cast it to `UIElement` or the element type. |
| `args` | `RoutedEventArgs` | The routed event data. For an event with a more specific args type, register a `RoutedEventHandler<TArgs>` instead of casting. |

## Applies to

`UIElement.AddHandler`, `UIElement.RemoveHandler`, `UiInputTree.AddHandler`, and routed CLR events.

## See also

- `Cerneala.UI.Input.RoutedEventHandler<TArgs>`
- `Cerneala.UI.Input.RoutedEventArgs`
- `Cerneala.UI.Input.RoutedEvent`
- `Cerneala.UI.Input.RoutedEventRouter`
- `Cerneala.UI.Elements.UIElement`
