using Cerneala.UI.Elements;

namespace Cerneala.UI.Aspect;

public sealed class AspectInvalidation
{
    private readonly AspectEngine engine;
    private readonly AspectCatalog catalog;
    private readonly AspectEnvironment environment;

    public AspectInvalidation(AspectEngine engine, AspectCatalog catalog, AspectEnvironment environment)
    {
        this.engine = engine ?? throw new ArgumentNullException(nameof(engine));
        this.catalog = catalog ?? throw new ArgumentNullException(nameof(catalog));
        this.environment = environment ?? throw new ArgumentNullException(nameof(environment));
    }

    public AspectApplicationResult Track(UIElement element)
    {
        return engine.Apply(element, catalog, environment);
    }
}
