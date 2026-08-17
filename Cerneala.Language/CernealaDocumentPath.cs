namespace Cerneala.Language;

internal static class CernealaDocumentPath
{
    internal const string MarkupExtension = ".crn";

    internal static bool IsMarkupFile(string path) =>
        !string.IsNullOrEmpty(path) && path.EndsWith(MarkupExtension, StringComparison.OrdinalIgnoreCase);

    internal static string GetLogicalName(string path)
    {
        EnsureMarkupPath(path);
        string fileName = Path.GetFileName(path);
        return fileName.Substring(0, fileName.Length - MarkupExtension.Length);
    }

    internal static string GetCompanionPath(string path)
    {
        EnsureMarkupPath(path);
        return path + ".cs";
    }

    private static void EnsureMarkupPath(string path)
    {
        if (!IsMarkupFile(path))
        {
            throw new ArgumentException($"Cerneala markup paths must end with '{MarkupExtension}'.", nameof(path));
        }
    }
}
