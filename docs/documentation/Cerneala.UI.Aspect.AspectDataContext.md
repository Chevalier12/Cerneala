# AspectDataContext Class

## Definition
Namespace: `Cerneala.UI.Aspect`

Assembly/Project: `Cerneala`

Source: `UI/Aspect/AspectDataContext.cs`

Carries optional data-object metadata used while aspect rules evaluate data-context conditions.

```csharp
public sealed class AspectDataContext
```

Inheritance:
`object` -> `AspectDataContext`

## Examples

Pass template or item data into aspect resolution:

```csharp
using Cerneala.UI.Aspect;
using Cerneala.UI.Controls;

Button button = new();
AspectDataContext dataContext = new(new UserCard(true));

AspectCondition condition = AspectCondition.Data<UserCard>(
    "important user",
    user => user.IsImportant,
    AspectDataDependency.Property<UserCard, bool>(nameof(UserCard.IsImportant)));

AspectMatchContext context = new(button, dataContext: dataContext);
bool matches = condition.Evaluate(context).Matches;

internal sealed record UserCard(bool IsImportant);
```

Create a context for an indexed item with an explicit declared type:

```csharp
using Cerneala.UI.Aspect;

object item = "Inbox";
AspectDataContext dataContext = new(
    data: item,
    dataType: typeof(string),
    index: 0,
    owner: "menu");
```

## Remarks

`AspectDataContext` is a lightweight immutable container. It stores the current data object, an optional data type, an optional item index, and an optional owner object.

When the constructor receives `dataType`, `DataType` uses that value. Otherwise, `DataType` is inferred from `data?.GetType()`. If both `dataType` and `data` are `null`, `DataType` is `null`.

`AspectMatchContext` stores an `AspectDataContext` and exposes convenience properties for `Data`, `DataType`, and `ItemIndex`. `AspectEngine.Resolve` and `AspectEngine.Apply` use `AspectDataContext.Empty` when no data context is supplied.

Data aspect conditions evaluate against `AspectMatchContext.Data`. A condition created through `AspectCondition.Data<TData>` matches only when the stored `Data` value is assignable to `TData` and its predicate returns `true`.

## Constructors

| Name | Description |
| --- | --- |
| `AspectDataContext(object? data, Type? dataType = null, int? index = null, object? owner = null)` | Initializes a data context with optional data, explicit data type, item index, and owner metadata. |

## Properties

| Name | Type | Description |
| --- | --- | --- |
| `Data` | `object?` | Gets the data object available to data-context aspect conditions. |
| `DataType` | `Type?` | Gets the explicit data type, or the runtime type inferred from `Data` when no explicit type was supplied. |
| `Empty` | `AspectDataContext` | Gets a shared empty context whose data, data type, index, and owner are all unset. |
| `Index` | `int?` | Gets the optional item index associated with the data object. |
| `Owner` | `object?` | Gets the optional owner associated with the data object. |

## Applies to

Cerneala UI aspect matching and data-context aspect invalidation.

## See also

- `Cerneala.UI.Aspect.AspectCondition`
- `Cerneala.UI.Aspect.AspectDataDependency`
- `Cerneala.UI.Aspect.AspectMatchContext`
- `Cerneala.UI.Aspect.AspectEngine`
