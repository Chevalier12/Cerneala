using System.Security.Cryptography;
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
        Assert.Contains(
            "FilterImplementationMatrix",
            compilation.GeneratedSource,
            StringComparison.Ordinal);
        Assert.Contains(
            "PrismCatalogExecutionDescriptor",
            compilation.GeneratedSource,
            StringComparison.Ordinal);
        Assert.Contains(
            "[global::System.Flags]",
            compilation.GeneratedSource,
            StringComparison.Ordinal);
        Assert.Contains(
            "internal enum PrismCatalogCacheDependency",
            compilation.GeneratedSource,
            StringComparison.Ordinal);
        Assert.Contains(
            "PrismCatalogCacheDependency CacheDependencies",
            compilation.GeneratedSource,
            StringComparison.Ordinal);
        Assert.Contains(
            "CachePolicyMatrix = Entries;",
            compilation.GeneratedSource,
            StringComparison.Ordinal);
    }

    [Fact]
    public void EverySymbolParameterHasAValidPublicMetadataSeed()
    {
        PrismCatalogCompilation compilation = PrismCatalogCompiler.Compile(ReadRepositoryCatalog());

        Assert.Empty(compilation.Issues);
        PrismCatalogCompiler.CatalogProperty[] symbols = compilation.Model!.Entries
            .Where(entry => entry.Kind is "filter" or "style")
            .SelectMany(entry => entry.Properties)
            .Where(property => property.ValueType == "symbol")
            .ToArray();

        Assert.NotEmpty(symbols);
        Assert.All(symbols, property =>
        {
            Assert.False(property.Required);
            Assert.False(string.IsNullOrWhiteSpace(property.DefaultValue));
            Assert.Equal("catalog-symbol", property.Domain.Kind);
        });
    }

    [Fact]
    public void RepositoryCatalogGeneratesCompleteFilterImplementationMatrix()
    {
        string catalogText = ReadRepositoryCatalog();
        PrismCatalogCompilation compilation = PrismCatalogCompiler.Compile(catalogText);
        JsonObject catalog = ParseCatalog(catalogText);
        JsonObject[] catalogFilters = catalog["entries"]!
            .AsArray()
            .Select(entry => entry!.AsObject())
            .Where(entry => entry["kind"]!.GetValue<string>() == "filter")
            .ToArray();
        PrismCatalogCompiler.CatalogEntry[] compiledFilters = compilation.Model!.Entries
            .Where(entry => entry.Kind == "filter")
            .ToArray();

        Assert.Empty(compilation.Issues);
        Assert.Equal(catalogFilters.Length, compiledFilters.Length);
        Assert.Equal(
            compiledFilters.Length,
            CountOccurrences(compilation.GeneratedSource!, "        Entries["));

        for (int index = 0; index < compiledFilters.Length; index++)
        {
            PrismCatalogCompiler.CatalogEntry entry = compiledFilters[index];
            JsonObject source = catalogFilters[index];
            Assert.Equal(source["properties"]!.AsArray().Count, entry.Properties.Count);
            Assert.NotSame(PrismCatalogCompiler.CatalogExecutionProfile.Empty, entry.ExecutionProfile);
            Assert.False(string.IsNullOrWhiteSpace(entry.ExecutionProfile.Primitive));
            Assert.False(string.IsNullOrWhiteSpace(entry.ExecutionProfile.Bounds));
            Assert.False(string.IsNullOrWhiteSpace(entry.ExecutionProfile.Sampling));
            Assert.False(string.IsNullOrWhiteSpace(entry.ExecutionProfile.SurfaceFormat));
            Assert.False(string.IsNullOrWhiteSpace(entry.ExecutionProfile.ColorSpace));
            Assert.NotEmpty(entry.ExecutionProfile.GpuCapabilities);
            Assert.False(string.IsNullOrWhiteSpace(entry.Coverage.Runtime));
            Assert.False(string.IsNullOrWhiteSpace(entry.Coverage.Planner));
            Assert.False(string.IsNullOrWhiteSpace(entry.Coverage.Kernel));
            Assert.False(string.IsNullOrWhiteSpace(entry.Coverage.Test));
            Assert.False(string.IsNullOrWhiteSpace(entry.Coverage.Golden));
            Assert.False(string.IsNullOrWhiteSpace(entry.Coverage.Documentation));

            foreach (PrismCatalogCompiler.CatalogProperty property in entry.Properties)
            {
                Assert.False(string.IsNullOrWhiteSpace(property.Name));
                Assert.False(string.IsNullOrWhiteSpace(property.ValueType));
                Assert.False(string.IsNullOrWhiteSpace(property.Domain.Kind));
            }
        }
    }

    [Fact]
    public void CompletenessGateRejectsForgottenFilterAndPropertyWithoutParallelAllowlists()
    {
        JsonObject missingFilterImplementation = ParseCatalog(ReadRepositoryCatalog());
        JsonObject filter = missingFilterImplementation["entries"]!
            .AsArray()
            .Select(entry => entry!.AsObject())
            .First(entry => entry["kind"]!.GetValue<string>() == "filter");
        filter["coverage"]!.AsObject().Remove("kernel");

        PrismCatalogIssue missingFilterIssue = Assert.Single(
            PrismCatalogCompiler.Compile(Serialize(missingFilterImplementation)).Issues,
            issue => issue.Id == "PRISM3005");
        Assert.Contains(filter["id"]!.GetValue<string>(), missingFilterIssue.Message, StringComparison.Ordinal);

        JsonObject missingPropertyImplementation = ParseCatalog(ReadRepositoryCatalog());
        JsonObject property = missingPropertyImplementation["entries"]!
            .AsArray()
            .Select(entry => entry!.AsObject())
            .First(entry => entry["kind"]!.GetValue<string>() == "filter")["properties"]!
            .AsArray()[0]!
            .AsObject();
        property.Remove("domain");

        PrismCatalogIssue missingPropertyIssue = Assert.Single(
            PrismCatalogCompiler.Compile(Serialize(missingPropertyImplementation)).Issues,
            issue => issue.Id == "PRISM3001" &&
                issue.Message.Contains(".domain must be an object", StringComparison.Ordinal));
        Assert.Contains("entries[", missingPropertyIssue.Message, StringComparison.Ordinal);

        JsonObject missingClassification = ParseCatalog(ReadRepositoryCatalog());
        JsonArray profiles = missingClassification["executionProfiles"]!.AsArray();
        string category = profiles[0]!["category"]!.GetValue<string>();
        profiles.RemoveAt(0);

        var missingClassificationIssues =
            PrismCatalogCompiler.Compile(Serialize(missingClassification)).Issues;
        Assert.Contains(
            missingClassificationIssues,
            issue => issue.Id == "PRISM3007" &&
                issue.Message.Contains("has no execution profile", StringComparison.Ordinal));
        PrismCatalogIssue missingClassificationIssue = missingClassificationIssues.First(issue =>
            issue.Id == "PRISM3007" &&
            issue.Message.Contains("has no execution profile", StringComparison.Ordinal));
        Assert.Contains(category, missingClassificationIssue.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void SeededAndResourceFiltersHaveExplicitDeterministicInputs()
    {
        PrismCatalogCompilation compilation = PrismCatalogCompiler.Compile(ReadRepositoryCatalog());

        Assert.Empty(compilation.Issues);
        foreach (PrismCatalogCompiler.CatalogEntry filter in
            compilation.Model!.Entries.Where(entry => entry.Kind == "filter"))
        {
            bool seeded = filter.Capabilities.Contains("seeded", StringComparer.Ordinal);
            PrismCatalogCompiler.CatalogProperty? seed = filter.Properties.SingleOrDefault(property =>
                property.Name == "Seed");
            Assert.Equal(seeded, seed is not null);
            Assert.True(filter.Deterministic);

            string[] resources = filter.Properties
                .Where(property => property.ValueType == "resource")
                .Select(property => property.Name)
                .ToArray();
            Assert.Equal(
                resources.Length > 0,
                filter.Capabilities.Contains("auxiliary-resource", StringComparer.Ordinal));
        }
    }

    [Fact]
    public void GeneratedCachePolicyDependenciesCoverEveryCatalogEntry()
    {
        PrismCatalogCompilation compilation =
            PrismCatalogCompiler.Compile(ReadRepositoryCatalog());
        PrismCatalogCompiler.CatalogEntry[] entries =
            compilation.Model!.Entries.ToArray();
        string generated = compilation.GeneratedSource!;

        Assert.Empty(compilation.Issues);
        Assert.Equal(
            entries.Length,
            CountOccurrences(
                generated,
                "PrismCatalogCacheDependency.InputPixels"));
        Assert.Equal(
            entries.Count(entry => entry.Properties.Count > 0),
            CountOccurrences(
                generated,
                "PrismCatalogCacheDependency.ParameterValues"));
        Assert.Equal(
            entries.Count(entry =>
                entry.Capabilities.Contains(
                    "seeded",
                    StringComparer.Ordinal)),
            CountOccurrences(
                generated,
                "PrismCatalogCacheDependency.ExplicitSeed"));
        Assert.Equal(
            entries.Count(entry =>
                entry.Properties.Any(
                    property =>
                        property.ValueType == "resource")),
            CountOccurrences(
                generated,
                "PrismCatalogCacheDependency.VersionedResources"));
    }

    [Fact]
    public void NonDeterminismAndUnseededRandomnessCannotEnterCacheMatrix()
    {
        JsonObject nonDeterministic =
            ParseCatalog(ReadRepositoryCatalog());
        FindEntry(nonDeterministic, "filter:blur")["deterministic"] =
            false;
        Assert.Contains(
            PrismCatalogCompiler.Compile(
                    Serialize(nonDeterministic))
                .Issues,
            issue =>
                issue.Id == "PRISM3007" &&
                issue.Message.Contains(
                    "deterministic output",
                    StringComparison.Ordinal));

        JsonObject unseeded =
            ParseCatalog(ReadRepositoryCatalog());
        JsonObject seededEntry =
            unseeded["entries"]!
                .AsArray()
                .Select(entry => entry!.AsObject())
                .First(entry =>
                    entry["capabilities"]!
                        .AsArray()
                        .Any(capability =>
                            capability!.GetValue<string>() ==
                            "seeded"));
        JsonArray capabilities =
            seededEntry["capabilities"]!.AsArray();
        JsonNode seededCapability = capabilities.Single(
            capability =>
                capability!.GetValue<string>() == "seeded")!;
        capabilities.Remove(seededCapability);

        Assert.Contains(
            PrismCatalogCompiler.Compile(
                    Serialize(unseeded))
                .Issues,
            issue =>
                issue.Id == "PRISM3007" &&
                issue.Message.Contains(
                    "explicit Seed property",
                    StringComparison.Ordinal));
    }

    [Fact]
    public void RepositoryCatalogMarksOnlyProvenIdempotentThresholdFusion()
    {
        PrismCatalogCompilation compilation =
            PrismCatalogCompiler.Compile(ReadRepositoryCatalog());

        Assert.Empty(compilation.Issues);
        PrismCatalogCompiler.CatalogEntry fusion = Assert.Single(
            compilation.Model!.Entries.Where(entry => entry.Fusion is not null));
        Assert.Equal("filter:threshold", fusion.Id);
        Assert.Equal("same-parameters-idempotent", fusion.Fusion);
        Assert.Contains(
            "string? Fusion",
            compilation.GeneratedSource,
            StringComparison.Ordinal);
    }

    [Fact]
    public void UnknownFusionModeHasPreciseDiagnostic()
    {
        JsonObject catalog = ParseCatalog(ReadRepositoryCatalog());
        FindEntry(catalog, "filter:threshold")["fusion"] = "trust-me";

        PrismCatalogIssue issue = Assert.Single(
            PrismCatalogCompiler.Compile(Serialize(catalog)).Issues,
            candidate => candidate.Id == "PRISM3007" &&
                candidate.Message.Contains("unknown fusion mode", StringComparison.Ordinal));

        Assert.Contains("filter:threshold", issue.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void GeneratedFilterReferenceMatchesEveryCatalogEntryAndDefault()
    {
        PrismCatalogCompilation compilation =
            PrismCatalogCompiler.Compile(ReadRepositoryCatalog());
        string reference = ReadRepositoryFilterReference();
        string catalogHash = Convert.ToHexString(
                SHA256.HashData(
                    File.ReadAllBytes(RepositoryCatalogPath())))
            .ToLowerInvariant();
        PrismCatalogCompiler.CatalogEntry[] filters =
            compilation.Model!.Entries
                .Where(entry => entry.Kind == "filter")
                .OrderBy(entry => entry.StableId)
                .ToArray();

        Assert.Empty(compilation.Issues);
        Assert.Contains(
            $"catalog-sha256: {catalogHash}",
            reference,
            StringComparison.Ordinal);
        Assert.Equal(
            filters.Length,
            CountOccurrences(reference, "\n## `"));
        for (int index = 0; index < filters.Length; index++)
        {
            PrismCatalogCompiler.CatalogEntry filter = filters[index];
            string heading = $"## `{filter.Symbol}` (`{filter.Id}`)";
            int start = reference.IndexOf(
                heading,
                StringComparison.Ordinal);
            Assert.True(start >= 0, $"Missing generated reference heading '{heading}'.");
            int end = reference.IndexOf(
                "\n## `",
                start + heading.Length,
                StringComparison.Ordinal);
            string section = end >= 0
                ? reference.Substring(start, end - start)
                : reference.Substring(start);
            foreach (PrismCatalogCompiler.CatalogProperty property in
                filter.Properties)
            {
                Assert.Contains(
                    $"| `{property.Name}` |",
                    section,
                    StringComparison.Ordinal);
                if (property.DefaultValue is not null)
                {
                    Assert.Contains(
                        $"`{property.DefaultValue}`",
                        section,
                        StringComparison.Ordinal);
                }
            }
        }
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

    [Theory]
    [InlineData("planned:PrismKernelRegistry/Blur")]
    [InlineData("future:PrismKernelRegistry/Blur")]
    public void SpeculativeCoverageOwnerIsRejected(string owner)
    {
        JsonObject catalog = ParseCatalog(ReadRepositoryCatalog());
        JsonObject blur = FindEntry(catalog, "filter:blur");
        blur["coverage"]!["kernel"] = owner;

        PrismCatalogIssue issue = Assert.Single(
            PrismCatalogCompiler.Compile(Serialize(catalog)).Issues,
            candidate => candidate.Id == "PRISM3005");

        Assert.Contains("filter:blur", issue.Message, StringComparison.Ordinal);
        Assert.Contains(owner, issue.Message, StringComparison.Ordinal);
        Assert.Contains("not implemented", issue.Message, StringComparison.Ordinal);
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
        return File.ReadAllText(RepositoryCatalogPath());
    }

    private static string RepositoryCatalogPath()
    {
        return Path.GetFullPath(
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
    }

    private static string ReadRepositoryFilterReference()
    {
        string path = Path.GetFullPath(
            Path.Combine(
                AppContext.BaseDirectory,
                "..",
                "..",
                "..",
                "..",
                "..",
                "docs",
                "prism-filter-reference.generated.md"));
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

    private static int CountOccurrences(string text, string value)
    {
        int count = 0;
        int position = 0;
        while ((position = text.IndexOf(value, position, StringComparison.Ordinal)) >= 0)
        {
            count++;
            position += value.Length;
        }

        return count;
    }

    private static string Serialize(JsonObject catalog)
    {
        return catalog.ToJsonString(new JsonSerializerOptions { WriteIndented = true });
    }
}
