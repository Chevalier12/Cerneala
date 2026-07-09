# DataTransfer Class

## Definition
Namespace: `Cerneala.UI.Input`

Assembly/Project: `Cerneala`

Source: `UI/Input/DataTransfer.cs`

Stores drag-and-drop payload values by string data format.

```csharp
public sealed class DataTransfer
```

Inheritance:
`object` -> `DataTransfer`

## Examples

```csharp
using Cerneala.UI.Input;

DataTransfer data = new DataTransfer()
    .SetData("text/plain", "payload");

if (data.TryGetData("text/plain", out string? text))
{
    // Use text.
}
```

## Remarks

`DataTransfer` is the payload container used by the retained input drag-and-drop path. `DragDropController.Begin` receives a `DataTransfer` instance, and `DragEventArgs.Data` exposes the same instance to drag and drop event handlers.

Formats are compared with `StringComparer.Ordinal`. `SetData` rejects null, empty, or whitespace-only format names and overwrites any existing value stored for the same format.

Stored values may be null. `TryGetData<T>` returns true for a null stored value only when `T` can represent null, such as a reference type or nullable value type.

## Constructors

| Name | Description |
| --- | --- |
| `DataTransfer()` | Initializes an empty data transfer container. |

## Properties

| Name | Description |
| --- | --- |
| `Formats` | Gets the currently stored format names. |

## Methods

| Name | Description |
| --- | --- |
| `SetData(string format, object? value)` | Stores a value for a format and returns the current `DataTransfer` instance. |
| `Contains(string format)` | Returns true when a value is stored for the specified format. |
| `TryGetData<T>(string format, out T? value)` | Attempts to retrieve the stored value for a format when the value is assignable to `T`. |

## Exceptions

| Member | Exception | Condition |
| --- | --- | --- |
| `SetData(string format, object? value)` | `ArgumentException` | `format` is null, empty, or whitespace-only. |

## Applies to

Cerneala retained UI input and drag-and-drop event handling.

## See also

- `Cerneala.UI.Input.DragDropController`
- `Cerneala.UI.Input.DragEventArgs`
