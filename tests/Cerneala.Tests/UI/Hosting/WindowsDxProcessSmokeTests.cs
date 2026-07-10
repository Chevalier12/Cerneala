using System.Diagnostics;

namespace Cerneala.Tests.UI.Hosting;

public sealed class WindowsDxProcessSmokeTests
{
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
            "Cerneala.WindowsDxSmoke",
            "bin",
            configuration,
            "net8.0-windows",
            "Cerneala.WindowsDxSmoke.exe");
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
        process.Start();
        Task<string> output = process.StandardOutput.ReadToEndAsync();
        Task<string> error = process.StandardError.ReadToEndAsync();

        using CancellationTokenSource timeout = new(TimeSpan.FromSeconds(20));
        try
        {
            await process.WaitForExitAsync(timeout.Token);
        }
        catch (OperationCanceledException)
        {
            process.Kill(entireProcessTree: true);
            throw new TimeoutException("The isolated WindowsDX smoke process exceeded 20 seconds.");
        }

        Assert.True(
            process.ExitCode == 0,
            $"WindowsDX smoke failed with exit code {process.ExitCode}. stdout: {await output} stderr: {await error}");
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
