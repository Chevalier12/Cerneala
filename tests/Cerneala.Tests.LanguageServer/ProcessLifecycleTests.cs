using System.Diagnostics;
using Cerneala.LanguageServer.Protocol;
using StreamJsonRpc;

namespace Cerneala.Tests.LanguageServer;

public sealed class ProcessLifecycleTests
{
    [Fact]
    public async Task StdioProcessNegotiatesShutdownAndLeavesNoServerProcess()
    {
        using CancellationTokenSource timeout = new(TimeSpan.FromSeconds(60));
        string repositoryRoot = FindRepositoryRoot();
        string configuration = AppContext.BaseDirectory.Contains(
            $"{Path.DirectorySeparatorChar}Release{Path.DirectorySeparatorChar}",
            StringComparison.OrdinalIgnoreCase)
            ? "Release"
            : "Debug";
        ProcessStartInfo startInfo = new("dotnet")
        {
            WorkingDirectory = repositoryRoot,
            RedirectStandardInput = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };
        startInfo.ArgumentList.Add("run");
        startInfo.ArgumentList.Add("--no-build");
        startInfo.ArgumentList.Add("--no-launch-profile");
        startInfo.ArgumentList.Add("--configuration");
        startInfo.ArgumentList.Add(configuration);
        startInfo.ArgumentList.Add("--project");
        startInfo.ArgumentList.Add(Path.Combine(repositoryRoot, "Cerneala.LanguageServer", "Cerneala.LanguageServer.csproj"));

        using Process process = Process.Start(startInfo) ?? throw new InvalidOperationException("Could not start language server.");
        Task<string> stderr = process.StandardError.ReadToEndAsync(timeout.Token);
        using JsonRpc rpc = new(new HeaderDelimitedMessageHandler(
            process.StandardInput.BaseStream,
            process.StandardOutput.BaseStream,
            new SystemTextJsonFormatter()));
        rpc.StartListening();

        try
        {
            InitializeResult result = await rpc.InvokeWithParameterObjectAsync<InitializeResult>(
                "initialize",
                new InitializeParams
                {
                    ProcessId = Environment.ProcessId,
                    RootUri = null,
                    Capabilities = new { }
                },
                timeout.Token);
            Assert.Equal("Cerneala Language Server", result.ServerInfo.Name);

            await rpc.InvokeWithCancellationAsync<object?>("shutdown", [], timeout.Token);
            await rpc.NotifyAsync("exit");
            await process.WaitForExitAsync(timeout.Token);

            Assert.True(process.HasExited);
            Assert.Equal(0, process.ExitCode);
            Assert.Contains("lifecycle.exit", await stderr, StringComparison.Ordinal);
        }
        finally
        {
            if (!process.HasExited)
            {
                process.Kill(entireProcessTree: true);
                await process.WaitForExitAsync(CancellationToken.None);
            }
        }
    }

    [Fact]
    public async Task HostDisconnectTerminatesTheServerWithoutLeavingAProcess()
    {
        using CancellationTokenSource timeout = new(TimeSpan.FromSeconds(60));
        string repositoryRoot = FindRepositoryRoot();
        string configuration = AppContext.BaseDirectory.Contains(
            $"{Path.DirectorySeparatorChar}Release{Path.DirectorySeparatorChar}",
            StringComparison.OrdinalIgnoreCase)
            ? "Release"
            : "Debug";
        string assembly = Path.Combine(
            repositoryRoot,
            "Cerneala.LanguageServer",
            "bin",
            configuration,
            "net10.0",
            "Cerneala.LanguageServer.dll");
        ProcessStartInfo startInfo = new("dotnet")
        {
            WorkingDirectory = repositoryRoot,
            RedirectStandardInput = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };
        startInfo.ArgumentList.Add(assembly);

        using Process process = Process.Start(startInfo) ?? throw new InvalidOperationException("Could not start language server.");
        Task<string> stderr = process.StandardError.ReadToEndAsync(timeout.Token);
        process.StandardInput.Close();
        await process.WaitForExitAsync(timeout.Token);

        Assert.True(process.HasExited);
        Assert.Equal(0, process.ExitCode);
        Assert.Contains("server.disconnected", await stderr, StringComparison.Ordinal);
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
}
