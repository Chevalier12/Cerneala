namespace Cerneala.UI.Aspect;

public sealed class AspectToken<T> : AspectToken
{
    internal AspectToken(string name)
        : base(name, typeof(T))
    {
    }

    public AspectValue<T> Ref()
    {
        return AspectValue<T>.Token(this);
    }
}
