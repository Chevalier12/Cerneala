using System.Text.Json;

namespace Cerneala.Tests.Language;

public sealed class SourceGeneratorDiagnosticBaselineTests
{
    [Fact]
    public void InvalidCorpusMatchesVersionedSourceGeneratorDiagnostics()
    {
        HarnessDiagnostic[] actual = CorpusCatalog.Load()
            .Where(item => item.Recovery)
            .SelectMany(item => LanguagePipelineHarness.Analyze(item.Id + ".crn", item.Invalid)
                .SourceGeneratorDiagnostics)
            .ToArray();
        string sourcePath = Path.Combine(
            CorpusCatalog.RepositoryRoot(),
            "tests",
            "Cerneala.Tests.Language",
            "Corpus",
            "sourcegen-diagnostics.json");
        JsonSerializerOptions options = new() { WriteIndented = true };

        if (string.Equals(Environment.GetEnvironmentVariable("CERNEALA_UPDATE_BASELINES"), "1", StringComparison.Ordinal))
        {
            File.WriteAllText(sourcePath, JsonSerializer.Serialize(actual, options) + Environment.NewLine);
        }

        HarnessDiagnostic[] expected = JsonSerializer.Deserialize<HarnessDiagnostic[]>(
            File.ReadAllText(sourcePath),
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true })!;
        Assert.Equal(expected, actual);
    }
}
