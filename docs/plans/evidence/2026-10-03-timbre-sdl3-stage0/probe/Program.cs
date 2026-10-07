using System.Collections.Concurrent;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text.Json;
using SDL3;

// Stage 0 interop probe for the Timbre SDL3 output. Every native step is
// bounded by the watchdog; results are written as JSON next to the probe.
internal static class Program
{
    private const int Rate = 48000;
    private const int Channels = 2;
    private const int BytesPerFrame = Channels * sizeof(float);

    // Rooted for the process lifetime: the native side only ever sees this
    // single function pointer; per-stream state is looked up by userdata id.
    private static readonly SDL.AudioStreamCallback Callback = OnRequest;
    private static readonly ConcurrentDictionary<nint, Recorder> Registry = new();
    private static long lateCallbacks;
    private static readonly Dictionary<string, object?> Results = [];

    private static int Main(string[] args)
    {
        string output = args.Length > 0 ? args[0] : "results.json";
        Thread watchdog = new(() =>
        {
            Thread.Sleep(TimeSpan.FromSeconds(45));
            Console.Error.WriteLine("PROBE_TIMEOUT");
            Environment.Exit(3);
        })
        { IsBackground = true };
        watchdog.Start();

        Results["process"] = new
        {
            os = RuntimeInformation.OSDescription,
            arch = RuntimeInformation.ProcessArchitecture.ToString(),
            runtime = RuntimeInformation.FrameworkDescription,
            sdlRevision = SDL.GetRevision(),
            sdlVersion = SDL.GetVersion(),
            mainThread = Environment.CurrentManagedThreadId
        };
        Results["abi"] = new
        {
            audioSpecSize = Unsafe.SizeOf<SDL.AudioSpec>(),
            formatOffset = Marshal.OffsetOf<SDL.AudioSpec>(nameof(SDL.AudioSpec.Format)).ToInt32(),
            channelsOffset = Marshal.OffsetOf<SDL.AudioSpec>(nameof(SDL.AudioSpec.Channels)).ToInt32(),
            freqOffset = Marshal.OffsetOf<SDL.AudioSpec>(nameof(SDL.AudioSpec.Freq)).ToInt32(),
            audioFormatUnderlying = Enum.GetUnderlyingType(typeof(SDL.AudioFormat)).Name,
            f32le = $"0x{(uint)SDL.AudioFormat.AudioF32LE:X}",
            f32ByteSize = SDL.AudioByteSize((uint)SDL.AudioFormat.AudioF32LE),
            frameSize = SDL.AudioFrameSize(new SDL.AudioSpec { Format = SDL.AudioFormat.AudioF32LE, Channels = Channels, Freq = Rate }),
            callbackConvention = typeof(SDL.AudioStreamCallback)
                .GetCustomAttributes(typeof(UnmanagedFunctionPointerAttribute), false)
                .Cast<UnmanagedFunctionPointerAttribute>()
                .Select(attribute => attribute.CallingConvention.ToString())
                .FirstOrDefault()
        };

        if (!SDL.Init(SDL.InitFlags.Video | SDL.InitFlags.Events))
        {
            Results["fatal"] = "video init failed: " + SDL.GetError();
            return Write(output, 2);
        }

        try
        {
            Results["subsystemBalance"] = ProbeSubsystemBalance();
            Results["realDevice"] = OnWorker(() => ProbeDevice(driver: null));
            Results["conversion"] = ProbeConversion();
            Results["dummyDriver"] = OnWorker(() => ProbeDevice(driver: "dummy"));
            Results["absentDriver"] = OnWorker(ProbeAbsentDriver);
            Results["lateCallbacksTotal"] = Interlocked.Read(ref lateCallbacks);
            Results["wasInitAfterProbes"] = SDL.WasInit(0).ToString();
        }
        catch (Exception exception)
        {
            Results["fatal"] = exception.ToString();
            return Write(output, 2);
        }
        finally
        {
            SDL.Quit();
        }

        Results["wasInitAfterQuit"] = SDL.WasInit(0).ToString();
        return Write(output, 0);
    }

    private static object ProbeSubsystemBalance()
    {
        string before = SDL.WasInit(0).ToString();
        List<object> rounds = [];
        for (int round = 0; round < 2; round++)
        {
            rounds.Add(OnWorker(() =>
            {
                bool initialized = SDL.InitSubSystem(SDL.InitFlags.Audio);
                string error = initialized ? "" : SDL.GetError();
                string during = SDL.WasInit(0).ToString();
                string driver = SDL.GetCurrentAudioDriver() ?? "<null>";
                SDL.QuitSubSystem(SDL.InitFlags.Audio);
                return new
                {
                    thread = Environment.CurrentManagedThreadId,
                    initialized,
                    error,
                    during,
                    driver,
                    after = SDL.WasInit(0).ToString()
                };
            }));
        }

        return new { before, rounds, final = SDL.WasInit(0).ToString() };
    }

    private static object ProbeDevice(string? driver)
    {
        if (driver is not null)
        {
            SDL.SetHint(SDL.Hints.AudioDriver, driver);
        }

        try
        {
            if (!SDL.InitSubSystem(SDL.InitFlags.Audio))
            {
                return new { initialized = false, error = SDL.GetError() };
            }

            try
            {
                return ProbeStream(SDL.GetCurrentAudioDriver() ?? "<null>");
            }
            finally
            {
                SDL.QuitSubSystem(SDL.InitFlags.Audio);
            }
        }
        finally
        {
            if (driver is not null)
            {
                SDL.ResetHint(SDL.Hints.AudioDriver);
            }
        }
    }

    private static object ProbeStream(string driver)
    {
        SDL.AudioSpec spec = new() { Format = SDL.AudioFormat.AudioF32LE, Channels = Channels, Freq = Rate };
        nint id = 1 + Registry.Count + (nint)Interlocked.Read(ref lateCallbacks) * 1000 + Environment.TickCount;
        Recorder recorder = new();
        Registry[id] = recorder;
        nint stream = SDL.OpenAudioDeviceStream(SDL.AudioDeviceDefaultPlayback, in spec, Callback, id);
        if (stream == 0)
        {
            Registry.TryRemove(id, out _);
            return new { driver, opened = false, error = SDL.GetError() };
        }

        uint device = SDL.GetAudioStreamDevice(stream);
        bool pausedAtOpen = SDL.AudioStreamDevicePaused(stream);
        SDL.GetAudioDeviceFormat(device, out SDL.AudioSpec deviceSpec, out int deviceFrames);
        SDL.GetAudioStreamFormat(stream, out SDL.AudioSpec source, out SDL.AudioSpec destination);

        // Paused at open: queue data and confirm nothing is consumed yet.
        float[] block = Sine(480, 440, 0.05f);
        bool firstPut = SDL.PutAudioStreamData(stream, MemoryMarshal.AsBytes(block.AsSpan()), block.Length * sizeof(float));
        Thread.Sleep(50);
        int queuedWhilePaused = SDL.GetAudioStreamQueued(stream);
        long callbacksWhilePaused = recorder.Callbacks;

        bool resumed = SDL.ResumeAudioStreamDevice(stream);
        Stopwatch clock = Stopwatch.StartNew();
        int puts = 1;
        int queueHighWater = queuedWhilePaused;
        // Keep at most 1920 frames (40 ms) queued for ~600 ms, as the runtime would.
        while (clock.ElapsedMilliseconds < 600)
        {
            int queued = SDL.GetAudioStreamQueued(stream);
            queueHighWater = Math.Max(queueHighWater, queued);
            if (queued / BytesPerFrame + 480 <= 1920)
            {
                SDL.PutAudioStreamData(stream, MemoryMarshal.AsBytes(block.AsSpan()), block.Length * sizeof(float));
                puts++;
                continue;
            }

            recorder.Signal.Wait(TimeSpan.FromMilliseconds(100));
            recorder.Signal.Reset();
            if (puts % 8 == 0)
            {
                GC.Collect();
                GC.WaitForPendingFinalizers();
            }
        }

        long callbacksBeforeDrain = recorder.Callbacks;
        Stopwatch drain = Stopwatch.StartNew();
        while (SDL.GetAudioStreamQueued(stream) > 0 && drain.ElapsedMilliseconds < 1000)
        {
            recorder.Signal.Wait(TimeSpan.FromMilliseconds(100));
            recorder.Signal.Reset();
        }

        double drainMs = drain.Elapsed.TotalMilliseconds;
        int queuedAfterDrain = SDL.GetAudioStreamQueued(stream);
        long starvedBeforeDrain = recorder.StarvedBeforeDrain(callbacksBeforeDrain);

        SDL.DestroyAudioStream(stream);
        long callbacksAtDestroy = recorder.Callbacks;
        Registry.TryRemove(id, out _);
        Thread.Sleep(150);

        return new
        {
            driver,
            opened = true,
            device,
            pausedAtOpen,
            deviceFormat = Describe(deviceSpec),
            deviceSampleFrames = deviceFrames,
            streamSource = Describe(source),
            streamDestination = Describe(destination),
            firstPut,
            queuedWhilePausedBytes = queuedWhilePaused,
            callbacksWhilePaused,
            resumed,
            puts,
            queueHighWaterBytes = queueHighWater,
            queueHighWaterFrames = queueHighWater / BytesPerFrame,
            callbacks = recorder.Callbacks,
            callbackThreads = recorder.Threads.Keys.Order().ToArray(),
            callbackOnMainThread = recorder.Threads.ContainsKey(1),
            additionalAmounts = recorder.Additional.Keys.Order().Take(16).ToArray(),
            totalAmounts = recorder.Total.Keys.Order().Take(16).ToArray(),
            zeroAdditionalCallbacks = recorder.ZeroAdditional,
            starvedCallbacksWhileFeeding = starvedBeforeDrain,
            drainMs,
            queuedAfterDrainBytes = queuedAfterDrain,
            callbacksAfterDestroy = recorder.Callbacks - callbacksAtDestroy
        };
    }

    private static object ProbeConversion()
    {
        SDL.AudioSpec source = new() { Format = SDL.AudioFormat.AudioF32LE, Channels = Channels, Freq = Rate };
        SDL.AudioSpec destination = new() { Format = SDL.AudioFormat.AudioS16LE, Channels = Channels, Freq = 44100 };
        nint stream = SDL.CreateAudioStream(in source, in destination);
        if (stream == 0)
        {
            return new { created = false, error = SDL.GetError() };
        }

        try
        {
            float[] input = Sine(4800, 1000, 0.5f);
            bool put = SDL.PutAudioStreamData(stream, MemoryMarshal.AsBytes(input.AsSpan()), input.Length * sizeof(float));
            bool flushed = SDL.FlushAudioStream(stream);
            int available = SDL.GetAudioStreamAvailable(stream);
            short[] converted = new short[available / sizeof(short)];
            int read = SDL.GetAudioStreamData(stream, MemoryMarshal.AsBytes(converted.AsSpan()), available);
            int frames = read / (Channels * sizeof(short));
            int crossings = 0;
            double sumSquares = 0;
            double channelDelta = 0;
            for (int frame = 1; frame < frames; frame++)
            {
                short left = converted[frame * 2];
                short previous = converted[(frame - 1) * 2];
                if ((left >= 0) != (previous >= 0))
                {
                    crossings++;
                }

                sumSquares += (double)left * left;
                channelDelta = Math.Max(channelDelta, Math.Abs(left - converted[(frame * 2) + 1]));
            }

            double rms = Math.Sqrt(sumSquares / Math.Max(1, frames)) / short.MaxValue;
            return new
            {
                created = true,
                put,
                flushed,
                inputFrames = input.Length / Channels,
                outputFrames = frames,
                expectedFrames = 4800 * 44100 / 48000,
                estimatedFrequencyHz = crossings / 2.0 / (frames / 44100.0),
                rms,
                expectedRms = 0.5 / Math.Sqrt(2),
                maxChannelDelta = channelDelta
            };
        }
        finally
        {
            SDL.DestroyAudioStream(stream);
        }
    }

    private static object ProbeAbsentDriver()
    {
        SDL.SetHint(SDL.Hints.AudioDriver, "cerneala-absent-driver");
        try
        {
            bool initialized = SDL.InitSubSystem(SDL.InitFlags.Audio);
            string error = initialized ? "" : SDL.GetError();
            string wasInit = SDL.WasInit(SDL.InitFlags.Audio).ToString();
            if (initialized)
            {
                SDL.QuitSubSystem(SDL.InitFlags.Audio);
            }

            return new { initialized, error, wasInitAudio = wasInit, after = SDL.WasInit(0).ToString() };
        }
        finally
        {
            SDL.ResetHint(SDL.Hints.AudioDriver);
        }
    }

    private static void OnRequest(nint userdata, nint stream, int additional, int total)
    {
        if (!Registry.TryGetValue(userdata, out Recorder? recorder))
        {
            Interlocked.Increment(ref lateCallbacks);
            return;
        }

        recorder.Record(additional, total);
    }

    private static T OnWorker<T>(Func<T> action)
    {
        T? result = default;
        Exception? failure = null;
        Thread worker = new(() =>
        {
            try
            {
                result = action();
            }
            catch (Exception exception)
            {
                failure = exception;
            }
        })
        { IsBackground = true, Name = "probe worker" };
        worker.Start();
        worker.Join();
        return failure is null ? result! : throw new InvalidOperationException("worker failed", failure);
    }

    private static float[] Sine(int frames, double frequency, float amplitude)
    {
        float[] samples = new float[frames * Channels];
        for (int frame = 0; frame < frames; frame++)
        {
            float value = amplitude * (float)Math.Sin(2 * Math.PI * frequency * frame / Rate);
            samples[frame * 2] = value;
            samples[(frame * 2) + 1] = value;
        }

        return samples;
    }

    private static object Describe(SDL.AudioSpec spec) => new
    {
        format = $"0x{(uint)spec.Format:X}",
        spec.Channels,
        spec.Freq
    };

    private static int Write(string path, int code)
    {
        Results["exitCode"] = code;
        File.WriteAllText(path, JsonSerializer.Serialize(Results, new JsonSerializerOptions { WriteIndented = true }));
        Console.WriteLine(File.ReadAllText(path));
        return code;
    }

    private sealed class Recorder
    {
        private long callbacks;
        private long zeroAdditional;
        private readonly ConcurrentQueue<(long Index, int Additional)> requests = new();

        public ManualResetEventSlim Signal { get; } = new(false);

        public ConcurrentDictionary<int, byte> Threads { get; } = new();

        public ConcurrentDictionary<int, byte> Additional { get; } = new();

        public ConcurrentDictionary<int, byte> Total { get; } = new();

        public long Callbacks => Interlocked.Read(ref callbacks);

        public long ZeroAdditional => Interlocked.Read(ref zeroAdditional);

        public void Record(int additional, int total)
        {
            long index = Interlocked.Increment(ref callbacks);
            Threads.TryAdd(Environment.CurrentManagedThreadId, 0);
            Additional.TryAdd(additional, 0);
            Total.TryAdd(total, 0);
            if (additional == 0)
            {
                Interlocked.Increment(ref zeroAdditional);
            }

            requests.Enqueue((index, additional));
            Signal.Set();
        }

        public long StarvedBeforeDrain(long limit) =>
            requests.Count(request => request.Index <= limit && request.Index > 2 && request.Additional > 0);
    }
}
