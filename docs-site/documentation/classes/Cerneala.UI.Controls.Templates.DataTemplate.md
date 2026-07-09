# DataTemplate Class

## Definition

Namespace: `Cerneala.UI.Controls.Templates`

Assembly/Project: `Cerneala`

Source: `UI/Controls/Templates/DataTemplate.cs`

Represents the non-generic base class for classic data templates that validate a data value and create a retained `UIElement`.

```csharp
public abstract class DataTemplate
```

Inheritance:
`object` -> `DataTemplate`

Derived:
`DataTemplate<T>`

## Examples

Create a typed template and materialize an element for compatible data:

```csharp
using Cerneala.UI.Controls;
using Cerneala.UI.Elements;

DataTemplate<string> template = new(value => new TextBlock { Text = value });

UIElement? element = template.CreateElement("Ink");
```

Use a data template with a `ContentPresenter`:

```csharp
using Cerneala.UI.Controls;

ContentPresenter presenter = new()
{
    Content = "Preview",
    ContentTemplate = new DataTemplate<string>(value => new TextBlock { Text = value })
};
```

Check compatibility before creating an element:

```csharp
using Cerneala.UI.Controls;

DataTemplate<string> template = new(value => new TextBlock { Text = value });

bool acceptsText = template.CanApply("Preview");
bool acceptsNumber = template.CanApply(42);
```

## Remarks

`DataTemplate` stores the data type accepted by a template through `DataType`. `CanApply(object?)` returns `true` when the supplied data is `null` or when `DataType.IsInstanceOfType(data)` returns `true`.

`CreateElement(object?)` is the public creation entry point. It first validates the data with `CanApply`; incompatible data throws `InvalidOperationException`. Compatible data is passed to the derived implementation through `CreateElementCore(object?)`.

The base constructor and `CreateElementCore` override point are `private protected`, so implementations of this abstract base are limited to derived types inside the `Cerneala` assembly. Public callers normally use `DataTemplate<T>`, which sets `DataType` to `typeof(T)` and invokes a typed `Func<T, UIElement?>` factory. In that built-in implementation, `null` data creates no child and returns `null`.

`ContentPresenter.ContentTemplate`, `ItemsControl.ItemTemplate`, and `ItemsPresenter.ItemTemplate` consume this classic template type. Newer template APIs under `Cerneala.UI.Controls.Templates` are separate from this base class.

## Properties

| Name | Type | Description |
| --- | --- | --- |
| `DataType` | `Type` | Gets the data type accepted by this template. |

## Methods

| Name | Return Type | Description |
| --- | --- | --- |
| `CanApply(object? data)` | `bool` | Returns `true` when `data` is `null` or is an instance of `DataType`; otherwise returns `false`. |
| `CreateElement(object? data)` | `UIElement?` | Creates an element for compatible data, or throws `InvalidOperationException` when the supplied value cannot be applied to this template. |

## Exceptions

| Member | Exception | Condition |
| --- | --- | --- |
| `CreateElement(object? data)` | `InvalidOperationException` | `data` is not `null` and is not an instance of `DataType`. |

## Applies To

Project: `Cerneala`

## See Also

- `UI/Controls/Templates/DataTemplate{T}.cs`
- `UI/Controls/ContentPresenter.cs`
- `UI/Controls/ItemsControl.cs`
- `UI/Controls/ItemsPresenter.cs`
