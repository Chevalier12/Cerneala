using Cerneala.Timbre;
using Cerneala.UI.Elements;
using Cerneala.UI.Motion.Core;
using Cerneala.UI.Motion.Properties;
using Cerneala.UI.Motion.Specs;
using Cerneala.UI.Resources;
using Cerneala.UI.Timbre;

namespace Cerneala.UI.Markup;

public static partial class GeneratedMarkup
{
    // The `@timbre $Clip(args);` of an Aspect: a lifetime of the Aspect
    // behavior. The resource is resolved now, at the application.
    public static IDisposable AttachTimbre(
        UIElement target,
        ResourceId<TimbreClipDefinition> clip,
        IReadOnlyDictionary<string, float>? arguments)
    {
        ArgumentNullException.ThrowIfNull(target);
        return TimbreAttachment.Attach(target, target.FindResource(clip), arguments);
    }

    // The inline `@timbre { … }` of an Aspect.
    public static IDisposable AttachTimbre(
        UIElement target,
        TimbreClipDefinition clip,
        IReadOnlyDictionary<string, float>? arguments)
    {
        return TimbreAttachment.Attach(target, clip, arguments);
    }

    // `@play $X.timbre.Sound;`: starts the sound of the target's current
    // Aspect, restarting it when it is already playing.
    public static TimbrePlayback PlayTimbre(UIElement target, string sound)
    {
        return TimbreAttachment.Require(target, sound).Play(sound);
    }

    public static void StopTimbre(UIElement target, string sound)
    {
        TimbreAttachment.Require(target, sound).Stop(sound);
    }

    public static void PauseTimbre(UIElement target, string sound)
    {
        _ = TimbreAttachment.Require(target, sound).Playing(sound)?.TryPause();
    }

    public static void ResumeTimbre(UIElement target, string sound)
    {
        _ = TimbreAttachment.Require(target, sound).Playing(sound)?.TryResume();
    }

    public static Task SeekTimbre(UIElement target, string sound, TimeSpan position)
    {
        return TimbreAttachment.Require(target, sound).Playing(sound)?.TrySeekAsync(position) ?? Task.CompletedTask;
    }

    // The Timbre session of one Aspect application: owns the event handlers
    // of @on bodies with Timbre commands and the audio Motion executions the
    // Aspect starts. A lifetime of the Aspect behavior; hiding the owner does
    // not affect it.
    public static IDisposable AttachTimbreSession(UIElement owner)
    {
        ArgumentNullException.ThrowIfNull(owner);
        MarkupTimbreSession session = new(owner);
        owner.AddLifecycleBehavior(session);
        if (owner.IsAttached)
        {
            session.Attach();
        }

        return session;
    }

    public static void AddTimbreTrigger(IDisposable session, Action attach, Action detach)
    {
        GetTimbreSession(session).AddTrigger(attach, detach);
    }

    public static TimbreParameter<float> GetTimbreParameter(TimbreSound sound, string name)
    {
        ArgumentNullException.ThrowIfNull(sound);
        ArgumentNullException.ThrowIfNull(name);
        foreach (TimbreParameter parameter in sound.Parameters)
        {
            if (string.Equals(parameter.Name, name, StringComparison.Ordinal) && parameter is TimbreParameter<float> typed)
            {
                return typed;
            }
        }

        throw new ArgumentException($"The sound declares no float parameter named '{name}'.", nameof(name));
    }

    // Starts an audio Motion execution owned by the Timbre session: it survives
    // hiding the owner and ends with the session.
    public static MarkupMotionExecution StartTimbreMotion(IDisposable session, Func<MarkupMotionExecution> start)
    {
        ArgumentNullException.ThrowIfNull(start);
        return GetTimbreSession(session).StartMotion(start);
    }

    // One `$X.timbre.Sound.Property` leaf: captures the sound's current
    // playback and the named descriptor of its sound once, at activation. A
    // sound that is not playing is a no-op.
    public static MotionHandle StartTimbreMotionProperty(
        UIElement target,
        string sound,
        string parameterName,
        bool hasFrom,
        float from,
        bool toCurrent,
        float to,
        MotionSpec<float>? spec,
        MotionPropertyStartOptions options)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(parameterName);
        ArgumentNullException.ThrowIfNull(options);
        if (TimbreAttachment.Require(target, sound).Playing(sound) is not TimbrePlayback playback || target.Root is not UIRoot root)
        {
            return TimbrePlaybackMotion.CompletedNoOp();
        }

        TimbreParameter<float> parameter = parameterName == "Volume"
            ? TimbrePlayback.VolumeParameter
            : GetTimbreParameter(playback.Sound, parameterName);
        return TimbrePlaybackMotion.AnimateOccupant(root, playback, parameter, hasFrom, from, toCurrent, to, spec ?? TimbrePlaybackMotion.DefaultSpec, options);
    }

    private static MarkupTimbreSession GetTimbreSession(IDisposable session)
    {
        ArgumentNullException.ThrowIfNull(session);
        return session as MarkupTimbreSession
            ?? throw new ArgumentException("The lifetime was not created by AttachTimbreSession.", nameof(session));
    }

    private sealed class MarkupTimbreSession : IElementLifecycleBehavior, IDisposable
    {
        private readonly UIElement owner;
        private readonly List<(Action Attach, Action Detach)> triggers = [];
        private readonly List<MarkupMotionExecution> motionExecutions = [];
        private bool attached;
        private bool triggersAttached;
        private bool disposed;

        public MarkupTimbreSession(UIElement owner)
        {
            this.owner = owner;
        }

        public void Attach()
        {
            if (attached || disposed)
            {
                return;
            }

            attached = true;
            UpdateTriggers();
        }

        public void Detach()
        {
            if (!attached)
            {
                return;
            }

            attached = false;
            UpdateTriggers();
            Retire();
        }

        public void Dispose()
        {
            if (disposed)
            {
                return;
            }

            Detach();
            disposed = true;
            Retire();
            owner.RemoveLifecycleBehavior(this);
        }

        public void AddTrigger(Action attach, Action detach)
        {
            ArgumentNullException.ThrowIfNull(attach);
            ArgumentNullException.ThrowIfNull(detach);
            ObjectDisposedException.ThrowIf(disposed, this);
            triggers.Add((attach, detach));
            if (triggersAttached)
            {
                attach();
            }
        }

        // Audio executions are owned by this session, not by the visual Motion
        // session: renderability is ignored and Retire cancels them.
        public MarkupMotionExecution StartMotion(Func<MarkupMotionExecution> start)
        {
            ObjectDisposedException.ThrowIf(disposed, this);
            if (!attached || !owner.IsAttached)
            {
                throw new InvalidOperationException("Markup sound Motion can start only while its owner is attached.");
            }

            MarkupMotionExecution execution = start() ?? throw new InvalidOperationException("A Motion execution returned null.");
            motionExecutions.RemoveAll(candidate => candidate.IsCompleted || candidate.IsCanceled);
            if (!execution.IsCompleted && !execution.IsCanceled)
            {
                motionExecutions.Add(execution);
            }

            return execution;
        }

        public void OnRenderabilityChanged(bool isRenderable)
        {
        }

        private void UpdateTriggers()
        {
            bool active = attached && !disposed;
            if (active == triggersAttached)
            {
                return;
            }

            triggersAttached = active;
            foreach ((Action attach, Action detach) in triggers)
            {
                (active ? attach : detach)();
            }
        }

        private void Retire()
        {
            MarkupMotionExecution[] active = motionExecutions.ToArray();
            motionExecutions.Clear();
            foreach (MarkupMotionExecution execution in active)
            {
                execution.Cancel();
            }
        }
    }
}
