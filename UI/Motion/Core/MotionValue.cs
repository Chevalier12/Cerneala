namespace Cerneala.UI.Motion.Core;

public abstract class MotionValue
{
    internal abstract Type ValueType { get; }

    public static DerivedMotionValue<TOut> Combine<T1, T2, TOut>(
        MotionValue<T1> first,
        MotionValue<T2> second,
        Func<T1, T2, TOut> selector)
    {
        return new DerivedMotionValue<TOut>(
            () => selector(first.Current, second.Current),
            subscribeDependencies =>
            [
                first.Subscribe(_ => subscribeDependencies()),
                second.Subscribe(_ => subscribeDependencies())
            ]);
    }
}
