using Cerneala.UI.Core;
using Cerneala.UI.Elements;
using Cerneala.UI.Motion.Specs;
using Cerneala.UI.Motion.Transactions;

namespace Cerneala.UI.Motion.Interpolation;

internal interface IValueMixerDispatcher
{
    MotionSampler CreateTweenSampler(
        TimeSpan duration,
        IEasing? easing,
        object? from,
        object? to,
        MotionSpecContext context);

    MotionSampler CreateSpringSampler(
        float stiffness,
        float damping,
        float mass,
        object? from,
        object? to,
        MotionSpecContext context);

    void AnimateMutation(
        MotionTransactionContext context,
        UIElement element,
        UiPropertyMutation mutation,
        MotionSpec spec);
}
