# IFileDialogService Interface

## Definition
Namespace: `Cerneala.UI.Platform`

Assembly/Project: `Cerneala`

Source: `UI/Platform/IFileDialogService.cs`

Shows open-file and save-file dialogs.

```csharp
public interface IFileDialogService
```

## Examples

Ask the root's file dialog service for a file to open:

```csharp
using Cerneala.UI.Elements;
using Cerneala.UI.Platform;

UIRoot root = new(200, 200);
root.SetPlatformServices(new PlatformServices(FileDialogs: new FixedFileDialogs()));

FileDialogOptions options = new(
    Title: "Open image",
    Filters: [new FileDialogFilter("Images", [".png", ".jpg"])]);

string? path = root.PlatformServices.FileDialogs?.OpenFile(options);
Console.WriteLine(path ?? "(cancelled)");
// C:\images\logo.png

sealed class FixedFileDialogs : IFileDialogService
{
    public string? OpenFile(FileDialogOptions options) => @"C:\images\logo.png";

    public string? SaveFile(FileDialogOptions options) => null;
}
```

## Remarks

This is an optional seam in `IPlatformServices`. No Cerneala control or host calls it, and no Cerneala platform supplies an implementation, including the SDL platform. Application code calls it directly, as in the example. The interface does not define the meaning of a `null` result; the example treats it as a cancelled dialog. `Detective` reports only whether a service is present.

## Methods

| Name | Return Type | Description |
| --- | --- | --- |
| `OpenFile(FileDialogOptions options)` | `string?` | Shows an open-file dialog and returns the chosen path, or `null`. |
| `SaveFile(FileDialogOptions options)` | `string?` | Shows a save-file dialog and returns the chosen path, or `null`. |

## Applies to

`IPlatformServices.FileDialogs`.

## See also

- `Cerneala.UI.Platform.FileDialogOptions`
- `Cerneala.UI.Platform.FileDialogFilter`
- `Cerneala.UI.Platform.IPlatformServices`
