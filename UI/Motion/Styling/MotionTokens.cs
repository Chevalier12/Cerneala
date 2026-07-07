using Cerneala.UI.Motion.Specs;

namespace Cerneala.UI.Motion.Styling;

public sealed class MotionTokens
{
    private readonly Dictionary<string, MotionSpec> specs = new(StringComparer.Ordinal);

    public TimeSpan DefaultDuration { get; } = TimeSpan.FromMilliseconds(200);

    public MotionTokens Set(string name, MotionSpec spec)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Motion token name cannot be empty.", nameof(name));
        }

        specs[name] = spec ?? throw new ArgumentNullException(nameof(spec));
        return this;
    }

    public bool TryGet(string name, out MotionSpec spec)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        return specs.TryGetValue(name, out spec!);
    }

    public MotionSpec Get(string name)
    {
        if (TryGet(name, out MotionSpec? spec))
        {
            return spec;
        }

        throw new KeyNotFoundException($"MotionTokens does not contain token '{name}'.");
    }
}
