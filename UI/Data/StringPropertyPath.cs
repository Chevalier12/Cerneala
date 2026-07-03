namespace Cerneala.UI.Data;

public sealed class StringPropertyPath
{
    private StringPropertyPath(string path)
    {
        Path = path;
    }

    public static bool IsSupported => false;

    public string Path { get; }

    public static StringPropertyPath Parse(string path)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        throw new NotSupportedException("String property paths are deferred and unsupported in core binding.");
    }

    public object? Evaluate(object source)
    {
        ArgumentNullException.ThrowIfNull(source);
        throw new NotSupportedException("String property paths are deferred and unsupported in core binding.");
    }
}
