using Cerneala.Timbre;
using Cerneala.UI.Aspect;
using Cerneala.UI.Core;
using Cerneala.UI.Elements;
using Cerneala.UI.Resources;
using Cerneala.UI.Timbre;

namespace Cerneala.UI.Markup;

public static partial class GeneratedMarkup
{
    public static IDisposable AttachSoundSession(UIElement owner)
    {
        ArgumentNullException.ThrowIfNull(owner);
        return AttachSoundSessionCore(owner, null, aspectScoped: false);
    }

    public static IDisposable AttachSoundSession(UIElement owner, ElementAspect? aspect)
    {
        ArgumentNullException.ThrowIfNull(owner);
        return AttachSoundSessionCore(owner, aspect, aspectScoped: true);
    }

    public static void AddSoundTrigger(IDisposable session, Action attach, Action detach)
    {
        GetSoundSession(session).AddTrigger(attach, detach);
    }

    public static SoundPlayback? PlaySound(
        IDisposable session,
        ResourceId<SoundClip> clip,
        Action<SoundClip, SoundStartOptions>? configure,
        string? handleName)
    {
        return GetSoundSession(session).Play(clip, configure, handleName);
    }

    public static SoundParameter<float> GetSoundParameter(SoundClip clip, string name)
    {
        ArgumentNullException.ThrowIfNull(clip);
        ArgumentNullException.ThrowIfNull(name);
        foreach (SoundParameter parameter in clip.Parameters)
        {
            if (string.Equals(parameter.Name, name, StringComparison.Ordinal) && parameter is SoundParameter<float> typed)
            {
                return typed;
            }
        }

        throw new ArgumentException($"The sound clip declares no float parameter named '{name}'.", nameof(name));
    }

    public static void CancelSound(IDisposable session, string handleName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(handleName);
        GetSoundSession(session).Cancel(handleName);
    }

    public static void PauseSound(IDisposable session, string handleName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(handleName);
        _ = GetSoundSession(session).Occupant(handleName)?.TryPause();
    }

    public static void ResumeSound(IDisposable session, string handleName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(handleName);
        _ = GetSoundSession(session).Occupant(handleName)?.TryResume();
    }

    public static Task SeekSound(IDisposable session, string handleName, TimeSpan position)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(handleName);
        return GetSoundSession(session).Occupant(handleName)?.TrySeekAsync(position) ?? Task.CompletedTask;
    }

    private static IDisposable AttachSoundSessionCore(UIElement owner, ElementAspect? aspect, bool aspectScoped)
    {
        MarkupSoundSession session = new(owner, aspect, aspectScoped);
        owner.AddLifecycleBehavior(session);
        if (owner.IsAttached)
        {
            session.Attach();
        }

        return session;
    }

    private static MarkupSoundSession GetSoundSession(IDisposable session)
    {
        ArgumentNullException.ThrowIfNull(session);
        return session as MarkupSoundSession
            ?? throw new ArgumentException("The lifetime was not created by AttachSoundSession.", nameof(session));
    }

    // Audio owner of one concrete Aspect application: a Timbre scope created
    // from the root runtime on first use, plus the named handle slots. Detach,
    // replacement of the captured Aspect and disposal retire the scope and
    // cancel every playback it started; renderability is ignored.
    private sealed class MarkupSoundSession : IElementLifecycleBehavior, IDisposable
    {
        private readonly UIElement owner;
        private readonly bool aspectScoped;
        private readonly ElementSoundOwner sounds;
        private readonly List<(Action Attach, Action Detach)> triggers = [];
        private readonly Dictionary<string, SoundHandle> slots = new(StringComparer.Ordinal);
        private ElementAspect? aspect;
        private SoundScope? slotScope;
        private bool attached;
        private bool triggersAttached;
        private bool disposed;

        public MarkupSoundSession(UIElement owner, ElementAspect? aspect, bool aspectScoped)
        {
            this.owner = owner;
            this.aspect = aspect;
            this.aspectScoped = aspectScoped;
            sounds = new ElementSoundOwner(owner);
        }

        private bool IsOwnedByCurrentAspect =>
            !aspectScoped || (aspect is not null && ReferenceEquals(owner.Aspect, aspect));

        public void Attach()
        {
            if (attached || disposed)
            {
                return;
            }

            attached = true;
            owner.PropertyChanged += OnOwnerPropertyChanged;
            CaptureAspectIfNeeded();
            UpdateTriggers();
        }

        public void Detach()
        {
            if (!attached)
            {
                return;
            }

            attached = false;
            owner.PropertyChanged -= OnOwnerPropertyChanged;
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

        public SoundPlayback? Play(
            ResourceId<SoundClip> clipId,
            Action<SoundClip, SoundStartOptions>? configure,
            string? handleName)
        {
            ObjectDisposedException.ThrowIf(disposed, this);
            if (aspectScoped && !IsOwnedByCurrentAspect)
            {
                return null;
            }

            if (!attached || !owner.IsAttached)
            {
                throw new InvalidOperationException("Markup sounds can start only while their owner is attached.");
            }

            SoundScope scope = Scope();
            SoundClip clip = owner.FindResource(clipId);
            Action<SoundStartOptions>? start = configure is null ? null : options => configure(clip, options);
            return scope.Play(clip, start, handleName is null ? null : Slot(scope, handleName));
        }

        public void Cancel(string handleName)
        {
            if (slotScope is { IsDisposed: false } && slots.TryGetValue(handleName, out SoundHandle? slot))
            {
                slot.Cancel();
            }
        }

        // The current non-terminal occupant of a slot; an empty slot is null.
        public SoundPlayback? Occupant(string handleName) =>
            slotScope is { IsDisposed: false } && slots.TryGetValue(handleName, out SoundHandle? slot)
                ? slot.Current
                : null;

        public void OnRenderabilityChanged(bool isRenderable)
        {
        }

        private SoundScope Scope()
        {
            SoundScope scope = sounds.GetScope();
            if (!ReferenceEquals(scope, slotScope))
            {
                slots.Clear();
                slotScope = scope;
            }

            return scope;
        }

        private SoundHandle Slot(SoundScope scope, string handleName)
        {
            if (!slots.TryGetValue(handleName, out SoundHandle? slot))
            {
                slot = scope.CreateHandle();
                slots.Add(handleName, slot);
            }

            return slot;
        }

        private void OnOwnerPropertyChanged(object? sender, UiPropertyChangedEventArgs args)
        {
            if (!aspectScoped || !ReferenceEquals(args.Property, UIElement.AspectProperty))
            {
                return;
            }

            CaptureAspectIfNeeded();
            if (!IsOwnedByCurrentAspect)
            {
                Retire();
            }

            UpdateTriggers();
        }

        private void CaptureAspectIfNeeded()
        {
            if (aspectScoped && aspect is null && owner.Aspect is ElementAspect current)
            {
                aspect = current;
            }
        }

        private void UpdateTriggers()
        {
            bool active = attached && !disposed && IsOwnedByCurrentAspect;
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
            sounds.Retire();
            slots.Clear();
            slotScope = null;
        }
    }
}
