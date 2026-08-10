using Cerneala.UI.Controls;
using Cerneala.UI.Controls.Templates;
using Cerneala.UI.Elements;
using Cerneala.UI.Theming;

namespace Cerneala.UI.Aspect;

public sealed class AspectProcessor
{
    private readonly UIRoot root;
    private readonly AspectEngine engine;
    private readonly AspectEnvironment environment;
    private ContentTemplateRegistry contentTemplates = new();
    private int synchronizedCatalogVersion = -1;
    private int synchronizedContentTemplateCatalogVersion = -1;
    private Theme? synchronizedTheme;

    public AspectProcessor(UIRoot root)
    {
        this.root = root ?? throw new ArgumentNullException(nameof(root));
        engine = new AspectEngine(root.Relay);
        environment = new AspectEnvironment(root.Relay, "runtime");
    }

    public AspectEngine Engine => engine;

    internal ContentTemplateRegistry ContentTemplates
    {
        get
        {
            root.Relay.VerifyAccess();
            AspectCatalog catalog = root.AspectRegistry.BuildCatalog();
            SynchronizeContentTemplates(catalog);
            return contentTemplates;
        }
    }

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
        SynchronizeContentTemplates(catalog);
        if (element is ContentPresenter presenter)
        {
            presenter.RefreshAspectContentTemplates(catalog.Version);
        }

        if (TemplateAspectContext.TryGet(element, out TemplateAspectContext.Registration registration))
        {
            engine.ApplyTemplateSlot(
                element,
                registration.Owner,
                catalog,
                environment,
                root.ThemeProvider,
                registration.Owner.AspectVariants,
                new AspectDataContext(element.DataContext, owner: registration.Owner),
                registration.SlotPath);
        }
        else
        {
            AspectVariantSet variants = element is Control control
                ? control.AspectVariants
                : AspectVariantSet.Empty;
            engine.Apply(
                element,
                catalog,
                environment,
                root.ThemeProvider,
                variants,
                new AspectDataContext(element.DataContext, owner: element));
        }

        if (element is Control templatedControl)
        {
            templatedControl.ApplyTemplate();
        }
    }

    public void Clear(UIElement element)
    {
        root.Relay.VerifyAccess();
        engine.Clear(element);
    }

    internal ComponentTemplate? ResolveComponentTemplate(Control owner, string? key)
    {
        root.Relay.VerifyAccess();
        ArgumentNullException.ThrowIfNull(owner);
        if (string.IsNullOrWhiteSpace(key))
        {
            return null;
        }

        ComponentTemplateDefinition? best = null;
        int bestDistance = int.MaxValue;
        int bestIndex = -1;
        IReadOnlyList<ComponentTemplateDefinition> definitions = root.AspectRegistry.BuildCatalog().ComponentTemplates;
        for (int index = 0; index < definitions.Count; index++)
        {
            ComponentTemplateDefinition candidate = definitions[index];
            if (!string.Equals(candidate.Name, key, StringComparison.Ordinal) ||
                !candidate.OwnerType.IsAssignableFrom(owner.GetType()) ||
                candidate.Template is null)
            {
                continue;
            }

            int distance = InheritanceDistance(owner.GetType(), candidate.OwnerType);
            if (distance < bestDistance || distance == bestDistance && index > bestIndex)
            {
                best = candidate;
                bestDistance = distance;
                bestIndex = index;
            }
        }

        if (best is null)
        {
            return null;
        }

        if (best.Template is not ComponentTemplate template)
        {
            throw new InvalidOperationException(
                $"Aspect component template '{best.Name}' must contain a ComponentTemplate value.");
        }

        if (!template.OwnerType.IsInstanceOfType(owner))
        {
            throw new InvalidOperationException(
                $"Aspect component template '{best.Name}' cannot be applied to '{owner.GetType().FullName}'.");
        }

        return template;
    }

    private void SynchronizeContentTemplates(AspectCatalog catalog)
    {
        if (synchronizedContentTemplateCatalogVersion == catalog.Version)
        {
            return;
        }

        ContentTemplateRegistry next = new();
        foreach (ContentTemplateDefinition definition in catalog.ContentTemplates)
        {
            if (definition.Template is null)
            {
                continue;
            }

            if (definition.Template is not ContentTemplate template)
            {
                throw new InvalidOperationException(
                    $"Aspect content template '{definition.Name}' must contain a ContentTemplate value.");
            }

            next.Register(template);
        }

        contentTemplates = next;
        synchronizedContentTemplateCatalogVersion = catalog.Version;
    }

    private static int InheritanceDistance(Type actualType, Type targetType)
    {
        if (actualType == targetType)
        {
            return 0;
        }

        int distance = 1;
        for (Type? current = actualType.BaseType; current is not null; current = current.BaseType)
        {
            if (current == targetType)
            {
                return distance;
            }

            distance++;
        }

        return targetType.IsInterface ? 10_000 : int.MaxValue;
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
