using Cerneala.Drawing;
using Cerneala.UI.Resources;

namespace Cerneala.UI.Text;

public sealed class FontResolver
{
    private readonly IFontSource? fontSource;
    private readonly IResourceProvider? resourceProvider;

    public FontResolver()
    {
    }

    public FontResolver(IFontSource fontSource)
    {
        this.fontSource = fontSource ?? throw new ArgumentNullException(nameof(fontSource));
    }

    public FontResolver(IResourceProvider resourceProvider)
    {
        this.resourceProvider = resourceProvider ?? throw new ArgumentNullException(nameof(resourceProvider));
    }

    public FontResolver(IFontSource? fontSource, IResourceProvider? resourceProvider)
    {
        this.fontSource = fontSource;
        this.resourceProvider = resourceProvider;
    }

    public static FontResolver Default { get; } = new();

    public ResolvedTextFont Resolve(string familyName, float size)
    {
        if (string.IsNullOrWhiteSpace(familyName))
        {
            throw new ArgumentException("Font family cannot be empty.", nameof(familyName));
        }

        if (size <= 0 || !float.IsFinite(size))
        {
            throw new ArgumentOutOfRangeException(nameof(size), "Font size must be positive and finite.");
        }

        IDrawFont font = fontSource is null
            ? new FallbackDrawFont(familyName, size)
            : fontSource.LoadFont(familyName, size);
        return new ResolvedTextFont(font);
    }

    public ResolvedTextFont Resolve(TextRunStyle style)
    {
        if (style.FontResourceId is ResourceId<FontResource> fontResourceId)
        {
            return Resolve(fontResourceId);
        }

        return Resolve(style.FontFamily, style.FontSize * style.Scale);
    }

    public ResolvedTextFont Resolve(ResourceId<FontResource> id)
    {
        if (resourceProvider is null)
        {
            throw new InvalidOperationException("A resource provider is required to resolve font resources.");
        }

        FontResource resource = resourceProvider.GetResource(id);
        return new ResolvedTextFont(resource.Resolve(), $"resource:{id}:{GetResourceVersion(id)}");
    }

    private long GetResourceVersion(ResourceId<FontResource> id)
    {
        return resourceProvider is ResourceStore store ? store.GetVersion(id) : 0;
    }

    private sealed class FallbackDrawFont : IDrawFont
    {
        public FallbackDrawFont(string familyName, float size)
        {
            FamilyName = familyName;
            Size = size;
        }

        public string FamilyName { get; }

        public float Size { get; }
    }
}
