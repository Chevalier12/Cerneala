using System.Globalization;
using Cerneala.Language.Semantics.Symbols;
using Cerneala.Language.Syntax;

namespace Cerneala.Language.Semantics;

internal sealed partial class CernealaSemanticModel
{
    private readonly Dictionary<ElementSyntax, Dictionary<string, (float Width, float Height)>> tileImageSizes = new();

    internal static bool IsTileImageSizeAttribute(string name) => name is "ImageWidth" or "ImageHeight";

    internal static bool IsRemovedTileMapSourceMember(ILanguageTypeSymbol? type, string name) =>
        type?.MetadataName == "Cerneala.UI.Controls.TileMap2D" && name == "Source";

    // The owner of a body collider: Sprite2D.Collider, which is also the content of a Sprite2D.
    internal static bool IsLiveColliderOwner(ILanguageTypeSymbol? type) =>
        type?.MetadataName == "Cerneala.UI.Controls.Sprite2D";

    // The owner of a Vision slot: a Sprite2D or a Scene2D (also a scene component derived from it).
    internal static bool IsVisionOwner(ILanguageTypeSymbol? type) =>
        IsLiveColliderOwner(type) || type?.IsOrDerivesFrom("Cerneala.UI.Controls.Scene2D") == true;

    internal static bool IsConeCollider(ILanguageTypeSymbol? type) =>
        type?.MetadataName == "Cerneala.UI.Controls.ConeCollider2D";

    internal static bool IsTileColliderAttribute(ILanguageTypeSymbol type, string name) =>
        name is "OffsetX" or "OffsetY" or "CollisionLayer" or "CollisionMask" or "IsTrigger" ||
        type.Name == "BoxCollider2D" && name is "Width" or "Height" ||
        type.Name == "CircleCollider2D" && name == "Radius" ||
        type.Name == "PolygonCollider2D" && name == "Points" ||
        type.Name == "SegmentCollider2D" && name is "EndX" or "EndY";

    // The shape of a Vision property element itself: one per owner, with one cone inside and nothing else (no text,
    // no attribute other than a namespace declaration; the source generator rejects the same). What the cone is
    // is checked where the cone is bound (GetColliderPlacementError).
    private void ReportVisionPropertyElementShape(ElementSyntax element, ILanguageTypeSymbol? ownerType)
    {
        if (element.Name.Split('.').Last() != "Vision" || !IsVisionOwner(ownerType))
        {
            return;
        }

        bool hasText = element.Children.OfType<TextSyntax>().Any(static text =>
            text.Kind != SyntaxKind.Comment && !string.IsNullOrWhiteSpace(text.Token.Text));
        bool hasAttribute = element.Attributes.Any(static attribute =>
            attribute.NameToken.Text != "xmlns" && !attribute.NameToken.Text.StartsWith("xmlns:", StringComparison.Ordinal));
        if (!element.Children.OfType<ElementSyntax>().Any() || hasText || hasAttribute)
        {
            AddShapeDiagnostic(element.NameToken.Span, element.Name + " requires exactly one ConeCollider2D child.");
        }

        if (parents.TryGetValue(element, out ElementSyntax? owner) && owner is not null &&
            !ReferenceEquals(owner.Children.OfType<ElementSyntax>().First(sibling => sibling.Name == element.Name), element))
        {
            AddShapeDiagnostic(element.NameToken.Span, element.Name + " may be declared only once.");
        }
    }

    // The CERNEALAUI005 text for an element declared where it cannot live, or null when the place is valid.
    // A body collider is a direct child of a Sprite2D or of Sprite2D.Collider. A ConeCollider2D is a sensor, not a
    // body: it lives only in Sprite2D.Vision or Scene2D.Vision, and a Vision slot holds exactly one cone
    // (docs/plans/2026-10-25-scene2d-vision.md, V4).
    private string? GetColliderPlacementError(ElementSyntax element, ILanguageTypeSymbol type)
    {
        // Runs for every bound element, so leave early (a syntax-only check) unless this is a collider or
        // the direct child of a Vision property element.
        bool isCollider = type.IsOrDerivesFrom("Cerneala.UI.Controls.Collider2D");
        parents.TryGetValue(element, out ElementSyntax? parent);
        if (!isCollider && !(parent?.Kind == SyntaxKind.PropertyElement &&
            (parent.Name == "Vision" || parent.Name.EndsWith(".Vision", StringComparison.Ordinal))))
        {
            return null;
        }

        ElementSyntax? slot = null;
        ElementSyntax? owner = null;
        if (parent is not null)
        {
            owner = parent;
            if (parent.Kind == SyntaxKind.PropertyElement)
            {
                slot = parent;
                parents.TryGetValue(parent, out owner);
            }
        }

        ILanguageTypeSymbol? ownerType = owner is null ? null : GetElementType(owner, ReferenceEquals(owner, root));
        string? slotName = slot?.Name.Split('.').Last();
        if (slot is not null && slotName == "Vision" && IsVisionOwner(ownerType))
        {
            if (!IsConeCollider(type))
            {
                return slot.Name + " accepts a ConeCollider2D only.";
            }

            return ReferenceEquals(slot.Children.OfType<ElementSyntax>().First(), element)
                ? null
                : slot.Name + " accepts exactly one ConeCollider2D.";
        }

        if (IsConeCollider(type))
        {
            return "ConeCollider2D can only be a Vision; put it inside <Sprite2D.Vision> or <Scene2D.Vision>.";
        }

        bool bodySlot = (slot is null || slotName == "Collider") && IsLiveColliderOwner(ownerType);
        return isCollider && !bodySlot
            ? "Colliders can only be owned by Sprite2D or tiles; they cannot be scene roots or children of other UI elements."
            : null;
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

        Dictionary<string, (float Width, float Height)> imageSizes = GetTileImageSizes(parent);
        AttributeSyntax? imageAttribute = FindAttribute(element, "Image");
        if (imageAttribute is not null &&
            (FindAttribute(element, "Width") is null || FindAttribute(element, "Height") is null) &&
            !imageSizes.ContainsKey(Unquote(imageAttribute.ValueToken.Text)))
        {
            AddShapeDiagnostic(imageAttribute.ValueToken.Span,
                "Natural-size Tile dimensions require ImageWidth and ImageHeight metadata for that image in this TileMap2D.");
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
            bool imageSize = IsTileImageSizeAttribute(name);
            if (name is not ("Image" or "X" or "Y" or "Width" or "Height") && !imageSize)
            {
                AddDiagnostic("CERNEALAUI003", attribute.NameToken.Span, type.Name, name);
                continue;
            }
            // Intrinsic image metadata belongs to the source catalog. Reuse the
            // numeric type, not Tile.Width's CLR member/definition identity.
            ILanguageMemberSymbol? member = FindProperty(type, imageSize ? "Width" : name);
            if (member is null) { AddDiagnostic("CERNEALAUI003", attribute.NameToken.Span, type.Name, name); continue; }
            string value = Unquote(attribute.ValueToken.Text);
            if (name == "Image")
            {
                if (!TryBindDirectResourceReference(element, member, value, AttributeContentSpan(attribute), out _))
                {
                    AddDiagnostic("CERNEALAUI004", attribute.ValueToken.Span, type.Name, name, value);
                }
            }
            else if (!TryParseTileFloat(value, out float number) ||
                (name is "Width" or "Height") && number < 0 || imageSize && number <= 0)
            {
                AddDiagnostic("CERNEALAUI004", attribute.ValueToken.Span, type.Name, name, value);
            }
            symbols.Add(new CernealaSemanticSymbol(CernealaSemanticSymbolKind.Property, name,
                member.ValueTypeMetadataName, attribute.NameToken.Span, member.ValueType, imageSize ? null : member, value, isWritable: false));
        }

        ElementSyntax[] colliderDeclarations = element.Children.OfType<ElementSyntax>().ToArray();
        if (colliderDeclarations.Length > 1)
        {
            AddShapeDiagnostic(colliderDeclarations[1].NameToken.Span, "Tile accepts at most one collider.");
        }
        foreach (ElementSyntax child in colliderDeclarations)
        {
            BindTileColliderDeclaration(child);
        }
    }

    private Dictionary<string, (float Width, float Height)> GetTileImageSizes(ElementSyntax map)
    {
        if (tileImageSizes.TryGetValue(map, out var existing)) { return existing; }
        Dictionary<string, (float Width, float Height)> sizes = new(StringComparer.Ordinal);
        tileImageSizes.Add(map, sizes);
        foreach (ElementSyntax tile in map.Children.OfType<ElementSyntax>())
        {
            if (GetElementType(tile, isRoot: false)?.MetadataName != "Cerneala.UI.Controls.Tile") { continue; }
            AttributeSyntax? width = FindAttribute(tile, "ImageWidth");
            AttributeSyntax? height = FindAttribute(tile, "ImageHeight");
            if (width is null && height is null) { continue; }
            if (width is null || height is null)
            {
                AddShapeDiagnostic((width ?? height)!.NameToken.Span, "Tile image metadata requires both ImageWidth and ImageHeight.");
                continue;
            }
            if (!TryParseTileFloat(Unquote(width.ValueToken.Text), out float w) || w <= 0 ||
                !TryParseTileFloat(Unquote(height.ValueToken.Text), out float h) || h <= 0) { continue; }
            AttributeSyntax? image = FindAttribute(tile, "Image");
            if (image is null) { continue; }
            string reference = Unquote(image.ValueToken.Text);
            if (sizes.TryGetValue(reference, out var before) && before != (w, h))
            {
                AddShapeDiagnostic(image.ValueToken.Span, "Tile image metadata cannot declare conflicting dimensions for the same image in one TileMap2D.");
            }
            else { sizes[reference] = (w, h); }
        }
        return sizes;
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
                _ => TryParseTileFloat(value, out float number) &&
                    (name is not ("Width" or "Height" or "Radius") || number > 0)
            };
            if (!valid) { AddDiagnostic("CERNEALAUI004", attribute.ValueToken.Span, type.Name, name, value); }
            symbols.Add(new CernealaSemanticSymbol(CernealaSemanticSymbolKind.Property, name,
                member.ValueTypeMetadataName, attribute.NameToken.Span, member.ValueType, member, value, isWritable: false));
        }
    }

    private static bool TryParseTileFloat(string value, out float number) =>
        float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out number) &&
        !float.IsNaN(number) && !float.IsInfinity(number) && Math.Abs(number) <= 2_000_000_000f;
}
