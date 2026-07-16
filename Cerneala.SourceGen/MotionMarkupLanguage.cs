using System;
using System.Collections.Generic;

namespace Cerneala.SourceGen;

internal static class MotionMarkupLanguage
{
    private static readonly string[] directives =
    [
        "@when",
        "@if",
        "@on",
        "@presence",
        "@layout",
        "@scroll",
        "@drag",
        "@gesture",
        "@animate",
        "@keyframes",
        "@stagger",
        "@parallel",
        "@sequence",
        "@run",
        "@cancel",
        "@handle",
        "@parameter",
        "@from",
        "@to"
    ];

    internal static IReadOnlyList<string> DirectiveNames => directives;

    internal static bool IsDirective(string directive)
    {
        return Array.IndexOf(directives, directive) >= 0;
    }
}
