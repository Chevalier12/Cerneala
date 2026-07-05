using System.Diagnostics;
using System.Text.Json;
using RoslynRepoIndexer.Core;

namespace RoslynRepoIndexer.Tests;

public sealed class LinkedGeneratedIndexingTests
{
    [Fact]
    public async Task Linked_file_in_two_projects_keeps_semantic_documents_but_deduplicates_full_text_tokens()
    {
        using var repo = TestRepo.Create();
        await CreateLinkedFileProjectsAsync(repo.Root);

        await new IndexBuilder().BuildAsync(repo.Root, force: true, IndexerConfig.Default);

        var documents = ReadJsonLines(IndexFile(repo.Root, "documents.jsonl")).ToArray();
        var symbols = ReadJsonLines(IndexFile(repo.Root, "symbols.jsonl")).ToArray();
        var tokens = ReadJsonLines(IndexFile(repo.Root, "tokens.jsonl")).ToArray();

        var linkedDocuments = documents
            .Where(document => document.GetProperty("relativePath").GetString() == "Shared/LinkedCustomer.cs")
            .ToArray();
        Assert.Equal(2, linkedDocuments.Length);
        Assert.Equal(new[] { "AlphaProject", "BetaProject" }, linkedDocuments.Select(ProjectName).Order(StringComparer.Ordinal).ToArray());
        Assert.Equal(2, linkedDocuments.Select(document => document.GetProperty("documentId").GetString()).Distinct(StringComparer.Ordinal).Count());

        var linkedSymbols = symbols
            .Where(symbol => symbol.GetProperty("path").GetString() == "Shared/LinkedCustomer.cs"
                             && symbol.GetProperty("name").GetString() == "LinkedCustomer")
            .ToArray();
        Assert.Equal(2, linkedSymbols.Length);
        Assert.Equal(new[] { "AlphaProject", "BetaProject" }, linkedSymbols.Select(ProjectName).Order(StringComparer.Ordinal).ToArray());

        var fullTextTokenCopies = tokens
            .Where(token => token.GetProperty("path").GetString() == "Shared/LinkedCustomer.cs"
                            && token.GetProperty("token").GetString() == "linkedcustomer"
                            && token.GetProperty("field").GetString() == "csharp")
            .ToArray();
        Assert.Single(fullTextTokenCopies);
    }

    [Fact]
    public async Task Multi_targeted_project_indexes_stable_documents_without_duplicate_symbol_rows()
    {
        using var repo = TestRepo.Create();
        await CreateMultiTargetProjectAsync(repo.Root);

        var first = await new IndexBuilder().BuildAsync(repo.Root, force: true, IndexerConfig.Default);
        var firstDocuments = ReadJsonLines(IndexFile(repo.Root, "documents.jsonl")).ToArray();
        var firstSymbols = ReadJsonLines(IndexFile(repo.Root, "symbols.jsonl")).ToArray();
        var firstIds = firstDocuments.Select(document => document.GetProperty("documentId").GetString()).Order(StringComparer.Ordinal).ToArray();

        var second = await new IndexBuilder().BuildAsync(repo.Root, force: true, IndexerConfig.Default);
        var secondDocuments = ReadJsonLines(IndexFile(repo.Root, "documents.jsonl")).ToArray();
        var secondSymbols = ReadJsonLines(IndexFile(repo.Root, "symbols.jsonl")).ToArray();
        var secondIds = secondDocuments.Select(document => document.GetProperty("documentId").GetString()).Order(StringComparer.Ordinal).ToArray();

        Assert.True(first.Documents > 0);
        Assert.Equal(first.Documents, second.Documents);
        Assert.Equal(firstIds, secondIds);
        Assert.Equal(firstDocuments.Length, firstIds.Distinct(StringComparer.Ordinal).Count());

        var symbolKeys = firstSymbols
            .Select(symbol => string.Join('|',
                symbol.GetProperty("path").GetString(),
                symbol.GetProperty("projectName").GetString(),
                symbol.GetProperty("kind").GetString(),
                symbol.GetProperty("fullyQualifiedName").GetString(),
                symbol.GetProperty("spanStart").GetInt32(),
                symbol.GetProperty("spanLength").GetInt32()))
            .ToArray();
        Assert.Equal(symbolKeys.Length, symbolKeys.Distinct(StringComparer.Ordinal).Count());
        Assert.Equal(firstSymbols.Length, secondSymbols.Length);
    }

    [Fact]
    public async Task Source_generated_documents_are_indexed_only_when_include_generated_is_enabled()
    {
        using var repo = TestRepo.Create();
        var generatorRoot = Path.Combine(Path.GetTempPath(), "ri-generator-fixtures", Guid.NewGuid().ToString("N"));
        await CreateSourceGeneratorFixtureAsync(repo.Root, generatorRoot);
        await BuildProjectAsync(Path.Combine(generatorRoot, "Generator.csproj"));

        var config = IndexerConfig.Default with { Solution = "GeneratedConsumer.csproj" };

        await new IndexBuilder().BuildAsync(repo.Root, force: true, config);
        var defaultDocuments = ReadJsonLines(IndexFile(repo.Root, "documents.jsonl")).ToArray();
        Assert.DoesNotContain(defaultDocuments, document => document.GetProperty("relativePath").GetString() == "generated/GeneratedCustomer.g.cs");

        await new IndexBuilder().BuildAsync(repo.Root, force: true, config with { IncludeGenerated = true });

        var generatedDocuments = ReadJsonLines(IndexFile(repo.Root, "documents.jsonl"))
            .Where(document => document.GetProperty("relativePath").GetString() == "generated/GeneratedCustomer.g.cs")
            .ToArray();
        var generatedSymbols = ReadJsonLines(IndexFile(repo.Root, "symbols.jsonl"))
            .Where(symbol => symbol.GetProperty("path").GetString() == "generated/GeneratedCustomer.g.cs"
                             && symbol.GetProperty("name").GetString() == "GeneratedCustomer")
            .ToArray();

        Assert.Single(generatedDocuments);
        Assert.True(generatedDocuments[0].GetProperty("isGenerated").GetBoolean());
        Assert.Single(generatedSymbols);
    }

    [Fact]
    public async Task Large_generated_files_are_excluded_by_default_without_reading_full_text()
    {
        using var repo = TestRepo.Create();
        await CreateProjectWithLargeGeneratedFileAsync(repo.Root);

        var summary = await new IndexBuilder().BuildAsync(repo.Root, force: true, IndexerConfig.Default with { MaxTextFileBytes = 16 });

        var documents = ReadJsonLines(IndexFile(repo.Root, "documents.jsonl")).ToArray();
        Assert.DoesNotContain(documents, document => document.GetProperty("relativePath").GetString() == "LargeOutput.g.cs");
        Assert.True(summary.Documents > 0);
    }

    private static async Task CreateLinkedFileProjectsAsync(string root)
    {
        Directory.CreateDirectory(Path.Combine(root, ".git"));
        Directory.CreateDirectory(Path.Combine(root, "Shared"));
        Directory.CreateDirectory(Path.Combine(root, "Alpha"));
        Directory.CreateDirectory(Path.Combine(root, "Beta"));
        await File.WriteAllTextAsync(Path.Combine(root, "Shared", "LinkedCustomer.cs"), """
            namespace SharedLinked;

            public sealed class LinkedCustomer
            {
                public string Name => "shared";
            }
            """);
        await File.WriteAllTextAsync(Path.Combine(root, "Alpha", "AlphaProject.csproj"), LinkedProject("AlphaProject"));
        await File.WriteAllTextAsync(Path.Combine(root, "Beta", "BetaProject.csproj"), LinkedProject("BetaProject"));
    }

    private static string LinkedProject(string assemblyName)
        => $$"""
            <Project Sdk="Microsoft.NET.Sdk">
              <PropertyGroup>
                <TargetFramework>net8.0</TargetFramework>
                <AssemblyName>{{assemblyName}}</AssemblyName>
                <ImplicitUsings>enable</ImplicitUsings>
                <Nullable>enable</Nullable>
                <EnableDefaultCompileItems>false</EnableDefaultCompileItems>
              </PropertyGroup>
              <ItemGroup>
                <Compile Include="..\Shared\LinkedCustomer.cs" Link="Shared\LinkedCustomer.cs" />
              </ItemGroup>
            </Project>
            """;

    private static async Task CreateMultiTargetProjectAsync(string root)
    {
        await File.WriteAllTextAsync(Path.Combine(root, "MultiTarget.csproj"), """
            <Project Sdk="Microsoft.NET.Sdk">
              <PropertyGroup>
                <TargetFrameworks>net8.0;net9.0</TargetFrameworks>
                <ImplicitUsings>enable</ImplicitUsings>
                <Nullable>enable</Nullable>
              </PropertyGroup>
            </Project>
            """);
        await File.WriteAllTextAsync(Path.Combine(root, "MultiTargetSubject.cs"), """
            namespace Multi.Targeting;

            public sealed class MultiTargetSubject
            {
                public string Value => "stable";
            }
            """);
    }

    private static async Task CreateSourceGeneratorFixtureAsync(string root, string generatorRoot)
    {
        Directory.CreateDirectory(generatorRoot);
        await File.WriteAllTextAsync(Path.Combine(generatorRoot, "Generator.csproj"), """
            <Project Sdk="Microsoft.NET.Sdk">
              <PropertyGroup>
                <TargetFramework>netstandard2.0</TargetFramework>
                <LangVersion>latest</LangVersion>
                <Nullable>enable</Nullable>
                <NoWarn>RS1036;RS1042</NoWarn>
              </PropertyGroup>
              <ItemGroup>
                <PackageReference Include="Microsoft.CodeAnalysis.CSharp" Version="5.6.0" PrivateAssets="all" />
              </ItemGroup>
            </Project>
            """);
        await File.WriteAllTextAsync(Path.Combine(generatorRoot, "GeneratedCustomerGenerator.cs"), """""
            using Microsoft.CodeAnalysis;
            using Microsoft.CodeAnalysis.Text;
            using System.Text;

            namespace Generator;

            [Generator]
            public sealed class GeneratedCustomerGenerator : ISourceGenerator
            {
                public void Initialize(GeneratorInitializationContext context) { }

                public void Execute(GeneratorExecutionContext context)
                {
                    context.AddSource("GeneratedCustomer.g.cs", SourceText.From("""
                        namespace Generated.Sample;

                        public sealed class GeneratedCustomer
                        {
                            public string Name => "generated";
                        }
                        """, Encoding.UTF8));
                }
            }
            """"");
        await File.WriteAllTextAsync(Path.Combine(root, "GeneratedConsumer.csproj"), """
            <Project Sdk="Microsoft.NET.Sdk">
              <PropertyGroup>
                <TargetFramework>net8.0</TargetFramework>
                <ImplicitUsings>enable</ImplicitUsings>
                <Nullable>enable</Nullable>
              </PropertyGroup>
              <ItemGroup>
                <Analyzer Include="{{AnalyzerPath}}" />
              </ItemGroup>
            </Project>
            """.Replace("{{AnalyzerPath}}", Path.Combine(generatorRoot, "bin", "Debug", "netstandard2.0", "Generator.dll")));
        await File.WriteAllTextAsync(Path.Combine(root, "Consumer.cs"), """
            namespace Generated.Sample;

            public sealed class Consumer
            {
                public string Read(GeneratedCustomer customer) => customer.Name;
            }
            """);
    }

    private static async Task CreateProjectWithLargeGeneratedFileAsync(string root)
    {
        await File.WriteAllTextAsync(Path.Combine(root, "LargeGenerated.csproj"), """
            <Project Sdk="Microsoft.NET.Sdk">
              <PropertyGroup>
                <TargetFramework>net8.0</TargetFramework>
                <ImplicitUsings>enable</ImplicitUsings>
                <Nullable>enable</Nullable>
              </PropertyGroup>
            </Project>
            """);
        await File.WriteAllTextAsync(Path.Combine(root, "Regular.cs"), """
            namespace Large.Generated;

            public sealed class Regular
            {
                public string Name => "regular";
            }
            """);
        await File.WriteAllTextAsync(Path.Combine(root, "LargeOutput.g.cs"), "namespace Large.Generated;\npublic static class LargeOutput\n{\n" + new string(' ', 10_000) + "\n}\n");
    }

    private static string? ProjectName(JsonElement element)
        => element.GetProperty("projectName").ValueKind == JsonValueKind.Null
            ? null
            : element.GetProperty("projectName").GetString();

    private static async Task BuildProjectAsync(string projectPath)
    {
        var processStartInfo = new ProcessStartInfo("dotnet")
        {
            RedirectStandardOutput = true,
            RedirectStandardError = true,
        };
        processStartInfo.ArgumentList.Add("build");
        processStartInfo.ArgumentList.Add(projectPath);
        processStartInfo.ArgumentList.Add("--nologo");

        using var process = Process.Start(processStartInfo) ?? throw new InvalidOperationException("Failed to start dotnet build.");
        var stdout = await process.StandardOutput.ReadToEndAsync();
        var stderr = await process.StandardError.ReadToEndAsync();
        await process.WaitForExitAsync();

        Assert.True(process.ExitCode == 0, stdout + Environment.NewLine + stderr);
    }

    private static IEnumerable<JsonElement> ReadJsonLines(string path)
    {
        foreach (var line in File.ReadLines(path))
        {
            using var document = JsonDocument.Parse(line);
            yield return document.RootElement.Clone();
        }
    }

    private static string IndexFile(string root, string fileName)
        => Path.Combine(root, ".roslyn-index", "v1", fileName);
}
