using Cerneala.Timbre.Catalog;

namespace Cerneala.Timbre;

public abstract class SoundModifier
{
    private protected SoundModifier(TimbreCatalogInput[] catalogInputs)
    {
        CatalogInputs = catalogInputs;
    }

    // Inputs in catalog order; GetInput(i) feeds CatalogInputs[i].
    internal TimbreCatalogInput[] CatalogInputs { get; }

    internal abstract SoundInput<float> GetInput(int index);

    private protected static SoundInput<float> ResolveInput(SoundInput<float>? input, TimbreCatalogInput catalog, string parameterName)
    {
        SoundInput<float> resolved = input ?? new SoundInput<float>(catalog.DefaultValue);
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

public sealed class LowPass : SoundModifier
{
    public LowPass(SoundInput<float>? cutoff = null)
        : base(TimbreCatalog.LowPassInputs)
    {
        Cutoff = ResolveInput(cutoff, TimbreCatalog.LowPassCutoff, nameof(cutoff));
    }

    public SoundInput<float> Cutoff { get; }

    internal override SoundInput<float> GetInput(int index) => index == 0 ? Cutoff : throw new ArgumentOutOfRangeException(nameof(index));
}

public sealed class Delay : SoundModifier
{
    public Delay(SoundInput<float>? time = null, SoundInput<float>? feedback = null, SoundInput<float>? mix = null)
        : base(TimbreCatalog.DelayInputs)
    {
        Time = ResolveInput(time, TimbreCatalog.DelayTime, nameof(time));
        Feedback = ResolveInput(feedback, TimbreCatalog.DelayFeedback, nameof(feedback));
        Mix = ResolveInput(mix, TimbreCatalog.DelayMix, nameof(mix));
    }

    public SoundInput<float> Time { get; }

    public SoundInput<float> Feedback { get; }

    public SoundInput<float> Mix { get; }

    internal override SoundInput<float> GetInput(int index) => index switch
    {
        0 => Time,
        1 => Feedback,
        2 => Mix,
        _ => throw new ArgumentOutOfRangeException(nameof(index))
    };
}
