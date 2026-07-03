namespace Cerneala.UI.Platform;

public interface IFileDialogService
{
    string? OpenFile(FileDialogOptions options);

    string? SaveFile(FileDialogOptions options);
}

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

public sealed record FileDialogFilter
{
    public FileDialogFilter(string Name, IReadOnlyList<string> Extensions)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(Name);
        ArgumentNullException.ThrowIfNull(Extensions);

        this.Name = Name;
        this.Extensions = Array.AsReadOnly(Extensions.ToArray());
    }

    public string Name { get; }

    public IReadOnlyList<string> Extensions { get; }
}
