using System.Diagnostics;

namespace Cerneala.Tests.UI.Hosting;

public sealed class SdlProcessSmokeTests
{
    private static readonly TimeSpan SmokeTimeout = TimeSpan.FromSeconds(60);

    [Fact]
    public async Task TwoWindowSmokeCompletesInIsolatedProcess()
    {
        if (!OperatingSystem.IsWindows())
        {
            return;
        }

        string configuration = new DirectoryInfo(AppContext.BaseDirectory).Parent?.Name ?? "Debug";
        string executable = Path.Combine(
            FindRepositoryRoot(),
            "tests",
            "Cerneala.SdlGpuSmoke",
            "bin",
            configuration,
            "net8.0",
            "Cerneala.SdlGpuSmoke.exe");
        Assert.True(File.Exists(executable), $"Smoke executable was not built: {executable}");

        using Process process = new()
        {
            StartInfo = new ProcessStartInfo(executable)
            {
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            }
        };
        process.StartInfo.ArgumentList.Add("--mode");
        process.StartInfo.ArgumentList.Add("multi-window");
        process.StartInfo.ArgumentList.Add("--artifacts");
        process.StartInfo.ArgumentList.Add(Path.Combine(FindRepositoryRoot(), "artifacts", "sdl-process-smoke"));
        process.Start();
        Task<string> output = process.StandardOutput.ReadToEndAsync();
        Task<string> error = process.StandardError.ReadToEndAsync();

        using CancellationTokenSource timeout = new(SmokeTimeout);
        try
        {
            await process.WaitForExitAsync(timeout.Token);
        }
        catch (OperationCanceledException)
        {
            process.Kill(entireProcessTree: true);
            await process.WaitForExitAsync(CancellationToken.None);
            throw new TimeoutException(
                $"The isolated SDL smoke process exceeded {SmokeTimeout.TotalSeconds} seconds. " +
                $"stdout: {await output} stderr: {await error}");
        }

        Assert.True(
            process.ExitCode == 0,
            $"SDL smoke failed with exit code {process.ExitCode}. stdout: {await output} stderr: {await error}");
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

        throw new DirectoryNotFoundException("Could not find repository root.");
    }
}
