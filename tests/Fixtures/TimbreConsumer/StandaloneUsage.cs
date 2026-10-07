using Cerneala.Timbre;

namespace TimbreConsumer;

// The C# usage shape from the Timbre core plan §2.1, compiled outside the core
// assembly against public API only.
public static class StandaloneUsage
{
    public static async Task RunPlanExampleAsync(SoundScope sounds)
    {
        var plainSound = new SoundClip("audio/confirm.wav");
        var first = sounds.Play(plainSound);
        var second = sounds.Play(plainSound); // Overlap: independent instances.
        first.Cancel(); // Does not cancel second.

        var toneCutoff = new SoundParameter<float>(
            name: "ToneCutoff", defaultValue: 1200f);

        var filteredSound = new SoundClip(
            source: "audio/confirm.wav",
            parameters: [toneCutoff],
            modifiers: [new LowPass(cutoff: toneCutoff)]);

        var slot = sounds.CreateHandle();
        var playback = sounds.Play(filteredSound, start =>
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

    public static SoundClip CreateConstantModifierClip() =>
        new("audio/confirm.wav", modifiers: [new LowPass(cutoff: 1200f), new Delay(time: 0.12f, feedback: 0.2f, mix: 0.15f)]);

    public static SoundPlayback PlayLooping(SoundScope sounds, SoundClip clip) =>
        sounds.Play(clip, start => start.Loop = true);
}
