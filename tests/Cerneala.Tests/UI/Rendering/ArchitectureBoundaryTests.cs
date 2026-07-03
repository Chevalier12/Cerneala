namespace Cerneala.Tests.UI.Rendering;

public sealed class ArchitectureBoundaryTests
{
    [Fact]
    public void UiRenderingDoesNotReferenceConcreteBackends()
    {
        string renderingRoot = FindRepositoryPath("UI", "Rendering");
        string[] forbiddenTerms =
        [
            "MonoGame",
            "Skia",
            "HarfBuzz",
            "Texture2D",
            "SpriteBatch",
            "MonoGameDrawingBackend"
        ];

        foreach (string file in Directory.EnumerateFiles(renderingRoot, "*.cs", SearchOption.AllDirectories))
        {
            string text = File.ReadAllText(file);

            foreach (string forbiddenTerm in forbiddenTerms)
            {
                Assert.DoesNotContain(forbiddenTerm, text, StringComparison.Ordinal);
            }
        }
    }

    [Fact]
    public void UiDrawingDoesNotReferenceRetainedRendering()
    {
        string drawingRoot = FindRepositoryPath("UI", "Drawing");

        foreach (string file in Directory.EnumerateFiles(drawingRoot, "*.cs", SearchOption.AllDirectories))
        {
            string text = File.ReadAllText(file);

            Assert.DoesNotContain("Cerneala.UI.Rendering", text, StringComparison.Ordinal);
            Assert.DoesNotContain("RetainedRenderCache", text, StringComparison.Ordinal);
            Assert.DoesNotContain("ElementRenderCache", text, StringComparison.Ordinal);
            Assert.DoesNotContain("RenderQueueProcessor", text, StringComparison.Ordinal);
        }
    }

    [Fact]
    public void DrawCommandListPoolDeferralIsDocumentedConsistently()
    {
        string renderingRoot = FindRepositoryPath("UI", "Rendering");
        string roadmap = File.ReadAllText(FindRepositoryPath("ROADMAPv2.md"));
        string spec = File.ReadAllText(FindRepositoryPath("openspec", "specs", "retained-rendering-cache", "spec.md"));

        Assert.False(File.Exists(Path.Combine(renderingRoot, "DrawCommandListPool.cs")));
        Assert.Contains("DrawCommandListPool.cs", roadmap, StringComparison.Ordinal);
        Assert.Contains("deferred", roadmap, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("DrawCommandListPool is deferred", spec, StringComparison.Ordinal);
    }

    private static string FindRepositoryPath(params string[] segments)
    {
        string repositoryRoot = FindRepositoryRoot();
        string candidate = Path.Combine(new[] { repositoryRoot }.Concat(segments).ToArray());

        if (Directory.Exists(candidate) || File.Exists(candidate))
        {
            return candidate;
        }

        throw new DirectoryNotFoundException($"Could not find repository path: {Path.Combine(segments)}");
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
