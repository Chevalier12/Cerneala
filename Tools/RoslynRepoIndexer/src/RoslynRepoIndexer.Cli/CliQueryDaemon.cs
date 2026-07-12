using System.Diagnostics;
using System.IO.Pipes;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using RoslynRepoIndexer.Core;

internal sealed record CliProxyResponse(int ExitCode, string StandardOutput, string StandardError);

internal static class CliQueryDaemon
{
    private const string ServerArgument = "__query_server";
    private static readonly HashSet<string> ProxiedCommands = new(StringComparer.OrdinalIgnoreCase)
    {
        "search", "refs", "goto", "symbols", "status"
    };

    public static async Task<int?> TryHandleServerModeAsync(string[] args)
    {
        if (args.Length != 3 || !string.Equals(args[0], ServerArgument, StringComparison.Ordinal))
        {
            return null;
        }

        await RunServerAsync(args[1], args[2]).ConfigureAwait(false);
        return 0;
    }

    public static async Task<CliProxyResponse?> TryProxyAsync(string[] args)
    {
        if (string.Equals(Environment.GetEnvironmentVariable("RI_DISABLE_DAEMON"), "1", StringComparison.Ordinal)
            || args.Length == 0
            || !ProxiedCommands.Contains(args[0])
            || args.Contains("--help", StringComparer.OrdinalIgnoreCase))
        {
            return null;
        }

        string repoRoot;
        try
        {
            repoRoot = RepositoryDiscovery.FindRoot(Directory.GetCurrentDirectory()).RootPath;
        }
        catch
        {
            return null;
        }

        var pipeName = PipeName(repoRoot);
        var request = JsonSerializer.Serialize(new CliProxyRequest(Directory.GetCurrentDirectory(), args));
        var response = await TrySendAsync(pipeName, request, TimeSpan.FromMilliseconds(75)).ConfigureAwait(false);
        if (response is null)
        {
            StartServer(repoRoot, pipeName);
            for (var attempt = 0; attempt < 30 && response is null; attempt++)
            {
                await Task.Delay(50).ConfigureAwait(false);
                response = await TrySendAsync(pipeName, request, TimeSpan.FromMilliseconds(100)).ConfigureAwait(false);
            }
        }

        return response;
    }

    private static async Task RunServerAsync(string repoRoot, string pipeName)
    {
        Directory.SetCurrentDirectory(repoRoot);
        CliApp.ServerSessions = new RepositorySessionRegistry(maxSessions: 1);
        while (true)
        {
            await using var server = new NamedPipeServerStream(
                pipeName,
                PipeDirection.InOut,
                1,
                PipeTransmissionMode.Byte,
                PipeOptions.Asynchronous);
            using var idle = new CancellationTokenSource(TimeSpan.FromMinutes(10));
            try
            {
                await server.WaitForConnectionAsync(idle.Token).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                return;
            }

            using var reader = new StreamReader(server, Encoding.UTF8, leaveOpen: true);
            using var writer = new StreamWriter(server, new UTF8Encoding(false), leaveOpen: true) { AutoFlush = true };
            var line = await reader.ReadLineAsync().ConfigureAwait(false);
            if (line is null)
            {
                continue;
            }

            var request = JsonSerializer.Deserialize<CliProxyRequest>(line);
            var response = request is null
                ? new CliProxyResponse(4, string.Empty, "Invalid CLI daemon request." + Environment.NewLine)
                : await ExecuteAsync(request).ConfigureAwait(false);
            try
            {
                await writer.WriteLineAsync(JsonSerializer.Serialize(response)).ConfigureAwait(false);
            }
            catch (IOException)
            {
                // A client may time out or be terminated while a long first load is completing.
            }
        }
    }

    private static async Task<CliProxyResponse> ExecuteAsync(CliProxyRequest request)
    {
        var previousDirectory = Directory.GetCurrentDirectory();
        var previousOut = Console.Out;
        var previousError = Console.Error;
        using var output = new StringWriter();
        using var error = new StringWriter();
        try
        {
            Directory.SetCurrentDirectory(request.WorkingDirectory);
            Console.SetOut(output);
            Console.SetError(error);
            var exitCode = await CliApp.RunLocalAsync(request.Arguments).ConfigureAwait(false);
            return new CliProxyResponse(exitCode, output.ToString(), error.ToString());
        }
        catch (Exception ex)
        {
            return new CliProxyResponse(4, output.ToString(), error.ToString() + ex.Message + Environment.NewLine);
        }
        finally
        {
            Console.SetOut(previousOut);
            Console.SetError(previousError);
            Directory.SetCurrentDirectory(previousDirectory);
        }
    }

    private static async Task<CliProxyResponse?> TrySendAsync(string pipeName, string request, TimeSpan timeout)
    {
        try
        {
            await using var client = new NamedPipeClientStream(".", pipeName, PipeDirection.InOut, PipeOptions.Asynchronous);
            client.Connect(Math.Max(1, (int)timeout.TotalMilliseconds));
            using var reader = new StreamReader(client, Encoding.UTF8, leaveOpen: true);
            using var writer = new StreamWriter(client, new UTF8Encoding(false), leaveOpen: true) { AutoFlush = true };
            await writer.WriteLineAsync(request).ConfigureAwait(false);
            using var responseTimeout = new CancellationTokenSource(TimeSpan.FromSeconds(60));
            var line = await reader.ReadLineAsync(responseTimeout.Token).ConfigureAwait(false);
            return line is null ? null : JsonSerializer.Deserialize<CliProxyResponse>(line);
        }
        catch (Exception ex) when (ex is IOException or OperationCanceledException or TimeoutException)
        {
            return null;
        }
    }

    private static void StartServer(string repoRoot, string pipeName)
    {
        var executable = Environment.ProcessPath ?? throw new InvalidOperationException("Cannot resolve the CLI process path.");
        var assemblyPath = Assembly.GetExecutingAssembly().Location;
        var serverAssemblyPath = CreateShadowCopy(assemblyPath, pipeName);
        var usesDotnetHost = string.Equals(
            Path.GetFileNameWithoutExtension(executable),
            "dotnet",
            StringComparison.OrdinalIgnoreCase);
        var serverExecutable = usesDotnetHost
            ? executable
            : Path.Combine(Path.GetDirectoryName(serverAssemblyPath)!, Path.GetFileName(executable));
        var startInfo = new ProcessStartInfo(serverExecutable)
        {
            WorkingDirectory = repoRoot,
            UseShellExecute = false,
            CreateNoWindow = true,
            WindowStyle = ProcessWindowStyle.Hidden,
            RedirectStandardInput = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true
        };
        if (usesDotnetHost)
        {
            startInfo.ArgumentList.Add(serverAssemblyPath);
        }
        startInfo.ArgumentList.Add(ServerArgument);
        startInfo.ArgumentList.Add(repoRoot);
        startInfo.ArgumentList.Add(pipeName);
        _ = Process.Start(startInfo);
    }

    private static string CreateShadowCopy(string assemblyPath, string pipeName)
    {
        var sourceDirectory = Path.GetDirectoryName(assemblyPath)
            ?? throw new InvalidOperationException("Cannot resolve the CLI assembly directory.");
        var targetDirectory = Path.Combine(Path.GetTempPath(), "ri-query-daemon", pipeName);
        Directory.CreateDirectory(targetDirectory);
        foreach (var sourcePath in Directory.EnumerateFiles(sourceDirectory, "*", SearchOption.AllDirectories))
        {
            var relative = Path.GetRelativePath(sourceDirectory, sourcePath);
            var targetPath = Path.Combine(targetDirectory, relative);
            Directory.CreateDirectory(Path.GetDirectoryName(targetPath)!);
            File.Copy(sourcePath, targetPath, overwrite: true);
        }

        return Path.Combine(targetDirectory, Path.GetFileName(assemblyPath));
    }

    private static string PipeName(string repoRoot)
    {
        var assembly = new FileInfo(Assembly.GetExecutingAssembly().Location);
        var identity = repoRoot + "|" + assembly.FullName + "|" + assembly.LastWriteTimeUtc.Ticks;
        return "ri-query-" + Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(identity)))[..20].ToLowerInvariant();
    }

    private sealed record CliProxyRequest(string WorkingDirectory, string[] Arguments);
}
