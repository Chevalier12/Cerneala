using Cerneala.Timbre;

namespace Cerneala.Platforms.Sdl3;

internal readonly record struct SdlTimbreOutputDiagnostics(
    int OpenCount,
    int CloseCount,
    bool IsOpen,
    bool IsTerminated,
    uint Device,
    SdlAudioFormat DeviceFormat,
    int DeviceSampleFrames,
    long Requests,
    long RequestedBytes,
    long StarvedRequests,
    long StarvedBytes,
    long FramesSubmitted,
    int QueueHighWaterFrames,
    long PutFailures,
    long Flushes,
    long DevicesLost);

// The platform-wide sink for the final Timbre mix: one SDL stream bound to the
// default playback device, opened lazily by the runtime's mixer and closed by
// it or by platform termination. The SDL request callback only counts and
// signals the mixer; all PCM and every flush come from the mixer thread.
internal sealed class SdlTimbreOutput : ITimbreOutput
{
    private const int BytesPerFrame = TimbreRuntime.ChannelCount * sizeof(float);

    private static readonly SdlAudioFormat MixFormat = new(
        SdlAudioFormat.Float32LittleEndian,
        TimbreRuntime.ChannelCount,
        TimbreRuntime.SampleRate);

    private readonly object sync = new();
    private readonly ISdlAudioApi api;
    private readonly SdlAudioRequest onRequest;
    private ITimbreOutputClient? client;
    private nint stream;
    private uint device;
    private bool terminated;

    // Guarded by sync.
    private int openCount;
    private int closeCount;
    private SdlAudioFormat deviceFormat;
    private int deviceSampleFrames;
    private long framesSubmitted;
    private int queueHighWaterFrames;
    private long putFailures;
    private long flushes;
    private bool submittedSinceQuery;
    private int lastQueuedFrames;

    // Written from SDL threads.
    private long requests;
    private long requestedBytes;
    private long starvedRequests;
    private long starvedBytes;
    private long devicesLost;
    private int deviceShort;

    public SdlTimbreOutput(ISdlAudioApi api)
    {
        this.api = api ?? throw new ArgumentNullException(nameof(api));
        onRequest = OnRequest;
    }

    public int QueuedFrames
    {
        get
        {
            lock (sync)
            {
                nint current = RequireStreamLocked();
                int bytes = api.GetStreamQueuedBytes(current);
                if (bytes < 0)
                {
                    throw CreateError("SDL audio queue query");
                }

                // The device ran short and the mixer produced nothing since its last
                // check: the queued PCM is all there is for now. A resampling stream
                // holds its last input frames back until it is flushed, so without
                // this the tail would never play and completion would never drain.
                bool deviceRanShort = Interlocked.Exchange(ref deviceShort, 0) != 0;
                if (deviceRanShort && !submittedSinceQuery && bytes > 0)
                {
                    if (!api.FlushStream(current))
                    {
                        throw CreateError("SDL audio stream flush");
                    }

                    flushes++;
                }

                submittedSinceQuery = false;

                lastQueuedFrames = bytes / BytesPerFrame;
                return lastQueuedFrames;
            }
        }
    }

    public void Open(ITimbreOutputClient client)
    {
        ArgumentNullException.ThrowIfNull(client);
        lock (sync)
        {
            if (terminated)
            {
                throw new TimbreException(
                    TimbreErrorKind.DeviceUnavailable,
                    "The SDL platform has shut down its audio output.");
            }

            if (stream != 0)
            {
                throw new InvalidOperationException("The SDL audio output is already open.");
            }

            if (!api.InitializeAudio())
            {
                throw CreateError("SDL audio initialization");
            }

            nint opened = api.OpenDefaultPlaybackStream(MixFormat, onRequest);
            if (opened == 0)
            {
                TimbreException error = CreateError("SDL default playback device open");
                api.QuitAudio();
                throw error;
            }

            uint openedDevice = api.GetStreamDevice(opened);
            api.GetDeviceFormat(openedDevice, out deviceFormat, out deviceSampleFrames);
            // Published before resume: the first device request already signals.
            Volatile.Write(ref this.client, client);
            if (!api.ResumeStreamDevice(opened))
            {
                TimbreException error = CreateError("SDL audio device resume");
                Volatile.Write(ref this.client, null);
                api.DestroyStream(opened);
                api.QuitAudio();
                throw error;
            }

            stream = opened;
            Volatile.Write(ref device, openedDevice);
            openCount++;
        }
    }

    public void Submit(ReadOnlySpan<float> samples)
    {
        if (samples.Length % TimbreRuntime.ChannelCount != 0)
        {
            throw new ArgumentException("The output accepts complete stereo frames only.", nameof(samples));
        }

        lock (sync)
        {
            if (!api.PutStreamData(RequireStreamLocked(), samples))
            {
                putFailures++;
                throw CreateError("SDL audio stream put");
            }

            int frames = samples.Length / TimbreRuntime.ChannelCount;
            framesSubmitted += frames;
            // Upper bound of the software queue right after the put.
            lastQueuedFrames += frames;
            queueHighWaterFrames = Math.Max(queueHighWaterFrames, lastQueuedFrames);
            submittedSinceQuery = true;
        }
    }

    public void Close()
    {
        lock (sync)
        {
            CloseLocked();
        }
    }

    // Platform shutdown: releases the device before SDL quits and rejects every
    // later open. The runtime is told the device is gone so it stops pushing.
    public void Terminate()
    {
        ITimbreOutputClient? lost;
        lock (sync)
        {
            if (terminated)
            {
                return;
            }

            terminated = true;
            lost = Volatile.Read(ref client);
            CloseLocked();
        }

        lost?.NotifyDeviceLost(new TimbreException(
            TimbreErrorKind.DeviceUnavailable,
            "The SDL platform shut down its audio output."));
    }

    // Called from whichever SDL thread publishes the removal event.
    public void HandleDeviceRemoved(uint removed)
    {
        if (removed == 0 || removed != Volatile.Read(ref device) ||
            Volatile.Read(ref client) is not { } current)
        {
            return;
        }

        Interlocked.Increment(ref devicesLost);
        current.NotifyDeviceLost(new TimbreException(
            TimbreErrorKind.DeviceUnavailable,
            "The SDL default playback device was removed."));
    }

    public SdlTimbreOutputDiagnostics GetDiagnostics()
    {
        lock (sync)
        {
            return new SdlTimbreOutputDiagnostics(
                openCount,
                closeCount,
                stream != 0,
                terminated,
                device,
                deviceFormat,
                deviceSampleFrames,
                Interlocked.Read(ref requests),
                Interlocked.Read(ref requestedBytes),
                Interlocked.Read(ref starvedRequests),
                Interlocked.Read(ref starvedBytes),
                framesSubmitted,
                queueHighWaterFrames,
                putFailures,
                flushes,
                Interlocked.Read(ref devicesLost));
        }
    }

    // SDL audio thread, stream lock held: count and signal only.
    private void OnRequest(int additionalBytes, int totalBytes)
    {
        Interlocked.Increment(ref requests);
        Interlocked.Add(ref requestedBytes, totalBytes);
        if (additionalBytes > 0)
        {
            Interlocked.Increment(ref starvedRequests);
            Interlocked.Add(ref starvedBytes, additionalBytes);
            Volatile.Write(ref deviceShort, 1);
        }

        Volatile.Read(ref client)?.NotifyCapacityAvailable();
    }

    private void CloseLocked()
    {
        if (stream == 0)
        {
            return;
        }

        Volatile.Write(ref client, null);
        Volatile.Write(ref device, 0u);
        Volatile.Write(ref deviceShort, 0);
        submittedSinceQuery = false;
        lastQueuedFrames = 0;
        // Waits for a request already running; none runs afterwards.
        api.DestroyStream(stream);
        stream = 0;
        api.QuitAudio();
        closeCount++;
    }

    private nint RequireStreamLocked() =>
        stream != 0 ? stream : throw new InvalidOperationException("The SDL audio output is not open.");

    private TimbreException CreateError(string operation)
    {
        string error = api.GetError();
        return new TimbreException(
            TimbreErrorKind.DeviceUnavailable,
            string.IsNullOrWhiteSpace(error)
                ? $"{operation} failed without an SDL error message."
                : $"{operation} failed: {error}");
    }
}
