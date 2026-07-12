using System.Diagnostics;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace RoslynRepoIndexer.Tests;

public sealed class PerformanceSmokeTests
{
    private static readonly SemaphoreSlim BuildLock = new(1, 1);
    private static string? cliDllPath;

    [Fact]
    public void Existing_index_commands_do_not_reference_roslyn_or_msbuild_runtime_services()
    {
        var program = File.ReadAllText(Path.Combine(TestPaths.RepositoryRoot, "Tools", "RoslynRepoIndexer", "src", "RoslynRepoIndexer.Cli", "Program.cs"));

        foreach (var methodName in new[] { "Search", "Goto", "Symbols", "Status" })
        {
            var body = ExtractMethodBody(program, methodName);

            Assert.DoesNotContain("MSBuild", body, StringComparison.Ordinal);
            Assert.DoesNotContain("Microsoft.CodeAnalysis", body, StringComparison.Ordinal);
            Assert.DoesNotContain("IndexBuilder", body, StringComparison.Ordinal);
            Assert.DoesNotContain("DoctorService", body, StringComparison.Ordinal);
            Assert.DoesNotContain("ExactReferenceService", body, StringComparison.Ordinal);
            Assert.DoesNotContain("FindExactAsync", body, StringComparison.Ordinal);
            Assert.DoesNotContain("FindReferencesAsync", body, StringComparison.Ordinal);
        }
    }

    [Fact]
    public void Query_core_services_do_not_reference_roslyn_or_msbuild_packages()
    {
        var search = File.ReadAllText(Path.Combine(TestPaths.RepositoryRoot, "Tools", "RoslynRepoIndexer", "src", "RoslynRepoIndexer.Core", "Search.cs"));
        var indexStore = File.ReadAllText(Path.Combine(TestPaths.RepositoryRoot, "Tools", "RoslynRepoIndexer", "src", "RoslynRepoIndexer.Core", "IndexStore.cs"));

        foreach (var source in new[] { search, indexStore })
        {
            Assert.DoesNotContain("Microsoft.CodeAnalysis", source, StringComparison.Ordinal);
            Assert.DoesNotContain("Microsoft.Build", source, StringComparison.Ordinal);
            Assert.DoesNotContain("MSBuildWorkspace", source, StringComparison.Ordinal);
            Assert.DoesNotContain("MSBuildRegistration", source, StringComparison.Ordinal);
            Assert.DoesNotContain("SymbolFinder", source, StringComparison.Ordinal);
        }
    }

    [Fact]
    public async Task Json_commands_report_elapsed_ms_without_time_thresholds()
    {
        using var repo = TestRepo.Create();
        await CreateProjectAsync(repo.Root);

        var index = await RunCliAsync(new[] { "index", ".", "--json" }, repo.Root);
        AssertElapsedMs(index);

        var commands = new[]
        {
            new[] { "search", "CustomerService", "--json" },
            new[] { "refs", "CustomerService", "--json" },
            new[] { "goto", "CustomerService", "--json" },
            new[] { "symbols", "--contains", "CustomerService", "--json" },
            new[] { "doctor", "--json" },
            new[] { "status", "--json" },
            new[] { "clean", "--yes", "--json" }
        };

        foreach (var command in commands)
        {
            var result = await RunCliAsync(command, repo.Root);

            AssertElapsedMs(result);
        }
    }

    [Fact]
    public async Task Large_repo_smoke_indexes_200_files_searches_under_relaxed_threshold_and_keeps_body_only_dirty_below_10_percent()
    {
        using var repo = TestRepo.Create();
        await CreateLargeProjectAsync(repo.Root, fileCount: 200);

        var fullIndex = await RunCliAsync(new[] { "index", ".", "--json" }, repo.Root);

        Assert.Equal(0, fullIndex.ExitCode);
        using var fullIndexJson = JsonDocument.Parse(fullIndex.Stdout);
        var fullIndexData = fullIndexJson.RootElement.GetProperty("data");
        var documentCount = fullIndexData.GetProperty("documents").GetInt32();
        var dirtyDocumentCount = fullIndexData.GetProperty("dirtyDocuments").GetInt32();

        Assert.True(documentCount >= 200, $"Expected at least 200 indexed documents, got {documentCount}.");
        Assert.Equal(documentCount, dirtyDocumentCount);
        Assert.True(fullIndexData.GetProperty("symbols").GetInt32() >= 200);
        Assert.True(fullIndexData.GetProperty("tokens").GetInt32() > documentCount);
        Assert.Equal(0, fullIndexData.GetProperty("deletedDocuments").GetInt32());

        var searchWatch = Stopwatch.StartNew();
        var search = await RunCliAsync(new[] { "search", "GeneratedService199", "--json", "--limit", "5" }, repo.Root);
        searchWatch.Stop();

        Assert.Equal(0, search.ExitCode);
        Assert.True(searchWatch.Elapsed < TimeSpan.FromSeconds(20), $"Search smoke took {searchWatch.Elapsed.TotalSeconds:0.00}s, above the relaxed 20s threshold.");
        using var searchJson = JsonDocument.Parse(search.Stdout);
        Assert.Contains(searchJson.RootElement.GetProperty("data").EnumerateArray(), result =>
            result.GetProperty("path").GetString() == "Services/GeneratedService199.cs");

        await File.WriteAllTextAsync(Path.Combine(repo.Root, "Services", "GeneratedService042.cs"), LargeServiceSource(42, """
                return input + "-body-only-change";
            """));

        var incremental = await RunCliAsync(new[] { "index", ".", "--json" }, repo.Root);

        Assert.Equal(0, incremental.ExitCode);
        using var incrementalJson = JsonDocument.Parse(incremental.Stdout);
        var incrementalData = incrementalJson.RootElement.GetProperty("data");
        var incrementalDocuments = incrementalData.GetProperty("documents").GetInt32();
        var incrementalDirtyDocuments = incrementalData.GetProperty("dirtyDocuments").GetInt32();

        Assert.False(incrementalData.GetProperty("fullRebuild").GetBoolean());
        Assert.True(incrementalData.GetProperty("incremental").GetBoolean());
        Assert.Equal(documentCount, incrementalDocuments);
        Assert.True(incrementalDirtyDocuments > 0);
        Assert.True(incrementalDirtyDocuments * 10 < incrementalDocuments, $"Expected body-only dirty documents below 10%, got {incrementalDirtyDocuments}/{incrementalDocuments}.");
    }

    private static void AssertElapsedMs(CliResult result)
    {
        Assert.Equal(0, result.ExitCode);
        using var json = JsonDocument.Parse(result.Stdout);
        var elapsedMs = json.RootElement.GetProperty("elapsedMs");
        Assert.Equal(JsonValueKind.Number, elapsedMs.ValueKind);
        Assert.True(elapsedMs.GetInt64() >= 0);
    }

    private static string ExtractMethodBody(string source, string methodName)
    {
        var match = Regex.Match(source, $@"private static (?:async Task<int>|int) {methodName}\([^)]*\)\s*\{{", RegexOptions.CultureInvariant);
        Assert.True(match.Success, $"Method {methodName} was not found.");

        var depth = 1;
        var index = match.Index + match.Length;
        for (; index < source.Length && depth > 0; index++)
        {
            depth += source[index] switch
            {
                '{' => 1,
                '}' => -1,
                _ => 0
            };
        }

        Assert.Equal(0, depth);
        return source[match.Index..index];
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
                public string GetCustomerName() => "Ada";
            }
            """);
    }

    private static async Task CreateLargeProjectAsync(string root, int fileCount)
    {
        await File.WriteAllTextAsync(Path.Combine(root, "LargeSmoke.csproj"), """
            <Project Sdk="Microsoft.NET.Sdk">
              <PropertyGroup>
                <TargetFramework>net8.0</TargetFramework>
                <ImplicitUsings>enable</ImplicitUsings>
                <Nullable>enable</Nullable>
              </PropertyGroup>
            </Project>
            """);

        var servicesDirectory = Path.Combine(root, "Services");
        Directory.CreateDirectory(servicesDirectory);
        for (var index = 0; index < fileCount; index++)
        {
            await File.WriteAllTextAsync(
                Path.Combine(servicesDirectory, $"GeneratedService{index:000}.cs"),
                LargeServiceSource(index, """
                        return input + "-value";
                    """));
        }
    }

    private static string LargeServiceSource(int index, string methodBody)
        => $$"""
            namespace LargeSmoke.Services;

            public sealed class GeneratedService{{index:000}}
            {
                public string Execute{{index:000}}(string input)
                {
            {{methodBody}}
                }
            }
            """;

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

            var project = Path.Combine(TestPaths.RepositoryRoot, "Tools", "RoslynRepoIndexer", "src", "RoslynRepoIndexer.Cli", "RoslynRepoIndexer.Cli.csproj");
            var output = Path.Combine(Path.GetTempPath(), "RoslynRepoIndexer.PerformanceSmoke.Tests", Guid.NewGuid().ToString("N"));
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
