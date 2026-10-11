# RoutedEventHandler Delegate

## Definition
Namespace: `Cerneala.UI.Input`

Assembly/Project: `Cerneala`

Source: `UI/Input/UiInputTree.cs`

Represents a method that handles a routed event on one element of the route.

```csharp
public delegate void RoutedEventHandler(UiElementId sender, RoutedEventArgs args)
```

## Examples

The same handler runs on the button and on its parent. `sender` is the id of the element whose handler runs. `args.Source` is updated to the same element before each handler runs.

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
    Console.WriteLine($"{sender} source={args.Source} original={args.OriginalSource}");
panel.AddHandler(ButtonBase.ClickEvent, log);
button.AddHandler(ButtonBase.ClickEvent, log);

button.RaiseEvent(new RoutedEventArgs(ButtonBase.ClickEvent, button));
// ui-3 source=ui-3 original=Cerneala.UI.Controls.Button
// ui-2 source=ui-2 original=Cerneala.UI.Controls.Button
```

## Remarks

`sender` is a `UiElementId`, not the element object. It identifies the element whose handler is running, so it changes as the event moves along the route. To get the element that raised the event, read `args.OriginalSource`.

When `UIElement.RaiseEvent` calls only the element's own handlers, because the event is `Direct` or the element has no root or no id yet, `sender` is the element's `ElementId`. When the element has no id, `sender` is a new `UiElementId` made from the element's type name, for example `Button`.

A handler runs only when `args.Handled` is `false`, unless it was added with `handledEventsToo: true`.

Register a handler with `UIElement.AddHandler`, `UiInputTree.AddHandler`, or a CLR event that wraps a routed event, such as `ButtonBase.Click`. Typed CLR events such as `EventHandler<TEventArgs>` use an internal adapter that passes the element object as `sender` instead.

## Parameters

| Name | Type | Description |
| --- | --- | --- |
| `sender` | `UiElementId` | The id of the route element whose handler is running. |
| `args` | `RoutedEventArgs` | The routed event data. Cast it to the event's argument type, for example `MouseButtonEventArgs`. |

## Applies to

`UIElement.AddHandler`, `UIElement.RemoveHandler`, `UiInputTree.AddHandler`, and routed CLR events.

## See also

- `Cerneala.UI.Input.RoutedEventArgs`
- `Cerneala.UI.Input.RoutedEvent`
- `Cerneala.UI.Input.RoutedEventRouter`
- `Cerneala.UI.Input.UiElementId`
