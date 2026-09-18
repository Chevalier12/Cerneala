namespace Cerneala.Scene2D.Packages;

internal static class PackageFiles
{
    internal const string CatalogName = "catalog.c2d";
    internal const string DataName = "payloads.c2d";
    internal const string AssetsName = "assets";

    internal static string Normalize(string path)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        string normalized = path.Replace('\\', '/');
        foreach (string part in normalized.Split('/'))
        {
            if (part is "" or "." or ".." || part.EndsWith('.') || part.EndsWith(' ') ||
                part.Any(static character => character < 32 || "<>:\"|?*".Contains(character)))
            {
                throw new ArgumentException("Package file paths must be portable, normalized and root-relative.", nameof(path));
            }
            string stem = part.Split('.')[0].ToUpperInvariant();
            if (stem is "CON" or "PRN" or "AUX" or "NUL" ||
                (stem.Length == 4 && (stem.StartsWith("COM", StringComparison.Ordinal) || stem.StartsWith("LPT", StringComparison.Ordinal)) &&
                 stem[3] is >= '1' and <= '9'))
            {
                throw new ArgumentException("Package file paths cannot use reserved device names.", nameof(path));
            }
        }
        return normalized;
    }

    internal static string Root(string path)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        string root = Path.GetFullPath(path);
        for (DirectoryInfo? directory = new(root); directory is not null; directory = directory.Parent)
        {
            if (directory.Exists && (directory.Attributes & FileAttributes.ReparsePoint) != 0)
            {
                throw new IOException("Package directories cannot traverse symbolic links or reparse points.");
            }
        }
        return root;
    }

    internal static string ExistingFile(string root, string relative)
    {
        string current = Root(root);
        string[] parts = Normalize(relative).Split('/');
        for (int index = 0; index < parts.Length; index++)
        {
            current = Path.Combine(current, parts[index]);
            FileAttributes attributes = File.GetAttributes(current);
            if ((attributes & FileAttributes.ReparsePoint) != 0 ||
                ((attributes & FileAttributes.Directory) != 0) != (index < parts.Length - 1))
            {
                throw new IOException("Package files cannot traverse links or have the wrong filesystem kind.");
            }
        }
        return current;
    }
}
