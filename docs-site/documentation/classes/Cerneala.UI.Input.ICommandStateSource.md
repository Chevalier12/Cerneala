# ICommandStateSource Interface

## Definition
Namespace: `Cerneala.UI.Input`

Assembly/Project: `Cerneala`

Source: `UI/Input/ICommandStateSource.cs`

Marks an element that updates its own state from a command during the command-state phase of a frame.

```csharp
public interface ICommandStateSource
```

## Examples

The refresh does not run every frame. It runs once for each queued request:

```csharp
using Cerneala.UI.Controls;
using Cerneala.UI.Elements;
using Cerneala.UI.Input;

UIRoot root = new(200, 200);
StateProbe probe = new();
root.VisualChildren.Add(probe);
root.ProcessFrame();
Console.WriteLine(probe.Refreshes); // 0, attaching did not queue a refresh

probe.QueueCommandStateRefresh();
root.ProcessFrame();
Console.WriteLine(probe.Refreshes); // 1

root.ProcessFrame();
Console.WriteLine(probe.Refreshes); // 1, nothing new was queued

sealed class StateProbe : Control, ICommandStateSource
{
    public int Refreshes { get; private set; }

    public bool RefreshCommandState(CommandRouter router, ElementInputRouteMap routeMap)
    {
        Refreshes++;
        return false;
    }
}
```

## Remarks

`UIElement.QueueCommandStateRefresh` adds the element to the root's command-state queue only when the element implements `ICommandStateSource`; on other elements it does nothing. When the element has no root yet, the request is remembered and queued when the element is attached.

During `UIRoot.ProcessFrame`, the command-state phase calls `RefreshCommandState` once for each queued element. The `routeMap` comes from `ElementInputRouteBuilder.BuildForCommandState` and is built at most once per frame. The `router` is a new `CommandRouter` for the frame, which a `RoutedCommand` needs to find its `CommandBinding`. The frame ignores the returned value.

`ButtonBase` and `MenuItem` implement this interface. They queue a refresh when they are attached, when `Command` or `CommandParameter` changes, and when an `IObservableCommand` raises `CanExecuteChanged`. Their refresh sets `IsEnabled` to the command's `CanExecute(CommandParameter)` and returns `true` only when `IsEnabled` changed. With a `null` command they return `false` and leave `IsEnabled` alone.

For a `RoutedCommand`, their refresh also returns `false` and leaves `IsEnabled` alone when the element is not in `routeMap`, because the command cannot reach its `CommandBinding` from there. That happens while the element takes no part in input: it is collapsed, `Hidden`, has `IsVisible` set to `false`, is under such an ancestor, or is skipped by virtualization. The root then refreshes the element again after the next change of the element tree or of the input route, such as a `Visibility` or `IsVisible` change; an element still outside the route waits again. So a button that was collapsed during its refresh gets its command's `CanExecute` in the first frame processed after it becomes visible:

```csharp
using Cerneala.UI.Controls.Primitives;
using Cerneala.UI.Elements;
using Cerneala.UI.Input;
using Cerneala.UI.Layout;

UIRoot root = new(200, 200);
ButtonBase save = new() { Visibility = Visibility.Collapsed };
root.VisualChildren.Add(save);
RoutedCommand command = new("Save", typeof(ButtonBase));
root.CommandBindings.Add(new CommandBinding(command, null, (_, args) =>
{
    args.CanExecute = false;
    args.Handled = true;
}));

save.Command = command;
root.ProcessFrame();
Console.WriteLine(save.IsEnabled); // True, the collapsed button kept its value

save.Visibility = Visibility.Visible;
root.ProcessFrame();
Console.WriteLine(save.IsEnabled); // False, the binding's CanExecute
```

## Methods

| Name | Return Type | Description |
| --- | --- | --- |
| `RefreshCommandState(CommandRouter router, ElementInputRouteMap routeMap)` | `bool` | Updates the element from its command. Return `true` when the element's state changed. |

## Applies to

`ButtonBase`, `MenuItem`, and custom elements that react to a command.

## See also

- `Cerneala.UI.Input.ICommand`
- `Cerneala.UI.Input.IObservableCommand`
- `Cerneala.UI.Input.CommandRouter`
- `Cerneala.UI.Input.IInputCommandSource`
