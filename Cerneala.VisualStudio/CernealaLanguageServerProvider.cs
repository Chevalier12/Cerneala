namespace Cerneala.VisualStudio;

using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Cerneala.VisualStudio.Server;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.LanguageServer.Client;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Threading;
using Microsoft.VisualStudio.Utilities;

[Export(typeof(ILanguageClient))]
[ContentType(CernealaContentType.Name)]
[PartCreationPolicy(CreationPolicy.Shared)]
internal sealed class CernealaLanguageServerProvider : ILanguageClient, IVsSolutionEvents, IDisposable
{
    private static long activationSequence;
    private static long activationStartedTimestamp;
    private static long lastActivationCpuTicks = -1;
    private static long lastServerReadyTicks = -1;
    private static long serverReadySequence;
    private readonly VisualStudioServerLog log;
    private readonly CernealaServerProcessManager processManager;
    private readonly object optionsGate = new();
    private IReadOnlyDictionary<string, object?> initializationOptions = CreateInitializationOptions(null);
    private IVsSolution? solution;
    private uint solutionEventsCookie;
    private CancellationTokenRegistration activationCancellation;
    private bool disposed;

    [DllImport("kernel32.dll")]
    private static extern IntPtr GetCurrentThread();

    [DllImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool GetThreadTimes(
        IntPtr thread,
        out NativeFileTime creationTime,
        out NativeFileTime exitTime,
        out NativeFileTime kernelTime,
        out NativeFileTime userTime);

    public CernealaLanguageServerProvider()
    {
        log = new VisualStudioServerLog();
        string assemblyPath = typeof(CernealaLanguageServerProvider).Assembly.Location;
        string version = GetExtensionVersion(typeof(CernealaLanguageServerProvider).Assembly);
        string serverPath = CernealaServerProcessManager.ResolveBundledServerPath(assemblyPath, version);
        processManager = new CernealaServerProcessManager(
            serverPath,
            new SystemCernealaServerProcessFactory(log),
            log);
        log.Info("Cerneala language client was created.");
        AppDomain.CurrentDomain.ProcessExit += OnVisualStudioProcessExit;
        AppDomain.CurrentDomain.DomainUnload += OnVisualStudioProcessExit;
    }

    public event AsyncEventHandler<EventArgs>? StartAsync;

    public event AsyncEventHandler<EventArgs>? StopAsync;

    internal static long ActivationSequence => Interlocked.Read(ref activationSequence);

    internal static double LastActivationCpuMilliseconds =>
        TimeSpan.FromTicks(Interlocked.Read(ref lastActivationCpuTicks)).TotalMilliseconds;

    internal static double LastServerReadyMilliseconds =>
        TimeSpan.FromTicks(Interlocked.Read(ref lastServerReadyTicks)).TotalMilliseconds;

    internal static long ServerReadySequence => Interlocked.Read(ref serverReadySequence);

    public string Name => "Cerneala";

    public IEnumerable<string> ConfigurationSections => Array.Empty<string>();

    public object InitializationOptions
    {
        get
        {
            lock (optionsGate)
            {
                return initializationOptions;
            }
        }
    }

    public IEnumerable<string> FilesToWatch => Array.Empty<string>();

    public bool ShowNotificationOnInitializeFailed => false;

    public async Task OnLoadedAsync()
    {
        await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
        log.Info("Cerneala language client loaded for a .crn document.");
        log.InitializeOutputPane();
        solution = ServiceProvider.GlobalProvider.GetService(typeof(SVsSolution)) as IVsSolution;
        if (solution is not null)
        {
            ErrorHandler.ThrowOnFailure(solution.AdviseSolutionEvents(this, out solutionEventsCookie));
            ErrorHandler.ThrowOnFailure(solution.GetSolutionInfo(
                out _,
                out string solutionPath,
                out _));
            lock (optionsGate)
            {
                initializationOptions = CreateInitializationOptions(
                    string.IsNullOrWhiteSpace(solutionPath) ? null : solutionPath);
            }
        }

        AsyncEventHandler<EventArgs>? start = StartAsync;
        if (start is not null)
        {
            await start.InvokeAsync(this, EventArgs.Empty).ConfigureAwait(false);
        }
    }

    public async Task<Connection?> ActivateAsync(CancellationToken token)
    {
        long activationCpuTicks = 0;
        Interlocked.Exchange(ref lastActivationCpuTicks, -1);
        Interlocked.Exchange(ref lastServerReadyTicks, -1);
        Interlocked.Exchange(ref activationStartedTimestamp, Stopwatch.GetTimestamp());
        Task<CernealaServerSession> startTask;
        long startSegmentBefore = GetCurrentThreadCpuTicks();
        try
        {
            log.Info("Starting the bundled Cerneala language server.");
            activationCancellation.Dispose();
            activationCancellation = token.Register(
                () => processManager.StopImmediately(CernealaServerShutdownReason.ActivationCancelled));
            startTask = processManager.StartAsync(token);
        }
        finally
        {
            activationCpuTicks = GetCpuSegmentTicks(startSegmentBefore);
        }

        try
        {
            CernealaServerSession session = await startTask.ConfigureAwait(false);
            long connectionSegmentBefore = GetCurrentThreadCpuTicks();
            try
            {
                return new Connection(session.Reader, session.Writer);
            }
            finally
            {
                long connectionCpuTicks = GetCpuSegmentTicks(connectionSegmentBefore);
                activationCpuTicks = activationCpuTicks < 0 || connectionCpuTicks < 0
                    ? -1
                    : activationCpuTicks + connectionCpuTicks;
            }
        }
        finally
        {
            Interlocked.Exchange(
                ref lastActivationCpuTicks,
                activationCpuTicks);
            Interlocked.Increment(ref activationSequence);
        }
    }

    public Task OnServerInitializedAsync()
    {
        long startedTimestamp = Interlocked.Read(ref activationStartedTimestamp);
        if (startedTimestamp > 0)
        {
            long elapsedTimestamp = Stopwatch.GetTimestamp() - startedTimestamp;
            long elapsedTicks = (long)(elapsedTimestamp *
                (TimeSpan.TicksPerSecond / (double)Stopwatch.Frequency));
            Interlocked.Exchange(ref lastServerReadyTicks, elapsedTicks);
            Interlocked.Increment(ref serverReadySequence);
        }

        processManager.MarkInitialized();
        log.Info("Cerneala language server initialized for the active Visual Studio workspace.");
        return Task.CompletedTask;
    }

    public async Task<InitializationFailureContext?> OnServerInitializeFailedAsync(
        ILanguageClientInitializationInfo initializationState)
    {
        string details = initializationState.InitializationException?.Message
            ?? initializationState.StatusMessage
            ?? initializationState.Status.ToString();
        bool shouldRestart = await processManager.ReportProtocolFailureAsync(
                details,
                CancellationToken.None)
            .ConfigureAwait(false);
        if (shouldRestart && !disposed)
        {
            log.Info("Requesting a bounded Cerneala language server restart after initialization failure.");
            AsyncEventHandler<EventArgs>? stop = StopAsync;
            if (stop is not null)
            {
                await stop.InvokeAsync(this, EventArgs.Empty).ConfigureAwait(false);
            }

            AsyncEventHandler<EventArgs>? start = StartAsync;
            if (start is not null)
            {
                await start.InvokeAsync(this, EventArgs.Empty).ConfigureAwait(false);
            }
        }

        return new InitializationFailureContext
        {
            FailureMessage = "Cerneala language server initialization failed. See the Cerneala output channel."
        };
    }

    public async Task RestartAsync(CancellationToken cancellationToken)
    {
        processManager.ResetCrashLoop();
        await RequestStopAsync(CernealaServerShutdownReason.Restart, cancellationToken)
            .ConfigureAwait(false);
        AsyncEventHandler<EventArgs>? start = StartAsync;
        if (start is not null)
        {
            await start.InvokeAsync(this, EventArgs.Empty).ConfigureAwait(false);
        }
    }

    public void Dispose()
    {
        if (disposed)
        {
            return;
        }

        disposed = true;
        AppDomain.CurrentDomain.ProcessExit -= OnVisualStudioProcessExit;
        AppDomain.CurrentDomain.DomainUnload -= OnVisualStudioProcessExit;
        activationCancellation.Dispose();
        processManager.Dispose();
#pragma warning disable VSTHRD108 // MEF disposal may run off-thread; COM cleanup is only legal when access is available.
        if (ThreadHelper.CheckAccess() && solution is not null && solutionEventsCookie != 0)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            solution.UnadviseSolutionEvents(solutionEventsCookie);
            solutionEventsCookie = 0;
        }
#pragma warning restore VSTHRD108
    }

    public int OnAfterCloseSolution(object reserved)
    {
        RequestStopAsync(CernealaServerShutdownReason.SolutionClose, CancellationToken.None)
            .FileAndForget("Cerneala/StopAfterSolutionClose");
        return VSConstants.S_OK;
    }

    public int OnAfterLoadProject(IVsHierarchy hierarchy, IVsHierarchy stubHierarchy) => VSConstants.S_OK;

    public int OnAfterOpenProject(IVsHierarchy hierarchy, int added) => VSConstants.S_OK;

    public int OnAfterOpenSolution(object reserved, int newSolution) => VSConstants.S_OK;

    public int OnBeforeCloseProject(IVsHierarchy hierarchy, int removed) => VSConstants.S_OK;

    public int OnBeforeCloseSolution(object reserved) => VSConstants.S_OK;

    public int OnBeforeUnloadProject(IVsHierarchy hierarchy, IVsHierarchy stubHierarchy) => VSConstants.S_OK;

    public int OnQueryCloseProject(IVsHierarchy hierarchy, int removing, ref int cancel) => VSConstants.S_OK;

    public int OnQueryCloseSolution(object reserved, ref int cancel) => VSConstants.S_OK;

    public int OnQueryUnloadProject(IVsHierarchy hierarchy, ref int cancel) => VSConstants.S_OK;

    private static IReadOnlyDictionary<string, object?> CreateInitializationOptions(string? solutionPath) =>
        new Dictionary<string, object?>(StringComparer.Ordinal)
        {
            ["solutionPath"] = solutionPath,
            ["host"] = "visualStudio",
            ["diagnosticsMode"] = "push",
            ["deferWorkspaceLoad"] = true,
            ["telemetryEnabled"] = false
        };

    private static string GetExtensionVersion(Assembly assembly)
    {
        string? informational = assembly
            .GetCustomAttribute<AssemblyInformationalVersionAttribute>()?
            .InformationalVersion;
        if (!string.IsNullOrWhiteSpace(informational))
        {
            int metadata = informational!.IndexOf('+');
            return metadata < 0 ? informational : informational.Substring(0, metadata);
        }

        Version version = assembly.GetName().Version ?? new Version(0, 1, 0);
        return version.Major + "." + version.Minor + "." + version.Build;
    }

    private static long GetCurrentThreadCpuTicks()
    {
        if (!GetThreadTimes(
            GetCurrentThread(),
            out _,
            out _,
            out NativeFileTime kernelTime,
            out NativeFileTime userTime))
        {
            return -1;
        }

        return kernelTime.Ticks + userTime.Ticks;
    }

    private static long GetCpuSegmentTicks(long before)
    {
        long after = GetCurrentThreadCpuTicks();
        return before < 0 || after < before ? -1 : after - before;
    }

    private async Task RequestStopAsync(
        CernealaServerShutdownReason reason,
        CancellationToken cancellationToken)
    {
        AsyncEventHandler<EventArgs>? stop = StopAsync;
        if (stop is not null)
        {
            await stop.InvokeAsync(this, EventArgs.Empty).ConfigureAwait(false);
        }

        await processManager.StopAsync(reason, cancellationToken).ConfigureAwait(false);
    }

    private void OnVisualStudioProcessExit(object? sender, EventArgs args) =>
        processManager.StopImmediately(CernealaServerShutdownReason.VisualStudioShutdown);

    [StructLayout(LayoutKind.Sequential)]
    private readonly struct NativeFileTime
    {
        private readonly uint lowDateTime;
        private readonly uint highDateTime;

        public long Ticks => ((long)highDateTime << 32) | lowDateTime;
    }
}
