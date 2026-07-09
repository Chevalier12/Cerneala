namespace Cerneala.UI.Platform;

public interface IFileDialogService
{
    string? OpenFile(FileDialogOptions options);

    string? SaveFile(FileDialogOptions options);
}
