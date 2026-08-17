using Cerneala.Language.Syntax;
using Cerneala.Language.Text;

namespace Cerneala.Tests.Language;

public sealed class MarkupParserTests
{
    [Fact]
    public void LexerIsLosslessAcrossXmlTriviaNamespacesAndEmbeddedComparators()
    {
        const string markup = "<!-- before --><ns:Button xmlns:ns=\"clr-namespace:Demo\">@if value < 5 { <![CDATA[x<y]]> }</ns:Button>";
        SourceText source = SourceText.From(markup);
        IReadOnlyList<SyntaxToken> tokens = new MarkupLexer(source).Lex();

        Assert.Equal(markup, string.Concat(tokens.Select(token => token.Text)));
        Assert.Contains(tokens, token => token.Kind == SyntaxKind.CommentToken);
        Assert.Contains(tokens, token => token.Kind == SyntaxKind.CDataToken);
        Assert.Contains(tokens, token => token.Kind == SyntaxKind.NameToken && token.Text == "ns:Button");
        Assert.Contains(tokens, token => token.Kind == SyntaxKind.TextToken && token.Text.Contains("value < 5", StringComparison.Ordinal));
    }

    [Fact]
    public void ValidDocumentsRoundTripByteForByteWithCompleteTree()
    {
        foreach (CorpusCase item in CorpusCatalog.Load())
        {
            SourceText source = SourceText.From(item.Valid);
            DocumentSyntax document = MarkupParser.Parse(source);

            Assert.Equal(item.Valid, document.ToFullString());
            Assert.All(document.Tokens, token => Assert.InRange(token.Span.End, 0, source.Length));
            Assert.DoesNotContain(document.DescendantElements(), element => element.HasMissingTokens);
        }
    }

    [Fact]
    public void RealRepositoryDocumentsRoundTripAndHaveCompleteTrees()
    {
        string root = CorpusCatalog.RepositoryRoot();
        string manifest = Path.Combine(AppContext.BaseDirectory, "Corpus", "repository-documents.txt");
        string[] paths = File.ReadAllLines(manifest)
            .Select(line => line.Trim())
            .Where(line => line.EndsWith(".crn", StringComparison.Ordinal))
            .ToArray();

        foreach (string path in paths)
        {
            string markup = File.ReadAllText(Path.Combine(root, path));
            DocumentSyntax document = MarkupParser.Parse(SourceText.From(markup));
            Assert.Equal(markup, document.ToFullString());
            Assert.DoesNotContain(document.DescendantElements(), element => element.HasMissingTokens);
        }
    }

    [Fact]
    public void MissingTokensUseDeterministicZeroWidthSpans()
    {
        DocumentSyntax first = MarkupParser.Parse(SourceText.From("<StackPanel><Button /></StackPanel"));
        DocumentSyntax second = MarkupParser.Parse(SourceText.From("<StackPanel><Button /></StackPanel"));
        ElementSyntax firstRoot = first.DescendantElements().First();
        ElementSyntax secondRoot = second.DescendantElements().First();

        Assert.True(firstRoot.CloseGreaterThanToken.IsMissing);
        Assert.Equal(0, firstRoot.CloseGreaterThanToken.Span.Length);
        Assert.Equal(firstRoot.CloseGreaterThanToken.Span, secondRoot.CloseGreaterThanToken.Span);
    }

    [Fact]
    public void OverlappedElementsRecoverAtTheMatchingAncestorClose()
    {
        DocumentSyntax document = MarkupParser.Parse(SourceText.From("<StackPanel><Border><Button Name=\"Inside\" /></StackPanel><TextBlock Name=\"After\" />"));
        string[] names = document.DescendantElements().Select(element => element.Name).ToArray();

        Assert.Contains("Button", names);
        Assert.Contains("TextBlock", names);
        Assert.True(document.DescendantElements().Single(element => element.Name == "Border").HasMissingTokens);
    }

    [Fact]
    public void TenThousandRandomIncrementalEditsNeverThrowOrProduceInvalidSpans()
    {
        string seedPath = Path.Combine(CorpusCatalog.RepositoryRoot(), "CernealaPresentation", "AspectChapterView.crn");
        SourceText source = SourceText.From(File.ReadAllText(seedPath));
        Random random = new(0xCE2E_A1A);
        string[] insertions = ["<", ">", "\"", "'", "@if ", "{}", " ", "x", "</", "<!--"];

        for (int iteration = 0; iteration < 10_000; iteration++)
        {
            int start = random.Next(source.Length + 1);
            int maximumDelete = Math.Min(3, source.Length - start);
            int delete = maximumDelete == 0 ? 0 : random.Next(maximumDelete + 1);
            string insertion = insertions[random.Next(insertions.Length)];
            source = source.WithChange(new TextChange(new TextSpan(start, delete), insertion));
            DocumentSyntax document = MarkupParser.Parse(source);

            Assert.Equal(source.ToString(), document.ToFullString());
            Assert.All(document.Tokens, token =>
            {
                Assert.InRange(token.Span.Start, 0, source.Length);
                Assert.InRange(token.Span.End, token.Span.Start, source.Length);
            });
            Assert.All(document.Diagnostics, diagnostic =>
            {
                Assert.InRange(diagnostic.Span.Start, 0, source.Length);
                Assert.InRange(diagnostic.Span.End, diagnostic.Span.Start, source.Length);
            });
        }
    }
}
