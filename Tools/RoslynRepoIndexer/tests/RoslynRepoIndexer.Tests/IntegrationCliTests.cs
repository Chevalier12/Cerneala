using System.Diagnostics;
using System.Text.Json;
using RoslynRepoIndexer.Core;

namespace RoslynRepoIndexer.Tests;

public sealed class IntegrationCliTests
{
    [Fact]
    public async Task Index_search_goto_and_status_work_on_minimal_csharp_project()
    {
        using var repo = TestRepo.Create();
        await CreateProjectAsync(repo.Root);

        var index = await RunCliAsync(new[] { "index", ".", "--json" }, repo.Root);
        var status = await RunCliAsync(new[] { "status", "--json" }, repo.Root);
        var search = await RunCliAsync(new[] { "search", "CustomerService", "--mode", "symbol", "--json" }, repo.Root);
        var gotoResult = await RunCliAsync(new[] { "goto", "GetCustomerAsync", "--json" }, repo.Root);

        Assert.Equal(0, index.ExitCode);
        Assert.Equal(0, status.ExitCode);
        Assert.Equal(0, search.ExitCode);
        Assert.Equal(0, gotoResult.ExitCode);
        Assert.Contains("CustomerService", search.Stdout, StringComparison.Ordinal);
        Assert.Contains("GetCustomerAsync", gotoResult.Stdout, StringComparison.Ordinal);
        using var statusJson = JsonDocument.Parse(status.Stdout);
        Assert.Equal("valid", statusJson.RootElement.GetProperty("data").GetProperty("status").GetString());
    }

    [Fact]
    public async Task Index_reports_incremental_dirty_counts_and_removes_deleted_files()
    {
        using var repo = TestRepo.Create();
        await CreateProjectAsync(repo.Root);

        var first = await RunCliAsync(new[] { "index", ".", "--json" }, repo.Root);
        var indexFiles = Directory.GetFiles(IndexStore.GetVersionDirectory(repo.Root));
        var writeTimesBeforeNoOp = indexFiles.ToDictionary(path => path, File.GetLastWriteTimeUtc, StringComparer.Ordinal);
        var second = await RunCliAsync(new[] { "index", ".", "--json" }, repo.Root);
        var writeTimesAfterNoOp = indexFiles.ToDictionary(path => path, File.GetLastWriteTimeUtc, StringComparer.Ordinal);
        var segmentFilesBeforeBodyChange = Directory.GetFiles(Path.Combine(IndexStore.GetIndexDirectory(repo.Root), "segments"), "*.bin")
            .ToHashSet(StringComparer.Ordinal);
        await File.WriteAllTextAsync(Path.Combine(repo.Root, "CustomerService.cs"), """
            namespace My.App.Services;

            public sealed class CustomerService
            {
                public Task<string> GetCustomerAsync(string id)
                {
                    return Task.FromResult(id + "!");
                }
            }
            """);
        var bodyOnly = await RunCliAsync(new[] { "index", ".", "--json" }, repo.Root);
        var bodyOnlyManifest = IndexStore.ReadManifest(repo.Root);
        var segmentFilesAfterBodyChange = Directory.GetFiles(Path.Combine(IndexStore.GetIndexDirectory(repo.Root), "segments"), "*.bin")
            .ToHashSet(StringComparer.Ordinal);
        File.Delete(Path.Combine(repo.Root, "CustomerService.cs"));
        var deleted = await RunCliAsync(new[] { "index", ".", "--json" }, repo.Root);
        var searchDeleted = await RunCliAsync(new[] { "search", "CustomerService", "--json" }, repo.Root);

        Assert.Equal(0, first.ExitCode);
        Assert.Equal(0, second.ExitCode);
        Assert.Equal(0, bodyOnly.ExitCode);
        Assert.Equal(0, deleted.ExitCode);

        using var secondJson = JsonDocument.Parse(second.Stdout);
        using var bodyOnlyJson = JsonDocument.Parse(bodyOnly.Stdout);
        using var deletedJson = JsonDocument.Parse(deleted.Stdout);
        Assert.False(secondJson.RootElement.GetProperty("data").GetProperty("fullRebuild").GetBoolean());
        Assert.True(secondJson.RootElement.GetProperty("data").GetProperty("incremental").GetBoolean());
        Assert.Equal(0, secondJson.RootElement.GetProperty("data").GetProperty("dirtyDocuments").GetInt32());
        Assert.Equal(0, secondJson.RootElement.GetProperty("data").GetProperty("timings").GetProperty("workspaceLoadMs").GetInt64());
        Assert.Equal(writeTimesBeforeNoOp, writeTimesAfterNoOp);
        Assert.Equal(1, bodyOnlyJson.RootElement.GetProperty("data").GetProperty("dirtyDocuments").GetInt32());
        Assert.Equal(1, bodyOnlyManifest.SegmentsWritten);
        Assert.True(bodyOnlyManifest.SegmentsReused >= 1);
        Assert.Single(segmentFilesAfterBodyChange.Except(segmentFilesBeforeBodyChange));
        Assert.Equal(1, deletedJson.RootElement.GetProperty("data").GetProperty("deletedDocuments").GetInt32());
        using var searchDeletedJson = JsonDocument.Parse(searchDeleted.Stdout);
        Assert.Empty(searchDeletedJson.RootElement.GetProperty("data").EnumerateArray());
        Assert.Empty(searchDeletedJson.RootElement.GetProperty("results").EnumerateArray());
    }

    [Fact]
    public async Task Incremental_preserves_declaration_hash_for_body_only_change_and_changes_it_for_rename()
    {
        using var repo = TestRepo.Create();
        await CreateDeclarationHashProjectAsync(repo.Root);

        var first = await new IndexBuilder().BuildAsync(repo.Root, force: true, IndexerConfig.Default);
        var initialHash = ReadDeclarationHash(repo.Root, "HashSubject.cs");

        await File.WriteAllTextAsync(Path.Combine(repo.Root, "HashSubject.cs"), DeclarationHashSubjectSource("""
                return "body-only";
            """));
        var bodyOnly = await new IndexBuilder().BuildAsync(repo.Root, force: false, IndexerConfig.Default);
        var bodyOnlyHash = ReadDeclarationHash(repo.Root, "HashSubject.cs");

        await File.WriteAllTextAsync(Path.Combine(repo.Root, "HashSubject.cs"), """
            namespace Hashing.Sample;

            public sealed class HashSubject
            {
                string RenamedPrivateValue()
                {
                    return "body-only";
                }
            }
            """);
        var rename = await new IndexBuilder().BuildAsync(repo.Root, force: false, IndexerConfig.Default);
        var renamedHash = ReadDeclarationHash(repo.Root, "HashSubject.cs");

        Assert.True(first.FullRebuild);
        Assert.True(bodyOnly.Incremental);
        Assert.False(bodyOnly.FullRebuild);
        Assert.True(rename.Incremental);
        Assert.Equal(initialHash, bodyOnlyHash);
        Assert.NotEqual(bodyOnlyHash, renamedHash);
        Assert.Equal(1, bodyOnly.DirtyDocuments);
        Assert.True(bodyOnly.UnchangedDocuments >= 1);
    }

    [Fact]
    public async Task Persistent_workspace_body_update_matches_a_full_rebuild_and_avoids_workspace_reload()
    {
        using var repo = TestRepo.Create();
        await CreateDeclarationHashProjectAsync(repo.Root);
        using var workspaceSession = new RepositoryWorkspaceSession();
        var builder = new IndexBuilder(workspaceSession);

        var first = await builder.BuildAsync(repo.Root, force: true, IndexerConfig.Default);
        await File.WriteAllTextAsync(Path.Combine(repo.Root, "HashSubject.cs"), DeclarationHashSubjectSource("""
                return "persistent-body";
            """));
        var incremental = await builder.BuildAsync(repo.Root, force: false, IndexerConfig.Default);
        var incrementalSnapshot = IndexStore.Read(repo.Root);

        await new IndexBuilder().BuildAsync(repo.Root, force: true, IndexerConfig.Default);
        var fullSnapshot = IndexStore.Read(repo.Root);

        Assert.Equal(1, incremental.DirtyDocuments);
        Assert.True(incremental.Timings.WorkspaceLoadMs < first.Timings.WorkspaceLoadMs);
        Assert.Equal(CanonicalRows(incrementalSnapshot), CanonicalRows(fullSnapshot));
    }

    [Fact]
    public async Task Persistent_workspace_reloads_when_globbed_source_files_are_added_or_deleted()
    {
        using var repo = TestRepo.Create();
        await CreateDeclarationHashProjectAsync(repo.Root);
        using var workspaceSession = new RepositoryWorkspaceSession();
        var builder = new IndexBuilder(workspaceSession);
        var addedPath = Path.Combine(repo.Root, "AddedLater.cs");

        var first = await builder.BuildAsync(repo.Root, force: true, IndexerConfig.Default);
        await File.WriteAllTextAsync(addedPath, "namespace Hashing.Sample; public sealed class AddedLater { }");
        var added = await builder.BuildAsync(repo.Root, force: false, IndexerConfig.Default);
        var addedSnapshot = IndexStore.Read(repo.Root);
        File.Delete(addedPath);
        var deleted = await builder.BuildAsync(repo.Root, force: false, IndexerConfig.Default);
        var deletedSnapshot = IndexStore.Read(repo.Root);

        Assert.Equal(first.Documents + 1, added.Documents);
        Assert.Contains(addedSnapshot.Documents, document => document.RelativePath == "AddedLater.cs");
        Assert.Equal(first.Documents, deleted.Documents);
        Assert.DoesNotContain(deletedSnapshot.Documents, document => document.RelativePath == "AddedLater.cs");
    }

    public static IEnumerable<object[]> SemanticRebuildTriggerFiles()
    {
        yield return new object[] { "Hashing.csproj", "<!-- trigger -->" };
        yield return new object[] { "Directory.Build.props", "<Project><PropertyGroup><Nullable>enable</Nullable></PropertyGroup></Project>" };
        yield return new object[] { "Directory.Build.targets", "<Project><Target Name=\"RoslynIndexerTrigger\" /></Project>" };
        yield return new object[] { "Directory.Packages.props", "<Project><ItemGroup><PackageVersion Include=\"Example\" Version=\"1.0.0\" /></ItemGroup></Project>" };
        yield return new object[] { "global.json", "{\"sdk\":{\"rollForward\":\"latestFeature\"}}" };
        yield return new object[] { "NuGet.config", "<configuration><packageSources /></configuration>" };
        yield return new object[] { "packages.lock.json", "{\"version\":1,\"dependencies\":{}}" };
    }

    [Theory]
    [MemberData(nameof(SemanticRebuildTriggerFiles))]
    public async Task Incremental_full_rebuilds_when_workspace_input_files_change(string relativePath, string content)
    {
        using var repo = TestRepo.Create();
        await CreateDeclarationHashProjectAsync(repo.Root);

        var first = await new IndexBuilder().BuildAsync(repo.Root, force: true, IndexerConfig.Default);
        var triggerPath = Path.Combine(repo.Root, relativePath);
        if (relativePath.EndsWith(".csproj", StringComparison.OrdinalIgnoreCase))
        {
            await File.AppendAllTextAsync(triggerPath, Environment.NewLine + content);
        }
        else
        {
            await File.WriteAllTextAsync(triggerPath, content);
        }

        var rebuild = await new IndexBuilder().BuildAsync(repo.Root, force: false, IndexerConfig.Default);

        Assert.True(first.FullRebuild);
        Assert.True(rebuild.FullRebuild);
        Assert.False(rebuild.Incremental);
        Assert.Equal(rebuild.Documents, rebuild.DirtyDocuments);
        Assert.Equal(0, rebuild.UnchangedDocuments);
    }

    [Fact]
    public async Task Incremental_full_rebuilds_when_config_changes()
    {
        using var repo = TestRepo.Create();
        await CreateDeclarationHashProjectAsync(repo.Root);

        var first = await new IndexBuilder().BuildAsync(repo.Root, force: true, IndexerConfig.Default);
        var rebuild = await new IndexBuilder().BuildAsync(repo.Root, force: false, IndexerConfig.Default with { SearchResultLimit = 17 });

        Assert.True(first.FullRebuild);
        Assert.True(rebuild.FullRebuild);
        Assert.False(rebuild.Incremental);
        Assert.Equal(rebuild.Documents, rebuild.DirtyDocuments);
    }

    [Fact]
    public async Task Incremental_adds_new_csharp_file_documents_symbols_and_tokens()
    {
        using var repo = TestRepo.Create();
        await CreateDeclarationHashProjectAsync(repo.Root);

        await new IndexBuilder().BuildAsync(repo.Root, force: true, IndexerConfig.Default);
        await File.WriteAllTextAsync(Path.Combine(repo.Root, "AddedFeature.cs"), """
            namespace Hashing.Sample;

            public sealed class AddedFeature
            {
                public string NewTokenValue() => "new-token-value";
            }
            """);

        var incremental = await new IndexBuilder().BuildAsync(repo.Root, force: false, IndexerConfig.Default);
        var documents = ReadJsonLines(IndexFile(repo.Root, "documents.jsonl")).ToArray();
        var symbols = ReadJsonLines(IndexFile(repo.Root, "symbols.jsonl")).ToArray();
        var tokens = ReadJsonLines(IndexFile(repo.Root, "tokens.jsonl")).ToArray();

        Assert.True(incremental.Incremental);
        Assert.False(incremental.FullRebuild);
        Assert.Equal(1, incremental.DirtyDocuments);
        Assert.Contains(documents, d => d.GetProperty("relativePath").GetString() == "AddedFeature.cs");
        Assert.Contains(symbols, s => s.GetProperty("name").GetString() == "AddedFeature");
        Assert.Contains(symbols, s => s.GetProperty("name").GetString() == "NewTokenValue");
        Assert.Contains(tokens, t => t.GetProperty("path").GetString() == "AddedFeature.cs"
                                     && t.GetProperty("token").GetString() == "addedfeature");
        Assert.Contains(tokens, t => t.GetProperty("path").GetString() == "AddedFeature.cs"
                                     && t.GetProperty("token").GetString() == "newtokenvalue");
    }

    [Fact]
    public async Task Incremental_full_rebuilds_when_project_reference_metadata_changes()
    {
        using var repo = TestRepo.Create();
        await CreateSemanticDependencySolutionAsync(repo.Root);

        await new IndexBuilder().BuildAsync(repo.Root, force: true, IndexerConfig.Default);
        var appProjectPath = Path.Combine(repo.Root, "src", "App", "App.csproj");
        var appProject = await File.ReadAllTextAsync(appProjectPath);
        await File.WriteAllTextAsync(appProjectPath, appProject.Replace(
            """<ProjectReference Include="..\Core\Core.csproj" />""",
            """<ProjectReference Include="..\Core\Core.csproj" PrivateAssets="all" />""",
            StringComparison.Ordinal));

        var rebuild = await new IndexBuilder().BuildAsync(repo.Root, force: false, IndexerConfig.Default);

        Assert.True(rebuild.FullRebuild);
        Assert.False(rebuild.Incremental);
        Assert.Equal(rebuild.Documents, rebuild.DirtyDocuments);
    }

    [Fact]
    public async Task Incremental_full_rebuilds_when_compilation_options_change()
    {
        using var repo = TestRepo.Create();
        await CreateDeclarationHashProjectAsync(repo.Root);

        await new IndexBuilder().BuildAsync(repo.Root, force: true, IndexerConfig.Default);
        var projectPath = Path.Combine(repo.Root, "Hashing.csproj");
        var project = await File.ReadAllTextAsync(projectPath);
        await File.WriteAllTextAsync(projectPath, project.Replace(
            "<Nullable>enable</Nullable>",
            "<Nullable>disable</Nullable>",
            StringComparison.Ordinal));

        var rebuild = await new IndexBuilder().BuildAsync(repo.Root, force: false, IndexerConfig.Default);

        Assert.True(rebuild.FullRebuild);
        Assert.False(rebuild.Incremental);
        Assert.Equal(rebuild.Documents, rebuild.DirtyDocuments);
    }

    [Fact]
    public async Task Incremental_declaration_hash_change_reindexes_project_and_direct_dependents()
    {
        using var repo = TestRepo.Create();
        await CreateSemanticDependencySolutionAsync(repo.Root);

        await new IndexBuilder().BuildAsync(repo.Root, force: true, IndexerConfig.Default);
        await File.WriteAllTextAsync(Path.Combine(repo.Root, "src", "Core", "DomainThing.cs"), """
            namespace Dependency.Core;

            public sealed class DomainThing
            {
                public string RenamedValue() => "changed";
            }
            """);

        var incremental = await new IndexBuilder().BuildAsync(repo.Root, force: false, IndexerConfig.Default);
        var documents = ReadJsonLines(IndexFile(repo.Root, "documents.jsonl")).ToArray();

        Assert.True(incremental.Incremental);
        Assert.False(incremental.FullRebuild);
        Assert.Equal(2, incremental.DirtyDocuments);
        Assert.Contains(documents, d => d.GetProperty("relativePath").GetString() == "src/Core/DomainThing.cs"
                                       && d.GetProperty("projectName").GetString() == "Core");
        Assert.Contains(documents, d => d.GetProperty("relativePath").GetString() == "src/App/AppService.cs"
                                       && d.GetProperty("projectName").GetString() == "App");
    }

    [Fact]
    public async Task Incremental_reindexes_all_csharp_documents_when_more_than_twenty_percent_are_dirty()
    {
        using var repo = TestRepo.Create();
        await CreateManyDocumentProjectAsync(repo.Root, documentCount: 6);

        await new IndexBuilder().BuildAsync(repo.Root, force: true, IndexerConfig.Default);
        await File.WriteAllTextAsync(Path.Combine(repo.Root, "Feature1.cs"), ManyDocumentSource(1, """
                return "changed-one";
            """));
        await File.WriteAllTextAsync(Path.Combine(repo.Root, "Feature2.cs"), ManyDocumentSource(2, """
                return "changed-two";
            """));

        var incremental = await new IndexBuilder().BuildAsync(repo.Root, force: false, IndexerConfig.Default);

        Assert.True(incremental.Incremental);
        Assert.False(incremental.FullRebuild);
        Assert.Equal(6, incremental.DirtyDocuments);
    }

    [Fact]
    public async Task Index_json_reports_counters_and_stage_timings_in_output_and_diagnostics()
    {
        using var repo = TestRepo.Create();
        await CreateProjectAsync(repo.Root);

        var index = await RunCliAsync(new[] { "index", ".", "--json" }, repo.Root);

        Assert.Equal(0, index.ExitCode);
        using var json = JsonDocument.Parse(index.Stdout);
        var data = json.RootElement.GetProperty("data");
        Assert.True(data.GetProperty("documents").GetInt32() > 0);
        Assert.True(data.GetProperty("symbols").GetInt32() > 0);
        Assert.True(data.GetProperty("references").GetInt32() >= 0);
        Assert.True(data.GetProperty("tokens").GetInt32() > 0);
        Assert.True(data.GetProperty("warnings").GetInt32() >= 0);

        var timings = data.GetProperty("timings");
        AssertTiming(timings, "discoveryMs");
        AssertTiming(timings, "workspaceLoadMs");
        AssertTiming(timings, "semanticIndexMs");
        AssertTiming(timings, "textIndexMs");
        AssertTiming(timings, "persistMs");
        AssertTiming(timings, "totalMs");
        Assert.Equal(timings.GetProperty("totalMs").GetInt64(), data.GetProperty("totalMs").GetInt64());

        var diagnostics = ReadJsonLines(IndexFile(repo.Root, "diagnostics.jsonl")).ToArray();
        var timingDiagnostics = diagnostics
            .Where(entry => entry.GetProperty("severity").GetString() == "timing")
            .Select(entry => entry.GetProperty("stage").GetString())
            .ToHashSet(StringComparer.Ordinal);
        Assert.Contains("discovery", timingDiagnostics);
        Assert.Contains("workspaceLoad", timingDiagnostics);
        Assert.Contains("semanticIndex", timingDiagnostics);
        Assert.Contains("textIndex", timingDiagnostics);
        Assert.Contains("persist", timingDiagnostics);
        Assert.Contains("total", timingDiagnostics);
    }

    [Fact]
    public async Task Search_reports_schema_incompatible_index_with_force_rebuild_guidance()
    {
        using var repo = TestRepo.Create();
        await CreateProjectAsync(repo.Root);

        var index = await RunCliAsync(new[] { "index", ".", "--json" }, repo.Root);
        var manifestPath = IndexFile(repo.Root, "manifest.json");
        using (var manifest = JsonDocument.Parse(await File.ReadAllTextAsync(manifestPath)))
        {
            var manifestObject = manifest.RootElement.EnumerateObject()
                .ToDictionary(property => property.Name, property => property.Value.Clone(), StringComparer.Ordinal);
            manifestObject["schemaVersion"] = JsonDocument.Parse("1").RootElement.Clone();
            await File.WriteAllTextAsync(manifestPath, JsonSerializer.Serialize(manifestObject));
        }

        var search = await RunCliAsync(new[] { "search", "CustomerService", "--json" }, repo.Root);

        Assert.Equal(0, index.ExitCode);
        Assert.Equal(3, search.ExitCode);
        using var searchJson = JsonDocument.Parse(search.Stdout);
        Assert.False(searchJson.RootElement.GetProperty("success").GetBoolean());
        var error = searchJson.RootElement.GetProperty("errors")[0].GetString();
        Assert.Contains("schema", error, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("ri index --force", error, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Search_supports_exact_fqn_tokens_phrase_file_path_limit_and_json()
    {
        using var repo = TestRepo.Create();
        await CreateSearchProjectAsync(repo.Root);

        var index = await RunCliAsync(new[] { "index", ".", "--json" }, repo.Root);
        var exactFqn = await RunCliAsync(new[] { "search", "My.Namespace.CustomerService", "--json" }, repo.Root);
        var separateTokens = await RunCliAsync(new[] { "search", "customer", "service", "--json" }, repo.Root);
        var phrase = await RunCliAsync(new[] { "search", "\"Customer Service\"", "--mode", "text", "--json" }, repo.Root);
        var fileMode = await RunCliAsync(new[] { "search", "CustomerService.cs", "--mode", "file", "--json" }, repo.Root);
        var pathFilter = await RunCliAsync(new[] { "search", "CustomerService", "--path", "Services", "--json" }, repo.Root);
        var limited = await RunCliAsync(new[] { "search", "CustomerService", "--limit", "1", "--json" }, repo.Root);

        Assert.Equal(0, index.ExitCode);
        Assert.Equal(0, exactFqn.ExitCode);
        Assert.Equal(0, separateTokens.ExitCode);
        Assert.Equal(0, phrase.ExitCode);
        Assert.Equal(0, fileMode.ExitCode);
        Assert.Equal(0, pathFilter.ExitCode);
        Assert.Equal(0, limited.ExitCode);

        using var exactJson = JsonDocument.Parse(exactFqn.Stdout);
        var exactResults = exactJson.RootElement.GetProperty("data").EnumerateArray().ToArray();
        Assert.NotEmpty(exactResults);
        Assert.Equal("My.Namespace.CustomerService", exactResults[0].GetProperty("fullyQualifiedName").GetString());
        Assert.Equal(exactResults.Max(r => r.GetProperty("score").GetDouble()), exactResults[0].GetProperty("score").GetDouble());

        using var tokenJson = JsonDocument.Parse(separateTokens.Stdout);
        Assert.Contains(tokenJson.RootElement.GetProperty("data").EnumerateArray(), result =>
            result.TryGetProperty("fullyQualifiedName", out var fullyQualifiedName) && fullyQualifiedName.GetString() == "My.Namespace.CustomerService");

        using var phraseJson = JsonDocument.Parse(phrase.Stdout);
        var phraseResults = phraseJson.RootElement.GetProperty("data").EnumerateArray().ToArray();
        Assert.NotEmpty(phraseResults);
        Assert.All(phraseResults, result => Assert.Contains("Customer Service", result.GetProperty("snippet").GetString(), StringComparison.OrdinalIgnoreCase));

        using var fileJson = JsonDocument.Parse(fileMode.Stdout);
        var fileResult = Assert.Single(fileJson.RootElement.GetProperty("data").EnumerateArray());
        Assert.Equal("file", fileResult.GetProperty("kind").GetString());
        Assert.EndsWith("Services/CustomerService.cs", fileResult.GetProperty("path").GetString(), StringComparison.Ordinal);

        using var pathJson = JsonDocument.Parse(pathFilter.Stdout);
        var pathResults = pathJson.RootElement.GetProperty("data").EnumerateArray().ToArray();
        Assert.NotEmpty(pathResults);
        Assert.All(pathResults, result => Assert.Contains("Services", result.GetProperty("path").GetString(), StringComparison.Ordinal));

        using var limitedJson = JsonDocument.Parse(limited.Stdout);
        Assert.True(limitedJson.RootElement.GetProperty("success").GetBoolean());
        Assert.Equal(0, limitedJson.RootElement.GetProperty("exitCode").GetInt32());
        Assert.Single(limitedJson.RootElement.GetProperty("data").EnumerateArray());
        Assert.Single(limitedJson.RootElement.GetProperty("results").EnumerateArray());
    }

    [Fact]
    public async Task Search_supports_acronym_kind_filter_goto_and_dedupes_all_mode_results()
    {
        using var repo = TestRepo.Create();
        await CreateSearchProjectAsync(repo.Root);

        var index = await RunCliAsync(new[] { "index", ".", "--json" }, repo.Root);
        var acronym = await RunCliAsync(new[] { "search", "CS", "--json" }, repo.Root);
        var methodKind = await RunCliAsync(new[] { "search", "--mode", "symbol", "--kind", "method", "GetCustomerAsync", "--json" }, repo.Root);
        var gotoClass = await RunCliAsync(new[] { "goto", "CustomerService", "--json" }, repo.Root);
        var deduped = await RunCliAsync(new[] { "search", "CustomerService", "--limit", "5", "--json" }, repo.Root);

        Assert.Equal(0, index.ExitCode);
        Assert.Equal(0, acronym.ExitCode);
        Assert.Equal(0, methodKind.ExitCode);
        Assert.Equal(0, gotoClass.ExitCode);
        Assert.Equal(0, deduped.ExitCode);

        using var acronymJson = JsonDocument.Parse(acronym.Stdout);
        var acronymResults = acronymJson.RootElement.GetProperty("data").EnumerateArray().ToArray();
        Assert.Contains(acronymResults, result =>
            result.GetProperty("fullyQualifiedName").GetString() == "My.Namespace.CustomerService"
            && result.GetProperty("matchReason").GetString() == "acronym-symbol");

        using var methodKindJson = JsonDocument.Parse(methodKind.Stdout);
        var methodResults = methodKindJson.RootElement.GetProperty("data").EnumerateArray().ToArray();
        Assert.NotEmpty(methodResults);
        Assert.All(methodResults, result => Assert.Equal("method", result.GetProperty("kind").GetString()));
        Assert.Contains(methodResults, result =>
            result.GetProperty("symbolName").GetString() == "GetCustomerAsync"
            && result.GetProperty("fullyQualifiedName").GetString()!.Contains("GetCustomerAsync", StringComparison.Ordinal));

        using var gotoJson = JsonDocument.Parse(gotoClass.Stdout);
        var gotoResults = gotoJson.RootElement.GetProperty("data").EnumerateArray().ToArray();
        Assert.NotEmpty(gotoResults);
        Assert.Equal("class", gotoResults[0].GetProperty("kind").GetString());
        Assert.Equal("My.Namespace.CustomerService", gotoResults[0].GetProperty("fullyQualifiedName").GetString());

        using var dedupedJson = JsonDocument.Parse(deduped.Stdout);
        var resultLocations = dedupedJson.RootElement.GetProperty("data").EnumerateArray()
            .Select(result => new
            {
                Path = result.GetProperty("path").GetString(),
                Line = result.GetProperty("line").GetInt32(),
                Column = result.GetProperty("column").GetInt32()
            })
            .ToArray();

        Assert.Equal(
            resultLocations.Length,
            resultLocations.Distinct().Count());
    }

    [Fact]
    public async Task Search_from_file_boosts_current_project_and_exclude_tests_removes_test_project_results()
    {
        using var repo = TestRepo.Create();
        await CreateProjectWithDuplicateNamesAsync(repo.Root);

        var index = await RunCliAsync(new[] { "index", ".", "--json" }, repo.Root);
        var boosted = await RunCliAsync(new[] { "search", "CustomerService", "--from-file", "src/App/OrderController.cs", "--json" }, repo.Root);
        var excludeTests = await RunCliAsync(new[] { "search", "CustomerService", "--exclude-tests", "--json" }, repo.Root);

        Assert.Equal(0, index.ExitCode);
        Assert.Equal(0, boosted.ExitCode);
        Assert.Equal(0, excludeTests.ExitCode);

        using var boostedJson = JsonDocument.Parse(boosted.Stdout);
        var boostedResults = boostedJson.RootElement.GetProperty("data").EnumerateArray().ToArray();
        Assert.NotEmpty(boostedResults);
        Assert.Equal("App", boostedResults[0].GetProperty("projectName").GetString());
        Assert.Contains("context-boost", boostedResults[0].GetProperty("matchReason").GetString(), StringComparison.OrdinalIgnoreCase);

        using var excludeJson = JsonDocument.Parse(excludeTests.Stdout);
        var excludeResults = excludeJson.RootElement.GetProperty("data").EnumerateArray().ToArray();
        Assert.NotEmpty(excludeResults);
        Assert.DoesNotContain(excludeResults, result => result.GetProperty("projectName").GetString() == "App.Tests");
        Assert.DoesNotContain(excludeResults, result => result.GetProperty("path").GetString()!.Contains("Tests", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task Index_persists_rich_document_symbol_and_reference_metadata()
    {
        using var repo = TestRepo.Create();
        await CreateProjectAsync(repo.Root);

        var index = await RunCliAsync(new[] { "index", ".", "--json" }, repo.Root);

        Assert.Equal(0, index.ExitCode);
        using var manifestJson = JsonDocument.Parse(File.ReadAllText(IndexFile(repo.Root, "manifest.json")));
        Assert.Equal(repo.Root, manifestJson.RootElement.GetProperty("repoRoot").GetString());
        Assert.All(manifestJson.RootElement.GetProperty("workspaceInputs").EnumerateArray(), input =>
            Assert.False(Path.IsPathRooted(input.GetProperty("path").GetString())));
        var documents = ReadJsonLines(IndexFile(repo.Root, "documents.jsonl")).ToArray();
        var symbols = ReadJsonLines(IndexFile(repo.Root, "symbols.jsonl")).ToArray();
        var references = ReadJsonLines(IndexFile(repo.Root, "references.jsonl")).ToArray();
        Assert.True(File.Exists(IndexFile(repo.Root, "diagnostics.jsonl")));
        var document = documents.Single(d => d.GetProperty("relativePath").GetString() == "CustomerService.cs");
        var classSymbol = symbols.Single(s => s.GetProperty("name").GetString() == "CustomerService" && s.GetProperty("kind").GetString() == "class");
        var methodSymbol = symbols.Single(s => s.GetProperty("name").GetString() == "GetCustomerAsync" && s.GetProperty("kind").GetString() == "method");
        var methodReference = references.First(r => r.GetProperty("referencedName").GetString() == "GetCustomerAsync");

        Assert.Equal("Sample", document.GetProperty("projectName").GetString());
        Assert.False(string.IsNullOrWhiteSpace(document.GetProperty("projectId").GetString()));
        Assert.Equal("C#", document.GetProperty("language").GetString());
        Assert.False(document.GetProperty("isNonCSharpText").GetBoolean());
        Assert.True(document.GetProperty("lengthBytes").GetInt64() > 0);
        Assert.True(document.GetProperty("lineCount").GetInt32() > 0);
        Assert.False(string.IsNullOrWhiteSpace(document.GetProperty("lastWriteUtc").GetString()));

        Assert.Equal(document.GetProperty("documentId").GetString(), classSymbol.GetProperty("documentId").GetString());
        Assert.Equal(document.GetProperty("projectId").GetString(), classSymbol.GetProperty("projectId").GetString());
        Assert.Equal("CustomerService", classSymbol.GetProperty("metadataName").GetString());
        Assert.Equal("My.App.Services", classSymbol.GetProperty("containerName").GetString());
        Assert.Equal("public", classSymbol.GetProperty("accessibility").GetString());
        Assert.True(classSymbol.GetProperty("spanStart").GetInt32() >= 0);
        Assert.True(classSymbol.GetProperty("spanLength").GetInt32() > 0);
        Assert.True(classSymbol.GetProperty("isDefinition").GetBoolean());
        Assert.False(classSymbol.GetProperty("isPartial").GetBoolean());
        Assert.Equal("Task<string>", methodSymbol.GetProperty("returnType").GetString());
        Assert.Contains("string", methodSymbol.GetProperty("parameterTypes").EnumerateArray().Select(p => p.GetString()));

        Assert.False(string.IsNullOrWhiteSpace(methodReference.GetProperty("referenceId").GetString()));
        Assert.Equal(document.GetProperty("documentId").GetString(), methodReference.GetProperty("documentId").GetString());
        Assert.Equal(document.GetProperty("projectId").GetString(), methodReference.GetProperty("projectId").GetString());
        Assert.True(methodReference.GetProperty("spanStart").GetInt32() >= 0);
        Assert.True(methodReference.GetProperty("spanLength").GetInt32() > 0);
        Assert.Equal("invocation", methodReference.GetProperty("referenceKind").GetString());
    }

    [Fact]
    public async Task Index_persists_token_weights_and_search_outputs_rich_result_metadata()
    {
        using var repo = TestRepo.Create();
        await CreateProjectAsync(repo.Root);

        var index = await RunCliAsync(new[] { "index", ".", "--json" }, repo.Root);
        var symbolSearch = await RunCliAsync(new[] { "search", "CustomerService", "--mode", "symbol", "--json" }, repo.Root);
        var referenceSearch = await RunCliAsync(new[] { "search", "GetCustomerAsync", "--mode", "reference", "--json" }, repo.Root);

        Assert.Equal(0, index.ExitCode);
        Assert.Equal(0, symbolSearch.ExitCode);
        Assert.Equal(0, referenceSearch.ExitCode);
        var tokens = ReadJsonLines(IndexFile(repo.Root, "tokens.jsonl")).ToArray();
        Assert.Contains(tokens, t => t.GetProperty("token").GetString() == "customerservice" && t.GetProperty("weight").GetString() == "symbol-name");
        Assert.Contains(tokens, t => t.GetProperty("field").GetString() == "csharp" && t.GetProperty("weight").GetString() == "identifier");

        using var symbolJson = JsonDocument.Parse(symbolSearch.Stdout);
        var symbolResult = symbolJson.RootElement.GetProperty("data")[0];
        Assert.Equal("CustomerService", symbolResult.GetProperty("symbolName").GetString());
        Assert.Equal("My.App.Services.CustomerService", symbolResult.GetProperty("fullyQualifiedName").GetString());
        Assert.Equal("My.App.Services", symbolResult.GetProperty("containingType").GetString());
        Assert.Equal(symbolResult.GetProperty("line").GetInt32(), symbolResult.GetProperty("startLine").GetInt32());
        Assert.Equal(symbolResult.GetProperty("column").GetInt32(), symbolResult.GetProperty("startColumn").GetInt32());
        Assert.True(symbolResult.GetProperty("endLine").GetInt32() >= symbolResult.GetProperty("line").GetInt32());
        Assert.True(symbolResult.GetProperty("endColumn").GetInt32() > 0);

        using var referenceJson = JsonDocument.Parse(referenceSearch.Stdout);
        var referenceResult = referenceJson.RootElement.GetProperty("data")[0];
        Assert.Equal("GetCustomerAsync", referenceResult.GetProperty("symbolName").GetString());
        Assert.Equal("invocation", referenceResult.GetProperty("referenceKind").GetString());
        Assert.True(referenceResult.GetProperty("endLine").GetInt32() >= referenceResult.GetProperty("line").GetInt32());
    }

    [Fact]
    public async Task Human_search_output_uses_titles_snippets_symbol_fqn_reference_kind_and_showing_summary()
    {
        using var repo = TestRepo.Create();
        await CreateProjectAsync(repo.Root);

        var index = await RunCliAsync(new[] { "index", ".", "--json" }, repo.Root);
        var symbolSearch = await RunCliAsync(new[] { "search", "CustomerService", "--mode", "symbol" }, repo.Root);
        var referenceSearch = await RunCliAsync(new[] { "search", "GetCustomerAsync", "--mode", "reference" }, repo.Root);
        var multipleResults = await RunCliAsync(new[] { "search", "Customer", "--limit", "3" }, repo.Root);

        Assert.Equal(0, index.ExitCode);
        Assert.Equal(0, symbolSearch.ExitCode);
        Assert.Equal(0, referenceSearch.ExitCode);
        Assert.Equal(0, multipleResults.ExitCode);

        Assert.DoesNotContain("{", symbolSearch.Stdout, StringComparison.Ordinal);
        Assert.Contains("[class] My.App.Services.CustomerService", symbolSearch.Stdout, StringComparison.Ordinal);
        Assert.Contains("CustomerService.cs:", symbolSearch.Stdout, StringComparison.Ordinal);
        Assert.Contains("score=", symbolSearch.Stdout, StringComparison.Ordinal);
        Assert.Contains("    public sealed class CustomerService", symbolSearch.Stdout, StringComparison.Ordinal);

        Assert.DoesNotContain("{", referenceSearch.Stdout, StringComparison.Ordinal);
        Assert.Contains("[reference] GetCustomerAsync", referenceSearch.Stdout, StringComparison.Ordinal);
        Assert.Contains("ref-kind=invocation", referenceSearch.Stdout, StringComparison.Ordinal);
        Assert.Contains("    return service.GetCustomerAsync(\"42\");", referenceSearch.Stdout, StringComparison.Ordinal);

        Assert.Contains("showing 3 of 3", multipleResults.Stdout, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Index_classifies_simple_read_and_write_references()
    {
        using var repo = TestRepo.Create();
        await CreateProjectAsync(repo.Root);

        var index = await RunCliAsync(new[] { "index", ".", "--json" }, repo.Root);

        Assert.Equal(0, index.ExitCode);
        var fieldReferences = ReadJsonLines(IndexFile(repo.Root, "references.jsonl"))
            .Where(r => r.GetProperty("referencedName").GetString() == "_currentName")
            .Select(r => r.GetProperty("referenceKind").GetString())
            .ToArray();

        Assert.Contains("write", fieldReferences);
        Assert.Contains("read", fieldReferences);
    }

    [Fact]
    public async Task Exact_refs_reports_ambiguous_symbol_candidates_before_loading_workspace()
    {
        using var repo = TestRepo.Create();
        await File.WriteAllTextAsync(Path.Combine(repo.Root, "Sample.csproj"), """
            <Project Sdk="Microsoft.NET.Sdk">
              <PropertyGroup>
                <TargetFramework>net8.0</TargetFramework>
                <ImplicitUsings>enable</ImplicitUsings>
                <Nullable>enable</Nullable>
              </PropertyGroup>
            </Project>
            """);
        await File.WriteAllTextAsync(Path.Combine(repo.Root, "One.cs"), """
            namespace One;
            public sealed class Duplicate
            {
                public void Run() { }
            }
            """);
        await File.WriteAllTextAsync(Path.Combine(repo.Root, "Two.cs"), """
            namespace Two;
            public sealed class Duplicate
            {
                public void Run() { }
            }
            """);

        var index = await RunCliAsync(new[] { "index", ".", "--json" }, repo.Root);
        var refs = await RunCliAsync(new[] { "refs", "Duplicate", "--exact", "--json" }, repo.Root);

        Assert.Equal(0, index.ExitCode);
        Assert.Equal(1, refs.ExitCode);
        using var json = JsonDocument.Parse(refs.Stdout);
        Assert.False(json.RootElement.GetProperty("success").GetBoolean());
        Assert.Contains("--symbol-id", json.RootElement.GetProperty("errors")[0].GetString(), StringComparison.Ordinal);
        Assert.Equal(2, json.RootElement.GetProperty("data").GetArrayLength());
    }

    [Fact]
    public async Task Exact_refs_uses_configured_timeout_and_reports_cancellation()
    {
        using var repo = TestRepo.Create();
        await CreateProjectAsync(repo.Root);
        await File.WriteAllTextAsync(Path.Combine(repo.Root, ".roslyn-index.json"), """
            {
              "exactRefsTimeoutSeconds": 0
            }
            """);

        var index = await RunCliAsync(new[] { "index", ".", "--json" }, repo.Root);
        var refs = await RunCliAsync(new[] { "refs", "GetCustomerAsync", "--exact", "--json" }, repo.Root);

        Assert.Equal(0, index.ExitCode);
        Assert.Equal(5, refs.ExitCode);
        using var json = JsonDocument.Parse(refs.Stdout);
        Assert.False(json.RootElement.GetProperty("success").GetBoolean());
        Assert.Contains("timed out", json.RootElement.GetProperty("errors")[0].GetString(), StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Exact_refs_uses_cache_until_index_updated_utc_changes()
    {
        using var repo = TestRepo.Create();
        await CreateProjectAsync(repo.Root);

        var index = await RunCliAsync(new[] { "index", ".", "--json" }, repo.Root);
        var first = await RunCliAsync(new[] { "refs", "GetCustomerAsync", "--exact", "--json" }, repo.Root);
        await File.WriteAllTextAsync(Path.Combine(repo.Root, ".roslyn-index.json"), """
            {
              "exactRefsTimeoutSeconds": 0
            }
            """);
        var cached = await RunCliAsync(new[] { "refs", "GetCustomerAsync", "--exact", "--json" }, repo.Root);
        await ChangeIndexUpdatedUtcAsync(repo.Root, DateTimeOffset.UtcNow.AddDays(1));
        var invalidated = await RunCliAsync(new[] { "refs", "GetCustomerAsync", "--exact", "--json" }, repo.Root);

        Assert.Equal(0, index.ExitCode);
        Assert.Equal(0, first.ExitCode);
        Assert.Equal(0, cached.ExitCode);
        Assert.Equal(5, invalidated.ExitCode);

        Assert.True(Directory.Exists(IndexStore.GetExactReferenceCacheDirectory(repo.Root)));
        using var firstJson = JsonDocument.Parse(first.Stdout);
        using var cachedJson = JsonDocument.Parse(cached.Stdout);
        Assert.NotEmpty(firstJson.RootElement.GetProperty("data").EnumerateArray());
        Assert.Equal(
            firstJson.RootElement.GetProperty("data").GetRawText(),
            cachedJson.RootElement.GetProperty("data").GetRawText());
    }

    [Fact]
    public async Task Exact_refs_outputs_real_references_sorted_by_path_line_and_column()
    {
        using var repo = TestRepo.Create();
        await CreateExactRefsSortingProjectAsync(repo.Root);

        var index = await RunCliAsync(new[] { "index", ".", "--json" }, repo.Root);
        Assert.Equal(0, index.ExitCode);

        var symbolId = ReadJsonLines(IndexFile(repo.Root, "symbols.jsonl"))
            .Single(symbol => symbol.GetProperty("name").GetString() == "GetName"
                              && symbol.GetProperty("containerName").GetString() == "ExactRefs.Sorting.TargetService")
            .GetProperty("symbolId")
            .GetString()!;
        var refsJson = await RunCliAsync(new[] { "refs", "--symbol-id", symbolId, "--exact", "--json" }, repo.Root);
        var refsHuman = await RunCliAsync(new[] { "refs", "--symbol-id", symbolId, "--exact" }, repo.Root);

        Assert.Equal(0, refsJson.ExitCode);
        Assert.Equal(0, refsHuman.ExitCode);

        using var json = JsonDocument.Parse(refsJson.Stdout);
        var results = json.RootElement.GetProperty("data").EnumerateArray()
            .Select(result => new
            {
                Path = result.GetProperty("path").GetString()!,
                Line = result.GetProperty("line").GetInt32(),
                Column = result.GetProperty("column").GetInt32(),
                Kind = result.GetProperty("kind").GetString(),
                SymbolName = result.GetProperty("symbolName").GetString(),
                MatchReason = result.GetProperty("matchReason").GetString()
            })
            .ToArray();

        Assert.Equal(new[] { "AUsage.cs", "AUsage.cs", "ZUsage.cs" }, results.Select(result => result.Path).ToArray());
        Assert.True(results[0].Line < results[1].Line || results[0].Column < results[1].Column);
        Assert.All(results, result =>
        {
            Assert.Equal("reference", result.Kind);
            Assert.Equal("GetName", result.SymbolName);
            Assert.Equal("exact Roslyn reference", result.MatchReason);
        });

        Assert.Contains("[reference] GetName", refsHuman.Stdout, StringComparison.Ordinal);
        Assert.Contains("AUsage.cs:", refsHuman.Stdout, StringComparison.Ordinal);
        Assert.Contains("ZUsage.cs:", refsHuman.Stdout, StringComparison.Ordinal);
        Assert.True(refsHuman.Stdout.IndexOf("AUsage.cs:", StringComparison.Ordinal) < refsHuman.Stdout.IndexOf("ZUsage.cs:", StringComparison.Ordinal));
    }

    private static async Task CreateProjectAsync(string root)
    {
        await File.WriteAllTextAsync(Path.Combine(root, "Sample.csproj"), """
            <Project Sdk="Microsoft.NET.Sdk">
              <PropertyGroup>
                <TargetFramework>net8.0</TargetFramework>
                <ImplicitUsings>enable</ImplicitUsings>
                <Nullable>enable</Nullable>
              </PropertyGroup>
            </Project>
            """);
        await File.WriteAllTextAsync(Path.Combine(root, "CustomerService.cs"), """
            namespace My.App.Services;

            public sealed class CustomerService
            {
                private string _currentName = string.Empty;

                public Task<string> GetCustomerAsync(string id)
                {
                    _currentName = id;
                    var lastName = _currentName;
                    return Task.FromResult(id);
                }
            }

            public sealed class CustomerController
            {
                public Task<string> Get(CustomerService service)
                {
                    return service.GetCustomerAsync("42");
                }
            }
            """);
    }

    private static async Task CreateExactRefsSortingProjectAsync(string root)
    {
        await File.WriteAllTextAsync(Path.Combine(root, "ExactRefsSorting.csproj"), """
            <Project Sdk="Microsoft.NET.Sdk">
              <PropertyGroup>
                <TargetFramework>net8.0</TargetFramework>
                <ImplicitUsings>enable</ImplicitUsings>
                <Nullable>enable</Nullable>
                <EnableDefaultCompileItems>false</EnableDefaultCompileItems>
              </PropertyGroup>
              <ItemGroup>
                <Compile Include="TargetService.cs" />
                <Compile Include="ZUsage.cs" />
                <Compile Include="AUsage.cs" />
              </ItemGroup>
            </Project>
            """);
        await File.WriteAllTextAsync(Path.Combine(root, "TargetService.cs"), """
            namespace ExactRefs.Sorting;

            public sealed class TargetService
            {
                public string GetName() => "target";
            }
            """);
        await File.WriteAllTextAsync(Path.Combine(root, "ZUsage.cs"), """
            namespace ExactRefs.Sorting;

            public static class ZUsage
            {
                public static string Run(TargetService service)
                {
                    return service.GetName();
                }
            }
            """);
        await File.WriteAllTextAsync(Path.Combine(root, "AUsage.cs"), """
            namespace ExactRefs.Sorting;

            public static class AUsage
            {
                public static string Run(TargetService service)
                {
                    var first = service.GetName();
                    return first + service.GetName();
                }
            }
            """);
    }

    private static async Task CreateSearchProjectAsync(string root)
    {
        Directory.CreateDirectory(Path.Combine(root, "Services"));
        Directory.CreateDirectory(Path.Combine(root, "Other"));
        await File.WriteAllTextAsync(Path.Combine(root, "SearchSample.csproj"), """
            <Project Sdk="Microsoft.NET.Sdk">
              <PropertyGroup>
                <TargetFramework>net8.0</TargetFramework>
                <ImplicitUsings>enable</ImplicitUsings>
                <Nullable>enable</Nullable>
              </PropertyGroup>
            </Project>
            """);
        await File.WriteAllTextAsync(Path.Combine(root, "Services", "CustomerService.cs"), """
            namespace My.Namespace;

            public sealed class CustomerService
            {
                public const string DisplayName = "Customer Service";

                public Task<string> GetCustomerAsync(string id)
                {
                    return Task.FromResult(DisplayName + id);
                }
            }
            """);
        await File.WriteAllTextAsync(Path.Combine(root, "Other", "CustomerNotes.cs"), """
            namespace My.Namespace.Other;

            public sealed class CustomerNotes
            {
                public string Describe()
                {
                    return "Customer records are grouped by service tier.";
                }
            }
            """);
    }

    private static async Task CreateProjectWithDuplicateNamesAsync(string root)
    {
        Directory.CreateDirectory(Path.Combine(root, "src", "App"));
        Directory.CreateDirectory(Path.Combine(root, "tests", "App.Tests"));
        await File.WriteAllTextAsync(Path.Combine(root, "App.csproj"), """
            <Project Sdk="Microsoft.NET.Sdk">
              <PropertyGroup>
                <TargetFramework>net8.0</TargetFramework>
                <ImplicitUsings>enable</ImplicitUsings>
                <Nullable>enable</Nullable>
                <EnableDefaultCompileItems>false</EnableDefaultCompileItems>
              </PropertyGroup>
              <ItemGroup>
                <Compile Include="src\App\**\*.cs" />
              </ItemGroup>
            </Project>
            """);
        await File.WriteAllTextAsync(Path.Combine(root, "tests", "App.Tests", "App.Tests.csproj"), """
            <Project Sdk="Microsoft.NET.Sdk">
              <PropertyGroup>
                <TargetFramework>net8.0</TargetFramework>
                <ImplicitUsings>enable</ImplicitUsings>
                <Nullable>enable</Nullable>
                <EnableDefaultCompileItems>false</EnableDefaultCompileItems>
              </PropertyGroup>
              <ItemGroup>
                <Compile Include="**\*.cs" />
              </ItemGroup>
            </Project>
            """);
        await File.WriteAllTextAsync(Path.Combine(root, "src", "App", "OrderController.cs"), """
            namespace App;

            public sealed class OrderController
            {
                public CustomerService Service { get; } = new();
            }
            """);
        await File.WriteAllTextAsync(Path.Combine(root, "src", "App", "CustomerService.cs"), """
            namespace App;

            public sealed class CustomerService
            {
                public string Name => "production";
            }
            """);
        await File.WriteAllTextAsync(Path.Combine(root, "tests", "App.Tests", "CustomerServiceTests.cs"), """
            namespace App.Tests;

            public sealed class CustomerService
            {
                public string Name => "test double";
            }
            """);
    }

    private static async Task CreateDeclarationHashProjectAsync(string root)
    {
        await File.WriteAllTextAsync(Path.Combine(root, "Hashing.csproj"), """
            <Project Sdk="Microsoft.NET.Sdk">
              <PropertyGroup>
                <TargetFramework>net8.0</TargetFramework>
                <ImplicitUsings>enable</ImplicitUsings>
                <Nullable>enable</Nullable>
              </PropertyGroup>
            </Project>
            """);
        await File.WriteAllTextAsync(Path.Combine(root, "HashSubject.cs"), DeclarationHashSubjectSource("""
                return "initial";
            """));
        await File.WriteAllTextAsync(Path.Combine(root, "Unchanged.cs"), """
            namespace Hashing.Sample;

            public sealed class Unchanged
            {
                public string Name => "stable";
            }
            """);
    }

    private static async Task CreateSemanticDependencySolutionAsync(string root)
    {
        Directory.CreateDirectory(Path.Combine(root, "src", "Core"));
        Directory.CreateDirectory(Path.Combine(root, "src", "App"));
        await File.WriteAllTextAsync(Path.Combine(root, "DependencySample.sln"), """
            
            Microsoft Visual Studio Solution File, Format Version 12.00
            # Visual Studio Version 17
            VisualStudioVersion = 17.0.31903.59
            MinimumVisualStudioVersion = 10.0.40219.1
            Project("{FAE04EC0-301F-11D3-BF4B-00C04F79EFBC}") = "Core", "src\Core\Core.csproj", "{11111111-1111-1111-1111-111111111111}"
            EndProject
            Project("{FAE04EC0-301F-11D3-BF4B-00C04F79EFBC}") = "App", "src\App\App.csproj", "{22222222-2222-2222-2222-222222222222}"
            EndProject
            Global
            EndGlobal
            """);
        await File.WriteAllTextAsync(Path.Combine(root, "src", "Core", "Core.csproj"), """
            <Project Sdk="Microsoft.NET.Sdk">
              <PropertyGroup>
                <TargetFramework>net8.0</TargetFramework>
                <ImplicitUsings>enable</ImplicitUsings>
                <Nullable>enable</Nullable>
              </PropertyGroup>
            </Project>
            """);
        await File.WriteAllTextAsync(Path.Combine(root, "src", "App", "App.csproj"), """
            <Project Sdk="Microsoft.NET.Sdk">
              <PropertyGroup>
                <TargetFramework>net8.0</TargetFramework>
                <ImplicitUsings>enable</ImplicitUsings>
                <Nullable>enable</Nullable>
              </PropertyGroup>
              <ItemGroup>
                <ProjectReference Include="..\Core\Core.csproj" />
              </ItemGroup>
            </Project>
            """);
        await File.WriteAllTextAsync(Path.Combine(root, "src", "Core", "DomainThing.cs"), """
            namespace Dependency.Core;

            public sealed class DomainThing
            {
                public string Value() => "initial";
            }
            """);
        await File.WriteAllTextAsync(Path.Combine(root, "src", "App", "AppService.cs"), """
            using Dependency.Core;

            namespace Dependency.App;

            public sealed class AppService
            {
                public string Read(DomainThing thing) => thing.Value();
            }
            """);
    }

    private static async Task CreateManyDocumentProjectAsync(string root, int documentCount)
    {
        await File.WriteAllTextAsync(Path.Combine(root, "ManyDocs.csproj"), """
            <Project Sdk="Microsoft.NET.Sdk">
              <PropertyGroup>
                <TargetFramework>net8.0</TargetFramework>
                <ImplicitUsings>enable</ImplicitUsings>
                <Nullable>enable</Nullable>
              </PropertyGroup>
            </Project>
            """);

        for (var i = 1; i <= documentCount; i++)
        {
            await File.WriteAllTextAsync(Path.Combine(root, $"Feature{i}.cs"), ManyDocumentSource(i, """
                    return "initial";
                """));
        }
    }

    private static string ManyDocumentSource(int number, string body)
        => $$"""
            namespace ManyDocs;

            public sealed class Feature{{number}}
            {
                public string Read()
                {
            {{body}}
                }
            }
            """;

    private static string DeclarationHashSubjectSource(string body)
        => $$"""
            namespace Hashing.Sample;

            public sealed class HashSubject
            {
                string PrivateValue()
                {
            {{body}}
                }
            }
            """;

    private static string ReadDeclarationHash(string root, string relativePath)
        => IndexStore.Read(root).Documents.Single(document => document.RelativePath == relativePath).DeclarationHash;

    private static string CanonicalRows(IndexSnapshot snapshot)
        => JsonSerializer.Serialize(new
        {
            Documents = snapshot.Documents.OrderBy(row => row.DocumentId, StringComparer.Ordinal),
            Symbols = snapshot.Symbols.OrderBy(row => row.SymbolId, StringComparer.Ordinal).ThenBy(row => row.DocumentId, StringComparer.Ordinal).ThenBy(row => row.SpanStart),
            References = snapshot.References.OrderBy(row => row.ReferenceId, StringComparer.Ordinal),
            Tokens = snapshot.Tokens.OrderBy(row => row.Token, StringComparer.Ordinal).ThenBy(row => row.DocumentId, StringComparer.Ordinal).ThenBy(row => row.Line).ThenBy(row => row.Column)
        }, JsonOptions.Compact);

    private static async Task<CliResult> RunCliAsync(string[] args, string workingDirectory)
    {
        var project = Path.Combine(TestPaths.RepositoryRoot, "tools", "RoslynRepoIndexer", "src", "RoslynRepoIndexer.Cli", "RoslynRepoIndexer.Cli.csproj");
        var psi = new ProcessStartInfo("dotnet")
        {
            WorkingDirectory = workingDirectory,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
        };
        psi.Environment["RI_DISABLE_DAEMON"] = "1";
        psi.ArgumentList.Add("run");
        psi.ArgumentList.Add("--project");
        psi.ArgumentList.Add(project);
        psi.ArgumentList.Add("--");
        foreach (var arg in args)
        {
            psi.ArgumentList.Add(arg);
        }

        using var process = Process.Start(psi) ?? throw new InvalidOperationException("Failed to start dotnet.");
        var stdout = await process.StandardOutput.ReadToEndAsync();
        var stderr = await process.StandardError.ReadToEndAsync();
        await process.WaitForExitAsync();
        return new CliResult(process.ExitCode, stdout, stderr);
    }

    private static IEnumerable<JsonElement> ReadJsonLines(string path)
    {
        if (File.Exists(path))
        {
            foreach (var line in File.ReadLines(path).Where(line => !string.IsNullOrWhiteSpace(line)))
            {
                using var document = JsonDocument.Parse(line);
                yield return document.RootElement.Clone();
            }
            yield break;
        }

        var repoRoot = FindRepositoryRootFromIndexPath(path);
        var snapshot = IndexStore.Read(repoRoot);
        IEnumerable<object> rows = Path.GetFileName(path) switch
        {
            "documents.jsonl" => snapshot.Documents.Cast<object>(),
            "symbols.jsonl" => snapshot.Symbols.Cast<object>(),
            "references.jsonl" => snapshot.References.Cast<object>(),
            "tokens.jsonl" => snapshot.Tokens.Cast<object>(),
            _ => throw new InvalidOperationException($"Unknown index table '{Path.GetFileName(path)}'.")
        };
        foreach (var row in rows)
        {
            yield return JsonSerializer.SerializeToElement(row, JsonOptions.Default);
        }
    }

    private static string IndexFile(string root, string fileName)
        => Path.Combine(IndexStore.GetVersionDirectory(root), fileName);

    private static string FindRepositoryRootFromIndexPath(string path)
    {
        for (var directory = Directory.GetParent(path); directory is not null; directory = directory.Parent)
        {
            if (directory.Name == IndexStore.IndexDirectoryName && directory.Parent is not null) return directory.Parent.FullName;
        }
        throw new InvalidOperationException("Missing repository root.");
    }

    private static async Task ChangeIndexUpdatedUtcAsync(string root, DateTimeOffset updatedUtc)
    {
        var manifestPath = IndexFile(root, "manifest.json");
        using var manifest = JsonDocument.Parse(await File.ReadAllTextAsync(manifestPath));
        var manifestObject = manifest.RootElement.EnumerateObject()
            .ToDictionary(property => property.Name, property => property.Value.Clone(), StringComparer.Ordinal);
        manifestObject["updatedUtc"] = JsonDocument.Parse(JsonSerializer.Serialize(updatedUtc)).RootElement.Clone();
        await File.WriteAllTextAsync(manifestPath, JsonSerializer.Serialize(manifestObject));
    }

    private static void AssertTiming(JsonElement timings, string propertyName)
    {
        Assert.True(timings.TryGetProperty(propertyName, out var value), $"Missing timing '{propertyName}'.");
        Assert.Equal(JsonValueKind.Number, value.ValueKind);
        Assert.True(value.GetInt64() >= 0, $"Timing '{propertyName}' must be non-negative.");
    }

    private static void AssertCliSuccess(CliResult result)
        => Assert.True(result.ExitCode == 0, $"Expected exit code 0, got {result.ExitCode}. Stdout: {result.Stdout}. Stderr: {result.Stderr}");

    private sealed record CliResult(int ExitCode, string Stdout, string Stderr);
}
