using System.Reflection;
using System.Runtime.ExceptionServices;
using Cerneala.UI.Motion.Interpolation;

namespace Cerneala.UI.Motion.Specs;

public static class Motion
{
    private static readonly MethodInfo CreateTweenSamplerMethod =
        typeof(Motion).GetMethod(nameof(CreateTweenSampler), BindingFlags.NonPublic | BindingFlags.Static)!;

    private static readonly MethodInfo CreateSpringSamplerMethod =
        typeof(Motion).GetMethod(nameof(CreateSpringSampler), BindingFlags.NonPublic | BindingFlags.Static)!;

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

    private static MotionSampler CreateTweenSampler<T>(
        TimeSpan duration,
        IEasing? easing,
        object? from,
        object? to,
        IValueMixer mixer,
        MotionSpecContext context)
    {
        if (mixer is not ValueMixer<T> typedMixer)
        {
            throw new ArgumentException($"Expected mixer for {typeof(T).Name}.", nameof(mixer));
        }

        return new TweenSpec<T>(duration, easing).CreateSampler(Cast<T>(from, nameof(from)), Cast<T>(to, nameof(to)), typedMixer, context);
    }

    private static MotionSampler CreateSpringSampler<T>(
        float stiffness,
        float damping,
        float mass,
        object? from,
        object? to,
        IValueMixer mixer,
        MotionSpecContext context)
    {
        if (mixer is not ValueMixer<T> typedMixer)
        {
            throw new ArgumentException($"Expected mixer for {typeof(T).Name}.", nameof(mixer));
        }

        return new SpringSpec<T>(stiffness, damping, mass).CreateSampler(Cast<T>(from, nameof(from)), Cast<T>(to, nameof(to)), typedMixer, context);
    }

    private static T Cast<T>(object? value, string parameterName)
    {
        if (value is T typed)
        {
            return typed;
        }

        if (value is null && default(T) is null)
        {
            return default!;
        }

        throw new ArgumentException($"Expected value of type {typeof(T).Name}.", parameterName);
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
            return InvokeSampler(
                CreateTweenSamplerMethod.MakeGenericMethod(mixer.ValueType),
                [duration, easing, from, to, mixer, context]);
        }
    }

    private sealed class UntypedSpringSpec : MotionSpec
    {
        private readonly float stiffness;
        private readonly float damping;
        private readonly float mass;

        public UntypedSpringSpec(float stiffness, float damping, float mass)
        {
            if (stiffness <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(stiffness), "Stiffness must be positive.");
            }

            if (damping < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(damping), "Damping cannot be negative.");
            }

            if (mass <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(mass), "Mass must be positive.");
            }

            this.stiffness = stiffness;
            this.damping = damping;
            this.mass = mass;
        }

        public override MotionSampler CreateSamplerUntyped(object? from, object? to, IValueMixer mixer, MotionSpecContext context)
        {
            return InvokeSampler(
                CreateSpringSamplerMethod.MakeGenericMethod(mixer.ValueType),
                [stiffness, damping, mass, from, to, mixer, context]);
        }
    }

    private static MotionSampler InvokeSampler(MethodInfo method, object?[] arguments)
    {
        try
        {
            return (MotionSampler)method.Invoke(null, arguments)!;
        }
        catch (TargetInvocationException ex) when (ex.InnerException is not null)
        {
            ExceptionDispatchInfo.Capture(ex.InnerException).Throw();
            throw;
        }
    }
}
