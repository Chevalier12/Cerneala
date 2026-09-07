using Cerneala.UI.Hosting;

namespace Cerneala.Tests.UI.Hosting;

public sealed class CoreHostingBoundaryTests
{
    [Fact]
    public void CoreHostingFilesDoNotReferenceGraphicsAdapters()
    {
        string hostingDirectory = Path.Combine(FindRepositoryRoot(), "UI", "Hosting");
        string[] forbidden =
        [
            "Cerneala.Backends.SdlGpu",
            "Cerneala.Platforms.Sdl3"
        ];

        foreach (string file in Directory.EnumerateFiles(hostingDirectory, "*.cs", SearchOption.TopDirectoryOnly))
        {
            string text = File.ReadAllText(file);
            foreach (string token in forbidden)
            {
                Assert.DoesNotContain(token, text);
            }
        }
    }

    private static string FindRepositoryRoot()
    {
        DirectoryInfo? directory = new(AppContext.BaseDirectory);
        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "Cerneala.csproj")))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        throw new InvalidOperationException("Could not locate repository root.");
    }
}
