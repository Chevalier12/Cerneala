using Cerneala.UI.Elements;
using Cerneala.UI.Theming;

namespace Cerneala.UI.Aspect;

public sealed class AspectResolutionContext
{
    public AspectResolutionContext(
        UIElement element,
        AspectEnvironment environment,
        AspectStateSet? states = null,
        AspectVariantSet? variants = null,
        ThemeProvider? themeProvider = null)
    {
        Element = element ?? throw new ArgumentNullException(nameof(element));
        Environment = environment ?? throw new ArgumentNullException(nameof(environment));
        States = states ?? AspectStateSet.Empty;
        Variants = variants ?? AspectVariantSet.Empty;
        ThemeProvider = themeProvider;
    }

    public UIElement Element { get; }

    public AspectEnvironment Environment { get; }

    public AspectStateSet States { get; }

    public AspectVariantSet Variants { get; }

    public ThemeProvider? ThemeProvider { get; }
}
