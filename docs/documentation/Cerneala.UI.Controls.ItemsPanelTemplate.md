# ItemsPanelTemplate Class

## Definition

Namespace: `Cerneala.UI.Controls`

Assembly/Project: `Cerneala`

Source: `UI/Controls/ItemsPanelTemplate.cs`

Stores a factory that creates the panel used by an items presenter or items control to host realized item elements.

```csharp
public sealed class ItemsPanelTemplate
```

Inheritance:
`object` -> `ItemsPanelTemplate`

## Examples

Use a vertical stack panel as the items panel for an `ItemsControl`:

```csharp
using Cerneala.UI.Controls;
using Cerneala.UI.Layout.Panels;

ItemsControl itemsControl = new()
{
    ItemsPanel = new ItemsPanelTemplate(() => new StackPanel())
};
```

Create a controls-facing panel directly from a template:

```csharp
using Cerneala.UI.Controls;

ItemsPanelTemplate template = new(() => new Panel());

Panel panel = template.CreatePanel();
```

Use a virtualizing stack panel with an `ItemsPresenter`:

```csharp
using Cerneala.UI.Controls;
using Cerneala.UI.Layout.Panels;
using Cerneala.UI.Layout.Virtualization;

ItemsPresenter presenter = new()
{
    Items = new[] { "zero", "one", "two" },
    ItemsPanel = new ItemsPanelTemplate(() => new VirtualizingStackPanel()),
    VirtualizationContext = new VirtualizationContext(itemCount: 3, itemExtent: 20, viewportExtent: 40, offset: 0)
};
```

## Remarks

`ItemsPanelTemplate` is a small factory wrapper. The constructor stores a `Func<Cerneala.UI.Layout.Panels.Panel>` and rejects a `null` factory. Each refresh of `ItemsPresenter` asks the active template for a new layout panel, then adds the realized item elements as logical and visual children of that panel.

`ItemsPresenter` uses its own `ItemsPanel` when set. When it is owned by an `ItemsControl`, it falls back to `ItemsOwner.ItemsPanel`; otherwise it falls back to an internal default template that creates `Cerneala.UI.Controls.Panel`. `ItemsControl.ItemsPanel` and `ItemsPresenter.ItemsPanel` are UI properties whose metadata affects measure and render.

The factory may return layout-facing panel types such as `Cerneala.UI.Layout.Panels.StackPanel` or `VirtualizingStackPanel` for use by the items pipeline. The public `CreatePanel()` method is stricter: it returns `Cerneala.UI.Controls.Panel` and throws if the factory returns a layout panel that is not the controls-facing `Panel` type. Internally, `ItemsPresenter` uses the layout-panel creation path so specialized layout panels can still be used as items panels.

Factories must create and return a non-null panel instance. Returning `null` throws `InvalidOperationException`.

## Constructors

| Name | Description |
| --- | --- |
| `ItemsPanelTemplate(Func<Cerneala.UI.Layout.Panels.Panel> factory)` | Initializes a template with the factory used to create items panels. |

## Methods

| Name | Return Type | Description |
| --- | --- | --- |
| `CreatePanel()` | `Panel` | Creates a controls-facing `Panel` from the stored factory, or throws when the factory returns `null` or a non-controls-facing layout panel. |

## Exceptions

| Member | Exception | Condition |
| --- | --- | --- |
| `ItemsPanelTemplate(Func<Cerneala.UI.Layout.Panels.Panel> factory)` | `ArgumentNullException` | `factory` is `null`. |
| `CreatePanel()` | `InvalidOperationException` | The factory returns `null`. |
| `CreatePanel()` | `InvalidOperationException` | The factory returns a `Cerneala.UI.Layout.Panels.Panel` that is not a `Cerneala.UI.Controls.Panel`. |

## Applies To

Project: `Cerneala`

## See Also

- `UI/Controls/ItemsControl.cs`
- `UI/Controls/ItemsPresenter.cs`
- `UI/Controls/Panel.cs`
- `UI/Layout/Panels/Panel.cs`
- `UI/Layout/Panels/StackPanel.cs`
- `UI/Layout/Panels/VirtualizingStackPanel.cs`
