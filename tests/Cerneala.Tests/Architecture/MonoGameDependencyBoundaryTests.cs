namespace Cerneala.Tests.Architecture;

public sealed class MonoGameDependencyBoundaryTests
{
    private static readonly string[] AdvancedInputForbiddenBackendTerms =
    [
        "MonoGame",
        "Microsoft.Xna",
        "Skia",
        "SkiaSharp",
        "HarfBuzz",
        "SpriteBatch",
        "Texture2D",
        "Mouse.GetState",
        "Keyboard.GetState",
        "GamePad.GetState",
        "TouchPanel.GetState",
        "System.Windows",
        "Windows.Win32",
        "Microsoft.Win32"
    ];

    [Fact]
    public void AdvancedInputBackendGuardCoversPlatformPollingAndOsBackendTerms()
    {
        string[] expectedTerms =
        [
            "Keyboard.GetState",
            "GamePad.GetState",
            "TouchPanel.GetState",
            "System.Windows",
            "Windows.Win32",
            "Microsoft.Win32"
        ];

        foreach (string term in expectedTerms)
        {
            Assert.Contains(term, AdvancedInputForbiddenBackendTerms, StringComparer.Ordinal);
        }
    }

    [Fact]
    public void AdvancedInputSourcesStayIndependentOfRenderingAndPlatformBackends()
    {
        string root = FindRepositoryRoot();
        string[] checkedRoots =
        [
            Path.Combine(root, "UI", "Input"),
            Path.Combine(root, "UI", "Ink"),
            Path.Combine(root, "UI", "Controls")
        ];
        string[] checkedFiles =
        [
            "TouchInputBridge.cs",
            "StylusInputBridge.cs",
            "GestureRecognizer.cs",
            "ManipulationProcessor.cs",
            "DragDropController.cs",
            "DataTransfer.cs",
            "Cursor.cs",
            "CursorService.cs",
            "Stroke.cs",
            "StrokeCollection.cs",
            "InkCanvas.cs"
        ];

        foreach (string file in checkedRoots.SelectMany(EnumerateSourceFiles).Where(file => checkedFiles.Contains(Path.GetFileName(file), StringComparer.Ordinal)))
        {
            string text = File.ReadAllText(file);
            string[] foundTerms = AdvancedInputForbiddenBackendTerms
                .Where(term => text.Contains(term, StringComparison.Ordinal))
                .ToArray();

            Assert.True(
                foundTerms.Length == 0,
                $"{Path.GetRelativePath(root, file)} references backend-specific advanced input terms: {string.Join(", ", foundTerms)}.");
        }
    }

    [Fact]
    public void MarkupSourcesStayIndependentOfRenderingBackends()
    {
        string root = FindRepositoryRoot();
        string markupRoot = Path.Combine(root, "UI", "Markup");
        string[] forbiddenTerms =
        [
            "MonoGame",
            "Microsoft.Xna",
            "Skia",
            "SkiaSharp",
            "HarfBuzz",
            "SkiaTextShaper",
            "SkiaTextRasterizer",
            "MonoGameDrawingBackend",
            "MonoGameImageLoader",
            "MonoGameUiHost",
            "MonoGameInput"
        ];

        foreach (string file in EnumerateSourceFiles(markupRoot))
        {
            string text = File.ReadAllText(file);
            string[] foundTerms = forbiddenTerms
                .Where(term => text.Contains(term, StringComparison.Ordinal))
                .ToArray();

            Assert.True(
                foundTerms.Length == 0,
                $"{Path.GetRelativePath(root, file)} references backend-specific markup dependency terms: {string.Join(", ", foundTerms)}.");
        }
    }

    [Fact]
    public void MonoGameHostAdapterReferencesStayUnderHostingMonoGame()
    {
        string root = FindRepositoryRoot();
        string monoGameHostRoot = Path.Combine(root, "UI", "Hosting", "MonoGame");
        string[] monoGameTerms =
        [
            "MonoGameUiHost",
            "MonoGameUiHostOptions",
            "MonoGameContentServices"
        ];

        foreach (string file in EnumerateSourceFiles(root))
        {
            string text = File.ReadAllText(file);
            bool containsMonoGameTerm = monoGameTerms.Any(term => text.Contains(term, StringComparison.Ordinal));
            if (!containsMonoGameTerm)
            {
                continue;
            }

            Assert.True(
                IsUnder(file, monoGameHostRoot) || IsTestFile(root, file) || IsPlaygroundFile(root, file),
                $"{Path.GetRelativePath(root, file)} references a MonoGame host adapter concept outside the adapter folder.");
        }
    }

    [Fact]
    public void MonoGameFrameworkDependenciesStayInKnownAdapterOrConsumerCode()
    {
        string root = FindRepositoryRoot();
        string[] allowedRoots =
        [
            Path.Combine(root, "UI", "Hosting", "MonoGame"),
            Path.Combine(root, "UI", "Input", "MonoGame"),
            Path.Combine(root, "UI", "Drawing", "MonoGame"),
            Path.Combine(root, "UI", "Resources", "MonoGame"),
            Path.Combine(root, "Playground")
        ];

        foreach (string file in EnumerateSourceFiles(root))
        {
            if (IsTestFile(root, file))
            {
                continue;
            }

            string text = File.ReadAllText(file);
            bool referencesMonoGameFramework = text.Contains("Microsoft.Xna.Framework", StringComparison.Ordinal) ||
                text.Contains("Texture2D", StringComparison.Ordinal) ||
                text.Contains("SpriteBatch", StringComparison.Ordinal);

            if (!referencesMonoGameFramework)
            {
                continue;
            }

            Assert.True(
                allowedRoots.Any(allowedRoot => IsUnder(file, allowedRoot)) || Path.GetFileName(file) == "GameBootstrap.cs",
                $"{Path.GetRelativePath(root, file)} references MonoGame framework APIs outside known adapter or consumer code.");
        }
    }

    [Fact]
    public void OptionalPackageSplitProjectsAreNotClaimedUnlessFilesExist()
    {
        string root = FindRepositoryRoot();
        string roadmap = File.ReadAllText(Path.Combine(root, "ROADMAPv2.md"));
        string[] optionalProjectFiles =
        [
            "Cerneala.Core.csproj",
            "Cerneala.MonoGame.csproj",
            "Cerneala.Tests.Core.csproj",
            "Cerneala.Tests.MonoGame.csproj"
        ];

        foreach (string projectFile in optionalProjectFiles)
        {
            bool exists = File.Exists(Path.Combine(root, projectFile));
            string expectedCheckbox = exists ? $"- [x] `{projectFile}`" : $"- [ ] `{projectFile}`";

            Assert.Contains(expectedCheckbox, roadmap, StringComparison.Ordinal);
        }
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

    private static bool IsTestFile(string root, string file)
    {
        return IsUnder(file, Path.Combine(root, "tests"));
    }

    private static bool IsPlaygroundFile(string root, string file)
    {
        return IsUnder(file, Path.Combine(root, "Playground"));
    }

    private static bool IsUnder(string file, string directory)
    {
        string relativePath = Path.GetRelativePath(directory, file);
        return relativePath != "." &&
            !relativePath.StartsWith("..", StringComparison.Ordinal) &&
            !Path.IsPathRooted(relativePath);
    }
}
