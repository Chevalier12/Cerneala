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
    public void UiControlsDoNotReferenceConcreteBackends()
    {
        string controlsRoot = FindRepositoryPath("UI", "Controls");
        string[] forbiddenTerms =
        [
            "MonoGame",
            "Skia",
            "HarfBuzz",
            "Texture2D",
            "SpriteBatch"
        ];

        foreach (string file in Directory.EnumerateFiles(controlsRoot, "*.cs", SearchOption.AllDirectories))
        {
            string text = File.ReadAllText(file);

            foreach (string forbiddenTerm in forbiddenTerms)
            {
                Assert.DoesNotContain(forbiddenTerm, text, StringComparison.Ordinal);
            }
        }
    }

    [Fact]
    public void UiControlTemplateApisDoNotReferenceConcreteBackends()
    {
        string controlsRoot = FindRepositoryPath("UI", "Controls");
        string[] templateFiles =
        [
            "ControlTemplate.cs",
            "ControlTemplate{TControl}.cs",
            "TemplateContext.cs",
            "TemplateInstance.cs",
            "TemplateBinding{T}.cs",
            "TemplatePartAttribute.cs",
            "ContentPresenter.cs",
            "ItemsPresenter.cs",
            "ItemsPanelTemplate.cs",
            "DataTemplate.cs",
            "DataTemplate{T}.cs"
        ];
        string[] forbiddenTerms =
        [
            "MonoGame",
            "Skia",
            "HarfBuzz",
            "Texture2D",
            "SpriteBatch"
        ];

        foreach (string templateFile in templateFiles)
        {
            string text = File.ReadAllText(Path.Combine(controlsRoot, templateFile));
            foreach (string forbiddenTerm in forbiddenTerms)
            {
                Assert.DoesNotContain(forbiddenTerm, text, StringComparison.Ordinal);
            }
        }
    }

    [Fact]
    public void UiTextDoesNotReferenceConcreteBackends()
    {
        string textRoot = FindRepositoryPath("UI", "Text");
        string[] forbiddenTerms =
        [
            "MonoGame",
            "Skia",
            "HarfBuzz",
            "Texture2D",
            "SpriteBatch"
        ];

        foreach (string file in Directory.EnumerateFiles(textRoot, "*.cs", SearchOption.AllDirectories))
        {
            string text = File.ReadAllText(file);

            foreach (string forbiddenTerm in forbiddenTerms)
            {
                Assert.DoesNotContain(forbiddenTerm, text, StringComparison.Ordinal);
            }
        }
    }

    [Fact]
    public void UiResourcesCoreDoesNotReferenceConcreteBackends()
    {
        string resourcesRoot = FindRepositoryPath("UI", "Resources");
        string monoGameResourcesRoot = Path.Combine(resourcesRoot, "MonoGame");
        string[] forbiddenTerms =
        [
            "MonoGame",
            "Skia",
            "HarfBuzz",
            "Texture2D",
            "SpriteBatch"
        ];

        foreach (string file in Directory.EnumerateFiles(resourcesRoot, "*.cs", SearchOption.AllDirectories))
        {
            if (file.StartsWith(monoGameResourcesRoot, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            string text = File.ReadAllText(file);

            foreach (string forbiddenTerm in forbiddenTerms)
            {
                Assert.DoesNotContain(forbiddenTerm, text, StringComparison.Ordinal);
            }
        }
    }

    [Fact]
    public void UiStylingDoesNotReferenceConcreteBackends()
    {
        string stylingRoot = FindRepositoryPath("UI", "Styling");
        string[] forbiddenTerms =
        [
            "MonoGame",
            "Skia",
            "HarfBuzz",
            "Texture2D",
            "SpriteBatch"
        ];

        foreach (string file in Directory.EnumerateFiles(stylingRoot, "*.cs", SearchOption.AllDirectories))
        {
            string text = File.ReadAllText(file);

            foreach (string forbiddenTerm in forbiddenTerms)
            {
                Assert.DoesNotContain(forbiddenTerm, text, StringComparison.Ordinal);
            }
        }
    }

    [Fact]
    public void MonoGameImageLoadingIsAdapterScoped()
    {
        string resourcesRoot = FindRepositoryPath("UI", "Resources");
        string monoGameResourcesRoot = Path.Combine(resourcesRoot, "MonoGame");

        foreach (string file in Directory.EnumerateFiles(resourcesRoot, "*.cs", SearchOption.AllDirectories))
        {
            string text = File.ReadAllText(file);
            if (text.Contains("Texture2D", StringComparison.Ordinal) ||
                text.Contains("SpriteBatch", StringComparison.Ordinal))
            {
                Assert.StartsWith(monoGameResourcesRoot, file, StringComparison.OrdinalIgnoreCase);
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

    [Fact]
    public void TextServicesRoadmapCompletionIsDocumented()
    {
        string roadmap = File.ReadAllText(FindRepositoryPath("ROADMAPv2.md"));

        Assert.Contains("- [x] A retained `TextBlock` measures text using the existing Skia/HarfBuzz pipeline through higher-level text services.", roadmap, StringComparison.Ordinal);
        Assert.Contains("- [x] `UI/Text/TextRenderer.cs`", roadmap, StringComparison.Ordinal);
        Assert.Contains("- [x] `tests/Cerneala.Tests/UI/Text/TextRendererTests.cs`", roadmap, StringComparison.Ordinal);
        Assert.Contains("- [x] Re-rendering unchanged text reuses cached text layout and retained render commands.", roadmap, StringComparison.Ordinal);
    }

    [Fact]
    public void ResourceServicesRoadmapCompletionIsDocumented()
    {
        string roadmap = File.ReadAllText(FindRepositoryPath("ROADMAPv2.md"));

        Assert.Contains("- [x] `tests/Cerneala.Tests/UI/Rendering/ResourceRenderDependencyTests.cs`", roadmap, StringComparison.Ordinal);
        Assert.Contains("- [x] `tests/Cerneala.Tests/UI/Rendering/ArchitectureBoundaryTests.cs` — covers resource and control backend boundaries.", roadmap, StringComparison.Ordinal);
        Assert.Contains("- [x] Retained render caches include resource dependency identity/version in staleness checks.", roadmap, StringComparison.Ordinal);
        Assert.Contains("- [x] Core resources and controls do not reference MonoGame, Skia, HarfBuzz, `Texture2D`, or `SpriteBatch`.", roadmap, StringComparison.Ordinal);
        Assert.Contains("- [x] MonoGame image loading is adapter-scoped under `UI/Resources/MonoGame`.", roadmap, StringComparison.Ordinal);
    }

    [Fact]
    public void RuntimeTestsDoNotDependOnActiveOpenSpecChanges()
    {
        string testsRoot = FindRepositoryPath("tests", "Cerneala.Tests");
        string openSpecSegment = "open" + "spec";
        string changesSegment = "chang" + "es";
        string activeChangePathPattern = string.Join("\", \"", openSpecSegment, changesSegment);
        string activeChangeSlashPattern = string.Join("/", openSpecSegment, changesSegment);

        foreach (string file in Directory.EnumerateFiles(testsRoot, "*.cs", SearchOption.AllDirectories))
        {
            string text = File.ReadAllText(file);

            Assert.DoesNotContain(activeChangePathPattern, text, StringComparison.Ordinal);
            Assert.DoesNotContain(activeChangeSlashPattern, text, StringComparison.Ordinal);
        }
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
