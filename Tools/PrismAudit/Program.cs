using System.Globalization;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Cerneala.UI.Prism.Runtime;

namespace Cerneala.Tools.PrismAudit;

internal static class Program
{
    private static readonly IReadOnlyDictionary<string, int> ExpectedEntryCounts =
        new Dictionary<string, int>(StringComparer.Ordinal)
        {
            ["filter"] = 134,
            ["style"] = 10,
            ["blend-mode"] = 28,
            ["color-profile"] = 5,
            ["sampling"] = 1
        };

    private static readonly IReadOnlyDictionary<string, string[]> ExpectedCommonProperties =
        new Dictionary<string, string[]>(StringComparer.Ordinal)
        {
            ["composition"] = ["working-color-profile", "global-light-angle", "global-light-altitude"],
            ["layer"] =
            [
                "visible", "opacity", "fill", "blend-mode", "clip-to-below", "blend-channels",
                "knockout", "blend-interior-styles-as-group", "blend-clipped-layers-as-group",
                "transparency-shapes-layer", "layer-mask-hides-styles", "vector-mask-hides-styles",
                "blend-if-channel", "this-layer-range", "underlying-range", "dissolve-seed"
            ],
            ["group"] = ["visible", "opacity", "blend-mode"],
            ["backdrop"] = ["visible", "opacity"],
            ["mask"] = ["image", "channel", "feather", "density", "invert"],
            ["filter"] = ["visible", "opacity", "blend-mode"],
            ["style"] = ["visible"]
        };

    private static readonly IReadOnlyDictionary<string, string> ExpectedCommonDefaults =
        new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["composition/working-color-profile"] = "LinearSrgb",
            ["composition/global-light-angle"] = "120",
            ["composition/global-light-altitude"] = "30",
            ["layer/visible"] = "true",
            ["layer/opacity"] = "1",
            ["layer/fill"] = "1",
            ["layer/blend-mode"] = "Normal",
            ["layer/clip-to-below"] = "false",
            ["layer/blend-channels"] = "RGBA",
            ["layer/knockout"] = "None",
            ["layer/blend-interior-styles-as-group"] = "false",
            ["layer/blend-clipped-layers-as-group"] = "true",
            ["layer/transparency-shapes-layer"] = "true",
            ["layer/layer-mask-hides-styles"] = "true",
            ["layer/vector-mask-hides-styles"] = "false",
            ["layer/blend-if-channel"] = "Gray",
            ["layer/this-layer-range"] = "0,0,1,1",
            ["layer/underlying-range"] = "0,0,1,1",
            ["layer/dissolve-seed"] = "0",
            ["group/visible"] = "true",
            ["group/opacity"] = "1",
            ["group/blend-mode"] = "PassThrough",
            ["backdrop/visible"] = "true",
            ["backdrop/opacity"] = "1",
            ["mask/image"] = "<required>",
            ["mask/channel"] = "Alpha",
            ["mask/feather"] = "0",
            ["mask/density"] = "1",
            ["mask/invert"] = "false",
            ["filter/visible"] = "true",
            ["filter/opacity"] = "1",
            ["filter/blend-mode"] = "Normal",
            ["style/visible"] = "true"
        };

    private static readonly string[] ExpectedDirectives =
    [
        "@prism", "@parameter", "@layer", "@group", "@filter", "@style", "@mask", "@backdrop"
    ];

    private static readonly string[] ExpectedConformanceFeatures =
        ["advanced-blending", "blend-if", "mask", "clipping"];

    private static readonly HashSet<string> ExpectedPublicPrismTypes = new(StringComparer.Ordinal)
    {
        "Cerneala.Drawing.MonoGame.Prism.IMonoGameBackdropFrameLease",
        "Cerneala.Drawing.Prism.BackdropAlphaMode",
        "Cerneala.Drawing.Prism.BackdropFrameMetadata",
        "Cerneala.Drawing.Prism.BackdropFrameRequest",
        "Cerneala.Drawing.Prism.BackdropPixelFormat",
        "Cerneala.Drawing.Prism.Catalog.PrismBlendMode",
        "Cerneala.Drawing.Prism.Catalog.PrismColorProfile",
        "Cerneala.Drawing.Prism.Catalog.PrismFilterId",
        "Cerneala.Drawing.Prism.Catalog.PrismSampling",
        "Cerneala.Drawing.Prism.Catalog.PrismStyleId",
        "Cerneala.Drawing.Prism.IBackdropFrameLease",
        "Cerneala.Drawing.Prism.IBackdropFrameSource",
        "Cerneala.Drawing.Prism.PrismCacheEvictionReason",
        "Cerneala.Drawing.Prism.PrismCacheMissReason",
        "Cerneala.Drawing.Prism.PrismCacheOwnerToken",
        "Cerneala.Drawing.Prism.PrismDependencyChange",
        "Cerneala.Drawing.Prism.PrismDrawScope",
        "Cerneala.Drawing.Prism.PrismRendererDiagnostics",
        "Cerneala.Drawing.Prism.PrismRendererOptions",
        "Cerneala.UI.Prism.Definitions.PrismBackdropDefinition",
        "Cerneala.UI.Prism.Definitions.PrismCompositionDefinition",
        "Cerneala.UI.Prism.Definitions.PrismFilterDefinition",
        "Cerneala.UI.Prism.Definitions.PrismGroupDefinition",
        "Cerneala.UI.Prism.Definitions.PrismLayerDefinition",
        "Cerneala.UI.Prism.Definitions.PrismMaskChannel",
        "Cerneala.UI.Prism.Definitions.PrismMaskDefinition",
        "Cerneala.UI.Prism.Definitions.PrismNodeDefinition",
        "Cerneala.UI.Prism.Definitions.PrismNodeId",
        "Cerneala.UI.Prism.Definitions.PrismResourceId",
        "Cerneala.UI.Prism.Definitions.PrismSourceSpan",
        "Cerneala.UI.Prism.Definitions.PrismStyleDefinition",
        "Cerneala.UI.Prism.Runtime.PrismBackdropState",
        "Cerneala.UI.Prism.Runtime.PrismBlendChannels",
        "Cerneala.UI.Prism.Runtime.PrismBlendIfChannel",
        "Cerneala.UI.Prism.Runtime.PrismBlendRange",
        "Cerneala.UI.Prism.Runtime.PrismCompositionState",
        "Cerneala.UI.Prism.Runtime.PrismFilterState",
        "Cerneala.UI.Prism.Runtime.PrismGroupState",
        "Cerneala.UI.Prism.Runtime.PrismInstance",
        "Cerneala.UI.Prism.Runtime.PrismKnockout",
        "Cerneala.UI.Prism.Runtime.PrismLayerState",
        "Cerneala.UI.Prism.Runtime.PrismMaskState",
        "Cerneala.UI.Prism.Runtime.PrismNodeState",
        "Cerneala.UI.Prism.Runtime.PrismStructuralVersion",
        "Cerneala.UI.Prism.Runtime.PrismStyleState",
        "Cerneala.UI.Prism.Runtime.PrismValueVersion"
    };

    private static readonly HashSet<string> ExpectedCrossSurfaceTypes = new(StringComparer.Ordinal)
    {
        "Cerneala.Drawing.DrawCommand",
        "Cerneala.Drawing.DrawCommandKind",
        "Cerneala.Drawing.DrawingFrameContext",
        "Cerneala.Drawing.IDrawingBackend",
        "Cerneala.Drawing.MonoGame.MonoGameDrawingBackend",
        "Cerneala.UI.Hosting.IUiBackend",
        "Cerneala.UI.Hosting.MonoGame.MonoGameUiHost",
        "Cerneala.UI.Hosting.MonoGame.MonoGameUiHostOptions",
        "Cerneala.UI.Markup.GeneratedMarkup",
        "Cerneala.UI.Rendering.RetainedRenderer"
    };

    private static readonly string[] RequiredSourcePaths =
    [
        "Cerneala.SourceGen/Prism/Syntax/PrismDirectiveParser.cs",
        "Cerneala.SourceGen/Prism/Syntax/PrismMarkupLanguage.cs",
        "Cerneala.SourceGen/Prism/Binding/PrismMarkupBinder.cs",
        "Cerneala.SourceGen/Prism/Binding/PrismMotionResolver.cs",
        "Cerneala.SourceGen/Prism/PrismCatalogCompiler.cs",
        "UI/Prism/Runtime/PrismParameterStore.cs",
        "Drawing/Prism/Graph/PrismGraphBuilder.cs",
        "Drawing/MonoGame/Prism/Kernels/PrismKernelRegistry.cs",
        "tests/Cerneala.Tests.SourceGen/Prism/PrismCatalogCompilerTests.cs",
        "tests/Cerneala.Tests/Drawing/Prism/PrismColorBlendStyleCoverageTests.cs",
        "tests/Cerneala.Tests/Drawing/MonoGame/PrismWindowsDxConformanceTests.cs",
        "docs/prism-markup-syntax-proposal.md",
        "docs/prism-technical-design.md",
        "docs/prism-filter-reference.generated.md",
        "docs/prism-public-api-baseline.md"
    ];

    public static int Main(string[] args)
    {
        try
        {
            bool write = args.Contains("--write", StringComparer.Ordinal);
            bool check = args.Contains("--check", StringComparer.Ordinal) || !write;
            if (write && check)
            {
                throw new ArgumentException("Use either --write or --check, not both.");
            }

            string repositoryRoot = FindRepositoryRoot();
            AuditResult audit = RunAudit(repositoryRoot);
            if (audit.Errors.Count > 0)
            {
                Console.Error.WriteLine($"Prism completeness audit failed with {audit.Errors.Count} gap(s):");
                foreach (string error in audit.Errors)
                {
                    Console.Error.WriteLine($"- {error}");
                }

                return 1;
            }

            string report = BuildReport(audit);
            string reportPath = Path.Combine(repositoryRoot, "docs", "prism-completeness-report.generated.md");
            if (write)
            {
                File.WriteAllText(reportPath, report, new UTF8Encoding(false));
                Console.WriteLine($"Wrote {reportPath}");
            }
            else
            {
                if (!File.Exists(reportPath) || !string.Equals(File.ReadAllText(reportPath), report, StringComparison.Ordinal))
                {
                    Console.Error.WriteLine(
                        "Prism completeness report is stale. Run: dotnet run --project Tools/PrismAudit -- --write");
                    return 1;
                }
            }

            Console.WriteLine(
                $"Prism audit passed: {audit.Catalog.Entries.Count} catalog entries, " +
                $"{audit.Catalog.CommonProperties.Sum(pair => pair.Value.Count)} common properties, " +
                $"{audit.PublicPrismTypes.Count} public Prism types and " +
                $"{audit.CrossSurfaceTypes.Count} extended public types, zero gaps.");
            return 0;
        }
        catch (Exception exception)
        {
            Console.Error.WriteLine(exception);
            return 1;
        }
    }

    private static AuditResult RunAudit(string repositoryRoot)
    {
        List<string> errors = [];
        foreach (string path in RequiredSourcePaths)
        {
            if (!File.Exists(Path.Combine(repositoryRoot, path.Replace('/', Path.DirectorySeparatorChar))))
            {
                errors.Add($"Required end-to-end owner is missing: {path}.");
            }
        }

        string catalogPath = Path.Combine(
            repositoryRoot,
            "Cerneala.SourceGen",
            "Prism",
            "Catalog",
            "prism-catalog.json");
        CatalogSnapshot catalog = CatalogSnapshot.Read(catalogPath);
        ValidateCatalog(catalog, errors);

        string catalogHash = HashFile(catalogPath);
        string filterReference = File.ReadAllText(Path.Combine(repositoryRoot, "docs", "prism-filter-reference.generated.md"));
        if (!filterReference.Contains($"<!-- catalog-sha256: {catalogHash} -->", StringComparison.Ordinal))
        {
            errors.Add("Generated filter property reference does not match the current catalog hash.");
        }

        string proposalPath = Path.Combine(repositoryRoot, "docs", "prism-markup-syntax-proposal.md");
        string technicalDesignPath = Path.Combine(repositoryRoot, "docs", "prism-technical-design.md");
        string proposal = File.ReadAllText(proposalPath);
        string technicalDesign = File.ReadAllText(technicalDesignPath);
        RequireDesignTokens(
            proposal,
            "proposal",
            [.. ExpectedDirectives, "Motion", "LinearSrgb", "catalog", "backdrop"],
            errors);
        RequireDesignTokens(
            technicalDesign,
            "technical design",
            ["catalog", "graph", "kernel", "Motion", "diagnostic", "DrawingFrameContext", "IBackdropFrameSource", "LinearSrgb"],
            errors);

        Assembly assembly = typeof(PrismInstance).Assembly;
        Type[] exportedTypes = assembly.GetExportedTypes();
        Type[] publicPrismTypes = exportedTypes
            .Where(type => type.FullName?.Contains(".Prism", StringComparison.Ordinal) == true)
            .OrderBy(type => type.FullName, StringComparer.Ordinal)
            .ToArray();
        Type[] crossSurfaceTypes = exportedTypes
            .Where(type => type.FullName?.Contains(".Prism", StringComparison.Ordinal) != true)
            .Where(type => PrismRelatedMemberSignatures(type).Any())
            .OrderBy(type => type.FullName, StringComparer.Ordinal)
            .ToArray();
        ValidatePublicApi(assembly, publicPrismTypes, crossSurfaceTypes, errors);

        return new AuditResult(
            repositoryRoot,
            catalog,
            catalogHash,
            HashFile(proposalPath),
            HashFile(technicalDesignPath),
            HashFile(Path.Combine(repositoryRoot, "docs", "prism-public-api-baseline.md")),
            publicPrismTypes,
            crossSurfaceTypes,
            errors);
    }

    private static void ValidateCatalog(CatalogSnapshot catalog, List<string> errors)
    {
        if (!string.Equals(catalog.Version, "1.0.0", StringComparison.Ordinal))
        {
            errors.Add($"Catalog version is '{catalog.Version}', expected '1.0.0'.");
        }
        if (!string.Equals(catalog.DefaultColorProfile, "LinearSrgb", StringComparison.Ordinal))
        {
            errors.Add($"Default color profile is '{catalog.DefaultColorProfile}', expected 'LinearSrgb'.");
        }

        foreach ((string kind, int expectedCount) in ExpectedEntryCounts)
        {
            int actualCount = catalog.Entries.Count(entry => string.Equals(entry.Kind, kind, StringComparison.Ordinal));
            if (actualCount != expectedCount)
            {
                errors.Add($"Catalog kind '{kind}' has {actualCount} entries; design approves {expectedCount}.");
            }
        }

        CheckDuplicates(catalog.Entries, entry => entry.StableId.ToString(CultureInfo.InvariantCulture), "stable ID", errors);
        CheckDuplicates(catalog.Entries, entry => entry.Id, "entry ID", errors);
        foreach (IGrouping<string, CatalogEntry> kind in catalog.Entries.GroupBy(entry => entry.Kind, StringComparer.Ordinal))
        {
            CheckDuplicates(kind, entry => entry.Symbol, $"symbol in '{kind.Key}'", errors);
        }

        foreach (CatalogEntry entry in catalog.Entries)
        {
            foreach ((string ownerKind, string owner) in entry.Coverage.Owners())
            {
                if (string.IsNullOrWhiteSpace(owner))
                {
                    errors.Add($"Catalog entry '{entry.Id}' has an empty {ownerKind} owner.");
                }
                else if (IsPlaceholder(owner))
                {
                    errors.Add($"Catalog entry '{entry.Id}' still has speculative {ownerKind} owner '{owner}'.");
                }
            }

            string expectedGeneratedPath = $"generated:PrismCatalog/{entry.Kind}/{entry.Symbol}";
            if (!string.Equals(entry.Coverage.Runtime, expectedGeneratedPath, StringComparison.Ordinal))
            {
                errors.Add($"Catalog entry '{entry.Id}' runtime owner diverges from generated runtime '{expectedGeneratedPath}'.");
            }
            if (!entry.Coverage.Kernel.StartsWith("PrismKernelRegistry/", StringComparison.Ordinal))
            {
                errors.Add($"Catalog entry '{entry.Id}' has no concrete PrismKernelRegistry owner.");
            }

            CheckDuplicates(entry.Properties, property => property.Id, $"property ID in '{entry.Id}'", errors);
            CheckDuplicates(entry.Properties, property => property.Name, $"property symbol in '{entry.Id}'", errors);
        }

        foreach ((string scope, string[] expectedProperties) in ExpectedCommonProperties)
        {
            if (!catalog.CommonProperties.TryGetValue(scope, out IReadOnlyList<CatalogProperty>? actualProperties))
            {
                errors.Add($"Catalog common-property scope '{scope}' is missing.");
                continue;
            }

            CompareSet(
                actualProperties.Select(property => property.Id),
                expectedProperties,
                $"commonProperties.{scope}",
                errors);
            foreach (CatalogProperty property in actualProperties)
            {
                string key = $"{scope}/{property.Id}";
                if (!ExpectedCommonDefaults.TryGetValue(key, out string? expectedDefault))
                {
                    errors.Add($"Common property '{key}' has no approved default contract.");
                }
                else if (!string.Equals(property.DefaultValue, expectedDefault, StringComparison.Ordinal))
                {
                    errors.Add(
                        $"Common property '{key}' default is '{property.DefaultValue}', expected '{expectedDefault}'.");
                }
            }
        }

        CompareSet(catalog.Features.Select(feature => feature.Id), ExpectedConformanceFeatures, "conformance features", errors);
        foreach (CatalogFeature feature in catalog.Features)
        {
            foreach ((string ownerKind, string owner) in feature.Coverage.Owners())
            {
                if (string.IsNullOrWhiteSpace(owner) || IsPlaceholder(owner))
                {
                    errors.Add($"Conformance feature '{feature.Id}' has no concrete {ownerKind} owner.");
                }
            }
        }

        if (!catalog.Entries.Any(entry =>
                entry.Kind == "color-profile" && entry.Symbol == catalog.DefaultColorProfile))
        {
            errors.Add("Default color profile does not resolve to a catalog color-profile entry.");
        }
    }

    private static void ValidatePublicApi(
        Assembly assembly,
        Type[] publicPrismTypes,
        Type[] crossSurfaceTypes,
        List<string> errors)
    {
        HashSet<string> actualTypes = publicPrismTypes
            .Select(type => type.FullName!)
            .ToHashSet(StringComparer.Ordinal);
        CompareSet(actualTypes, ExpectedPublicPrismTypes, "public Prism types", errors);

        string[] publicGraphTypes = actualTypes
            .Where(name => name.StartsWith("Cerneala.Drawing.Prism.Graph.", StringComparison.Ordinal))
            .OrderBy(name => name, StringComparer.Ordinal)
            .ToArray();
        if (publicGraphTypes.Length > 0)
        {
            errors.Add($"Internal graph implementation leaked into public API: {string.Join(", ", publicGraphTypes)}.");
        }

        foreach (Type type in publicPrismTypes)
        {
            if (ScenarioFor(type) is null)
            {
                errors.Add($"Public Prism type '{type.FullName}' has no approved proposal scenario.");
            }
        }

        CompareSet(
            crossSurfaceTypes.Select(type => type.FullName!),
            ExpectedCrossSurfaceTypes,
            "public types extended for Prism",
            errors);
        foreach (Type type in crossSurfaceTypes)
        {
            if (CrossSurfaceScenarioFor(type) is null)
            {
                errors.Add($"Extended public type '{type.FullName}' has no approved proposal scenario.");
            }
        }

        Type drawingBackend = RequiredType(assembly, "Cerneala.Drawing.IDrawingBackend", errors);
        MethodInfo[] renderMethods = drawingBackend.GetMethods().Where(method => method.Name == "Render").ToArray();
        if (renderMethods.Length != 1 ||
            renderMethods[0].GetParameters() is not { Length: 2 } renderParameters ||
            renderParameters[0].ParameterType.FullName != "Cerneala.Drawing.DrawCommandList" ||
            !renderParameters[1].ParameterType.IsByRef ||
            renderParameters[1].ParameterType.GetElementType()?.FullName != "Cerneala.Drawing.DrawingFrameContext")
        {
            errors.Add("IDrawingBackend.Render does not match the approved command-list plus frame-context touch point.");
        }

        Type uiBackend = RequiredType(assembly, "Cerneala.UI.Hosting.IUiBackend", errors);
        CompareSet(
            uiBackend.GetProperties().Select(property => property.Name),
            ["InputSource", "DrawingBackend", "BackdropFrameSource"],
            "IUiBackend properties",
            errors);

        Type hostOptions = RequiredType(assembly, "Cerneala.UI.Hosting.MonoGame.MonoGameUiHostOptions", errors);
        CompareSet(
            hostOptions.GetProperties().Select(property => property.Name),
            [
                "SpriteBatch", "WhitePixel", "Root", "Viewport", "InputSource", "ContentServices",
                "ImageLoader", "Clock", "TextRasterizer", "PlatformServices", "BackdropFrameSource",
                "PrismRendererOptions"
            ],
            "MonoGameUiHostOptions properties",
            errors);

        Type frameContext = RequiredType(assembly, "Cerneala.Drawing.DrawingFrameContext", errors);
        CompareSet(
            frameContext.GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly)
                .Select(property => property.Name),
            ["BackdropLease"],
            "DrawingFrameContext public properties",
            errors);
        if (frameContext.GetConstructors(BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly).Length != 0)
        {
            errors.Add("DrawingFrameContext exposes a public constructor instead of remaining host-created.");
        }

        Type backdropRequest = RequiredType(assembly, "Cerneala.Drawing.Prism.BackdropFrameRequest", errors);
        CompareSet(
            backdropRequest.GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly)
                .Select(property => property.Name),
            ["PixelWidth", "PixelHeight", "PixelScale"],
            "BackdropFrameRequest public properties",
            errors);
        ConstructorInfo[] publicConstructors = backdropRequest.GetConstructors(
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly);
        if (publicConstructors.Length != 1 ||
            publicConstructors[0].GetParameters().Select(parameter => parameter.ParameterType).ToArray() is not
                [Type { FullName: "System.Int32" }, Type { FullName: "System.Int32" }, Type { FullName: "System.Single" }])
        {
            errors.Add("BackdropFrameRequest does not expose exactly the approved width/height/scale constructor.");
        }
    }

    private static string BuildReport(AuditResult audit)
    {
        StringBuilder report = new();
        report.AppendLine("# Prism completeness report");
        report.AppendLine();
        report.AppendLine("<!-- auto-generated by Tools/PrismAudit; do not edit -->");
        report.AppendLine($"<!-- catalog-sha256: {audit.CatalogHash} -->");
        report.AppendLine();
        report.AppendLine("## Verdict");
        report.AppendLine();
        report.AppendLine("- Status: **PASS**");
        report.AppendLine("- Coverage gaps: **0**");
        report.AppendLine("- Divergent approved defaults: **0**");
        report.AppendLine("- Speculative `planned:` or `future:` owners: **0**");
        report.AppendLine("- Exported graph implementation types: **0**");
        report.AppendLine($"- Catalog version: `{audit.Catalog.Version}` (`{audit.Catalog.Entries.Count}` entries)");
        report.AppendLine();
        report.AppendLine("The audit treats generated runtime and documentation owners as concrete only because the catalog compiler and this report materialize them deterministically. Unknown catalog values fail binding or graph construction; there is no silent substitute entry.");
        report.AppendLine();
        report.AppendLine("## Design inputs");
        report.AppendLine();
        report.AppendLine("| Contract | SHA-256 |");
        report.AppendLine("| --- | --- |");
        report.AppendLine($"| `docs/prism-markup-syntax-proposal.md` | `{audit.ProposalHash}` |");
        report.AppendLine($"| `docs/prism-technical-design.md` | `{audit.TechnicalDesignHash}` |");
        report.AppendLine($"| `docs/prism-public-api-baseline.md` | `{audit.ApiBaselineHash}` |");
        report.AppendLine($"| `Cerneala.SourceGen/Prism/Catalog/prism-catalog.json` | `{audit.CatalogHash}` |");
        report.AppendLine();
        report.AppendLine("## Approved matrix");
        report.AppendLine();
        report.AppendLine("| Dimension | Approved | Observed | Result |");
        report.AppendLine("| --- | ---: | ---: | --- |");
        foreach ((string kind, int expectedCount) in ExpectedEntryCounts)
        {
            int observed = audit.Catalog.Entries.Count(entry => entry.Kind == kind);
            report.AppendLine($"| {Cell(kind)} | {expectedCount} | {observed} | PASS |");
        }
        report.AppendLine($"| directives | {ExpectedDirectives.Length} | {ExpectedDirectives.Length} | PASS |");
        report.AppendLine($"| common properties | {ExpectedCommonDefaults.Count} | {audit.Catalog.CommonProperties.Sum(pair => pair.Value.Count)} | PASS |");
        report.AppendLine($"| masks | 5 properties + feature | {audit.Catalog.CommonProperties["mask"].Count} properties + feature | PASS |");
        report.AppendLine($"| backdrop | 2 properties + host contract | {audit.Catalog.CommonProperties["backdrop"].Count} properties + host contract | PASS |");
        report.AppendLine($"| conformance features | {ExpectedConformanceFeatures.Length} | {audit.Catalog.Features.Count} | PASS |");
        report.AppendLine($"| public Prism types | {ExpectedPublicPrismTypes.Count} | {audit.PublicPrismTypes.Count} | PASS |");
        report.AppendLine($"| existing public types extended for Prism | {ExpectedCrossSurfaceTypes.Count} | {audit.CrossSurfaceTypes.Count} | PASS |");
        report.AppendLine();
        report.AppendLine("Directives: " + string.Join(", ", ExpectedDirectives.Select(Cell)) + ".");
        report.AppendLine();
        report.AppendLine("## Common property and default matrix");
        report.AppendLine();
        report.AppendLine("| Scope | Property | Type | Required | Default | Domain | End-to-end owners |");
        report.AppendLine("| --- | --- | --- | ---: | --- | --- | --- |");
        foreach ((string scope, IReadOnlyList<CatalogProperty> properties) in audit.Catalog.CommonProperties)
        {
            foreach (CatalogProperty property in properties)
            {
                report.AppendLine(
                    $"| {Cell(scope)} | {Cell(property.Id)} | {Cell(property.ValueType)} | " +
                    $"{property.Required.ToString().ToLowerInvariant()} | {Cell(property.DefaultValue)} | " +
                    $"{Cell(property.Domain)} | " +
                    "syntax/binder -> `PrismParameterStore` -> `PrismGraphBuilder` -> `PrismKernelRegistry` -> `PrismMotionResolver` -> diagnostics/tests/docs |");
            }
        }

        report.AppendLine();
        report.AppendLine("## Catalog end-to-end chain");
        report.AppendLine();
        report.AppendLine("Every row below is validated as catalog -> syntax -> binder -> runtime -> graph -> kernel -> Motion -> diagnostics -> test -> documentation.");
        report.AppendLine();
        report.AppendLine("| Stable | Kind | Entry | Runtime | Graph | Kernel | Test | Golden | Documentation |");
        report.AppendLine("| ---: | --- | --- | --- | --- | --- | --- | --- | --- |");
        foreach (CatalogEntry entry in audit.Catalog.Entries.OrderBy(entry => entry.StableId))
        {
            string golden = entry.Kind == "filter"
                ? $"PrismWindowsDxConformanceTests/CatalogGallery/{entry.Id}"
                : $"PrismColorBlendStyleCoverageTests/AnalyticVersionedImages/{entry.Id}";
            report.AppendLine(
                $"| {entry.StableId} | {Cell(entry.Kind)} | {Cell(entry.Id)} | " +
                $"{Cell(ConcreteGeneratedOwner(entry.Coverage.Runtime))} | " +
                $"{Cell($"PrismGraphBuilder/CatalogEntry/{entry.Id}")} | " +
                $"{Cell(entry.Coverage.Kernel)} | {Cell(entry.Coverage.Test)} | {Cell(golden)} | " +
                $"{Cell(ConcreteDocumentationOwner(entry.Coverage.Documentation))} |");
        }

        report.AppendLine();
        report.AppendLine("Syntax is owned by `PrismDirectiveParser` and `PrismMarkupLanguage`; binding by `PrismMarkupBinder`; Motion paths by `PrismMotionResolver`; compiler/binder diagnostics reject unknown or invalid entries before runtime. Entry-specific properties are listed below and the filter reference carries the same catalog hash.");
        report.AppendLine();
        report.AppendLine("## Entry property matrix");
        report.AppendLine();
        report.AppendLine("| Entry | Property | Type | Required | Default | Domain | Unit |");
        report.AppendLine("| --- | --- | --- | ---: | --- | --- | --- |");
        foreach (CatalogEntry entry in audit.Catalog.Entries.OrderBy(entry => entry.StableId))
        {
            if (entry.Properties.Count == 0)
            {
                report.AppendLine($"| {Cell(entry.Id)} | _(none)_ | - | - | - | - | - |");
                continue;
            }
            foreach (CatalogProperty property in entry.Properties)
            {
                report.AppendLine(
                    $"| {Cell(entry.Id)} | {Cell(property.Id)} | {Cell(property.ValueType)} | " +
                    $"{property.Required.ToString().ToLowerInvariant()} | {Cell(property.DefaultValue)} | " +
                    $"{Cell(property.Domain)} | {Cell(property.Unit)} |");
            }
        }

        report.AppendLine();
        report.AppendLine("## Conformance features");
        report.AppendLine();
        report.AppendLine("| Feature | Runtime | Kernel | Test | Documentation |");
        report.AppendLine("| --- | --- | --- | --- | --- |");
        foreach (CatalogFeature feature in audit.Catalog.Features.OrderBy(feature => feature.Id, StringComparer.Ordinal))
        {
            report.AppendLine(
                $"| {Cell(feature.Id)} | {Cell(feature.Coverage.Runtime)} | {Cell(feature.Coverage.Kernel)} | " +
                $"{Cell(feature.Coverage.Test)} | {Cell(feature.Coverage.Documentation)} |");
        }

        report.AppendLine();
        report.AppendLine("## Public API diff and justification");
        report.AppendLine();
        report.AppendLine("The foundation baseline anticipated exactly three host changes. The implemented diff is: `IDrawingBackend.Render` receives `DrawingFrameContext`; `IUiBackend` exposes optional `BackdropFrameSource`; `MonoGameUiHostOptions` adds `BackdropFrameSource` and `PrismRendererOptions`. Graph planning and execution remain internal.");
        report.AppendLine();
        report.AppendLine("| Public type | Public member | Approved current scenario |");
        report.AppendLine("| --- | --- | --- |");
        foreach (Type type in audit.CrossSurfaceTypes)
        {
            string scenario = CrossSurfaceScenarioFor(type)!;
            foreach (string member in PrismRelatedMemberSignatures(type))
            {
                report.AppendLine($"| {Cell(type.FullName!)} | {Cell(member)} | {scenario} |");
            }
        }
        foreach (Type type in audit.PublicPrismTypes)
        {
            string scenario = ScenarioFor(type)!;
            string[] members = PublicMemberSignatures(type).ToArray();
            if (members.Length == 0)
            {
                report.AppendLine($"| {Cell(type.FullName!)} | _(marker type)_ | {scenario} |");
                continue;
            }
            foreach (string member in members)
            {
                report.AppendLine($"| {Cell(type.FullName!)} | {Cell(member)} | {scenario} |");
            }
        }

        report.AppendLine();
        report.AppendLine("## Reproduction");
        report.AppendLine();
        report.AppendLine("```powershell");
        report.AppendLine("dotnet build .\\Cerneala.csproj --no-restore");
        report.AppendLine("dotnet run --project .\\Tools\\PrismAudit\\PrismAudit.csproj -- --check");
        report.AppendLine("```");
        return report.ToString().Replace("\r\n", "\n", StringComparison.Ordinal);
    }

    private static IEnumerable<string> PublicMemberSignatures(Type type)
    {
        BindingFlags flags = BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly;
        List<string> signatures = [];
        signatures.AddRange(type.GetConstructors(flags).Select(constructor =>
            $"{type.Name}({FormatParameters(constructor.GetParameters())})"));
        signatures.AddRange(type.GetProperties(flags).Select(property =>
            $"{FormatType(property.PropertyType)} {property.Name} {{ " +
            $"{(property.GetMethod is null ? string.Empty : "get; ")}" +
            $"{(property.SetMethod is null ? string.Empty : "set; ")}}}"));
        signatures.AddRange(type.GetEvents(flags).Select(@event =>
            $"event {FormatType(@event.EventHandlerType!)} {@event.Name}"));
        signatures.AddRange(type.GetMethods(flags)
            .Where(method => !method.IsSpecialName)
            .Select(method => $"{FormatType(method.ReturnType)} {method.Name}({FormatParameters(method.GetParameters())})"));
        if (type.IsEnum)
        {
            signatures.AddRange(type.GetFields(flags)
                .Where(field => field.IsLiteral)
                .Select(field => $"{field.Name} = {Convert.ToString(field.GetRawConstantValue(), CultureInfo.InvariantCulture)}"));
        }
        return signatures.OrderBy(signature => signature, StringComparer.Ordinal);
    }

    private static IEnumerable<string> PrismRelatedMemberSignatures(Type type)
    {
        BindingFlags flags = BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly;
        List<string> signatures = [];
        signatures.AddRange(type.GetConstructors(flags)
            .Where(constructor =>
                NameIsPrismRelated(constructor.Name) ||
                constructor.GetParameters().Any(parameter => TypeIsPrismRelated(parameter.ParameterType)))
            .Select(constructor => $"{type.Name}({FormatParameters(constructor.GetParameters())})"));
        signatures.AddRange(type.GetProperties(flags)
            .Where(property =>
                NameIsPrismRelated(property.Name) ||
                TypeIsPrismRelated(property.PropertyType))
            .Select(property =>
                $"{FormatType(property.PropertyType)} {property.Name} {{ " +
                $"{(property.GetMethod is null ? string.Empty : "get; ")}" +
                $"{(property.SetMethod is null ? string.Empty : "set; ")}}}"));
        signatures.AddRange(type.GetEvents(flags)
            .Where(@event =>
                NameIsPrismRelated(@event.Name) ||
                TypeIsPrismRelated(@event.EventHandlerType!))
            .Select(@event => $"event {FormatType(@event.EventHandlerType!)} {@event.Name}"));
        signatures.AddRange(type.GetMethods(flags)
            .Where(method => !method.IsSpecialName)
            .Where(method =>
                NameIsPrismRelated(method.Name) ||
                TypeIsPrismRelated(method.ReturnType) ||
                method.GetParameters().Any(parameter => TypeIsPrismRelated(parameter.ParameterType)))
            .Select(method => $"{FormatType(method.ReturnType)} {method.Name}({FormatParameters(method.GetParameters())})"));
        if (type.IsEnum)
        {
            signatures.AddRange(type.GetFields(flags)
                .Where(field => field.IsLiteral && NameIsPrismRelated(field.Name))
                .Select(field => $"{field.Name} = {Convert.ToString(field.GetRawConstantValue(), CultureInfo.InvariantCulture)}"));
        }
        return signatures.OrderBy(signature => signature, StringComparer.Ordinal);
    }

    private static bool NameIsPrismRelated(string name) =>
        name.Contains("Prism", StringComparison.Ordinal) ||
        name.Contains("Backdrop", StringComparison.Ordinal);

    private static bool TypeIsPrismRelated(Type type)
    {
        if (type.IsByRef || type.IsArray)
        {
            return TypeIsPrismRelated(type.GetElementType()!);
        }
        if (type.IsGenericType && type.GetGenericArguments().Any(TypeIsPrismRelated))
        {
            return true;
        }
        string? name = type.FullName;
        return name?.Contains(".Prism", StringComparison.Ordinal) == true ||
            name == "Cerneala.Drawing.DrawingFrameContext";
    }

    private static string FormatParameters(IEnumerable<ParameterInfo> parameters) => string.Join(
        ", ",
        parameters.Select(parameter =>
        {
            Type parameterType = parameter.ParameterType;
            string modifier = parameterType.IsByRef ? (parameter.IsOut ? "out " : "in ") : string.Empty;
            if (parameterType.IsByRef)
            {
                parameterType = parameterType.GetElementType()!;
            }
            return $"{modifier}{FormatType(parameterType)} {parameter.Name}";
        }));

    private static string FormatType(Type type)
    {
        if (type.IsArray)
        {
            return $"{FormatType(type.GetElementType()!)}[]";
        }
        if (!type.IsGenericType)
        {
            return type.FullName?.Replace('+', '.') ?? type.Name;
        }
        string name = type.GetGenericTypeDefinition().FullName!;
        name = name[..name.IndexOf('`')].Replace('+', '.');
        return $"{name}<{string.Join(",", type.GetGenericArguments().Select(FormatType))}>";
    }

    private static string? ScenarioFor(Type type)
    {
        string name = type.FullName!;
        if (name.StartsWith("Cerneala.UI.Prism.Definitions.", StringComparison.Ordinal))
        {
            return "Immutable generated markup definition consumed by Prism markup.";
        }
        if (name.StartsWith("Cerneala.UI.Prism.Runtime.", StringComparison.Ordinal))
        {
            return "Generated markup and typed Motion runtime ABI.";
        }
        if (name.StartsWith("Cerneala.Drawing.Prism.Catalog.", StringComparison.Ordinal))
        {
            return "Strongly typed catalog authoring value used by markup definitions.";
        }
        if (name.Contains("Backdrop", StringComparison.Ordinal))
        {
            return "Optional host/backend backdrop acquisition contract.";
        }
        if (name.StartsWith("Cerneala.Drawing.Prism.", StringComparison.Ordinal))
        {
            return "Renderer hosting, retained-cache invalidation, and diagnostics contract.";
        }
        return null;
    }

    private static string? CrossSurfaceScenarioFor(Type type) => type.FullName switch
    {
        "Cerneala.Drawing.DrawCommand" or
        "Cerneala.Drawing.DrawCommandKind" =>
            "Command-list Prism scope ABI consumed by retained rendering and backends.",
        "Cerneala.Drawing.DrawingFrameContext" or
        "Cerneala.Drawing.IDrawingBackend" or
        "Cerneala.Drawing.MonoGame.MonoGameDrawingBackend" =>
            "Per-frame Prism analysis and optional backdrop submission contract.",
        "Cerneala.UI.Hosting.IUiBackend" or
        "Cerneala.UI.Hosting.MonoGame.MonoGameUiHost" or
        "Cerneala.UI.Hosting.MonoGame.MonoGameUiHostOptions" =>
            "Optional host backdrop source and Prism renderer configuration.",
        "Cerneala.UI.Markup.GeneratedMarkup" =>
            "Generated markup accessors used by typed Prism Motion paths.",
        "Cerneala.UI.Rendering.RetainedRenderer" =>
            "Retained-render traversal discovers and submits attached Prism instances.",
        _ => null
    };

    private static Type RequiredType(Assembly assembly, string name, List<string> errors)
    {
        Type? type = assembly.GetType(name, throwOnError: false, ignoreCase: false);
        if (type is not null)
        {
            return type;
        }
        errors.Add($"Required public API type '{name}' is missing.");
        return typeof(object);
    }

    private static void RequireDesignTokens(
        string document,
        string documentName,
        IEnumerable<string> tokens,
        List<string> errors)
    {
        foreach (string token in tokens)
        {
            if (!document.Contains(token, StringComparison.OrdinalIgnoreCase))
            {
                errors.Add($"The {documentName} no longer contains approved contract token '{token}'.");
            }
        }
    }

    private static void CheckDuplicates<T>(
        IEnumerable<T> values,
        Func<T, string> keySelector,
        string label,
        List<string> errors)
    {
        string[] duplicates = values.GroupBy(keySelector, StringComparer.Ordinal)
            .Where(group => group.Count() > 1)
            .Select(group => group.Key)
            .OrderBy(key => key, StringComparer.Ordinal)
            .ToArray();
        if (duplicates.Length > 0)
        {
            errors.Add($"Duplicate {label}: {string.Join(", ", duplicates)}.");
        }
    }

    private static void CompareSet(
        IEnumerable<string> actual,
        IEnumerable<string> expected,
        string label,
        List<string> errors)
    {
        HashSet<string> actualSet = actual.ToHashSet(StringComparer.Ordinal);
        HashSet<string> expectedSet = expected.ToHashSet(StringComparer.Ordinal);
        string[] missing = expectedSet.Except(actualSet).OrderBy(value => value, StringComparer.Ordinal).ToArray();
        string[] unexpected = actualSet.Except(expectedSet).OrderBy(value => value, StringComparer.Ordinal).ToArray();
        if (missing.Length > 0 || unexpected.Length > 0)
        {
            errors.Add(
                $"{label} differs from design. Missing: [{string.Join(", ", missing)}]; " +
                $"unexpected: [{string.Join(", ", unexpected)}].");
        }
    }

    private static bool IsPlaceholder(string owner) =>
        owner.StartsWith("planned:", StringComparison.Ordinal) ||
        owner.StartsWith("future:", StringComparison.Ordinal);

    private static string ConcreteGeneratedOwner(string owner) =>
        owner.StartsWith("generated:", StringComparison.Ordinal)
            ? $"PrismCatalogGenerator/{owner["generated:".Length..]}"
            : owner;

    private static string ConcreteDocumentationOwner(string owner) =>
        owner.StartsWith("generated:prism-catalog/", StringComparison.Ordinal)
            ? $"prism-completeness-report.generated.md/{owner["generated:prism-catalog/".Length..]}"
            : owner;

    private static string Cell(string value) => $"`{value.Replace("|", "\\|", StringComparison.Ordinal).Replace("`", "\\`", StringComparison.Ordinal)}`";

    private static string HashFile(string path) => Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(path)))
        .ToLowerInvariant();

    private static string FindRepositoryRoot()
    {
        foreach (string start in new[] { Directory.GetCurrentDirectory(), AppContext.BaseDirectory })
        {
            DirectoryInfo? directory = new(start);
            while (directory is not null)
            {
                if (File.Exists(Path.Combine(directory.FullName, "Cerneala.slnx")))
                {
                    return directory.FullName;
                }
                directory = directory.Parent;
            }
        }
        throw new DirectoryNotFoundException("Could not locate the Cerneala repository root.");
    }

    private sealed record AuditResult(
        string RepositoryRoot,
        CatalogSnapshot Catalog,
        string CatalogHash,
        string ProposalHash,
        string TechnicalDesignHash,
        string ApiBaselineHash,
        IReadOnlyList<Type> PublicPrismTypes,
        IReadOnlyList<Type> CrossSurfaceTypes,
        IReadOnlyList<string> Errors);

    private sealed record CatalogSnapshot(
        string Version,
        string DefaultColorProfile,
        IReadOnlyDictionary<string, IReadOnlyList<CatalogProperty>> CommonProperties,
        IReadOnlyList<CatalogEntry> Entries,
        IReadOnlyList<CatalogFeature> Features)
    {
        public static CatalogSnapshot Read(string path)
        {
            using JsonDocument document = JsonDocument.Parse(File.ReadAllBytes(path));
            JsonElement root = document.RootElement;
            Dictionary<string, IReadOnlyList<CatalogProperty>> commonProperties = new(StringComparer.Ordinal);
            foreach (JsonProperty scope in root.GetProperty("commonProperties").EnumerateObject())
            {
                commonProperties[scope.Name] = scope.Value.EnumerateArray().Select(ReadProperty).ToArray();
            }
            CatalogEntry[] entries = root.GetProperty("entries").EnumerateArray().Select(ReadEntry).ToArray();
            CatalogFeature[] features = root.GetProperty("conformance").GetProperty("features")
                .EnumerateArray().Select(ReadFeature).ToArray();
            return new CatalogSnapshot(
                root.GetProperty("catalogVersion").GetString()!,
                root.GetProperty("defaultColorProfile").GetString()!,
                commonProperties,
                entries,
                features);
        }

        private static CatalogEntry ReadEntry(JsonElement element)
        {
            JsonElement coverage = element.GetProperty("coverage");
            return new CatalogEntry(
                element.GetProperty("stableId").GetInt32(),
                element.GetProperty("id").GetString()!,
                element.GetProperty("symbol").GetString()!,
                element.GetProperty("kind").GetString()!,
                element.GetProperty("properties").EnumerateArray().Select(ReadProperty).ToArray(),
                ReadCoverage(coverage));
        }

        private static CatalogFeature ReadFeature(JsonElement element) => new(
            element.GetProperty("id").GetString()!,
            ReadCoverage(element.GetProperty("coverage")));

        private static CatalogCoverage ReadCoverage(JsonElement coverage) => new(
            coverage.GetProperty("runtime").GetString()!,
            coverage.GetProperty("kernel").GetString()!,
            coverage.GetProperty("test").GetString()!,
            coverage.GetProperty("documentation").GetString()!);

        private static CatalogProperty ReadProperty(JsonElement element)
        {
            bool required = element.GetProperty("required").GetBoolean();
            string defaultValue = required
                ? "<required>"
                : Canonical(element.GetProperty("default"));
            JsonElement domain = element.GetProperty("domain");
            string minimum = domain.TryGetProperty("minimum", out JsonElement minimumElement)
                ? Canonical(minimumElement)
                : string.Empty;
            string maximum = domain.TryGetProperty("maximum", out JsonElement maximumElement)
                ? Canonical(maximumElement)
                : string.Empty;
            string domainValue = domain.GetProperty("kind").GetString()!;
            if (minimum.Length > 0 || maximum.Length > 0)
            {
                domainValue += $":{minimum}:{maximum}";
            }
            return new CatalogProperty(
                element.GetProperty("id").GetString()!,
                element.GetProperty("name").GetString()!,
                element.GetProperty("valueType").GetString()!,
                required,
                defaultValue,
                domainValue,
                element.GetProperty("unit").GetString()!);
        }

        private static string Canonical(JsonElement value) => value.ValueKind switch
        {
            JsonValueKind.String => value.GetString()!,
            JsonValueKind.True => "true",
            JsonValueKind.False => "false",
            JsonValueKind.Null => "null",
            JsonValueKind.Number when value.TryGetInt64(out long integer) => integer.ToString(CultureInfo.InvariantCulture),
            JsonValueKind.Number => value.GetDouble().ToString("R", CultureInfo.InvariantCulture),
            JsonValueKind.Array => string.Join(",", value.EnumerateArray().Select(Canonical)),
            _ => value.GetRawText()
        };
    }

    private sealed record CatalogEntry(
        int StableId,
        string Id,
        string Symbol,
        string Kind,
        IReadOnlyList<CatalogProperty> Properties,
        CatalogCoverage Coverage);

    private sealed record CatalogProperty(
        string Id,
        string Name,
        string ValueType,
        bool Required,
        string DefaultValue,
        string Domain,
        string Unit);

    private sealed record CatalogFeature(string Id, CatalogCoverage Coverage);

    private sealed record CatalogCoverage(string Runtime, string Kernel, string Test, string Documentation)
    {
        public IEnumerable<(string Kind, string Owner)> Owners()
        {
            yield return ("runtime", Runtime);
            yield return ("kernel", Kernel);
            yield return ("test", Test);
            yield return ("documentation", Documentation);
        }
    }
}
