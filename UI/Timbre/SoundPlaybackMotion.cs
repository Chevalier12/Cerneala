using System.Runtime.CompilerServices;
using Cerneala.Timbre;
using Cerneala.UI.Elements;
using Cerneala.UI.Motion.Core;
using Cerneala.UI.Motion.Interpolation;
using Cerneala.UI.Motion.Properties;
using Cerneala.UI.Motion.Specs;

namespace Cerneala.UI.Timbre;

// The single audio Motion operation behind the C# facade and the generated
// `.sound.` lowering. Each animated playback gets one SoundPlaybackMotionTarget
// sampled by the root that owns it; nothing is retargeted to a slot's later
// occupant.
internal static class SoundPlaybackMotion
{
    private static readonly ConditionalWeakTable<SoundScope, UIRoot> ScopeRoots = new();
    private static readonly ConditionalWeakTable<SoundPlayback, SoundPlaybackMotionTarget> Targets = new();
    private static readonly ConditionalWeakTable<UIRoot, RootState> Roots = new();

    // Default of `@animate` without a spec, like other non-property targets.
    internal static MotionSpec<float> DefaultSpec { get; } = new TweenSpec<float>(TimeSpan.FromMilliseconds(180), Easings.Standard);

    internal static void RegisterScope(SoundScope scope, UIRoot root) => ScopeRoots.AddOrUpdate(scope, root);

    // Audio Motion targets currently registered in the root's Motion graph.
    internal static int ActiveTargets(UIRoot root) => Roots.TryGetValue(root, out RootState? state) ? state.ActiveTargets : 0;

    internal static MotionHandle Animate(
        UIRoot? root,
        SoundPlayback playback,
        SoundParameter<float> parameter,
        bool hasFrom,
        float from,
        bool toCurrent,
        float to,
        MotionSpec<float> spec,
        MotionPropertyStartOptions options) =>
        AnimateCore(root, playback, parameter, hasFrom, from, toCurrent, to, spec, options, terminalIsNoOp: false);

    // Markup leaf: a playback that turned terminal is an empty slot.
    internal static MotionHandle AnimateOccupant(
        UIRoot root,
        SoundPlayback playback,
        SoundParameter<float> parameter,
        bool hasFrom,
        float from,
        bool toCurrent,
        float to,
        MotionSpec<float> spec,
        MotionPropertyStartOptions options) =>
        AnimateCore(root, playback, parameter, hasFrom, from, toCurrent, to, spec, options, terminalIsNoOp: true);

    internal static MotionHandle CompletedNoOp()
    {
        MotionHandle handle = new(_ => { }, () => { }, () => { });
        handle.FinishCompleted(fireEvent: false);
        return handle;
    }

    private static MotionHandle AnimateCore(
        UIRoot? root,
        SoundPlayback playback,
        SoundParameter<float> parameter,
        bool hasFrom,
        float from,
        bool toCurrent,
        float to,
        MotionSpec<float> spec,
        MotionPropertyStartOptions options,
        bool terminalIsNoOp)
    {
        ArgumentNullException.ThrowIfNull(playback);
        ArgumentNullException.ThrowIfNull(parameter);
        ArgumentNullException.ThrowIfNull(spec);
        ArgumentNullException.ThrowIfNull(options);
        UIRoot owner = root ?? (ScopeRoots.TryGetValue(playback.Scope, out UIRoot? scopeRoot)
            ? scopeRoot
            : throw new InvalidOperationException(
                "The playback's scope is not owned by an element, so no UIRoot samples its Motion; use playback.Motion(root)."));
        owner.Relay.VerifyAccess();
        int slot = playback.GetMotionSlot(parameter, nameof(parameter));
        if (hasFrom)
        {
            playback.ValidateMotionValue(slot, from, nameof(from));
        }

        if (!toCurrent)
        {
            playback.ValidateMotionValue(slot, to, nameof(to));
        }

        SoundPlaybackMotionTarget target = Targets.GetValue(
            playback,
            animated => new SoundPlaybackMotionTarget(owner, Roots.GetValue(owner, _ => new RootState()), animated));
        if (!ReferenceEquals(target.Root, owner))
        {
            throw new InvalidOperationException("The sound playback is already animated by another UIRoot.");
        }

        return target.Animate(slot, hasFrom, from, toCurrent, to, spec, options, terminalIsNoOp);
    }

    internal sealed class RootState
    {
        public int ActiveTargets { get; set; }
    }
}

// Motion of one captured playback: a node of the root's Motion graph that owns
// a private MotionGraph with one MotionValue per animated slot (0 = Volume,
// i + 1 = clip parameter i). The root frame's delta reaches that graph only
// while the playback produces PCM, so pending, paused and seeking playbacks
// hold their animation time. Samples of one frame are published together.
internal sealed class SoundPlaybackMotionTarget : MotionNode
{
    private readonly SoundPlaybackMotion.RootState rootState;
    private readonly SoundPlayback playback;
    private readonly MotionGraph graph;
    private readonly ValueMixer<float> mixer;
    private readonly Slot?[] slots;
    private readonly int[] manualVersions;
    private readonly bool[] dirty;
    private readonly float[] samples;
    private bool registered;

    public SoundPlaybackMotionTarget(UIRoot root, SoundPlaybackMotion.RootState rootState, SoundPlayback playback)
    {
        Root = root;
        this.rootState = rootState;
        this.playback = playback;
        // Own policy: the root's Reduced Motion preference is visual and does
        // not disable audio animations.
        graph = new MotionGraph(root.Motion.Mixers, new ReducedMotionPolicy());
        mixer = root.Motion.Mixers.Resolve<float>();
        int count = playback.MotionSlotCount;
        slots = new Slot?[count];
        manualVersions = new int[count];
        dirty = new bool[count];
        samples = new float[count];
    }

    public UIRoot Root { get; }

    public MotionHandle Animate(
        int index,
        bool hasFrom,
        float from,
        bool toCurrent,
        float to,
        MotionSpec<float> spec,
        MotionPropertyStartOptions options,
        bool terminalIsNoOp)
    {
        playback.ReadMotionSlot(index, out float current, out int manualVersion, out bool terminal);
        if (terminal)
        {
            return terminalIsNoOp
                ? SoundPlaybackMotion.CompletedNoOp()
                : throw new InvalidOperationException($"The sound playback is {playback.State}; start a new playback instead.");
        }

        Slot slot = slots[index] ??= CreateSlot(index, current);
        if (slot.Active is null || slot.ManualVersion != manualVersion)
        {
            // Not animating, or a manual write superseded the animation: the
            // published value is the start point, not a stale sample.
            slot.Synchronize(current);
            slot.LastPublished = current;
        }

        slot.ManualVersion = manualVersion;
        slot.Base = slot.Value.Current;
        if (hasFrom)
        {
            slot.Value.JumpTo(from);
        }

        float destination = toCurrent ? slot.Value.Current : to;
        MotionHandle handle = slot.Value.AnimateTo(destination, spec, options.ToMotionStartOptions());
        if (handle.IsCanceled)
        {
            return handle; // rejected by a higher-priority animation of the slot
        }

        slot.Track(handle, options.HoldOnComplete);
        if (Flush() < 0)
        {
            return handle;
        }

        if (slot.Active is not null && !registered)
        {
            registered = true;
            rootState.ActiveTargets++;
            Root.Motion.Graph.Register(this);
        }

        return handle;
    }

    protected internal override MotionNodeTickResult Tick(MotionFrame frame)
    {
        bool terminal = playback.ReadMotionState(manualVersions, out bool clockRunning);
        if (terminal)
        {
            CancelAll();
            return Unregistered(0);
        }

        foreach (Slot? slot in slots)
        {
            if (slot?.Active is MotionHandle active && manualVersions[slot.Index] != slot.ManualVersion)
            {
                // A manual setter owns the value from now on.
                slot.ManualVersion = manualVersions[slot.Index];
                slot.CancelQuietly(active);
                dirty[slot.Index] = false;
            }
        }

        graph.Tick(new MotionFrame(frame.Now, clockRunning ? frame.Delta : TimeSpan.Zero, frame.FrameIndex, frame.Reason, frame.Phase));
        int published = Flush();
        if (published < 0)
        {
            return Unregistered(0);
        }

        foreach (Slot? slot in slots)
        {
            if (slot?.Active is not null)
            {
                return new MotionNodeTickResult(ValuesChanged: published);
            }
        }

        return Unregistered(published);
    }

    // Publishes the dirty slots in one batch; returns the number published,
    // or -1 when the playback turned terminal (every animation is canceled).
    private int Flush()
    {
        int count = 0;
        foreach (Slot? slot in slots)
        {
            if (slot is null || !dirty[slot.Index])
            {
                continue;
            }

            if (!playback.IsValidMotionValue(slot.Index, samples[slot.Index]))
            {
                // No hidden clamp: only this slot's animation ends, on the
                // last valid value, and the playback keeps sounding.
                dirty[slot.Index] = false;
                playback.ReportRejectedMotionSample();
                slot.Reject();
                continue;
            }

            count++;
        }

        if (count == 0)
        {
            return 0;
        }

        if (!playback.TryPublishMotion(dirty, samples))
        {
            CancelAll();
            return -1;
        }

        foreach (Slot? slot in slots)
        {
            if (slot is not null && dirty[slot.Index])
            {
                slot.LastPublished = samples[slot.Index];
                dirty[slot.Index] = false;
            }
        }

        return count;
    }

    private void CancelAll()
    {
        foreach (Slot? slot in slots)
        {
            if (slot?.Active is MotionHandle active)
            {
                slot.CancelQuietly(active);
            }
        }

        Array.Clear(dirty);
    }

    private MotionNodeTickResult Unregistered(int published)
    {
        if (registered)
        {
            registered = false;
            rootState.ActiveTargets--;
        }

        return new MotionNodeTickResult(ValuesChanged: published, Completed: true);
    }

    private Slot CreateSlot(int index, float current)
    {
        Slot slot = new(index, graph.CreateValue(current, mixer));
        slot.Value.Subscribe(change =>
        {
            if (!slot.Muted)
            {
                samples[index] = change.NewValue;
                dirty[index] = true;
            }
        });
        return slot;
    }

    private sealed class Slot(int index, MotionValue<float> value)
    {
        public int Index { get; } = index;

        public MotionValue<float> Value { get; } = value;

        public MotionHandle? Active { get; private set; }

        public int ManualVersion { get; set; }

        public float Base { get; set; }

        public float LastPublished { get; set; }

        public bool Muted { get; private set; }

        private bool hold;

        public void Track(MotionHandle handle, bool holdOnComplete)
        {
            Active = handle;
            hold = holdOnComplete;
            if (handle.IsCompleted)
            {
                Finish(completed: true);
                return;
            }

            handle.Completed += (sender, args) =>
            {
                if (ReferenceEquals(sender, Active))
                {
                    Finish(args.State == MotionCompletionState.Completed);
                }
            };
        }

        // The mutating operations below run muted: their value changes are
        // not samples to publish. No closures, so the per-frame loops that
        // reach them allocate nothing.
        public void Synchronize(float current)
        {
            Muted = true;
            try
            {
                Value.JumpTo(current);
            }
            finally
            {
                Muted = false;
            }
        }

        public void CancelQuietly(MotionHandle active)
        {
            Muted = true;
            try
            {
                active.Cancel(MotionCancelBehavior.KeepCurrent);
            }
            finally
            {
                Muted = false;
            }
        }

        // An invalid sample ends this slot's animation on the last published value.
        public void Reject()
        {
            Muted = true;
            try
            {
                Active?.Cancel(MotionCancelBehavior.KeepCurrent);
                Value.JumpTo(LastPublished);
            }
            finally
            {
                Muted = false;
            }
        }

        private void Finish(bool completed)
        {
            Active = null;
            if (completed && !hold)
            {
                Value.JumpTo(Base);
            }
        }
    }
}
