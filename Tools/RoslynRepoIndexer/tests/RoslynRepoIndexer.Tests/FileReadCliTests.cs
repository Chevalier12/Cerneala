using System.Diagnostics;
using System.Text.Json;

namespace RoslynRepoIndexer.Tests;

public sealed class FileReadCliTests
{
    private static readonly SemaphoreSlim BuildLock = new(1, 1);
    private static string? cliDllPath;

    [Fact]
    public async Task Read_returns_full_csharp_file_with_json_metadata()
    {
        using var repo = TestRepo.Create();
        Directory.CreateDirectory(Path.Combine(repo.Root, ".git"));
        Directory.CreateDirectory(Path.Combine(repo.Root, "src"));
        var file = Path.Combine(repo.Root, "src", "Foo.cs");
        await File.WriteAllTextAsync(file, "namespace Demo;\r\npublic sealed class Foo\r\n{\r\n}\r\n");

        var result = await RunCliAsync(new[] { "read", "src/Foo.cs", "--json" }, repo.Root);

        Assert.Equal(0, result.ExitCode);
        Assert.True(string.IsNullOrWhiteSpace(result.Stderr));
        using var json = JsonDocument.Parse(result.Stdout);
        Assert.True(json.RootElement.GetProperty("success").GetBoolean());
        Assert.Equal("read", json.RootElement.GetProperty("command").GetString());
        Assert.Equal("src/Foo.cs", json.RootElement.GetProperty("filePath").GetString());
        Assert.Equal("csharp", json.RootElement.GetProperty("language").GetString());
        var data = json.RootElement.GetProperty("data");
        Assert.Equal("src/Foo.cs", data.GetProperty("filePath").GetString());
        Assert.Equal(repo.Root, data.GetProperty("repoRoot").GetString());
        Assert.Equal("csharp", data.GetProperty("language").GetString());
        Assert.Equal(4, data.GetProperty("lineCount").GetInt32());
        Assert.True(data.GetProperty("sizeBytes").GetInt64() > 0);
        Assert.StartsWith("sha256:", data.GetProperty("contentHash").GetString(), StringComparison.Ordinal);
        Assert.Equal("namespace Demo;\npublic sealed class Foo\n{\n}\n", data.GetProperty("content").GetString());
        Assert.False(data.GetProperty("isIndexed").GetBoolean());
        Assert.True(data.TryGetProperty("lastModifiedUtc", out _));
    }

    [Fact]
    public async Task Read_returns_full_non_csharp_text_file_in_human_output()
    {
        using var repo = TestRepo.Create();
        Directory.CreateDirectory(Path.Combine(repo.Root, ".git"));
        await File.WriteAllTextAsync(Path.Combine(repo.Root, "README.md"), "title\r\nbody\r\n");

        var result = await RunCliAsync(new[] { "read", "README.md" }, repo.Root);

        Assert.Equal(0, result.ExitCode);
        Assert.True(string.IsNullOrWhiteSpace(result.Stderr));
        Assert.Equal("title\nbody\n", result.Stdout.ReplaceLineEndings("\n"));
    }

    [Theory]
    [InlineData("missing.txt", "does not exist")]
    [InlineData("docs", "directory")]
    [InlineData("../outside.txt", "outside")]
    public async Task Read_rejects_invalid_paths(string path, string expectedMessage)
    {
        using var repo = TestRepo.Create();
        Directory.CreateDirectory(Path.Combine(repo.Root, ".git"));
        Directory.CreateDirectory(Path.Combine(repo.Root, "docs"));
        await File.WriteAllTextAsync(Path.Combine(Path.GetDirectoryName(repo.Root)!, "outside.txt"), "outside");

        var result = await RunCliAsync(new[] { "read", path, "--json" }, repo.Root);

        Assert.Equal(2, result.ExitCode);
        using var json = JsonDocument.Parse(result.Stdout);
        Assert.False(json.RootElement.GetProperty("success").GetBoolean());
        Assert.Contains(expectedMessage, json.RootElement.GetProperty("errors")[0].GetString(), StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Read_rejects_absolute_paths_outside_repo_root()
    {
        using var repo = TestRepo.Create();
        Directory.CreateDirectory(Path.Combine(repo.Root, ".git"));
        var outside = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N") + ".txt");
        await File.WriteAllTextAsync(outside, "outside");

        var result = await RunCliAsync(new[] { "read", outside, "--json" }, repo.Root);

        Assert.Equal(2, result.ExitCode);
        using var json = JsonDocument.Parse(result.Stdout);
        Assert.Contains("outside", json.RootElement.GetProperty("errors")[0].GetString(), StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Read_rejects_binary_files_and_files_over_configured_max_size()
    {
        using var repo = TestRepo.Create();
        Directory.CreateDirectory(Path.Combine(repo.Root, ".git"));
        await File.WriteAllBytesAsync(Path.Combine(repo.Root, "image.bin"), new byte[] { 1, 0, 2 });
        await File.WriteAllTextAsync(Path.Combine(repo.Root, "large.txt"), "abcdef");

        var binary = await RunCliAsync(new[] { "read", "image.bin", "--json" }, repo.Root);
        var large = await RunCliAsync(new[] { "read", "large.txt", "--max-text-file-bytes", "4", "--json" }, repo.Root);

        Assert.Equal(2, binary.ExitCode);
        Assert.Equal(2, large.ExitCode);
        using var binaryJson = JsonDocument.Parse(binary.Stdout);
        using var largeJson = JsonDocument.Parse(large.Stdout);
        Assert.Contains("binary", binaryJson.RootElement.GetProperty("errors")[0].GetString(), StringComparison.OrdinalIgnoreCase);
        Assert.Contains("exceeds", largeJson.RootElement.GetProperty("errors")[0].GetString(), StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task PRead_range_returns_slice_with_json_metadata_and_clamps_end_line()
    {
        using var repo = TestRepo.Create();
        Directory.CreateDirectory(Path.Combine(repo.Root, ".git"));
        await File.WriteAllTextAsync(Path.Combine(repo.Root, "Foo.cs"), "one\ntwo\nthree\nfour\n");

        var result = await RunCliAsync(new[] { "pread", "Foo.cs", "--range", "2:99", "--json" }, repo.Root);

        Assert.Equal(0, result.ExitCode);
        using var json = JsonDocument.Parse(result.Stdout);
        Assert.Equal("Foo.cs", json.RootElement.GetProperty("filePath").GetString());
        Assert.Equal("range", json.RootElement.GetProperty("selectionMode").GetString());
        Assert.Equal(2, json.RootElement.GetProperty("startLine").GetInt32());
        Assert.Equal(4, json.RootElement.GetProperty("endLine").GetInt32());
        var data = json.RootElement.GetProperty("data");
        Assert.Equal("range", data.GetProperty("selectionMode").GetString());
        Assert.Equal(2, data.GetProperty("startLine").GetInt32());
        Assert.Equal(4, data.GetProperty("endLine").GetInt32());
        Assert.Equal(3, data.GetProperty("selectedLineCount").GetInt32());
        Assert.Equal("two\nthree\nfour", data.GetProperty("content").GetString());
        Assert.Equal("csharp", data.GetProperty("language").GetString());
    }

    [Fact]
    public async Task PRead_around_uses_default_and_custom_context()
    {
        using var repo = TestRepo.Create();
        Directory.CreateDirectory(Path.Combine(repo.Root, ".git"));
        await File.WriteAllTextAsync(Path.Combine(repo.Root, "notes.txt"), string.Join('\n', Enumerable.Range(1, 100).Select(i => $"line {i}")));

        var defaultContext = await RunCliAsync(new[] { "pread", "notes.txt", "--around", "50", "--json" }, repo.Root);
        var customContext = await RunCliAsync(new[] { "pread", "notes.txt", "--around", "50", "--context", "2", "--json" }, repo.Root);

        Assert.Equal(0, defaultContext.ExitCode);
        Assert.Equal(0, customContext.ExitCode);
        using var defaultJson = JsonDocument.Parse(defaultContext.Stdout);
        using var customJson = JsonDocument.Parse(customContext.Stdout);
        var defaultData = defaultJson.RootElement.GetProperty("data");
        var customData = customJson.RootElement.GetProperty("data");
        Assert.Equal(40, defaultData.GetProperty("context").GetInt32());
        Assert.Equal(10, defaultData.GetProperty("startLine").GetInt32());
        Assert.Equal(90, defaultData.GetProperty("endLine").GetInt32());
        Assert.Equal(2, customData.GetProperty("context").GetInt32());
        Assert.Equal(48, customData.GetProperty("startLine").GetInt32());
        Assert.Equal(52, customData.GetProperty("endLine").GetInt32());
        Assert.Equal("line 48\nline 49\nline 50\nline 51\nline 52", customData.GetProperty("content").GetString());
    }

    [Fact]
    public async Task PRead_around_clamps_to_file_boundaries()
    {
        using var repo = TestRepo.Create();
        Directory.CreateDirectory(Path.Combine(repo.Root, ".git"));
        await File.WriteAllTextAsync(Path.Combine(repo.Root, "notes.txt"), "one\ntwo\nthree");

        var result = await RunCliAsync(new[] { "pread", "notes.txt", "--around", "1", "--context", "5", "--json" }, repo.Root);

        Assert.Equal(0, result.ExitCode);
        using var json = JsonDocument.Parse(result.Stdout);
        var data = json.RootElement.GetProperty("data");
        Assert.Equal("around", data.GetProperty("selectionMode").GetString());
        Assert.Equal(1, data.GetProperty("targetLine").GetInt32());
        Assert.Equal(1, data.GetProperty("startLine").GetInt32());
        Assert.Equal(3, data.GetProperty("endLine").GetInt32());
        Assert.Equal(3, data.GetProperty("selectedLineCount").GetInt32());
    }

    [Theory]
    [InlineData(new[] { "pread", "Foo.cs", "--json" }, "either --range or --around")]
    [InlineData(new[] { "pread", "Foo.cs", "--range", "1:1", "--around", "1", "--json" }, "not both")]
    [InlineData(new[] { "pread", "Foo.cs", "--range", "0:1", "--json" }, "1-based")]
    [InlineData(new[] { "pread", "Foo.cs", "--range", "3:2", "--json" }, "greater than endLine")]
    [InlineData(new[] { "pread", "Foo.cs", "--range", "9:10", "--json" }, "greater than the file line count")]
    [InlineData(new[] { "pread", "Foo.cs", "--around", "0", "--json" }, "1-based")]
    public async Task PRead_rejects_invalid_line_selection(string[] args, string expectedMessage)
    {
        using var repo = TestRepo.Create();
        Directory.CreateDirectory(Path.Combine(repo.Root, ".git"));
        await File.WriteAllTextAsync(Path.Combine(repo.Root, "Foo.cs"), "one\ntwo\nthree");

        var result = await RunCliAsync(args, repo.Root);

        Assert.Equal(2, result.ExitCode);
        using var json = JsonDocument.Parse(result.Stdout);
        Assert.Contains(expectedMessage, json.RootElement.GetProperty("errors")[0].GetString(), StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task PRead_reuses_file_validation_errors()
    {
        using var repo = TestRepo.Create();
        Directory.CreateDirectory(Path.Combine(repo.Root, ".git"));
        Directory.CreateDirectory(Path.Combine(repo.Root, "docs"));
        await File.WriteAllBytesAsync(Path.Combine(repo.Root, "blob.bin"), new byte[] { 1, 0, 2 });
        await File.WriteAllTextAsync(Path.Combine(repo.Root, "large.txt"), "abcdef");
        var absoluteOutside = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N") + ".txt");
        await File.WriteAllTextAsync(absoluteOutside, "outside");

        var missing = await RunCliAsync(new[] { "pread", "missing.txt", "--range", "1:1", "--json" }, repo.Root);
        var directory = await RunCliAsync(new[] { "pread", "docs", "--range", "1:1", "--json" }, repo.Root);
        var traversal = await RunCliAsync(new[] { "pread", "../outside.txt", "--range", "1:1", "--json" }, repo.Root);
        var outside = await RunCliAsync(new[] { "pread", absoluteOutside, "--range", "1:1", "--json" }, repo.Root);
        var binary = await RunCliAsync(new[] { "pread", "blob.bin", "--range", "1:1", "--json" }, repo.Root);
        var large = await RunCliAsync(new[] { "pread", "large.txt", "--range", "1:1", "--max-text-file-bytes", "4", "--json" }, repo.Root);

        Assert.Equal(2, missing.ExitCode);
        Assert.Equal(2, directory.ExitCode);
        Assert.Equal(2, traversal.ExitCode);
        Assert.Equal(2, outside.ExitCode);
        Assert.Equal(2, binary.ExitCode);
        Assert.Equal(2, large.ExitCode);
        Assert.Contains("does not exist", missing.Stdout, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("directory", directory.Stdout, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("outside", traversal.Stdout, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("outside", outside.Stdout, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("binary", binary.Stdout, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("exceeds", large.Stdout, StringComparison.OrdinalIgnoreCase);
    }

    private static async Task<CliResult> RunCliAsync(string[] args, string workingDirectory)
    {
        var cliDll = await GetCliDllPathAsync();
        var psi = new ProcessStartInfo("dotnet")
        {
            WorkingDirectory = workingDirectory,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
        };
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
            var output = Path.Combine(Path.GetTempPath(), "RoslynRepoIndexer.Cli.FileReadTests", Guid.NewGuid().ToString("N"));
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
