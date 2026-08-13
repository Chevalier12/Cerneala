using System.Text.Json;

namespace Cerneala.Tests.Language;

public sealed record CorpusCase(
    string Id,
    string Family,
    string Construct,
    string Valid,
    string Invalid,
    string Origin,
    string Tests,
    bool Recovery = false,
    string? FollowingSibling = null);

internal static class CorpusCatalog
{
    public static IReadOnlyList<CorpusCase> Load()
    {
        string path = Path.Combine(AppContext.BaseDirectory, "Corpus", "constructs.json");
        return JsonSerializer.Deserialize<CorpusCase[]>(
            File.ReadAllText(path),
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true })!;
    }

    public static string RepositoryRoot()
    {
        DirectoryInfo? directory = new(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "Cerneala.slnx")))
        {
            directory = directory.Parent;
        }

        return directory?.FullName
            ?? throw new InvalidOperationException("Could not locate the Cerneala repository root.");
    }
}
