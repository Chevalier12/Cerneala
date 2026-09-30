using Cerneala.Language.Syntax;
using Cerneala.Language.Text;

namespace Cerneala.Tests.Language;

public sealed class SyntaxTraversalTests
{
    [Fact]
    public void DescendantsPreservePreorderAndIdentityWithoutIncludingAttributesOrTheRoot()
    {
        DocumentSyntax document = MarkupParser.Parse(SourceText.From(
            "<!--before--><Panel Name=\"Root\">first<Panel.Content><!--inside--><Button />last</Panel.Content>tail</Panel><After />"));
        TextSyntax before = Assert.IsType<TextSyntax>(document.Children[0]);
        ElementSyntax panel = Assert.IsType<ElementSyntax>(document.Children[1]);
        ElementSyntax after = Assert.IsType<ElementSyntax>(document.Children[2]);
        TextSyntax first = Assert.IsType<TextSyntax>(panel.Children[0]);
        ElementSyntax content = Assert.IsType<ElementSyntax>(panel.Children[1]);
        TextSyntax tail = Assert.IsType<TextSyntax>(panel.Children[2]);
        TextSyntax inside = Assert.IsType<TextSyntax>(content.Children[0]);
        ElementSyntax button = Assert.IsType<ElementSyntax>(content.Children[1]);
        TextSyntax last = Assert.IsType<TextSyntax>(content.Children[2]);
        Assert.Equal(SyntaxKind.PropertyElement, content.Kind);
        Assert.Single(panel.Attributes);

        SyntaxNode[] expected = [before, panel, first, content, inside, button, last, tail, after];
        IEnumerable<SyntaxNode> descendants = document.DescendantNodes();
        AssertSameNodes(expected, descendants);
        AssertSameNodes(expected, descendants);
        AssertSameNodes([first, content, inside, button, last, tail], panel.DescendantNodes());
    }

    [Fact]
    public void EmptyDocumentsAndLeafNodesHaveNoDescendants()
    {
        DocumentSyntax document = MarkupParser.Parse(SourceText.From("<Button Name=\"Action\" />"));
        ElementSyntax button = Assert.IsType<ElementSyntax>(Assert.Single(document.Children));
        SyntaxToken token = new(SyntaxKind.TextToken, new TextSpan(0, 4), "text");

        Assert.Empty(MarkupParser.Parse(SourceText.From(string.Empty)).DescendantNodes());
        Assert.Empty(button.DescendantNodes());
        Assert.Empty(Assert.Single(button.Attributes).DescendantNodes());
        Assert.Empty(new TextSyntax(SyntaxKind.Text, token).DescendantNodes());
        Assert.Empty(new ErrorSyntax(token).DescendantNodes());
    }

    private static void AssertSameNodes(SyntaxNode[] expected, IEnumerable<SyntaxNode> descendants)
    {
        SyntaxNode[] actual = descendants.ToArray();
        Assert.Equal(expected.Length, actual.Length);
        for (int index = 0; index < expected.Length; index++)
        {
            Assert.Same(expected[index], actual[index]);
        }
    }
}
