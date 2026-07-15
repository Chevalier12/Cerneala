using Cerneala.Drawing;
using Cerneala.UI.Aspect;
using Cerneala.UI.Controls;
using Cerneala.UI.Controls.Templates;

namespace Cerneala.Tests.UI.Aspect;

public sealed class AspectEngineStressBudgetTests
{
    [Fact]
    public void ApplyingDefaultPackageToThousandButtonsStaysWithinBudget()
    {
        AspectCatalog catalog = new AspectRegistry().Register(DefaultAspectPackage.Create()).BuildCatalog();
        AspectEnvironment environment = DefaultAspectPackage.CreateEnvironment();
        AspectEngine engine = new();

        for (int index = 0; index < 1_000; index++)
        {
            engine.Apply(new Button(), catalog, environment);
        }

        Assert.Equal(catalog.Rules.Count * 1_000, engine.Counters.RulesConsidered);
        Assert.Equal(1_000, engine.Counters.RulesMatched);
        Assert.True(engine.Counters.DeclarationsResolved <= 5_000);
        Assert.True(engine.Counters.TokenLookups <= 5_000);
    }

    [Fact]
    public void TokenChangeInvalidatesOnlyDependentElements()
    {
        AspectCatalog catalog = new AspectRegistry().Register(DefaultAspectPackage.Create()).BuildCatalog();
        AspectEnvironment environment = DefaultAspectPackage.CreateEnvironment();
        AspectEngine engine = new();
        Button button = new();
        Border border = new();

        engine.Apply(button, catalog, environment);
        engine.Apply(border, catalog, environment);

        AspectDependencySet buttonDependencies = engine.GetDependencies(button);
        AspectDependencySet borderDependencies = engine.GetDependencies(border);

        Assert.Contains(ButtonTokens.Background, buttonDependencies.Tokens);
        Assert.DoesNotContain(DefaultAspectTokens.Color.Surface, buttonDependencies.Tokens);
        Assert.DoesNotContain(DefaultAspectTokens.Brush.Surface, borderDependencies.Tokens);
        Assert.Contains(DefaultAspectTokens.Brush.Border, borderDependencies.Tokens);
    }

    [Fact]
    public void StateChangeDoesNotRecomputeUnrelatedRules()
    {
        AspectRuleSet baseRule = new(
            "button.base",
            AspectLayer.App,
            new AspectTarget(typeof(Button)),
            [new AspectDeclaration(Control.BackgroundProperty, AspectValue<Cerneala.UI.Media.Brush?>.Literal(new Cerneala.UI.Media.SolidColorBrush(Color.White)))],
            0);
        AspectRuleSet hoverRule = new(
            "button.hover",
            AspectLayer.App,
            new AspectTarget(typeof(Button), conditions: [AspectCondition.State(AspectState.Hover)]),
            [new AspectDeclaration(Control.BorderBrushProperty, AspectValue<Cerneala.UI.Media.Brush?>.Literal(new Cerneala.UI.Media.SolidColorBrush(Color.Black)))],
            1);
        AspectRuleSet textRule = new(
            "text.selected",
            AspectLayer.App,
            new AspectTarget(typeof(TextBlock), conditions: [AspectCondition.State(AspectState.Selected)]),
            [new AspectDeclaration(Control.ForegroundProperty, AspectValue<Cerneala.UI.Media.Brush?>.Literal(new Cerneala.UI.Media.SolidColorBrush(Color.Black)))],
            2);
        AspectCatalog catalog = new AspectRegistry()
            .Register(AspectPackage.Create("stress").Components(components =>
            {
                components.AddRule(baseRule);
                components.AddRule(hoverRule);
                components.AddRule(textRule);
            }))
            .BuildCatalog();
        AspectEngine engine = new();

        engine.Apply(new Button(), catalog, new AspectEnvironment("stress"));

        Assert.Equal(3, engine.Counters.RulesConsidered);
        Assert.Equal(1, engine.Counters.RulesMatched);
        Assert.Equal(1, engine.Counters.DeclarationsResolved);
    }

    [Fact]
    public void ContentTemplateLookupUsesCacheForRepeatedTypes()
    {
        ContentTemplateRegistry registry = new();
        registry.Register(new ContentTemplate<string>(
            "string-card",
            key: null,
            priority: 0,
            context => new TextBlock { Text = context.Data ?? string.Empty }));

        Assert.True(registry.TryResolve(new ContentTemplateMatchContext("first"), out _));
        Assert.True(registry.TryResolve(new ContentTemplateMatchContext("second"), out _));

        Assert.Equal(1, registry.CacheMisses);
        Assert.Equal(1, registry.CacheHits);
    }
}
