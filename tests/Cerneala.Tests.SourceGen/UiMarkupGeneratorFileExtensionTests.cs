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
        Assert.Equal("DCD437128E0720708F736B79865B5D8C9F3A1D38C2BA580473DD655E62F4CF9F", sourceHash);
        Assert.Empty(result.Diagnostics);
    }
}
