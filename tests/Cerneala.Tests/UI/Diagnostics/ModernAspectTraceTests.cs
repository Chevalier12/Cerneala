using Cerneala.Drawing;
using Cerneala.UI.Aspect;
using Cerneala.UI.Controls;
using Cerneala.UI.Diagnostics;

namespace Cerneala.Tests.UI.Diagnostics;

public sealed class ModernAspectTraceTests
{
    [Fact]
    public void TraceShowsWinningDeclarationLayerSpecificityAndPackage()
    {
        Button button = new();
        AspectEngine engine = new();
        AspectCatalog catalog = CatalogWith("App", Rule("button", AspectLayer.App, Declaration(Color.White)));

        engine.Apply(button, catalog, new AspectEnvironment("test"));

        AspectTraceSnapshot trace = AspectTrace.Capture(button, Control.BackgroundProperty, engine.GetDiagnostics(button));
        Assert.Contains(trace.Lines, line => line.Contains("App", StringComparison.Ordinal));
        Assert.Contains(trace.Lines, line => line.Contains("button", StringComparison.Ordinal));
        Assert.Contains(trace.Lines, line => line.Contains("App:300", StringComparison.Ordinal));
    }

    [Fact]
    public void TraceShowsRejectedDeclarationsWithReasons()
    {
        Button button = new();
        AspectEngine engine = new();
        AspectCatalog catalog = CatalogWith(
            "App",
            Rule("first", AspectLayer.App, Declaration(Color.White), order: 0),
            Rule("second", AspectLayer.App, Declaration(Color.Black), order: 1));

        engine.Apply(button, catalog, new AspectEnvironment("test"));

        AspectTraceSnapshot trace = AspectTrace.Capture(button, Control.BackgroundProperty, engine.GetDiagnostics(button));
        Assert.Contains(trace.Lines, line => line.Contains("rejected", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void TraceShowsTokenResolutionChain()
    {
        AspectToken<Cerneala.UI.Media.Brush?> token = AspectToken.Create<Cerneala.UI.Media.Brush?>("app.accent");
        AspectEnvironment environment = new("scope");
        environment.Set(token, new Cerneala.UI.Media.SolidColorBrush(Color.White));
        Button button = new();
        AspectEngine engine = new();
        AspectCatalog catalog = CatalogWith("App", Rule("token", AspectLayer.App, new AspectDeclaration(Control.BackgroundProperty, token.Ref())));

        engine.Apply(button, catalog, environment);

        AspectTraceSnapshot trace = AspectTrace.Capture(button, Control.BackgroundProperty, engine.GetDiagnostics(button));
        Assert.Contains(trace.Lines, line => line.Contains("app.accent", StringComparison.Ordinal));
        Assert.Contains(trace.Lines, line => line.Contains("scope", StringComparison.Ordinal));
    }

    [Fact]
    public void TraceShowsSlotAndVariantContext()
    {
        AspectVariantKey<Button, ButtonKind> key = AspectVariantKey.For<Button, ButtonKind>("kind");
        Button button = new();
        AspectEngine engine = new();
        AspectCatalog catalog = CatalogWith("App", new AspectRuleSet(
            "variant",
            AspectLayer.App,
            new AspectTarget(typeof(Button), AspectSlot.Root<Button>(), [AspectCondition.Variant(key, ButtonKind.Primary)]),
            [Declaration(Color.White)],
            0));

        engine.Apply(
            button,
            catalog,
            new AspectEnvironment("test"),
            variants: AspectVariantSet.Empty.Set(key, ButtonKind.Primary),
            slotPath: new AspectSlotPath(AspectSlot.Root<Button>(), "Root"));

        AspectTraceSnapshot trace = AspectTrace.Capture(button, Control.BackgroundProperty, engine.GetDiagnostics(button));
        Assert.Contains(trace.Lines, line => line.Contains("Root", StringComparison.Ordinal));
        Assert.Contains(trace.Lines, line => line.Contains("kind", StringComparison.Ordinal));
    }

    [Fact]
    public void TraceReportsOriginScopeRejectedRulesConditionsAndDependencies()
    {
        AspectOrigin origin = new(
            AspectAuthoringKind.MarkupDefault,
            "TraceDocument.crn",
            "Button");
        AspectPackage package = AspectPackage.Create("Markup.TraceDocument.Button")
            .Origin(origin)
            .Components(components =>
            {
                components.AddRule(Rule("base", AspectLayer.App, Declaration(Color.White)));
                components.AddRule(new AspectRuleSet(
                    "hover",
                    AspectLayer.App,
                    new AspectTarget(
                        typeof(Button),
                        conditions: [AspectCondition.Property(Cerneala.UI.Elements.UIElement.IsMouseOverProperty).Is(true)]),
                    [Declaration(Color.Black)],
                    1));
                components.AddRule(new AspectRuleSet(
                    "wrong-target",
                    AspectLayer.App,
                    new AspectTarget(typeof(TextBlock)),
                    [Declaration(Color.Black)],
                    2));
            });
        AspectCatalog catalog = new AspectRegistry().Register(package).BuildCatalog();
        Button button = new();
        AspectEngine engine = new();

        engine.Apply(button, catalog, new AspectEnvironment("test"));

        AspectDiagnostics.Snapshot diagnostics = engine.GetDiagnostics(button);
        AspectResolutionStep matched = Assert.Single(diagnostics.ResolutionSteps, step => step.RuleName == "base");
        AspectResolutionStep conditional = Assert.Single(diagnostics.ResolutionSteps, step => step.RuleName == "hover");
        AspectResolutionStep wrongTarget = Assert.Single(diagnostics.ResolutionSteps, step => step.RuleName == "wrong-target");
        Assert.Equal(origin, matched.Origin);
        Assert.Equal("root", matched.Scope);
        Assert.Equal(0, matched.SourceOrder);
        Assert.Equal("matched", matched.Outcome);
        Assert.Equal("rejected: condition mismatch", conditional.Outcome);
        Assert.False(Assert.Single(conditional.Conditions).Matches);
        Assert.Contains(conditional.Dependencies, dependency =>
            dependency.Kind == AspectConditionDependencyKind.UiProperty &&
            ReferenceEquals(dependency.Property, Cerneala.UI.Elements.UIElement.IsMouseOverProperty));
        Assert.StartsWith("rejected: target type mismatch", wrongTarget.Outcome, StringComparison.Ordinal);
        Assert.Equal(1, diagnostics.Counters.ConditionEvaluations);

        AspectTraceSnapshot trace = AspectTrace.Capture(button, Control.BackgroundProperty, diagnostics);
        Assert.Contains(trace.Lines, line => line.Contains("document=TraceDocument.crn", StringComparison.Ordinal));
        Assert.Contains(trace.Lines, line => line.Contains("origin=MarkupDefault", StringComparison.Ordinal));
        Assert.Contains(trace.Lines, line => line.Contains("scope=root", StringComparison.Ordinal));
        Assert.Contains(trace.Lines, line => line.Contains("dependencies=[UiProperty:", StringComparison.Ordinal));
        Assert.Contains(trace.Lines, line => line.Contains("outcome=rejected: condition mismatch", StringComparison.Ordinal));
    }

    private static AspectCatalog CatalogWith(string packageName, params AspectRuleSet[] rules)
    {
        AspectPackage package = AspectPackage.Create(packageName).Components(components =>
        {
            foreach (AspectRuleSet rule in rules)
            {
                components.AddRule(rule);
            }
        });
        return new AspectRegistry().Register(package).BuildCatalog();
    }

    private static AspectRuleSet Rule(string name, AspectLayer layer, AspectDeclaration declaration, int order = 0)
    {
        return new AspectRuleSet(name, layer, new AspectTarget(typeof(Button)), [declaration], order);
    }

    private static AspectDeclaration Declaration(Color color)
    {
        return new AspectDeclaration(
            Control.BackgroundProperty,
            AspectValue<Cerneala.UI.Media.Brush?>.Literal(new Cerneala.UI.Media.SolidColorBrush(color)));
    }

    private enum ButtonKind
    {
        Primary
    }
}
