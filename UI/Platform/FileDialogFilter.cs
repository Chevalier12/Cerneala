namespace Cerneala.UI.Platform;

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
