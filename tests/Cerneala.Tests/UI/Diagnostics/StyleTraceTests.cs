using Cerneala.Drawing;
using Cerneala.UI.Controls;
using Cerneala.UI.Core;
using Cerneala.UI.Diagnostics;
using Cerneala.UI.Styling;

namespace Cerneala.Tests.UI.Diagnostics;

public sealed class StyleTraceTests
{
    [Fact]
    public void CaptureReportsEffectiveValueSourceAndStyleDiagnostics()
    {
        Button button = new();
        StyleRule rule = new StyleRule(StyleSelector.ForType<Button>())
            .Add(new Setter<DrawColor>(Control.BackgroundProperty, DrawColor.White));
        StyleApplicator applicator = new();
        applicator.Apply(button, new StyleSheet().Add(rule));

        StyleTraceSnapshot trace = StyleTrace.Capture(button, Control.BackgroundProperty, applicator.GetDiagnostics(button));

        Assert.Equal(Control.BackgroundProperty.DiagnosticName, trace.PropertyName);
        Assert.Equal(DrawColor.White, trace.EffectiveValue);
        Assert.Equal(UiPropertyValueSource.StyleBase, trace.EffectiveSource);
        StyleTraceRule matchedRule = Assert.Single(trace.MatchedRules);
        Assert.Equal(rule.Selector.Description, matchedRule.Selector);
        StyleTraceAppliedValue appliedValue = Assert.Single(trace.AppliedValues);
        Assert.Equal(Control.BackgroundProperty.DiagnosticName, appliedValue.PropertyName);
        Assert.Equal(DrawColor.White, appliedValue.Value);
        Assert.Equal(UiPropertyValueSource.StyleBase, appliedValue.Source);
    }

    [Fact]
    public void CaptureReportsClearedStyleValues()
    {
        Button button = new() { IsPointerOver = true };
        StyleRule rule = new StyleRule(StyleSelector.ForType<Button>(), new VisualStateRule(PseudoClass.Hover))
            .Add(new Setter<DrawColor>(Control.BackgroundProperty, DrawColor.Black));
        StyleApplicator applicator = new();
        StyleSheet sheet = new StyleSheet().Add(rule);
        applicator.Apply(button, sheet);

        button.IsPointerOver = false;
        applicator.Apply(button, sheet);

        StyleTraceSnapshot trace = StyleTrace.Capture(button, Control.BackgroundProperty, applicator.GetDiagnostics(button));

        StyleTraceClearedValue clearedValue = Assert.Single(trace.ClearedValues);
        Assert.Equal(Control.BackgroundProperty.DiagnosticName, clearedValue.PropertyName);
        Assert.Equal(UiPropertyValueSource.StyleVisualState, clearedValue.Source);
    }

    [Fact]
    public void CaptureOnlyReportsAppliedAndClearedValuesForInspectedProperty()
    {
        Button button = new() { IsPointerOver = true };
        StyleRule rule = new StyleRule(StyleSelector.ForType<Button>(), new VisualStateRule(PseudoClass.Hover))
            .Add(new Setter<DrawColor>(Control.BackgroundProperty, DrawColor.Black))
            .Add(new Setter<DrawColor>(Control.ForegroundProperty, DrawColor.White));
        StyleApplicator applicator = new();
        StyleSheet sheet = new StyleSheet().Add(rule);
        applicator.Apply(button, sheet);

        StyleTraceSnapshot appliedTrace = StyleTrace.Capture(button, Control.BackgroundProperty, applicator.GetDiagnostics(button));

        StyleTraceAppliedValue appliedValue = Assert.Single(appliedTrace.AppliedValues);
        Assert.Equal(Control.BackgroundProperty.DiagnosticName, appliedValue.PropertyName);

        button.IsPointerOver = false;
        applicator.Apply(button, sheet);

        StyleTraceSnapshot clearedTrace = StyleTrace.Capture(button, Control.BackgroundProperty, applicator.GetDiagnostics(button));

        StyleTraceClearedValue clearedValue = Assert.Single(clearedTrace.ClearedValues);
        Assert.Equal(Control.BackgroundProperty.DiagnosticName, clearedValue.PropertyName);
    }
}
