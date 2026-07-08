using Cerneala.Playground.Samples;

namespace Cerneala.Tests.Architecture;

public sealed class DeveloperPreviewCompletionTests
{
    private static readonly string[] DeferredClaims =
    [
        "package split scenario-complete",
        "native accessibility scenario-complete",
        "full IME scenario-complete",
        "markup scenario-complete",
        "source generation scenario-complete",
        "advanced rendering scenario-complete"
    ];

    [Fact]
    public void DeveloperPreviewDocsExistAndNameSupportedSurface()
    {
        string gettingStarted = Read("docs", "getting-started.md");
        string scope = Read("docs", "developer-preview-scope.md");

        Assert.Contains("UIRoot", gettingStarted, StringComparison.Ordinal);
        Assert.Contains("UiHost", gettingStarted, StringComparison.Ordinal);
        Assert.Contains("DefaultAspectPackage", gettingStarted, StringComparison.Ordinal);
        Assert.Contains("BindingOperations", gettingStarted, StringComparison.Ordinal);
        Assert.Contains("ActionCommand", gettingStarted, StringComparison.Ordinal);
        Assert.Contains("TextBox", gettingStarted, StringComparison.Ordinal);
        Assert.Contains("Button", gettingStarted, StringComparison.Ordinal);
        Assert.Contains("ListBox", gettingStarted, StringComparison.Ordinal);
        Assert.Contains("supported", scope, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("deferred", scope, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void DeveloperPreviewDocsDoNotClaimDeferredFeaturesComplete()
    {
        string docs = Read("docs", "getting-started.md") + "\n" +
            Read("docs", "developer-preview-scope.md") + "\n" +
            Read("docs", "developer-preview-checklist.md");

        foreach (string claim in DeferredClaims)
        {
            Assert.DoesNotContain(claim, docs, StringComparison.OrdinalIgnoreCase);
        }

        Assert.DoesNotContain("native accessibility is complete", docs, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("full IME is complete", docs, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("package split is complete", docs, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void DeveloperPreviewRoadmapRecordsDeveloperPreviewHardeningWithoutClosingDeferredSections()
    {
        string roadmap = Read("ROADMAPv2.md");

        Assert.Contains("Developer Preview hardening checkpoint", roadmap, StringComparison.Ordinal);
        Assert.Contains("Tab focus navigation", roadmap, StringComparison.Ordinal);
        Assert.Contains("Grid definition mutation invalidation", roadmap, StringComparison.Ordinal);
        Assert.Contains("Retained lifecycle/subscription cleanup", roadmap, StringComparison.Ordinal);
        Assert.Contains("Developer Preview scope guardrails", roadmap, StringComparison.Ordinal);
        Assert.Contains("Retained stress budget gates", roadmap, StringComparison.Ordinal);
        Assert.Contains("Getting Started docs/sample", roadmap, StringComparison.Ordinal);
        Assert.Contains("Developer Preview completion gate", roadmap, StringComparison.Ordinal);

        foreach (string claim in DeferredClaims)
        {
            Assert.DoesNotContain(claim, roadmap, StringComparison.OrdinalIgnoreCase);
        }
    }

    [Fact]
    public void DeveloperPreviewArchiveScriptExistsAtExpectedPath()
    {
        Assert.True(File.Exists(Path.Combine(RepoRoot(), "Tools", "scripts", "Archive-Repo.ps1")));
    }

    [Fact]
    public void DeveloperPreviewStressBudgetsArePresent()
    {
        AssertFileContains("tests", "Cerneala.Tests", "UI", "Hosting", "RetainedStressBudgetTests.cs", "RetainedStressBudgetTests");
        AssertFileContains("tests", "Cerneala.Tests", "UI", "Rendering", "RenderStressBudgetTests.cs", "RenderStressBudgetTests");
        AssertFileContains("tests", "Cerneala.Tests", "UI", "Controls", "ListStressBudgetTests.cs", "ListStressBudgetTests");
        AssertFileContains("tests", "Cerneala.Tests", "UI", "Accessibility", "SemanticsStressBudgetTests.cs", "SemanticsStressBudgetTests");
    }

    [Fact]
    public void DeveloperPreviewCoreAuthoringRuntimeAndGettingStartedSamplesAreRegistered()
    {
        SampleSelector selector = SampleSelector.CreateDefault();
        string[] names = selector.Samples.Select(sample => sample.Name).ToArray();

        Assert.Contains("Retained App", names);
        Assert.Contains("Runtime Preview", names);
        Assert.Contains("Authoring App", names);
        Assert.Contains("Getting Started", names);
    }

    [Fact]
    public void DeveloperPreviewAllPreviewSamplesAvoidFrozenNamespaces()
    {
        string[] sampleFiles =
        [
            "RetainedAppSample.cs",
            "RuntimePreviewSample.cs",
            "AuthoringAppSample.cs",
            "GettingStartedSample.cs"
        ];

        foreach (string file in sampleFiles)
        {
            string source = Read("Playground", "Cerneala.Playground", "Samples", file);
            Assert.DoesNotContain("Cerneala.SourceGen", source, StringComparison.Ordinal);
            Assert.DoesNotContain("Cerneala.UI.Markup", source, StringComparison.Ordinal);
            Assert.DoesNotContain("Xaml", source, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("new Binding(\"", source, StringComparison.Ordinal);
            Assert.DoesNotContain("MonoGame", source, StringComparison.Ordinal);
        }
    }

    private static void AssertFileContains(params string[] pathParts)
    {
        string expected = pathParts[^1];
        string path = Path.Combine(pathParts[..^1]);
        string text = Read(pathParts[..^1]);

        Assert.Contains(expected, text, StringComparison.Ordinal);
        Assert.True(File.Exists(Path.Combine(RepoRoot(), path)));
    }

    private static string Read(params string[] pathParts)
    {
        return File.ReadAllText(Path.Combine(new[] { RepoRoot() }.Concat(pathParts).ToArray()));
    }

    private static string RepoRoot()
    {
        DirectoryInfo? current = new(AppContext.BaseDirectory);
        while (current is not null)
        {
            if (File.Exists(Path.Combine(current.FullName, "Cerneala.slnx")))
            {
                return current.FullName;
            }

            current = current.Parent;
        }

        throw new InvalidOperationException("Could not locate repository root.");
    }
}
