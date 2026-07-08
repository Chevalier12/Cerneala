namespace Cerneala.UI.Controls.Templates;

public sealed class ContentTemplateMatchContext
{
    public ContentTemplateMatchContext(object? data, string? requestedKey = null, ContentPresenter? presenter = null, object? owner = null, int index = -1)
    {
        Data = data;
        RequestedKey = requestedKey;
        Presenter = presenter;
        Owner = owner;
        Index = index;
    }

    public object? Data { get; }

    public Type? DataType => Data?.GetType();

    public string? RequestedKey { get; }

    public ContentPresenter? Presenter { get; }

    public object? Owner { get; }

    public int Index { get; }
}
