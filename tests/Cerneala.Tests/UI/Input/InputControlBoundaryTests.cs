using Cerneala.Tests.Architecture;

namespace Cerneala.Tests.UI.Input;

public sealed class InputControlBoundaryTests
{
    private static readonly string[] ForbiddenControlTerms =
    [
        "Cerneala.UI.Controls",
        "ButtonBase",
        "Thumb"
    ];

    [Fact]
    public void UiInputDoesNotReferenceControls()
    {
        string repositoryRoot = FindRepositoryRoot();
        string inputRoot = FindRepositoryPath("UI", "Input");

        foreach (string file in Directory.EnumerateFiles(inputRoot, "*.cs", SearchOption.AllDirectories))
        {
            string[] foundTerms = FindForbiddenControlTerms(File.ReadAllText(file));

            Assert.True(
                foundTerms.Length == 0,
                $"{Path.GetRelativePath(repositoryRoot, file)} references the Controls layer: {string.Join(", ", foundTerms)}.");
        }
    }

    [Fact]
    public void ControlTermScanIgnoresCommentsAndStringLiterals()
    {
        const string source =
            "namespace Cerneala.UI.Input;\n" +
            "\n" +
            "// Like WPF ButtonBase.OnLostKeyboardFocus: a comment is not a reference.\n" +
            "/* Thumb drags belong to Cerneala.UI.Controls. */\n" +
            "internal sealed class DocumentationOnlyInput\n" +
            "{\n" +
            "    private const string ControlExample = \"ButtonBase Thumb\";\n" +
            "    private const string VerbatimControlExample = @\"Cerneala.UI.Controls\";\n" +
            "}\n";

        Assert.Empty(FindForbiddenControlTerms(source));
    }

    [Fact]
    public void ControlTermScanFindsCodeReferences()
    {
        const string source =
            "using Cerneala.UI.Controls;\n" +
            "using Cerneala.UI.Controls.Primitives;\n" +
            "\n" +
            "namespace Cerneala.UI.Input;\n" +
            "\n" +
            "internal sealed class ControlDependentInput\n" +
            "{\n" +
            "    private ButtonBase? button;\n" +
            "    private Thumb? thumb;\n" +
            "}\n";

        Assert.Equal(ForbiddenControlTerms, FindForbiddenControlTerms(source));
    }

    private static string[] FindForbiddenControlTerms(string text)
    {
        string searchableText = CSharpSourceText.RemoveCommentsAndStringLiterals(text);

        return ForbiddenControlTerms
            .Where(term => searchableText.Contains(term, StringComparison.Ordinal))
            .ToArray();
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
