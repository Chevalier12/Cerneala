using Cerneala.Drawing;
using Cerneala.UI.Layout;
using Cerneala.UI.Media;

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
        return Resolve<T>(propertyName: null);
    }

    public ValueMixer<T> Resolve<T>(string? propertyName)
    {
        if (TryResolve(out ValueMixer<T> mixer))
        {
            return mixer;
        }

        throw CreateMissingMixerException(typeof(T), propertyName);
    }

    public bool TryResolve(Type valueType, out IValueMixer mixer)
    {
        ArgumentNullException.ThrowIfNull(valueType);
        return mixers.TryGetValue(valueType, out mixer!);
    }

    public IValueMixer Resolve(Type valueType, string? propertyName = null)
    {
        ArgumentNullException.ThrowIfNull(valueType);
        if (TryResolve(valueType, out IValueMixer mixer))
        {
            return mixer;
        }

        throw CreateMissingMixerException(valueType, propertyName);
    }

    public void RegisterBuiltIns()
    {
        Register(new FloatMixer());
        Register(new DoubleMixer());
        Register(new ColorMixer());
        Register(new BrushMixer());
        Register(new ThicknessMixer());
        Register(new DrawPointMixer());
        Register(new DrawSizeMixer());
        Register(new DrawRectMixer());
        Register(new TransformMixer());
    }

    private static InvalidOperationException CreateMissingMixerException(Type valueType, string? propertyName)
    {
        string propertyText = string.IsNullOrWhiteSpace(propertyName)
            ? string.Empty
            : $" for property '{propertyName}'";
        return new InvalidOperationException(
            $"No ValueMixer registered for {valueType.Name}{propertyText}. Register a ValueMixer<{valueType.Name}> in the root MotionSystem mixer registry or provide a local custom mixer.");
    }
}
