using System.Windows.Threading;
using Cerneala.UI.Elements;
using Cerneala.UI.Relay;

namespace Cerneala.WpfDispatcherBenchmarks;

internal sealed class CernealaRelayHarness : IDisposable
{
    private readonly AutoResetEvent request = new(false);
    private readonly ManualResetEventSlim ready = new(false);
    private readonly ManualResetEventSlim completed = new(false);
    private readonly Thread ownerThread;
    private UIRoot? root;
    private Exception? ownerFailure;
    private int command;

    public CernealaRelayHarness(int maxCallbacksPerUpdate = 100_000)
    {
        ownerThread = new Thread(OwnerThreadMain)
        {
            IsBackground = true,
            Name = "Cerneala benchmark owner"
        };
        ownerThread.SetApartmentState(ApartmentState.STA);
        ownerThread.Start(maxCallbacksPerUpdate);
        ready.Wait();
        ThrowIfOwnerFailed();
    }

    public int OwnerThreadId { get; private set; }

    public int PendingCount => RequireRoot().Relay.PendingCount;

    public void Post(Action callback)
    {
        RequireRoot().Relay.Post(callback);
    }

    public Task InvokeAsync(Action callback)
    {
        return RequireRoot().Relay.InvokeAsync(callback);
    }

    public void Drain()
    {
        completed.Reset();
        Volatile.Write(ref command, 1);
        request.Set();
        completed.Wait();
        ThrowIfOwnerFailed();
    }

    public void Dispose()
    {
        Volatile.Write(ref command, 2);
        request.Set();
        ownerThread.Join();
        request.Dispose();
        ready.Dispose();
        completed.Dispose();
    }

    private void OwnerThreadMain(object? state)
    {
        try
        {
            OwnerThreadId = Environment.CurrentManagedThreadId;
            root = new UIRoot(relayOptions: new UiRelayOptions
            {
                MaxCallbacksPerUpdate = (int)state!
            });
            root.ProcessFrame();
            ready.Set();

            while (true)
            {
                request.WaitOne();
                int currentCommand = Volatile.Read(ref command);
                if (currentCommand == 2)
                {
                    return;
                }

                root.ProcessFrame();
                completed.Set();
            }
        }
        catch (Exception exception)
        {
            ownerFailure = exception;
            ready.Set();
            completed.Set();
        }
    }

    private UIRoot RequireRoot()
    {
        ThrowIfOwnerFailed();
        return root ?? throw new InvalidOperationException("The Cerneala owner thread is not ready.");
    }

    private void ThrowIfOwnerFailed()
    {
        if (ownerFailure is not null)
        {
            throw new InvalidOperationException("The Cerneala owner thread failed.", ownerFailure);
        }
    }
}

internal sealed class WpfDispatcherHarness : IDisposable
{
    private readonly AutoResetEvent request = new(false);
    private readonly ManualResetEventSlim ready = new(false);
    private readonly ManualResetEventSlim completed = new(false);
    private readonly Thread ownerThread;
    private Dispatcher? dispatcher;
    private DispatcherFrame? drainFrame;
    private Exception? ownerFailure;
    private int command;

    public WpfDispatcherHarness()
    {
        ownerThread = new Thread(OwnerThreadMain)
        {
            IsBackground = true,
            Name = "WPF Dispatcher benchmark owner"
        };
        ownerThread.SetApartmentState(ApartmentState.STA);
        ownerThread.Start();
        ready.Wait();
        ThrowIfOwnerFailed();
    }

    public int OwnerThreadId { get; private set; }

    public void BeginInvoke(Action callback)
    {
        _ = RequireDispatcher().BeginInvoke(DispatcherPriority.Normal, callback);
    }

    public Task InvokeAsync(Action callback)
    {
        return RequireDispatcher().InvokeAsync(callback, DispatcherPriority.Normal).Task;
    }

    public void EnqueueDrainBatch(int callbackCount, Action callback)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(callbackCount, 1);
        for (int index = 1; index < callbackCount; index++)
        {
            BeginInvoke(callback);
        }

        BeginInvoke(() =>
        {
            callback();
            drainFrame!.Continue = false;
        });
    }

    public void DrainPreparedBatch()
    {
        completed.Reset();
        Volatile.Write(ref command, 1);
        request.Set();
        completed.Wait();
        ThrowIfOwnerFailed();
    }

    public void Dispose()
    {
        Volatile.Write(ref command, 2);
        request.Set();
        ownerThread.Join();
        request.Dispose();
        ready.Dispose();
        completed.Dispose();
    }

    private void OwnerThreadMain()
    {
        try
        {
            OwnerThreadId = Environment.CurrentManagedThreadId;
            dispatcher = Dispatcher.CurrentDispatcher;
            ready.Set();

            while (true)
            {
                request.WaitOne();
                int currentCommand = Volatile.Read(ref command);
                if (currentCommand == 2)
                {
                    dispatcher.InvokeShutdown();
                    return;
                }

                drainFrame = new DispatcherFrame();
                Dispatcher.PushFrame(drainFrame);
                drainFrame = null;
                completed.Set();
            }
        }
        catch (Exception exception)
        {
            ownerFailure = exception;
            ready.Set();
            completed.Set();
        }
    }

    private Dispatcher RequireDispatcher()
    {
        ThrowIfOwnerFailed();
        return dispatcher ?? throw new InvalidOperationException("The WPF Dispatcher thread is not ready.");
    }

    private void ThrowIfOwnerFailed()
    {
        if (ownerFailure is not null)
        {
            throw new InvalidOperationException("The WPF Dispatcher owner thread failed.", ownerFailure);
        }
    }
}
