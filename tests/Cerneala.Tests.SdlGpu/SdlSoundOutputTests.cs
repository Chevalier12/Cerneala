using Cerneala.Platforms.Sdl3;
using Cerneala.Timbre;

namespace Cerneala.Tests.SdlGpu;

public sealed class SdlSoundOutputTests
{
    private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(10);

    [Fact]
    public void OpenInitializesOpensAndResumesAndCloseDestroysBeforeQuitting()
    {
        FakeSdlAudioApi api = new();
        SdlSoundOutput output = new(api);
        RecordingClient client = new();

        output.Open(client);

        Assert.Equal(["init", "open", "resume"], api.Operations);
        Assert.Equal(new SdlAudioFormat(SdlAudioFormat.Float32LittleEndian, 2, 48000), api.OpenedFormat);
        Assert.Equal(1, api.AudioReferences);
        Assert.True(output.GetDiagnostics().IsOpen);

        output.Close();
        output.Close();

        Assert.Equal(["init", "open", "resume", "destroy", "quit"], api.Operations);
        Assert.Equal(0, api.AudioReferences);
        SdlSoundOutputDiagnostics diagnostics = output.GetDiagnostics();
        Assert.Equal(1, diagnostics.OpenCount);
        Assert.Equal(1, diagnostics.CloseCount);
        Assert.False(diagnostics.IsOpen);

        output.Open(client);
        output.Close();

        Assert.Equal(2, output.GetDiagnostics().OpenCount);
        Assert.Equal(0, api.AudioReferences);
    }

    [Theory]
    [InlineData("init", new[] { "init" })]
    [InlineData("open", new[] { "init", "open", "quit" })]
    [InlineData("resume", new[] { "init", "open", "resume", "destroy", "quit" })]
    public void AFailingOpenStepReportsTheSdlErrorAndUnwindsWhatItAcquired(string step, string[] expected)
    {
        FakeSdlAudioApi api = new()
        {
            InitializeResult = step != "init",
            OpenResult = step != "open",
            ResumeResult = step != "resume",
            Error = $"{step} refused"
        };
        SdlSoundOutput output = new(api);
        RecordingClient client = new();

        SoundException exception = Assert.Throws<SoundException>(() => output.Open(client));

        Assert.Equal(SoundErrorKind.DeviceUnavailable, exception.Kind);
        Assert.Contains($"{step} refused", exception.Message, StringComparison.Ordinal);
        Assert.Equal(expected, api.Operations);
        Assert.Equal(0, api.AudioReferences);
        Assert.False(api.IsStreamOpen);
        Assert.False(output.GetDiagnostics().IsOpen);
        Assert.Equal(0, client.CapacityNotifications);

        api.InitializeResult = api.OpenResult = api.ResumeResult = true;
        output.Open(client);
        Assert.True(output.GetDiagnostics().IsOpen);
        output.Close();
        Assert.Equal(0, api.AudioReferences);
    }

    [Fact]
    public void OpeningAnOpenOutputIsRejectedWithoutTouchingTheStream()
    {
        FakeSdlAudioApi api = new();
        SdlSoundOutput output = new(api);
        output.Open(new RecordingClient());

        Assert.Throws<InvalidOperationException>(() => output.Open(new RecordingClient()));

        Assert.Equal(["init", "open", "resume"], api.Operations);
        output.Close();
    }

    [Fact]
    public void StreamOperationsRequireAnOpenOutput()
    {
        SdlSoundOutput output = new(new FakeSdlAudioApi());

        Assert.Throws<InvalidOperationException>(() => output.QueuedFrames);
        Assert.Throws<InvalidOperationException>(() => output.Submit(new float[960]));
    }

    [Fact]
    public void SubmitQueuesCompleteFramesAndQueuedFramesConvertsInputBytes()
    {
        FakeSdlAudioApi api = new();
        SdlSoundOutput output = new(api);
        output.Open(new RecordingClient());
        float[] block = Enumerable.Range(0, 960).Select(index => index / 960f).ToArray();

        output.Submit(block);
        Assert.Equal(480, output.QueuedFrames);
        Assert.Equal(block, api.Submitted);

        Assert.Equal(1000, api.Pull(1000));
        Assert.Equal(355, output.QueuedFrames);

        SdlSoundOutputDiagnostics diagnostics = output.GetDiagnostics();
        Assert.Equal(480, diagnostics.FramesSubmitted);
        Assert.Equal(480, diagnostics.QueueHighWaterFrames);
        Assert.Throws<ArgumentException>(() => output.Submit(new float[3]));
        output.Close();
    }

    [Fact]
    public void APutFailureIsReportedWithoutCountingTheBlock()
    {
        FakeSdlAudioApi api = new() { Error = "put refused" };
        SdlSoundOutput output = new(api);
        output.Open(new RecordingClient());
        api.PutResult = false;

        SoundException exception = Assert.Throws<SoundException>(() => output.Submit(new float[960]));

        Assert.Equal(SoundErrorKind.DeviceUnavailable, exception.Kind);
        Assert.Contains("put refused", exception.Message, StringComparison.Ordinal);
        SdlSoundOutputDiagnostics diagnostics = output.GetDiagnostics();
        Assert.Equal(0, diagnostics.FramesSubmitted);
        Assert.Equal(1, diagnostics.PutFailures);
        output.Close();
    }

    [Fact]
    public void AQueuedQueryFailureIsReported()
    {
        FakeSdlAudioApi api = new() { Error = "queued refused" };
        SdlSoundOutput output = new(api);
        output.Open(new RecordingClient());
        api.QueuedFails = true;

        SoundException exception = Assert.Throws<SoundException>(() => output.QueuedFrames);

        Assert.Contains("queued refused", exception.Message, StringComparison.Ordinal);
        output.Close();
    }

    [Fact]
    public void RequestsOfAnySizeFromAnyThreadOnlyNotifyTheClient()
    {
        FakeSdlAudioApi api = new();
        SdlSoundOutput output = new(api);
        RecordingClient client = new();
        output.Open(client);
        output.Submit(new float[960]);
        int[] sizes = [7, 3840, 8192, 1, 3840];
        using Barrier barrier = new(sizes.Length);
        Thread[] threads = sizes
            .Select(size => new Thread(() =>
            {
                barrier.SignalAndWait();
                api.Pull(size);
            }))
            .ToArray();

        foreach (Thread thread in threads)
        {
            thread.Start();
        }

        foreach (Thread thread in threads)
        {
            Assert.True(thread.Join(Timeout));
        }

        Assert.Equal(sizes.Length, client.CapacityNotifications);
        Assert.Equal(sizes.Length, client.NotifyingThreads.Count);
        Assert.DoesNotContain(Environment.CurrentManagedThreadId, client.NotifyingThreads);
        SdlSoundOutputDiagnostics diagnostics = output.GetDiagnostics();
        Assert.Equal(sizes.Length, diagnostics.Requests);
        Assert.Equal(sizes.Sum(), diagnostics.RequestedBytes);
        Assert.InRange(diagnostics.StarvedRequests, 1, sizes.Length);
        Assert.True(diagnostics.StarvedBytes > 0);
        // Requests never put or clear PCM themselves.
        Assert.Equal(960, api.Submitted.Length);
        output.Close();
    }

    [Fact]
    public void ARequestWithNothingMissingStillSignalsCapacity()
    {
        FakeSdlAudioApi api = new();
        SdlSoundOutput output = new(api);
        RecordingClient client = new();
        output.Open(client);
        output.Submit(new float[960]);

        api.Pull(8);

        Assert.Equal(1, client.CapacityNotifications);
        Assert.Equal(0, output.GetDiagnostics().StarvedRequests);
        output.Close();
    }

    [Fact]
    public void ARequestAfterCloseNeverReachesTheClient()
    {
        FakeSdlAudioApi api = new();
        SdlSoundOutput output = new(api);
        RecordingClient client = new();
        output.Open(client);
        output.Close();

        api.Pull(3840);

        Assert.Equal(0, client.CapacityNotifications);
        Assert.Equal(1, api.LateRequests);
    }

    [Fact]
    public void CloseWaitsForARunningRequestWithoutDeadlockAndNothingRunsAfterIt()
    {
        FakeSdlAudioApi api = new();
        SdlSoundOutput output = new(api);
        using ManualResetEventSlim release = new(false);
        using ManualResetEventSlim destroyEntered = new(false);
        RecordingClient client = new() { Block = release };
        output.Open(client);
        output.Submit(new float[960]);
        api.DestroyEntered = destroyEntered.Set;

        Thread device = new(() => api.Pull(3840));
        device.Start();
        Assert.True(client.Entered.Wait(Timeout));
        Thread closer = new(output.Close);
        closer.Start();
        Assert.True(destroyEntered.Wait(Timeout));

        Assert.DoesNotContain("destroy", api.Operations);
        release.Set();
        Assert.True(device.Join(Timeout));
        Assert.True(closer.Join(Timeout));

        Assert.Equal(["init", "open", "resume", "destroy", "quit"], api.Operations);
        api.Pull(3840);
        Assert.Equal(1, client.CapacityNotifications);
        Assert.Equal(0, api.AudioReferences);
    }

    [Fact]
    public void TerminationReleasesTheDeviceReportsItLostAndRejectsLaterOpens()
    {
        FakeSdlAudioApi api = new();
        SdlSoundOutput output = new(api);
        RecordingClient client = new();
        output.Open(client);

        output.Terminate();
        output.Terminate();
        output.Close();

        Assert.Equal(["init", "open", "resume", "destroy", "quit"], api.Operations);
        SoundException lost = Assert.IsType<SoundException>(Assert.Single(client.Lost));
        Assert.Equal(SoundErrorKind.DeviceUnavailable, lost.Kind);
        SoundException rejected = Assert.Throws<SoundException>(() => output.Open(client));
        Assert.Equal(SoundErrorKind.DeviceUnavailable, rejected.Kind);
        Assert.Equal(["init", "open", "resume", "destroy", "quit"], api.Operations);
        Assert.True(output.GetDiagnostics().IsTerminated);
        Assert.Throws<InvalidOperationException>(() => output.Submit(new float[960]));
    }

    [Fact]
    public void TerminationBeforeAnyOpenTouchesNoNativeState()
    {
        FakeSdlAudioApi api = new();
        SdlSoundOutput output = new(api);

        output.Terminate();

        Assert.Empty(api.Operations);
        Assert.Throws<SoundException>(() => output.Open(new RecordingClient()));
        Assert.Empty(api.Operations);
    }

    [Fact]
    public void RemovalOfTheOpenDeviceIsReportedAsLostAndOtherDevicesAreIgnored()
    {
        FakeSdlAudioApi api = new() { Device = 41 };
        SdlSoundOutput output = new(api);
        RecordingClient client = new();
        output.Open(client);

        output.HandleDeviceRemoved(40);
        Assert.Empty(client.Lost);

        output.HandleDeviceRemoved(41);
        SoundException lost = Assert.IsType<SoundException>(Assert.Single(client.Lost));
        Assert.Equal(SoundErrorKind.DeviceUnavailable, lost.Kind);
        Assert.Equal(1, output.GetDiagnostics().DevicesLost);

        output.Close();
        output.HandleDeviceRemoved(41);
        Assert.Single(client.Lost);
        // A loss is reported, never retried: the device stays as the runtime left it.
        Assert.Equal(["init", "open", "resume", "destroy", "quit"], api.Operations);
    }

    [Fact]
    public void DiagnosticsReportTheNegotiatedDeviceFormat()
    {
        FakeSdlAudioApi api = new()
        {
            Device = 9,
            DeviceFormat = new SdlAudioFormat(0x8010, 2, 44100),
            DeviceSampleFrames = 1024
        };
        SdlSoundOutput output = new(api);
        output.Open(new RecordingClient());

        SdlSoundOutputDiagnostics diagnostics = output.GetDiagnostics();

        Assert.Equal(9u, diagnostics.Device);
        Assert.Equal(new SdlAudioFormat(0x8010, 2, 44100), diagnostics.DeviceFormat);
        Assert.Equal(1024, diagnostics.DeviceSampleFrames);
        output.Close();
        Assert.Equal(0u, output.GetDiagnostics().Device);
    }

    internal sealed class RecordingClient : ISoundOutputClient
    {
        private int capacityNotifications;

        public ManualResetEventSlim Entered { get; } = new(false);

        public ManualResetEventSlim? Block { get; init; }

        public int CapacityNotifications => Volatile.Read(ref capacityNotifications);

        public HashSet<int> NotifyingThreads { get; } = [];

        public List<Exception?> Lost { get; } = [];

        public void NotifyCapacityAvailable()
        {
            Interlocked.Increment(ref capacityNotifications);
            lock (NotifyingThreads)
            {
                NotifyingThreads.Add(Environment.CurrentManagedThreadId);
            }

            Entered.Set();
            Block?.Wait();
        }

        public void NotifyDeviceLost(Exception? error)
        {
            lock (Lost)
            {
                Lost.Add(error);
            }
        }
    }
}
