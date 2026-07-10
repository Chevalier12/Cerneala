namespace Cerneala.Tests.Docs;

public sealed class GettingStartedDocsTests
{
    [Fact]
    public void GettingStartedDocumentExists()
    {
        Assert.True(File.Exists(GettingStartedPath()));
    }

    [Fact]
    public void GettingStartedDocumentMentionsRetainedUpdateDrawContract()
    {
        string text = ReadGettingStarted();

        Assert.Contains("retained", text, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Update", text, StringComparison.Ordinal);
        Assert.Contains("Draw", text, StringComparison.Ordinal);
        Assert.Contains("no-work", text, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("draw-purity", text, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void GettingStartedDocumentUsesUiHostUiRootDefaultAspectPackageAndBindingOperations()
    {
        string text = ReadGettingStarted();

        Assert.Contains("UIRoot", text, StringComparison.Ordinal);
        Assert.Contains("UiHost", text, StringComparison.Ordinal);
        Assert.Contains("UiViewport", text, StringComparison.Ordinal);
        Assert.Contains("DefaultAspectPackage.Create()", text, StringComparison.Ordinal);
        Assert.Contains("ObservableValue<string>", text, StringComparison.Ordinal);
        Assert.Contains("ObservableList<string>", text, StringComparison.Ordinal);
        Assert.Contains("BindingOperations.BindTwoWay", text, StringComparison.Ordinal);
        Assert.Contains("ActionCommand", text, StringComparison.Ordinal);
        Assert.Contains("TextBox", text, StringComparison.Ordinal);
        Assert.Contains("Button", text, StringComparison.Ordinal);
        Assert.Contains("ListBox", text, StringComparison.Ordinal);
    }

    [Fact]
    public void GettingStartedDocumentShowsCodeFirstAndGeneratedMarkupPaths()
    {
        string text = ReadGettingStarted();

        Assert.Contains("code-first", text, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("<Window", text, StringComparison.OrdinalIgnoreCase);
        Assert.Contains(".cui.xml", text, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("@template", text, StringComparison.Ordinal);
        Assert.DoesNotContain("x:Class", text, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("new Binding(\"", text, StringComparison.Ordinal);
        Assert.DoesNotContain("Use XAML", text, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void GettingStartedDocumentMentionsDeferredScopeForArbitraryXamlPackageSplitAndFullIme()
    {
        string text = ReadGettingStarted();

        Assert.Contains("deferred", text, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("arbitrary XAML compatibility", text, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("package split", text, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("full IME", text, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void DeveloperPreviewChecklistNamesTargetedAndFullTestCommands()
    {
        string text = ReadChecklist();

        Assert.Contains("CorePreviewContractTests", text, StringComparison.Ordinal);
        Assert.Contains("AuthoringPreviewContractTests", text, StringComparison.Ordinal);
        Assert.Contains("RuntimePreviewContractTests", text, StringComparison.Ordinal);
        Assert.Contains("DeveloperPreviewScopeTests", text, StringComparison.Ordinal);
        Assert.Contains("dotnet test Cerneala.slnx", text, StringComparison.Ordinal);
        Assert.Contains("dotnet test", text, StringComparison.Ordinal);
    }

    [Fact]
    public void DeveloperPreviewChecklistNamesArchiveCommand()
    {
        string text = ReadChecklist();

        Assert.Contains("powershell -NoProfile -ExecutionPolicy Bypass -File .\\Tools\\scripts\\Archive-Repo.ps1 -RepoRoot .", text, StringComparison.Ordinal);
    }

    private static string ReadGettingStarted()
    {
        return File.ReadAllText(GettingStartedPath());
    }

    private static string ReadChecklist()
    {
        return File.ReadAllText(Path.Combine(RepoRoot(), "docs", "developer-preview-checklist.md"));
    }

    private static string GettingStartedPath()
    {
        return Path.Combine(RepoRoot(), "docs", "getting-started.md");
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
