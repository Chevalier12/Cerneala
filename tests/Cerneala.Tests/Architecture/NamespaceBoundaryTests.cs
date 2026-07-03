namespace Cerneala.Tests.Architecture;

public sealed class NamespaceBoundaryTests
{
    private static readonly string[] ForbiddenBackendTerms =
    [
        "MonoGame",
        "Microsoft.Xna",
        "Skia",
        "SkiaSharp",
        "HarfBuzz",
        "SpriteBatch",
        "Texture2D",
        "Mouse.GetState",
        "Keyboard.GetState",
        "GamePad.GetState",
        "TouchPanel.GetState",
        "System.Windows",
        "Windows.Win32",
        "Microsoft.Win32"
    ];

    [Fact]
    public void RetainedUiCoreNamespacesDoNotReferenceBackendSpecificApis()
    {
        string root = FindRepositoryRoot();
        string uiRoot = Path.Combine(root, "UI");
        string[] allowedRoots =
        [
            Path.Combine(uiRoot, "Drawing"),
            Path.Combine(uiRoot, "Hosting", "MonoGame"),
            Path.Combine(uiRoot, "Input", "MonoGame"),
            Path.Combine(uiRoot, "Resources", "MonoGame")
        ];

        foreach (string file in EnumerateSourceFiles(uiRoot))
        {
            if (allowedRoots.Any(allowedRoot => IsUnder(file, allowedRoot)))
            {
                continue;
            }

            string[] foundTerms = FindForbiddenBackendTerms(File.ReadAllText(file));

            Assert.True(
                foundTerms.Length == 0,
                $"{Path.GetRelativePath(root, file)} references backend-specific APIs outside adapter or existing drawing/input foundation boundaries: {string.Join(", ", foundTerms)}.");
        }
    }

    [Fact]
    public void NamespaceBoundaryForbiddenTermListCoversCurrentBackendRisks()
    {
        string[] expectedTerms =
        [
            "Microsoft.Xna",
            "SkiaSharp",
            "HarfBuzz",
            "SpriteBatch",
            "Texture2D",
            "Mouse.GetState",
            "Keyboard.GetState",
            "TouchPanel.GetState"
        ];

        foreach (string term in expectedTerms)
        {
            Assert.Contains(term, ForbiddenBackendTerms, StringComparer.Ordinal);
        }
    }

    [Fact]
    public void BackendTermScanIgnoresCommentsAndStringLiterals()
    {
        const string source =
            "namespace Cerneala.UI.Controls;\n" +
            "\n" +
            "// MonoGame and Microsoft.Xna are backend examples, not references.\n" +
            "/* SkiaSharp is another documentation-only example. */\n" +
            "public sealed class DocumentationOnlyControl\n" +
            "{\n" +
            "    private const string BackendExample = \"SpriteBatch Texture2D\";\n" +
            "    private const string VerbatimBackendExample = @\"Mouse.GetState Keyboard.GetState\";\n" +
            "    private const string RawBackendExample =\n" +
            "        \"\"\"\n" +
            "        TouchPanel.GetState System.Windows\n" +
            "        \"\"\";\n" +
            "}\n";

        string[] foundTerms = FindForbiddenBackendTerms(source);

        Assert.Empty(foundTerms);
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

    private static IEnumerable<string> EnumerateSourceFiles(string root)
    {
        return Directory.EnumerateFiles(root, "*.cs", SearchOption.AllDirectories)
            .Where(file => !IsUnder(file, Path.Combine(root, "bin")))
            .Where(file => !IsUnder(file, Path.Combine(root, "obj")))
            .Where(file => !file.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}", StringComparison.OrdinalIgnoreCase))
            .Where(file => !file.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}", StringComparison.OrdinalIgnoreCase));
    }

    private static string[] FindForbiddenBackendTerms(string text)
    {
        string searchableText = RemoveCommentsAndStringLiterals(text);

        return ForbiddenBackendTerms
            .Where(term => searchableText.Contains(term, StringComparison.Ordinal))
            .ToArray();
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

    private static bool IsUnder(string file, string directory)
    {
        string relativePath = Path.GetRelativePath(directory, file);
        return relativePath != "." &&
            !relativePath.StartsWith("..", StringComparison.Ordinal) &&
            !Path.IsPathRooted(relativePath);
    }
}
