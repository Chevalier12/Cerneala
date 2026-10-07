using System.Collections.Concurrent;
using System.Runtime.InteropServices;
using SDL3;

namespace Cerneala.Platforms.Sdl3;

internal sealed class NativeSdlAudioApi : ISdlAudioApi
{
    // One rooted delegate for the process: native code only ever holds this
    // function pointer. Streams are identified by an opaque userdata id, so a
    // callback that races destruction finds no handler instead of freed state.
    private static readonly SDL.AudioStreamCallback Callback = OnRequest;
    private static readonly ConcurrentDictionary<nint, SdlAudioRequest> Handlers = new();
    private static long nextId;
    private static long lateCallbacks;
    private static long handlerFailures;

    private readonly ConcurrentDictionary<nint, nint> streamIds = new();

    // Callbacks that found no registered handler.
    internal static long LateCallbacks => Interlocked.Read(ref lateCallbacks);

    // Handler exceptions swallowed at the native boundary.
    internal static long HandlerFailures => Interlocked.Read(ref handlerFailures);

    internal static int RegisteredHandlers => Handlers.Count;

    public bool InitializeAudio() => SDL.InitSubSystem(SDL.InitFlags.Audio);

    public void QuitAudio() => SDL.QuitSubSystem(SDL.InitFlags.Audio);

    public string GetError() => SDL.GetError();

    public nint OpenDefaultPlaybackStream(in SdlAudioFormat source, SdlAudioRequest onRequest)
    {
        ArgumentNullException.ThrowIfNull(onRequest);
        nint id = (nint)Interlocked.Increment(ref nextId);
        Handlers[id] = onRequest;
        SDL.AudioSpec spec = new()
        {
            Format = (SDL.AudioFormat)source.Format,
            Channels = source.Channels,
            Freq = source.Frequency
        };
        nint stream = SDL.OpenAudioDeviceStream(SDL.AudioDeviceDefaultPlayback, in spec, Callback, id);
        if (stream == 0)
        {
            Handlers.TryRemove(id, out _);
            return 0;
        }

        streamIds[stream] = id;
        return stream;
    }

    public uint GetStreamDevice(nint stream) => SDL.GetAudioStreamDevice(stream);

    public bool GetDeviceFormat(uint device, out SdlAudioFormat format, out int sampleFrames)
    {
        bool result = SDL.GetAudioDeviceFormat(device, out SDL.AudioSpec spec, out sampleFrames);
        format = new SdlAudioFormat((uint)spec.Format, spec.Channels, spec.Freq);
        return result;
    }

    public bool ResumeStreamDevice(nint stream) => SDL.ResumeAudioStreamDevice(stream);

    public bool PutStreamData(nint stream, ReadOnlySpan<float> samples)
    {
        ReadOnlySpan<byte> bytes = MemoryMarshal.AsBytes(samples);
        return SDL.PutAudioStreamData(stream, bytes, bytes.Length);
    }

    public int GetStreamQueuedBytes(nint stream) => SDL.GetAudioStreamQueued(stream);

    public bool FlushStream(nint stream) => SDL.FlushAudioStream(stream);

    public void DestroyStream(nint stream)
    {
        // Destruction takes the stream lock, so an in-flight callback ends first.
        SDL.DestroyAudioStream(stream);
        if (streamIds.TryRemove(stream, out nint id))
        {
            Handlers.TryRemove(id, out _);
        }
    }

    private static void OnRequest(nint userdata, nint stream, int additionalAmount, int totalAmount)
    {
        if (!Handlers.TryGetValue(userdata, out SdlAudioRequest? handler))
        {
            Interlocked.Increment(ref lateCallbacks);
            return;
        }

        try
        {
            handler(additionalAmount, totalAmount);
        }
        catch
        {
            // A managed exception must never unwind into SDL's audio thread.
            Interlocked.Increment(ref handlerFailures);
        }
    }
}
