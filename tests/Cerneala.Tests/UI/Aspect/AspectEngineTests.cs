using Cerneala.Drawing;
using Cerneala.UI.Aspect;
using Cerneala.UI.Controls;
using Cerneala.UI.Controls.Primitives;
using Cerneala.UI.Core;
using Cerneala.UI.Elements;

namespace Cerneala.Tests.UI.Aspect;

public sealed class AspectEngineTests
{
    [Fact]
    public void EngineAppliesResolvedAspectValuesToElement()
    {
        Button button = new();
        AspectCatalog catalog = CatalogWith(Rule("base", Declaration(Color.White)));

        AspectApplicationResult result = new AspectEngine().Apply(button, catalog, new AspectEnvironment("test"));

        Assert.True(result.Applied);
        Assert.Equal(Color.White, button.Background);
        Assert.Equal(UiPropertyValueSource.AspectBase, button.GetValueSource(Control.BackgroundProperty));
    }

    [Fact]
    public void EngineClearsValuesNoLongerProvided()
    {
        Button button = new();
        AspectEngine engine = new();
        engine.Apply(button, CatalogWith(Rule("base", Declaration(Color.White))), new AspectEnvironment("test"));

        engine.Apply(button, EmptyCatalog(), new AspectEnvironment("test"));

        Assert.Equal(Color.Transparent, button.Background);
        Assert.Equal(UiPropertyValueSource.Default, button.GetValueSource(Control.BackgroundProperty));
    }

    [Fact]
    public void EngineDoesNotReapplyWhenCatalogEnvironmentAndStatesAreUnchanged()
    {
        Button button = new();
        AspectEngine engine = new();
        AspectCatalog catalog = CatalogWith(Rule("base", Declaration(Color.White)));
        AspectEnvironment environment = new("test");

        engine.Apply(button, catalog, environment);
        AspectApplicationResult second = engine.Apply(button, catalog, environment);

        Assert.False(second.Applied);
    }

    [Fact]
    public void EngineReappliesWhenTokenDependencyChanges()
    {
        AspectToken<Color> token = AspectToken.Color("app.background");
        Button button = new();
        AspectEnvironment environment = new("test");
        environment.Set(token, Color.White);
        AspectEngine engine = new();
        AspectCatalog catalog = CatalogWith(Rule("token", new AspectDeclaration(Control.BackgroundProperty, token.Ref())));

        engine.Apply(button, catalog, environment);
        environment.Set(token, Color.Black);
        AspectApplicationResult second = engine.Apply(button, catalog, environment);

        Assert.True(second.Applied);
        Assert.Equal(Color.Black, button.Background);
    }

    [Fact]
    public void EngineReappliesWhenStateDependencyChanges()
    {
        Button button = new();
        AspectEngine engine = new();
        AspectCatalog catalog = CatalogWith(new AspectRuleSet(
            "hover",
            AspectLayer.App,
            new AspectTarget(typeof(Button), conditions: [AspectCondition.State(AspectState.Hover)]),
            [Declaration(Color.Black)],
            0));

        engine.Apply(button, catalog, new AspectEnvironment("test"));
        button.IsPointerOver = true;
        AspectApplicationResult second = engine.Apply(button, catalog, new AspectEnvironment("test"));

        Assert.True(second.Applied);
        Assert.Equal(Color.Black, button.Background);
    }

    [Fact]
    public void EngineReappliesWhenVariantDependencyChanges()
    {
        AspectVariantKey<Button, ButtonKind> key = AspectVariantKey.For<Button, ButtonKind>("kind");
        Button button = new();
        AspectEngine engine = new();
        AspectCatalog catalog = CatalogWith(new AspectRuleSet(
            "primary",
            AspectLayer.App,
            new AspectTarget(typeof(Button), conditions: [AspectCondition.Variant(key, ButtonKind.Primary)]),
            [Declaration(Color.Black)],
            0));

        engine.Apply(button, catalog, new AspectEnvironment("test"), variants: AspectVariantSet.Empty.Set(key, ButtonKind.Neutral));
        AspectApplicationResult second = engine.Apply(button, catalog, new AspectEnvironment("test"), variants: AspectVariantSet.Empty.Set(key, ButtonKind.Primary));

        Assert.True(second.Applied);
        Assert.Equal(Color.Black, button.Background);
    }

    [Fact]
    public void EngineReappliesWhenPropertyConditionDependencyChanges()
    {
        Button button = new();
        AspectEngine engine = new();
        AspectCatalog catalog = CatalogWith(new AspectRuleSet(
            "pressed",
            AspectLayer.App,
            new AspectTarget(typeof(Button), conditions: [AspectCondition.Property(ButtonBase.IsPressedProperty).Is(true)]),
            [Declaration(Color.Black)],
            0));

        engine.Apply(button, catalog, new AspectEnvironment("test"));
        button.IsPressed = true;
        AspectApplicationResult second = engine.Apply(button, catalog, new AspectEnvironment("test"));

        Assert.True(second.Applied);
        Assert.Equal(Color.Black, button.Background);
    }

    [Fact]
    public void EngineReappliesWhenDataContextDependencyChanges()
    {
        Button button = new();
        AspectEngine engine = new();
        AspectCatalog catalog = CatalogWith(new AspectRuleSet(
            "data",
            AspectLayer.App,
            new AspectTarget(typeof(Button), conditions:
            [
                AspectCondition.Data<UserCard>("important", user => user.IsImportant, AspectDataDependency.Named("user"))
            ]),
            [Declaration(Color.Black)],
            0));

        engine.Apply(button, catalog, new AspectEnvironment("test"), dataContext: new AspectDataContext(new UserCard(false)));
        AspectApplicationResult second = engine.Apply(button, catalog, new AspectEnvironment("test"), dataContext: new AspectDataContext(new UserCard(true)));

        Assert.True(second.Applied);
        Assert.Equal(Color.Black, button.Background);
    }

    [Fact]
    public void EngineDoesNotReapplyUnrelatedDataConditions()
    {
        Button button = new();
        AspectEngine engine = new();
        AspectCatalog catalog = CatalogWith(Rule("base", Declaration(Color.White)));

        engine.Apply(button, catalog, new AspectEnvironment("test"), dataContext: new AspectDataContext(new UserCard(false)));
        AspectApplicationResult second = engine.Apply(button, catalog, new AspectEnvironment("test"), dataContext: new AspectDataContext(new UserCard(true)));

        Assert.False(second.Applied);
    }

    [Fact]
    public void EngineReportsWinnerAndRejectedDeclarations()
    {
        Button button = new();
        AspectDeclaration loser = Declaration(Color.White);
        AspectDeclaration winner = Declaration(Color.Black);
        AspectCatalog catalog = CatalogWith(Rule("first", loser, order: 0), Rule("second", winner, order: 1));

        ResolvedAspect resolved = new AspectEngine().Resolve(button, catalog, new AspectEnvironment("test"));

        Assert.Same(winner, resolved.Values[Control.BackgroundProperty].SourceDeclaration);
        Assert.Contains(resolved.RejectedDeclarations, rejected => ReferenceEquals(rejected.Rejected, loser));
    }

    [Fact]
    public void RootExposesAspectProcessorAsCanonicalProperty()
    {
        UIRoot root = new();

        Assert.NotNull(root.AspectProcessor);
    }

    private static AspectCatalog EmptyCatalog()
    {
        return new AspectRegistry().BuildCatalog();
    }

    private static AspectCatalog CatalogWith(params AspectRuleSet[] rules)
    {
        AspectPackage package = AspectPackage.Create("App")
            .Components(components =>
            {
                foreach (AspectRuleSet rule in rules)
                {
                    components.AddRule(rule);
                }
            });
        return new AspectRegistry().Register(package).BuildCatalog();
    }

    private static AspectRuleSet Rule(string name, AspectDeclaration declaration, int order = 0)
    {
        return new AspectRuleSet(name, AspectLayer.App, new AspectTarget(typeof(Button)), [declaration], order);
    }

    private static AspectDeclaration Declaration(Color color)
    {
        return new AspectDeclaration(Control.BackgroundProperty, AspectValue<Color>.Literal(color));
    }

    private sealed record UserCard(bool IsImportant);

    private enum ButtonKind
    {
        Neutral,
        Primary
    }
}
