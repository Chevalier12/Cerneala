using Cerneala.UI.Motion.Interpolation;

namespace Cerneala.UI.Motion.Specs;

public static class Motion
{
    public static TweenSpec<T> Tween<T>(TimeSpan duration, IEasing? easing = null)
    {
        return new TweenSpec<T>(duration, easing);
    }

    public static SpringSpec<T> Spring<T>(float stiffness = 520, float damping = 38, float mass = 1)
    {
        return new SpringSpec<T>(stiffness, damping, mass);
    }

    public static KeyframesSpec<T> Keyframes<T>(params MotionKeyframe<T>[] frames)
    {
        return new KeyframesSpec<T>(frames);
    }

    public static DecaySpec<T> Decay<T>(MotionVelocity<T> initialVelocity, float deceleration = 0.998f)
    {
        return new DecaySpec<T>(initialVelocity, deceleration);
    }

    public static MotionSpec Tween(TimeSpan duration, IEasing? easing = null)
    {
        return new UntypedTweenSpec(duration, easing);
    }

    public static MotionSpec Spring(float stiffness = 520, float damping = 38, float mass = 1)
    {
        return new UntypedSpringSpec(stiffness, damping, mass);
    }

    private sealed class UntypedTweenSpec : MotionSpec
    {
        private readonly TimeSpan duration;
        private readonly IEasing? easing;

        public UntypedTweenSpec(TimeSpan duration, IEasing? easing)
        {
            if (duration <= TimeSpan.Zero)
            {
                throw new ArgumentOutOfRangeException(nameof(duration), "Tween duration must be positive.");
            }

            this.duration = duration;
            this.easing = easing;
        }

        public override MotionSampler CreateSamplerUntyped(object? from, object? to, IValueMixer mixer, MotionSpecContext context)
        {
            return Dispatcher(mixer).CreateTweenSampler(duration, easing, from, to, context);
        }
    }

    private sealed class UntypedSpringSpec : MotionSpec
    {
        private readonly float stiffness;
        private readonly float damping;
        private readonly float mass;

        public UntypedSpringSpec(float stiffness, float damping, float mass)
        {
            if (!float.IsFinite(stiffness) || stiffness <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(stiffness), "Stiffness must be positive.");
            }

            if (!float.IsFinite(damping) || damping < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(damping), "Damping cannot be negative.");
            }

            if (!float.IsFinite(mass) || mass <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(mass), "Mass must be positive.");
            }

            this.stiffness = stiffness;
            this.damping = damping;
            this.mass = mass;
        }

        public override MotionSampler CreateSamplerUntyped(object? from, object? to, IValueMixer mixer, MotionSpecContext context)
        {
            return Dispatcher(mixer).CreateSpringSampler(stiffness, damping, mass, from, to, context);
        }
    }

    private static IValueMixerDispatcher Dispatcher(IValueMixer mixer)
    {
        return mixer as IValueMixerDispatcher
            ?? throw new InvalidOperationException($"Mixer for {mixer.ValueType.Name} cannot create motion samplers.");
    }
}
