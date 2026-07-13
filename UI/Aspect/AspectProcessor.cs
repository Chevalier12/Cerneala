using Cerneala.UI.Elements;
using Cerneala.UI.Theming;

namespace Cerneala.UI.Aspect;

public sealed class AspectProcessor
{
    private readonly UIRoot root;
    private readonly AspectEngine engine = new();
    private readonly AspectEnvironment environment = new("runtime");
    private int synchronizedCatalogVersion = -1;
    private Theme? synchronizedTheme;

    public AspectProcessor(UIRoot root)
    {
        this.root = root ?? throw new ArgumentNullException(nameof(root));
    }

    public AspectEngine Engine => engine;

    internal AspectEnvironment Environment
    {
        get
        {
            SynchronizeEnvironment(root.AspectRegistry.BuildCatalog());
            return environment;
        }
    }

    public void Process(UIElement element)
    {
        ArgumentNullException.ThrowIfNull(element);
        AspectCatalog catalog = root.AspectRegistry.BuildCatalog();
        SynchronizeEnvironment(catalog);
        AspectVariantSet variants = element is Cerneala.UI.Controls.Control control
            ? control.AspectVariants
            : AspectVariantSet.Empty;
        AspectDataContext dataContext = new(element.DataContext, owner: element);
        engine.Apply(element, catalog, environment, root.ThemeProvider, variants, dataContext);
    }

    public void Clear(UIElement element)
    {
        engine.Clear(element);
    }

    private void SynchronizeEnvironment(AspectCatalog catalog)
    {
        Theme? theme = root.ThemeProvider?.Theme;
        if (synchronizedCatalogVersion == catalog.Version && ReferenceEquals(synchronizedTheme, theme))
        {
            return;
        }

        AspectEnvironment next = new("runtime.next");
        foreach ((AspectToken token, AspectValue defaultValue) in catalog.TokenDefaults)
        {
            object? resolved = defaultValue.Resolve(new AspectResolutionContext(
                root,
                next,
                AspectStateSet.Empty,
                AspectVariantSet.Empty,
                root.ThemeProvider));
            next.Set(token, resolved);
        }

        if (theme is not null)
        {
            ThemeTokenBridge.Apply(theme, next);
        }

        environment.ReplaceWith(next);
        synchronizedCatalogVersion = catalog.Version;
        synchronizedTheme = theme;
    }
}
