using Cerneala.Drawing;
using Cerneala.UI.Aspect;
using Cerneala.UI.Controls;
using Cerneala.UI.Controls.Templates;

namespace Cerneala.Tests.UI.Aspect;

public sealed class AspectPackageTests
{
    [Fact]
    public void PackageContainsNamedTokensComponentsAndTemplates()
    {
        AspectToken<DrawColor> token = AspectToken.Color("app.accent");
        ComponentTemplateDefinition componentTemplate = new("button.modern", typeof(Button), template: null);
        ContentTemplateDefinition contentTemplate = new("string", typeof(string), key: null, template: null);

        AspectPackage package = AspectPackage.Create("App")
            .Tokens(tokens => tokens.Set(token, DrawColor.White))
            .Components(components => components.AddTemplate(componentTemplate))
            .Content(content => content.Add(contentTemplate));

        Assert.Equal("App", package.Name);
        Assert.Single(package.Tokens);
        Assert.Same(componentTemplate, Assert.Single(package.ComponentTemplates));
        Assert.Same(contentTemplate, Assert.Single(package.ContentTemplates));
    }

    [Fact]
    public void RegistryCombinesPackagesInRegistrationOrder()
    {
        AspectToken<DrawColor> firstToken = AspectToken.Color("first");
        AspectToken<DrawColor> secondToken = AspectToken.Color("second");
        AspectRegistry registry = new();

        registry.Register(AspectPackage.Create("First").Tokens(tokens => tokens.Set(firstToken, DrawColor.White)));
        registry.Register(AspectPackage.Create("Second").Tokens(tokens => tokens.Set(secondToken, DrawColor.Black)));

        AspectCatalog catalog = registry.BuildCatalog();

        Assert.Equal(["First", "Second"], catalog.PackageDiagnostics.Select(package => package.Name));
        Assert.True(catalog.TryGetTokenDefault(firstToken, out AspectValue? first));
        Assert.True(catalog.TryGetTokenDefault(secondToken, out AspectValue? second));
        Assert.Equal(DrawColor.White, first.Resolve(new AspectResolutionContext(new Button(), new AspectEnvironment("test"))));
        Assert.Equal(DrawColor.Black, second.Resolve(new AspectResolutionContext(new Button(), new AspectEnvironment("test"))));
    }

    [Fact]
    public void DuplicatePackageNameThrows()
    {
        AspectRegistry registry = new();
        registry.Register(AspectPackage.Create("App"));

        Assert.Throws<InvalidOperationException>(() => registry.Register(AspectPackage.Create("App")));
    }

    [Fact]
    public void DuplicateTokenWithDifferentValueTypeThrows()
    {
        AspectRegistry registry = new();
        registry.Register(AspectPackage.Create("Colors").Tokens(tokens => tokens.Set(AspectToken.Color("app.value"), DrawColor.White)));
        registry.Register(AspectPackage.Create("Text").Tokens(tokens => tokens.Set(AspectToken.String("app.value"), "white")));

        Assert.Throws<InvalidOperationException>(() => registry.BuildCatalog());
    }

    [Fact]
    public void PackageCanContributeContentTemplates()
    {
        ContentTemplateDefinition definition = new("user-card", typeof(UserCard), key: "card", template: null);

        AspectCatalog catalog = new AspectRegistry()
            .Register(AspectPackage.Create("Content").Content(content => content.Add(definition)))
            .BuildCatalog();

        Assert.Same(definition, Assert.Single(catalog.ContentTemplates));
    }

    private sealed record UserCard(string Name);
}
