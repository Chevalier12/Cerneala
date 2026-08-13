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
}
