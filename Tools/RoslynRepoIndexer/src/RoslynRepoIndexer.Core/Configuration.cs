using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace RoslynRepoIndexer.Core;

public sealed record IndexerConfig
{
    public string? Solution { get; init; }
    public bool IncludeGenerated { get; init; } = false;
    public bool IncludeNonCSharpText { get; init; } = true;
    public long MaxTextFileBytes { get; init; } = 1_048_576;
    public int MaxDegreeOfParallelism { get; init; } = Math.Min(Environment.ProcessorCount, 8);
    public int SearchResultLimit { get; init; } = 50;
    public int SuggestionLimit { get; init; } = 5;
    public int ExactRefsTimeoutSeconds { get; init; } = 30;
    public IReadOnlyList<string> ExcludeDirectories { get; init; } = DefaultExcludeDirectories;
    public IReadOnlyList<string> ExcludeFileSuffixes { get; init; } = DefaultExcludeFileSuffixes;

    public static readonly string[] DefaultExcludeDirectories =
    {
        ".git", "bin", "obj", ".vs", ".idea", ".vscode", "node_modules", ".roslyn-index", "TestResults", "artifacts", "packages"
    };

    public static readonly string[] DefaultExcludeFileSuffixes =
    {
        ".dll", ".exe", ".pdb", ".png", ".jpg", ".jpeg", ".gif", ".webp", ".ico", ".pdf", ".zip", ".7z", ".tar", ".gz"
    };

    public static IndexerConfig Default { get; } = new();
}

public static class ConfigLoader
{
    private static readonly HashSet<string> KnownProperties = new(StringComparer.OrdinalIgnoreCase)
    {
        "solution", "includeGenerated", "includeNonCSharpText", "maxTextFileBytes", "maxDegreeOfParallelism",
        "searchResultLimit", "suggestionLimit", "exactRefsTimeoutSeconds", "excludeDirectories", "excludeFileSuffixes"
    };

    public static ConfigLoadResult Load(string repoRoot, string? explicitPath)
    {
        var warnings = new List<string>();
        var path = explicitPath ?? Path.Combine(repoRoot, ".roslyn-index.json");
        if (!File.Exists(path))
        {
            return new ConfigLoadResult(IndexerConfig.Default, warnings);
        }

        try
        {
            using var document = JsonDocument.Parse(File.ReadAllText(path), new JsonDocumentOptions { AllowTrailingCommas = true });
            foreach (var property in document.RootElement.EnumerateObject())
            {
                if (!KnownProperties.Contains(property.Name))
                {
                    warnings.Add($"Unknown config property '{property.Name}' ignored.");
                }
            }

            var config = JsonSerializer.Deserialize<IndexerConfig>(document.RootElement.GetRawText(), JsonOptions.Default) ?? IndexerConfig.Default;
            if (config.MaxTextFileBytes <= 0)
            {
                warnings.Add("Invalid maxTextFileBytes; using default.");
                config = config with { MaxTextFileBytes = IndexerConfig.Default.MaxTextFileBytes };
            }

            if (config.MaxDegreeOfParallelism <= 0)
            {
                warnings.Add("Invalid maxDegreeOfParallelism; using default.");
                config = config with { MaxDegreeOfParallelism = IndexerConfig.Default.MaxDegreeOfParallelism };
            }

            if (config.SearchResultLimit <= 0)
            {
                warnings.Add("Invalid searchResultLimit; using default.");
                config = config with { SearchResultLimit = IndexerConfig.Default.SearchResultLimit };
            }

            if (config.SuggestionLimit <= 0)
            {
                warnings.Add("Invalid suggestionLimit; using default.");
                config = config with { SuggestionLimit = IndexerConfig.Default.SuggestionLimit };
            }

            return new ConfigLoadResult(config, warnings);
        }
        catch (JsonException ex)
        {
            warnings.Add($"Invalid .roslyn-index.json: {ex.Message}. Defaults used.");
            return new ConfigLoadResult(IndexerConfig.Default, warnings);
        }
    }

    public static string ComputeHash(IndexerConfig config)
        => HashText(JsonSerializer.Serialize(config, JsonOptions.Default));

    internal static string HashText(string text)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(text));
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }
}

public static class JsonOptions
{
    public static readonly JsonSerializerOptions Default = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    public static readonly JsonSerializerOptions Compact = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    static JsonOptions()
    {
        Default.Converters.Add(new JsonStringEnumConverter(JsonNamingPolicy.CamelCase));
        Compact.Converters.Add(new JsonStringEnumConverter(JsonNamingPolicy.CamelCase));
    }
}
