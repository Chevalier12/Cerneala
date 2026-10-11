namespace Cerneala.Language.Prism.Catalog;

// ThisLayerRange and UnderlyingRange become PrismBlendRange(blackStart,
// blackEnd, whiteStart, whiteEnd). Unlike other vectors, a shorter value has
// no valid zero padding, so both the editor and the source generator reject it.
internal static class PrismBlendRangeRule
{
    // Returns the PRISM2009 message for a vector value that cannot become a
    // PrismBlendRange of the named property, or null when the value is valid.
    public static string? Validate(string propertyName, string vectorText)
    {
        if (propertyName is not ("ThisLayerRange" or "UnderlyingRange") ||
            vectorText.Split(',').Length == 4)
        {
            return null;
        }

        return "Prism property '" + propertyName + "' value '" + vectorText + "' must have four components.";
    }
}
