using System.Diagnostics;

namespace Cerneala.Tests.SdlGpu;

// Runs the real Tools/Cerneala.SdlShaderCompiler, the way VerifySdlShaderArtifacts does.
// DXIL bytes are reproducible only on Windows: the SDL_shadercross DXC build for Linux and
// macOS writes a different compiler identity into the DXIL bitcode.
public sealed class SdlShaderCompilerVerificationTests
{
    private const string DxilWindowsOnlyNotice = "DXIL is compiled and verified only on Windows";
    private static readonly string[] ShaderDirectories =
    [
        Path.Combine("Cerneala.Backends.SdlGpu", "Shaders"),
        Path.Combine("Cerneala.Backends.SdlGpu", "Gpu", "Shaders"),
        Path.Combine("Cerneala.Backends.SdlGpu", "Prism", "Shaders"),
        Path.Combine("Drawing", "Prism", "Shaders", "Hlsl")
    ];

    [Fact]
    public async Task Verify_accepts_the_committed_artifacts_and_states_where_dxil_is_checked()
    {
        string repository = FindRepositoryRoot();

        (int exitCode, string output, string error) = await RunCompilerAsync(
            repository,
            "--verify",
            "--manifest",
            ManifestPath(repository));

        Assert.True(exitCode == 0, output + Environment.NewLine + error);
        Assert.Equal(
            !OperatingSystem.IsWindows(),
            output.Contains(DxilWindowsOnlyNotice, StringComparison.Ordinal));
    }

    [Fact]
    public async Task Verify_rejects_an_edited_dxil_artifact()
    {
        string repository = FindRepositoryRoot();
        using TemporaryShaderTree tree = TemporaryShaderTree.Copy(repository);
        string dxil = Path.Combine(tree.Root, "Cerneala.Backends.SdlGpu", "Gpu", "Shaders", "Drawing.frag.dxil");
        byte[] bytes = File.ReadAllBytes(dxil);
        bytes[^1] ^= 0xFF;
        File.WriteAllBytes(dxil, bytes);

        (int exitCode, string output, string error) = await RunCompilerAsync(
            repository,
            "--verify",
            "--manifest",
            ManifestPath(tree.Root));

        Assert.True(exitCode == 1, output + Environment.NewLine + error);
        Assert.Contains("is missing or stale.", error, StringComparison.Ordinal);
        // Windows compares the DXIL bytes; other hosts compare its hash in artifacts.json.
        Assert.Contains(
            OperatingSystem.IsWindows() ? "Drawing.frag.dxil" : "artifacts.json",
            error,
            StringComparison.Ordinal);
    }

    [Fact]
    public async Task Generate_reproduces_the_committed_artifacts_only_on_windows()
    {
        string repository = FindRepositoryRoot();
        using TemporaryShaderTree tree = TemporaryShaderTree.Copy(repository);
        Dictionary<string, byte[]> before = tree.ReadAllFiles();

        (int exitCode, string output, string error) = await RunCompilerAsync(
            repository,
            "--manifest",
            ManifestPath(tree.Root));

        if (OperatingSystem.IsWindows())
        {
            Assert.True(exitCode == 0, output + Environment.NewLine + error);
        }
        else
        {
            Assert.True(exitCode == 1, output + Environment.NewLine + error);
            Assert.Contains(DxilWindowsOnlyNotice, error, StringComparison.Ordinal);
        }
        Dictionary<string, byte[]> after = tree.ReadAllFiles();
        Assert.Equal(before.Keys.Order(StringComparer.Ordinal), after.Keys.Order(StringComparer.Ordinal));
        Assert.All(before, file => Assert.True(
            file.Value.AsSpan().SequenceEqual(after[file.Key]),
            $"'{file.Key}' changed."));
    }

    private static string ManifestPath(string root) =>
        Path.Combine(root, "Cerneala.Backends.SdlGpu", "Shaders", "manifest.json");

    private static async Task<(int ExitCode, string Output, string Error)> RunCompilerAsync(
        string repository,
        params string[] arguments)
    {
        using Process process = new()
        {
            StartInfo = new ProcessStartInfo("dotnet")
            {
                WorkingDirectory = repository,
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true
            }
        };
        process.StartInfo.ArgumentList.Add("run");
        process.StartInfo.ArgumentList.Add("--project");
        process.StartInfo.ArgumentList.Add(Path.Combine(
            repository,
            "Tools",
            "Cerneala.SdlShaderCompiler",
            "Cerneala.SdlShaderCompiler.csproj"));
        process.StartInfo.ArgumentList.Add("--no-restore");
        process.StartInfo.ArgumentList.Add("--");
        foreach (string argument in arguments)
        {
            process.StartInfo.ArgumentList.Add(argument);
        }

        Assert.True(process.Start());
        Task<string> standardOutput = process.StandardOutput.ReadToEndAsync();
        Task<string> standardError = process.StandardError.ReadToEndAsync();
        try
        {
            await process.WaitForExitAsync().WaitAsync(TimeSpan.FromSeconds(180));
            return (process.ExitCode, await standardOutput, await standardError);
        }
        finally
        {
            if (!process.HasExited)
            {
                process.Kill(entireProcessTree: true);
                await process.WaitForExitAsync().WaitAsync(TimeSpan.FromSeconds(5));
            }
        }
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
        throw new DirectoryNotFoundException("Could not locate the Cerneala repository root.");
    }

    private sealed class TemporaryShaderTree : IDisposable
    {
        private TemporaryShaderTree(string root) => Root = root;

        public string Root { get; }

        public static TemporaryShaderTree Copy(string repository)
        {
            TemporaryShaderTree tree = new(Path.Combine(
                Path.GetTempPath(),
                "cerneala-sdl-shaders-" + Guid.NewGuid().ToString("N")));
            foreach (string directory in ShaderDirectories)
            {
                string source = Path.Combine(repository, directory);
                foreach (string file in Directory.EnumerateFiles(source, "*", SearchOption.AllDirectories))
                {
                    string target = Path.Combine(tree.Root, directory, Path.GetRelativePath(source, file));
                    Directory.CreateDirectory(Path.GetDirectoryName(target)!);
                    File.Copy(file, target);
                }
            }
            return tree;
        }

        public Dictionary<string, byte[]> ReadAllFiles() =>
            Directory.EnumerateFiles(Root, "*", SearchOption.AllDirectories).ToDictionary(
                file => Path.GetRelativePath(Root, file),
                File.ReadAllBytes,
                StringComparer.Ordinal);

        public void Dispose()
        {
            if (Directory.Exists(Root))
            {
                Directory.Delete(Root, recursive: true);
            }
        }
    }
}
