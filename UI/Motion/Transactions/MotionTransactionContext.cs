using Cerneala.UI.Core;
using Cerneala.UI.Elements;
using Cerneala.UI.Motion.Core;
using Cerneala.UI.Motion.Interpolation;
using Cerneala.UI.Motion.Properties;
using Cerneala.UI.Motion.Specs;

namespace Cerneala.UI.Motion.Transactions;

public sealed class MotionTransactionContext : UiPropertyMutationObserver, IDisposable
{
    private readonly MotionSystem motion;
    private readonly Stack<MotionTransaction> stack = new();
    private bool disposed;

    public MotionTransactionContext(MotionSystem motion)
    {
        this.motion = motion ?? throw new ArgumentNullException(nameof(motion));
    }

    public int Depth => stack.Count;

    public MotionTransactionScope Begin(MotionSpec defaultSpec)
    {
        return Begin(new MotionTransactionOptions(defaultSpec));
    }

    public MotionTransactionScope Begin(MotionTransactionOptions options)
    {
        motion.ThreadGuard.VerifyAccess();
        MotionTransaction transaction = new(options);
        stack.Push(transaction);
        return new MotionTransactionScope(this, transaction);
    }

    public MotionTransactionScope Disable()
    {
        return Begin(new MotionTransactionOptions(Specs.Motion.Tween(TimeSpan.FromMilliseconds(1)), isDisabled: true));
    }

    internal void Pop(MotionTransaction transaction)
    {
        motion.ThreadGuard.VerifyAccess();
        if (stack.Count == 0)
        {
            return;
        }

        if (!ReferenceEquals(stack.Peek(), transaction))
        {
            throw new InvalidOperationException("Motion transaction scopes must be disposed in stack order.");
        }

        stack.Pop();
    }

    public void Dispose()
    {
        if (disposed)
        {
            return;
        }

        disposed = true;
        stack.Clear();
    }

    internal override void OnPropertyMutated(UiPropertyMutation mutation)
    {
        if (disposed || stack.Count == 0)
        {
            return;
        }

        MotionTransaction transaction = stack.Peek();
        if (transaction.IsDisabled ||
            mutation.MutatingSource == UiPropertyValueSource.Animation ||
            mutation.Property.AreEqualUntyped(mutation.OldEffectiveValue, mutation.NewEffectiveValue) ||
            mutation.Target is not UIElement element ||
            !ReferenceEquals(element.Root, motion.Root) ||
            !element.IsAttached)
        {
            return;
        }

        if (!motion.AnimatableProperties.TryGet(mutation.Property, out MotionPropertyOptions? options))
        {
            return;
        }

        AnimateMutationUntyped(element, mutation, options, transaction.Options.DefaultSpec);
    }

    private void AnimateMutationUntyped(
        UIElement element,
        UiPropertyMutation mutation,
        MotionPropertyOptions options,
        MotionSpec spec)
    {
        IValueMixer mixer = motion.Mixers.Resolve(mutation.Property.ValueType, mutation.Property.DiagnosticName);
        MotionSpec typedSpec = spec is null ? options.DefaultSpec : spec;
        if (mixer is not IValueMixerDispatcher dispatcher)
        {
            throw new InvalidOperationException($"Mixer for {mutation.Property.ValueType.Name} cannot animate property mutations.");
        }

        dispatcher.AnimateMutation(this, element, mutation, typedSpec);
    }

    internal void AnimateMutation<T>(
        UIElement element,
        UiPropertyMutation mutation,
        ValueMixer<T> mixer,
        MotionSpec spec)
    {
        UiProperty<T> property = (UiProperty<T>)mutation.Property;
        MotionPropertyBinding<T> binding = motion.Properties.GetOrCreateBinding(motion, element, property);
        binding.Value.JumpTo(Cast<T>(mutation.OldEffectiveValue));
        binding.AnimateTo(Cast<T>(mutation.NewEffectiveValue), ToTypedSpec<T>(spec, mixer));
    }

    private static MotionSpec<T> ToTypedSpec<T>(MotionSpec spec, ValueMixer<T> mixer)
    {
        return spec is MotionSpec<T> typed
            ? typed
            : new UntypedMotionSpecAdapter<T>(spec, mixer);
    }

    private static T Cast<T>(object? value)
    {
        if (value is T typed)
        {
            return typed;
        }

        if (value is null && default(T) is null)
        {
            return default!;
        }

        throw new InvalidOperationException($"Motion transaction value must be assignable to {typeof(T).Name}.");
    }

    private sealed class UntypedMotionSpecAdapter<T>(MotionSpec inner, ValueMixer<T> mixer) : MotionSpec<T>
    {
        public override MotionSampler<T> CreateSampler(T from, T to, ValueMixer<T> _, MotionSpecContext context)
        {
            MotionSampler sampler = inner.CreateSamplerUntyped(from, to, mixer, context);
            if (sampler is MotionSampler<T> typed)
            {
                return typed;
            }

            throw new InvalidOperationException($"Motion spec did not create a typed sampler for {typeof(T).Name}.");
        }
    }
}
