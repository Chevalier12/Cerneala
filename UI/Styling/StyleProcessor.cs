using Cerneala.UI.Elements;

namespace Cerneala.UI.Styling;

public sealed class StyleProcessor
{
    private static readonly StyleSheet EmptyStyleSheet = new();

    private readonly StyleApplicator applicator;
    private readonly Func<StyleSheet?> styleSheetProvider;
    private readonly Func<ThemeProvider?> themeProviderProvider;

    public StyleProcessor(
        StyleApplicator applicator,
        Func<StyleSheet?> styleSheetProvider,
        Func<ThemeProvider?> themeProviderProvider)
    {
        this.applicator = applicator ?? throw new ArgumentNullException(nameof(applicator));
        this.styleSheetProvider = styleSheetProvider ?? throw new ArgumentNullException(nameof(styleSheetProvider));
        this.themeProviderProvider = themeProviderProvider ?? throw new ArgumentNullException(nameof(themeProviderProvider));
    }

    public void Process(UIElement element)
    {
        ArgumentNullException.ThrowIfNull(element);
        StyleSheet styleSheet = styleSheetProvider() ?? EmptyStyleSheet;

        applicator.Apply(element, styleSheet, themeProviderProvider());
    }
}
