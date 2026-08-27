namespace Cerneala.Tests.Architecture;

public sealed class SdlDependencyBoundaryTests
{
    [Fact]
    public void ExistingProjectsRemainFreeOfSdlDependencies()
    {
        string root = FindRepositoryRoot();
        string[] projects =
        [
            "Cerneala.csproj",
            Path.Combine("Cerneala.Platforms.Win32", "Cerneala.Platforms.Win32.csproj"),
            Path.Combine("Cerneala.Backends.MonoGame", "Cerneala.Backends.MonoGame.csproj")
        ];

        foreach (string project in projects)
        {
            string text = File.ReadAllText(Path.Combine(root, project));
            Assert.DoesNotContain("<PackageReference Include=\"SDL3-CS", text, StringComparison.Ordinal);
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
        Assert.Contains("SDL3-CS.Windows\" Version=\"3.4.14.1", platformProject, StringComparison.Ordinal);
        Assert.Contains("SDL3-CS.Linux\" Version=\"3.4.14.1", platformProject, StringComparison.Ordinal);
        Assert.Contains("SDL3-CS.MacOS\" Version=\"3.4.14.1", platformProject, StringComparison.Ordinal);
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
