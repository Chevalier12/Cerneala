namespace Cerneala.Tests.Architecture;

public sealed class NamespaceBoundaryTests
{
    private static readonly string[] ForbiddenBackendTerms =
    [
        "Skia",
        "SkiaSharp",
        "HarfBuzz",
        "System.Windows",
        "Windows.Win32",
        "Microsoft.Win32"
    ];

    [Fact]
    public void RetainedUiCoreNamespacesDoNotReferenceBackendSpecificApis()
    {
        string root = FindRepositoryRoot();
        string uiRoot = Path.Combine(root, "UI");
        string[] allowedRoots =
        [
            Path.Combine(uiRoot, "Drawing")
        ];

        foreach (string file in EnumerateSourceFiles(uiRoot))
        {
            if (allowedRoots.Any(allowedRoot => IsUnder(file, allowedRoot)))
            {
                continue;
            }

            string[] foundTerms = FindForbiddenBackendTerms(File.ReadAllText(file));

            Assert.True(
                foundTerms.Length == 0,
                $"{Path.GetRelativePath(root, file)} references backend-specific APIs outside adapter or existing drawing/input foundation boundaries: {string.Join(", ", foundTerms)}.");
        }
    }

    [Fact]
    public void NamespaceBoundaryForbiddenTermListCoversCurrentBackendRisks()
    {
        string[] expectedTerms =
        [
            "SkiaSharp",
            "HarfBuzz"
        ];

        foreach (string term in expectedTerms)
        {
            Assert.Contains(term, ForbiddenBackendTerms, StringComparer.Ordinal);
        }
    }

    [Fact]
    public void BackendTermScanIgnoresCommentsAndStringLiterals()
    {
        const string source =
            "namespace Cerneala.UI.Controls;\n" +
            "\n" +
            "// Skia and HarfBuzz are backend examples, not references.\n" +
            "/* SkiaSharp is another documentation-only example. */\n" +
            "public sealed class DocumentationOnlyControl\n" +
            "{\n" +
            "    private const string BackendExample = \"SkiaSharp HarfBuzz\";\n" +
            "    private const string VerbatimBackendExample = @\"Windows.Win32 Microsoft.Win32\";\n" +
            "    private const string RawBackendExample =\n" +
            "        \"\"\"\n" +
            "        Skia System.Windows\n" +
            "        \"\"\";\n" +
            "}\n";

        string[] foundTerms = FindForbiddenBackendTerms(source);

        Assert.Empty(foundTerms);
    }

    private static string FindRepositoryRoot()
    {
        DirectoryInfo? directory = new(AppContext.BaseDirectory);
        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "Cerneala.slnx")))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        throw new DirectoryNotFoundException("Could not find repository root.");
    }

    private static IEnumerable<string> EnumerateSourceFiles(string root)
    {
        return Directory.EnumerateFiles(root, "*.cs", SearchOption.AllDirectories)
            .Where(file => !IsUnder(file, Path.Combine(root, "bin")))
            .Where(file => !IsUnder(file, Path.Combine(root, "obj")))
            .Where(file => !file.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}", StringComparison.OrdinalIgnoreCase))
            .Where(file => !file.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}", StringComparison.OrdinalIgnoreCase));
    }

    private static string[] FindForbiddenBackendTerms(string text)
    {
        string searchableText = CSharpSourceText.RemoveCommentsAndStringLiterals(text);

        return ForbiddenBackendTerms
            .Where(term => searchableText.Contains(term, StringComparison.Ordinal))
            .ToArray();
    }

    private static bool IsUnder(string file, string directory)
    {
        string relativePath = Path.GetRelativePath(directory, file);
        return relativePath != "." &&
            !relativePath.StartsWith("..", StringComparison.Ordinal) &&
            !Path.IsPathRooted(relativePath);
    }
}
