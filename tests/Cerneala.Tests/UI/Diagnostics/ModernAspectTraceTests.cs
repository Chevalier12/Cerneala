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
        AspectCatalog catalog = CatalogWith("App", Rule("button", AspectLayer.App, Declaration(DrawColor.White)));

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
            Rule("first", AspectLayer.App, Declaration(DrawColor.White), order: 0),
            Rule("second", AspectLayer.App, Declaration(DrawColor.Black), order: 1));

        engine.Apply(button, catalog, new AspectEnvironment("test"));

        AspectTraceSnapshot trace = AspectTrace.Capture(button, Control.BackgroundProperty, engine.GetDiagnostics(button));
        Assert.Contains(trace.Lines, line => line.Contains("rejected", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void TraceShowsTokenResolutionChain()
    {
        AspectToken<DrawColor> token = AspectToken.Color("app.accent");
        AspectEnvironment environment = new("scope");
        environment.Set(token, DrawColor.White);
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
            [Declaration(DrawColor.White)],
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

    private static AspectDeclaration Declaration(DrawColor color)
    {
        return new AspectDeclaration(Control.BackgroundProperty, AspectValue<DrawColor>.Literal(color));
    }

    private enum ButtonKind
    {
        Primary
    }
}
