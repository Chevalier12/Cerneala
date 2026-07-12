using System.Diagnostics;
using System.Text.Json;
using RoslynRepoIndexer.Core;

namespace RoslynRepoIndexer.Tests;

public sealed class CliBehaviorTests
{
    private static readonly SemaphoreSlim BuildLock = new(1, 1);
    private static string? cliDllPath;

    [Fact]
    public async Task Help_and_version_return_success()
    {
        var help = await RunCliAsync("--help");
        var version = await RunCliAsync("--version");

        Assert.Equal(0, help.ExitCode);
        Assert.Contains("ri index", help.Stdout, StringComparison.Ordinal);
        Assert.DoesNotContain("ri suggest", help.Stdout, StringComparison.OrdinalIgnoreCase);
        Assert.Equal(0, version.ExitCode);
        Assert.Contains("0.1.0", version.Stdout, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Index_help_returns_success()
    {
        var result = await RunCliAsync("index", "--help");

        Assert.Equal(0, result.ExitCode);
        Assert.Contains("ri index", result.Stdout, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Unknown_command_returns_exit_code_2()
    {
        var result = await RunCliAsync("nope");

        Assert.Equal(2, result.ExitCode);
        Assert.Contains("Unknown command", result.Stderr, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Removed_suggest_command_returns_unknown_command()
    {
        var result = await RunCliAsync("suggest", "anything");

        Assert.Equal(2, result.ExitCode);
        Assert.Contains("Unknown command", result.Stderr, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Search_without_query_returns_exit_code_2()
    {
        var result = await RunCliAsync("search");

        Assert.Equal(2, result.ExitCode);
        Assert.Contains("Missing search query", result.Stderr, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Search_without_index_returns_exit_code_3_and_json_contract()
    {
        using var repo = TestRepo.Create();
        File.WriteAllText(Path.Combine(repo.Root, "Repo.sln"), string.Empty);

        var result = await RunCliAsync(new[] { "search", "CustomerService", "--json" }, repo.Root);

        Assert.Equal(3, result.ExitCode);
        using var json = JsonDocument.Parse(result.Stdout);
        Assert.False(json.RootElement.GetProperty("success").GetBoolean());
        Assert.Equal(3, json.RootElement.GetProperty("exitCode").GetInt32());
    }

    [Fact]
    public async Task Unknown_option_returns_exit_code_2_before_command_execution()
    {
        using var repo = TestRepo.Create();
        File.WriteAllText(Path.Combine(repo.Root, "Repo.sln"), string.Empty);

        var result = await RunCliAsync(new[] { "search", "CustomerService", "--bogus", "--json" }, repo.Root);

        Assert.Equal(2, result.ExitCode);
        using var json = JsonDocument.Parse(result.Stdout);
        Assert.False(json.RootElement.GetProperty("success").GetBoolean());
        Assert.Equal(2, json.RootElement.GetProperty("exitCode").GetInt32());
        Assert.Contains("Unknown option", json.RootElement.GetProperty("errors")[0].GetString(), StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Status_and_clean_work_without_roslyn_workspace_loading()
    {
        using var repo = TestRepo.Create();
        File.WriteAllText(Path.Combine(repo.Root, "Repo.sln"), string.Empty);

        var missing = await RunCliAsync(new[] { "status", "--json" }, repo.Root);
        Directory.CreateDirectory(Path.Combine(repo.Root, ".roslyn-index", "v1"));
        File.WriteAllText(Path.Combine(repo.Root, ".roslyn-index", "v1", "manifest.json"), "{}");
        var clean = await RunCliAsync(new[] { "clean", "--yes" }, repo.Root);

        Assert.Equal(0, missing.ExitCode);
        Assert.Contains("missing", missing.Stdout, StringComparison.OrdinalIgnoreCase);
        Assert.Equal(0, clean.ExitCode);
        Assert.False(Directory.Exists(Path.Combine(repo.Root, ".roslyn-index")));
    }

    [Fact]
    public async Task Clean_yes_deletes_index_and_reports_json_contract()
    {
        using var repo = TestRepo.Create();
        File.WriteAllText(Path.Combine(repo.Root, "Repo.sln"), string.Empty);
        Directory.CreateDirectory(Path.Combine(repo.Root, ".roslyn-index", "v1"));
        File.WriteAllText(Path.Combine(repo.Root, ".roslyn-index", "v1", "manifest.json"), "{}");

        var result = await RunCliAsync(new[] { "clean", "--yes", "--json" }, repo.Root);

        Assert.Equal(0, result.ExitCode);
        Assert.True(string.IsNullOrWhiteSpace(result.Stderr));
        Assert.False(Directory.Exists(Path.Combine(repo.Root, ".roslyn-index")));
        using var json = JsonDocument.Parse(result.Stdout);
        Assert.True(json.RootElement.GetProperty("success").GetBoolean());
        Assert.Equal(0, json.RootElement.GetProperty("exitCode").GetInt32());
        Assert.True(json.RootElement.GetProperty("data").GetProperty("cleaned").GetBoolean());
        Assert.Equal(repo.Root, json.RootElement.GetProperty("data").GetProperty("repoRoot").GetString());
    }

    [Fact]
    public async Task Status_json_includes_stable_index_state_contract()
    {
        using var repo = TestRepo.Create();
        File.WriteAllText(Path.Combine(repo.Root, "Repo.sln"), string.Empty);

        var missing = await RunCliAsync(new[] { "status", "--json" }, repo.Root);

        Assert.Equal(0, missing.ExitCode);
        using var json = JsonDocument.Parse(missing.Stdout);
        Assert.Equal("missing", json.RootElement.GetProperty("data").GetProperty("indexState").GetString());
    }

    [Fact]
    public async Task Status_before_and_after_index_reports_missing_and_counters()
    {
        using var repo = TestRepo.Create();
        await CreateMinimalProjectAsync(repo.Root);

        var before = await RunCliAsync(new[] { "status", "--json" }, repo.Root);
        var index = await RunCliAsync(new[] { "index", ".", "--json" }, repo.Root);
        var after = await RunCliAsync(new[] { "status", "--json" }, repo.Root);

        Assert.Equal(0, before.ExitCode);
        Assert.Equal(0, index.ExitCode);
        Assert.Equal(0, after.ExitCode);

        using var beforeJson = JsonDocument.Parse(before.Stdout);
        Assert.Equal("missing", beforeJson.RootElement.GetProperty("data").GetProperty("indexState").GetString());

        using var afterJson = JsonDocument.Parse(after.Stdout);
        var status = afterJson.RootElement.GetProperty("data");
        Assert.Equal("valid", status.GetProperty("indexState").GetString());
        Assert.True(status.GetProperty("documents").GetInt32() > 0);
        Assert.True(status.GetProperty("symbols").GetInt32() > 0);
        Assert.True(status.GetProperty("references").GetInt32() >= 0);
        Assert.True(status.GetProperty("tokens").GetInt32() > 0);
    }

    [Fact]
    public async Task Status_reports_corrupt_when_required_index_files_are_missing_or_invalid()
    {
        using var repo = TestRepo.Create();
        await CreateMinimalProjectAsync(repo.Root);

        var index = await RunCliAsync(new[] { "index", ".", "--json" }, repo.Root);
        File.Delete(Path.Combine(IndexStore.GetVersionDirectory(repo.Root), "segments.json"));
        var missingFile = await RunCliAsync(new[] { "status", "--json" }, repo.Root);

        Assert.Equal(0, index.ExitCode);
        Assert.Equal(0, missingFile.ExitCode);
        using (var json = JsonDocument.Parse(missingFile.Stdout))
        {
            Assert.Equal("corrupt", json.RootElement.GetProperty("data").GetProperty("indexState").GetString());
            Assert.Contains("segments.json", json.RootElement.GetProperty("data").GetProperty("warnings")[0].GetString(), StringComparison.OrdinalIgnoreCase);
        }

        var rebuilt = await RunCliAsync(new[] { "index", ".", "--json" }, repo.Root);
        Assert.Equal(0, rebuilt.ExitCode);
        var corruptSegment = Directory.GetFiles(Path.Combine(IndexStore.GetIndexDirectory(repo.Root), "segments"), "*.bin").First();
        await File.WriteAllTextAsync(corruptSegment, "not valid binary");
        var invalidJson = await RunCliAsync(new[] { "status", "--json" }, repo.Root);

        Assert.Equal(0, invalidJson.ExitCode);
        using var invalidJsonDoc = JsonDocument.Parse(invalidJson.Stdout);
        Assert.Equal("corrupt", invalidJsonDoc.RootElement.GetProperty("data").GetProperty("indexState").GetString());
        Assert.Contains("Segment", invalidJsonDoc.RootElement.GetProperty("data").GetProperty("warnings")[0].GetString(), StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Search_returns_exit_code_3_when_index_files_are_deleted_or_corrupt_during_read()
    {
        using var repo = TestRepo.Create();
        await CreateMinimalProjectAsync(repo.Root);

        var index = await RunCliAsync(new[] { "index", ".", "--json" }, repo.Root);
        File.Delete(Path.Combine(IndexStore.GetVersionDirectory(repo.Root), "segments.json"));
        var missingFile = await RunCliAsync(new[] { "search", "CustomerService", "--json" }, repo.Root);

        Assert.Equal(0, index.ExitCode);
        Assert.Equal(3, missingFile.ExitCode);
        using (var json = JsonDocument.Parse(missingFile.Stdout))
        {
            Assert.False(json.RootElement.GetProperty("success").GetBoolean());
            Assert.Equal(3, json.RootElement.GetProperty("exitCode").GetInt32());
            Assert.Contains("Index is corrupt", json.RootElement.GetProperty("errors")[0].GetString(), StringComparison.OrdinalIgnoreCase);
            Assert.Contains("ri index --force", json.RootElement.GetProperty("errors")[0].GetString(), StringComparison.OrdinalIgnoreCase);
        }

        await RunCliAsync(new[] { "index", ".", "--json" }, repo.Root);
        var corruptReferenceSegment = Directory.GetFiles(Path.Combine(IndexStore.GetIndexDirectory(repo.Root), "segments"), "*.bin").First();
        await File.WriteAllTextAsync(corruptReferenceSegment, "not valid binary");
        var corruptFile = await RunCliAsync(new[] { "search", "CustomerService", "--json" }, repo.Root);

        Assert.Equal(3, corruptFile.ExitCode);
        using var corruptJson = JsonDocument.Parse(corruptFile.Stdout);
        Assert.False(corruptJson.RootElement.GetProperty("success").GetBoolean());
        Assert.Equal(3, corruptJson.RootElement.GetProperty("exitCode").GetInt32());
        Assert.Contains("Index is corrupt", corruptJson.RootElement.GetProperty("errors")[0].GetString(), StringComparison.OrdinalIgnoreCase);
        Assert.Contains("ri index --force", corruptJson.RootElement.GetProperty("errors")[0].GetString(), StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Doctor_json_returns_machine_readable_checks_with_details()
    {
        using var repo = TestRepo.Create();
        await CreateMinimalProjectAsync(repo.Root);

        var result = await RunCliAsync(new[] { "doctor", ".", "--json" }, repo.Root);

        Assert.Equal(0, result.ExitCode);
        Assert.True(string.IsNullOrWhiteSpace(result.Stderr));
        using var json = JsonDocument.Parse(result.Stdout);
        Assert.True(json.RootElement.GetProperty("success").GetBoolean());
        Assert.Equal("doctor", json.RootElement.GetProperty("command").GetString());
        var checks = json.RootElement.GetProperty("data").GetProperty("checks").EnumerateArray().ToArray();
        Assert.NotEmpty(checks);
        Assert.Contains(checks, check => check.GetProperty("name").GetString() == "repo-root");
        Assert.Contains(checks, check => check.GetProperty("name").GetString() == "workspace-inputs");
        foreach (var check in checks)
        {
            Assert.False(string.IsNullOrWhiteSpace(check.GetProperty("name").GetString()));
            Assert.False(string.IsNullOrWhiteSpace(check.GetProperty("status").GetString()));
            Assert.False(string.IsNullOrWhiteSpace(check.GetProperty("severity").GetString()));
            Assert.False(string.IsNullOrWhiteSpace(check.GetProperty("message").GetString()));
            Assert.Equal(JsonValueKind.Object, check.GetProperty("details").ValueKind);
        }
    }

    [Fact]
    public async Task Json_output_keeps_config_warnings_machine_readable_without_human_stdout_lines()
    {
        using var repo = TestRepo.Create();
        await CreateMinimalProjectAsync(repo.Root);
        await File.WriteAllTextAsync(Path.Combine(repo.Root, ".roslyn-index.json"), """
            {
              "surpriseSetting": true
            }
            """);

        var result = await RunCliAsync(new[] { "index", ".", "--json" }, repo.Root);

        Assert.Equal(0, result.ExitCode);
        Assert.True(string.IsNullOrWhiteSpace(result.Stderr));
        Assert.DoesNotContain("warning:", result.Stdout, StringComparison.OrdinalIgnoreCase);
        using var json = JsonDocument.Parse(result.Stdout);
        Assert.True(json.RootElement.GetProperty("success").GetBoolean());
        var warning = Assert.Single(json.RootElement.GetProperty("warnings").EnumerateArray());
        Assert.Contains("surpriseSetting", warning.GetString(), StringComparison.Ordinal);
    }

    [Fact]
    public async Task Non_json_index_writes_config_warnings_to_stderr_not_stdout()
    {
        using var repo = TestRepo.Create();
        await CreateMinimalProjectAsync(repo.Root);
        await File.WriteAllTextAsync(Path.Combine(repo.Root, ".roslyn-index.json"), """
            {
              "surpriseSetting": true
            }
            """);

        var result = await RunCliAsync(new[] { "index", "." }, repo.Root);

        Assert.Equal(0, result.ExitCode);
        Assert.Contains("warning:", result.Stderr, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("surpriseSetting", result.Stderr, StringComparison.Ordinal);
        Assert.DoesNotContain("warning:", result.Stdout, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Indexed ", result.Stdout, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Json_search_response_includes_common_metadata_and_results_alias()
    {
        using var repo = TestRepo.Create();
        await CreateMinimalProjectAsync(repo.Root);

        var index = await RunCliAsync(new[] { "index", ".", "--json" }, repo.Root);
        var search = await RunCliAsync(new[] { "search", "CustomerService", "--json" }, repo.Root);

        Assert.Equal(0, index.ExitCode);
        Assert.Equal(0, search.ExitCode);
        using var json = JsonDocument.Parse(search.Stdout);
        var root = json.RootElement;
        Assert.Equal("search", root.GetProperty("command").GetString());
        Assert.Equal("CustomerService", root.GetProperty("query").GetString());
        Assert.Equal(repo.Root, root.GetProperty("repoRoot").GetString());
        Assert.True(root.GetProperty("elapsedMs").GetInt64() >= 0);
        Assert.False(string.IsNullOrWhiteSpace(root.GetProperty("indexUpdatedUtc").GetString()));
        Assert.True(root.GetProperty("results").GetArrayLength() > 0);
        Assert.Equal(root.GetProperty("data")[0].GetProperty("path").GetString(), root.GetProperty("results")[0].GetProperty("path").GetString());
    }

    [Fact]
    public async Task Search_timeout_returns_partial_results_with_warning_or_exit_5_when_empty()
    {
        using var repo = TestRepo.Create();
        await CreateMinimalProjectAsync(repo.Root);

        var index = await RunCliAsync(new[] { "index", ".", "--json" }, repo.Root);
        var partial = await RunCliAsync(new[] { "search", "CustomerService", "--json", "--timeout", "0" }, repo.Root);
        var empty = await RunCliAsync(new[] { "search", "DefinitelyMissingSymbol", "--json", "--timeout", "0" }, repo.Root);

        Assert.Equal(0, index.ExitCode);
        Assert.Equal(0, partial.ExitCode);
        using var partialJson = JsonDocument.Parse(partial.Stdout);
        Assert.True(partialJson.RootElement.GetProperty("data").GetArrayLength() > 0);
        Assert.Contains("timeout", partialJson.RootElement.GetProperty("warnings")[0].GetString(), StringComparison.OrdinalIgnoreCase);
        Assert.Contains("searchLoadMs", partialJson.RootElement.GetProperty("warnings")[0].GetString(), StringComparison.Ordinal);
        Assert.Contains("searchScoreMs", partialJson.RootElement.GetProperty("warnings")[0].GetString(), StringComparison.Ordinal);

        Assert.Equal(5, empty.ExitCode);
        using var emptyJson = JsonDocument.Parse(empty.Stdout);
        Assert.False(emptyJson.RootElement.GetProperty("success").GetBoolean());
        Assert.Equal(5, emptyJson.RootElement.GetProperty("exitCode").GetInt32());
        Assert.Empty(emptyJson.RootElement.GetProperty("data").EnumerateArray());
        Assert.Contains("timeout", emptyJson.RootElement.GetProperty("warnings")[0].GetString(), StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Index_returns_exit_code_2_when_configured_workspace_input_is_missing()
    {
        using var repo = TestRepo.Create();
        await File.WriteAllTextAsync(Path.Combine(repo.Root, "Sample.csproj"), """
            <Project Sdk="Microsoft.NET.Sdk">
              <PropertyGroup>
                <TargetFramework>net8.0</TargetFramework>
              </PropertyGroup>
            </Project>
            """);
        await File.WriteAllTextAsync(Path.Combine(repo.Root, ".roslyn-index.json"), """
            { "solution": "Missing.sln" }
            """);

        var result = await RunCliAsync(new[] { "index", ".", "--json" }, repo.Root);

        Assert.Equal(2, result.ExitCode);
        using var json = JsonDocument.Parse(result.Stdout);
        Assert.False(json.RootElement.GetProperty("success").GetBoolean());
        Assert.Equal(2, json.RootElement.GetProperty("exitCode").GetInt32());
        Assert.Contains("workspace", json.RootElement.GetProperty("errors")[0].GetString(), StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Index_returns_exit_code_4_when_workspace_has_no_csharp_documents()
    {
        using var repo = TestRepo.Create();
        await File.WriteAllTextAsync(Path.Combine(repo.Root, "Empty.csproj"), """
            <Project Sdk="Microsoft.NET.Sdk">
              <PropertyGroup>
                <TargetFramework>net8.0</TargetFramework>
              </PropertyGroup>
              <ItemGroup>
                <Compile Remove="**\*.cs" />
              </ItemGroup>
            </Project>
            """);

        var result = await RunCliAsync(new[] { "index", ".", "--json" }, repo.Root);

        Assert.Equal(4, result.ExitCode);
        using var json = JsonDocument.Parse(result.Stdout);
        Assert.False(json.RootElement.GetProperty("success").GetBoolean());
        Assert.Equal(4, json.RootElement.GetProperty("exitCode").GetInt32());
        Assert.Contains("No C# documents", json.RootElement.GetProperty("errors")[0].GetString(), StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Index_continues_when_one_discovered_project_is_unloadable()
    {
        using var repo = TestRepo.Create();
        Directory.CreateDirectory(Path.Combine(repo.Root, ".git"));
        Directory.CreateDirectory(Path.Combine(repo.Root, "Good"));
        Directory.CreateDirectory(Path.Combine(repo.Root, "Bad"));
        await File.WriteAllTextAsync(Path.Combine(repo.Root, "Good", "Good.csproj"), """
            <Project Sdk="Microsoft.NET.Sdk">
              <PropertyGroup>
                <TargetFramework>net8.0</TargetFramework>
              </PropertyGroup>
            </Project>
            """);
        await File.WriteAllTextAsync(Path.Combine(repo.Root, "Good", "CustomerService.cs"), "namespace Good; public sealed class CustomerService { }");
        await File.WriteAllTextAsync(Path.Combine(repo.Root, "Bad", "Bad.csproj"), "<Project><PropertyGroup>");

        var result = await RunCliAsync(new[] { "index", ".", "--json" }, repo.Root);

        Assert.Equal(0, result.ExitCode);
        using var json = JsonDocument.Parse(result.Stdout);
        var data = json.RootElement.GetProperty("data");
        Assert.True(data.GetProperty("documents").GetInt32() > 0);
        Assert.True(data.GetProperty("warnings").GetInt32() > 0);
    }

    private static async Task CreateMinimalProjectAsync(string root)
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
            namespace Sample;

            public sealed class CustomerService
            {
                public string GetCustomerName(string id) => id;
            }
            """);
    }

    private static async Task<CliResult> RunCliAsync(params string[] args)
        => await RunCliAsync(args, TestPaths.RepositoryRoot);

    private static async Task<CliResult> RunCliAsync(string[] args, string workingDirectory)
    {
        var cliDll = await GetCliDllPathAsync();
        var psi = new ProcessStartInfo("dotnet")
        {
            WorkingDirectory = workingDirectory,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
        };
        psi.Environment["RI_DISABLE_DAEMON"] = "1";
        psi.ArgumentList.Add(cliDll);
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

    private static async Task<string> GetCliDllPathAsync()
    {
        if (cliDllPath is not null)
        {
            return cliDllPath;
        }

        await BuildLock.WaitAsync();
        try
        {
            if (cliDllPath is not null)
            {
                return cliDllPath;
            }

            var project = Path.Combine(TestPaths.RepositoryRoot, "tools", "RoslynRepoIndexer", "src", "RoslynRepoIndexer.Cli", "RoslynRepoIndexer.Cli.csproj");
            var output = Path.Combine(Path.GetTempPath(), "RoslynRepoIndexer.Cli.Tests", Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(output);

            var build = new ProcessStartInfo("dotnet")
            {
                WorkingDirectory = TestPaths.RepositoryRoot,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
            };
            build.ArgumentList.Add("publish");
            build.ArgumentList.Add(project);
            build.ArgumentList.Add("--configuration");
            build.ArgumentList.Add("Debug");
            build.ArgumentList.Add("--output");
            build.ArgumentList.Add(output);
            build.ArgumentList.Add("--no-restore");

            using var process = Process.Start(build) ?? throw new InvalidOperationException("Failed to start dotnet publish.");
            var stdout = await process.StandardOutput.ReadToEndAsync();
            var stderr = await process.StandardError.ReadToEndAsync();
            await process.WaitForExitAsync();
            if (process.ExitCode != 0)
            {
                throw new InvalidOperationException($"CLI publish failed with exit code {process.ExitCode}.{Environment.NewLine}{stdout}{stderr}");
            }

            cliDllPath = Path.Combine(output, "RoslynRepoIndexer.Cli.dll");
            return cliDllPath;
        }
        finally
        {
            BuildLock.Release();
        }
    }

    private sealed record CliResult(int ExitCode, string Stdout, string Stderr);
}
