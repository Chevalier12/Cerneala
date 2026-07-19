using System.Text.Json;
using System.Text.Json.Nodes;
using Cerneala.SourceGen.Prism;

namespace Cerneala.Tests.SourceGen.Prism;

public sealed class PrismCatalogCompilerTests
{
    [Fact]
    public void RepositoryCatalogCompilesAllApprovedFamilies()
    {
        string catalogText = ReadRepositoryCatalog();
        PrismCatalogCompilation compilation = PrismCatalogCompiler.Compile(catalogText);
        JsonObject catalog = ParseCatalog(catalogText);
        JsonArray entries = catalog["entries"]!.AsArray();

        Assert.Empty(compilation.Issues);
        Assert.NotNull(compilation.GeneratedSource);
        Assert.Equal(134, CountKind(entries, "filter"));
        Assert.Equal(10, CountKind(entries, "style"));
        Assert.Equal(28, CountKind(entries, "blend-mode"));
        Assert.Equal(5, CountKind(entries, "color-profile"));
        Assert.Equal(1, CountKind(entries, "sampling"));
        Assert.Contains("public enum PrismFilterId", compilation.GeneratedSource, StringComparison.Ordinal);
        Assert.Contains("Blur =", compilation.GeneratedSource, StringComparison.Ordinal);
        Assert.Contains("DropShadow =", compilation.GeneratedSource, StringComparison.Ordinal);
        Assert.Contains("LinearSrgb =", compilation.GeneratedSource, StringComparison.Ordinal);
        Assert.Contains("CommonLayerProperties", compilation.GeneratedSource, StringComparison.Ordinal);
        Assert.Contains("PrismLayerPropertyKeys", compilation.GeneratedSource, StringComparison.Ordinal);
        Assert.Contains(
            "internal const float LayerOpacity = 1f;",
            compilation.GeneratedSource,
            StringComparison.Ordinal);
        Assert.Contains(
            "PrismCatalogValueType.Integer",
            compilation.GeneratedSource,
            StringComparison.Ordinal);
    }

    [Fact]
    public void DuplicateIdentifiersHavePreciseDiagnostic()
    {
        JsonObject catalog = ParseCatalog(ReadRepositoryCatalog());
        JsonArray entries = catalog["entries"]!.AsArray();
        entries[1]!["id"] = entries[0]!["id"]!.GetValue<string>();

        PrismCatalogIssue issue = Assert.Single(
            PrismCatalogCompiler.Compile(Serialize(catalog)).Issues,
            candidate => candidate.Id == "PRISM3002" &&
                candidate.Message.Contains("catalog identifier", StringComparison.Ordinal));

        Assert.Contains(entries[0]!["id"]!.GetValue<string>(), issue.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void IncompatibleDefaultHasPreciseDiagnostic()
    {
        JsonObject catalog = ParseCatalog(ReadRepositoryCatalog());
        JsonObject radius = FindProperty(catalog, "filter:blur", "radius");
        radius["default"] = "not-a-number";

        PrismCatalogIssue issue = Assert.Single(
            PrismCatalogCompiler.Compile(Serialize(catalog)).Issues,
            candidate => candidate.Id == "PRISM3003");

        Assert.Contains("entries[", issue.Message, StringComparison.Ordinal);
        Assert.Contains("valueType 'number'", issue.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void InvalidRangeHasPreciseDiagnostic()
    {
        JsonObject catalog = ParseCatalog(ReadRepositoryCatalog());
        JsonObject domain = FindProperty(catalog, "filter:blur", "radius")["domain"]!.AsObject();
        domain["minimum"] = 3;
        domain["maximum"] = 2;

        PrismCatalogIssue issue = Assert.Single(
            PrismCatalogCompiler.Compile(Serialize(catalog)).Issues,
            candidate => candidate.Id == "PRISM3004");

        Assert.Contains("minimum 3 exceeds maximum 2", issue.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void UnknownPropertyHasPreciseDiagnostic()
    {
        JsonObject catalog = ParseCatalog(ReadRepositoryCatalog());
        FindProperty(catalog, "filter:blur", "radius")["surprise"] = true;

        PrismCatalogIssue issue = Assert.Single(
            PrismCatalogCompiler.Compile(Serialize(catalog)).Issues,
            candidate => candidate.Id == "PRISM3006");

        Assert.Contains("unknown property 'surprise'", issue.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void MissingCoverageOwnerHasPreciseDiagnostic()
    {
        JsonObject catalog = ParseCatalog(ReadRepositoryCatalog());
        JsonObject blur = FindEntry(catalog, "filter:blur");
        blur["coverage"]!.AsObject().Remove("kernel");

        PrismCatalogIssue issue = Assert.Single(
            PrismCatalogCompiler.Compile(Serialize(catalog)).Issues,
            candidate => candidate.Id == "PRISM3005");

        Assert.Equal(
            "Catalog entry 'filter:blur' is missing coverage owner 'kernel'.",
            issue.Message);
    }

    [Fact]
    public void GeneratedOutputIsDeterministicAcrossEntryOrder()
    {
        JsonObject original = ParseCatalog(ReadRepositoryCatalog());
        JsonObject reordered = ParseCatalog(ReadRepositoryCatalog());
        JsonArray entries = reordered["entries"]!.AsArray();
        JsonNode?[] reversed = entries.Select(node => node!.DeepClone()).Reverse().ToArray();
        entries.Clear();
        foreach (JsonNode? entry in reversed)
        {
            entries.Add(entry);
        }

        PrismCatalogCompilation first = PrismCatalogCompiler.Compile(Serialize(original));
        PrismCatalogCompilation second = PrismCatalogCompiler.Compile(Serialize(reordered));

        Assert.Empty(first.Issues);
        Assert.Empty(second.Issues);
        Assert.Equal(first.GeneratedSource, second.GeneratedSource);
    }

    private static string ReadRepositoryCatalog()
    {
        string path = Path.GetFullPath(
            Path.Combine(
                AppContext.BaseDirectory,
                "..",
                "..",
                "..",
                "..",
                "..",
                "Cerneala.SourceGen",
                "Prism",
                "Catalog",
                "prism-catalog.json"));
        return File.ReadAllText(path);
    }

    private static JsonObject ParseCatalog(string text)
    {
        return JsonNode.Parse(text)?.AsObject()
            ?? throw new InvalidOperationException("Catalog JSON did not contain an object.");
    }

    private static JsonObject FindEntry(JsonObject catalog, string id)
    {
        return catalog["entries"]!
            .AsArray()
            .Select(entry => entry!.AsObject())
            .Single(entry => entry["id"]!.GetValue<string>() == id);
    }

    private static JsonObject FindProperty(JsonObject catalog, string entryId, string propertyId)
    {
        return FindEntry(catalog, entryId)["properties"]!
            .AsArray()
            .Select(property => property!.AsObject())
            .Single(property => property["id"]!.GetValue<string>() == propertyId);
    }

    private static int CountKind(JsonArray entries, string kind)
    {
        return entries.Count(entry => entry!["kind"]!.GetValue<string>() == kind);
    }

    private static string Serialize(JsonObject catalog)
    {
        return catalog.ToJsonString(new JsonSerializerOptions { WriteIndented = true });
    }
}
