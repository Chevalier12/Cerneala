using Cerneala.Drawing;
using Cerneala.UI.Controls;
using Cerneala.UI.Core;
using Cerneala.UI.Styling;

namespace Cerneala.Tests.UI.Styling;

public sealed class StyleApplicatorTests
{
    [Fact]
    public void AppliesBaseStyleThroughStyleBaseSource()
    {
        Button button = new();
        StyleSheet sheet = new StyleSheet().Add(new StyleRule(StyleSelector.ForType<Button>())
            .Add(new Setter<DrawColor>(Control.BackgroundProperty, DrawColor.White)));

        new StyleApplicator().Apply(button, sheet);

        Assert.Equal(DrawColor.White, button.Background);
        Assert.Equal(UiPropertyValueSource.StyleBase, button.GetValueSource(Control.BackgroundProperty));
    }

    [Fact]
    public void AppliesVisualStateThroughStyleVisualStateSource()
    {
        Button button = new() { IsPointerOver = true };
        StyleSheet sheet = new StyleSheet().Add(new StyleRule(
                StyleSelector.ForType<Button>(),
                new VisualStateRule(PseudoClass.Hover))
            .Add(new Setter<DrawColor>(Control.BackgroundProperty, DrawColor.Black)));

        new StyleApplicator().Apply(button, sheet);

        Assert.Equal(DrawColor.Black, button.Background);
        Assert.Equal(UiPropertyValueSource.StyleVisualState, button.GetValueSource(Control.BackgroundProperty));
    }

    [Fact]
    public void LocalValueOverridesStyleValue()
    {
        Button button = new() { Background = DrawColor.Black };
        StyleSheet sheet = new StyleSheet().Add(new StyleRule(StyleSelector.ForType<Button>())
            .Add(new Setter<DrawColor>(Control.BackgroundProperty, DrawColor.White)));

        new StyleApplicator().Apply(button, sheet);

        Assert.Equal(DrawColor.Black, button.Background);
        Assert.Equal(UiPropertyValueSource.Local, button.GetValueSource(Control.BackgroundProperty));
    }

    [Fact]
    public void LaterMatchingRuleWinsForSameSourceAndProperty()
    {
        Button button = new();
        StyleSheet sheet = new StyleSheet()
            .Add(new StyleRule(StyleSelector.ForType<Button>())
                .Add(new Setter<DrawColor>(Control.BackgroundProperty, DrawColor.White)))
            .Add(new StyleRule(StyleSelector.ForType<Button>())
                .Add(new Setter<DrawColor>(Control.BackgroundProperty, DrawColor.Black)));

        new StyleApplicator().Apply(button, sheet);

        Assert.Equal(DrawColor.Black, button.Background);
    }

    [Fact]
    public void ClearsStaleValuesWhenRuleStopsMatching()
    {
        Button button = new() { IsPointerOver = true };
        StyleApplicator applicator = new();
        StyleSheet sheet = new StyleSheet().Add(new StyleRule(
                StyleSelector.ForType<Button>(),
                new VisualStateRule(PseudoClass.Hover))
            .Add(new Setter<DrawColor>(Control.BackgroundProperty, DrawColor.Black)));
        applicator.Apply(button, sheet);

        button.IsPointerOver = false;
        StyleApplicationResult result = applicator.Apply(button, sheet);

        Assert.Equal(default, button.Background);
        Assert.Single(result.ClearedValues);
        Assert.Equal(UiPropertyValueSource.StyleVisualState, result.ClearedValues[0].Source);
    }

    [Fact]
    public void DiagnosticsReportMatchedRulesAndAppliedValues()
    {
        Button button = new();
        StyleRule rule = new StyleRule(StyleSelector.ForType<Button>())
            .Add(new Setter<DrawColor>(Control.BackgroundProperty, DrawColor.White));
        StyleApplicator applicator = new();
        applicator.Apply(button, new StyleSheet().Add(rule));

        StyleDiagnostics.Snapshot diagnostics = applicator.GetDiagnostics(button);

        Assert.Equal(new[] { rule }, diagnostics.MatchedRules);
        StyleDiagnostics.AppliedValue applied = Assert.Single(diagnostics.AppliedValues);
        Assert.Equal(Control.BackgroundProperty, applied.Property);
        Assert.Equal(UiPropertyValueSource.StyleBase, applied.Source);
        Assert.Equal(DrawColor.White, applied.Value);
    }

    [Fact]
    public void DiagnosticsReportEffectiveSourceWhenLocalValueOverridesStyle()
    {
        Button button = new() { Background = DrawColor.Black };
        StyleRule rule = new StyleRule(StyleSelector.ForType<Button>())
            .Add(new Setter<DrawColor>(Control.BackgroundProperty, DrawColor.White));
        StyleApplicator applicator = new();

        applicator.Apply(button, new StyleSheet().Add(rule));

        StyleDiagnostics.AppliedValue applied = Assert.Single(applicator.GetDiagnostics(button).AppliedValues);
        Assert.Equal(UiPropertyValueSource.Local, applied.Source);
        Assert.Equal(DrawColor.Black, applied.Value);
    }

    [Fact]
    public void DiagnosticsReportClearedValues()
    {
        Button button = new() { IsPointerOver = true };
        StyleRule rule = new StyleRule(StyleSelector.ForType<Button>(), new VisualStateRule(PseudoClass.Hover))
            .Add(new Setter<DrawColor>(Control.BackgroundProperty, DrawColor.Black));
        StyleApplicator applicator = new();
        StyleSheet sheet = new StyleSheet().Add(rule);
        applicator.Apply(button, sheet);

        button.IsPointerOver = false;
        applicator.Apply(button, sheet);

        StyleDiagnostics.ClearedValue cleared = Assert.Single(applicator.GetDiagnostics(button).ClearedValues);
        Assert.Equal(Control.BackgroundProperty, cleared.Property);
        Assert.Equal(UiPropertyValueSource.StyleVisualState, cleared.Source);
        Assert.Same(rule, cleared.Rule);
    }

    [Fact]
    public void ReapplyingUnchangedStyleDoesNotRaiseDuplicatePropertyChange()
    {
        Button button = new();
        int changes = 0;
        button.PropertyChanged += (_, _) => changes++;
        StyleApplicator applicator = new();
        StyleSheet sheet = new StyleSheet().Add(new StyleRule(StyleSelector.ForType<Button>())
            .Add(new Setter<DrawColor>(Control.BackgroundProperty, DrawColor.White)));

        applicator.Apply(button, sheet);
        applicator.Apply(button, sheet);

        Assert.Equal(1, changes);
    }
}
