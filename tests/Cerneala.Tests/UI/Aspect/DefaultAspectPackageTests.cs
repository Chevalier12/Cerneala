using Cerneala.Drawing;
using Cerneala.UI.Aspect;
using Cerneala.UI.Controls;
using Cerneala.UI.Elements;
using System.Reflection;

namespace Cerneala.Tests.UI.Aspect;

public sealed class DefaultAspectPackageTests
{
    [Fact]
    public void DefaultPackageDefinesCoreSemanticTokens()
    {
        AspectCatalog catalog = new AspectRegistry().Register(DefaultAspectPackage.Create()).BuildCatalog();

        Assert.True(catalog.TryGetTokenDefault(DefaultAspectTokens.Color.Background, out _));
        Assert.True(catalog.TryGetTokenDefault(DefaultAspectTokens.Color.Foreground, out _));
        Assert.True(catalog.TryGetTokenDefault(DefaultAspectTokens.Brush.Background, out _));
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
    public void DefaultPackageAndEnvironmentDefineEquivalentTokenDefaults()
    {
        AspectCatalog catalog = new AspectRegistry().Register(DefaultAspectPackage.Create()).BuildCatalog();
        AspectEnvironment environment = DefaultAspectPackage.CreateEnvironment();
        AspectResolutionContext context = new(new UIElement(), environment);

        foreach ((AspectToken token, AspectValue defaultValue) in catalog.TokenDefaults)
        {
            object? packageValue = defaultValue.Resolve(context);
            object? environmentValue = ReadEnvironmentValue(environment, token);

            Assert.Equivalent(packageValue, environmentValue, strict: true);
        }
    }

    [Fact]
    public void DefaultPackageRegistersModernButtonTemplate()
    {
        AspectCatalog catalog = new AspectRegistry().Register(DefaultAspectPackage.Create()).BuildCatalog();

        Assert.Contains(catalog.ComponentTemplates, template => template.Template == ButtonTemplates.Modern);
    }

    [Fact]
    public void DefaultPackageStylesButtonsButLeavesStructuralBordersTransparent()
    {
        AspectCatalog catalog = new AspectRegistry().Register(DefaultAspectPackage.Create()).BuildCatalog();
        AspectEngine engine = new();
        AspectEnvironment environment = DefaultAspectPackage.CreateEnvironment();
        Button button = new();
        Border border = new();

        engine.Apply(button, catalog, environment);
        engine.Apply(border, catalog, environment);

        Assert.Equal(new Cerneala.UI.Media.SolidColorBrush(new Color(255, 255, 255)), button.Background);
        Assert.Null(border.Background);
    }

    [Fact]
    public void DefaultPackageDoesNotRequireLegacyRuleSheet()
    {
        AspectPackage package = DefaultAspectPackage.Create();

        Assert.NotEmpty(package.Rules);
    }

    private static object? ReadEnvironmentValue(AspectEnvironment environment, AspectToken token)
    {
        MethodInfo method = typeof(DefaultAspectPackageTests)
            .GetMethod(nameof(ReadEnvironmentValueCore), BindingFlags.NonPublic | BindingFlags.Static)!
            .MakeGenericMethod(token.ValueType);
        return method.Invoke(null, [environment, token]);
    }

    private static object? ReadEnvironmentValueCore<T>(AspectEnvironment environment, AspectToken token)
    {
        Assert.True(environment.TryGet((AspectToken<T>)token, out T value));
        return value;
    }
}
