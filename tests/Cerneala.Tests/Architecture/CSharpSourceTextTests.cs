namespace Cerneala.Tests.Architecture;

public sealed class CSharpSourceTextTests
{
    [Fact]
    public void EscapedQuoteDoesNotEndStringLiteral()
    {
        string scanned = Scan("var q = \"\\\"\"; ButtonBase b;\n");

        Assert.Contains("; ButtonBase b;", scanned, StringComparison.Ordinal);
    }

    [Fact]
    public void EscapedQuotesInsideStringStayLiteralText()
    {
        string scanned = Scan("var m = \"\\\"Thumb\\\" is invalid\"; ButtonBase b;\n");

        Assert.DoesNotContain("Thumb", scanned, StringComparison.Ordinal);
        Assert.DoesNotContain("invalid", scanned, StringComparison.Ordinal);
        Assert.Contains("; ButtonBase b;", scanned, StringComparison.Ordinal);
    }

    [Fact]
    public void EscapedQuoteDoesNotEndCharacterLiteral()
    {
        string scanned = Scan("var c = '\\''; Thumb t;\n");

        Assert.Contains("; Thumb t;", scanned, StringComparison.Ordinal);
    }

    [Fact]
    public void EscapedBackslashEndsStringAndCharacterLiterals()
    {
        string scanned = Scan("var s = \"Thumb\\\\\"; var c = '\\\\'; ButtonBase b;\n");

        Assert.DoesNotContain("Thumb", scanned, StringComparison.Ordinal);
        Assert.Contains("; var c =", scanned, StringComparison.Ordinal);
        Assert.Contains("; ButtonBase b;", scanned, StringComparison.Ordinal);
    }

    [Fact]
    public void InterpolationHoleCodeIsKept()
    {
        string scanned = Scan("var s = $\"{typeof(Thumb).Name}\";\n");

        Assert.Contains("typeof(Thumb).Name", scanned, StringComparison.Ordinal);
    }

    [Fact]
    public void InterpolatedStringTextAroundHolesIsBlanked()
    {
        string scanned = Scan("var s = $\"Thumb {count} \\\"ButtonBase\\\" {{Thumb}}\"; Cerneala.UI.Controls.Button b;\n");

        Assert.DoesNotContain("Thumb", scanned, StringComparison.Ordinal);
        Assert.DoesNotContain("ButtonBase", scanned, StringComparison.Ordinal);
        Assert.Contains("count", scanned, StringComparison.Ordinal);
        Assert.Contains("; Cerneala.UI.Controls.Button b;", scanned, StringComparison.Ordinal);
    }

    [Fact]
    public void InterpolationHoleKeepsCodeButBlanksNestedLiteralsAndFormatText()
    {
        string scanned = Scan("var s = $\"{Format(\"Thumb\", 'T')} {typeof(ButtonBase).Name,10:Thumb}\";\n");

        Assert.DoesNotContain("Thumb", scanned, StringComparison.Ordinal);
        Assert.Contains("Format(", scanned, StringComparison.Ordinal);
        Assert.Contains("typeof(ButtonBase).Name,10", scanned, StringComparison.Ordinal);
    }

    [Fact]
    public void VerbatimInterpolationHoleCodeIsKept()
    {
        string scanned = Scan(
            "var a = $@\"C:\\{typeof(Thumb).Name}\\ButtonBase\"\"\";\n" +
            "var b = @$\"C:\\{typeof(ButtonBase).Name}\\Thumb\";\n");

        Assert.Contains("typeof(Thumb).Name", scanned, StringComparison.Ordinal);
        Assert.Contains("typeof(ButtonBase).Name", scanned, StringComparison.Ordinal);
        Assert.DoesNotContain("\\ButtonBase", scanned, StringComparison.Ordinal);
        Assert.DoesNotContain("\\Thumb", scanned, StringComparison.Ordinal);
    }

    [Fact]
    public void RawInterpolationHoleCodeIsKept()
    {
        string scanned = Scan(
            "var a = $\"\"\"Thumb {typeof(ButtonBase).Name}\"\"\";\n" +
            "var b = $$\"\"\"\n" +
            "    {Thumb} {{typeof(Thumb).Name}} ButtonBase\n" +
            "    \"\"\";\n");

        Assert.Contains("typeof(ButtonBase).Name", scanned, StringComparison.Ordinal);
        Assert.Contains("typeof(Thumb).Name", scanned, StringComparison.Ordinal);
        Assert.DoesNotContain("Thumb ", scanned, StringComparison.Ordinal);
        Assert.DoesNotContain("{Thumb}", scanned, StringComparison.Ordinal);
        Assert.DoesNotContain(" ButtonBase", scanned, StringComparison.Ordinal);
    }

    [Fact]
    public void BlockCommentDoesNotEndAtItsOwnOpeningStar()
    {
        string scanned = Scan("/*/ Thumb */ ButtonBase b;\n");

        Assert.DoesNotContain("Thumb", scanned, StringComparison.Ordinal);
        Assert.Contains(" ButtonBase b;", scanned, StringComparison.Ordinal);
    }

    private static string Scan(string source)
    {
        string scanned = CSharpSourceText.RemoveCommentsAndStringLiterals(source);

        Assert.Equal(source.Length, scanned.Length);
        Assert.Equal(source.Count(character => character == '\n'), scanned.Count(character => character == '\n'));
        return scanned;
    }
}
