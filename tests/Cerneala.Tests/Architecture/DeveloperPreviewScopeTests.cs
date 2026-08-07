namespace Cerneala.Tests.Architecture;

public sealed class DeveloperPreviewScopeTests
{
    private static readonly string[] PreviewSampleForbiddenMarkupTerms =
    [
        "Cerneala.UI.Markup",
        "Cerneala.SourceGen",
        "UiMarkup",
        "GeneratedUiFactory",
        "SourceGenerator",
        "ISourceGenerator",
        "IncrementalGenerator"
    ];

    private static readonly string[] PreviewSampleForbiddenAnimationAndMediaTerms =
    [
        "Cerneala.UI.Animation",
        "AnimationScheduler",
        "AnimatedValueSource",
        "Storyboard",
        "Transition",
        "LinearGradientBrush",
        "RadialGradientBrush",
        "PathGeometry",
        "OpacityLayer",
        "ShadowEffect",
        "RenderTarget"
    ];

    private static readonly string[] PreviewSampleForbiddenNativeAccessibilityTerms =
    [
        "AutomationPeer",
        "System.Windows.Automation",
        "Windows.Win32",
        "UIAutomation",
        "IAccessible",
        "NativeAccessibility"
    ];

    [Fact]
    public void PreviewSamplesDoNotReferenceMarkupOrSourceGeneration()
    {
        AssertNoForbiddenTermsInPreviewSources(PreviewSampleForbiddenMarkupTerms);
    }

    [Fact]
    public void PreviewSamplesDoNotReferenceAnimationOrAdvancedMediaEffects()
    {
        AssertNoForbiddenTermsInPreviewSources(PreviewSampleForbiddenAnimationAndMediaTerms);
    }

    [Fact]
    public void PreviewSamplesDoNotReferenceNativeAccessibilityAdapters()
    {
        AssertNoForbiddenTermsInPreviewSources(PreviewSampleForbiddenNativeAccessibilityTerms);
    }

    [Fact]
    public void PreviewSamplesUseDefaultThemeInsteadOfHardCodingAllControlChrome()
    {
        string root = FindRepositoryRoot();
        string[] forbiddenButtonChrome =
        [
            "Background",
            "Foreground",
            "BorderBrush",
            "BorderThickness",
            "Template"
        ];

        foreach (string file in EnumeratePlaygroundSampleFiles(root))
        {
            string source = RemoveCommentsAndStringLiterals(File.ReadAllText(file));
            string[] buttonInitializers = ExtractObjectInitializers(source, "Button")
                .Concat(ExtractTargetTypedObjectInitializers(source, "Button"))
                .ToArray();
            foreach (string initializer in buttonInitializers)
            {
                string[] assignedProperties = ExtractTopLevelAssignedProperties(initializer);
                string[] foundTerms = forbiddenButtonChrome
                    .Where(term => assignedProperties.Contains(term, StringComparer.Ordinal))
                    .ToArray();

                Assert.True(
                    foundTerms.Length == 0,
                    $"{Path.GetRelativePath(root, file)} hard-codes Button chrome instead of using the default theme: {string.Join(", ", foundTerms)}.");
            }
        }
    }

    [Fact]
    public void EffectsSurfaceRemainsAbsentUntilDesignedEndToEnd()
    {
        System.Reflection.Assembly assembly = typeof(Cerneala.UI.Media.Transform).Assembly;

        Assert.Null(assembly.GetType("Cerneala.UI.Media.OpacityLayer"));
        Assert.Null(assembly.GetType("Cerneala.UI.Media.ShadowEffect"));
        Assert.Null(typeof(Cerneala.UI.Controls.Shapes.Shape).GetProperty("Shadow"));
        Assert.Null(typeof(Cerneala.UI.Controls.Shapes.Shape).GetField("ShadowProperty"));
    }

    private static void AssertNoForbiddenTermsInPreviewSources(string[] forbiddenTerms, bool removeStringLiterals = true)
    {
        string root = FindRepositoryRoot();
        foreach (string file in EnumeratePreviewSourceFiles())
        {
            string source = File.ReadAllText(file);
            string searchableText = removeStringLiterals
                ? RemoveCommentsAndStringLiterals(source)
                : RemoveComments(source);
            string[] foundTerms = forbiddenTerms
                .Where(term => searchableText.Contains(term, StringComparison.Ordinal))
                .ToArray();

            Assert.True(
                foundTerms.Length == 0,
                $"{Path.GetRelativePath(root, file)} references frozen Developer Preview scope: {string.Join(", ", foundTerms)}.");
        }
    }

    private static IEnumerable<string> EnumeratePreviewSourceFiles()
    {
        string root = FindRepositoryRoot();

        return EnumeratePlaygroundSampleFiles(root)
            .Concat(Directory.EnumerateFiles(Path.Combine(root, "tests", "Cerneala.Tests", "UI", "Hosting"), "*PreviewContractTests.cs", SearchOption.TopDirectoryOnly));
    }

    private static IEnumerable<string> EnumeratePlaygroundSampleFiles(string root)
    {
        return Directory.EnumerateFiles(Path.Combine(root, "Playground", "Cerneala.Playground"), "*.cs", SearchOption.TopDirectoryOnly)
            .Where(file => !file.EndsWith(".g.cs", StringComparison.OrdinalIgnoreCase));
    }

    private static string[] ExtractObjectInitializers(string source, string typeName)
    {
        List<string> initializers = [];
        string needle = "new " + typeName;
        int searchStart = 0;

        while (searchStart < source.Length)
        {
            int typeStart = source.IndexOf(needle, searchStart, StringComparison.Ordinal);
            if (typeStart < 0)
            {
                break;
            }

            int braceStart = source.IndexOf('{', typeStart + needle.Length);
            if (braceStart < 0)
            {
                break;
            }

            string betweenTypeAndBrace = source[(typeStart + needle.Length)..braceStart];
            if (!betweenTypeAndBrace.All(char.IsWhiteSpace))
            {
                searchStart = typeStart + needle.Length;
                continue;
            }

            int braceDepth = 0;
            for (int i = braceStart; i < source.Length; i++)
            {
                if (source[i] == '{')
                {
                    braceDepth++;
                }
                else if (source[i] == '}')
                {
                    braceDepth--;
                    if (braceDepth == 0)
                    {
                        initializers.Add(source[braceStart..(i + 1)]);
                        searchStart = i + 1;
                        break;
                    }
                }
            }

            if (braceDepth != 0)
            {
                break;
            }
        }

        return initializers.ToArray();
    }

    private static string[] ExtractTopLevelAssignedProperties(string initializer)
    {
        List<string> properties = [];
        int braceDepth = 0;
        int segmentStart = -1;

        for (int i = 0; i < initializer.Length; i++)
        {
            char current = initializer[i];
            if (current == '{')
            {
                braceDepth++;
                if (braceDepth == 1)
                {
                    segmentStart = i + 1;
                }

                continue;
            }

            if (current == '}')
            {
                if (braceDepth == 1 && segmentStart >= 0)
                {
                    AddTopLevelAssignedProperty(initializer, segmentStart, i, properties);
                    segmentStart = -1;
                }

                braceDepth--;
                continue;
            }

            if (current == ',' && braceDepth == 1 && segmentStart >= 0)
            {
                AddTopLevelAssignedProperty(initializer, segmentStart, i, properties);
                segmentStart = i + 1;
            }
        }

        return properties.ToArray();
    }

    private static void AddTopLevelAssignedProperty(string initializer, int start, int end, List<string> properties)
    {
        string segment = initializer[start..end].Trim();
        int equalsIndex = segment.IndexOf('=', StringComparison.Ordinal);
        if (equalsIndex <= 0)
        {
            return;
        }

        string candidate = segment[..equalsIndex].Trim();
        if (candidate.Length == 0 || candidate.Any(static character => !char.IsLetterOrDigit(character) && character != '_'))
        {
            return;
        }

        properties.Add(candidate);
    }

    private static string[] ExtractTargetTypedObjectInitializers(string source, string typeName)
    {
        List<string> initializers = [];
        string typeNeedle = typeName + " ";
        int searchStart = 0;

        while (searchStart < source.Length)
        {
            int typeStart = source.IndexOf(typeNeedle, searchStart, StringComparison.Ordinal);
            if (typeStart < 0)
            {
                break;
            }

            int initializerStart = source.IndexOf("= new()", typeStart + typeNeedle.Length, StringComparison.Ordinal);
            if (initializerStart < 0)
            {
                break;
            }

            int braceStart = source.IndexOf('{', initializerStart);
            if (braceStart < 0)
            {
                break;
            }

            int braceDepth = 0;
            for (int i = braceStart; i < source.Length; i++)
            {
                if (source[i] == '{')
                {
                    braceDepth++;
                }
                else if (source[i] == '}')
                {
                    braceDepth--;
                    if (braceDepth == 0)
                    {
                        initializers.Add(source[braceStart..(i + 1)]);
                        searchStart = i + 1;
                        break;
                    }
                }
            }

            if (braceDepth != 0)
            {
                break;
            }
        }

        return initializers.ToArray();
    }

    private static string FindRepositoryRoot()
    {
        DirectoryInfo? directory = new(AppContext.BaseDirectory);
        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "Cerneala.slnx")))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        throw new DirectoryNotFoundException("Could not find repository root.");
    }

    private static string RemoveCommentsAndStringLiterals(string text)
    {
        char[] searchableText = text.ToCharArray();

        for (int i = 0; i < searchableText.Length; i++)
        {
            if (searchableText[i] == '/' && i + 1 < searchableText.Length)
            {
                if (searchableText[i + 1] == '/')
                {
                    i = ClearUntilLineEnd(searchableText, i);
                    continue;
                }

                if (searchableText[i + 1] == '*')
                {
                    i = ClearBlockComment(searchableText, i);
                    continue;
                }
            }

            if (searchableText[i] == '@' && i + 1 < searchableText.Length && searchableText[i + 1] == '"')
            {
                i = ClearVerbatimStringLiteral(searchableText, i);
                continue;
            }

            if (searchableText[i] == '"' && CountQuoteRun(searchableText, i) >= 3)
            {
                i = ClearRawStringLiteral(searchableText, i, CountQuoteRun(searchableText, i));
                continue;
            }

            if (searchableText[i] == '"')
            {
                i = ClearStringLiteral(searchableText, i);
                continue;
            }

            if (searchableText[i] == '\'')
            {
                i = ClearCharacterLiteral(searchableText, i);
            }
        }

        return new string(searchableText);
    }

    private static string RemoveComments(string text)
    {
        char[] searchableText = text.ToCharArray();

        for (int i = 0; i < searchableText.Length; i++)
        {
            if (searchableText[i] == '/' && i + 1 < searchableText.Length)
            {
                if (searchableText[i + 1] == '/')
                {
                    i = ClearUntilLineEnd(searchableText, i);
                    continue;
                }

                if (searchableText[i + 1] == '*')
                {
                    i = ClearBlockComment(searchableText, i);
                }
            }
        }

        return new string(searchableText);
    }

    private static int ClearUntilLineEnd(char[] text, int start)
    {
        int i = start;
        for (; i < text.Length && text[i] is not '\r' and not '\n'; i++)
        {
            text[i] = ' ';
        }

        return i;
    }

    private static int ClearBlockComment(char[] text, int start)
    {
        for (int i = start; i < text.Length; i++)
        {
            if (i + 1 < text.Length && text[i] == '*' && text[i + 1] == '/')
            {
                text[i] = ' ';
                text[i + 1] = ' ';
                return i + 1;
            }

            ClearNonLineEnding(text, i);
        }

        return text.Length - 1;
    }

    private static int ClearStringLiteral(char[] text, int start)
    {
        for (int i = start; i < text.Length; i++)
        {
            bool escaped = i > start && text[i - 1] == '\\' && !IsEscapedEscape(text, i - 1, start);
            char current = text[i];
            ClearNonLineEnding(text, i);

            if (current == '"' && i > start && !escaped)
            {
                return i;
            }

            if (current is '\r' or '\n')
            {
                return i;
            }
        }

        return text.Length - 1;
    }

    private static int ClearVerbatimStringLiteral(char[] text, int start)
    {
        text[start] = ' ';
        text[start + 1] = ' ';

        for (int i = start + 2; i < text.Length; i++)
        {
            if (text[i] == '"' && i + 1 < text.Length && text[i + 1] == '"')
            {
                text[i] = ' ';
                text[i + 1] = ' ';
                i++;
                continue;
            }

            char current = text[i];
            ClearNonLineEnding(text, i);

            if (current == '"')
            {
                return i;
            }
        }

        return text.Length - 1;
    }

    private static int ClearRawStringLiteral(char[] text, int start, int quoteRunLength)
    {
        for (int i = start; i < text.Length; i++)
        {
            if (i > start && CountQuoteRun(text, i) >= quoteRunLength)
            {
                for (int j = 0; j < quoteRunLength; j++)
                {
                    text[i + j] = ' ';
                }

                return i + quoteRunLength - 1;
            }

            ClearNonLineEnding(text, i);
        }

        return text.Length - 1;
    }

    private static int CountQuoteRun(char[] text, int start)
    {
        int quoteCount = 0;
        for (int i = start; i < text.Length && text[i] == '"'; i++)
        {
            quoteCount++;
        }

        return quoteCount;
    }

    private static int ClearCharacterLiteral(char[] text, int start)
    {
        for (int i = start; i < text.Length; i++)
        {
            bool escaped = i > start && text[i - 1] == '\\' && !IsEscapedEscape(text, i - 1, start);
            char current = text[i];
            ClearNonLineEnding(text, i);

            if (current == '\'' && i > start && !escaped)
            {
                return i;
            }

            if (current is '\r' or '\n')
            {
                return i;
            }
        }

        return text.Length - 1;
    }

    private static bool IsEscapedEscape(char[] text, int slashIndex, int literalStart)
    {
        int slashCount = 0;
        for (int i = slashIndex; i > literalStart && text[i] == '\\'; i--)
        {
            slashCount++;
        }

        return slashCount % 2 == 0;
    }

    private static void ClearNonLineEnding(char[] text, int index)
    {
        if (text[index] is not '\r' and not '\n')
        {
            text[index] = ' ';
        }
    }
}
