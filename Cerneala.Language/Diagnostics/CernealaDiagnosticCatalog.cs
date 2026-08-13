namespace Cerneala.Language.Diagnostics;

internal static class CernealaDiagnosticCatalog
{
    private const string UiCategory = "Cerneala.UiMarkup";
    private const string MotionCategory = "Cerneala.UiMarkup.Motion";
    private const string PrismCategory = "Cerneala.Prism.Markup";

    private static readonly LanguageDiagnosticDescriptor[] descriptors =
    [
        UiTransient("CERNEALAUI001", "Malformed UI markup", "Markup file '{0}' could not be parsed: {1}"),
        Ui("CERNEALAUI002", "Unsupported UI markup element", "Markup element '{0}' is not supported by the source generator"),
        Ui("CERNEALAUI003", "Unsupported UI markup property", "Markup property '{0}.{1}' is not supported by the source generator"),
        Ui("CERNEALAUI004", "Invalid UI markup property value", "Markup property '{0}.{1}' has invalid value '{2}'"),
        Ui("CERNEALAUI005", "Invalid UI markup document shape", "Markup file '{0}' has invalid document shape: {1}"),
        Ui("CERNEALAUI006", "Invalid UI markup directive", "Markup directive in '{0}' is invalid: {1}"),
        Ui("CERNEALAUI007", "Invalid UI markup binding source", "Markup binding source '{0}' is invalid: {1}"),
        Ui("CERNEALAUI008", "Invalid UserControl declaration", "UserControl markup file '{0}' is invalid: {1}"),
        Ui("CERNEALAUI009", "Invalid markup event handler", "Event handler '{0}' for '{1}.{2}' is invalid: {3}"),
        Ui("CERNEALAUI010", "Invalid Window declaration", "Window markup file '{0}' is invalid: {1}"),
        Ui("CERNEALAUI011", "Invalid Window application startup", "Generated Window startup is invalid: {0}"),
        Ui("CERNEALAUI012", "Invalid component template declaration", "Component template in '{0}' is invalid: {1}"),
        Ui("CERNEALAUI013", "Invalid Application declaration", "Application markup file '{0}' is invalid: {1}"),
        Ui("CERNEALAUI014", "Invalid Application startup", "Application startup in '{0}' is invalid: {1}"),
        Motion("CERNEALAUI020", "Invalid Motion markup syntax", "Motion syntax in '{0}' is invalid: {1}"),
        Motion("CERNEALAUI021", "Invalid Motion target", "Motion target resolution in '{0}' failed: {1}"),
        Motion("CERNEALAUI022", "Invalid Motion event", "Motion event resolution in '{0}' failed: {1}"),
        Motion("CERNEALAUI023", "Invalid Motion property or spec type", "Motion property/spec typing in '{0}' failed: {1}"),
        Motion("CERNEALAUI024", "Invalid Motion composition", "Motion composition in '{0}' is invalid: {1}"),
        Motion("CERNEALAUI025", "Invalid Motion lifecycle directive", "Motion lifecycle directive in '{0}' is invalid: {1}"),
        Motion("CERNEALAUI026", "Unsupported Motion runtime capability", "Motion runtime capability in '{0}' is unsupported: {1}"),
        Prism("PRISM1001", "Unknown Prism directive", "Prism markup in '{0}' is invalid: {1}"),
        PrismTransient("PRISM1002", "Missing Prism delimiter", "Prism markup in '{0}' is invalid: {1}"),
        Prism("PRISM1003", "Invalid Prism syntax", "Prism markup in '{0}' is invalid: {1}"),
        PrismBinding("PRISM2001", "Unknown Prism property"),
        PrismBinding("PRISM2002", "Unknown Prism symbol"),
        PrismBinding("PRISM2003", "Duplicate Prism name"),
        PrismBinding("PRISM2004", "Invalid Prism parameter"),
        PrismBinding("PRISM2005", "Invalid Prism nesting"),
        PrismBinding("PRISM2006", "Multiple Prism backdrops"),
        PrismBinding("PRISM2007", "Invalid Prism backdrop order"),
        PrismBinding("PRISM2008", "Invalid Prism clipping base"),
        PrismBinding("PRISM2009", "Invalid Prism value"),
        PrismBinding("PRISM2010", "Invalid Prism Motion target"),
        PrismBinding("PRISM2011", "Unknown Prism Motion node"),
        PrismBinding("PRISM2012", "Unknown Prism Motion property"),
        PrismBinding("PRISM2013", "Invalid Prism structure")
    ];

    private static readonly IReadOnlyDictionary<string, LanguageDiagnosticDescriptor> byId =
        descriptors.ToDictionary(descriptor => descriptor.Id, StringComparer.Ordinal);

    public static IReadOnlyList<LanguageDiagnosticDescriptor> All => descriptors;

    public static LanguageDiagnosticDescriptor Get(string id) =>
        byId.TryGetValue(id, out LanguageDiagnosticDescriptor? descriptor)
            ? descriptor
            : throw new KeyNotFoundException("Unknown Cerneala diagnostic id '" + id + "'.");

    private static LanguageDiagnosticDescriptor Ui(string id, string title, string message) =>
        new(id, title, message, UiCategory, LanguageDiagnosticSeverity.Error, LanguageDiagnosticSeverity.Error);

    private static LanguageDiagnosticDescriptor Motion(string id, string title, string message) =>
        new(id, title, message, MotionCategory, LanguageDiagnosticSeverity.Error, LanguageDiagnosticSeverity.Error);

    private static LanguageDiagnosticDescriptor UiTransient(string id, string title, string message) =>
        new(id, title, message, UiCategory, LanguageDiagnosticSeverity.Error, LanguageDiagnosticSeverity.Information);

    private static LanguageDiagnosticDescriptor Prism(string id, string title, string message) =>
        new(id, title, message, PrismCategory, LanguageDiagnosticSeverity.Error, LanguageDiagnosticSeverity.Error);

    private static LanguageDiagnosticDescriptor PrismTransient(string id, string title, string message) =>
        new(id, title, message, PrismCategory, LanguageDiagnosticSeverity.Error, LanguageDiagnosticSeverity.Information);

    private static LanguageDiagnosticDescriptor PrismBinding(string id, string title) =>
        new(id, title, "Prism binding in '{0}' failed: {1}", "Cerneala.Prism.Binding", LanguageDiagnosticSeverity.Error, LanguageDiagnosticSeverity.Error);
}
