using Cerneala.Backends.SdlGpu;
using Cerneala.Drawing;
using Cerneala.UI.Resources;

namespace Cerneala.Tests.SdlGpu;

// Hold the real asynchronous decoder at a deterministic boundary. A scene/UI
// consumer invoking the synchronous path is a contract failure, not a fallback.
internal sealed class DelayedSdlImageLoader : IAsyncImageLoader
{
    private readonly SdlGpuImageLoader decoder = new();
    private TaskCompletionSource<bool>? pending;
    internal int AsyncLoads;
    internal int SyncLoads;
    internal void Begin() => pending = new(TaskCreationOptions.RunContinuationsAsynchronously);
    internal void Complete() => pending!.SetResult(true);
    internal void Fail(Exception failure) => pending!.SetException(failure);
    internal void Cancel() => pending?.TrySetCanceled();
    public IDrawImage Load(string path)
    {
        Interlocked.Increment(ref SyncLoads);
        throw new InvalidOperationException("Presentation must not invoke the synchronous decoder.");
    }
    public async ValueTask<IDrawImage> LoadAsync(string path, CancellationToken cancellationToken = default)
    {
        Interlocked.Increment(ref AsyncLoads);
        await pending!.Task.WaitAsync(TimeSpan.FromSeconds(10), cancellationToken).ConfigureAwait(false);
        return await decoder.LoadAsync(path, cancellationToken).ConfigureAwait(false);
    }
}
