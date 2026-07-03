namespace Cerneala.UI.Markup;

public sealed record MarkupLoadOptions(bool ContinueOnError = false)
{
    public static MarkupLoadOptions Strict { get; } = new();

    public static MarkupLoadOptions Recover { get; } = new(true);
}
