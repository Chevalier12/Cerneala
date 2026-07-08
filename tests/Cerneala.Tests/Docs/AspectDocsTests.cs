namespace Cerneala.Tests.Docs;

public sealed class AspectDocsTests
{
    [Fact]
    public void AspectDocsDoNotPresentWpfOrAvaloniaConceptsAsTheModel()
    {
        string docs = ReadDoc("aspect-system.md");

        Assert.DoesNotContain("ResourceDictionary", docs, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("BasedOn", docs, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("DataTrigger", docs, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("MultiDataTrigger", docs, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("XAML", docs, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void AspectDocsMentionModernBuildingBlocks()
    {
        string docs = ReadDoc("aspect-system.md");

        Assert.Contains("tokens", docs, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("variants", docs, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("slots", docs, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("component templates", docs, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("content templates", docs, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void GettingStartedUsesModernAspectSnippet()
    {
        string docs = ReadDoc("getting-started.md");

        Assert.Contains("DefaultAspectPackage.Create()", docs, StringComparison.Ordinal);
        Assert.DoesNotContain("DefaultTheme.CreateStyleSheet()", docs, StringComparison.Ordinal);
    }

    private static string ReadDoc(string fileName)
    {
        return File.ReadAllText(Path.Combine(FindRepositoryRoot(), "docs", fileName));
    }

    private static string FindRepositoryRoot()
    {
        DirectoryInfo? directory = new(AppContext.BaseDirectory);
        while (directory is not null)
        {
            if (directory.EnumerateFiles("Cerneala.slnx").Any())
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        throw new InvalidOperationException("Could not locate repository root.");
    }
}
