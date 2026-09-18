namespace Cerneala.UI.Controls;

// Cancellation belongs to the request, not to the rendering host. In particular,
// late retirement must not require pumping a context that has already closed.
internal class ScenePreparationRequest2D
{
    private readonly object gate = new();
    private readonly CancellationTokenSource cancellation = new();
    private bool completed;
    private int cancelCalls;

    internal ScenePreparationRequest2D(SceneSimulationContext2D context, long version)
    {
        Context = context;
        Version = version;
        Token = cancellation.Token;
    }

    internal SceneSimulationContext2D Context { get; }
    internal long Version { get; }
    internal CancellationToken Token { get; }

    internal void Cancel()
    {
        lock (gate)
        {
            if (completed) { return; }
            cancelCalls++;
        }
        try { cancellation.Cancel(); }
        finally
        {
            bool release;
            lock (gate) { release = --cancelCalls == 0 && completed; }
            if (release) { cancellation.Dispose(); }
        }
    }

    internal void Complete()
    {
        bool release;
        lock (gate)
        {
            if (completed) { return; }
            completed = true;
            release = cancelCalls == 0;
        }
        if (release) { cancellation.Dispose(); }
    }
}
