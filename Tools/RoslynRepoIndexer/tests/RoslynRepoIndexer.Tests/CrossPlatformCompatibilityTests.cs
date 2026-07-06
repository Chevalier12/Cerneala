using System.Text;
using System.Runtime.CompilerServices;
using RoslynRepoIndexer.Core;

namespace RoslynRepoIndexer.Tests;

public sealed class CrossPlatformCompatibilityTests
{
    [Fact]
    public async Task Index_normalizes_paths_and_handles_repo_root_with_spaces()
    {
        using var repo = SpacedTestRepo.Create();
        CreateMinimalProject(repo.Root);
        Directory.CreateDirectory(Path.Combine(repo.Root, "src", "Feature Area"));
        await File.WriteAllTextAsync(Path.Combine(repo.Root, "src", "Feature Area", "Customer Service Notes.md"), "Feature Area customer notes");

        await new IndexBuilder().BuildAsync(repo.Root, force: true, IndexerConfig.Default);

        var snapshot = IndexStore.Read(repo.Root);
        var document = Assert.Single(snapshot.Documents, d => d.RelativePath == "src/Feature Area/Customer Service Notes.md");
        Assert.Equal(repo.Root, snapshot.Manifest.RepoRoot);
        Assert.DoesNotContain('\\', document.RelativePath);
        Assert.Contains(snapshot.Tokens, t => t.Path == document.RelativePath && t.Weight == "path" && t.Token == "feature");
        Assert.Contains(snapshot.Tokens, t => t.Path == document.RelativePath && t.Weight == "path" && t.Token == "service");
    }

    [Fact]
    public async Task Index_handles_utf8_bom_crlf_and_lf_without_shifting_line_or_column()
    {
        using var repo = TestRepo.Create();
        CreateMinimalProject(repo.Root);
        Directory.CreateDirectory(Path.Combine(repo.Root, "src"));
        await File.WriteAllTextAsync(
            Path.Combine(repo.Root, "src", "BomCrlf.cs"),
            "\uFEFFnamespace Compat;\r\npublic sealed class BomCustomerService\r\n{\r\n}\r\n",
            new UTF8Encoding(encoderShouldEmitUTF8Identifier: true));
        await File.WriteAllTextAsync(
            Path.Combine(repo.Root, "notes-lf.md"),
            "alpha\nBetaToken\n",
            new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));

        await new IndexBuilder().BuildAsync(repo.Root, force: true, IndexerConfig.Default);

        var snapshot = IndexStore.Read(repo.Root);
        var bomClass = Assert.Single(snapshot.Symbols, s => s.Name == "BomCustomerService");
        var lfText = Assert.Single(snapshot.Documents, d => d.RelativePath == "notes-lf.md");

        Assert.Equal("src/BomCrlf.cs", bomClass.Path);
        Assert.Equal(2, bomClass.Line);
        Assert.Equal(1, bomClass.Column);
        Assert.Equal(2, lfText.LineCount);
        Assert.Contains(snapshot.Tokens, t => t.Path == "notes-lf.md" && t.Token == "betatoken" && t.Line == 2 && t.Column == 1);
    }

    [Fact]
    public void Exclude_directory_comparison_follows_current_platform_case_rules()
    {
        var config = IndexerConfig.Default with { ExcludeDirectories = new[] { "Generated" } };
        var lowerCasePath = "src/generated/Output.cs";

        var excluded = RepositoryDiscovery.IsExcluded(lowerCasePath, config);

        if (OperatingSystem.IsWindows())
        {
            Assert.True(excluded);
        }
        else
        {
            Assert.False(excluded);
        }
    }

    [Fact]
    public void NormalizeRelative_converts_windows_and_unix_separators_to_index_separator()
    {
        Assert.Equal("src/Services/CustomerService.cs", RepositoryDiscovery.NormalizeRelative(@"src\Services\CustomerService.cs"));
        Assert.Equal("src/Services/CustomerService.cs", RepositoryDiscovery.NormalizeRelative("/src/Services/CustomerService.cs"));
    }

    [Fact]
    public void Cli_publish_test_paths_match_repository_directory_casing()
    {
        var testSource = Path.Combine(
            GetRepositoryRootFromSourceFile(),
            "Tools",
            "RoslynRepoIndexer",
            "tests",
            "RoslynRepoIndexer.Tests",
            "FileReadCliTests.cs");

        var source = File.ReadAllText(testSource);

        Assert.DoesNotContain("Path.Combine(TestPaths.RepositoryRoot, \"tools\",", source, StringComparison.Ordinal);
        Assert.Contains("Path.Combine(TestPaths.RepositoryRoot, \"Tools\",", source, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Index_handles_reasonably_long_relative_paths()
    {
        using var repo = TestRepo.Create();
        CreateMinimalProject(repo.Root);
        var longDirectory = Path.Combine(repo.Root, "docs", new string('a', 48), new string('b', 48), new string('c', 48));
        Directory.CreateDirectory(longDirectory);
        var longFile = Path.Combine(longDirectory, "LongCustomerCompatibilityNotes.md");
        await File.WriteAllTextAsync(longFile, "long path customer compatibility");

        await new IndexBuilder().BuildAsync(repo.Root, force: true, IndexerConfig.Default);

        var snapshot = IndexStore.Read(repo.Root);
        var relative = RepositoryDiscovery.NormalizeRelative(Path.GetRelativePath(repo.Root, longFile));
        Assert.Contains(snapshot.Documents, d => d.RelativePath == relative);
        Assert.DoesNotContain('\\', relative);
        Assert.Contains(snapshot.Tokens, t => t.Path == relative && t.Token == "longcustomercompatibilitynotes");
    }

    private static void CreateMinimalProject(string root)
    {
        File.WriteAllText(Path.Combine(root, "App.csproj"), """
            <Project Sdk="Microsoft.NET.Sdk">
              <PropertyGroup>
                <TargetFramework>net8.0</TargetFramework>
                <ImplicitUsings>enable</ImplicitUsings>
                <Nullable>enable</Nullable>
              </PropertyGroup>
            </Project>
            """);
        File.WriteAllText(Path.Combine(root, "Program.cs"), "namespace App; public sealed class Program { }");
    }

    private static string GetRepositoryRootFromSourceFile([CallerFilePath] string sourceFile = "")
    {
        DirectoryInfo? directory = new(Path.GetDirectoryName(sourceFile)!);
        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "Cerneala.slnx")))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        throw new InvalidOperationException("Repository root not found.");
    }

    private sealed class SpacedTestRepo : IDisposable
    {
        private SpacedTestRepo(string root) => Root = root;

        public string Root { get; }

        public static SpacedTestRepo Create()
        {
            var root = Path.Combine(Path.GetTempPath(), "ri tests with spaces", Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(root);
            return new SpacedTestRepo(Path.GetFullPath(root).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar));
        }

        public void Dispose()
        {
            if (Directory.Exists(Root))
            {
                Directory.Delete(Root, recursive: true);
            }
        }
    }
}
