# ElementIdProvider Class

## Definition
Namespace: `Cerneala.UI.Elements`

Assembly/Project: `Cerneala`

Source: `UI/Elements/ElementIdProvider.cs`

Assigns stable `UiElementId` values to `UIElement` instances and keeps bidirectional lookup between elements and ids.

```csharp
public sealed class ElementIdProvider
```

## Examples

```csharp
using Cerneala.UI.Elements;

ElementIdProvider provider = new();
UIElement element = new();

var id = provider.GetOrCreate(element);

if (provider.TryGetElement(id, out UIElement? resolvedElement))
{
    // resolvedElement is the same UIElement instance.
}

provider.Release(element);
```

## Remarks

`ElementIdProvider` is used by `UIRoot.ElementIds` to assign ids as elements attach to a retained UI tree. `ElementLifecycle` calls `GetOrCreate` during attach and `Release` during detach.

Ids are created as `UiElementId` values with sequential string values such as `ui-1`, `ui-2`, and so on. Calling `GetOrCreate` again for the same `UIElement` instance returns the existing id instead of allocating a new one.

Element lookup uses reference equality for `UIElement` keys. Two different element instances receive different ids even if their values compare as equal.

`Release` removes both lookup directions for an element. If the element is not registered, the method returns without changing the provider.

## Constructors

| Name | Description |
| --- | --- |
| `ElementIdProvider()` | Initializes an empty provider. |

## Methods

| Name | Description |
| --- | --- |
| `GetOrCreate(UIElement)` | Returns the existing id for an element or creates a new sequential `UiElementId`. Throws `ArgumentNullException` when `element` is `null`. |
| `TryGetElement(UiElementId, out UIElement?)` | Looks up the element registered for an id. |
| `TryGetId(UIElement, out UiElementId)` | Looks up the id registered for an element. Throws `ArgumentNullException` when `element` is `null`. |
| `Release(UIElement)` | Removes an element and its id from the provider. Throws `ArgumentNullException` when `element` is `null`. |

## Applies to

Cerneala retained UI element lifecycle and input identity mapping.

## See also

- `Cerneala.UI.Elements.UIElement`
- `Cerneala.UI.Elements.UIRoot`
- `Cerneala.UI.Elements.ElementLifecycle`
- `Cerneala.UI.Input.UiElementId`
