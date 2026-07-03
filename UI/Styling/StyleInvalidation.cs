using Cerneala.UI.Core;
using Cerneala.UI.Elements;

namespace Cerneala.UI.Styling;

public sealed class StyleInvalidation : IDisposable
{
    private readonly StyleApplicator applicator;
    private readonly StyleSheet styleSheet;
    private readonly ThemeProvider? themeProvider;
    private readonly PseudoClassRegistry pseudoClassRegistry;
    private readonly HashSet<UIElement> trackedElements = new(ReferenceEqualityComparer.Instance);
    private bool disposed;

    public StyleInvalidation(
        StyleApplicator applicator,
        StyleSheet styleSheet,
        ThemeProvider? themeProvider = null,
        PseudoClassRegistry? pseudoClassRegistry = null)
    {
        this.applicator = applicator ?? throw new ArgumentNullException(nameof(applicator));
        this.styleSheet = styleSheet ?? throw new ArgumentNullException(nameof(styleSheet));
        this.themeProvider = themeProvider;
        this.pseudoClassRegistry = pseudoClassRegistry ?? PseudoClassRegistry.Default;
        if (themeProvider is not null)
        {
            themeProvider.ThemeChanged += OnThemeChanged;
        }
    }

    public void Track(UIElement element)
    {
        ThrowIfDisposed();
        ArgumentNullException.ThrowIfNull(element);
        if (!trackedElements.Add(element))
        {
            return;
        }

        element.PropertyChanged += OnElementPropertyChanged;
        applicator.Apply(element, styleSheet, themeProvider);
    }

    public void Recompute(UIElement element)
    {
        ThrowIfDisposed();
        ArgumentNullException.ThrowIfNull(element);
        applicator.Apply(element, styleSheet, themeProvider);
    }

    public void Dispose()
    {
        if (disposed)
        {
            return;
        }

        disposed = true;
        foreach (UIElement element in trackedElements)
        {
            element.PropertyChanged -= OnElementPropertyChanged;
        }

        trackedElements.Clear();
        if (themeProvider is not null)
        {
            themeProvider.ThemeChanged -= OnThemeChanged;
        }
    }

    private bool AffectsPseudoClass(UiProperty property)
    {
        return pseudoClassRegistry.AffectsPseudoClass(property) ||
            property.Options.HasFlag(UiPropertyOptions.AffectsStyle);
    }

    private void OnElementPropertyChanged(object? sender, UiPropertyChangedEventArgs args)
    {
        if (sender is UIElement element && AffectsPseudoClass(args.Property))
        {
            applicator.Apply(element, styleSheet, themeProvider);
        }
    }

    private void OnThemeChanged(object? sender, ThemeChangedEventArgs args)
    {
        foreach (UIElement element in trackedElements)
        {
            applicator.Apply(element, styleSheet, themeProvider);
        }
    }

    private void ThrowIfDisposed()
    {
        ObjectDisposedException.ThrowIf(disposed, this);
    }
}
