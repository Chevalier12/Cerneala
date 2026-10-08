using System.Runtime.CompilerServices;
using Cerneala.Timbre;
using Cerneala.UI.Elements;

namespace Cerneala.UI.Timbre;

// The sounds one Aspect application brings to its element: a TimbreClip with
// one playback slot per sound, in a scope of its own. It is a lifetime of the
// Aspect behavior: replacing the Aspect or detaching the element disposes it,
// which stops every sound it started. Renderability is ignored.
//
// Each element has at most one current attachment, registered here so that
// commands written in other Aspects (`@pause $Speaker.timbre.Music;`) reach
// the sounds of the Aspect the element has now.
internal sealed class TimbreAttachment : IDisposable
{
    private static readonly ConditionalWeakTable<UIElement, TimbreAttachment> Current = new();

    private readonly UIElement target;
    private readonly TimbreClipDefinition clip;
    private readonly IReadOnlyDictionary<string, float> arguments;
    private readonly ElementTimbreOwner owner;
    private readonly Dictionary<string, TimbreHandle> handles = new(StringComparer.Ordinal);
    private TimbreScope? handleScope;
    private bool disposed;

    private TimbreAttachment(UIElement target, TimbreClipDefinition clip, IReadOnlyDictionary<string, float> arguments)
    {
        this.target = target;
        this.clip = clip;
        this.arguments = arguments;
        owner = new ElementTimbreOwner(target);
    }

    // Registers the attachment as the element's current one and starts every
    // AutoPlay sound in declaration order.
    public static TimbreAttachment Attach(UIElement target, TimbreClipDefinition clip, IReadOnlyDictionary<string, float>? arguments)
    {
        ArgumentNullException.ThrowIfNull(target);
        ArgumentNullException.ThrowIfNull(clip);
        Dictionary<string, float> values = new(StringComparer.Ordinal);
        foreach (KeyValuePair<string, float> argument in arguments ?? new Dictionary<string, float>())
        {
            if (!clip.Parameters.Any(parameter => parameter.Name == argument.Key))
            {
                throw new InvalidOperationException(
                    $"TimbreClip '{clip.Name}' has no parameter '{argument.Key}' for @timbre to set.");
            }

            values.Add(argument.Key, argument.Value);
        }

        if (!target.IsAttached)
        {
            throw new InvalidOperationException("A TimbreClip can be attached only while its element is attached.");
        }

        TimbreAttachment attachment = new(target, clip, values);
        Current.AddOrUpdate(target, attachment);
        foreach (TimbreClipSound sound in clip.Sounds.Values)
        {
            if (sound.AutoPlay)
            {
                attachment.Play(sound.Name);
            }
        }

        return attachment;
    }

    // The current attachment of `target` that has `sound`.
    public static TimbreAttachment Require(UIElement target, string sound)
    {
        ArgumentNullException.ThrowIfNull(target);
        ArgumentException.ThrowIfNullOrWhiteSpace(sound);
        if (!target.IsAttached)
        {
            throw new InvalidOperationException(
                $"Sound '{sound}' of a {target.GetType().Name} cannot be used: the element is not attached.");
        }

        if (!Current.TryGetValue(target, out TimbreAttachment? attachment) || attachment.disposed)
        {
            throw new InvalidOperationException(
                $"The {target.GetType().Name} has no sound '{sound}': its current Aspect brings no @timbre.");
        }

        if (!attachment.clip.Sounds.ContainsKey(sound))
        {
            throw new InvalidOperationException(
                $"The {target.GetType().Name} has no sound '{sound}': its current Aspect's TimbreClip '{attachment.clip.Name}' does not declare it.");
        }

        return attachment;
    }

    // Starts the sound; a running playback of the same sound is replaced.
    public TimbrePlayback Play(string sound)
    {
        ObjectDisposedException.ThrowIf(disposed, this);
        TimbreClipSound declared = clip.Sounds[sound];
        TimbreScope scope = Scope();
        return scope.Play(declared.Sound, Configure(declared.Sound), Handle(scope, sound));
    }

    public void Stop(string sound)
    {
        if (handleScope is { IsDisposed: false } && handles.TryGetValue(sound, out TimbreHandle? handle))
        {
            handle.Cancel();
        }
    }

    // The running playback of the sound, or null.
    public TimbrePlayback? Playing(string sound) =>
        !disposed && handleScope is { IsDisposed: false } && handles.TryGetValue(sound, out TimbreHandle? handle)
            ? handle.Current
            : null;

    public void Dispose()
    {
        if (disposed)
        {
            return;
        }

        disposed = true;
        if (Current.TryGetValue(target, out TimbreAttachment? current) && ReferenceEquals(current, this))
        {
            Current.Remove(target);
        }

        owner.Retire();
        handles.Clear();
        handleScope = null;
    }

    private Action<TimbreStartOptions>? Configure(TimbreSound sound)
    {
        if (arguments.Count == 0)
        {
            return null;
        }

        List<(TimbreParameter<float> Parameter, float Value)> values = new();
        foreach (TimbreParameter parameter in sound.Parameters)
        {
            if (parameter is TimbreParameter<float> typed && arguments.TryGetValue(parameter.Name, out float value))
            {
                values.Add((typed, value));
            }
        }

        return values.Count == 0
            ? null
            : options =>
            {
                foreach ((TimbreParameter<float> parameter, float value) in values)
                {
                    options.Set(parameter, value);
                }
            };
    }

    private TimbreScope Scope()
    {
        TimbreScope scope = owner.GetScope();
        if (!ReferenceEquals(scope, handleScope))
        {
            handles.Clear();
            handleScope = scope;
        }

        return scope;
    }

    private TimbreHandle Handle(TimbreScope scope, string sound)
    {
        if (!handles.TryGetValue(sound, out TimbreHandle? handle))
        {
            handle = scope.CreateHandle();
            handles.Add(sound, handle);
        }

        return handle;
    }
}
