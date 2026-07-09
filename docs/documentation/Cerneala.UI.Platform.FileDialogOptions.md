# FileDialogOptions Class

## Definition

Namespace: `Cerneala.UI.Platform`

Assembly/Project: `Cerneala`

Source: `UI/Platform/IFileDialogService.cs`

Captures optional metadata used by `IFileDialogService` when opening or saving files.

```csharp
public sealed record FileDialogOptions
```

## Examples

```csharp
using Cerneala.UI.Platform;

FileDialogOptions options = new(
    Title: "Open drawing",
    InitialDirectory: @"C:\Projects\Sketches",
    Filters:
    [
        new FileDialogFilter("Images", ["png", "jpg"])
    ]);

string? selectedPath = platformServices.FileDialogs?.OpenFile(options);
```

## Remarks

`FileDialogOptions` is an immutable record for passing dialog hints to `IFileDialogService.OpenFile` and `IFileDialogService.SaveFile`. All constructor parameters are optional and default to `null`.

When a filters list is provided, the constructor snapshots the list into a read-only collection. Later changes to the caller-owned filter list do not change `Filters`.

## Constructors

| Name | Description |
| --- | --- |
| `FileDialogOptions(string? Title = null, string? InitialDirectory = null, IReadOnlyList<FileDialogFilter>? Filters = null, string? DefaultFileName = null)` | Initializes a new `FileDialogOptions` instance with optional title, starting directory, file type filters, and default file name. |

## Properties

| Name | Type | Description |
| --- | --- | --- |
| `Title` | `string?` | Gets the optional dialog title. |
| `InitialDirectory` | `string?` | Gets the optional initial directory for the dialog. |
| `Filters` | `IReadOnlyList<FileDialogFilter>?` | Gets the optional read-only file type filters collection. |
| `DefaultFileName` | `string?` | Gets the optional default file name, mainly useful for save dialogs. |

## Applies to

Cerneala UI platform services.

## See also

- `Cerneala.UI.Platform.IFileDialogService`
- `Cerneala.UI.Platform.FileDialogFilter`
