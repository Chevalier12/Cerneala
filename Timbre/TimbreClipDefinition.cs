using System.Collections;
using System.Diagnostics.CodeAnalysis;

namespace Cerneala.Timbre;

// A set of named sounds with clip-level parameters, the C# form of a markup
// TimbreClip and the counterpart of PrismClipDefinition. Immutable; nothing
// is opened or played by declaring it.
public sealed class TimbreClipDefinition
{
    public TimbreClipDefinition(
        string name,
        IEnumerable<TimbreClipSound> sounds,
        IEnumerable<TimbreParameter>? parameters = null)
    {
        ArgumentNullException.ThrowIfNull(name);
        ArgumentNullException.ThrowIfNull(sounds);
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("A Timbre clip name is required.", nameof(name));
        }

        TimbreParameter[] declared = parameters?.ToArray() ?? [];
        HashSet<TimbreParameter> declaredSet = new(ReferenceEqualityComparer.Instance);
        HashSet<string> parameterNames = new(StringComparer.Ordinal);
        foreach (TimbreParameter? parameter in declared)
        {
            if (parameter is null)
            {
                throw new ArgumentException("Timbre clip parameters cannot contain null.", nameof(parameters));
            }

            if (!declaredSet.Add(parameter) || !parameterNames.Add(parameter.Name))
            {
                throw new ArgumentException($"Timbre clip parameter '{parameter.Name}' is declared more than once.", nameof(parameters));
            }
        }

        TimbreClipSound[] ordered = sounds.ToArray();
        if (ordered.Length == 0)
        {
            throw new ArgumentException("A Timbre clip must contain at least one sound.", nameof(sounds));
        }

        Dictionary<string, TimbreClipSound> byName = new(StringComparer.Ordinal);
        foreach (TimbreClipSound? sound in ordered)
        {
            if (sound is null)
            {
                throw new ArgumentException("Timbre clip sounds cannot contain null.", nameof(sounds));
            }

            if (!byName.TryAdd(sound.Name, sound))
            {
                throw new ArgumentException($"Timbre sound '{sound.Name}' is declared more than once.", nameof(sounds));
            }

            foreach (TimbreParameter parameter in sound.Sound.Parameters)
            {
                if (!declaredSet.Contains(parameter))
                {
                    throw new ArgumentException(
                        $"Timbre sound '{sound.Name}' uses parameter '{parameter.Name}', which the clip does not declare.",
                        nameof(sounds));
                }
            }
        }

        Name = name;
        Sounds = new OrderedSounds(ordered, byName);
        Parameters = Array.AsReadOnly(declared);
    }

    public string Name { get; }

    // Keyed by sound name; enumerates in declaration order.
    public IReadOnlyDictionary<string, TimbreClipSound> Sounds { get; }

    public IReadOnlyList<TimbreParameter> Parameters { get; }

    public override string ToString() => Name;

    private sealed class OrderedSounds(TimbreClipSound[] ordered, Dictionary<string, TimbreClipSound> byName)
        : IReadOnlyDictionary<string, TimbreClipSound>
    {
        public TimbreClipSound this[string key] => byName[key];

        public IEnumerable<string> Keys => ordered.Select(sound => sound.Name);

        public IEnumerable<TimbreClipSound> Values => ordered;

        public int Count => ordered.Length;

        public bool ContainsKey(string key) => byName.ContainsKey(key);

        public bool TryGetValue(string key, [MaybeNullWhen(false)] out TimbreClipSound value) => byName.TryGetValue(key, out value);

        public IEnumerator<KeyValuePair<string, TimbreClipSound>> GetEnumerator() =>
            ordered.Select(sound => new KeyValuePair<string, TimbreClipSound>(sound.Name, sound)).GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}
