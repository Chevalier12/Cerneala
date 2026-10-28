using System.ComponentModel;
using Cerneala.VisualStudio.Server;

namespace Cerneala.Tests.VisualStudio;

public sealed class CernealaServerProcessManagerTests : IDisposable
{
    private readonly string directory = Path.Combine(
        Path.GetTempPath(),
        "Cerneala.VisualStudio.Tests",
        Guid.NewGuid().ToString("N"));
    private readonly string executablePath;

    public CernealaServerProcessManagerTests()
    {
        executablePath = Path.Combine(directory, "Cerneala.LanguageServer.exe");
        Directory.CreateDirectory(directory);
        File.WriteAllText(executablePath, "test server placeholder");
    }

    [Fact]
    public async Task MissingBundledBinaryFailsWithTheInstallRelativePath()
    {
        TestLog log = new();
        string missing = Path.Combine(directory, "missing", "Cerneala.LanguageServer.exe");
        using CernealaServerProcessManager manager = new(
            missing,
            new FakeProcessFactory(),
            log);

        FileNotFoundException exception = await Assert.ThrowsAsync<FileNotFoundException>(
            () => manager.StartAsync(CancellationToken.None));

        Assert.Equal(Path.GetFullPath(missing), exception.FileName);
        Assert.Contains(log.Errors, message => message.Contains("Expected path", StringComparison.Ordinal));
    }

    [Fact]
    public async Task StartupFailureIsLoggedAndDoesNotCreateASession()
    {
        TestLog log = new();
        FakeProcessFactory factory = new()
        {
            StartException = new Win32Exception("blocked by policy")
        };
        using CernealaServerProcessManager manager = new(executablePath, factory, log);

        Win32Exception exception = await Assert.ThrowsAsync<Win32Exception>(
            () => manager.StartAsync(CancellationToken.None));

        Assert.Equal("blocked by policy", exception.Message);
        Assert.Equal(1, factory.StartCount);
        Assert.Contains(log.Errors, message => message.Contains("failed to start", StringComparison.Ordinal));
    }

    [Fact]
    public async Task ProtocolFailureStopsTheSessionAndBacksOffBeforeRetry()
    {
        TestLog log = new();
        FakeProcess first = new();
        FakeProcess second = new();
        FakeProcessFactory factory = new(first, second);
        List<TimeSpan> delays = [];
        using CernealaServerProcessManager manager = new(
            executablePath,
            factory,
            log,
            delay: (delay, _) =>
            {
                delays.Add(delay);
                return Task.CompletedTask;
            });
        await manager.StartAsync(CancellationToken.None);

        bool shouldRestart = await manager.ReportProtocolFailureAsync(
            "invalid initialize response",
            CancellationToken.None);
        await manager.StartAsync(CancellationToken.None);

        Assert.True(shouldRestart);
        Assert.True(first.InputClosed);
        Assert.Equal([TimeSpan.FromMilliseconds(250)], delays);
        Assert.Contains(log.Errors, message => message.Contains("protocol initialization failed", StringComparison.Ordinal));
    }

    [Fact]
    public async Task CrashesUseBoundedBackoffAndDisableTheRestartLoopAtTheThreshold()
    {
        TestLog log = new();
        FakeProcess first = new();
        FakeProcess second = new();
        FakeProcess third = new();
        FakeProcessFactory factory = new(first, second, third);
        List<TimeSpan> delays = [];
        using CernealaServerProcessManager manager = new(
            executablePath,
            factory,
            log,
            delay: (delay, _) =>
            {
                delays.Add(delay);
                return Task.CompletedTask;
            });

        await manager.StartAsync(CancellationToken.None);
        manager.MarkInitialized();
        first.Exit(7);
        await manager.StartAsync(CancellationToken.None);
        manager.MarkInitialized();
        second.Exit(8);
        await manager.StartAsync(CancellationToken.None);
        manager.MarkInitialized();
        third.Exit(9);

        InvalidOperationException exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => manager.StartAsync(CancellationToken.None));

        Assert.Equal(
            [TimeSpan.FromMilliseconds(250), TimeSpan.FromSeconds(1)],
            delays);
        Assert.Contains("restart loop disabled", exception.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Contains(log.Errors, message => message.Contains("exit code 9", StringComparison.Ordinal));
    }

    [Fact]
    public async Task InitializationFailuresStopRequestingRestartsAtTheThreshold()
    {
        TestLog log = new();
        using CernealaServerProcessManager manager = new(
            executablePath,
            new FakeProcessFactory(),
            log);

        bool first = await manager.ReportProtocolFailureAsync("first", CancellationToken.None);
        bool second = await manager.ReportProtocolFailureAsync("second", CancellationToken.None);
        bool third = await manager.ReportProtocolFailureAsync("third", CancellationToken.None);

        Assert.True(first);
        Assert.True(second);
        Assert.False(third);
        Assert.Contains(log.Errors, message => message.Contains("3/3", StringComparison.Ordinal));
    }

    [Fact]
    public async Task CancellationDuringRestartBackoffDoesNotStartAnotherProcess()
    {
        TestLog log = new();
        FakeProcess first = new();
        FakeProcess second = new();
        FakeProcessFactory factory = new(first, second);
        using CancellationTokenSource cancellation = new();
        using CernealaServerProcessManager manager = new(
            executablePath,
            factory,
            log,
            delay: (_, token) =>
            {
                cancellation.Cancel();
                return Task.FromCanceled(token);
            });
        await manager.StartAsync(CancellationToken.None);
        manager.MarkInitialized();
        first.Exit(1);

        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => manager.StartAsync(cancellation.Token));

        Assert.Equal(1, factory.StartCount);
    }

    [Fact]
    public async Task DisposeDuringSolutionCloseStopLetsTheStopFinishWithoutThrowing()
    {
        TestLog log = new();
        FakeProcess process = new() { HoldAsyncWait = true };
        CernealaServerProcessManager manager = new(
            executablePath,
            new FakeProcessFactory(process),
            log);
        await manager.StartAsync(CancellationToken.None);

        Task stop = manager.StopAsync(
            CernealaServerShutdownReason.SolutionClose,
            CancellationToken.None);
        await process.AsyncWaitStarted.WaitAsync(TimeSpan.FromSeconds(2));
        manager.Dispose();
        process.ReleaseAsyncWait();
        Exception? observed = await Record.ExceptionAsync(
            () => stop.WaitAsync(TimeSpan.FromSeconds(5)));

        Assert.True(
            observed is null && process.DisposeCount == 1 && !process.Killed,
            $"StopAsync threw {observed?.GetType().FullName}: {observed?.Message}; " +
            $"DisposeCount={process.DisposeCount}, Killed={process.Killed}.");
    }

    [Fact]
    public async Task DisposeDuringRestartBackoffDoesNotStartAnotherProcess()
    {
        TestLog log = new();
        FakeProcess first = new();
        FakeProcess second = new();
        FakeProcessFactory factory = new(first, second);
        TaskCompletionSource<bool> delayEntered = new(TaskCreationOptions.RunContinuationsAsynchronously);
        TaskCompletionSource<bool> delayReleased = new(TaskCreationOptions.RunContinuationsAsynchronously);
        CernealaServerProcessManager manager = new(
            executablePath,
            factory,
            log,
            delay: async (_, _) =>
            {
                delayEntered.TrySetResult(true);
                await delayReleased.Task;
            });
        await manager.StartAsync(CancellationToken.None);
        manager.MarkInitialized();
        first.Exit(1);

        Task restart = manager.StartAsync(CancellationToken.None);
        await delayEntered.Task.WaitAsync(TimeSpan.FromSeconds(2));
        manager.Dispose();
        delayReleased.TrySetResult(true);
        Exception? observed = await Record.ExceptionAsync(
            () => restart.WaitAsync(TimeSpan.FromSeconds(5)));

        Assert.True(
            observed is ObjectDisposedException { ObjectName: nameof(CernealaServerProcessManager) } &&
                factory.StartCount == 1,
            $"StartCount={factory.StartCount}, restart threw {observed?.GetType().FullName}: {observed?.Message}");
    }

    [Fact]
    public async Task GracefulStopDoesNotDisposeProcessWhileExitCallbackReadsExitCode()
    {
        TestLog log = new();
        FakeProcess process = new() { CoordinateExitCodeDisposeRace = true };
        using CernealaServerProcessManager manager = new(
            executablePath,
            new FakeProcessFactory(process),
            log);
        await manager.StartAsync(CancellationToken.None);

        Task stop = manager.StopAsync(
            CernealaServerShutdownReason.Restart,
            CancellationToken.None);
        await process.ExitCodeReadStarted.WaitAsync(TimeSpan.FromSeconds(2));
        await stop;
        process.ReleaseExitCodeRead();
        await process.ExitNotification;

        Assert.Equal(1, process.DisposeCount);
        Assert.Contains(log.Information, message => message.Contains("stopped", StringComparison.Ordinal));
    }

    [Theory]
    [InlineData((int)CernealaServerShutdownReason.ExtensionDisable)]
    [InlineData((int)CernealaServerShutdownReason.Uninstall)]
    [InlineData((int)CernealaServerShutdownReason.Update)]
    public async Task DisableUpdateAndUninstallForceTerminationOnlyAfterTheTimeout(
        int reasonValue)
    {
        CernealaServerShutdownReason reason = (CernealaServerShutdownReason)reasonValue;
        TestLog log = new();
        FakeProcess process = new() { RefuseGracefulExit = true };
        using CernealaServerProcessManager manager = new(
            executablePath,
            new FakeProcessFactory(process),
            log);
        await manager.StartAsync(CancellationToken.None);

        await manager.StopAsync(reason, CancellationToken.None);

        Assert.True(process.InputClosed);
        Assert.True(process.Killed);
        Assert.True(process.WaitRequestedBeforeKill);
        Assert.Contains(log.Errors, message => message.Contains("did not exit", StringComparison.Ordinal));
    }

    [Theory]
    [InlineData("StopAsync")]
    [InlineData("Dispose")]
    [InlineData("VisualStudioShutdown")]
    public async Task EveryDisableShutdownPathWaitsBeforeKillingAndLogsTheForcedTermination(string path)
    {
        TestLog log = new();
        FakeProcess process = new() { RefuseGracefulExit = true };
        CernealaServerProcessManager manager = new(
            executablePath,
            new FakeProcessFactory(process),
            log);
        await manager.StartAsync(CancellationToken.None);

        switch (path)
        {
            case "StopAsync":
                await manager.StopAsync(CernealaServerShutdownReason.ExtensionDisable, CancellationToken.None);
                break;
            case "Dispose":
                manager.Dispose();
                break;
            default:
                manager.Stop(CernealaServerShutdownReason.VisualStudioShutdown);
                break;
        }

        manager.Dispose();
        bool loggedDidNotExit = log.Errors.Any(message => message.Contains("did not exit", StringComparison.Ordinal));
        Assert.True(
            process.InputClosed && process.Killed && process.WaitRequestedBeforeKill && loggedDidNotExit,
            $"{path}: InputClosed={process.InputClosed}, Killed={process.Killed}, " +
            $"WaitRequestedBeforeKill={process.WaitRequestedBeforeKill}, LoggedDidNotExit={loggedDidNotExit}.");
    }

    [Fact]
    public async Task DisposeOfAServerThatExitsOnInputCloseDoesNotKillIt()
    {
        TestLog log = new();
        FakeProcess process = new();
        CernealaServerProcessManager manager = new(
            executablePath,
            new FakeProcessFactory(process),
            log);
        await manager.StartAsync(CancellationToken.None);

        manager.Dispose();

        Assert.True(process.InputClosed && !process.Killed, $"InputClosed={process.InputClosed}, Killed={process.Killed}.");
    }

    [Fact]
    public async Task ExtensionDisableWaitsTheShutdownTimeoutAndVisualStudioShutdownStaysInsideTheProcessExitBudget()
    {
        FakeProcess disabled = new() { RefuseGracefulExit = true };
        FakeProcess shutDown = new() { RefuseGracefulExit = true };
        CernealaServerProcessManager disabledManager = new(
            executablePath,
            new FakeProcessFactory(disabled),
            new TestLog());
        using CernealaServerProcessManager shutDownManager = new(
            executablePath,
            new FakeProcessFactory(shutDown),
            new TestLog());
        await disabledManager.StartAsync(CancellationToken.None);
        await shutDownManager.StartAsync(CancellationToken.None);

        disabledManager.Dispose();
        shutDownManager.Stop(CernealaServerShutdownReason.VisualStudioShutdown);

        Assert.Equal([CernealaServerProcessManager.ShutdownTimeout], disabled.RequestedWaits);
        Assert.Equal([CernealaServerProcessManager.ProcessExitShutdownTimeout], shutDown.RequestedWaits);
        Assert.True(CernealaServerProcessManager.ProcessExitShutdownTimeout < TimeSpan.FromSeconds(2));
    }

    [Fact]
    public async Task ActivationCancellationStillAbortsWithoutWaiting()
    {
        TestLog log = new();
        FakeProcess process = new() { RefuseGracefulExit = true };
        using CernealaServerProcessManager manager = new(
            executablePath,
            new FakeProcessFactory(process),
            log);
        await manager.StartAsync(CancellationToken.None);

        manager.StopImmediately(CernealaServerShutdownReason.ActivationCancelled);

        Assert.True(process.InputClosed);
        Assert.True(process.Killed);
        Assert.Empty(process.RequestedWaits);
        Assert.DoesNotContain(log.Errors, message => message.Contains("did not exit", StringComparison.Ordinal));
    }

    [Fact]
    public void BundledPathIsVersionedUnderTheExtensionInstallRoot()
    {
        string assemblyPath = Path.Combine(directory, "Cerneala.VisualStudio.dll");

        string path = CernealaServerProcessManager.ResolveBundledServerPath(assemblyPath, "0.1.0");

        Assert.Equal(
            Path.Combine(directory, "Server", "0.1.0", "Cerneala.LanguageServer.exe"),
            path);
        Assert.DoesNotContain("Desktop", path, StringComparison.OrdinalIgnoreCase);
    }

    public void Dispose()
    {
        if (Directory.Exists(directory))
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    private sealed class FakeProcessFactory : ICernealaServerProcessFactory
    {
        private readonly Queue<FakeProcess> processes;

        public FakeProcessFactory(params FakeProcess[] processes)
        {
            this.processes = new Queue<FakeProcess>(processes);
        }

        public Exception? StartException { get; init; }

        public int StartCount { get; private set; }

        public ICernealaServerProcess Start(string executablePath, string workingDirectory)
        {
            StartCount++;
            if (StartException is not null)
            {
                throw StartException;
            }

            return processes.Count > 0
                ? processes.Dequeue()
                : throw new InvalidOperationException("No fake process was configured.");
        }
    }

    private sealed class FakeProcess : ICernealaServerProcess
    {
        private readonly MemoryStream output = new();
        private readonly MemoryStream input = new();
        private readonly TaskCompletionSource<bool> exitCodeReadStarted = new(
            TaskCreationOptions.RunContinuationsAsynchronously);
        private readonly TaskCompletionSource<bool> allowExitCodeRead = new(
            TaskCreationOptions.RunContinuationsAsynchronously);
        private readonly TaskCompletionSource<bool> asyncWaitStarted = new(
            TaskCreationOptions.RunContinuationsAsynchronously);
        private readonly TaskCompletionSource<bool> asyncWaitReleased = new(
            TaskCreationOptions.RunContinuationsAsynchronously);
        private int? exitCode;
        private bool disposed;

        public event EventHandler? Exited;

        public Stream StandardOutput => output;

        public Stream StandardInput => input;

        public bool HasExited { get; private set; }

        public int? ExitCode
        {
            get
            {
                if (CoordinateExitCodeDisposeRace)
                {
                    exitCodeReadStarted.TrySetResult(true);
                    allowExitCodeRead.Task.GetAwaiter().GetResult();
                }

                if (disposed)
                {
                    throw new InvalidOperationException("The process was disposed before its exit code was read.");
                }

                return exitCode;
            }
        }

        public bool RefuseGracefulExit { get; init; }

        public bool CoordinateExitCodeDisposeRace { get; init; }

        public bool HoldAsyncWait { get; init; }

        public bool InputClosed { get; private set; }

        public bool Killed { get; private set; }

        public bool WaitRequestedBeforeKill { get; private set; }

        public List<TimeSpan> RequestedWaits { get; } = [];

        public int DisposeCount { get; private set; }

        public Task ExitCodeReadStarted => exitCodeReadStarted.Task;

        public Task AsyncWaitStarted => asyncWaitStarted.Task;

        public Task ExitNotification { get; private set; } = Task.CompletedTask;

        public void CloseInput()
        {
            InputClosed = true;
            if (!RefuseGracefulExit)
            {
                Exit(0);
            }
        }

        public async Task<bool> WaitForExitAsync(TimeSpan timeout, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (!Killed)
            {
                WaitRequestedBeforeKill = true;
            }

            if (CoordinateExitCodeDisposeRace && HasExited)
            {
                await exitCodeReadStarted.Task.WaitAsync(cancellationToken);
            }

            if (HoldAsyncWait)
            {
                asyncWaitStarted.TrySetResult(true);
                await asyncWaitReleased.Task.WaitAsync(cancellationToken);
            }

            return HasExited;
        }

        public bool WaitForExit(TimeSpan timeout)
        {
            RequestedWaits.Add(timeout);
            if (!Killed)
            {
                WaitRequestedBeforeKill = true;
            }

            return HasExited;
        }

        public void Kill()
        {
            Killed = true;
            Exit(-1);
        }

        public void Exit(int exitCode)
        {
            if (HasExited)
            {
                return;
            }

            HasExited = true;
            this.exitCode = exitCode;
            if (CoordinateExitCodeDisposeRace)
            {
                ExitNotification = Task.Run(() => Exited?.Invoke(this, EventArgs.Empty));
            }
            else
            {
                Exited?.Invoke(this, EventArgs.Empty);
            }
        }

        public void Dispose()
        {
            disposed = true;
            DisposeCount++;
        }

        public void ReleaseExitCodeRead() => allowExitCodeRead.TrySetResult(true);

        public void ReleaseAsyncWait() => asyncWaitReleased.TrySetResult(true);
    }

    private sealed class TestLog : ICernealaServerLog
    {
        public List<string> Information { get; } = [];

        public List<string> Errors { get; } = [];

        public void Info(string message) => Information.Add(message);

        public void Error(string message, Exception? exception = null) => Errors.Add(message);
    }
}
