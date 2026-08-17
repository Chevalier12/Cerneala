using System.Text.Json;
using System.Text.RegularExpressions;
using TextMateSharp.Grammars;
using TextMateSharp.Registry;

namespace Cerneala.Tests.VisualStudio;

public sealed class CernealaGrammarTests
{
    [Fact]
    public void GoldenCorpusExposesApprovedFallbackScopes()
    {
        IReadOnlyList<TokenizedSpan> tokens = TokenizeCorpus(ThemeName.VisualStudioDark);
        GoldenToken[] expected = JsonSerializer.Deserialize<GoldenToken[]>(
            File.ReadAllText(GoldenPath()))!;

        foreach (GoldenToken golden in expected)
        {
            bool matched = tokens.Any(
                token => token.Line == golden.Line &&
                    token.Text == golden.Text &&
                    token.Scopes.Contains(golden.Scope, StringComparer.Ordinal));
            Assert.True(
                matched,
                $"Missing line {golden.Line} token '{golden.Text}' with scope '{golden.Scope}'. " +
                $"Actual line: {string.Join(" | ", tokens.Where(token => token.Line == golden.Line).Select(
                    token => $"'{token.Text}' [{string.Join(", ", token.Scopes)}]"))}");
        }
    }

    [Fact]
    public void IncompleteDirectiveEditDoesNotLeakIntoTheFollowingTag()
    {
        string[] lines =
        [
            "<Border>",
            "  @prism { @lay",
            "  <TextBlock Text=\"Still visible\" />",
            "</Border>"
        ];
        IReadOnlyList<TokenizedSpan> tokens = Tokenize(lines, ThemeName.VisualStudioDark);

        Assert.Contains(
            tokens,
            token => token.Line == 2 && token.Text == "@lay" &&
                token.Scopes.Contains("keyword.control.directive.incomplete.cerneala", StringComparer.Ordinal));
        Assert.Contains(
            tokens,
            token => token.Line == 3 && token.Text == "TextBlock" &&
                token.Scopes.Contains("keyword.other.type.cerneala", StringComparer.Ordinal));
        Assert.DoesNotContain(
            tokens.Where(token => token.Line >= 3),
            token => token.Scopes.Any(scope => scope.Contains("incomplete", StringComparison.Ordinal)));
    }

    [Fact]
    public void ElementTypeNamesUseTheBlueKeywordFallbackClassification()
    {
        IReadOnlyList<TokenizedSpan> tokens = Tokenize(
            ["<Grid><TextBlock /></Grid>"],
            ThemeName.VisualStudioDark);

        foreach (string typeName in new[] { "Grid", "TextBlock" })
        {
            Assert.Contains(
                tokens,
                token => token.Text == typeName &&
                    token.Scopes.Contains("keyword.other.type.cerneala", StringComparer.Ordinal));
            Assert.DoesNotContain(
                tokens,
                token => token.Text == typeName &&
                    token.Scopes.Contains("entity.name.tag.cerneala", StringComparer.Ordinal));
            Assert.DoesNotContain(
                tokens,
                token => token.Text == typeName &&
                    token.Scopes.Any(scope => scope.StartsWith("meta.tag", StringComparison.Ordinal)));
        }
    }

    [Fact]
    public void MotionHandleDeclarationAndUseHaveTheStandardLabelFallbackScope()
    {
        IReadOnlyList<TokenizedSpan> tokens = Tokenize(
            ["@handle Loading;", "@run $LoadingSequence as Loading;"],
            ThemeName.VisualStudioDark);

        TokenizedSpan[] handles = tokens.Where(token => token.Text == "Loading").ToArray();
        Assert.Equal(2, handles.Length);
        Assert.All(handles, token => Assert.Contains(
            "entity.name.label.handle.cerneala",
            token.Scopes,
            StringComparer.Ordinal));
    }

    [Theory]
    [InlineData(ThemeName.VisualStudioLight)]
    [InlineData(ThemeName.VisualStudioDark)]
    [InlineData(ThemeName.HighContrastDark)]
    public void StandardFallbackScopesResolveThroughEditorThemes(ThemeName themeName)
    {
        Registry registry = CreateRegistry(themeName, out TextMateSharp.Grammars.IGrammar grammar);
        IReadOnlyList<TokenizedSpan> tokens = Tokenize(
            ["<TextBlock Text=\"$DataContext.Title\" />"],
            grammar);
        string[] representativeScopes =
        [
            "keyword.other.type.cerneala",
            "entity.other.attribute-name.cerneala",
            "string.quoted.double.cerneala",
            "variable.other.binding.cerneala"
        ];

        foreach (string scope in representativeScopes)
        {
            Assert.Contains(
                tokens,
                candidate => candidate.Scopes.Contains(scope) &&
                    registry.GetTheme().Match(candidate.Scopes.ToList()).Any(
                        rule => !string.IsNullOrWhiteSpace(
                            registry.GetTheme().GetColor(rule.foreground))));
        }
    }

    [Fact]
    public void GrammarDelegatesColorsToClassificationThemes()
    {
        using JsonDocument grammar = JsonDocument.Parse(File.ReadAllText(GrammarPath()));
        Assert.DoesNotContain(
            DescendantPropertyNames(grammar.RootElement),
            property => property is "foreground" or "background" or "fontStyle" or "colors" or "settings");
    }

    [Fact]
    public void LanguageConfigurationProvidesLocalEditingWithoutAServer()
    {
        using JsonDocument configuration = JsonDocument.Parse(File.ReadAllText(ConfigurationPath()));
        JsonElement root = configuration.RootElement;
        Assert.Equal("<!--", root.GetProperty("comments").GetProperty("blockComment")[0].GetString());
        Assert.Equal("-->", root.GetProperty("comments").GetProperty("blockComment")[1].GetString());
        AssertPair(root.GetProperty("brackets"), "<", ">");
        AssertPair(root.GetProperty("brackets"), "{", "}");
        AssertPair(root.GetProperty("surroundingPairs"), "<", ">");
        Assert.Contains(
            root.GetProperty("autoClosingPairs").EnumerateArray(),
            pair => pair.GetProperty("open").GetString() == "<" && pair.GetProperty("close").GetString() == ">");

        JsonElement indentation = root.GetProperty("indentationRules");
        Regex increase = new(indentation.GetProperty("increaseIndentPattern").GetString()!);
        Regex decrease = new(indentation.GetProperty("decreaseIndentPattern").GetString()!);
        Regex nextLine = new(indentation.GetProperty("indentNextLinePattern").GetString()!);
        Assert.Matches(increase, "<Grid>");
        Assert.Matches(increase, "@sequence {");
        Assert.Matches(decrease, "</Grid>");
        Assert.Matches(decrease, "}");
        Assert.Matches(nextLine, "@prism");

        Regex word = new(root.GetProperty("wordPattern").GetString()!);
        Assert.Equal("$DataContext.Title:TwoWay", word.Match("$DataContext.Title:TwoWay").Value);
        Assert.Equal("@sequence", word.Match("@sequence").Value);
        Assert.Equal("120ms", word.Match("120ms").Value);
    }

    private static IReadOnlyList<TokenizedSpan> TokenizeCorpus(ThemeName themeName) =>
        Tokenize(File.ReadAllLines(CorpusPath()), themeName);

    private static IReadOnlyList<TokenizedSpan> Tokenize(IEnumerable<string> lines, ThemeName themeName)
    {
        CreateRegistry(themeName, out TextMateSharp.Grammars.IGrammar grammar);
        return Tokenize(lines, grammar);
    }

    private static Registry CreateRegistry(
        ThemeName themeName,
        out TextMateSharp.Grammars.IGrammar grammar)
    {
        Registry registry = new(new RegistryOptions(themeName));
        grammar = registry.LoadGrammarFromPathSync(
            GrammarPath(),
            initialLanguage: 0,
            embeddedLanguages: new Dictionary<string, int>());
        return registry;
    }

    private static IReadOnlyList<TokenizedSpan> Tokenize(
        IEnumerable<string> lines,
        TextMateSharp.Grammars.IGrammar grammar)
    {
        List<TokenizedSpan> spans = [];
        TextMateSharp.Grammars.IStateStack? state = null;
        int lineNumber = 0;
        foreach (string line in lines)
        {
            lineNumber++;
            TextMateSharp.Grammars.ITokenizeLineResult result = grammar.TokenizeLine(
                line,
                state,
                TimeSpan.FromSeconds(2));
            state = result.RuleStack;
            foreach (TextMateSharp.Grammars.IToken token in result.Tokens)
            {
                int start = Math.Min(token.StartIndex, line.Length);
                int end = Math.Min(token.EndIndex, line.Length);
                if (end > start)
                {
                    spans.Add(new TokenizedSpan(
                        lineNumber,
                        line[start..end],
                        token.Scopes.ToArray()));
                }
            }
        }

        return spans;
    }

    private static IEnumerable<string> DescendantPropertyNames(JsonElement element)
    {
        if (element.ValueKind == JsonValueKind.Object)
        {
            foreach (JsonProperty property in element.EnumerateObject())
            {
                yield return property.Name;
                foreach (string descendant in DescendantPropertyNames(property.Value))
                {
                    yield return descendant;
                }
            }
        }
        else if (element.ValueKind == JsonValueKind.Array)
        {
            foreach (JsonElement child in element.EnumerateArray())
            {
                foreach (string descendant in DescendantPropertyNames(child))
                {
                    yield return descendant;
                }
            }
        }
    }

    private static void AssertPair(JsonElement pairs, string open, string close)
    {
        Assert.Contains(
            pairs.EnumerateArray(),
            pair => pair[0].GetString() == open && pair[1].GetString() == close);
    }

    private static string CorpusPath() => Path.Combine(GoldenDirectory(), "cerneala-tokenization.crn");

    private static string GoldenPath() => Path.Combine(GoldenDirectory(), "cerneala-tokenization.golden.json");

    private static string GoldenDirectory() => Path.Combine(
        VisualStudioPackageTests.RepositoryRoot(),
        "tests",
        "Cerneala.Tests.VisualStudio",
        "Golden");

    private static string GrammarPath() => Path.Combine(
        VisualStudioPackageTests.RepositoryRoot(),
        "Cerneala.VisualStudio",
        "Grammars",
        "cerneala.tmLanguage.json");

    private static string ConfigurationPath() => Path.Combine(
        VisualStudioPackageTests.RepositoryRoot(),
        "Cerneala.VisualStudio",
        "language-configuration.json");

    private sealed record GoldenToken(int Line, string Text, string Scope);

    private sealed record TokenizedSpan(int Line, string Text, IReadOnlyList<string> Scopes);
}
