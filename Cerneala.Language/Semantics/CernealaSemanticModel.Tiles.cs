using System.Globalization;
using Cerneala.Language.Semantics.Symbols;
using Cerneala.Language.Syntax;

namespace Cerneala.Language.Semantics;

internal sealed partial class CernealaSemanticModel
{
    private static bool IsLegacyTileMapMember(ILanguageTypeSymbol? type, string name) =>
        type?.MetadataName == "Cerneala.UI.Controls.TileMap2D" && name is "Model" or "Layers";

    private void BindTileDeclaration(ElementSyntax element, ILanguageTypeSymbol type)
    {
        if (!parents.TryGetValue(element, out ElementSyntax? parent) || parent is null ||
            parent.Kind == SyntaxKind.PropertyElement ||
            GetElementType(parent, ReferenceEquals(parent, root))?.MetadataName != "Cerneala.UI.Controls.TileMap2D")
        {
            AddShapeDiagnostic(element.NameToken.Span, "Tile must be a direct child of TileMap2D.");
            return;
        }

        if (parent.Attributes.Any(static attribute => attribute.NameToken.Text == "Model") ||
            parent.Children.OfType<ElementSyntax>().Any(child =>
                GetElementType(child, isRoot: false)?.MetadataName == "Cerneala.UI.Controls.TileLayer2D"))
        {
            AddShapeDiagnostic(element.NameToken.Span, "Free Tile placements cannot be combined with a Model binding or imported layer presentations in one TileMap2D.");
            return;
        }

        symbols.Add(new CernealaSemanticSymbol(CernealaSemanticSymbolKind.Element, element.Name,
            type.MetadataName, element.NameToken.Span, type));
        if (!element.Attributes.Any(static attribute => attribute.NameToken.Text == "Image"))
        {
            AddShapeDiagnostic(element.NameToken.Span, "Tile requires Image=\"$ImageResource\".");
        }
        if (element.Children.Any(static child => child is ElementSyntax ||
            child is TextSyntax text && text.Kind != SyntaxKind.Comment && !string.IsNullOrWhiteSpace(text.Token.Text)))
        {
            AddShapeDiagnostic(element.Span, "Tile is immutable placement data and cannot contain child content or directives.");
        }

        foreach (AttributeSyntax attribute in element.Attributes)
        {
            string name = attribute.NameToken.Text;
            if (name == "xmlns" || name.StartsWith("xmlns:", StringComparison.Ordinal)) { continue; }
            if (name is not ("Image" or "X" or "Y" or "Width" or "Height"))
            {
                AddDiagnostic("CERNEALAUI003", attribute.NameToken.Span, type.Name, name);
                continue;
            }
            ILanguageMemberSymbol? member = FindProperty(type, name);
            if (member is null) { AddDiagnostic("CERNEALAUI003", attribute.NameToken.Span, type.Name, name); continue; }
            string value = Unquote(attribute.ValueToken.Text);
            if (name == "Image")
            {
                if (!TryBindDirectResourceReference(element, member, value, AttributeContentSpan(attribute), out _))
                {
                    AddDiagnostic("CERNEALAUI004", attribute.ValueToken.Span, type.Name, name, value);
                }
            }
            else if (!float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out float number) ||
                float.IsNaN(number) || float.IsInfinity(number) || Math.Abs(number) > 2_000_000_000f ||
                (name is "Width" or "Height") && number < 0)
            {
                AddDiagnostic("CERNEALAUI004", attribute.ValueToken.Span, type.Name, name, value);
            }
            symbols.Add(new CernealaSemanticSymbol(CernealaSemanticSymbolKind.Property, name,
                member.ValueTypeMetadataName, attribute.NameToken.Span, member.ValueType, member, value, isWritable: false));
        }
    }
}
