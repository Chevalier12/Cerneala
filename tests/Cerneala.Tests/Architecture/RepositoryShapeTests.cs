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
