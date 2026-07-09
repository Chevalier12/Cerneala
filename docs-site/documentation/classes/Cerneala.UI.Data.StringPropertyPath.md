# StringPropertyPath Class

## Definition
Namespace: `Cerneala.UI.Data`

Assembly/Project: `Cerneala`

Source: `UI/Data/StringPropertyPath.cs`

Represents the deferred string-based property path API surface for core binding.

```csharp
public sealed class StringPropertyPath
```

Inheritance:
`object` -> `StringPropertyPath`

## Examples

Check whether string property paths are available before attempting to parse one.

```csharp
using Cerneala.UI.Data;

var viewModel = new { User = new { Name = "Ada" } };

if (StringPropertyPath.IsSupported)
{
    StringPropertyPath path = StringPropertyPath.Parse("User.Name");
    object? value = path.Evaluate(viewModel);
}
```

`Parse` currently reports that string property paths are not supported.

```csharp
using Cerneala.UI.Data;

try
{
    StringPropertyPath.Parse("User.Name");
}
catch (NotSupportedException)
{
    // String property paths are deferred in the core binding layer.
}
```

## Remarks

`StringPropertyPath` is the deferred API surface for string-based property paths in the data binding layer. The type is public, but creation and evaluation are intentionally unavailable in the current core binding implementation.

Use `IsSupported` to detect this capability. In the current implementation it always returns `false`.

`Parse` validates that the provided path is not null, empty, or whitespace, then throws `NotSupportedException`. `Evaluate` validates that the source object is not null, then throws `NotSupportedException`.

The test suite verifies that binding flows do not depend on `StringPropertyPath`; typed and observable binding APIs are used instead.

## Properties

| Name | Type | Description |
| --- | --- | --- |
| `IsSupported` | `bool` | Gets whether string property paths are supported. Always returns `false` in the current implementation. |
| `Path` | `string` | Gets the original path string for an instance. Instances cannot currently be created through the public API because `Parse` throws `NotSupportedException`. |

## Methods

| Name | Return Type | Description |
| --- | --- | --- |
| `Parse(string path)` | `StringPropertyPath` | Validates `path`, then throws `NotSupportedException` because string property paths are deferred and unsupported in core binding. |
| `Evaluate(object source)` | `object?` | Validates `source`, then throws `NotSupportedException` because string property paths are deferred and unsupported in core binding. |

## Exceptions

| Member | Exception | Condition |
| --- | --- | --- |
| `Parse(string path)` | `ArgumentNullException` | `path` is null. |
| `Parse(string path)` | `ArgumentException` | `path` is empty or whitespace. |
| `Parse(string path)` | `NotSupportedException` | `path` is non-empty; string property paths are not supported in core binding. |
| `Evaluate(object source)` | `ArgumentNullException` | `source` is null. |
| `Evaluate(object source)` | `NotSupportedException` | `source` is non-null; string property paths are not supported in core binding. |

## Applies to

Project: `Cerneala`

## See also

- `Binding`
- `Binding<T>`
- `ObservableValue<T>`
