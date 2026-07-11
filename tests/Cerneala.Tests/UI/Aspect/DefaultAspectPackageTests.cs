using Cerneala.Drawing;
using Cerneala.UI.Aspect;
using Cerneala.UI.Controls;

namespace Cerneala.Tests.UI.Aspect;

public sealed class DefaultAspectPackageTests
{
    [Fact]
    public void DefaultPackageDefinesCoreSemanticTokens()
    {
        AspectCatalog catalog = new AspectRegistry().Register(DefaultAspectPackage.Create()).BuildCatalog();

        Assert.True(catalog.TryGetTokenDefault(DefaultAspectTokens.Color.Background, out _));
        Assert.True(catalog.TryGetTokenDefault(DefaultAspectTokens.Color.Foreground, out _));
        Assert.True(catalog.TryGetTokenDefault(DefaultAspectTokens.Spacing.ControlPadding, out _));
    }

    [Fact]
    public void DefaultPackageDefinesButtonComponentTokens()
    {
        AspectCatalog catalog = new AspectRegistry().Register(DefaultAspectPackage.Create()).BuildCatalog();

        Assert.True(catalog.TryGetTokenDefault(ButtonTokens.Background, out _));
        Assert.True(catalog.TryGetTokenDefault(ButtonTokens.Foreground, out _));
        Assert.True(catalog.TryGetTokenDefault(ButtonTokens.DisabledOpacity, out _));
    }

    [Fact]
    public void DefaultPackageRegistersModernButtonTemplate()
    {
        AspectCatalog catalog = new AspectRegistry().Register(DefaultAspectPackage.Create()).BuildCatalog();

        Assert.Contains(catalog.ComponentTemplates, template => template.Template == ButtonTemplates.Modern);
    }

    [Fact]
    public void DefaultPackageAspectsTextBlockBorderAndButton()
    {
        AspectCatalog catalog = new AspectRegistry().Register(DefaultAspectPackage.Create()).BuildCatalog();
        AspectEngine engine = new();
        AspectEnvironment environment = DefaultAspectPackage.CreateEnvironment();
        Button button = new();
        Border border = new();

        engine.Apply(button, catalog, environment);
        engine.Apply(border, catalog, environment);

        Assert.Equal(new Color(255, 255, 255), button.Background);
        Assert.Equal(new Color(255, 255, 255), border.Background);
    }

    [Fact]
    public void DefaultPackageDoesNotRequireLegacyRuleSheet()
    {
        AspectPackage package = DefaultAspectPackage.Create();

        Assert.NotEmpty(package.Rules);
    }
}
