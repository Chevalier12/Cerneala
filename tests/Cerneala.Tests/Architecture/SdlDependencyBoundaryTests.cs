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

        Assert.Contains("SDL3-CS\" Version=\"3.4.14.1", platformProject, StringComparison.Ordinal);
        Assert.Contains("Graphix.Native\" Version=\"3.4.16-graphix.2", platformProject, StringComparison.Ordinal);
        Assert.DoesNotContain("SDL3-CS.Windows", platformProject, StringComparison.Ordinal);
        Assert.DoesNotContain("SDL3-CS.Linux", platformProject, StringComparison.Ordinal);
        Assert.DoesNotContain("SDL3-CS.MacOS", platformProject, StringComparison.Ordinal);
        Assert.Contains("Cerneala.Platforms.Sdl3.csproj", backendProject, StringComparison.Ordinal);
        Assert.Contains("<Compile Remove=\"Cerneala.Platforms.Sdl3\\**\"", coreProject, StringComparison.Ordinal);
        Assert.Contains("<Compile Remove=\"Cerneala.Backends.SdlGpu\\**\"", coreProject, StringComparison.Ordinal);
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
