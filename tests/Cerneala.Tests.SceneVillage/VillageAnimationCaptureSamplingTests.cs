using Cerneala.SceneVillage;
using Cerneala.UI.Controls;
using SkiaSharp;

namespace Cerneala.Tests.SceneVillage;

public sealed class VillageAnimationCaptureSamplingTests
{
    [Fact]
    public void SevenCaptureScheduleDetectsWalkAcrossInitialPhasesAndCaptureCosts()
    {
        Assert.True(VillageArt.VillagerAnimations.TryGetClip("WalkDown", out var clip));
        using SKBitmap atlas = SKBitmap.Decode(Path.Combine(AppContext.BaseDirectory, "Assets", "ch003.png"));
        Assert.NotNull(atlas);
        bool[] differsFromIdle = clip!.Frames.Select(frame => DiffersFromIdle(atlas, frame)).ToArray();
        Assert.Equal(new[] { true, false, true, false }, differsFromIdle);

        // Bounded deterministic sampling model, not a timing guarantee for an
        // arbitrarily stalled native renderer: every whole-ms initial phase
        // and constant capture/readback/comparison cost through a full cycle.
        for (int captureCost = 0; captureCost <= 480; captureCost++)
        {
            for (int initialPhase = 0; initialPhase < 480; initialPhase++)
            {
                double elapsed = initialPhase;
                bool detected = IsDifferent(elapsed);
                for (int attempt = 1; !detected && attempt <= 6; attempt++)
                {
                    elapsed += captureCost + NativeVillageWindowTests.AnimationCaptureDelay(attempt).TotalMilliseconds;
                    detected = IsDifferent(elapsed);
                }

                Assert.True(detected, $"All seven captures missed walking: phase={initialPhase}ms, capture cost={captureCost}ms.");
            }
        }

        bool IsDifferent(double milliseconds) => differsFromIdle[
            SpriteAnimationSampler.Sample(clip, TimeSpan.FromMilliseconds(milliseconds), 1).FrameIndex];
    }

    private static bool DiffersFromIdle(SKBitmap atlas, SpriteAnimationFrame frame)
    {
        var rect = frame.SourceRect;
        for (int y = 0; y < 32; y++)
        {
            for (int x = 0; x < 32; x++)
            {
                SKColor actual = atlas.GetPixel((int)rect.X + x, (int)rect.Y + y);
                SKColor idle = atlas.GetPixel(32 + x, y);
                // Invisible RGB is not a visible pose difference.
                if (actual.Alpha == 0 && idle.Alpha == 0) continue;
                if (actual != idle) return true;
            }
        }

        return false;
    }
}
