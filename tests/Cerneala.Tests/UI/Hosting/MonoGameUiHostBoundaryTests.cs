using Cerneala.UI.Hosting;
using Cerneala.UI.Hosting.MonoGame;

namespace Cerneala.Tests.UI.Hosting;

public sealed class MonoGameUiHostBoundaryTests
{
    [Fact]
    public void CoreHostingFilesDoNotReferenceMonoGame()
    {
        string hostingDirectory = Path.Combine(FindRepositoryRoot(), "UI", "Hosting");
        string[] forbidden =
        [
            "Cerneala.UI.Hosting.MonoGame",
            "Microsoft.Xna.Framework",
            "MonoGame",
            "SpriteBatch",
            "Texture2D",
            "GameTime",
            "Mouse.GetState",
            "Keyboard.GetState"
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

    [Fact]
    public void MonoGameReferencesStayInMonoGameHostingAdapter()
    {
        string monoGameDirectory = Path.Combine(FindRepositoryRoot(), "UI", "Hosting", "MonoGame");
        string combined = string.Join(Environment.NewLine, Directory.EnumerateFiles(monoGameDirectory, "*.cs").Select(File.ReadAllText));

        Assert.Contains("Microsoft.Xna.Framework", combined);
        Assert.Contains("MonoGameInputSource", combined);
        Assert.Contains("MonoGameDrawingBackend", combined);
    }

    [Fact]
    public void MonoGameHostDoesNotExposeMutableCoreHost()
    {
        Type hostType = typeof(MonoGameUiHost);

        Assert.DoesNotContain(
            hostType.GetProperties(),
            property => property.PropertyType == typeof(UiHost));
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
