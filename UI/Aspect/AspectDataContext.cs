namespace Cerneala.UI.Aspect;

public sealed class AspectDataContext
{
    public static AspectDataContext Empty { get; } = new(null, null);

    public AspectDataContext(object? data, Type? dataType = null, int? index = null, object? owner = null)
    {
        Data = data;
        DataType = dataType ?? data?.GetType();
        Index = index;
        Owner = owner;
    }

    public object? Data { get; }

    public Type? DataType { get; }

    public int? Index { get; }

    public object? Owner { get; }
}
