using System.Globalization;
using Cerneala.Language.Semantics.Symbols;
using Cerneala.Language.Syntax;

namespace Cerneala.Language.Semantics;

internal sealed partial class CernealaSemanticModel
{
    private static bool IsLegacyTileMapMember(ILanguageTypeSymbol? type, string name) =>
        type?.MetadataName == "Cerneala.UI.Controls.TileMap2D" && name is "Model" or "Layers";

    internal static bool IsLiveColliderOwner(ILanguageTypeSymbol? type) =>
        type?.MetadataName is "Cerneala.UI.Controls.Sprite2D" or "Cerneala.UI.Controls.TileInstance2D";

    internal static bool IsTileColliderAttribute(ILanguageTypeSymbol type, string name) =>
        name is "OffsetX" or "OffsetY" or "CollisionLayer" or "CollisionMask" or "IsTrigger" ||
        type.Name == "BoxCollider2D" && name is "Width" or "Height" ||
        type.Name == "CircleCollider2D" && name == "Radius" ||
        type.Name == "PolygonCollider2D" && name == "Points" ||
        type.Name == "SegmentCollider2D" && name is "EndX" or "EndY";

    private bool HasLiveColliderOwner(ElementSyntax element)
    {
        if (!parents.TryGetValue(element, out ElementSyntax? parent) || parent is null) { return false; }
        if (parent.Kind == SyntaxKind.PropertyElement)
        {
            if (parent.Name.Split('.').Last() != "Colliders" ||
                !parents.TryGetValue(parent, out parent) || parent is null) { return false; }
        }
        return IsLiveColliderOwner(GetElementType(parent, ReferenceEquals(parent, root)));
    }

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
        if (element.Children.OfType<TextSyntax>().Any(static text =>
            text.Kind != SyntaxKind.Comment && !string.IsNullOrWhiteSpace(text.Token.Text)))
        {
            AddShapeDiagnostic(element.Span, "Tile is immutable placement data and cannot contain text or directives.");
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

        foreach (ElementSyntax child in element.Children.OfType<ElementSyntax>())
        {
            BindTileColliderDeclaration(child);
        }
    }

    private void BindTileColliderDeclaration(ElementSyntax element)
    {
        ILanguageTypeSymbol? type = GetElementType(element, isRoot: false);
        if (type?.MetadataName is not ("Cerneala.UI.Controls.BoxCollider2D" or "Cerneala.UI.Controls.CircleCollider2D" or
            "Cerneala.UI.Controls.PolygonCollider2D" or "Cerneala.UI.Controls.SegmentCollider2D"))
        {
            AddShapeDiagnostic(element.NameToken.Span, "Tile content accepts BoxCollider2D, CircleCollider2D, PolygonCollider2D or SegmentCollider2D descriptors only.");
            return;
        }
        symbols.Add(new CernealaSemanticSymbol(CernealaSemanticSymbolKind.Element, element.Name,
            type.MetadataName, element.NameToken.Span, type));
        if (element.Children.Any(static child => child is ElementSyntax ||
            child is TextSyntax text && text.Kind != SyntaxKind.Comment && !string.IsNullOrWhiteSpace(text.Token.Text)))
        {
            AddShapeDiagnostic(element.Span, "A static Tile collider is immutable data, not a UI node; child content and directives are not supported.");
        }
        foreach (AttributeSyntax attribute in element.Attributes)
        {
            string name = attribute.NameToken.Text;
            if (name == "xmlns" || name.StartsWith("xmlns:", StringComparison.Ordinal)) { continue; }
            ILanguageMemberSymbol? member = FindProperty(type, name);
            if (!IsTileColliderAttribute(type, name) || member is null)
            {
                AddDiagnostic("CERNEALAUI003", attribute.NameToken.Span, type.Name, name);
                continue;
            }
            string value = Unquote(attribute.ValueToken.Text);
            bool valid = name switch
            {
                "IsTrigger" => bool.TryParse(value, out _),
                "CollisionLayer" or "CollisionMask" => uint.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out _),
                "Points" => !string.IsNullOrWhiteSpace(value) && value.IndexOf('$') < 0,
                _ => float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out float number) &&
                    !float.IsNaN(number) && !float.IsInfinity(number) && Math.Abs(number) <= 2_000_000_000f &&
                    (name is not ("Width" or "Height" or "Radius") || number > 0)
            };
            if (!valid) { AddDiagnostic("CERNEALAUI004", attribute.ValueToken.Span, type.Name, name, value); }
            symbols.Add(new CernealaSemanticSymbol(CernealaSemanticSymbolKind.Property, name,
                member.ValueTypeMetadataName, attribute.NameToken.Span, member.ValueType, member, value, isWritable: false));
        }
    }
}
