namespace Cerneala.UI.Motion.Interpolation;

public sealed class ValueMixerRegistry
{
    private readonly Dictionary<Type, IValueMixer> mixers = [];

    public void Register<T>(ValueMixer<T> mixer)
    {
        ArgumentNullException.ThrowIfNull(mixer);
        mixers[typeof(T)] = mixer;
    }

    public bool TryResolve<T>(out ValueMixer<T> mixer)
    {
        if (mixers.TryGetValue(typeof(T), out IValueMixer? untyped) && untyped is ValueMixer<T> typed)
        {
            mixer = typed;
            return true;
        }

        mixer = null!;
        return false;
    }

    public ValueMixer<T> Resolve<T>()
    {
        if (TryResolve(out ValueMixer<T> mixer))
        {
            return mixer;
        }

        throw new InvalidOperationException($"No value mixer registered for {typeof(T).Name}.");
    }

    public bool TryResolve(Type valueType, out IValueMixer mixer)
    {
        ArgumentNullException.ThrowIfNull(valueType);
        return mixers.TryGetValue(valueType, out mixer!);
    }
}
