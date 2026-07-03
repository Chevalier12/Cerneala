namespace Cerneala.Tests.UI.Input;

public sealed class InputControlBoundaryTests
{
    [Fact]
    public void UiInputDoesNotReferenceControls()
    {
        string inputRoot = FindRepositoryPath("UI", "Input");
        string monoGameInputRoot = Path.Combine(inputRoot, "MonoGame");
        string[] forbiddenTerms =
        [
            "Cerneala.UI.Controls",
            "ButtonBase",
            "Thumb"
        ];

        foreach (string file in Directory.EnumerateFiles(inputRoot, "*.cs", SearchOption.AllDirectories))
        {
            if (file.StartsWith(monoGameInputRoot, StringComparison.OrdinalIgnoreCase))
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
