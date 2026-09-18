using System.Diagnostics;
using Cerneala.Scene2D.Packages;

namespace Cerneala.Tests.Scene2DPackages;

public sealed class PackageCompilerTests
{
    [Theory]
    [InlineData("tiled", "tiled-finite.tmj")]
    [InlineData("ldtk", "ldtk-separate.ldtk")]
    public async Task RealCompilerCreatesReadablePackageAndRefusesToOverwriteIt(string format, string file)
    {
        string root = RepoRoot();
        string fixtures = Path.Combine(root, "tests", "Fixtures", "Scene2DImport");
        string output = Path.Combine(Path.GetTempPath(), "CernealaPackageCompilerTests-" + Guid.NewGuid().ToString("N"));
        try
        {
            var first = await Compile(format, Path.Combine(fixtures, file), fixtures, output);
            Assert.True(first.ExitCode == 0, first.Output + first.Error);
            Assert.Contains("Prepared", first.Output);
            using (Scene2DPackage package = await Scene2DPackage.OpenAsync(output))
            {
                Assert.NotEmpty(package.Levels);
                Assert.NotEmpty(package.Assets);
            }
            byte[] catalog = await File.ReadAllBytesAsync(Path.Combine(output, PackageFiles.CatalogName));
            var second = await Compile(format, Path.Combine(fixtures, file), fixtures, output);
            Assert.Equal(1, second.ExitCode);
            Assert.Contains("never overwrites", second.Error);
            Assert.Equal(catalog, await File.ReadAllBytesAsync(Path.Combine(output, PackageFiles.CatalogName)));
        }
        finally
        {
            string resolved = Path.GetFullPath(output);
            Assert.Equal(Path.GetFullPath(Path.GetTempPath()).TrimEnd(Path.DirectorySeparatorChar), Path.GetDirectoryName(resolved));
            Assert.StartsWith("CernealaPackageCompilerTests-", Path.GetFileName(resolved), StringComparison.Ordinal);
            if (Directory.Exists(resolved)) { Directory.Delete(resolved, recursive: true); }
        }
    }

    [Fact]
    public async Task ArgumentAndImportFailuresReturnErrorsWithoutPublishing()
    {
        var arguments = await Compile("unsupported");
        Assert.Equal(2, arguments.ExitCode);
        Assert.Contains("Usage:", arguments.Error);
        string fixtures = Path.Combine(RepoRoot(), "tests", "Fixtures", "Scene2DImport");
        string output = Path.Combine(Path.GetTempPath(), "CernealaPackageCompilerTests-" + Guid.NewGuid().ToString("N"));
        var invalid = await Compile("tiled", Path.Combine(fixtures, "invalid", "unknown-field.tmj"), fixtures, output);
        Assert.Equal(1, invalid.ExitCode);
        Assert.Contains("SCN2D004", invalid.Error);
        Assert.False(Directory.Exists(output));
    }

    private static async Task<(int ExitCode, string Output, string Error)> Compile(params string[] arguments)
    {
#if DEBUG
        const string configuration = "Debug";
#else
        const string configuration = "Release";
#endif
        string compiler = Path.Combine(RepoRoot(), "Tools", "Cerneala.Scene2D.PackageCompiler", "bin", configuration, "net8.0", "Cerneala.Scene2D.PackageCompiler.dll");
        Assert.True(File.Exists(compiler), "The project reference must build the actual compiler.");
        ProcessStartInfo start = new("dotnet") { UseShellExecute = false, CreateNoWindow = true, RedirectStandardOutput = true, RedirectStandardError = true };
        start.ArgumentList.Add(compiler);
        foreach (string argument in arguments) { start.ArgumentList.Add(argument); }
        using Process process = Process.Start(start) ?? throw new InvalidOperationException("Compiler process did not start.");
        Task<string> output = process.StandardOutput.ReadToEndAsync(), error = process.StandardError.ReadToEndAsync();
        using CancellationTokenSource timeout = new(TimeSpan.FromSeconds(30));
        try { await process.WaitForExitAsync(timeout.Token); }
        catch
        {
            if (!process.HasExited) { process.Kill(entireProcessTree: true); }
            await process.WaitForExitAsync();
            throw;
        }
        return (process.ExitCode, await output, await error);
    }

    private static string RepoRoot()
    {
        DirectoryInfo? directory = new(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "Cerneala.slnx"))) { directory = directory.Parent; }
        return directory?.FullName ?? throw new InvalidOperationException("Repository root not found.");
    }
}
