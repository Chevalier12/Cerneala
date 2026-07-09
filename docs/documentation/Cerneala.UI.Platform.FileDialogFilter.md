# FileDialogFilter Class

## Definition
Namespace: `Cerneala.UI.Platform`

Assembly/Project: `Cerneala`

Source: `UI/Platform/IFileDialogService.cs`

Describes a named file type filter for file open and save dialogs.

```csharp
public sealed record FileDialogFilter
```

Inheritance:
`object` -> `FileDialogFilter`

## Examples
Create a filter and pass it through `FileDialogOptions` to a file dialog service.

```csharp
using Cerneala.UI.Platform;

FileDialogFilter imageFilter = new("Images", ["png", "jpg"]);

FileDialogOptions options = new(
    Title: "Open image",
    Filters: [imageFilter]);

string? selectedPath = fileDialogService.OpenFile(options);
```

## Remarks
`FileDialogFilter` stores the display name of a filter and the file extensions associated with it. `FileDialogOptions.Filters` uses these values when a platform `IFileDialogService` implementation shows an open or save dialog.

The constructor requires a non-null, non-whitespace `Name` and a non-null `Extensions` list. The extensions list is copied into a read-only snapshot during construction, so later changes to the caller's list do not change the filter.

The class does not validate, normalize, or copy individual extension strings beyond copying the list itself. Platform dialog implementations decide how to interpret the extension values.

Because this type is a record, it uses record value semantics for equality and formatting.

## Constructors
| Name | Description |
| --- | --- |
| `FileDialogFilter(string Name, IReadOnlyList<string> Extensions)` | Initializes a filter with a display name and a snapshot of the supplied extension list. |

## Properties
| Name | Type | Description |
| --- | --- | --- |
| `Name` | `string` | Gets the display name of the filter. |
| `Extensions` | `IReadOnlyList<string>` | Gets the read-only snapshot of extension strings associated with the filter. |

## Exceptions
| Exception | Condition |
| --- | --- |
| `ArgumentException` | `Name` is null, empty, or consists only of white-space characters. |
| `ArgumentNullException` | `Extensions` is null. |

## Applies to
`Cerneala` platform file dialog abstractions.

## See also
- `IFileDialogService`
- `FileDialogOptions`
