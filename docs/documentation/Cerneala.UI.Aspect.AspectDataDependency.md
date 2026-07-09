# AspectDataDependency Class

## Definition

Namespace: `Cerneala.UI.Aspect`

Assembly/Project: `Cerneala`

Source: `UI/Aspect/AspectDataDependency.cs`

Identifies a data-context input that an aspect data condition depends on for diagnostics and invalidation tracking.

```csharp
public sealed record AspectDataDependency(string Name, Type? OwnerType = null, string? PropertyName = null)
```

Inheritance:
`object` -> `AspectDataDependency`

## Examples

Declare a dependency on a specific property of the data item:

```csharp
using Cerneala.UI.Aspect;

AspectCondition condition = AspectCondition.Data<UserCard>(
    "important user",
    user => user.IsImportant,
    AspectDataDependency.Property<UserCard, bool>(nameof(UserCard.IsImportant)));

internal sealed record UserCard(bool IsImportant);
```

Declare a named data dependency when the condition depends on the data context as a whole:

```csharp
using Cerneala.UI.Aspect;

AspectCondition condition = AspectCondition.Data<UserCard>(
    "important user",
    user => user.IsImportant,
    AspectDataDependency.Named("user"));

internal sealed record UserCard(bool IsImportant);
```

## Remarks

`AspectDataDependency` is used by data aspect conditions created with `AspectCondition.Data<TData>` and `AspectCondition.Data<TData, TValue>`. Those conditions require at least one data dependency and report each dependency as an `AspectConditionDependency` with `AspectConditionDependencyKind.DataContext`.

`Property<TData, TValue>` creates a dependency whose `Name` is formatted as the data type name followed by the property name, such as `UserCard.IsImportant`. It also stores `typeof(TData)` in `OwnerType` and the supplied property name in `PropertyName`.

`Named` creates a dependency with only a logical `Name`. Use it when a condition depends on a data object, data source, or external data-context concept rather than a single known property.

Both factory methods reject `null`, empty, or whitespace-only names by throwing `ArgumentException`. The primary record constructor does not perform that validation, so callers that want the built-in guard should use the factory methods.

## Constructors

| Name | Description |
| --- | --- |
| `AspectDataDependency(string Name, Type? OwnerType = null, string? PropertyName = null)` | Initializes a data dependency with a diagnostic name and optional owner type and property name metadata. |

## Properties

| Name | Type | Description |
| --- | --- | --- |
| `Name` | `string` | Gets the dependency name reported to aspect diagnostics and invalidation dependency sets. |
| `OwnerType` | `Type?` | Gets the data owner type when the dependency was created for a specific property. |
| `PropertyName` | `string?` | Gets the property name when the dependency was created with `Property<TData, TValue>`. |

## Methods

| Name | Description |
| --- | --- |
| `Named(string name)` | Creates a named data dependency and throws `ArgumentException` when `name` is empty or whitespace. |
| `Property<TData, TValue>(string propertyName)` | Creates a property dependency for `TData`, storing the owner type, property name, and a formatted dependency name. |

## Applies to

Cerneala UI aspect data-context conditions, diagnostics, and invalidation tracking.

## See also

- `Cerneala.UI.Aspect.AspectCondition`
- `Cerneala.UI.Aspect.AspectConditionDependency`
- `Cerneala.UI.Aspect.AspectDataContext`
- `Cerneala.UI.Aspect.AspectDependencySet`
