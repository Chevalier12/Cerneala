namespace Cerneala.Tests.Architecture;

public sealed class RepositoryShapeTests
{
    [Fact]
    public void RoadmapSectionOneReferencesExistingCanonicalOpenSpecSpecs()
    {
        string root = FindRepositoryRoot();
        string roadmap = File.ReadAllText(Path.Combine(root, "ROADMAPv2.md"));
        string section = ExtractSection(roadmap, "## 1. [MVP] Architecture contracts and project memory");

        string[] canonicalSpecPaths =
        [
            "openspec/specs/retained-ui-mvp-foundation/spec.md",
            "openspec/specs/retained-element-tree/spec.md",
            "openspec/specs/retained-invalidation-frame-scheduler/spec.md",
            "openspec/specs/typed-state-model/spec.md",
            "openspec/specs/layout-system/spec.md",
            "openspec/specs/retained-rendering-cache/spec.md",
            "openspec/specs/retained-input-bridge/spec.md",
            "openspec/specs/command-router-actions/spec.md",
            "openspec/specs/styling-theme-engine/spec.md"
        ];

        foreach (string specPath in canonicalSpecPaths)
        {
            Assert.True(File.Exists(Path.Combine(root, Normalize(specPath))), $"{specPath} should exist.");
            Assert.Contains($"- [x] `{specPath}`", section, StringComparison.Ordinal);
        }
    }

    [Fact]
    public void RoadmapSectionOneDoesNotClaimLegacyPlaceholderSpecNames()
    {
        string root = FindRepositoryRoot();
        string roadmap = File.ReadAllText(Path.Combine(root, "ROADMAPv2.md"));
        string section = ExtractSection(roadmap, "## 1. [MVP] Architecture contracts and project memory");
        string[] legacySpecPaths =
        [
            "openspec/specs/retained-ui-tree/spec.md",
            "openspec/specs/invalidation-and-frame-loop/spec.md",
            "openspec/specs/typed-state/spec.md",
            "openspec/specs/layout/spec.md",
            "openspec/specs/render-cache/spec.md",
            "openspec/specs/input-focus-command-bridge/spec.md",
            "openspec/specs/styling-theme/spec.md"
        ];

        foreach (string specPath in legacySpecPaths)
        {
            Assert.False(Directory.Exists(Path.GetDirectoryName(Path.Combine(root, Normalize(specPath)))!), $"{specPath} should not be reintroduced as a duplicate capability.");
            Assert.DoesNotContain(specPath, section, StringComparison.Ordinal);
        }
    }

    [Fact]
    public void RoadmapSectionOneArchitectureFilesExist()
    {
        string root = FindRepositoryRoot();
        string roadmap = File.ReadAllText(Path.Combine(root, "ROADMAPv2.md"));
        string section = ExtractSection(roadmap, "## 1. [MVP] Architecture contracts and project memory");
        string[] requiredPaths =
        [
            "openspec/config.yaml",
            "openspec/README.md",
            "openspec/project.md",
            "docs/architecture-v2.md",
            "docs/diagrams/retained-frame-loop.md",
            "docs/diagrams/ui-layer-boundaries.md",
            "tests/Cerneala.Tests/Architecture/RepositoryShapeTests.cs",
            "tests/Cerneala.Tests/Architecture/NamespaceBoundaryTests.cs"
        ];

        foreach (string path in requiredPaths)
        {
            Assert.True(File.Exists(Path.Combine(root, Normalize(path))), $"{path} should exist.");
            Assert.Contains($"- [x] `{path}`", section, StringComparison.Ordinal);
        }
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

    private static string ExtractSection(string markdown, string heading)
    {
        int start = markdown.IndexOf(heading, StringComparison.Ordinal);
        Assert.True(start >= 0, $"Could not find heading '{heading}'.");
        int next = markdown.IndexOf("\n## ", start + heading.Length, StringComparison.Ordinal);
        return next < 0 ? markdown[start..] : markdown[start..next];
    }

    private static string Normalize(string path)
    {
        return path.Replace('/', Path.DirectorySeparatorChar);
    }
}
