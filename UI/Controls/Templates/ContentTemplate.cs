using Cerneala.UI.Aspect;
using Cerneala.UI.Elements;

namespace Cerneala.UI.Controls.Templates;

public class ContentTemplate
{
    private readonly Func<ContentTemplateContext, UIElement?> factory;
    private readonly Func<ContentTemplateMatchContext, bool>? predicate;

    public ContentTemplate(
        string name,
        Type? dataType,
        string? key,
        int priority,
        Func<ContentTemplateContext, UIElement?> factory,
        Func<ContentTemplateMatchContext, bool>? predicate = null)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Content template name cannot be empty.", nameof(name));
        }

        Name = name;
        DataType = dataType;
        Key = key;
        Priority = priority;
        this.factory = factory ?? throw new ArgumentNullException(nameof(factory));
        this.predicate = predicate;
    }

    public string Name { get; }

    public Type? DataType { get; }

    public string? Key { get; }

    public int Priority { get; }

    public bool HasPredicate => predicate is not null;

    public virtual bool CanApply(ContentTemplateMatchContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        if (Key is not null && !string.Equals(Key, context.RequestedKey, StringComparison.Ordinal))
        {
            return false;
        }

        if (Key is null && context.RequestedKey is not null)
        {
            return false;
        }

        bool typeMatches = DataType is null
            ? context.Data is null
            : context.Data is not null && DataType.IsInstanceOfType(context.Data);
        return typeMatches && (predicate?.Invoke(context) ?? true);
    }

    public virtual UIElement? Create(ContentTemplateContext context)
    {
        return factory(context);
    }
}

public sealed class ContentTemplate<TData> : ContentTemplate
{
    private readonly Func<ContentTemplateContext<TData>, UIElement?> typedFactory;

    public ContentTemplate(
        string name,
        string? key,
        int priority,
        Func<ContentTemplateContext<TData>, UIElement?> factory,
        Func<ContentTemplateMatchContext, bool>? predicate = null)
        : base(name, typeof(TData), key, priority, context => factory(new ContentTemplateContext<TData>(context)), predicate)
    {
        typedFactory = factory;
    }
}
