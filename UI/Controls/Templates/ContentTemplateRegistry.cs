namespace Cerneala.UI.Controls.Templates;

public sealed class ContentTemplateRegistry
{
    private readonly List<RegisteredTemplate> templates = [];
    private readonly Dictionary<CacheKey, ContentTemplate> cache = [];
    private int nextOrder;

    public int Version { get; private set; }

    public int CacheHits { get; private set; }

    public int CacheMisses { get; private set; }

    public void Register(ContentTemplate template)
    {
        templates.Add(new RegisteredTemplate(template ?? throw new ArgumentNullException(nameof(template)), nextOrder++));
        cache.Clear();
        Version++;
    }

    public bool Unregister(ContentTemplate template)
    {
        int index = templates.FindIndex(item => ReferenceEquals(item.Template, template));
        if (index < 0)
        {
            return false;
        }

        templates.RemoveAt(index);
        cache.Clear();
        Version++;
        return true;
    }

    public bool TryResolve(ContentTemplateMatchContext context, out ContentTemplate template)
    {
        ArgumentNullException.ThrowIfNull(context);
        bool canUseCache = templates.All(item => !item.Template.HasPredicate);
        CacheKey key = new(context.RequestedKey, context.DataType);
        if (canUseCache && cache.TryGetValue(key, out ContentTemplate? cached))
        {
            CacheHits++;
            template = cached;
            return true;
        }

        CacheMisses++;
        RegisteredTemplate? best = templates
            .Where(item => item.Template.CanApply(context))
            .OrderByDescending(item => item.Template.Key is not null && context.RequestedKey is not null)
            .ThenByDescending(item => item.Template.HasPredicate)
            .ThenByDescending(item => item.Template.Priority)
            .ThenByDescending(item => TypeScore(item.Template.DataType, context.DataType))
            .ThenBy(item => item.Order)
            .FirstOrDefault();

        if (best is null)
        {
            template = null!;
            return false;
        }

        template = best.Template;
        if (canUseCache)
        {
            cache[key] = template;
        }

        return true;
    }

    private static int TypeScore(Type? templateType, Type? dataType)
    {
        if (templateType is null)
        {
            return dataType is null ? int.MaxValue : -1;
        }

        if (dataType is null || !templateType.IsAssignableFrom(dataType))
        {
            return -1;
        }

        if (templateType == dataType)
        {
            return int.MaxValue - 1;
        }

        int distance = 0;
        for (Type? current = dataType; current is not null && current != templateType; current = current.BaseType)
        {
            distance++;
        }

        return 10_000 - distance;
    }

    private sealed record RegisteredTemplate(ContentTemplate Template, int Order);

    private sealed record CacheKey(string? RequestedKey, Type? DataType);
}
