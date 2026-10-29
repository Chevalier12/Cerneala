using System.Runtime.CompilerServices;
using Cerneala.Drawing;
using Cerneala.Drawing.Text;
using Cerneala.UI.Resources;

namespace Cerneala.UI.Text;

public sealed class FontResolver
{
    private static readonly ConditionalWeakTable<IResourceProvider, StrongBox<long>> ProviderNumbers = new();
    private static long nextProviderNumber;
    private readonly IFontSource? fontSource;
    private readonly IResourceProvider? resourceProvider;

    public FontResolver()
        : this(new SystemFontSource())
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

    public ResolvedTextFont Resolve(string familyName, float size, FontWeight weight, FontStyle style, FontStretch stretch)
    {
        if (weight == FontWeight.Normal && style == FontStyle.Normal && stretch == FontStretch.Normal)
        {
            return Resolve(familyName, size);
        }

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
            : fontSource.LoadFont(familyName, size, weight, style, stretch);
        ArgumentNullException.ThrowIfNull(font);
        return new ResolvedTextFont(
            font,
            $"{font.FamilyName}:{font.Size:R}:{(int)weight}:{style}:{stretch}");
    }

    public ResolvedTextFont Resolve(TextAspect aspect)
    {
        return Resolve(aspect, aspect.Scale);
    }

    // A font resource is an exact face, so it ignores the weight, style and stretch of the aspect.
    internal ResolvedTextFont Resolve(TextAspect aspect, float scale)
    {
        if (aspect.FontResourceId is ResourceId<FontResource> fontResourceId)
        {
            return Resolve(fontResourceId);
        }

        return Resolve(
            aspect.FontFamily,
            aspect.FontSize * scale,
            aspect.FontWeight,
            aspect.FontStyle,
            aspect.FontStretch);
    }

    public ResolvedTextFont Resolve(ResourceId<FontResource> id)
    {
        if (resourceProvider is null)
        {
            throw new InvalidOperationException("A resource provider is required to resolve font resources.");
        }

        FontResource resource = resourceProvider.GetResource(id);
        return new ResolvedTextFont(
            resource.Resolve(),
            $"resource:{id}:{GetProviderNumber(resourceProvider)}:{GetResourceVersion(id)}");
    }

    // Two providers can hold the same id at the same version, so the identity also names the provider. Otherwise text
    // measured with the font of one provider reuses the cached layout of the other, for example after
    // UIRoot.SetResourceProvider replaces the root provider.
    private static long GetProviderNumber(IResourceProvider provider) =>
        ProviderNumbers.GetValue(provider, static _ => new StrongBox<long>(Interlocked.Increment(ref nextProviderNumber))).Value;

    private long GetResourceVersion(ResourceId<FontResource> id)
    {
        return resourceProvider switch
        {
            ResourceStore store => store.GetVersion(id),
            ResourceDictionary dictionary => dictionary.Version,
            _ => 0
        };
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
