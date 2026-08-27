namespace Cerneala.UI.Aspect;

public enum AspectAuthoringKind
{
    Code,
    MarkupDefault,
    MarkupNamed,
    MarkupInline
}

public sealed record AspectOrigin
{
    public AspectOrigin(
        AspectAuthoringKind kind,
        string? document = null,
        string? name = null)
    {
        Kind = kind;
        Document = Normalize(document);
        Name = Normalize(name);
    }

    public AspectAuthoringKind Kind { get; }

    public string? Document { get; }

    public string? Name { get; }

    public static AspectOrigin Code(string? name = null)
    {
        return new AspectOrigin(AspectAuthoringKind.Code, name: name);
    }

    private static string? Normalize(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }
}
