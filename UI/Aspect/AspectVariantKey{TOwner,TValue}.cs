namespace Cerneala.UI.Aspect;

public sealed class AspectVariantKey<TOwner, TValue> : AspectVariantKey
{
    internal AspectVariantKey(string name)
        : base(name, typeof(TOwner), typeof(TValue))
    {
    }
}
