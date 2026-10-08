using Cerneala.Timbre.Catalog;

namespace Cerneala.Timbre;

public sealed class TimbreSound
{
    private readonly TimbreParameter[] parameters;
    private readonly TimbreModifier[] modifiers;
    private readonly Dictionary<TimbreParameter, int> parameterIndexes = new(ReferenceEqualityComparer.Instance);
    private readonly float[] defaults;
    private readonly float[] minimums;
    private readonly float[] maximums;

    public TimbreSound(
        TimbreSource source,
        float volume = 1f,
        bool loop = false,
        TimbreLoading loading = TimbreLoading.Auto,
        IEnumerable<TimbreParameter>? parameters = null,
        IEnumerable<TimbreModifier>? modifiers = null)
    {
        ArgumentNullException.ThrowIfNull(source);
        ValidateVolume(volume, nameof(volume));
        if (!Enum.IsDefined(loading))
        {
            throw new ArgumentOutOfRangeException(nameof(loading), loading, "Unknown sound loading policy.");
        }

        this.parameters = parameters?.ToArray() ?? [];
        this.modifiers = modifiers?.ToArray() ?? [];
        defaults = new float[this.parameters.Length];
        minimums = new float[this.parameters.Length];
        maximums = new float[this.parameters.Length];
        HashSet<string> names = new(StringComparer.Ordinal);
        for (int index = 0; index < this.parameters.Length; index++)
        {
            TimbreParameter parameter = this.parameters[index] ??
                throw new ArgumentException("Timbre clip parameters cannot contain null.", nameof(parameters));
            if (ReferenceEquals(parameter, TimbrePlayback.VolumeParameter))
            {
                throw new ArgumentException("TimbrePlayback.VolumeParameter is intrinsic to every playback and cannot be declared by a clip.", nameof(parameters));
            }

            if (!parameterIndexes.TryAdd(parameter, index) || !names.Add(parameter.Name))
            {
                throw new ArgumentException($"Timbre clip parameter '{parameter.Name}' is declared more than once.", nameof(parameters));
            }

            defaults[index] = ((TimbreParameter<float>)parameter).DefaultValue;
            minimums[index] = float.NegativeInfinity;
            maximums[index] = float.PositiveInfinity;
        }

        foreach (TimbreModifier? modifier in this.modifiers)
        {
            if (modifier is null)
            {
                throw new ArgumentException("Timbre clip modifiers cannot contain null.", nameof(modifiers));
            }

            for (int input = 0; input < modifier.CatalogInputs.Length; input++)
            {
                TimbreParameter<float>? parameter = modifier.GetInput(input).Parameter;
                if (parameter is null)
                {
                    continue;
                }

                if (!parameterIndexes.TryGetValue(parameter, out int index))
                {
                    throw new ArgumentException(
                        $"Modifier input {modifier.CatalogInputs[input].Owner}.{modifier.CatalogInputs[input].Name} uses parameter '{parameter.Name}', which the clip does not declare.",
                        nameof(modifiers));
                }

                minimums[index] = Math.Max(minimums[index], modifier.CatalogInputs[input].Minimum);
                maximums[index] = Math.Min(maximums[index], modifier.CatalogInputs[input].Maximum);
            }
        }

        for (int index = 0; index < defaults.Length; index++)
        {
            if (defaults[index] < minimums[index] || defaults[index] > maximums[index])
            {
                throw new ArgumentOutOfRangeException(
                    nameof(parameters),
                    defaults[index],
                    $"Parameter '{this.parameters[index].Name}' default is outside the range {minimums[index]}–{maximums[index]} of the modifier inputs it feeds.");
            }
        }

        Source = source;
        Volume = volume;
        Loop = loop;
        Loading = loading;
        Parameters = Array.AsReadOnly(this.parameters);
        Modifiers = Array.AsReadOnly(this.modifiers);
    }

    public TimbreSource Source { get; }

    public float Volume { get; }

    public bool Loop { get; }

    public TimbreLoading Loading { get; }

    public IReadOnlyList<TimbreParameter> Parameters { get; }

    public IReadOnlyList<TimbreModifier> Modifiers { get; }

    internal int ParameterCount => parameters.Length;

    internal float[] CopyDefaults() => (float[])defaults.Clone();

    internal int GetParameterIndex(TimbreParameter parameter, string argumentName)
    {
        ArgumentNullException.ThrowIfNull(parameter, argumentName);
        return parameterIndexes.TryGetValue(parameter, out int index)
            ? index
            : throw new ArgumentException($"Parameter '{parameter.Name}' is not declared by this sound clip.", argumentName);
    }

    internal bool IsParameterValueValid(int index, float value) =>
        float.IsFinite(value) && value >= minimums[index] && value <= maximums[index];

    internal void ValidateParameterValue(int index, float value, string argumentName)
    {
        if (!float.IsFinite(value) || value < minimums[index] || value > maximums[index])
        {
            throw new ArgumentOutOfRangeException(
                argumentName,
                value,
                $"Parameter '{parameters[index].Name}' must be finite and within {minimums[index]}–{maximums[index]}.");
        }
    }

    internal static void ValidateVolume(float volume, string argumentName)
    {
        if (!TimbreCatalog.Volume.Contains(volume))
        {
            throw new ArgumentOutOfRangeException(argumentName, volume, "Timbre volume must be finite and within 0–1.");
        }
    }
}
