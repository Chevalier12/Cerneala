using Cerneala.UI.Elements;

namespace Cerneala.UI.Aspect;

public sealed class AspectProcessor
{
    private readonly UIRoot root;
    private readonly AspectEngine engine = new();
    private readonly AspectEnvironment environment = DefaultAspectPackage.CreateEnvironment();
    private int synchronizedCatalogVersion = -1;

    public AspectProcessor(UIRoot root)
    {
        this.root = root ?? throw new ArgumentNullException(nameof(root));
    }

    public AspectEngine Engine => engine;

    public void Process(UIElement element)
    {
        ArgumentNullException.ThrowIfNull(element);
        AspectCatalog catalog = root.AspectRegistry.BuildCatalog();
        SynchronizeTokenDefaults(catalog);
        AspectVariantSet variants = element is Cerneala.UI.Controls.Control control
            ? control.AspectVariants
            : AspectVariantSet.Empty;
        engine.Apply(element, catalog, environment, root.ThemeProvider, variants);
    }

    public void Clear(UIElement element)
    {
        engine.Clear(element);
    }

    private void SynchronizeTokenDefaults(AspectCatalog catalog)
    {
        if (synchronizedCatalogVersion == catalog.Version)
        {
            return;
        }

        foreach ((AspectToken token, AspectValue defaultValue) in catalog.TokenDefaults)
        {
            object? resolved = defaultValue.Resolve(new AspectResolutionContext(
                root,
                environment,
                AspectStateSet.Empty,
                AspectVariantSet.Empty,
                root.ThemeProvider));
            environment.Set(token, resolved);
        }

        synchronizedCatalogVersion = catalog.Version;
    }
}
