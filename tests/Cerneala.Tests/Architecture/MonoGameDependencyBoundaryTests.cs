namespace Cerneala.Tests.Architecture;

public sealed class MonoGameDependencyBoundaryTests
{
    [Fact]
    public void MonoGameHostAdapterReferencesStayUnderHostingMonoGame()
    {
        string root = FindRepositoryRoot();
        string monoGameHostRoot = Path.Combine(root, "UI", "Hosting", "MonoGame");
        string prismAuditRoot = Path.Combine(root, "Tools", "PrismAudit");
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
                IsUnder(file, monoGameHostRoot) ||
                    IsUnder(file, prismAuditRoot) ||
                    IsTestFile(root, file) ||
                    IsPlaygroundFile(root, file),
                $"{Path.GetRelativePath(root, file)} references a MonoGame host adapter concept outside the adapter folder.");
        }
    }

    [Fact]
    public void CoreProjectDoesNotReferenceOrCompileMonoGame()
    {
        string root = FindRepositoryRoot();
        string project = File.ReadAllText(Path.Combine(root, "Cerneala.csproj"));

        Assert.DoesNotContain("MonoGame.Framework", project, StringComparison.Ordinal);
        Assert.DoesNotContain("PrismShaderSource", project, StringComparison.Ordinal);
        Assert.Contains("<Compile Remove=\"Drawing\\MonoGame\\**\"", project, StringComparison.Ordinal);
        Assert.Contains("<Compile Remove=\"Cerneala.Platforms.Win32\\**\"", project, StringComparison.Ordinal);
        Assert.Contains("<Compile Remove=\"UI\\Hosting\\MonoGame\\**\"", project, StringComparison.Ordinal);
        Assert.Contains("<Compile Remove=\"UI\\Input\\MonoGame\\**\"", project, StringComparison.Ordinal);
        Assert.Contains("<Compile Remove=\"UI\\Resources\\MonoGame\\**\"", project, StringComparison.Ordinal);
        Assert.Contains("<Compile Remove=\"UI\\Hosting\\Windows\\WindowsDxWindowGraphicsSession.cs\"", project, StringComparison.Ordinal);
    }

    [Fact]
    public void MonoGameBackendProjectOwnsFrameworkAndShaderDependencies()
    {
        string root = FindRepositoryRoot();
        string project = File.ReadAllText(Path.Combine(
            root,
            "Cerneala.Backends.MonoGame",
            "Cerneala.Backends.MonoGame.csproj"));

        Assert.Contains("MonoGame.Framework.WindowsDX", project, StringComparison.Ordinal);
        Assert.Contains("<ProjectReference Include=\"..\\Cerneala.csproj\"", project, StringComparison.Ordinal);
        Assert.Contains("<PrismShaderSource", project, StringComparison.Ordinal);
        Assert.Contains("..\\Drawing\\MonoGame\\**\\*.cs", project, StringComparison.Ordinal);
    }

    [Fact]
    public void MonoGameFrameworkDependenciesStayInBackendOrExplicitConsumerCode()
    {
        string root = FindRepositoryRoot();
        string[] allowedRoots =
        [
            Path.Combine(root, "UI", "Hosting", "MonoGame"),
            Path.Combine(root, "UI", "Input", "MonoGame"),
            Path.Combine(root, "Drawing", "MonoGame"),
            Path.Combine(root, "UI", "Resources", "MonoGame"),
            Path.Combine(root, "Playground"),
            Path.Combine(root, "benchmarks"),
            Path.Combine(root, "Tools", "PrismAudit")
        ];
        string windowsDxSession = Path.Combine(
            root,
            "UI",
            "Hosting",
            "Windows",
            "WindowsDxWindowGraphicsSession.cs");

        foreach (string file in EnumerateSourceFiles(root))
        {
            if (IsTestFile(root, file))
            {
                continue;
            }

            string text = File.ReadAllText(file);
            bool referencesMonoGameFramework = text.Contains("Microsoft.Xna.Framework", StringComparison.Ordinal);

            if (!referencesMonoGameFramework)
            {
                continue;
            }

            Assert.True(
                allowedRoots.Any(allowedRoot => IsUnder(file, allowedRoot)) ||
                    string.Equals(file, windowsDxSession, StringComparison.OrdinalIgnoreCase),
                $"{Path.GetRelativePath(root, file)} references MonoGame framework APIs outside known adapter or consumer code.");
        }
    }

    [Fact]
    public void WindowHostingCannotUseTheGeneralSkiaRenderer()
    {
        string root = FindRepositoryRoot();
        string[] windowHostingRoots =
        [
            Path.Combine(root, "UI", "Hosting", "Windowing"),
            Path.Combine(root, "UI", "Hosting", "Windows"),
            Path.Combine(root, "Cerneala.Platforms.Win32", "Hosting")
        ];

        foreach (string file in windowHostingRoots.SelectMany(windowHostingRoot =>
                     Directory.EnumerateFiles(windowHostingRoot, "*.cs", SearchOption.AllDirectories)))
        {
            string text = File.ReadAllText(file);
            Assert.DoesNotContain("SkiaDrawingBackend", text, StringComparison.Ordinal);
            Assert.DoesNotContain("SkiaDrawImage", text, StringComparison.Ordinal);
        }
    }

    [Fact]
    public void Win32PlatformProjectOwnsNativeWindowHosting()
    {
        string root = FindRepositoryRoot();
        string platformRoot = Path.Combine(root, "Cerneala.Platforms.Win32");
        string platformProject = File.ReadAllText(Path.Combine(
            platformRoot,
            "Cerneala.Platforms.Win32.csproj"));
        string backendProject = File.ReadAllText(Path.Combine(
            root,
            "Cerneala.Backends.MonoGame",
            "Cerneala.Backends.MonoGame.csproj"));

        Assert.Contains("<ProjectReference Include=\"..\\Cerneala.csproj\"", platformProject, StringComparison.Ordinal);
        Assert.Contains("..\\Cerneala.Platforms.Win32\\Cerneala.Platforms.Win32.csproj", backendProject, StringComparison.Ordinal);

        string[] ownedSources =
        [
            "Win32.cs",
            "Win32CursorService.cs",
            "Win32InputSource.cs",
            "Win32WindowPlatform.cs",
            "WindowsDpiAwareness.cs",
            "WindowsGpuPreference.cs"
        ];

        foreach (string source in ownedSources)
        {
            Assert.True(
                File.Exists(Path.Combine(platformRoot, "Hosting", source)),
                $"Cerneala.Platforms.Win32 must own {source}.");
            Assert.False(
                File.Exists(Path.Combine(root, "UI", "Hosting", "Windows", source)),
                $"Core must not own {source}.");
        }
    }

    [Fact]
    public void CoreWindowingContractsDoNotUseTheWindowsNamespaceOrRawNativeHandles()
    {
        string root = FindRepositoryRoot();
        string windowingRoot = Path.Combine(root, "UI", "Hosting", "Windowing");

        foreach (string file in Directory.EnumerateFiles(windowingRoot, "*.cs", SearchOption.AllDirectories))
        {
            string text = File.ReadAllText(file);
            Assert.DoesNotContain("Cerneala.UI.Hosting.Windows", text, StringComparison.Ordinal);
            Assert.DoesNotContain("nint windowHandle", text, StringComparison.Ordinal);
            Assert.DoesNotContain("nint Handle", text, StringComparison.Ordinal);
        }
    }

    [Fact]
    public void NativeWin32ImplementationTermsStayInPlatformProject()
    {
        string root = FindRepositoryRoot();
        string platformRoot = Path.Combine(root, "Cerneala.Platforms.Win32");
        string[] nativeTerms =
        [
            "user32.dll",
            "SetProcessDpiAwarenessContext",
            "UserGpuPreferences",
            "class Win32WindowPlatform",
            "class Win32InputSource"
        ];

        foreach (string file in EnumerateSourceFiles(root))
        {
            if (IsTestFile(root, file))
            {
                continue;
            }

            string text = File.ReadAllText(file);
            if (!nativeTerms.Any(term => text.Contains(term, StringComparison.Ordinal)))
            {
                continue;
            }

            Assert.True(
                IsUnder(file, platformRoot),
                $"{Path.GetRelativePath(root, file)} contains a native Win32 hosting implementation outside Cerneala.Platforms.Win32.");
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
