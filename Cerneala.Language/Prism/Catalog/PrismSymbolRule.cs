namespace Cerneala.Language.Prism.Catalog;

// A symbol property accepts only its catalog symbols, and BlendMode accepts
// PassThrough only on a group. The generator emits the symbol as a member of
// the property's enum, so both the editor and the source generator reject any
// other symbol, also one that a parameter default or argument supplies.
internal static class PrismSymbolRule
{
    // Returns the PRISM2009 message for a symbol that the named property of
    // the given family ("layer", "group", "filter", ...) cannot take, or null
    // when the symbol is valid.
    public static string? Validate(
        string propertyName,
        IReadOnlyList<string> symbols,
        string family,
        string symbol)
    {
        if (!symbols.Contains(symbol, StringComparer.Ordinal))
        {
            return "Unknown Prism symbol '" + symbol + "' for property '" + propertyName + "'.";
        }

        if (propertyName == "BlendMode" &&
            family != "group" &&
            string.Equals(symbol, "PassThrough", StringComparison.Ordinal))
        {
            return "PassThrough blending is valid only for Prism groups.";
        }

        return null;
    }
}
