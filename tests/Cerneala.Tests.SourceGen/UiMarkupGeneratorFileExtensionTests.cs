using System.Security.Cryptography;
using System.Text;
using Microsoft.CodeAnalysis;

namespace Cerneala.Tests.SourceGen;

public sealed partial class UiMarkupGeneratorTests
{
    private const string RepresentativeMarkup = "<Button Content=\"Migration baseline\" />";

    [Fact]
    public void CrnMarkupGeneratesSource()
    {
        GeneratorRunResult result = RunGenerator("View.crn", RepresentativeMarkup, out _);

        Assert.Single(result.GeneratedSources);
        Assert.DoesNotContain(result.Diagnostics, diagnostic => diagnostic.Severity == DiagnosticSeverity.Error);
    }

    [Fact]
    public void LegacyCuiXmlMarkupIsIgnored()
    {
        GeneratorRunResult result = RunGenerator("View.cui.xml", RepresentativeMarkup, out _);

        Assert.Empty(result.GeneratedSources);
        Assert.Empty(result.Diagnostics);
    }

    [Fact]
    public void RepresentativeGeneratedOutputAndDiagnosticsMatchThePreMigrationBaseline()
    {
        GeneratorRunResult result = RunGenerator("View.crn", RepresentativeMarkup, out _);
        GeneratedSourceResult source = Assert.Single(result.GeneratedSources);
        string normalizedSource = source.SourceText.ToString().Replace("\r\n", "\n", StringComparison.Ordinal);
        string sourceHash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(normalizedSource)));
        Assert.Equal("8FE54C678949788D6AB60A5C9EA49CE8B1988E48904399474DAE0A235EF91C17", sourceHash);
        Assert.Empty(result.Diagnostics);
    }
}
