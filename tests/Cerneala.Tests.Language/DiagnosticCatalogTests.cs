using Cerneala.Language.Diagnostics;

namespace Cerneala.Tests.Language;

public sealed class DiagnosticCatalogTests
{
    private static readonly string[] ExpectedIds =
    [
        "CERNEALAUI001", "CERNEALAUI002", "CERNEALAUI003", "CERNEALAUI004", "CERNEALAUI005",
        "CERNEALAUI006", "CERNEALAUI007", "CERNEALAUI008", "CERNEALAUI009", "CERNEALAUI010",
        "CERNEALAUI011", "CERNEALAUI012", "CERNEALAUI013", "CERNEALAUI014", "CERNEALAUI020",
        "CERNEALAUI021", "CERNEALAUI022", "CERNEALAUI023", "CERNEALAUI024", "CERNEALAUI025",
        "CERNEALAUI026", "PRISM1001", "PRISM1002", "PRISM1003", "PRISM2001", "PRISM2002",
        "PRISM2003", "PRISM2004", "PRISM2005", "PRISM2006", "PRISM2007", "PRISM2008",
        "PRISM2009", "PRISM2010", "PRISM2011", "PRISM2012", "PRISM2013"
    ];

    [Fact]
    public void CommonCatalogContainsEveryCurrentDescriptorExactlyOnce()
    {
        Assert.Equal(ExpectedIds, CernealaDiagnosticCatalog.All.Select(descriptor => descriptor.Id));
        Assert.Equal(ExpectedIds.Length, CernealaDiagnosticCatalog.All.Select(descriptor => descriptor.Id).Distinct().Count());
        Assert.All(CernealaDiagnosticCatalog.All, descriptor =>
        {
            Assert.False(string.IsNullOrWhiteSpace(descriptor.MessageFormat));
            Assert.False(string.IsNullOrWhiteSpace(descriptor.Category));
            Assert.Equal(LanguageDiagnosticSeverity.Error, descriptor.BuildSeverity);
        });
    }

    [Fact]
    public void ExistingDiagnosticFormatsRemainByteForByteStable()
    {
        Assert.Equal("Markup file '{0}' could not be parsed: {1}", CernealaDiagnosticCatalog.Get("CERNEALAUI001").MessageFormat);
        Assert.Equal("Markup binding source '{0}' is invalid: {1}", CernealaDiagnosticCatalog.Get("CERNEALAUI007").MessageFormat);
        Assert.Equal("Motion syntax in '{0}' is invalid: {1}", CernealaDiagnosticCatalog.Get("CERNEALAUI020").MessageFormat);
        Assert.Equal("Prism markup in '{0}' is invalid: {1}", CernealaDiagnosticCatalog.Get("PRISM1002").MessageFormat);
    }

    [Fact]
    public void EditorModeReducesOnlyTransientIncompleteDiagnostics()
    {
        LanguageDiagnosticDescriptor malformed = CernealaDiagnosticCatalog.Get("CERNEALAUI001");
        LanguageDiagnosticDescriptor unsupported = CernealaDiagnosticCatalog.Get("CERNEALAUI002");

        Assert.Equal(LanguageDiagnosticSeverity.Information, malformed.GetSeverity(AnalysisMode.Editor));
        Assert.Equal(LanguageDiagnosticSeverity.Error, malformed.GetSeverity(AnalysisMode.Build));
        Assert.Equal(LanguageDiagnosticSeverity.Error, unsupported.GetSeverity(AnalysisMode.Editor));
    }

    [Fact]
    public void LanguageCoreContainsNoXmlOrSourceGeneratorHostTypes()
    {
        string root = Path.Combine(CorpusCatalog.RepositoryRoot(), "Cerneala.Language", "Syntax");
        string source = string.Join(
            "\n",
            Directory.EnumerateFiles(root, "*.cs", SearchOption.AllDirectories).Select(File.ReadAllText));

        Assert.DoesNotContain("XText", source, StringComparison.Ordinal);
        Assert.DoesNotContain("XElement", source, StringComparison.Ordinal);
        Assert.DoesNotContain("SourceProductionContext", source, StringComparison.Ordinal);
    }
}
