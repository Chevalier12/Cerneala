namespace Cerneala.Timbre;

public sealed class SoundStartOptions
{
    private readonly SoundClip clip;
    private float volume;
    private bool loop;
    private bool sealed_;

    internal SoundStartOptions(SoundClip clip)
    {
        this.clip = clip;
        volume = clip.Volume;
        loop = clip.Loop;
        Values = clip.CopyDefaults();
    }

    public float Volume
    {
        get
        {
            ThrowIfSealed();
            return volume;
        }
        set
        {
            ThrowIfSealed();
            SoundClip.ValidateVolume(value, nameof(value));
            volume = value;
        }
    }

    public bool Loop
    {
        get
        {
            ThrowIfSealed();
            return loop;
        }
        set
        {
            ThrowIfSealed();
            loop = value;
        }
    }

    internal float VolumeValue => volume;

    internal bool LoopValue => loop;

    internal float[] Values { get; }

    public void Set<T>(SoundParameter<T> parameter, T value)
        where T : struct
    {
        ThrowIfSealed();
        if (ReferenceEquals(parameter, SoundPlayback.VolumeParameter))
        {
            Volume = SoundParameter<T>.ToFloat(value);
            return;
        }

        int index = clip.GetParameterIndex(parameter, nameof(parameter));
        float number = SoundParameter<T>.ToFloat(value);
        clip.ValidateParameterValue(index, number, nameof(value));
        Values[index] = number;
    }

    internal void Seal() => sealed_ = true;

    private void ThrowIfSealed()
    {
        if (sealed_)
        {
            throw new InvalidOperationException("Sound start options can only be used inside the configuration delegate passed to Play.");
        }
    }
}
