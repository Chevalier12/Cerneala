using Cerneala.UI.Controls;
using Microsoft.CodeAnalysis;

namespace Cerneala.Tests.SourceGen;

// #1164: an xmlns:prefix declaration is in scope for its element and its descendants, and an inner
// declaration of the same prefix replaces the outer one for its subtree (XML Namespaces, as in XAML).
public sealed partial class UiMarkupGeneratorTests
{
    private const string XmlnsScopeInput = """
        using Cerneala.UI.Controls;

        namespace A { public sealed class Foo : Border { } }
        namespace B { public sealed class Foo : Border { } }
        """;

    [Fact]
    public void InnerXmlnsShadowsRootForElementType()
    {
        const string markup = """
            <StackPanel xmlns:rows="clr-namespace:A">
              <StackPanel xmlns:rows="clr-namespace:B">
                <rows:Foo />
              </StackPanel>
              <rows:Foo />
            </StackPanel>
            """;

        StackPanel root = CreateXmlnsScopeView("InnerXmlns.crn", markup);

        StackPanel inner = Assert.IsType<StackPanel>(root.Children[0]);
        Assert.Equal("B.Foo", inner.Children[0].GetType().FullName);
        Assert.Equal("A.Foo", root.Children[1].GetType().FullName);
    }

    [Fact]
    public void AspectTargetTypeResolvesTheDeclarationInItsOwnScope()
    {
        const string markup = """
            <StackPanel xmlns:rows="clr-namespace:A">
              <StackPanel.Resources>
                <Aspect TargetType="rows:Foo">@default { Width = 37; }</Aspect>
              </StackPanel.Resources>
              <StackPanel xmlns:rows="clr-namespace:B">
                <StackPanel.Resources>
                  <Aspect TargetType="rows:Foo">@default { Height = 41; }</Aspect>
                </StackPanel.Resources>
                <rows:Foo />
              </StackPanel>
              <rows:Foo />
            </StackPanel>
            """;

        StackPanel root = CreateXmlnsScopeView("XmlnsAspectScope.crn", markup, out GeneratorRunResult result);

        StackPanel inner = Assert.IsType<StackPanel>(root.Children[0]);
        Assert.Equal("B.Foo", inner.Children[0].GetType().FullName);
        Assert.Equal("A.Foo", root.Children[1].GetType().FullName);
        string[] rules = Assert.Single(result.GeneratedSources).SourceText.ToString()
            .Split('\n')
            .Where(line => line.Contains("new global::Cerneala.UI.Aspect.AspectRuleSet(", StringComparison.Ordinal))
            .ToArray();
        Assert.Equal(2, rules.Length);
        string rootRule = Assert.Single(rules, rule => rule.Contains("AspectTarget(typeof(global::A.Foo))", StringComparison.Ordinal));
        string innerRule = Assert.Single(rules, rule => rule.Contains("AspectTarget(typeof(global::B.Foo))", StringComparison.Ordinal));
        Assert.Contains("WidthProperty", rootRule, StringComparison.Ordinal);
        Assert.Contains("HeightProperty", innerRule, StringComparison.Ordinal);
    }

    private static StackPanel CreateXmlnsScopeView(string fileName, string markup) =>
        CreateXmlnsScopeView(fileName, markup, out _);

    private static StackPanel CreateXmlnsScopeView(string fileName, string markup, out GeneratorRunResult result)
    {
        result = RunGeneratorWithInput(fileName, markup, XmlnsScopeInput, out Compilation compilation);
        AssertNoGeneratorOrCompilationErrors(result, compilation);
        using MemoryStream stream = new();
        Assert.True(compilation.Emit(stream).Success);
        return Assert.IsType<StackPanel>(
            InvokeCreate(stream, "Cerneala.GeneratedUi." + Path.GetFileNameWithoutExtension(fileName) + "Factory"));
    }
}
