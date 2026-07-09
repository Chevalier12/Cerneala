namespace Cerneala.UI.Platform;

public sealed record FileDialogOptions
{
    public FileDialogOptions(
        string? Title = null,
        string? InitialDirectory = null,
        IReadOnlyList<FileDialogFilter>? Filters = null,
        string? DefaultFileName = null)
    {
        this.Title = Title;
        this.InitialDirectory = InitialDirectory;
        this.Filters = Filters is null ? null : Array.AsReadOnly(Filters.ToArray());
        this.DefaultFileName = DefaultFileName;
    }

    public string? Title { get; }

    public string? InitialDirectory { get; }

    public IReadOnlyList<FileDialogFilter>? Filters { get; }

    public string? DefaultFileName { get; }
}
