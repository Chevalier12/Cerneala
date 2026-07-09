# DataTemplate&lt;T&gt; Class

## Definition
Namespace: `Cerneala.UI.Controls.Templates`

Assembly/Project: `Cerneala`

Source: `UI/Controls/Templates/DataTemplate{T}.cs`

Creates UI elements for data objects of a specific CLR type by calling a typed factory delegate.

```csharp
public sealed class DataTemplate<T> : DataTemplate
```

Inheritance:
`object` -> `DataTemplate` -> `DataTemplate<T>`

Generic type parameters:

| Name | Description |
| --- | --- |
| `T` | The data type accepted by the template factory. |

## Examples

```csharp
using Cerneala.UI.Controls;
using Cerneala.UI.Elements;

DataTemplate<string> template = new(value =>
{
    return new UIElement();
});

UIElement? element = template.CreateElement("row");
```

## Remarks

`DataTemplate<T>` is a typed wrapper around `DataTemplate`. Its constructor stores `typeof(T)` as the inherited `DataType`, and `CreateElement` uses that type to reject incompatible data before the typed factory is invoked.

When `CreateElement(null)` is called, the template returns `null` and does not call the factory. When non-null data is accepted, the value is cast to `T` and passed to the factory. The factory may return a `UIElement` or `null`.

No implicit conversion is performed for incoming data. The value must already be assignable to `T`; otherwise the inherited `CreateElement` method throws an `InvalidOperationException`.

## Constructors

| Name | Description |
| --- | --- |
| `DataTemplate(Func<T, UIElement?> factory)` | Initializes a template for `typeof(T)` and stores the factory used to create elements. Throws `ArgumentNullException` when `factory` is `null`. |

## Properties

| Name | Description |
| --- | --- |
| `DataType` | Gets the CLR type accepted by the template. Inherited from `DataTemplate`; for this class the value is `typeof(T)`. |

## Methods

| Name | Description |
| --- | --- |
| `CanApply(object? data)` | Returns `true` when `data` is `null` or is an instance of `DataType`. Inherited from `DataTemplate`. |
| `CreateElement(object? data)` | Creates an element for compatible data, returns `null` for `null` data, and throws `InvalidOperationException` for incompatible non-null data. Inherited from `DataTemplate`. |

## Applies to

Cerneala retained UI controls and templated content APIs.

## See also

- `DataTemplate`
- `ContentPresenter`
- `ItemsControl`
