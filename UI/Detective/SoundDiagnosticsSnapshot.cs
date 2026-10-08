using Cerneala.Timbre;

namespace Cerneala.UI.Detective;

// Read-only view of a root's sound runtime for diagnosing missing, starved, or
// clipped audio and its memory use. Detective never owns or drives audio.
public sealed record SoundDiagnosticsSnapshot(
    bool OutputOpen,
    int ActivePlaybacks,
    long PlaybacksFailed,
    long UnderrunFrames,
    long ClippedSamples,
    long CacheBytes,
    long StreamingBufferBytes,
    long DspStateBytes)
{
    // Audio Motion samples rejected as non-finite or out of range; each one
    // ended only the animation of its parameter.
    public long MotionSamplesRejected { get; init; }

    internal static SoundDiagnosticsSnapshot Capture(SoundRuntime runtime)
    {
        SoundRuntimeDiagnostics diagnostics = runtime.GetDiagnostics();
        return new SoundDiagnosticsSnapshot(
            diagnostics.OutputOpen,
            diagnostics.ActiveVoices,
            diagnostics.PlaybacksFailed,
            diagnostics.UnderrunFrames,
            diagnostics.ClippedSamples,
            diagnostics.CacheBytes,
            diagnostics.StreamingBufferBytes,
            diagnostics.DspStateBytes)
        {
            MotionSamplesRejected = diagnostics.MotionSamplesRejected
        };
    }
}
