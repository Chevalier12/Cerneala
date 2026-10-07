namespace Cerneala.Timbre.Engine;

// Mixer-thread instrumentation for the realtime cost gate. Implementations must
// not allocate or block: they run inside the measured path after every
// submitted block.
internal interface ISoundBlockObserver
{
    // `elapsedTicks` covers voice rendering, mixing, clipping, and Submit, in
    // Stopwatch ticks; `allocatedBytes` is the mixer thread's managed
    // allocation over the same span.
    void OnBlockSubmitted(long elapsedTicks, long allocatedBytes);
}
