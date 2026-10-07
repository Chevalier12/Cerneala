namespace Cerneala.Platforms.Sdl3;

// SDL_AudioSpec values: format is an SDL_AudioFormat (0x8120 = F32 little endian).
internal readonly record struct SdlAudioFormat(uint Format, int Channels, int Frequency)
{
    public const uint Float32LittleEndian = 0x8120;
}

// Runs on an SDL audio thread with the stream lock held, before SDL takes data
// from the stream. Byte counts are in the stream's input format and are SDL's
// estimate. The handler must not block or touch the stream.
internal delegate void SdlAudioRequest(int additionalBytes, int totalBytes);

// Audio seam kept apart from ISdlApi so GPU/video consumers never initialize
// audio. Only the default playback device and the single ownership path
// OpenAudioDeviceStream -> ResumeAudioStreamDevice -> DestroyAudioStream exist.
internal interface ISdlAudioApi
{
    bool InitializeAudio();

    void QuitAudio();

    string GetError();

    // Opens the default playback device bound to a new stream; the device
    // starts paused. Returns 0 on failure.
    nint OpenDefaultPlaybackStream(in SdlAudioFormat source, SdlAudioRequest onRequest);

    uint GetStreamDevice(nint stream);

    bool GetDeviceFormat(uint device, out SdlAudioFormat format, out int sampleFrames);

    bool ResumeStreamDevice(nint stream);

    bool PutStreamData(nint stream, ReadOnlySpan<float> samples);

    // Bytes of unconverted input still queued, or -1 on failure. A resampling
    // stream keeps its last input frames queued until more input or a flush.
    int GetStreamQueuedBytes(nint stream);

    // Marks the end of the queued input so a resampling stream converts it all.
    bool FlushStream(nint stream);

    // Frees the stream and closes the device opened with it. A request handler
    // already running finishes first; none runs after this returns.
    void DestroyStream(nint stream);
}
