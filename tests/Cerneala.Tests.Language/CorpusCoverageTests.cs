namespace Cerneala.Tests.Language;

public sealed class CorpusCoverageTests
{
    [Fact]
    public void EveryInventoriedConstructHasValidInvalidAndExistingTestEvidence()
    {
        IReadOnlyList<CorpusCase> cases = CorpusCatalog.Load();

        Assert.NotEmpty(cases);
        Assert.All(cases, item =>
        {
            Assert.False(string.IsNullOrWhiteSpace(item.Valid), item.Id + " has no valid sample.");
            Assert.False(string.IsNullOrWhiteSpace(item.Invalid), item.Id + " has no invalid sample.");
            Assert.False(string.IsNullOrWhiteSpace(item.Origin), item.Id + " has no implementation owner.");
            Assert.False(string.IsNullOrWhiteSpace(item.Tests), item.Id + " has no mapped sourcegen tests.");
        });
        Assert.Equal(cases.Count, cases.Select(item => item.Id).Distinct(StringComparer.Ordinal).Count());
        Assert.Contains(cases, item => item.Family == "Binding");
        Assert.Contains(cases, item => item.Family == "Directive");
        Assert.Contains(cases, item => item.Family == "Motion");
        Assert.Contains(cases, item => item.Family == "Prism");
    }

    [Fact]
    public void RepositoryAndDocumentationCorpusManifestPointsAtVersionedFiles()
    {
        string manifest = Path.Combine(AppContext.BaseDirectory, "Corpus", "repository-documents.txt");
        string root = CorpusCatalog.RepositoryRoot();
        string[] paths = File.ReadAllLines(manifest)
            .Select(line => line.Trim())
            .Where(line => line.Length > 0 && !line.StartsWith("#", StringComparison.Ordinal))
            .ToArray();

        Assert.NotEmpty(paths);
        Assert.All(paths, path => Assert.True(File.Exists(Path.Combine(root, path)), "Missing corpus source: " + path));
    }
}
