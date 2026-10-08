using Cerneala.Timbre;

namespace TimbreConsumer;

// The C# usage shape from the Timbre core plan §2.1, compiled outside the core
// assembly against public API only.
public static class StandaloneUsage
{
    public static async Task RunPlanExampleAsync(TimbreScope sounds)
    {
        var plainTimbre = new TimbreSound("audio/confirm.wav");
        var first = sounds.Play(plainTimbre);
        var second = sounds.Play(plainTimbre); // Overlap: independent instances.
        first.Cancel(); // Does not cancel second.

        var toneCutoff = new TimbreParameter<float>(
            name: "ToneCutoff", defaultValue: 1200f);

        var filteredTimbre = new TimbreSound(
            source: "audio/confirm.wav",
            parameters: [toneCutoff],
            modifiers: [new LowPass(cutoff: toneCutoff)]);

        var slot = sounds.CreateHandle();
        var playback = sounds.Play(filteredTimbre, start =>
        {
            start.Volume = 0.2f;
            start.Set(toneCutoff, 800f);
        }, handle: slot);

        playback.Volume = 0.8f;
        playback.Set(toneCutoff, 6000f);
        playback.Pause();
        playback.Resume();
        await playback.SeekAsync(TimeSpan.FromSeconds(1));
        playback.Cancel(); // Cancels this identity, not a future occupant.
        slot.Cancel();     // Cancels the slot's current occupant, if any.
        _ = second;
    }

    public static TimbreSound CreateConstantModifierClip() =>
        new("audio/confirm.wav", modifiers: [new LowPass(cutoff: 1200f), new Delay(time: 0.12f, feedback: 0.2f, mix: 0.15f)]);

    public static TimbrePlayback PlayLooping(TimbreScope sounds, TimbreSound clip) =>
        sounds.Play(clip, start => start.Loop = true);
}
