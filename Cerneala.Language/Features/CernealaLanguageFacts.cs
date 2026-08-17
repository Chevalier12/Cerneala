using Cerneala.Language.Prism.Catalog;

namespace Cerneala.Language.Features;

internal sealed class LanguageArgumentFact
{
    public LanguageArgumentFact(
        string name,
        string valueType,
        bool required,
        IReadOnlyList<string>? allowedValues = null)
    {
        Name = name;
        ValueType = valueType;
        Required = required;
        AllowedValues = allowedValues ?? Array.Empty<string>();
    }

    public string Name { get; }

    public string ValueType { get; }

    public bool Required { get; }

    public IReadOnlyList<string> AllowedValues { get; }
}

internal static class CernealaLanguageFacts
{
    private static readonly Lazy<PrismLanguageCatalog> prismCatalog =
        new(PrismLanguageCatalog.LoadDefault);

    private static readonly IReadOnlyDictionary<string, LanguageArgumentFact[]> MotionCallArguments =
        new Dictionary<string, LanguageArgumentFact[]>(StringComparer.Ordinal)
        {
            ["Tween"] =
            [
                new("duration", "duration", required: true, ["100ms", "250ms", "1s"]),
                new("easing", "easing", required: false,
                    ["Linear", "Standard", "Emphasized", "EaseIn", "EaseOut", "EaseInOut", "Sharp"])
            ],
            ["Spring"] =
            [
                new("stiffness", "number", required: true),
                new("damping", "number", required: false),
                new("mass", "number", required: false)
            ],
            ["Repeat"] =
            [
                new("spec", "motion spec", required: true),
                new("count", "positive count or forever", required: true, ["forever"])
            ],
            ["PingPong"] =
            [
                new("spec", "motion spec", required: true),
                new("count", "positive count or forever", required: true, ["forever"])
            ],
            ["Step"] =
            [
                new("count", "positive count", required: true),
                new("position", "step position", required: false,
                    ["JumpStart", "JumpEnd", "JumpBoth", "JumpNone"])
            ]
        };

    public static IReadOnlyList<string> MotionDirectiveKeywords { get; } =
    [
        "@when", "@if", "@on", "@presence", "@layout", "@scroll", "@drag", "@gesture",
        "@set", "@animate", "@keyframes", "@stagger", "@parallel", "@sequence", "@run",
        "@cancel", "@handle", "@parameter", "@from", "@to"
    ];

    public static IReadOnlyList<string> PrismDirectiveKeywords { get; } =
    [
        "@prism", "@parameter", "@layer", "@group", "@filter", "@style", "@mask", "@backdrop"
    ];

    public static IReadOnlyList<string> MotionSpecKinds { get; } =
        ["Tween", "Spring", "Repeat", "PingPong"];

    public static IReadOnlyList<LanguageArgumentFact> MotionOptions { get; } =
    [
        new("retarget", "Cerneala.UI.Motion.Core.RetargetBehavior", required: false, ["Restart", "PreserveProgress"]),
        new("holdOnComplete", "System.Boolean", required: false, ["true", "false"]),
        new("debugName", "System.String", required: false)
    ];

    public static IReadOnlyList<LanguageArgumentFact> FindMotionCallArguments(string functionName) =>
        MotionCallArguments.TryGetValue(functionName, out LanguageArgumentFact[]? arguments)
            ? arguments
            : Array.Empty<LanguageArgumentFact>();

    public static IReadOnlyList<string> GetPrismSymbols(string kind) => prismCatalog.Value.Symbols
        .Where(symbol => string.Equals(symbol.Kind, kind, StringComparison.Ordinal))
        .Select(symbol => symbol.Symbol)
        .OrderBy(symbol => symbol, StringComparer.Ordinal)
        .ToArray();

    public static IReadOnlyList<LanguageArgumentFact> GetPrismProperties(string familyOrKind, string? symbol = null)
    {
        IReadOnlyList<PrismCatalogProperty> properties = symbol is null
            ? prismCatalog.Value.GetCommonProperties(familyOrKind)
            : prismCatalog.Value.FindSymbol(familyOrKind, symbol)?.Properties ?? Array.Empty<PrismCatalogProperty>();
        return properties.Select(property => new LanguageArgumentFact(
                property.Name,
                property.ValueType,
                property.Required && property.DefaultValue is null,
                property.Symbols))
            .ToArray();
    }

    public static IReadOnlyList<LanguageArgumentFact> FindPrismProperties(string symbol)
    {
        PrismCatalogSymbol? match = prismCatalog.Value.Symbols.FirstOrDefault(candidate =>
            string.Equals(candidate.Symbol, symbol, StringComparison.Ordinal));
        return match is null ? Array.Empty<LanguageArgumentFact>() : GetPrismProperties(match.Kind, match.Symbol);
    }

    public static string? FindPrismKind(string symbol) => prismCatalog.Value.Symbols
        .FirstOrDefault(candidate => string.Equals(candidate.Symbol, symbol, StringComparison.Ordinal))?
        .Kind;
}
