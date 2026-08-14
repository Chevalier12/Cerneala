namespace Cerneala.LanguageServer.Workspace;

internal sealed class PathComparer : IEqualityComparer<string>
{
    public static readonly PathComparer Instance = new();

    public bool Equals(string? x, string? y) => string.Equals(x, y, Comparison);

    public int GetHashCode(string obj) => Comparer.GetHashCode(obj);

    public static string Normalize(string path) => Path.GetFullPath(path)
        .TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);

    public static string FromUri(string uri)
    {
        Uri parsed = new(uri, UriKind.Absolute);
        if (!parsed.IsFile)
        {
            throw new ArgumentException("Only file URIs are supported.", nameof(uri));
        }

        return Normalize(parsed.LocalPath);
    }

    private static StringComparison Comparison => OperatingSystem.IsWindows()
        ? StringComparison.OrdinalIgnoreCase
        : StringComparison.Ordinal;

    private static StringComparer Comparer => OperatingSystem.IsWindows()
        ? StringComparer.OrdinalIgnoreCase
        : StringComparer.Ordinal;
}
