namespace Cerneala.Tests.Architecture;

public sealed class SdlDependencyBoundaryTests
{
    [Fact]
    public void CoreAndBuildTimeAuthoringRemainFreeOfSdlDependencies()
    {
        string root = FindRepositoryRoot();
        string[] projects =
        [
            "Cerneala.csproj",
            Path.Combine("Cerneala.Language", "Cerneala.Language.csproj"),
            Path.Combine("Cerneala.SourceGen", "Cerneala.SourceGen.csproj")
        ];

        foreach (string project in projects)
        {
            string text = File.ReadAllText(Path.Combine(root, project));
            Assert.DoesNotContain("<PackageReference Include=\"SDL3-CS", text, StringComparison.Ordinal);
            Assert.DoesNotContain("<PackageReference Include=\"Graphix-CS", text, StringComparison.Ordinal);
            Assert.DoesNotContain("<PackageReference Include=\"Graphix.Native", text, StringComparison.Ordinal);
        }
    }

    [Fact]
    public void SdlPackagesAndSourceStayInTheTwoAdapterProjects()
    {
        string root = FindRepositoryRoot();
        string platformProject = File.ReadAllText(Path.Combine(
            root,
            "Cerneala.Platforms.Sdl3",
            "Cerneala.Platforms.Sdl3.csproj"));
        string backendProject = File.ReadAllText(Path.Combine(
            root,
            "Cerneala.Backends.SdlGpu",
            "Cerneala.Backends.SdlGpu.csproj"));
        string coreProject = File.ReadAllText(Path.Combine(root, "Cerneala.csproj"));

        Assert.Contains("Graphix-CS\" Version=\"3.4.16.1\"", platformProject, StringComparison.Ordinal);
        Assert.DoesNotContain("Include=\"SDL3-CS\"", platformProject, StringComparison.Ordinal);
        Assert.Contains("Graphix.Native\" Version=\"3.4.16-graphix.6\"", platformProject, StringComparison.Ordinal);
        Assert.DoesNotContain("SDL3-CS.Windows", platformProject, StringComparison.Ordinal);
        Assert.DoesNotContain("SDL3-CS.Linux", platformProject, StringComparison.Ordinal);
        Assert.DoesNotContain("SDL3-CS.MacOS", platformProject, StringComparison.Ordinal);
        Assert.Contains("Cerneala.Platforms.Sdl3.csproj", backendProject, StringComparison.Ordinal);
        Assert.Contains("<Compile Remove=\"Cerneala.Platforms.Sdl3\\**\"", coreProject, StringComparison.Ordinal);
        Assert.Contains("<Compile Remove=\"Cerneala.Backends.SdlGpu\\**\"", coreProject, StringComparison.Ordinal);
    }

    [Fact]
    public void ShaderCompilerUsesTheSameGraphixManagedAndNativePackages()
    {
        string project = File.ReadAllText(Path.Combine(
            FindRepositoryRoot(), "Tools", "Cerneala.SdlShaderCompiler", "Cerneala.SdlShaderCompiler.csproj"));

        Assert.Contains("Graphix-CS\" Version=\"3.4.16.1\" PrivateAssets=\"all\"", project, StringComparison.Ordinal);
        Assert.Contains("Graphix.Native\" Version=\"3.4.16-graphix.6\" PrivateAssets=\"all\"", project, StringComparison.Ordinal);
        Assert.DoesNotContain("Include=\"SDL3-CS\"", project, StringComparison.Ordinal);
    }

    [Fact]
    public void BenchmarksUseTheSameGraphixManagedAndNativePackages()
    {
        string project = File.ReadAllText(Path.Combine(
            FindRepositoryRoot(), "benchmarks", "Cerneala.Benchmarks", "Cerneala.Benchmarks.csproj"));

        Assert.Contains("Graphix-CS\" Version=\"3.4.16.1\"", project, StringComparison.Ordinal);
        Assert.Contains("Graphix.Native\" Version=\"3.4.16-graphix.6\"", project, StringComparison.Ordinal);
        Assert.DoesNotContain("Include=\"SDL3-CS\"", project, StringComparison.Ordinal);
    }

    [Fact]
    public void GraphixRestoreUsesPublicNuGetWithoutArtifactBootstrap()
    {
        string root = FindRepositoryRoot();
        string configuration = File.ReadAllText(Path.Combine(root, "NuGet.Config"));
        Assert.Contains("https://api.nuget.org/v3/index.json", configuration, StringComparison.Ordinal);
        Assert.DoesNotContain("GraphixLocal", configuration, StringComparison.Ordinal);
        Assert.DoesNotContain("artifacts/graphix-packages", configuration, StringComparison.Ordinal);

        foreach (string workflow in new[] { "desktop-backends.yml", "prism-shaders.yml" })
        {
            string text = File.ReadAllText(Path.Combine(root, ".github", "workflows", workflow));
            Assert.DoesNotContain("Get-GraphixPackages.ps1", text, StringComparison.Ordinal);
        }

        Assert.False(File.Exists(Path.Combine(root, "Tools", "scripts", "Get-GraphixPackages.ps1")));
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
}
