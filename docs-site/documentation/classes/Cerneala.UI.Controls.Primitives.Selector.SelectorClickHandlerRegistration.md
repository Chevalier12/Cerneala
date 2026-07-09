# Selector.SelectorClickHandlerRegistration Class

## Definition
Namespace: `Cerneala.UI.Controls.Primitives`

Assembly/Project: `Cerneala`

Source: `UI/Controls/Primitives/Selector.cs`

Stores the mouse-up handler state that connects a realized selector item container back to the `Selector` instance that currently owns it.

```csharp
private sealed class SelectorClickHandlerRegistration
```

Inheritance:
`object` -> `Selector.SelectorClickHandlerRegistration`

Declaring type:
`Selector`

Accessibility:
`private`

## Examples

`SelectorClickHandlerRegistration` is an implementation detail of `Selector`; callers do not create it directly. `Selector.PrepareItemContainer` creates one registration per `UIElement` container through a `ConditionalWeakTable`, attaches its `OnMouseUp` method to `InputEvents.MouseUpEvent`, and then updates the active owner.

```csharp
SelectorClickHandlerRegistration registration = clickHandlerRegistrations.GetValue(
    container,
    static key =>
    {
        SelectorClickHandlerRegistration created = new(key);
        key.Handlers.AddHandler(InputEvents.MouseUpEvent, created.OnMouseUp);
        return created;
    });

registration.Owner = this;
```

## Remarks

`SelectorClickHandlerRegistration` keeps a reference to the item container that raised the mouse event and an `Owner` reference to the `Selector` that currently prepared that container. When `OnMouseUp` receives a left-button `MouseButtonEventArgs` and `Owner` is not `null`, it asks the owner to select the stored container.

The registration is stored per container rather than per preparation pass. This keeps repeated selection invalidation from adding duplicate `MouseUp` handlers to the same container. When a container is cleared by the same selector that owns the registration, `Selector.ClearItemContainer` sets `Owner` to `null`; if the container is later reused by another selector, preparation updates `Owner` to the new selector.

The handler ignores the routed sender id and does not mark the event as handled. Non-left mouse button events and non-`MouseButtonEventArgs` routed events do not change selection.

## Constructors

| Name | Description |
| --- | --- |
| `SelectorClickHandlerRegistration(UIElement container)` | Initializes the registration with the item container that should be selected when a valid left-button mouse-up event is received. |

## Properties

| Name | Type | Description |
| --- | --- | --- |
| `Owner` | `Selector?` | Gets or sets the selector that currently owns the registered container. A `null` owner disables selection for the handler. |

## Methods

| Name | Return Type | Description |
| --- | --- | --- |
| `OnMouseUp(UiElementId _, RoutedEventArgs args)` | `void` | Handles `InputEvents.MouseUpEvent`; selects the stored container only when `Owner` is set and `args` is a left-button `MouseButtonEventArgs`. |

## Applies to

Project: `Cerneala`

## See also

- `Selector`
- `SelectionModel`
- `InputEvents.MouseUpEvent`
