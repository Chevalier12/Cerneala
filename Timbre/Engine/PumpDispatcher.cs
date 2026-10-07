namespace Cerneala.Timbre.Engine;

// Moves streaming-pump wakeups and cancellation off the mixer thread. The mixer
// only sets per-feed flags and an event; this thread then completes the pump's
// awaited signal (queuing its continuation on the thread pool) or runs the
// feed's cancellation callbacks, so neither allocates nor runs reader code on
// the realtime path.
internal sealed class PumpDispatcher : IDisposable
{
    private readonly object gate = new();
    private readonly List<StreamingFeed> feeds = [];
    private readonly AutoResetEvent signal = new(initialState: false);
    private StreamingFeed[] scratch = new StreamingFeed[8];
    private Thread? thread;
    private volatile bool disposed;

    internal void Register(StreamingFeed feed)
    {
        lock (gate)
        {
            ObjectDisposedException.ThrowIf(disposed, this);
            feeds.Add(feed);
            if (thread is null)
            {
                thread = new Thread(Run) { IsBackground = true, Name = "Timbre pump dispatcher" };
                thread.Start();
            }
        }
    }

    internal void Unregister(StreamingFeed feed)
    {
        lock (gate)
        {
            feeds.Remove(feed);
        }
    }

    // Realtime-safe: an event set, no allocation or lock. The feed has already
    // set its pending flag; once the dispatcher is disposed (its final pass may
    // have run) the request is dispatched inline instead of being lost.
    internal void Signal(StreamingFeed feed)
    {
        if (!disposed)
        {
            signal.Set();
            if (!disposed)
            {
                return;
            }
        }

        feed.Dispatch();
    }

    public void Dispose()
    {
        Thread? running;
        lock (gate)
        {
            if (disposed)
            {
                return;
            }

            disposed = true;
            running = thread;
        }

        signal.Set();
        running?.Join();
    }

    private void Run()
    {
        while (true)
        {
            signal.WaitOne();
            bool exiting = disposed;
            int count;
            lock (gate)
            {
                if (scratch.Length < feeds.Count)
                {
                    scratch = new StreamingFeed[feeds.Count * 2];
                }

                feeds.CopyTo(scratch);
                count = feeds.Count;
            }

            // Outside the lock: cancellation callbacks may unregister feeds.
            for (int index = 0; index < count; index++)
            {
                scratch[index].Dispatch();
            }

            Array.Clear(scratch, 0, count);

            if (exiting)
            {
                return;
            }
        }
    }
}
