using Cerneala.Platforms.Sdl3;

namespace Cerneala.Tests.SdlGpu;

// Models the SDL audio seam: one stream with a queue of input samples, device
// requests driven by the test from any thread, and SDL's stream lock — a
// request runs its handler under the lock, and Put/Queued/Destroy wait for it.
internal sealed class FakeSdlAudioApi : ISdlAudioApi
{
    private readonly object state = new();
    private readonly object streamLock = new();
    private readonly List<(string Name, int ThreadId)> operations = [];
    private readonly Queue<float> queued = new();
    private readonly List<float> submitted = [];
    private readonly List<float> consumed = [];
    private SdlAudioRequest? handler;
    private nint stream;
    private nint nextStream = 0x100;
    private int audioReferences;
    private int queueHighWaterBytes;
    private long lateRequests;
    private int releasableSamples;
    private int flushCount;

    public bool InitializeResult { get; set; } = true;

    public bool OpenResult { get; set; } = true;

    public bool ResumeResult { get; set; } = true;

    public bool PutResult { get; set; } = true;

    public bool QueuedFails { get; set; }

    // Models a resampling stream: this many trailing input bytes stay queued
    // until more input arrives or the stream is flushed.
    public int HeldBackBytes { get; set; }

    public int FlushCount
    {
        get
        {
            lock (state)
            {
                return flushCount;
            }
        }
    }

    public string Error { get; set; } = "fake SDL audio error";

    public uint Device { get; set; } = 7;

    public SdlAudioFormat DeviceFormat { get; set; } = new(SdlAudioFormat.Float32LittleEndian, 2, 48000);

    public int DeviceSampleFrames { get; set; } = 480;

    public SdlAudioFormat? OpenedFormat { get; private set; }

    // Invoked after an operation is recorded, on the calling thread.
    public Action<string>? OperationRecorded { get; set; }

    // Invoked when DestroyStream is entered, before it waits for the stream lock.
    public Action? DestroyEntered { get; set; }

    public bool IsStreamOpen
    {
        get
        {
            lock (state)
            {
                return stream != 0;
            }
        }
    }

    public int AudioReferences
    {
        get
        {
            lock (state)
            {
                return audioReferences;
            }
        }
    }

    public int QueuedBytes
    {
        get
        {
            lock (state)
            {
                return queued.Count * sizeof(float);
            }
        }
    }

    public int QueueHighWaterBytes
    {
        get
        {
            lock (state)
            {
                return queueHighWaterBytes;
            }
        }
    }

    public long LateRequests => Interlocked.Read(ref lateRequests);

    public string[] Operations
    {
        get
        {
            lock (state)
            {
                return operations.Select(operation => operation.Name).ToArray();
            }
        }
    }

    public int[] ThreadsOf(string name)
    {
        lock (state)
        {
            return operations.Where(operation => operation.Name == name).Select(operation => operation.ThreadId).ToArray();
        }
    }

    public float[] Submitted
    {
        get
        {
            lock (state)
            {
                return submitted.ToArray();
            }
        }
    }

    public float[] Consumed
    {
        get
        {
            lock (state)
            {
                return consumed.ToArray();
            }
        }
    }

    public float[] QueuedSamples
    {
        get
        {
            lock (state)
            {
                return queued.ToArray();
            }
        }
    }

    public bool InitializeAudio()
    {
        Record("init");
        if (!InitializeResult)
        {
            return false;
        }

        lock (state)
        {
            audioReferences++;
        }

        return true;
    }

    public void QuitAudio()
    {
        lock (state)
        {
            audioReferences--;
        }

        Record("quit");
    }

    public string GetError() => Error;

    public nint OpenDefaultPlaybackStream(in SdlAudioFormat source, SdlAudioRequest onRequest)
    {
        OpenedFormat = source;
        Record("open");
        if (!OpenResult)
        {
            return 0;
        }

        lock (state)
        {
            stream = nextStream++;
            handler = onRequest;
            return stream;
        }
    }

    public uint GetStreamDevice(nint value) => IsCurrent(value) ? Device : 0;

    public bool GetDeviceFormat(uint device, out SdlAudioFormat format, out int sampleFrames)
    {
        format = DeviceFormat;
        sampleFrames = DeviceSampleFrames;
        return device == Device;
    }

    public bool ResumeStreamDevice(nint value)
    {
        Record("resume");
        return ResumeResult && IsCurrent(value);
    }

    public bool PutStreamData(nint value, ReadOnlySpan<float> samples)
    {
        lock (streamLock)
        {
            lock (state)
            {
                if (!PutResult || value != stream || value == 0)
                {
                    return false;
                }

                foreach (float sample in samples)
                {
                    queued.Enqueue(sample);
                    submitted.Add(sample);
                }

                queueHighWaterBytes = Math.Max(queueHighWaterBytes, queued.Count * sizeof(float));
                return true;
            }
        }
    }

    public int GetStreamQueuedBytes(nint value)
    {
        lock (streamLock)
        {
            lock (state)
            {
                return QueuedFails || value != stream || value == 0 ? -1 : queued.Count * sizeof(float);
            }
        }
    }

    public bool FlushStream(nint value)
    {
        lock (streamLock)
        {
            lock (state)
            {
                if (value != stream || value == 0)
                {
                    return false;
                }

                flushCount++;
                releasableSamples = queued.Count;
                return true;
            }
        }
    }

    public void DestroyStream(nint value)
    {
        DestroyEntered?.Invoke();
        lock (streamLock)
        {
            lock (state)
            {
                if (value == stream)
                {
                    stream = 0;
                    handler = null;
                    queued.Clear();
                    releasableSamples = 0;
                }
            }

            Record("destroy");
        }
    }

    // A device request of totalBytes: runs the handler under the stream lock
    // with SDL's additional amount, then dequeues what is available.
    public int Pull(int totalBytes)
    {
        lock (streamLock)
        {
            SdlAudioRequest? current;
            int availableBytes;
            lock (state)
            {
                current = handler;
                availableBytes = AvailableSamplesLocked() * sizeof(float);
            }

            if (current is null)
            {
                Interlocked.Increment(ref lateRequests);
                return 0;
            }

            current(Math.Max(0, totalBytes - availableBytes), totalBytes);
            lock (state)
            {
                int samples = Math.Min(totalBytes / sizeof(float), AvailableSamplesLocked());
                releasableSamples = Math.Max(0, releasableSamples - samples);
                for (int index = 0; index < samples; index++)
                {
                    consumed.Add(queued.Dequeue());
                }

                return samples * sizeof(float);
            }
        }
    }

    private int AvailableSamplesLocked() =>
        Math.Max(releasableSamples, queued.Count - (HeldBackBytes / sizeof(float)));

    private bool IsCurrent(nint value)
    {
        lock (state)
        {
            return value != 0 && value == stream;
        }
    }

    private void Record(string name)
    {
        lock (state)
        {
            operations.Add((name, Environment.CurrentManagedThreadId));
        }

        OperationRecorded?.Invoke(name);
    }
}
