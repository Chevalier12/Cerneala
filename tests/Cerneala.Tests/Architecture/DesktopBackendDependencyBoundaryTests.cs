using System.Xml.Linq;

namespace Cerneala.Tests.Architecture;

public sealed class DesktopBackendDependencyBoundaryTests
{
    [Fact]
    public void CoreProjectDoesNotReferenceConcreteDesktopBackends()
    {
        XDocument project = XDocument.Load(Path.Combine(FindRepositoryRoot(), "Cerneala.csproj"));
        foreach (XElement reference in project.Descendants()
            .Where(element => element.Name.LocalName is "ProjectReference" or "PackageReference"))
        {
            string name = (string?)reference.Attribute("Include") ?? string.Empty;
            Assert.DoesNotContain("Cerneala.Backends.", name, StringComparison.Ordinal);
            Assert.DoesNotContain("Cerneala.Platforms.", name, StringComparison.Ordinal);
            Assert.DoesNotContain("SDL3", name, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("Graphix.Native", name, StringComparison.Ordinal);
        }
        Assert.Empty(project.Descendants("PrismShaderSource"));
    }

    [Fact]
    public void SdlGpuBackendOwnsDrawingAndDependsOnThePlatformBoundary()
    {
        string root = FindRepositoryRoot();
        XDocument project = XDocument.Load(Path.Combine(root,
            "Cerneala.Backends.SdlGpu", "Cerneala.Backends.SdlGpu.csproj"));
        string[] references = project.Descendants("ProjectReference")
            .Select(element => ((string?)element.Attribute("Include") ?? string.Empty).Replace('\\', '/'))
            .ToArray();
        Assert.Contains("../Cerneala.csproj", references);
        Assert.Contains("../Cerneala.Platforms.Sdl3/Cerneala.Platforms.Sdl3.csproj", references);
        Assert.Empty(project.Descendants("PackageReference"));
        Assert.True(File.Exists(Path.Combine(root, "Cerneala.Backends.SdlGpu", "Gpu", "SdlGpuDrawingBackend.cs")));
        Assert.True(File.Exists(Path.Combine(root, "Cerneala.Backends.SdlGpu", "Prism", "SdlGpuPrismExecutor.cs")));
    }

    [Fact]
    public void SdlPlatformProjectOwnsNativeBindingsAndWindowHosting()
    {
        string root = FindRepositoryRoot();
        string platformRoot = Path.Combine(root, "Cerneala.Platforms.Sdl3");
        XDocument project = XDocument.Load(Path.Combine(platformRoot, "Cerneala.Platforms.Sdl3.csproj"));
        string[] packages = project.Descendants("PackageReference")
            .Select(element => (string?)element.Attribute("Include") ?? string.Empty).ToArray();
        Assert.Contains("SDL3-CS", packages);
        Assert.Contains("Graphix.Native", packages);
        Assert.Contains(project.Descendants("ProjectReference"), reference =>
            ((string?)reference.Attribute("Include"))?.Replace('\\', '/') == "../Cerneala.csproj");
        Assert.True(File.Exists(Path.Combine(platformRoot, "Hosting", "SdlWindowPlatform.cs")));
        Assert.True(File.Exists(Path.Combine(platformRoot, "Input", "SdlInputSource.cs")));
        Assert.True(File.Exists(Path.Combine(platformRoot, "Interop", "NativeSdlApi.cs")));
    }

    [Fact]
    public void WindowHostingCannotUseTheGeneralSkiaRenderer()
    {
        string root = FindRepositoryRoot();
        string[] hostingRoots =
        [
            Path.Combine(root, "UI", "Hosting", "Windowing"),
            Path.Combine(root, "Cerneala.Platforms.Sdl3", "Hosting"),
            Path.Combine(root, "Cerneala.Backends.SdlGpu", "Hosting")
        ];
        foreach (string file in hostingRoots.SelectMany(path => EnumerateFiles(path, "*.cs")))
        {
            string text = File.ReadAllText(file);
            Assert.DoesNotContain("SkiaDrawingBackend", text, StringComparison.Ordinal);
            Assert.DoesNotContain("SkiaDrawImage", text, StringComparison.Ordinal);
        }
    }

    [Fact]
    public void CoreWindowingContractsDoNotUseConcretePlatformsOrRawNativeHandles()
    {
        string windowingRoot = Path.Combine(FindRepositoryRoot(), "UI", "Hosting", "Windowing");
        foreach (string file in EnumerateFiles(windowingRoot, "*.cs"))
        {
            string text = File.ReadAllText(file);
            Assert.DoesNotContain("Cerneala.Platforms.Sdl3", text, StringComparison.Ordinal);
            Assert.DoesNotContain("Cerneala.Backends.SdlGpu", text, StringComparison.Ordinal);
            Assert.DoesNotContain("nint windowHandle", text, StringComparison.Ordinal);
            Assert.DoesNotContain("nint Handle", text, StringComparison.Ordinal);
        }
    }

    [Fact]
    public void NativeWindowImplementationTermsStayInThePlatformProject()
    {
        string root = FindRepositoryRoot();
        string platformRoot = Path.Combine(root, "Cerneala.Platforms.Sdl3");
        string[] nativeTerms = ["user32.dll", "SetProcessDpiAwarenessContext", "UserGpuPreferences"];
        foreach (string file in EnumerateFiles(root, "*.cs")
            .Where(file => !IsUnder(file, Path.Combine(root, "tests"))))
        {
            string text = File.ReadAllText(file);
            if (nativeTerms.Any(term => text.Contains(term, StringComparison.Ordinal)))
            {
                Assert.True(IsUnder(file, platformRoot),
                    $"{Path.GetRelativePath(root, file)} contains native window implementation outside the platform project.");
            }
        }
    }

    private static IEnumerable<string> EnumerateFiles(string root, string pattern) =>
        Directory.EnumerateFiles(root, pattern, SearchOption.AllDirectories)
            .Where(file => !file.Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)
                .Any(segment => segment is "bin" or "obj" or "artifacts"));

    private static bool IsUnder(string file, string directory)
    {
        string relative = Path.GetRelativePath(directory, file);
        return relative != "." && !relative.StartsWith("..", StringComparison.Ordinal) && !Path.IsPathRooted(relative);
    }

    private static string FindRepositoryRoot()
    {
        DirectoryInfo? directory = new(AppContext.BaseDirectory);
        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "Cerneala.slnx"))) { return directory.FullName; }
            directory = directory.Parent;
        }
        throw new DirectoryNotFoundException("Could not find repository root.");
    }
}
