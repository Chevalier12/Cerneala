namespace Cerneala.UI.Aspect;

public sealed class AspectSlot<TOwner, TTarget> : AspectSlot
{
    internal AspectSlot(string name)
        : base(name, typeof(TOwner), typeof(TTarget))
    {
    }
}
