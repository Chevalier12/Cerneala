using System.Text.Json.Nodes;
using SkiaSharp;

namespace Cerneala.Tests.Drawing.Prism;

public sealed class PrismColorBlendStyleCoverageTests
{
    private const string UpdateGoldensVariable =
        "CERNEALA_UPDATE_PRISM_GOLDENS";
    private static readonly Lazy<CoverageMatrix> Matrix =
        new(CoverageMatrix.Load);

    public static IEnumerable<object[]> CoverageRows()
    {
        return Matrix.Value.Rows.Select(row => new object[] { row.Id });
    }

    [Fact]
    public void MatrixIsGeneratedFromEveryApprovedCatalogFamilyAndFeature()
    {
        CoverageMatrix matrix = Matrix.Value;
        Assert.Equal(
            matrix.CatalogEntryCount + matrix.FeatureCount,
            matrix.Rows.Count);
        Assert.Equal(
            matrix.Rows.Count,
            matrix.Rows.Select(row => row.Id).Distinct().Count());
        Assert.Contains(matrix.Rows, row => row.Kind == "color-profile");
        Assert.Contains(matrix.Rows, row => row.Kind == "blend-mode");
        Assert.Contains(matrix.Rows, row => row.Kind == "style");
        Assert.Contains(matrix.Rows, row => row.Kind == "advanced-blending");
        Assert.Contains(matrix.Rows, row => row.Kind == "blend-if");
        Assert.Contains(matrix.Rows, row => row.Kind == "mask");
        Assert.Contains(matrix.Rows, row => row.Kind == "clipping");
    }

    [Theory]
    [MemberData(nameof(CoverageRows))]
    public void EveryMatrixRowOwnsSemanticVisualKernelTestAndDocumentationCoverage(
        string id)
    {
        CoverageRow row = Matrix.Value.Rows.Single(candidate =>
            candidate.Id == id);

        Assert.Equal($"semantic:{row.Id}", row.SemanticCaseId);
        Assert.Equal($"visual:{row.Id}", row.VisualCaseId);
        Assert.False(string.IsNullOrWhiteSpace(row.Coverage.Runtime));
        Assert.False(string.IsNullOrWhiteSpace(row.Coverage.Kernel));
        Assert.False(string.IsNullOrWhiteSpace(row.Coverage.Test));
        Assert.False(string.IsNullOrWhiteSpace(row.Coverage.Documentation));
        Assert.InRange(row.Foreground.Alpha, (byte)1, (byte)254);
        Assert.InRange(row.Background.Alpha, (byte)1, (byte)254);
    }

    [Fact]
    public void MatrixDefaultsAreReadFromCatalogProperties()
    {
        CoverageMatrix matrix = Matrix.Value;

        foreach (CoverageRow row in matrix.Rows)
        {
            foreach ((string propertyRef, string? defaultValue) in
                row.PropertyDefaults)
            {
                Assert.Equal(
                    matrix.ResolveDefault(propertyRef),
                    defaultValue);
            }
        }
    }

    [Fact]
    public void MatrixRejectsAFeatureWithoutKernelTestOrDocumentationOwner()
    {
        JsonObject catalog = Matrix.Value.Catalog.DeepClone().AsObject();
        JsonObject coverage = catalog["conformance"]!["features"]!
            .AsArray()[0]!["coverage"]!.AsObject();

        foreach (string owner in new[] { "kernel", "test", "documentation" })
        {
            JsonNode? value = coverage[owner]?.DeepClone();
            coverage.Remove(owner);
            Assert.Throws<InvalidDataException>(
                () => CoverageMatrix.Build(
                    catalog,
                    Matrix.Value.Seed));
            coverage[owner] = value;
        }
    }

    [Fact]
    public void AnalyticInputsAndExactPixelCasesMatchVersionedImages()
    {
        CoverageMatrix matrix = Matrix.Value;
        string directory = Path.Combine(
            CoverageMatrix.RepositoryRoot,
            "tests",
            "Cerneala.Tests",
            "Golden",
            "Prism",
            "Analytic");
        Dictionary<string, SKBitmap> expected = new(StringComparer.Ordinal)
        {
            ["coverage-inputs.png"] =
                CreateCoverageInputs(matrix.Rows),
            ["alpha-extremes.png"] =
                CreateAlphaExtremes(),
            ["premultiplied-edge.png"] =
                CreatePremultipliedEdge(),
            ["multiply-opaque.png"] =
                CreateOpaqueMultiply()
        };

        try
        {
            foreach ((string fileName, SKBitmap bitmap) in expected)
            {
                string path = Path.Combine(directory, fileName);
                if (string.Equals(
                    Environment.GetEnvironmentVariable(
                        UpdateGoldensVariable),
                    "1",
                    StringComparison.Ordinal))
                {
                    Directory.CreateDirectory(directory);
                    WritePng(path, bitmap);
                }

                Assert.True(
                    File.Exists(path),
                    $"Missing analytic Prism image '{path}'.");
                using SKBitmap actual = SKBitmap.Decode(path) ??
                    throw new InvalidDataException(
                        $"Could not decode analytic Prism image '{path}'.");
                AssertBitmapEquals(bitmap, actual, fileName);
            }
        }
        finally
        {
            foreach (SKBitmap bitmap in expected.Values)
            {
                bitmap.Dispose();
            }
        }
    }

    private static SKBitmap CreateCoverageInputs(
        IReadOnlyList<CoverageRow> rows)
    {
        SKBitmap bitmap = new(rows.Count, 2);
        for (int x = 0; x < rows.Count; x++)
        {
            bitmap.SetPixel(x, 0, rows[x].Foreground);
            bitmap.SetPixel(x, 1, rows[x].Background);
        }

        return bitmap;
    }

    private static SKBitmap CreateAlphaExtremes()
    {
        SKBitmap bitmap = new(4, 1);
        bitmap.SetPixel(0, 0, new SKColor(0, 0, 0, 0));
        bitmap.SetPixel(1, 0, new SKColor(0, 0, 0, 255));
        bitmap.SetPixel(2, 0, new SKColor(255, 255, 255, 0));
        bitmap.SetPixel(3, 0, new SKColor(255, 255, 255, 255));
        return bitmap;
    }

    private static SKBitmap CreatePremultipliedEdge()
    {
        SKBitmap bitmap = new(4, 1);
        bitmap.SetPixel(0, 0, new SKColor(0, 0, 0, 0));
        bitmap.SetPixel(1, 0, new SKColor(25, 13, 6, 32));
        bitmap.SetPixel(2, 0, new SKColor(50, 25, 13, 64));
        bitmap.SetPixel(3, 0, new SKColor(100, 50, 25, 128));
        return bitmap;
    }

    private static SKBitmap CreateOpaqueMultiply()
    {
        (SKColor Foreground, SKColor Background)[] samples =
        [
            (new SKColor(255, 128, 64), new SKColor(64, 128, 255)),
            (new SKColor(32, 96, 160), new SKColor(224, 192, 128)),
            (new SKColor(255, 255, 255), new SKColor(17, 33, 65)),
            (new SKColor(0, 0, 0), new SKColor(238, 222, 190))
        ];
        SKBitmap bitmap = new(samples.Length, 1);
        for (int index = 0; index < samples.Length; index++)
        {
            SKColor foreground = samples[index].Foreground;
            SKColor background = samples[index].Background;
            bitmap.SetPixel(
                index,
                0,
                new SKColor(
                    Multiply(foreground.Red, background.Red),
                    Multiply(foreground.Green, background.Green),
                    Multiply(foreground.Blue, background.Blue)));
        }

        return bitmap;
    }

    private static byte Multiply(byte left, byte right)
    {
        return (byte)(((left * right) + 127) / 255);
    }

    private static void WritePng(string path, SKBitmap bitmap)
    {
        using SKImage image = SKImage.FromBitmap(bitmap);
        using SKData data =
            image.Encode(SKEncodedImageFormat.Png, quality: 100);
        using FileStream stream = File.Create(path);
        data.SaveTo(stream);
    }

    private static void AssertBitmapEquals(
        SKBitmap expected,
        SKBitmap actual,
        string name)
    {
        Assert.Equal(expected.Width, actual.Width);
        Assert.Equal(expected.Height, actual.Height);
        for (int y = 0; y < expected.Height; y++)
        {
            for (int x = 0; x < expected.Width; x++)
            {
                Assert.Equal(
                    expected.GetPixel(x, y),
                    actual.GetPixel(x, y));
            }
        }
    }

    internal sealed class CoverageMatrix
    {
        private static readonly HashSet<string> TargetKinds =
        [
            "color-profile",
            "blend-mode",
            "style"
        ];

        private CoverageMatrix(
            JsonObject catalog,
            int seed,
            IReadOnlyList<CoverageRow> rows,
            int catalogEntryCount,
            int featureCount)
        {
            Catalog = catalog;
            Seed = seed;
            Rows = rows;
            CatalogEntryCount = catalogEntryCount;
            FeatureCount = featureCount;
        }

        public static string RepositoryRoot { get; } =
            FindRepositoryRoot();

        public JsonObject Catalog { get; }

        public int Seed { get; }

        public IReadOnlyList<CoverageRow> Rows { get; }

        public int CatalogEntryCount { get; }

        public int FeatureCount { get; }

        public static CoverageMatrix Load()
        {
            JsonObject catalog = ReadObject(Path.Combine(
                RepositoryRoot,
                "Cerneala.SourceGen",
                "Prism",
                "Catalog",
                "prism-catalog.json"));
            JsonObject manifest = ReadObject(Path.Combine(
                RepositoryRoot,
                "tests",
                "Cerneala.Tests",
                "Golden",
                "Prism",
                "conformance.json"));
            return Build(catalog, manifest["seed"]!.GetValue<int>());
        }

        public static CoverageMatrix Build(
            JsonObject catalog,
            int seed)
        {
            List<CoverageRow> rows = [];
            JsonArray entries = catalog["entries"]?.AsArray() ??
                throw new InvalidDataException(
                    "Prism catalog is missing entries.");
            foreach (JsonObject entry in entries
                .Select(node => node!.AsObject())
                .Where(entry => TargetKinds.Contains(
                    RequiredString(entry, "kind"))))
            {
                string id = RequiredString(entry, "id");
                string kind = RequiredString(entry, "kind");
                List<string> propertyRefs = [];
                if (kind == "style")
                {
                    AddPropertyRefs(
                        propertyRefs,
                        catalog,
                        "style");
                }
                AddEntryPropertyRefs(propertyRefs, entry);
                rows.Add(CreateRow(
                    catalog,
                    seed,
                    id,
                    kind,
                    RequiredString(entry, "symbol"),
                    propertyRefs,
                    entry["coverage"]?.AsObject()));
            }

            int catalogEntryCount = rows.Count;
            JsonArray features =
                catalog["conformance"]?["features"]?.AsArray() ??
                throw new InvalidDataException(
                    "Prism catalog is missing conformance features.");
            foreach (JsonObject feature in
                features.Select(node => node!.AsObject()))
            {
                rows.Add(CreateRow(
                    catalog,
                    seed,
                    RequiredString(feature, "id"),
                    RequiredString(feature, "kind"),
                    RequiredString(feature, "symbol"),
                    feature["propertyRefs"]!.AsArray()
                        .Select(node => node!.GetValue<string>())
                        .ToArray(),
                    feature["coverage"]?.AsObject()));
            }

            return new CoverageMatrix(
                catalog,
                seed,
                rows,
                catalogEntryCount,
                features.Count);
        }

        public string? ResolveDefault(string propertyRef)
        {
            return ResolveProperty(Catalog, propertyRef)["default"]?
                .ToJsonString();
        }

        private static CoverageRow CreateRow(
            JsonObject catalog,
            int seed,
            string id,
            string kind,
            string symbol,
            IReadOnlyList<string> propertyRefs,
            JsonObject? coverage)
        {
            if (coverage is null)
            {
                throw new InvalidDataException(
                    $"Coverage row '{id}' is missing coverage.");
            }

            CoverageOwners owners = new(
                RequiredString(coverage, "runtime"),
                RequiredString(coverage, "kernel"),
                RequiredString(coverage, "test"),
                RequiredString(coverage, "documentation"));
            Dictionary<string, string?> defaults =
                new(StringComparer.Ordinal);
            foreach (string propertyRef in propertyRefs)
            {
                defaults.Add(
                    propertyRef,
                    ResolveProperty(catalog, propertyRef)["default"]?
                        .ToJsonString());
            }

            uint hash = StableHash(seed, id);
            SKColor foreground = NextColor(ref hash);
            SKColor background = NextColor(ref hash);
            return new CoverageRow(
                id,
                kind,
                symbol,
                $"semantic:{id}",
                $"visual:{id}",
                defaults,
                owners,
                foreground,
                background);
        }

        private static void AddPropertyRefs(
            List<string> target,
            JsonObject catalog,
            string scope)
        {
            JsonArray properties =
                catalog["commonProperties"]?[scope]?.AsArray() ??
                throw new InvalidDataException(
                    $"Prism catalog is missing common {scope} properties.");
            foreach (JsonObject property in
                properties.Select(node => node!.AsObject()))
            {
                target.Add(
                    $"commonProperties.{scope}." +
                    RequiredString(property, "name"));
            }
        }

        private static void AddEntryPropertyRefs(
            List<string> target,
            JsonObject entry)
        {
            string entryId = RequiredString(entry, "id");
            foreach (JsonObject property in entry["properties"]!
                .AsArray()
                .Select(node => node!.AsObject()))
            {
                target.Add(
                    $"entries.{entryId}." +
                    RequiredString(property, "name"));
            }
        }

        private static JsonObject ResolveProperty(
            JsonObject catalog,
            string propertyRef)
        {
            string[] segments = propertyRef.Split('.');
            if (segments.Length != 3)
            {
                throw new InvalidDataException(
                    $"Invalid catalog property reference '{propertyRef}'.");
            }

            JsonArray properties;
            if (segments[0] == "commonProperties")
            {
                properties =
                    catalog["commonProperties"]?[segments[1]]?.AsArray() ??
                    throw new InvalidDataException(
                        $"Unknown catalog property scope '{propertyRef}'.");
            }
            else if (segments[0] == "entries")
            {
                JsonObject entry = catalog["entries"]!.AsArray()
                    .Select(node => node!.AsObject())
                    .Single(candidate =>
                        RequiredString(candidate, "id") == segments[1]);
                properties = entry["properties"]!.AsArray();
            }
            else
            {
                throw new InvalidDataException(
                    $"Unknown catalog property scope '{propertyRef}'.");
            }

            return properties
                .Select(node => node!.AsObject())
                .Single(property =>
                    RequiredString(property, "name") == segments[2]);
        }

        private static JsonObject ReadObject(string path)
        {
            return JsonNode.Parse(File.ReadAllText(path))?.AsObject() ??
                throw new InvalidDataException(
                    $"JSON file '{path}' did not contain an object.");
        }

        private static string RequiredString(
            JsonObject owner,
            string property)
        {
            string? value = owner[property]?.GetValue<string>();
            return string.IsNullOrWhiteSpace(value)
                ? throw new InvalidDataException(
                    $"Catalog object is missing '{property}'.")
                : value;
        }

        private static uint StableHash(int seed, string value)
        {
            uint hash = unchecked((uint)seed) ^ 2166136261u;
            foreach (char character in value)
            {
                hash ^= character;
                hash *= 16777619u;
            }

            return hash;
        }

        private static SKColor NextColor(ref uint state)
        {
            byte red = NextByte(ref state, 24, 232);
            byte green = NextByte(ref state, 24, 232);
            byte blue = NextByte(ref state, 24, 232);
            byte alpha = NextByte(ref state, 64, 224);
            return new SKColor(red, green, blue, alpha);
        }

        private static byte NextByte(
            ref uint state,
            int minimum,
            int maximum)
        {
            state = (state * 1664525u) + 1013904223u;
            return (byte)(minimum +
                (state % (uint)(maximum - minimum)));
        }

        private static string FindRepositoryRoot()
        {
            DirectoryInfo? directory =
                new(AppContext.BaseDirectory);
            while (directory is not null)
            {
                if (File.Exists(
                    Path.Combine(directory.FullName, "Cerneala.slnx")))
                {
                    return directory.FullName;
                }

                directory = directory.Parent;
            }

            throw new DirectoryNotFoundException(
                "Could not locate the Cerneala repository root.");
        }
    }

    internal sealed record CoverageRow(
        string Id,
        string Kind,
        string Symbol,
        string SemanticCaseId,
        string VisualCaseId,
        IReadOnlyDictionary<string, string?> PropertyDefaults,
        CoverageOwners Coverage,
        SKColor Foreground,
        SKColor Background);

    internal sealed record CoverageOwners(
        string Runtime,
        string Kernel,
        string Test,
        string Documentation);
}
