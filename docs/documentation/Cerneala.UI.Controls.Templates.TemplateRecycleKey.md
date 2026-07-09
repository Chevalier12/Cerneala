# TemplateRecycleKey Class

## Definition
Namespace: `Cerneala.UI.Controls.Templates`

Assembly/Project: `Cerneala`

Source: `UI/Controls/Templates/TemplateRecycleKey.cs`

Identifies the data type, container type, and optional slot used to group recyclable template-created elements.

```csharp
public sealed record TemplateRecycleKey(Type? DataType, Type ContainerType, string? Slot);
```

Inheritance:
`object` -> `TemplateRecycleKey`

## Examples
Create a key for item presenters and use it with `TemplateRecyclePool`:

```csharp
using Cerneala.UI.Controls;
using Cerneala.UI.Controls.Templates;

TemplateRecyclePool pool = new();
ContentPresenter presenter = new() { Content = "old" };
TemplateRecycleKey key = new(typeof(string), typeof(ContentPresenter), "item");

pool.Release(key, presenter);

ContentPresenter? reused = pool.Rent(key) as ContentPresenter;
```

## Remarks
`TemplateRecycleKey` is the value key used by `TemplateRecyclePool` to keep recycled `UIElement` instances separated by template context. The `DataType` component identifies the data being presented when one is known, `ContainerType` identifies the element category being recycled, and `Slot` lets callers distinguish separate template positions that use the same data and container types.

Because this type is a sealed record, equality and hash code behavior are value-based across `DataType`, `ContainerType`, and `Slot`. Two keys with the same component values address the same stack in `TemplateRecyclePool`.

`DataType` and `Slot` may be `null`. The source does not add constructor validation, so callers are responsible for passing a meaningful non-null `ContainerType`.

## Constructors
| Name | Description |
| --- | --- |
| `TemplateRecycleKey(Type? dataType, Type containerType, string? slot)` | Initializes a recycle key with the data type, container type, and optional slot name. |

## Properties
| Name | Type | Description |
| --- | --- | --- |
| `DataType` | `Type?` | Gets the data type associated with the recycled template content, or `null` when no data type is recorded. |
| `ContainerType` | `Type` | Gets the UI element type used as the recycled container category. |
| `Slot` | `string?` | Gets the optional slot name that separates otherwise matching data and container types. |

## Applies To
Cerneala retained UI content templates and template recycle pooling.

## See Also
- `Cerneala.UI.Controls.Templates.TemplateRecyclePool`
- `Cerneala.UI.Controls.ContentPresenter`
- `Cerneala.UI.Controls.Templates.ContentTemplate`
