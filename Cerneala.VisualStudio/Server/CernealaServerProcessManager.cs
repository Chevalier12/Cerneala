namespace Cerneala.VisualStudio.Server;

using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

internal enum CernealaServerShutdownReason
{
    SolutionClose,
    ExtensionDisable,
    Update,
    VisualStudioShutdown,
    Uninstall,
    Restart,
    ProtocolFailure,
    ActivationCancelled
}

internal interface ICernealaServerLog
{
    void Info(string message);

    void Error(string message, Exception? exception = null);
}

internal interface ICernealaServerProcessFactory
{
    ICernealaServerProcess Start(string executablePath, string workingDirectory);
}

internal interface ICernealaServerProcess : IDisposable
{
    event EventHandler? Exited;

    Stream StandardOutput { get; }

    Stream StandardInput { get; }

    bool HasExited { get; }

    int? ExitCode { get; }

    void CloseInput();

    Task<bool> WaitForExitAsync(TimeSpan timeout, CancellationToken cancellationToken);

    void Kill();
}

internal sealed class CernealaServerSession(
    Stream reader,
    Stream writer)
{
    public Stream Reader { get; } = reader;

    public Stream Writer { get; } = writer;
}

internal sealed class CernealaServerProcessManager : IDisposable
{
    internal const int MaximumCrashCount = 3;
    internal static readonly TimeSpan ShutdownTimeout = TimeSpan.FromSeconds(3);

    private static readonly TimeSpan CrashWindow = TimeSpan.FromMinutes(2);
    private static readonly TimeSpan[] RestartDelays =
    [
        TimeSpan.FromMilliseconds(250),
        TimeSpan.FromSeconds(1),
        TimeSpan.FromSeconds(4)
    ];

    private readonly string executablePath;
    private readonly ICernealaServerProcessFactory processFactory;
    private readonly ICernealaServerLog log;
    private readonly Func<DateTimeOffset> utcNow;
    private readonly Func<TimeSpan, CancellationToken, Task> delay;
    private readonly SemaphoreSlim lifecycleGate = new(1, 1);
    private readonly object stateGate = new();
    private readonly List<DateTimeOffset> crashes = [];
    private ICernealaServerProcess? process;
    private bool expectedExit;
    private bool initialized;
    private bool disposed;

    public CernealaServerProcessManager(
        string executablePath,
        ICernealaServerProcessFactory processFactory,
        ICernealaServerLog log,
        Func<DateTimeOffset>? utcNow = null,
        Func<TimeSpan, CancellationToken, Task>? delay = null)
    {
        this.executablePath = Path.GetFullPath(
            executablePath ?? throw new ArgumentNullException(nameof(executablePath)));
        this.processFactory = processFactory ?? throw new ArgumentNullException(nameof(processFactory));
        this.log = log ?? throw new ArgumentNullException(nameof(log));
        this.utcNow = utcNow ?? (() => DateTimeOffset.UtcNow);
        this.delay = delay ?? Task.Delay;
    }

    public static string ResolveBundledServerPath(
        string extensionAssemblyPath,
        string extensionVersion)
    {
        string installRoot = Path.GetDirectoryName(Path.GetFullPath(extensionAssemblyPath))
            ?? throw new ArgumentException("The extension assembly path has no parent directory.", nameof(extensionAssemblyPath));
        return Path.Combine(
            installRoot,
            "Server",
            extensionVersion ?? throw new ArgumentNullException(nameof(extensionVersion)),
            "Cerneala.LanguageServer.exe");
    }

    public async Task<CernealaServerSession> StartAsync(CancellationToken cancellationToken)
    {
        await lifecycleGate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            ThrowIfDisposed();
            RemoveExpiredCrashes();
            if (crashes.Count >= MaximumCrashCount)
            {
                string message = "Cerneala language server restart loop disabled after " +
                    MaximumCrashCount + " crashes in " + CrashWindow.TotalMinutes + " minutes.";
                log.Error(message);
                throw new InvalidOperationException(message);
            }

            ICernealaServerProcess? current = GetProcess();
            if (current is not null && !current.HasExited)
            {
                throw new InvalidOperationException("The Cerneala language server is already running.");
            }

            if (crashes.Count > 0)
            {
                TimeSpan backoff = RestartDelays[Math.Min(crashes.Count - 1, RestartDelays.Length - 1)];
                log.Info("Cerneala language server restart delayed by " + backoff.TotalMilliseconds + " ms.");
                await delay(backoff, cancellationToken).ConfigureAwait(false);
            }

            if (!File.Exists(executablePath))
            {
                FileNotFoundException missing = new(
                    "The bundled Cerneala language server is missing.",
                    executablePath);
                log.Error(missing.Message + " Expected path: " + executablePath, missing);
                throw missing;
            }

            ICernealaServerProcess started;
            try
            {
                started = processFactory.Start(
                    executablePath,
                    Path.GetDirectoryName(executablePath)!);
            }
            catch (Exception exception)
            {
                log.Error("Cerneala language server failed to start from " + executablePath + ".", exception);
                throw;
            }

            lock (stateGate)
            {
                process = started;
                expectedExit = false;
                initialized = false;
            }

            started.Exited += OnProcessExited;
            if (started.HasExited)
            {
                OnProcessExited(started, EventArgs.Empty);
                throw new InvalidOperationException("The Cerneala language server exited during startup.");
            }

            log.Info("Cerneala language server started from " + executablePath + ".");
            return new CernealaServerSession(started.StandardOutput, started.StandardInput);
        }
        finally
        {
            lifecycleGate.Release();
        }
    }

    public async Task<bool> ReportProtocolFailureAsync(
        string details,
        CancellationToken cancellationToken)
    {
        RecordCrash("Cerneala language server protocol initialization failed: " + details, null);
        await StopAsync(CernealaServerShutdownReason.ProtocolFailure, cancellationToken)
            .ConfigureAwait(false);
        lock (stateGate)
        {
            RemoveExpiredCrashesCore();
            return crashes.Count < MaximumCrashCount;
        }
    }

    public void MarkInitialized()
    {
        lock (stateGate)
        {
            if (process is not null && !process.HasExited)
            {
                initialized = true;
            }
        }
    }

    public async Task StopAsync(
        CernealaServerShutdownReason reason,
        CancellationToken cancellationToken)
    {
        await lifecycleGate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            ICernealaServerProcess? current;
            lock (stateGate)
            {
                current = process;
                expectedExit = true;
            }

            if (current is null)
            {
                return;
            }

            log.Info("Stopping Cerneala language server. Reason: " + reason + ".");
            current.CloseInput();
            bool exited = await current.WaitForExitAsync(ShutdownTimeout, cancellationToken)
                .ConfigureAwait(false);
            if (!exited)
            {
                log.Error("Cerneala language server did not exit within " +
                    ShutdownTimeout.TotalSeconds + " seconds; terminating it.");
                current.Kill();
                await current.WaitForExitAsync(TimeSpan.FromSeconds(1), CancellationToken.None)
                    .ConfigureAwait(false);
            }

            ClearProcess(current);
        }
        finally
        {
            lifecycleGate.Release();
        }
    }

    public void ResetCrashLoop()
    {
        lock (stateGate)
        {
            crashes.Clear();
        }
    }

    public void StopImmediately(CernealaServerShutdownReason reason)
    {
        ICernealaServerProcess? current;
        lock (stateGate)
        {
            current = process;
            expectedExit = true;
            process = null;
        }

        if (current is null)
        {
            return;
        }

        log.Info("Terminating Cerneala language server immediately. Reason: " + reason + ".");
        try
        {
            current.CloseInput();
            if (!current.HasExited)
            {
                current.Kill();
            }
        }
        catch (Exception exception)
        {
            log.Error("Cerneala language server termination failed.", exception);
        }
        finally
        {
            current.Exited -= OnProcessExited;
            current.Dispose();
        }
    }

    public void Dispose()
    {
        if (disposed)
        {
            return;
        }

        disposed = true;
        StopImmediately(CernealaServerShutdownReason.ExtensionDisable);
        lifecycleGate.Dispose();
    }

    private ICernealaServerProcess? GetProcess()
    {
        lock (stateGate)
        {
            return process;
        }
    }

    private void OnProcessExited(object? sender, EventArgs args)
    {
        if (sender is not ICernealaServerProcess exited)
        {
            return;
        }

        bool crashed;
        bool crashedAfterInitialization;
        lock (stateGate)
        {
            if (!ReferenceEquals(process, exited))
            {
                return;
            }

            crashed = !expectedExit;
            crashedAfterInitialization = crashed && initialized;
            process = null;
            initialized = false;
        }

        exited.Exited -= OnProcessExited;
        int? exitCode = exited.ExitCode;
        exited.Dispose();
        if (crashedAfterInitialization)
        {
            RecordCrash(
                "Cerneala language server crashed with exit code " +
                    (exitCode?.ToString() ?? "unknown") + ".",
                null);
        }
        else if (crashed)
        {
            log.Error(
                "Cerneala language server exited before protocol initialization with exit code " +
                    (exitCode?.ToString() ?? "unknown") + ".");
        }
        else
        {
            log.Info("Cerneala language server stopped.");
        }
    }

    private void RecordCrash(string message, Exception? exception)
    {
        int count;
        lock (stateGate)
        {
            RemoveExpiredCrashesCore();
            crashes.Add(utcNow());
            count = crashes.Count;
        }

        log.Error(message + " Crash count: " + count + "/" + MaximumCrashCount + ".", exception);
    }

    private void RemoveExpiredCrashes()
    {
        lock (stateGate)
        {
            RemoveExpiredCrashesCore();
        }
    }

    private void RemoveExpiredCrashesCore()
    {
        DateTimeOffset cutoff = utcNow() - CrashWindow;
        crashes.RemoveAll(timestamp => timestamp < cutoff);
    }

    private void ClearProcess(ICernealaServerProcess current)
    {
        bool ownsProcess;
        lock (stateGate)
        {
            ownsProcess = ReferenceEquals(process, current);
            if (ownsProcess)
            {
                process = null;
            }
        }

        if (!ownsProcess)
        {
            return;
        }

        current.Exited -= OnProcessExited;
        current.Dispose();
    }

    private void ThrowIfDisposed()
    {
        if (disposed)
        {
            throw new ObjectDisposedException(nameof(CernealaServerProcessManager));
        }
    }
}
