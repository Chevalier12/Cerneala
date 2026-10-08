using Cerneala.Timbre.Catalog;

namespace Cerneala.Timbre;

public abstract class TimbreModifier
{
    private protected TimbreModifier(TimbreCatalogInput[] catalogInputs)
    {
        CatalogInputs = catalogInputs;
    }

    // Inputs in catalog order; GetInput(i) feeds CatalogInputs[i].
    internal TimbreCatalogInput[] CatalogInputs { get; }

    internal abstract TimbreInput<float> GetInput(int index);

    private protected static TimbreInput<float> ResolveInput(TimbreInput<float>? input, TimbreCatalogInput catalog, string parameterName)
    {
        TimbreInput<float> resolved = input ?? new TimbreInput<float>(catalog.DefaultValue);
        if (!resolved.IsParameter && !catalog.Contains(resolved.Value))
        {
            throw new ArgumentOutOfRangeException(
                parameterName,
                resolved.Value,
                $"{catalog.Owner}.{catalog.Name} must be finite and within {catalog.Minimum}–{catalog.Maximum} {catalog.Unit}.");
        }

        return resolved;
    }
}

public sealed class LowPass : TimbreModifier
{
    public LowPass(TimbreInput<float>? cutoff = null)
        : base(TimbreCatalog.LowPassInputs)
    {
        Cutoff = ResolveInput(cutoff, TimbreCatalog.LowPassCutoff, nameof(cutoff));
    }

    public TimbreInput<float> Cutoff { get; }

    internal override TimbreInput<float> GetInput(int index) => index == 0 ? Cutoff : throw new ArgumentOutOfRangeException(nameof(index));
}

public sealed class Delay : TimbreModifier
{
    public Delay(TimbreInput<float>? time = null, TimbreInput<float>? feedback = null, TimbreInput<float>? mix = null)
        : base(TimbreCatalog.DelayInputs)
    {
        Time = ResolveInput(time, TimbreCatalog.DelayTime, nameof(time));
        Feedback = ResolveInput(feedback, TimbreCatalog.DelayFeedback, nameof(feedback));
        Mix = ResolveInput(mix, TimbreCatalog.DelayMix, nameof(mix));
    }

    public TimbreInput<float> Time { get; }

    public TimbreInput<float> Feedback { get; }

    public TimbreInput<float> Mix { get; }

    internal override TimbreInput<float> GetInput(int index) => index switch
    {
        0 => Time,
        1 => Feedback,
        2 => Mix,
        _ => throw new ArgumentOutOfRangeException(nameof(index))
    };
}
