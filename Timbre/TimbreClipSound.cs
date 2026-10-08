namespace Cerneala.Timbre;

// One named sound of a TimbreClipDefinition: the `@sound Name { … }` node of
// a markup TimbreClip.
public sealed class TimbreClipSound
{
    public TimbreClipSound(string name, TimbreSound sound, bool autoPlay = false)
    {
        ArgumentNullException.ThrowIfNull(name);
        ArgumentNullException.ThrowIfNull(sound);
        if (!TimbreParameter.IsIdentifier(name))
        {
            throw new ArgumentException(
                $"Timbre sound name '{name}' must be an identifier: letters, digits, or underscores, not starting with a digit.",
                nameof(name));
        }

        Name = name;
        Sound = sound;
        AutoPlay = autoPlay;
    }

    public string Name { get; }

    public TimbreSound Sound { get; }

    // Starts the sound every time the clip is attached by an Aspect.
    public bool AutoPlay { get; }

    public override string ToString() => Name;
}
