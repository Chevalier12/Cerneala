using Cerneala.UI.Motion.Interpolation;

namespace Cerneala.UI.Motion.Specs;

public abstract class MotionSpec
{
    public abstract MotionSampler CreateSamplerUntyped(
        object? from,
        object? to,
        IValueMixer mixer,
        MotionSpecContext context);
}
