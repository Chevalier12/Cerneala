namespace Cerneala.Tests.UI.Platform;

public sealed class PlatformBoundaryTests
{
    [Fact]
    public void UiPlatformContractsDoNotReferenceConcreteBackendsOrNativeApis()
    {
        string platformRoot = FindRepositoryPath("UI", "Platform");
        string[] forbiddenTerms =
        [
            "MonoGame",
            "Microsoft.Xna",
            "Skia",
            "HarfBuzz",
            "Texture2D",
            "SpriteBatch",
            "System.Windows.Forms",
            "System.Windows.Automation",
            "Windows.UI",
            "Microsoft.UI",
            "NSAccessibility",
            "DllImport",
            "LibraryImport",
            "Cerneala.UI.Text",
            "ClipboardAdapter"
        ];

        foreach (string file in Directory.EnumerateFiles(platformRoot, "*.cs", SearchOption.AllDirectories))
        {
            string text = File.ReadAllText(file);
            foreach (string forbiddenTerm in forbiddenTerms)
            {
                Assert.DoesNotContain(forbiddenTerm, text, StringComparison.Ordinal);
            }
        }
    }

    [Fact]
    public void PlatformContractsIncludeRoadmapServices()
    {
        string platformRoot = FindRepositoryPath("UI", "Platform");
        string[] files =
        [
            "IPlatformServices.cs",
            "IClipboard.cs",
            "ICursorService.cs",
            "IFileDialogService.cs",
            "ITextInputPlatform.cs",
            "IDpiProvider.cs",
            "IAccessibilityPlatform.cs"
        ];

        foreach (string file in files)
        {
            Assert.True(File.Exists(Path.Combine(platformRoot, file)), file);
        }
    }

    private static string FindRepositoryPath(params string[] segments)
    {
        DirectoryInfo? directory = new(AppContext.BaseDirectory);
        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "Cerneala.slnx")))
            {
                return Path.Combine(new[] { directory.FullName }.Concat(segments).ToArray());
            }

            directory = directory.Parent;
        }

        throw new DirectoryNotFoundException("Could not find repository root.");
    }
}
