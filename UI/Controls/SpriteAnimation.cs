using System.Collections.ObjectModel;
using Cerneala.Drawing;

namespace Cerneala.UI.Controls;

public sealed class SpriteAnimationFrame
{
    public SpriteAnimationFrame(
        DrawRect sourceRect,
        TimeSpan duration,
        RenderSurface2DSpriteFlip flip = RenderSurface2DSpriteFlip.None)
    {
        ValidateFrame(sourceRect, duration, flip);

        SourceRect = sourceRect;
        Duration = duration;
        Flip = flip;
        Offset = default;
        FrameSize = new DrawSize(sourceRect.Width, sourceRect.Height);
    }

    // A trimmed frame: SourceRect holds only the visible pixels, which sat at
    // offset inside a logical (untrimmed) frame of frameSize, in source pixels.
    public SpriteAnimationFrame(
        DrawRect sourceRect,
        TimeSpan duration,
        RenderSurface2DSpriteFlip flip,
        DrawPoint offset,
        DrawSize frameSize)
    {
        ValidateFrame(sourceRect, duration, flip);
        if (!float.IsFinite(offset.X) || !float.IsFinite(offset.Y) || offset.X < 0 || offset.Y < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(offset));
        }
        if (!float.IsFinite(frameSize.Width) ||
            !float.IsFinite(frameSize.Height) ||
            frameSize.Width <= 0 ||
            frameSize.Height <= 0 ||
            offset.X + sourceRect.Width > frameSize.Width ||
            offset.Y + sourceRect.Height > frameSize.Height)
        {
            throw new ArgumentOutOfRangeException(nameof(frameSize));
        }

        SourceRect = sourceRect;
        Duration = duration;
        Flip = flip;
        Offset = offset;
        FrameSize = frameSize;
    }

    // Builds the frame at the given cell of a uniform grid atlas.
    public static SpriteAnimationFrame FromCell(
        int column,
        int row,
        float cellWidth,
        float cellHeight,
        TimeSpan duration,
        RenderSurface2DSpriteFlip flip = RenderSurface2DSpriteFlip.None,
        float margin = 0,
        float spacing = 0)
    {
        if (column < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(column));
        }
        if (row < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(row));
        }
        if (!float.IsFinite(cellWidth) || cellWidth <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(cellWidth));
        }
        if (!float.IsFinite(cellHeight) || cellHeight <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(cellHeight));
        }
        if (!float.IsFinite(margin) || margin < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(margin));
        }
        if (!float.IsFinite(spacing) || spacing < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(spacing));
        }

        return new SpriteAnimationFrame(
            new DrawRect(
                margin + (column * (cellWidth + spacing)),
                margin + (row * (cellHeight + spacing)),
                cellWidth,
                cellHeight),
            duration,
            flip);
    }

    private static void ValidateFrame(DrawRect sourceRect, TimeSpan duration, RenderSurface2DSpriteFlip flip)
    {
        if (!float.IsFinite(sourceRect.X) ||
            !float.IsFinite(sourceRect.Y) ||
            !float.IsFinite(sourceRect.Width) ||
            !float.IsFinite(sourceRect.Height) ||
            sourceRect.Width <= 0 ||
            sourceRect.Height <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(sourceRect));
        }
        if (duration <= TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(duration));
        }
        if ((flip & ~(RenderSurface2DSpriteFlip.Horizontal | RenderSurface2DSpriteFlip.Vertical)) != 0)
        {
            throw new ArgumentOutOfRangeException(nameof(flip));
        }
    }

    public DrawRect SourceRect { get; }

    public TimeSpan Duration { get; }

    public RenderSurface2DSpriteFlip Flip { get; }

    // Top-left of SourceRect's pixels inside the logical (untrimmed) frame, in source pixels.
    public DrawPoint Offset { get; }

    // Logical (untrimmed) frame size in source pixels.
    public DrawSize FrameSize { get; }
}

public sealed class SpriteAnimationClip
{
    private readonly ReadOnlyCollection<SpriteAnimationFrame> frames;
    private readonly long[] frameEndTicks;

    public SpriteAnimationClip(
        string name,
        IEnumerable<SpriteAnimationFrame> frames,
        bool isLooping = true)
        : this(name, frames, isLooping, version: 1)
    {
    }

    public SpriteAnimationClip(
        string name,
        IEnumerable<SpriteAnimationFrame> frames,
        bool isLooping,
        long version)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Animation clip name cannot be empty.", nameof(name));
        }
        if (version <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(version));
        }
        ArgumentNullException.ThrowIfNull(frames);
        SpriteAnimationFrame[] copied = frames.ToArray();
        if (copied.Length == 0)
        {
            throw new ArgumentException("An animation clip must define at least one frame.", nameof(frames));
        }
        if (copied.Any(static frame => frame is null))
        {
            throw new ArgumentException("An animation clip cannot contain null frames.", nameof(frames));
        }

        frameEndTicks = new long[copied.Length];
        long durationTicks = 0;
        long lastPresentationChangeTicks = 0;
        try
        {
            for (int index = 0; index < copied.Length; index++)
            {
                if (index > 0 && !SpriteAnimationPlayback.FramesHaveSamePresentation(copied[index - 1], copied[index]))
                {
                    lastPresentationChangeTicks = durationTicks;
                }
                durationTicks = checked(durationTicks + copied[index].Duration.Ticks);
                frameEndTicks[index] = durationTicks;
            }
        }
        catch (OverflowException exception)
        {
            throw new ArgumentException("The total animation clip duration exceeds TimeSpan.MaxValue.", nameof(frames), exception);
        }

        Name = name;
        this.frames = Array.AsReadOnly(copied);
        IsLooping = isLooping;
        Duration = TimeSpan.FromTicks(durationTicks);
        Version = version;
        LastPresentationChangeTicks = lastPresentationChangeTicks;
    }

    public string Name { get; }

    public IReadOnlyList<SpriteAnimationFrame> Frames => frames;

    public bool IsLooping { get; }

    public TimeSpan Duration { get; }

    public long Version { get; }

    internal ReadOnlySpan<long> FrameEndTicks => frameEndTicks;

    internal long LastPresentationChangeTicks { get; }
}

public sealed class SpriteAnimationSet
{
    private readonly ReadOnlyCollection<SpriteAnimationClip> clips;
    private readonly Dictionary<string, SpriteAnimationClip> clipsByName;

    public SpriteAnimationSet(IEnumerable<SpriteAnimationClip> clips)
        : this(clips, version: 1)
    {
    }

    public SpriteAnimationSet(IEnumerable<SpriteAnimationClip> clips, long version)
    {
        if (version <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(version));
        }
        ArgumentNullException.ThrowIfNull(clips);
        SpriteAnimationClip[] copied = clips.ToArray();
        if (copied.Length == 0)
        {
            throw new ArgumentException("An animation set must define at least one clip.", nameof(clips));
        }
        if (copied.Any(static clip => clip is null))
        {
            throw new ArgumentException("An animation set cannot contain null clips.", nameof(clips));
        }

        clipsByName = new Dictionary<string, SpriteAnimationClip>(StringComparer.Ordinal);
        foreach (SpriteAnimationClip clip in copied)
        {
            if (!clipsByName.TryAdd(clip.Name, clip))
            {
                throw new ArgumentException(
                    $"Animation clip name '{clip.Name}' is duplicated.",
                    nameof(clips));
            }
        }

        this.clips = Array.AsReadOnly(copied);
        Version = version;
    }

    public IReadOnlyList<SpriteAnimationClip> Clips => clips;

    public long Version { get; }

    public bool TryGetClip(string name, out SpriteAnimationClip? clip)
    {
        if (name is null)
        {
            clip = null;
            return false;
        }
        return clipsByName.TryGetValue(name, out clip);
    }
}

public enum SpriteAnimationStateChangeMode
{
    Restart,
    Resume
}

internal readonly record struct SpriteAnimationSample(
    SpriteAnimationFrame Frame,
    int FrameIndex,
    bool IsCompleted);

internal static class SpriteAnimationSampler
{
    internal static SpriteAnimationSample Sample(
        SpriteAnimationClip clip,
        TimeSpan elapsed,
        double playbackRate)
    {
        ArgumentNullException.ThrowIfNull(clip);
        if (elapsed < TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(elapsed));
        }
        if (!double.IsFinite(playbackRate) || playbackRate < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(playbackRate));
        }

        long scaledTicks = ScaleTicks(elapsed.Ticks, playbackRate);
        long durationTicks = clip.Duration.Ticks;
        bool completed = !clip.IsLooping && scaledTicks >= durationTicks;
        long clipTicks = clip.IsLooping
            ? scaledTicks % durationTicks
            : Math.Min(scaledTicks, durationTicks - 1);
        int frameIndex = FindFrame(clip.FrameEndTicks, clipTicks);
        return new SpriteAnimationSample(
            clip.Frames[frameIndex],
            frameIndex,
            completed);
    }

    // Samples at unscaled elapsed ticks and reports the elapsed-tick range
    // [start, end) over which that same frame remains current.
    internal static SpriteAnimationFrame SampleWithRange(
        SpriteAnimationClip clip,
        long elapsedTicks,
        out long rangeStart,
        out long rangeEnd)
    {
        SpriteAnimationSample sample = Sample(clip, TimeSpan.FromTicks(elapsedTicks), playbackRate: 1);
        long durationTicks = clip.Duration.Ticks;
        if (!clip.IsLooping && elapsedTicks >= durationTicks)
        {
            // A completed clip holds its final frame for any later time.
            rangeStart = durationTicks;
            rangeEnd = long.MaxValue;
            return sample.Frame;
        }

        long clipTicks = clip.IsLooping ? elapsedTicks % durationTicks : elapsedTicks;
        ReadOnlySpan<long> frameEnds = clip.FrameEndTicks;
        long frameStart = sample.FrameIndex == 0 ? 0 : frameEnds[sample.FrameIndex - 1];
        long frameEnd = frameEnds[sample.FrameIndex];
        rangeStart = elapsedTicks - (clipTicks - frameStart);
        rangeEnd = elapsedTicks + (frameEnd - clipTicks);
        return sample.Frame;
    }

    internal static long ScaleTicks(long elapsedTicks, double playbackRate)
    {
        if (elapsedTicks == 0 || playbackRate == 0)
        {
            return 0;
        }

        double scaled = elapsedTicks * playbackRate;
        if (scaled >= long.MaxValue)
        {
            return long.MaxValue;
        }
        return (long)Math.Floor(scaled);
    }

    private static int FindFrame(ReadOnlySpan<long> frameEndTicks, long elapsedTicks)
    {
        int low = 0;
        int high = frameEndTicks.Length - 1;
        while (low < high)
        {
            int middle = low + ((high - low) / 2);
            if (elapsedTicks < frameEndTicks[middle])
            {
                high = middle;
            }
            else
            {
                low = middle + 1;
            }
        }
        return low;
    }
}

internal sealed class SpriteAnimationPlayback
{
    private readonly Dictionary<string, long> resumedElapsedTicks = new(StringComparer.Ordinal);
    private SpriteAnimationSet? animations;
    private string? state;
    private SpriteAnimationClip? clip;
    private long elapsedTicks;
    private SpriteAnimationClip? sampledClip;
    private long sampledStart;
    private long sampledEnd;
    private SpriteAnimationFrame? sampledFrame;
    // While a clock drives this playback at rate one, its elapsed time trails
    // the clock by the ticks since the last synchronization.
    private SpriteAnimationClock? clock;
    private long syncedClockTicks;
    private SpriteAnimationFrame? reportedFrame;

    internal void Synchronize(
        SpriteAnimationSet? nextAnimations,
        string? nextState,
        SpriteAnimationStateChangeMode stateChangeMode)
    {
        Sync();
        if (!ReferenceEquals(animations, nextAnimations))
        {
            animations = nextAnimations;
            state = nextState;
            elapsedTicks = 0;
            resumedElapsedTicks.Clear();
            clip = ResolveClip(nextAnimations, nextState);
        }
        else if (!string.Equals(state, nextState, StringComparison.Ordinal))
        {
            if (stateChangeMode == SpriteAnimationStateChangeMode.Resume && clip is not null)
            {
                resumedElapsedTicks[clip.Name] = elapsedTicks;
            }

            state = nextState;
            clip = ResolveClip(nextAnimations, nextState);
            elapsedTicks = stateChangeMode == SpriteAnimationStateChangeMode.Resume &&
                nextState is not null &&
                resumedElapsedTicks.TryGetValue(nextState, out long saved)
                    ? saved
                    : 0;
        }
    }

    internal bool Restart()
    {
        SpriteAnimationFrame? previousFrame = CurrentFrame;
        elapsedTicks = 0;
        if (state is not null)
        {
            resumedElapsedTicks.Remove(state);
        }
        return !FramesHaveSamePresentation(previousFrame, CurrentFrame);
    }

    internal bool Advance(TimeSpan frameTime, double playbackRate, bool isPaused)
    {
        if (frameTime < TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(frameTime));
        }
        if (!double.IsFinite(playbackRate) || playbackRate < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(playbackRate));
        }
        Sync();
        if (clip is null || isPaused || playbackRate == 0 || !CanChangePresentation)
        {
            return false;
        }

        SpriteAnimationFrame? previousFrame = CurrentFrame;
        long deltaTicks = SpriteAnimationSampler.ScaleTicks(frameTime.Ticks, playbackRate);
        elapsedTicks = SaturatingAdd(elapsedTicks, deltaTicks);
        return !FramesHaveSamePresentation(previousFrame, CurrentFrame);
    }

    // Attaches or detaches the clock that drives this playback at rate one.
    // Time already accrued from a previous clock is kept.
    internal void AttachClock(SpriteAnimationClock? next)
    {
        Sync();
        clock = next;
        syncedClockTicks = next?.Ticks ?? 0;
        reportedFrame = CurrentFrame;
    }

    // Clock ticks at which the presentation can next change, or MaxValue.
    internal long NextChangeClockTicks
    {
        get
        {
            Sync();
            if (clock is null || !CanChangePresentation)
            {
                return long.MaxValue;
            }

            _ = CurrentFrame;
            long nextElapsed = clip!.IsLooping
                ? sampledEnd
                : Math.Min(sampledEnd, clip.LastPresentationChangeTicks);
            long remaining = Math.Max(1, nextElapsed - elapsedTicks);
            return remaining > long.MaxValue - clock.Ticks ? long.MaxValue : clock.Ticks + remaining;
        }
    }

    // Reports whether the clock-driven frame differs from the one last reported,
    // exactly as a per-frame advance would at the frame the boundary is crossed.
    internal bool AdvanceToClock()
    {
        SpriteAnimationFrame? current = CurrentFrame;
        bool changed = !FramesHaveSamePresentation(reportedFrame, current);
        reportedFrame = current;
        return changed;
    }

    // Geometry, culling and recording read the frame several times per frame;
    // sample the immutable clip once per playback position.
    internal SpriteAnimationFrame? CurrentFrame
    {
        get
        {
            Sync();
            if (clip is null)
            {
                return null;
            }

            if (!ReferenceEquals(sampledClip, clip) || elapsedTicks < sampledStart || elapsedTicks >= sampledEnd)
            {
                sampledFrame = SpriteAnimationSampler.SampleWithRange(clip, elapsedTicks, out sampledStart, out sampledEnd);
                sampledClip = clip;
            }

            return sampledFrame;
        }
    }

    internal bool IsActive(double playbackRate, bool isPaused)
    {
        Sync();
        return clip is not null &&
            !isPaused &&
            playbackRate > 0 &&
            double.IsFinite(playbackRate) &&
            CanChangePresentation;
    }

    private bool CanChangePresentation =>
        clip is not null && clip.LastPresentationChangeTicks > 0 &&
        elapsedTicks < long.MaxValue &&
        (clip.IsLooping || elapsedTicks < clip.LastPresentationChangeTicks);

    private void Sync()
    {
        if (clock is not null && clock.Ticks != syncedClockTicks)
        {
            elapsedTicks = SaturatingAdd(elapsedTicks, clock.Ticks - syncedClockTicks);
            syncedClockTicks = clock.Ticks;
        }
    }

    private static SpriteAnimationClip? ResolveClip(SpriteAnimationSet? animations, string? state) =>
        animations is not null && state is not null && animations.TryGetClip(state, out SpriteAnimationClip? resolved)
            ? resolved
            : null;

    internal static bool FramesHaveSamePresentation(
        SpriteAnimationFrame? left,
        SpriteAnimationFrame? right) =>
        ReferenceEquals(left, right) ||
        (left is not null && right is not null &&
         left.SourceRect == right.SourceRect && left.Flip == right.Flip &&
         left.Offset == right.Offset && left.FrameSize == right.FrameSize);

    private static long SaturatingAdd(long left, long right) =>
        right > long.MaxValue - left ? long.MaxValue : left + right;
}

// Render time accumulated by one surface for the playbacks it schedules.
internal sealed class SpriteAnimationClock
{
    internal long Ticks { get; private set; }

    internal void Advance(long ticks) =>
        Ticks = ticks > long.MaxValue - Ticks ? long.MaxValue : Ticks + ticks;
}
