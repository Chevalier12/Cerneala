namespace Cerneala.Tests.Architecture;

public sealed class RepositoryShapeTests
{
    [Fact]
    public void RepositoryDoesNotContainLegacySpecWorkspaceOrCodexSkills()
    {
        string root = FindRepositoryRoot();
        string legacySpec = "open" + "spec";
        string legacySpecName = "Open" + "Spec";

        Assert.False(Directory.Exists(Path.Combine(root, legacySpec)), $"{legacySpecName} workspace should not be reintroduced.");
        Assert.False(Directory.Exists(Path.Combine(root, ".codex", "skills", $"{legacySpec}-apply-change")), $"{legacySpecName} Codex skills should not be reintroduced.");
        Assert.False(Directory.Exists(Path.Combine(root, ".codex", "skills", $"{legacySpec}-archive-change")), $"{legacySpecName} Codex skills should not be reintroduced.");
        Assert.False(Directory.Exists(Path.Combine(root, ".codex", "skills", $"{legacySpec}-explore")), $"{legacySpecName} Codex skills should not be reintroduced.");
        Assert.False(Directory.Exists(Path.Combine(root, ".codex", "skills", $"{legacySpec}-propose")), $"{legacySpecName} Codex skills should not be reintroduced.");
        Assert.False(Directory.Exists(Path.Combine(root, ".codex", "skills", $"{legacySpec}-sync-specs")), $"{legacySpecName} Codex skills should not be reintroduced.");
    }

    [Fact]
    public void RoadmapSectionOneArchitectureFilesExist()
    {
        string root = FindRepositoryRoot();
        string roadmap = File.ReadAllText(Path.Combine(root, "ROADMAPv2.md"));
        string section = ExtractSection(roadmap, "## 1. [MVP] Architecture contracts and project memory");
        string[] requiredPaths =
        [
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

    [Fact]
    public void RoadmapNoLongerMentionsLegacySpecTool()
    {
        string root = FindRepositoryRoot();
        string roadmap = File.ReadAllText(Path.Combine(root, "ROADMAPv2.md"));
        string legacySpec = "open" + "spec";
        string legacySpecName = "Open" + "Spec";

        Assert.DoesNotContain(legacySpec, roadmap, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain(legacySpecName, roadmap, StringComparison.Ordinal);
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
