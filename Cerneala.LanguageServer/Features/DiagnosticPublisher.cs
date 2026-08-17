using System.Collections.Concurrent;
using Cerneala.LanguageServer.Logging;
using Cerneala.LanguageServer.Protocol;
using Cerneala.LanguageServer.Workspace;

namespace Cerneala.LanguageServer.Features;

internal sealed class DiagnosticPublisher : IAsyncDisposable
{
    private static readonly TimeSpan CoalesceDelay = TimeSpan.FromMilliseconds(15);
    private readonly DiagnosticService diagnostics;
    private readonly Func<PublishDiagnosticsParams, Task> publish;
    private readonly IServerLogger logger;
    private readonly CancellationTokenSource lifetimeCancellation = new();
    private readonly ConcurrentDictionary<string, DocumentSession> sessions = new(PathComparer.Instance);
    private int disposed;

    public DiagnosticPublisher(
        DiagnosticService diagnostics,
        Func<PublishDiagnosticsParams, Task> publish,
        IServerLogger logger)
    {
        this.diagnostics = diagnostics;
        this.publish = publish;
        this.logger = logger;
    }

    public void Schedule(string uri) => Schedule(uri, clear: false);

    public void Clear(string uri) => Schedule(uri, clear: true);

    public async ValueTask DisposeAsync()
    {
        if (Interlocked.Exchange(ref disposed, 1) != 0)
        {
            return;
        }

        lifetimeCancellation.Cancel();
        Task[] tasks = sessions.Values.Select(session => session.CancelAndGetLastTask()).ToArray();
        try
        {
            await Task.WhenAll(tasks).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
        }
        finally
        {
            foreach (DocumentSession session in sessions.Values)
            {
                session.Dispose();
            }

            sessions.Clear();
            lifetimeCancellation.Dispose();
        }
    }

    private void Schedule(string uri, bool clear)
    {
        ObjectDisposedException.ThrowIf(Volatile.Read(ref disposed) != 0, this);
        string path = PathComparer.FromUri(uri);
        DocumentSession session = sessions.GetOrAdd(path, _ => new DocumentSession());
        (long generation, CancellationToken token) = session.Begin(lifetimeCancellation.Token);
        Task task = RunAsync(session, generation, uri, clear, token);
        session.SetLastTask(task);
    }

    private async Task RunAsync(
        DocumentSession session,
        long generation,
        string uri,
        bool clear,
        CancellationToken cancellationToken)
    {
        try
        {
            await Task.Delay(CoalesceDelay, cancellationToken).ConfigureAwait(false);
            await session.Serial.WaitAsync(cancellationToken).ConfigureAwait(false);
            try
            {
                if (!session.IsCurrent(generation))
                {
                    return;
                }

                PublishDiagnosticsParams notification;
                if (clear)
                {
                    notification = new PublishDiagnosticsParams
                    {
                        Uri = uri,
                        Version = null,
                        Diagnostics = []
                    };
                }
                else
                {
                    VersionedDocumentResult<IReadOnlyList<LspDiagnostic>>? result =
                        await diagnostics.AnalyzeAsync(uri, cancellationToken).ConfigureAwait(false);
                    if (result is null || !session.IsCurrent(generation))
                    {
                        return;
                    }

                    notification = new PublishDiagnosticsParams
                    {
                        Uri = uri,
                        Version = result.Version,
                        Diagnostics = result.Value.ToArray()
                    };
                }

                await publish(notification).ConfigureAwait(false);
            }
            finally
            {
                session.Serial.Release();
            }
        }
        catch (OperationCanceledException) when (!lifetimeCancellation.IsCancellationRequested)
        {
        }
        catch (OperationCanceledException)
        {
        }
        catch (Exception exception)
        {
            logger.Critical("diagnostics.publishFailed", ("exceptionType", exception.GetType().FullName));
        }
    }

    private sealed class DocumentSession : IDisposable
    {
        private readonly object gate = new();
        private long generation;
        private CancellationTokenSource? requestCancellation;
        private Task lastTask = Task.CompletedTask;

        public SemaphoreSlim Serial { get; } = new(1, 1);

        public (long Generation, CancellationToken Token) Begin(CancellationToken lifetimeToken)
        {
            lock (gate)
            {
                requestCancellation?.Cancel();
                requestCancellation?.Dispose();
                requestCancellation = CancellationTokenSource.CreateLinkedTokenSource(lifetimeToken);
                return (++generation, requestCancellation.Token);
            }
        }

        public bool IsCurrent(long candidate)
        {
            lock (gate)
            {
                return generation == candidate && requestCancellation?.IsCancellationRequested == false;
            }
        }

        public void SetLastTask(Task task)
        {
            lock (gate)
            {
                lastTask = task;
            }
        }

        public Task CancelAndGetLastTask()
        {
            lock (gate)
            {
                requestCancellation?.Cancel();
                return lastTask;
            }
        }

        public void Dispose()
        {
            lock (gate)
            {
                requestCancellation?.Dispose();
                requestCancellation = null;
            }

            Serial.Dispose();
        }
    }
}
