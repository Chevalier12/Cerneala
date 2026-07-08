using Cerneala.Drawing;
using Cerneala.UI.Aspect;
using Cerneala.UI.Controls;

namespace Cerneala.Tests.UI.Aspect;

public sealed class AspectTokenTests
{
    [Fact]
    public void TypedTokenCarriesNameAndValueType()
    {
        AspectToken<DrawColor> token = AspectToken.Color("app.surface");

        Assert.Equal("app.surface", token.Name);
        Assert.Equal(typeof(DrawColor), token.ValueType);
    }

    [Fact]
    public void TokenRejectsEmptyName()
    {
        Assert.Throws<ArgumentException>(() => AspectToken.Create<string>(" "));
    }

    [Fact]
    public void TokenNamesAreComparedByOrdinalNameAndType()
    {
        AspectToken<string> first = AspectToken.String("app.title");
        AspectToken<string> second = AspectToken.String("app.title");
        AspectToken<float> differentType = AspectToken.Float("app.title");

        Assert.Equal(first, second);
        Assert.False(first.Equals(differentType));
    }

    [Fact]
    public void TokenReferenceResolvesThroughAspectEnvironment()
    {
        AspectToken<DrawColor> token = AspectToken.Color("app.accent");
        AspectEnvironment environment = new("root");
        environment.Set(token, DrawColor.White);
        AspectResolutionContext context = new(new Button(), environment);

        object? value = token.Ref().Resolve(context);

        Assert.Equal(DrawColor.White, value);
    }

    [Fact]
    public void MissingTokenProducesDiagnosticFailureInsteadOfInvalidCast()
    {
        AspectToken<DrawColor> token = AspectToken.Color("app.missing");
        AspectResolutionContext context = new(new Button(), new AspectEnvironment("root"));

        InvalidOperationException exception = Assert.Throws<InvalidOperationException>(() => token.Ref().Resolve(context));

        Assert.Contains("app.missing", exception.Message, StringComparison.Ordinal);
        Assert.DoesNotContain(nameof(InvalidCastException), exception.Message, StringComparison.Ordinal);
    }
}
