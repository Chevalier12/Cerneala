namespace Cerneala.UI.Aspect;

public static class AspectRef
{
    public static AspectValue<T> To<T>(AspectToken<T> token)
    {
        return AspectValue<T>.Token(token);
    }
}
