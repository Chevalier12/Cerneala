# TemplateRecyclePool Class

## Definition

Namespace: `Cerneala.UI.Controls.Templates`

Assembly/Project: `Cerneala`

Source: `UI/Controls/Templates/TemplateRecyclePool.cs`

Stores recyclable template-created UI elements in stacks keyed by `TemplateRecycleKey`.

```csharp
public sealed class TemplateRecyclePool
```

Inheritance:
`object` -> `TemplateRecyclePool`

## Examples

Release a content presenter and rent it again for a matching template recycle key:

```csharp
using Cerneala.UI.Controls;
using Cerneala.UI.Controls.Templates;
using Cerneala.UI.Elements;

TemplateRecyclePool pool = new();
TemplateRecycleKey key = new(typeof(string), typeof(ContentPresenter), "item");

ContentPresenter presenter = new()
{
    Content = "old",
    ContentTemplateKey = "old-template"
};

pool.Release(key, presenter);

UIElement? rented = pool.Rent(key);
ContentPresenter reused = (ContentPresenter)rented!;

// ContentPresenter-specific state is cleared when the element is released.
object? content = reused.Content;
string? templateKey = reused.ContentTemplateKey;
```

## Remarks

`TemplateRecyclePool` is a small retained-UI helper for virtualized or template-driven surfaces that need to reuse previously created elements. Elements are grouped by `TemplateRecycleKey`, which includes the data type, container type, and optional slot name.

Each key owns a last-in, first-out stack. `Release(TemplateRecycleKey, UIElement)` stores an element in the stack for the supplied key. `Rent(TemplateRecycleKey)` removes and returns the most recently released element for that key, or returns `null` when the key has no available element.

Before storing an element, `Release` resets `ContentPresenter` instances by clearing `Content`, `ContentTemplate`, `ContentTemplate`, and `ContentTemplateKey`, and by setting `ContentIndex` to `-1`. Other `UIElement` types are stored without additional reset behavior.

The pool does not verify that `key.ContainerType` matches the runtime type of the released element. Callers are responsible for using stable keys that match the element category they intend to recycle.

## Constructors

| Name | Description |
| --- | --- |
| `TemplateRecyclePool()` | Creates an empty template recycle pool. |

## Methods

| Name | Return Type | Description |
| --- | --- | --- |
| `Release(TemplateRecycleKey key, UIElement element)` | `void` | Resets supported template state on `element` and pushes it onto the stack for `key`. |
| `Rent(TemplateRecycleKey key)` | `UIElement?` | Pops and returns the most recently released element for `key`, or `null` when no element is available. |

## Exceptions

| Member | Exception | Condition |
| --- | --- | --- |
| `Release(TemplateRecycleKey key, UIElement element)` | `ArgumentNullException` | `key` or `element` is `null`. |
| `Rent(TemplateRecycleKey key)` | `ArgumentNullException` | `key` is `null`. |

## Applies To

`Cerneala` retained UI template and item-content recycling.

## See Also

- `Cerneala.UI.Controls.Templates.TemplateRecycleKey`
- `Cerneala.UI.Controls.ContentPresenter`
- `Cerneala.UI.Controls.Templates.ContentTemplate`
- `Cerneala.UI.Controls.Templates.ContentTemplateRegistry`
