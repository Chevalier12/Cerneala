using System;
using System.Linq;
using Cerneala.Language.Semantics;
using Cerneala.Language.Semantics.Symbols;
using Microsoft.CodeAnalysis;

namespace Cerneala.SourceGen;

public sealed partial class UiMarkupGenerator
{
    private sealed partial class GenerationScope
    {

        private enum MarkupValueKind
        {
            String,
            Bool,
            Float,
            Integer,
            Double,
            Decimal,
            NonNegativeFloat,
            PositiveFloat,
            Thickness,
            NonNegativeThickness,
            LayoutPoint,
            DrawPoint,
            DrawPointList,
            PathGeometry,
            Color,
            Brush,
            ImageResourceId,
            ImageReference,
            SpriteAnimationSet,
            ContentTemplate,
            Enum,
            Unsupported
        }

        private sealed class PropertySpec
        {
            private static readonly SymbolDisplayFormat NullableTypeDisplayFormat =
                SymbolDisplayFormat.FullyQualifiedFormat.WithMiscellaneousOptions(
                    SymbolDisplayFormat.FullyQualifiedFormat.MiscellaneousOptions |
                    SymbolDisplayMiscellaneousOptions.IncludeNullableReferenceTypeModifier);

            public PropertySpec(
                string name,
                MarkupValueKind valueKind,
                string propertyCode,
                ITypeSymbol valueType,
                bool assignable = true)
            {
                Name = name;
                ValueKind = valueKind;
                PropertyCode = propertyCode;
                ValueType = valueType;
                Assignable = assignable;
            }

            public string Name { get; }

            public MarkupValueKind ValueKind { get; }

            public string PropertyCode { get; }

            public bool IsUiProperty => PropertyCode.Length > 0;

            public ITypeSymbol ValueType { get; }

            public string ValueTypeCode => ValueType.ToDisplayString(NullableTypeDisplayFormat);

            public ITypeSymbol LiteralType => UnwrapNullable(ValueType);

            public bool Assignable { get; }
        }

        private INamedTypeSymbol? ResolveMarkupTypeReference(MarkupAttribute attribute) =>
            ResolveMarkupTypeReference(attribute.Value, attribute.Parent);

        private INamedTypeSymbol? ResolveMarkupTypeReference(string rawReference, MarkupElement? context)
        {
            string reference = rawReference.Trim();
            if (reference.StartsWith("global::", StringComparison.Ordinal))
            {
                reference = reference.Substring("global::".Length);
            }

            int prefixSeparator = reference.IndexOf(':');
            if (prefixSeparator < 0)
            {
                return compilation.GetTypeByMetadataName(reference);
            }

            if (prefixSeparator == 0 || prefixSeparator == reference.Length - 1)
            {
                return null;
            }

            string prefix = reference.Substring(0, prefixSeparator);
            string localName = reference.Substring(prefixSeparator + 1);
            MarkupNamespace? xmlNamespace = context?.GetNamespaceOfPrefix(prefix);
            const string clrNamespacePrefix = "clr-namespace:";
            if (xmlNamespace is null ||
                !xmlNamespace.NamespaceName.StartsWith(clrNamespacePrefix, StringComparison.Ordinal))
            {
                return null;
            }

            string declaration = xmlNamespace.NamespaceName.Substring(clrNamespacePrefix.Length);
            string[] segments = declaration.Split(';');
            string namespaceName = segments[0].Trim();
            string? assemblyName = null;
            for (int index = 1; index < segments.Length; index++)
            {
                string segment = segments[index].Trim();
                const string assemblyPrefix = "assembly=";
                if (segment.StartsWith(assemblyPrefix, StringComparison.Ordinal) && assemblyName is null)
                {
                    assemblyName = segment.Substring(assemblyPrefix.Length).Trim();
                    if (assemblyName.Length == 0)
                    {
                        return null;
                    }
                }
                else
                {
                    return null;
                }
            }

            string metadataName = namespaceName.Length == 0
                ? localName
                : namespaceName + "." + localName;
            INamedTypeSymbol? type = compilation.GetTypeByMetadataName(metadataName);
            return type is not null &&
                (assemblyName is null ||
                    string.Equals(type.ContainingAssembly.Name, assemblyName, StringComparison.Ordinal))
                ? type
                : null;
        }

        private string? ResolveElementType(MarkupElement element)
        {
            string elementName = element.Name.LocalName;
            CernealaSemanticSymbol? semanticSymbol = semanticModel.Symbols.FirstOrDefault(symbol =>
                symbol.Kind is CernealaSemanticSymbolKind.RootType or CernealaSemanticSymbolKind.Element &&
                symbol.Name.Split(':').Last() == elementName &&
                symbol.Span.Start >= element.Span.Start &&
                symbol.Span.End <= element.Span.End);
            INamedTypeSymbol? semanticType = semanticSymbol is null
                ? null
                : compilation.GetTypeByMetadataName(semanticSymbol.ValueType);
            if (semanticType is not null)
            {
                resolvedElementTypes[element.Name.Value] = semanticType;
                resolvedElementTypes[elementName] = semanticType;
                return semanticType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
            }

            INamedTypeSymbol? type = element.Name.Value.Contains(':')
                ? ResolveMarkupTypeReference(element.Name.Value, element)
                : ResolveBuiltInElementTypeSymbol(elementName);
            if (type is null)
            {
                return null;
            }

            resolvedElementTypes[element.Name.Value] = type;
            resolvedElementTypes[elementName] = type;
            return type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
        }

        private string? ResolveAspectTargetType(string targetName, MarkupElement? source = null)
        {
            return ResolveAspectTargetTypeSymbol(targetName, source)?
                .ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
        }

        private INamedTypeSymbol? ResolveAspectTargetTypeSymbol(string targetName, MarkupElement? source = null)
        {
            string reference = targetName.Trim();
            if (resolvedElementTypes.TryGetValue(reference, out INamedTypeSymbol? resolved))
            {
                return resolved;
            }

            INamedTypeSymbol? type = reference.Contains(':')
                ? ResolveMarkupTypeReference(reference, source)
                : ResolveBuiltInElementTypeSymbol(reference);

            INamedTypeSymbol? uiElementType = compilation.GetTypeByMetadataName("Cerneala.UI.Elements.UIElement");
            if (type is not null && type.TypeKind == TypeKind.Class &&
                uiElementType is not null && IsOrDerivesFrom(type, uiElementType))
            {
                resolvedElementTypes[reference] = type;
                resolvedElementTypes[type.Name] = type;
                return type;
            }

            return null;
        }

        private INamedTypeSymbol? ResolveBuiltInElementTypeSymbol(string elementName)
        {
            string metadataName = elementName.StartsWith("global::", StringComparison.Ordinal)
                ? elementName.Substring("global::".Length)
                : elementName;
            if (metadataName.StartsWith("Cerneala.UI.", StringComparison.Ordinal))
            {
                return compilation.GetTypeByMetadataName(metadataName);
            }

            if (metadataName.Contains('.'))
            {
                return null;
            }

            return compilation.GetTypeByMetadataName("Cerneala.UI.Controls." + metadataName) ??
                compilation.GetTypeByMetadataName("Cerneala.UI.Controls.Primitives." + metadataName) ??
                compilation.GetTypeByMetadataName("Cerneala.UI.Controls.Shapes." + metadataName) ??
                compilation.GetTypeByMetadataName("Cerneala.UI.Elements." + metadataName) ??
                compilation.GetTypeByMetadataName("Cerneala.UI.Layout.Panels." + metadataName) ??
                compilation.GetTypeByMetadataName("Cerneala.UI.Media." + metadataName) ??
                compilation.GetTypeByMetadataName("Cerneala.UI.Servo." + metadataName);
        }

        private INamedTypeSymbol? ResolveElementTypeSymbol(string elementName)
        {
            if (resolvedElementTypes.TryGetValue(elementName, out INamedTypeSymbol? resolved))
            {
                return resolved;
            }

            INamedTypeSymbol? type = ResolveBuiltInElementTypeSymbol(elementName);
            INamedTypeSymbol? uiElementType = compilation.GetTypeByMetadataName("Cerneala.UI.Elements.UIElement");
            INamedTypeSymbol? windowType = compilation.GetTypeByMetadataName("Cerneala.UI.Controls.Window");
            if (type is null || type.TypeKind != TypeKind.Class || type.IsAbstract ||
                uiElementType is null || !IsOrDerivesFrom(type, uiElementType) ||
                (windowType is not null && IsOrDerivesFrom(type, windowType)))
            {
                type = null;
            }

            if (type is not null)
            {
                resolvedElementTypes[elementName] = type;
            }

            return type;
        }

        private INamedTypeSymbol? ResolvePropertyOwnerType(string elementName, bool isRoot)
        {
            if (isRoot && string.Equals(document.Root.Name.LocalName, elementName, StringComparison.Ordinal))
            {
                if (userControlPair is not null)
                {
                    return userControlPair.TypeSymbol;
                }

                if (elementName is "Window" or "UserControl")
                {
                    return compilation.GetTypeByMetadataName("Cerneala.UI.Controls." + elementName);
                }
            }

            return ResolveElementTypeSymbol(elementName);
        }

        private PropertySpec? FindPropertySpec(string elementName, string propertyName)
        {
            return FindPropertySpec(elementName, propertyName, isRoot: false);
        }

        private PropertySpec? FindPropertySpec(string elementName, string propertyName, bool isRoot)
        {
            string cacheKey = (isRoot ? "root\0" : "element\0") + elementName + "\0" + propertyName;
            if (resolvedProperties.TryGetValue(cacheKey, out PropertySpec? resolved))
            {
                return resolved;
            }

            INamedTypeSymbol? elementType = ResolvePropertyOwnerType(elementName, isRoot);
            if (elementType is null)
            {
                return null;
            }

            resolved = FindPropertySpec(elementType, propertyName);
            if (resolved is not null)
            {
                resolvedProperties.Add(cacheKey, resolved);
            }

            return resolved;
        }

        private PropertySpec? FindPropertySpec(INamedTypeSymbol elementType, string propertyName)
        {
            INamedTypeSymbol? uiPropertyType = compilation.GetTypeByMetadataName("Cerneala.UI.Core.UiProperty`1");
            if (uiPropertyType is null)
            {
                return null;
            }

            IPropertySymbol? clrProperty = FindClrProperty(elementType, propertyName);
            IFieldSymbol? propertyField = FindUiPropertyField(elementType, propertyName + "Property", uiPropertyType);
            if (clrProperty is null || propertyField?.Type is not INamedTypeSymbol fieldType)
            {
                return null;
            }

            ITypeSymbol valueType = fieldType.TypeArguments[0];
            if (!SymbolEqualityComparer.Default.Equals(clrProperty.Type, valueType))
            {
                return null;
            }

            MarkupValueKind kind = GetMarkupValueKind(valueType, clrProperty);
            bool assignable = clrProperty.SetMethod is not null && IsAccessibleFromGeneratedCode(clrProperty.SetMethod);
            return new PropertySpec(
                propertyName,
                kind,
                propertyField.ContainingType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat) + "." + propertyField.Name,
                valueType,
                assignable);
        }

        private PropertySpec? FindClrPropertySpec(string elementName, string propertyName, bool isRoot)
        {
            INamedTypeSymbol? elementType = ResolvePropertyOwnerType(elementName, isRoot);
            IPropertySymbol? property = elementType is null ? null : FindClrProperty(elementType, propertyName);
            if (property?.SetMethod is null || !IsAccessibleFromGeneratedCode(property.SetMethod))
            {
                return null;
            }

            return new PropertySpec(
                propertyName,
                GetMarkupValueKind(property.Type, property),
                string.Empty,
                property.Type);
        }

        private IPropertySymbol? FindClrProperty(INamedTypeSymbol elementType, string propertyName)
        {
            for (INamedTypeSymbol? current = elementType; current is not null; current = current.BaseType)
            {
                IPropertySymbol? property = current.GetMembers(propertyName)
                    .OfType<IPropertySymbol>()
                    .FirstOrDefault(candidate => !candidate.IsStatic && candidate.GetMethod is not null &&
                        IsAccessibleFromGeneratedCode(candidate.GetMethod));
                if (property is not null)
                {
                    return property;
                }
            }

            return null;
        }

        private IFieldSymbol? FindUiPropertyField(INamedTypeSymbol elementType, string fieldName, INamedTypeSymbol uiPropertyType)
        {
            for (INamedTypeSymbol? current = elementType; current is not null; current = current.BaseType)
            {
                IFieldSymbol? field = current.GetMembers(fieldName)
                    .OfType<IFieldSymbol>()
                    .FirstOrDefault(candidate => candidate.IsStatic && candidate.Type is INamedTypeSymbol fieldType &&
                        SymbolEqualityComparer.Default.Equals(fieldType.OriginalDefinition, uiPropertyType) &&
                        IsAccessibleFromGeneratedCode(candidate));
                if (field is not null)
                {
                    return field;
                }
            }

            return null;
        }

        private bool IsAccessibleFromGeneratedCode(ISymbol symbol)
        {
            if (userControlPair is not null)
            {
                return compilation.IsSymbolAccessibleWithin(symbol, userControlPair.TypeSymbol);
            }

            if (symbol.DeclaredAccessibility == Accessibility.Public)
            {
                return true;
            }

            bool sameAssembly = SymbolEqualityComparer.Default.Equals(symbol.ContainingAssembly, compilation.Assembly);
            return sameAssembly && symbol.DeclaredAccessibility is Accessibility.Internal or Accessibility.ProtectedOrInternal;
        }

        private static MarkupValueKind GetMarkupValueKind(ITypeSymbol valueType, IPropertySymbol property)
        {
            valueType = UnwrapNullable(valueType);
            string constraint = property.GetAttributes()
                .FirstOrDefault(attribute => attribute.AttributeClass?.ToDisplayString() == "Cerneala.UI.Markup.MarkupValueConstraintAttribute")?
                .ConstructorArguments.FirstOrDefault().Value?.ToString() ?? string.Empty;
            string typeName = valueType.ToDisplayString();
            if (typeName == "Cerneala.UI.Layout.Thickness")
            {
                return constraint == "1" ? MarkupValueKind.NonNegativeThickness : MarkupValueKind.Thickness;
            }

            if (typeName == "Cerneala.UI.Layout.LayoutPoint")
            {
                return MarkupValueKind.LayoutPoint;
            }

            if (typeName == "Cerneala.Drawing.DrawPoint")
            {
                return MarkupValueKind.DrawPoint;
            }

            if (typeName == "System.Collections.Generic.IReadOnlyList<Cerneala.Drawing.DrawPoint>")
            {
                return MarkupValueKind.DrawPointList;
            }

            if (valueType.Name == "PathGeometry" &&
                valueType.ContainingNamespace.ToDisplayString() == "Cerneala.UI.Media")
            {
                return MarkupValueKind.PathGeometry;
            }

            if (typeName == "Cerneala.Drawing.Color")
            {
                return MarkupValueKind.Color;
            }

            if (valueType.Name == "Brush" && valueType.ContainingNamespace.ToDisplayString() == "Cerneala.UI.Media")
            {
                return MarkupValueKind.Brush;
            }

            if (valueType.Name == "ContentTemplate" &&
                valueType.ContainingNamespace.ToDisplayString() == "Cerneala.UI.Controls.Templates")
            {
                return MarkupValueKind.ContentTemplate;
            }

            if (valueType is INamedTypeSymbol namedType &&
                namedType.IsGenericType &&
                namedType.OriginalDefinition.ToDisplayString() == "Cerneala.UI.Resources.ResourceId<T>" &&
                namedType.TypeArguments.Length == 1 &&
                namedType.TypeArguments[0].ToDisplayString() == "Cerneala.UI.Resources.ImageResource")
            {
                return MarkupValueKind.ImageResourceId;
            }

            if (valueType.Name == "ImageReference" &&
                valueType.ContainingNamespace.ToDisplayString() == "Cerneala.UI.Resources")
            {
                return MarkupValueKind.ImageReference;
            }

            if (valueType.Name == "SpriteAnimationSet" &&
                valueType.ContainingNamespace.ToDisplayString() == "Cerneala.UI.Controls")
            {
                return MarkupValueKind.SpriteAnimationSet;
            }

            if (valueType.TypeKind == TypeKind.Enum)
            {
                return MarkupValueKind.Enum;
            }

            return valueType.SpecialType switch
            {
                SpecialType.System_String or SpecialType.System_Object => MarkupValueKind.String,
                SpecialType.System_Boolean => MarkupValueKind.Bool,
                SpecialType.System_Single when constraint == "1" => MarkupValueKind.NonNegativeFloat,
                SpecialType.System_Single when constraint == "2" => MarkupValueKind.PositiveFloat,
                SpecialType.System_Single => MarkupValueKind.Float,
                SpecialType.System_Double => MarkupValueKind.Double,
                SpecialType.System_Decimal => MarkupValueKind.Decimal,
                SpecialType.System_Byte or SpecialType.System_SByte or SpecialType.System_Int16 or
                    SpecialType.System_UInt16 or SpecialType.System_Int32 or SpecialType.System_UInt32 or
                    SpecialType.System_Int64 or SpecialType.System_UInt64 => MarkupValueKind.Integer,
                _ => MarkupValueKind.Unsupported
            };
        }
    }
}
