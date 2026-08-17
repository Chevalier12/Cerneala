using System.Diagnostics;
using Cerneala.Language.Semantics;
using Cerneala.LanguageServer.Logging;
using LanguageSourceText = Cerneala.Language.Text.SourceText;

namespace Cerneala.LanguageServer.Workspace;

internal sealed class CernealaWorkspace : IAsyncDisposable
{
    private readonly object stateGate = new();
    private readonly WorkspaceConfiguration configuration;
    private readonly IServerLogger logger;
    private readonly DocumentOverlayStore overlays = new();
    private readonly SemaphoreSlim reloadGate = new(1, 1);
    private readonly CancellationTokenSource lifetimeCancellation = new();
    private WorkspaceState state;
    private CancellationTokenSource stateVersionCancellation = new();
    private FileSystemWatcher? watcher;
    private CancellationTokenSource? debounceCancellation;
    private Task? initialReloadTask;
    private bool initialLoadDeferred;
    private bool disposed;

    private CernealaWorkspace(WorkspaceConfiguration configuration, IServerLogger logger)
    {
        this.configuration = configuration;
        this.logger = logger;
        Telemetry = new ServerTelemetry(logger);
        state = WorkspaceState.Empty(0);
    }

    internal event Action? Reloaded;

    public long Revision
    {
        get
        {
            lock (stateGate)
            {
                return state.Revision;
            }
        }
    }

    internal ServerTelemetry Telemetry { get; }

    internal int OpenDocumentCount => overlays.Count;

    public static async Task<CernealaWorkspace> CreateAsync(
        WorkspaceConfiguration configuration,
        IServerLogger logger,
        CancellationToken cancellationToken,
        bool deferInitialLoad = false)
    {
        CernealaWorkspace workspace = new(configuration, logger);
        if (deferInitialLoad)
        {
            workspace.initialLoadDeferred = true;
        }
        else
        {
            await workspace.ReloadAsync(cancellationToken).ConfigureAwait(false);
        }

        workspace.StartWatcher();
        return workspace;
    }

    internal void StartDeferredInitialLoad()
    {
        if (!initialLoadDeferred || initialReloadTask is not null)
        {
            return;
        }

        initialReloadTask = ReloadInitialWorkspaceAsync();
    }

    public bool OpenDocument(string uri, string text, long version) =>
        overlays.Open(PathComparer.FromUri(uri), text, version);

    public bool ApplyChanges(
        string uri,
        long version,
        IReadOnlyList<Protocol.TextDocumentContentChangeEvent> changes) =>
        overlays.ApplyChanges(PathComparer.FromUri(uri), version, changes);

    public void CloseDocument(string uri) => overlays.Close(PathComparer.FromUri(uri));

    public async Task ReloadAsync(CancellationToken cancellationToken)
    {
        ObjectDisposedException.ThrowIf(disposed, this);
        await reloadGate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            long revision;
            lock (stateGate)
            {
                revision = checked(state.Revision + 1);
            }

            using CancellationTokenSource linked = CancellationTokenSource.CreateLinkedTokenSource(
                cancellationToken,
                lifetimeCancellation.Token);
            WorkspaceState replacement = await WorkspaceState.LoadAsync(
                configuration,
                revision,
                logger,
                linked.Token).ConfigureAwait(false);

            WorkspaceState previous;
            CancellationTokenSource previousCancellation;
            lock (stateGate)
            {
                previous = state;
                state = replacement;
                previousCancellation = stateVersionCancellation;
                stateVersionCancellation = new CancellationTokenSource();
            }

            previousCancellation.Cancel();
            previousCancellation.Dispose();
            previous.Release();
            Reloaded?.Invoke();
        }
        finally
        {
            reloadGate.Release();
        }
    }

    public async Task<WorkspaceDocumentSnapshot> GetSnapshotAsync(string uri, CancellationToken cancellationToken)
    {
        long started = Stopwatch.GetTimestamp();
        long allocated = GC.GetTotalAllocatedBytes(precise: false);
        bool cancelled = false;
        cancellationToken.ThrowIfCancellationRequested();
        string path = PathComparer.FromUri(uri);
        WorkspaceState capturedState;
        lock (stateGate)
        {
            capturedState = state;
            capturedState.Retain();
        }

        try
        {
            ProjectContext[] owners = capturedState.GetOwners(path);
            CernealaDocument document;
            if (overlays.TryGet(path, out LanguageSourceText? overlay, out _))
            {
                document = new CernealaDocument(path, overlay!);
            }
            else if (owners.FirstOrDefault()?.TryGetDocument(path, out CernealaDocument? saved) == true)
            {
                document = saved!;
            }
            else
            {
                string text = await File.ReadAllTextAsync(path, cancellationToken).ConfigureAwait(false);
                document = new CernealaDocument(path, LanguageSourceText.From(text, capturedState.Revision));
            }

            return new WorkspaceDocumentSnapshot(capturedState, document, owners, Telemetry);
        }
        catch (OperationCanceledException)
        {
            cancelled = true;
            capturedState.Release();
            throw;
        }
        catch
        {
            capturedState.Release();
            throw;
        }
        finally
        {
            Telemetry.Record("parse", started, allocated, cancelled);
        }
    }

    public IReadOnlyList<WorkspaceProjectSummary> GetOwnerSummaries(string uri)
    {
        string path = PathComparer.FromUri(uri);
        lock (stateGate)
        {
            return state.GetOwners(path).Select(owner => owner.Summary).ToArray();
        }
    }

    internal IReadOnlyList<string> GetOpenDocumentUris() => overlays.GetSnapshots()
        .Select(snapshot => new Uri(snapshot.Path).AbsoluteUri)
        .ToArray();

    public async Task<VersionedDocumentResult<T>?> RunDocumentRequestAsync<T>(
        string uri,
        Func<WorkspaceDocumentSnapshot, CancellationToken, Task<T>> operation,
        CancellationToken cancellationToken)
    {
        long queued = Stopwatch.GetTimestamp();
        string path = PathComparer.FromUri(uri);
        WorkspaceState capturedState;
        CancellationToken stateToken;
        lock (stateGate)
        {
            capturedState = state;
            stateToken = stateVersionCancellation.Token;
        }

        CancellationToken documentToken = default;
        long? openVersion = null;
        if (overlays.TryGet(path, out LanguageSourceText? overlay, out CancellationToken versionToken))
        {
            openVersion = overlay!.Version;
            documentToken = versionToken;
        }

        using CancellationTokenSource linked = CancellationTokenSource.CreateLinkedTokenSource(
            cancellationToken,
            stateToken,
            documentToken);
        try
        {
            using WorkspaceDocumentSnapshot snapshot = await GetSnapshotAsync(uri, linked.Token).ConfigureAwait(false);
            Telemetry.RecordElapsed("queue", queued);
            T value = await operation(snapshot, linked.Token).ConfigureAwait(false);
            bool currentState;
            lock (stateGate)
            {
                currentState = ReferenceEquals(state, capturedState);
            }

            bool currentDocument = openVersion is null || overlays.IsCurrent(path, openVersion.Value);
            return currentState && currentDocument
                ? new VersionedDocumentResult<T>(snapshot.Version, value)
                : null;
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            Telemetry.RecordCancellation("request");
            return null;
        }
    }

    public async Task<VersionedWorkspaceResult<T>?> RunWorkspaceRequestAsync<T>(
        Func<IReadOnlyList<CernealaSemanticModel>, CancellationToken, Task<T>> operation,
        CancellationToken cancellationToken)
    {
        long queued = Stopwatch.GetTimestamp();
        WorkspaceState capturedState;
        CancellationToken stateToken;
        lock (stateGate)
        {
            capturedState = state;
            capturedState.Retain();
            stateToken = stateVersionCancellation.Token;
        }

        IReadOnlyList<DocumentOverlaySnapshot> overlaySnapshots = overlays.GetSnapshots();
        CancellationToken[] linkedTokens = new[] { cancellationToken, stateToken }
            .Concat(overlaySnapshots.Select(snapshot => snapshot.VersionToken))
            .ToArray();
        List<CernealaCompilation> temporaryCompilations = new();
        using CancellationTokenSource linked = CancellationTokenSource.CreateLinkedTokenSource(linkedTokens);
        try
        {
            Telemetry.RecordElapsed("queue", queued);
            List<CernealaSemanticModel> models = new();
            foreach (ProjectContext project in capturedState.Projects)
            {
                linked.Token.ThrowIfCancellationRequested();
                CernealaCompilation compilation = project.LanguageCompilation;
                bool temporary = false;
                foreach (DocumentOverlaySnapshot overlay in overlaySnapshots.Where(overlay =>
                    project.DocumentPaths.Contains(overlay.Path, PathComparer.Instance)))
                {
                    CernealaCompilation updated = compilation.WithDocument(
                        new CernealaDocument(overlay.Path, overlay.Text));
                    if (temporary)
                    {
                        compilation.Dispose();
                    }

                    compilation = updated;
                    temporary = true;
                }

                if (temporary)
                {
                    temporaryCompilations.Add(compilation);
                }

                models.AddRange(compilation.Documents.Select(document =>
                    compilation.GetSemanticModel(document.Path, linked.Token)));
            }

            T value = await operation(models, linked.Token).ConfigureAwait(false);
            bool currentState;
            lock (stateGate)
            {
                currentState = ReferenceEquals(state, capturedState);
            }

            bool currentOverlays = overlaySnapshots.All(snapshot =>
                overlays.IsCurrent(snapshot.Path, snapshot.Text.Version));
            return currentState && currentOverlays
                ? new VersionedWorkspaceResult<T>(capturedState.Revision, value)
                : null;
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            Telemetry.RecordCancellation("request");
            return null;
        }
        finally
        {
            foreach (CernealaCompilation compilation in temporaryCompilations)
            {
                compilation.Dispose();
            }

            capturedState.Release();
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (disposed)
        {
            return;
        }

        disposed = true;
        watcher?.Dispose();
        debounceCancellation?.Cancel();
        debounceCancellation?.Dispose();
        lifetimeCancellation.Cancel();
        overlays.Dispose();

        if (initialReloadTask is not null)
        {
            await initialReloadTask.ConfigureAwait(false);
        }

        await reloadGate.WaitAsync().ConfigureAwait(false);
        try
        {
            WorkspaceState previous;
            lock (stateGate)
            {
                previous = state;
            }

            stateVersionCancellation.Cancel();
            stateVersionCancellation.Dispose();
            previous.Release();
        }
        finally
        {
            reloadGate.Release();
            reloadGate.Dispose();
            lifetimeCancellation.Dispose();
        }
    }

    private void StartWatcher()
    {
        if (!configuration.WatchFileSystem ||
            configuration.RootPath is null ||
            !Directory.Exists(configuration.RootPath))
        {
            return;
        }

        watcher = new FileSystemWatcher(configuration.RootPath)
        {
            IncludeSubdirectories = true,
            NotifyFilter = NotifyFilters.FileName |
                NotifyFilters.DirectoryName |
                NotifyFilters.LastWrite |
                NotifyFilters.CreationTime,
            EnableRaisingEvents = true
        };
        watcher.Changed += OnWorkspaceFileChanged;
        watcher.Created += OnWorkspaceFileChanged;
        watcher.Deleted += OnWorkspaceFileChanged;
        watcher.Renamed += OnWorkspaceFileChanged;
    }

    private async Task ReloadInitialWorkspaceAsync()
    {
        try
        {
            await ReloadAsync(lifetimeCancellation.Token).ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (lifetimeCancellation.IsCancellationRequested)
        {
        }
        catch (Exception exception)
        {
            logger.Critical("workspace.initialLoadFailed", ("exceptionType", exception.GetType().FullName));
        }
    }

    private void OnWorkspaceFileChanged(object sender, FileSystemEventArgs args)
    {
        if (!ShouldReload(args.FullPath))
        {
            return;
        }

        CancellationTokenSource next = CancellationTokenSource.CreateLinkedTokenSource(lifetimeCancellation.Token);
        CancellationTokenSource? previous = Interlocked.Exchange(ref debounceCancellation, next);
        previous?.Cancel();
        previous?.Dispose();
        _ = DebouncedReloadAsync(next.Token);
    }

    private async Task DebouncedReloadAsync(CancellationToken cancellationToken)
    {
        try
        {
            await Task.Delay(TimeSpan.FromMilliseconds(150), cancellationToken).ConfigureAwait(false);
            await ReloadAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
        }
        catch (Exception exception)
        {
            logger.Critical("workspace.reloadFailed", ("exceptionType", exception.GetType().FullName));
        }
    }

    private static bool ShouldReload(string path)
    {
        string normalized = path.Replace(Path.AltDirectorySeparatorChar, Path.DirectorySeparatorChar);
        string separator = Path.DirectorySeparatorChar.ToString();
        if (normalized.Contains(separator + "bin" + separator, StringComparison.OrdinalIgnoreCase) ||
            normalized.Contains(separator + "obj" + separator, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        string fileName = Path.GetFileName(path);
        string extension = Path.GetExtension(path);
        return extension.Equals(".cs", StringComparison.OrdinalIgnoreCase) ||
            extension.Equals(".csproj", StringComparison.OrdinalIgnoreCase) ||
            extension.Equals(".props", StringComparison.OrdinalIgnoreCase) ||
            extension.Equals(".targets", StringComparison.OrdinalIgnoreCase) ||
            extension.Equals(".sln", StringComparison.OrdinalIgnoreCase) ||
            extension.Equals(".slnx", StringComparison.OrdinalIgnoreCase) ||
            extension.Equals(".dll", StringComparison.OrdinalIgnoreCase) ||
            fileName.Equals("global.json", StringComparison.OrdinalIgnoreCase);
    }
}
