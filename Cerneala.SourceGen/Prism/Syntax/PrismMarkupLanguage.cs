using System;
using System.Collections.Generic;

namespace Cerneala.SourceGen.Prism.Syntax;

internal static class PrismMarkupLanguage
{
    private static readonly string[] directives =
    [
        "@prism",
        "@parameter",
        "@layer",
        "@group",
        "@filter",
        "@style",
        "@mask",
        "@backdrop"
    ];

    internal static IReadOnlyList<string> DirectiveNames => directives;

    internal static bool IsDirective(string directive)
    {
        return Array.IndexOf(directives, directive) >= 0;
    }
}
