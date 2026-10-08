namespace Cerneala.Timbre;

// Runtime-scoped sink for the final Timbre mix: interleaved stereo float32 at
// 48 kHz, complete frames only. The runtime is the single producer.
public interface ITimbreOutput
{
    int QueuedFrames { get; }

    void Open(ITimbreOutputClient client);

    void Submit(ReadOnlySpan<float> samples);

    void Close();
}

// Implemented by the runtime. Outputs may call it from any thread, including a
// native audio callback; both members only signal and never block.
public interface ITimbreOutputClient
{
    void NotifyCapacityAvailable();

    void NotifyDeviceLost(Exception? error);
}
