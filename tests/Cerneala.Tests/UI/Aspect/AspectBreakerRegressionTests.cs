using Cerneala.UI.Aspect;
using Cerneala.UI.Controls;
using Cerneala.UI.Controls.Templates;
using Cerneala.UI.Elements;

namespace Cerneala.Tests.UI.Aspect;

public sealed class AspectBreakerRegressionTests
{
    [Fact]
    public void ParentTokenChangeReappliesTrackedElementUsingChildFallback()
    {
        AspectToken<float> opacity = AspectToken.Create<float>("breaker.parent.opacity");
        AspectEnvironment parent = new("parent");
        parent.Set(opacity, 0.25f);
        AspectEnvironment child = parent.CreateChildScope("child");
        AspectCatalog catalog = CreateOpacityCatalog(opacity);
        Border border = new();
        using AspectInvalidation invalidation = new(new AspectEngine(), catalog, child);

        invalidation.Track(border);
        Assert.Equal(0.25f, border.Opacity);

        parent.Set(opacity, 0.75f);

        Assert.True(child.TryGet(opacity, out float inheritedOpacity));
        Assert.Equal(0.75f, inheritedOpacity);
        Assert.Equal(0.75f, border.Opacity);
    }

    [Fact]
    public void ParentTokenChangeRefreshesAttachedTemplateBindingUsingChildFallback()
    {
        AspectToken<float> opacity = AspectToken.Create<float>("breaker.parent.template-opacity");
        AspectEnvironment parent = new("parent");
        parent.Set(opacity, 0.25f);
        AspectEnvironment child = parent.CreateChildScope("child");
        Border border = new();
        TemplateTokenBinding<float> binding = new(opacity, border, UIElement.OpacityProperty, child);

        binding.Attach();
        try
        {
            Assert.Equal(0.25f, border.Opacity);

            parent.Set(opacity, 0.75f);

            Assert.True(child.TryGet(opacity, out float inheritedOpacity));
            Assert.Equal(0.75f, inheritedOpacity);
            Assert.Equal(0.75f, border.Opacity);
        }
        finally
        {
            binding.Detach();
        }
    }

    [Fact]
    public void AncestorTokenChangeReappliesTrackedElementThroughNestedFallback()
    {
        AspectToken<float> opacity = AspectToken.Create<float>("breaker.ancestor.opacity");
        AspectEnvironment ancestor = new("ancestor");
        ancestor.Set(opacity, 0.25f);
        AspectEnvironment parent = ancestor.CreateChildScope("parent");
        AspectEnvironment child = parent.CreateChildScope("child");
        Border border = new();
        using AspectInvalidation invalidation = new(new AspectEngine(), CreateOpacityCatalog(opacity), child);

        invalidation.Track(border);
        ancestor.Set(opacity, 0.75f);

        Assert.Equal(0.75f, border.Opacity);
    }

    [Fact]
    public void ParentTokenChangeDoesNotReapplyBranchWithLocalOverride()
    {
        AspectToken<float> opacity = AspectToken.Create<float>("breaker.override.opacity");
        AspectEnvironment parent = new("parent");
        parent.Set(opacity, 0.25f);
        AspectEnvironment child = parent.CreateChildScope("child");
        child.Set(opacity, 0.5f);
        AspectEngine engine = new();
        Border border = new();
        using AspectInvalidation invalidation = new(engine, CreateOpacityCatalog(opacity), child);

        invalidation.Track(border);
        int rulesConsidered = engine.Counters.RulesConsidered;
        parent.Set(opacity, 0.75f);

        Assert.Equal(0.5f, border.Opacity);
        Assert.Equal(rulesConsidered, engine.Counters.RulesConsidered);
    }

    private static AspectCatalog CreateOpacityCatalog(AspectToken<float> opacity)
    {
        return CreateOpacityCatalog(opacity.Ref());
    }

    private static AspectCatalog CreateOpacityCatalog(AspectValue<float> opacity)
    {
        return AspectCatalog.FromPackages(
        [
            AspectPackage.Create("Breaker opacity")
                .Components(components => components.AddRule(new AspectRuleSet(
                    "opacity",
                    AspectLayer.App,
                    new AspectTarget(typeof(Border)),
                    [new AspectDeclaration(UIElement.OpacityProperty, opacity)],
                    declarationOrder: 0)))
        ],
        version: 1);
    }
}
