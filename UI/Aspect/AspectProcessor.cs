using Cerneala.UI.Elements;
using Cerneala.UI.Theming;

namespace Cerneala.UI.Aspect;

public sealed class AspectProcessor
{
    private readonly UIRoot root;
    private readonly AspectEngine engine;
    private readonly AspectEnvironment environment;
    private int synchronizedCatalogVersion = -1;
    private Theme? synchronizedTheme;

    public AspectProcessor(UIRoot root)
    {
        this.root = root ?? throw new ArgumentNullException(nameof(root));
        engine = new AspectEngine(root.Relay);
        environment = new AspectEnvironment(root.Relay, "runtime");
    }

    public AspectEngine Engine => engine;

    internal AspectEnvironment Environment
    {
        get
        {
            root.Relay.VerifyAccess();
            SynchronizeEnvironment(root.AspectRegistry.BuildCatalog());
            return environment;
        }
    }

    public void Process(UIElement element)
    {
        root.Relay.VerifyAccess();
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
        root.Relay.VerifyAccess();
        engine.Clear(element);
    }

    private void SynchronizeEnvironment(AspectCatalog catalog)
    {
        Theme? theme = root.ThemeProvider?.Theme;
        if (synchronizedCatalogVersion == catalog.Version && ReferenceEquals(synchronizedTheme, theme))
        {
            return;
        }

        AspectEnvironment next = new(root.Relay, "runtime.next");
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
