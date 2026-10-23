using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using Microsoft.CodeAnalysis;

namespace Cerneala.SourceGen;

public sealed partial class UiMarkupGenerator
{
    private sealed partial class GenerationScope
    {

        private sealed class NamedElementReference
        {
            public NamedElementReference(string code, MarkupElement element)
            {
                Code = code;
                Element = element;
            }

            public string Code { get; }

            public MarkupElement Element { get; }
        }

        public string EmitElement(MarkupElement element, bool initializeComponentRoot = false, bool allowInline = false)
        {
            string? requestedName = element.Attribute("Name")?.Value;
            string variable;
            TemplateEmissionContext? templateContext = templateEmissionContexts.Count == 0
                ? null
                : templateEmissionContexts.Peek();
            if (initializeComponentRoot)
            {
                variable = "this";
            }
            else if (string.IsNullOrWhiteSpace(requestedName) || templateContext?.RegisterParts == true)
            {
                variable = "element" + nextId.ToString(CultureInfo.InvariantCulture);
                nextId++;
            }
            else
            {
                string symbolName = requestedName!.Trim();
                variable = CreateIdentifier(symbolName);
                string referenceCode = userControlPair is null ? variable : "this." + variable;
                if (!AddSymbol(symbolName, NamedSymbolKind.Element, new NamedElementReference(referenceCode, element), element))
                {
                    variable = "element" + nextId.ToString(CultureInfo.InvariantCulture);
                    nextId++;
                }
            }

            if (ReferenceEquals(element, document.Root) && templateContext is null)
            {
                documentRootVariable = userControlPair is null ? variable : "this";
            }

            string? typeName = ResolveElementType(element);

            if (typeName is null)
            {
                Report(UnsupportedElement, element, element.Name.LocalName);
                return variable;
            }

            INamedTypeSymbol? elementType = GetElementTypeSymbol(element);
            bool isInline = elementType is not null && IsInlineType(elementType);
            if (isInline && !allowInline)
            {
                // An inline is created only as a child of a TextBlock or a Span; anywhere else (a template root, the
                // body of a directive) it cannot be used as a UIElement.
                Report(InvalidDirective, element, Path.GetFileName(file.Path), InlineDirectiveContentMessage);
                return variable;
            }

            ValidateStaticAnimationState(element);

            DirectiveParseResult parsedContent = GetDirectiveContent(
                element,
                DirectiveContentKind.Elements |
                DirectiveContentKind.Templates);
            bool isTileMap = ResolveElementTypeSymbol(element.Name.LocalName)?.ToDisplayString() == "Cerneala.UI.Controls.TileMap2D";
            string? tileMapInitializer = isTileMap ? BuildTileMapInitializer(element, parsedContent) : null;
            if (!initializeComponentRoot)
            {
                currentLines.Add(typeName + " " + variable + " = " + (tileMapInitializer ?? "new()") + ";");
            }
            EmitRuntimeResources(element, variable);
            if (!string.IsNullOrWhiteSpace(requestedName) && templateContext?.RegisterParts == true)
            {
                string partName = requestedName!.Trim();
                if (IsReservedTemplateReference(partName))
                {
                    Report(
                        InvalidComponentTemplate,
                        (MarkupObject?)element.Attribute("Name") ?? element,
                        Path.GetFileName(file.Path),
                        "Template part Name '" + partName + "' is reserved.");
                }
                else if (!templateContext.PartNames.Add(partName))
                {
                    Report(
                        InvalidComponentTemplate,
                        (MarkupObject?)element.Attribute("Name") ?? element,
                        Path.GetFileName(file.Path),
                        "Duplicate template part Name '" + partName + "'.");
                }
                else if (!ValidateDeclaredTemplatePartType(templateContext, partName, element))
                {
                    templateContext.PartNames.Remove(partName);
                }
                else
                {
                    templateContext.Parts.Add(partName, element);
                    currentLines.Add(templateContext.ContextVariable + ".RequirePart(" + Literal(partName) + ", " + variable + ");");
                }
            }
            else if (!string.IsNullOrWhiteSpace(requestedName) && userControlPair is not null)
            {
                RegisterNamedElement(requestedName!.Trim(), variable, typeName, element);
            }
            if (ReportPrismSyntaxDiagnostics(parsedContent))
            {
                return variable;
            }

            if (parsedContent.Error is not null)
            {
                Report(InvalidDirective, parsedContent.ErrorSource ?? element, Path.GetFileName(file.Path), parsedContent.Error);
                return variable;
            }

            if (isInline && element.Attribute("Aspect") is MarkupAttribute inlineAspect)
            {
                // An Aspect targets a UIElement.
                Report(UnsupportedProperty, inlineAspect, element.Name.LocalName, "Aspect");
                return variable;
            }

            IReadOnlyList<AspectResource> aspects = isInline ? [] : ResolveAspects(element);
            DirectiveTemplateNode[] templates = parsedContent.Nodes.OfType<DirectiveTemplateNode>().ToArray();
            DirectiveTemplatesNode[] templateCollections = parsedContent.Nodes.OfType<DirectiveTemplatesNode>().ToArray();
            if (templates.Length > 1)
            {
                Report(
                    InvalidComponentTemplate,
                    templates[1].Source,
                    Path.GetFileName(file.Path),
                    "An element may declare only one @template block.");
            }

            if (templateCollections.Length > 1)
            {
                Report(
                    InvalidDocumentShape,
                    templateCollections[1].Source,
                    Path.GetFileName(file.Path),
                    "An element may declare only one @templates block.");
            }

            MarkupAttribute[] propertyAttributes = element.Attributes()
                .Where(attribute => !attribute.IsNamespaceDeclaration &&
                    attribute.Name.LocalName is not "Aspect" and not "Name" and not "DataType")
                .ToArray();
            MarkupAttribute? dataContextAttribute = propertyAttributes.FirstOrDefault(
                attribute => attribute.Name.LocalName == "DataContext");
            if (dataContextAttribute is not null)
            {
                EmitProperty(element, variable, dataContextAttribute);
            }

            ITypeSymbol? localDataContextType = dataContextAttribute is null
                ? null
                : ResolveLocalDataContextType(element, variable, dataContextAttribute);
            if (localDataContextType is not null)
            {
                localDataContextTypes.Push(localDataContextType);
            }

            try
            {
                foreach (MarkupAttribute attribute in propertyAttributes.Where(attribute => !ReferenceEquals(attribute, dataContextAttribute)))
                {
                    if (attribute.Name.LocalName == "MotionClip")
                    {
                        Report(
                            InvalidDirective,
                            attribute,
                            Path.GetFileName(file.Path),
                            "MotionClip resources cannot be assigned directly to controls; invoke them with @run inside an Aspect.");
                        continue;
                    }

                    if (isInline && attribute.Name.LocalName.Contains('.'))
                    {
                        // Attached properties (Grid.Row, Servo.Id, Panel.ZIndex) belong to UIElements.
                        Report(UnsupportedProperty, attribute, element.Name.LocalName, attribute.Name.LocalName);
                        continue;
                    }

                    if (TryEmitGridAttachedProperty(variable, attribute))
                    {
                        continue;
                    }

                    if (TryEmitUiPropertyAttachedProperty(element, variable, attribute))
                    {
                        continue;
                    }

                    if (TryEmitDeclaredAttachedProperty(element, variable, attribute))
                    {
                        continue;
                    }

                    if (TryEmitEventAttribute(element, variable, attribute))
                    {
                        continue;
                    }

                    EmitProperty(element, variable, attribute);
                }

                ApplyAspects(element, variable, aspects);
                if (templates.Length == 1)
                {
                    EmitDirectTemplate(element, variable, templates[0], ReferenceEquals(element, document.Root));
                }

                EmitBrushPropertyElement(element, variable);
                EmitGridDefinitionElements(element, variable);
                EmitContentTemplatePropertyElement(element, variable);
                EmitRenderSurfaceSceneElement(element, variable);
                ReportLegacyTemplatesPropertyElements(element);
                if (templateCollections.Length == 1)
                {
                    EmitTemplatesDirective(element, variable, templateCollections[0]);
                }
                EmitItemsControlItemsPanelElement(element, variable);

                if (elementType is not null && HasInlineContent(element, elementType))
                {
                    EmitInlineContent(element, elementType, variable, parsedContent);
                }
                else if (!isTileMap && parsedContent.HasDirectives)
                {
                    EmitReactiveContent(element, variable, parsedContent);
                }
                else if (!isTileMap)
                {
                    foreach (DirectiveNode node in parsedContent.Nodes)
                    {
                        switch (node)
                        {
                            case DirectiveTextNode text:
                                EmitTextContent(element, variable, text.Text);
                                break;
                            case DirectiveElementNode child:
                                if (IsNonContentPropertyElement(element, child.Element))
                                {
                                    break;
                                }

                                bool childIsInline = IsInlineElement(child.Element);
                                string childVariable = EmitElement(child.Element, allowInline: childIsInline);
                                EmitChild(element, variable, childVariable, childIsInline);
                                break;
                            case DirectiveTemplateNode _:
                            case DirectiveTemplatesNode _:
                                break;
                            case DirectiveDefaultNode defaults:
                                Report(InvalidDirective, defaults.Source, Path.GetFileName(file.Path), "@default is valid only inside Aspect resources.");
                                break;
                            case DirectiveAssignmentNode assignment:
                                Report(InvalidDirective, assignment.Source, Path.GetFileName(file.Path), "Property assignments must be inside an @if block.");
                                break;
                        }
                    }
                }
            }
            finally
            {
                if (localDataContextType is not null)
                {
                    localDataContextTypes.Pop();
                }
            }

            return variable;
        }

        private string? BuildTileMapInitializer(MarkupElement map, DirectiveParseResult content)
        {
            List<string> placements = new();
            Dictionary<string, string> imageSizes = new(StringComparer.Ordinal);
            foreach (DirectiveNode node in content.Nodes)
            {
                if (node is DirectiveElementNode child)
                {
                    if (IsNonContentPropertyElement(map, child.Element)) { continue; }
                    MarkupElement tile = child.Element;
                    MarkupAttribute? image = tile.Attribute("Image");
                    if (image is null || !image.Value.StartsWith("$", StringComparison.Ordinal)) { continue; }
                    GeneratedExpression? reference = ResolveReferenceValue("Tile", "Image", image.Value.Substring(1), MarkupValueKind.ImageReference, image);
                    if (reference is null) { continue; }
                    if (tile.Attribute("ImageWidth") is MarkupAttribute imageWidth &&
                        tile.Attribute("ImageHeight") is MarkupAttribute imageHeight)
                    {
                        string width = float.Parse(imageWidth.Value, CultureInfo.InvariantCulture).ToString("R", CultureInfo.InvariantCulture) + "f";
                        string height = float.Parse(imageHeight.Value, CultureInfo.InvariantCulture).ToString("R", CultureInfo.InvariantCulture) + "f";
                        imageSizes[reference.Code] = "new global::Cerneala.Drawing.DrawSize(" + width + ", " + height + ")";
                    }
                    string[] values = new[] { "X", "Y", "Width", "Height" }.Select(name =>
                        tile.Attribute(name) is MarkupAttribute attribute
                            ? float.Parse(attribute.Value, CultureInfo.InvariantCulture).ToString("R", CultureInfo.InvariantCulture) + "f"
                            : name is "X" or "Y" ? "0f" : "float.NaN").ToArray();
                    string? collider = tile.Elements().Select(EmitTileColliderDescriptor).SingleOrDefault();
                    string colliderArgument = collider is null ? string.Empty : collider + ", ";
                    placements.Add("new global::Cerneala.UI.Controls.Tile(" + reference.Code + ", " + colliderArgument + string.Join(", ", values) + ")");
                }
                else if (node is DirectiveTextNode text && !string.IsNullOrWhiteSpace(text.Text) ||
                    node is not DirectiveTextNode && node is not DirectiveTemplateNode && node is not DirectiveTemplatesNode && node is not DirectivePrismNode)
                {
                    Report(InvalidDocumentShape, map, Path.GetFileName(file.Path), "TileMap2D content accepts static Tile declarations only.");
                }
            }
            if (placements.Count > 0)
            {
                string metadata = imageSizes.Count == 0 ? string.Empty :
                    ", new global::System.Collections.Generic.Dictionary<string, global::Cerneala.Drawing.DrawSize>(global::System.StringComparer.Ordinal) { " +
                    string.Join(", ", imageSizes.Select(pair => "[(" + pair.Key + ").ResourceId!.Value.Key] = " + pair.Value)) + " }";
                return "global::Cerneala.UI.Controls.TileMap2D.FromModel(new global::Cerneala.UI.Controls.TileMap2DModel(new global::Cerneala.UI.Controls.Tile[] { " + string.Join(", ", placements) + " })" + metadata + ")";
            }

            return null;
        }

        private string EmitTileColliderDescriptor(MarkupElement element)
        {
            string typeName = ResolveElementTypeSymbol(element.Name.LocalName)!.Name;
            string shape = typeName.Substring(0, typeName.Length - "Collider2D".Length);
            List<string> arguments = new() { "global::Cerneala.UI.Controls.TileColliderShape2D." + shape };
            foreach (MarkupAttribute attribute in element.Attributes())
            {
                string name = attribute.Name.LocalName;
                if (attribute.IsNamespaceDeclaration || name is "EndX" or "EndY") { continue; }
                string value = name switch
                {
                    "Points" => Literal(attribute.Value),
                    "IsTrigger" => bool.Parse(attribute.Value) ? "true" : "false",
                    "CollisionLayer" or "CollisionMask" => uint.Parse(attribute.Value, CultureInfo.InvariantCulture).ToString(CultureInfo.InvariantCulture) + "u",
                    _ => float.Parse(attribute.Value, CultureInfo.InvariantCulture).ToString("R", CultureInfo.InvariantCulture) + "f"
                };
                arguments.Add(char.ToLowerInvariant(name[0]) + name.Substring(1) + ": " + value);
            }
            if (shape == "Segment")
            {
                string endX = float.Parse(element.Attribute("EndX")?.Value ?? "1", CultureInfo.InvariantCulture)
                    .ToString("R", CultureInfo.InvariantCulture);
                string endY = float.Parse(element.Attribute("EndY")?.Value ?? "0", CultureInfo.InvariantCulture)
                    .ToString("R", CultureInfo.InvariantCulture);
                arguments.Add("points: " + Literal("0,0 " + endX + "," + endY));
            }
            return "new global::Cerneala.UI.Controls.TileColliderDescriptor2D(" + string.Join(", ", arguments) + ")";
        }

        private ITypeSymbol? ResolveLocalDataContextType(
            MarkupElement element,
            string variable,
            MarkupAttribute attribute)
        {
            PropertySpec? dataContextSpec = FindPropertySpec(
                element.Name.LocalName,
                "DataContext",
                ReferenceEquals(element, document.Root));
            if (dataContextSpec is null)
            {
                return null;
            }

            ParsedMarkupValue? parsed = ParseMarkupBindingValue(
                attribute.Value,
                assignment: false,
                stringTarget: false,
                attribute);
            if (parsed?.Kind != ParsedMarkupValueKind.DirectBinding || parsed.Binding is null)
            {
                return null;
            }

            BindingResolutionContext bindingContext = CreateBindingResolutionContext(
                variable,
                element.Name.LocalName,
                ReferenceEquals(element, document.Root));
            return ResolveBindingSource(bindingContext, parsed.Binding.Path, attribute, attribute)?.ValueType;
        }

        private bool TryEmitGridAttachedProperty(string variable, MarkupAttribute attribute)
        {
            string method = attribute.Name.LocalName switch
            {
                "Grid.Row" => "SetRow",
                "Grid.Column" => "SetColumn",
                "Grid.RowSpan" => "SetRowSpan",
                "Grid.ColumnSpan" => "SetColumnSpan",
                _ => string.Empty
            };
            if (method.Length == 0)
            {
                return false;
            }

            bool isSpan = method.EndsWith("Span", StringComparison.Ordinal);
            if (!int.TryParse(attribute.Value.Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out int value) ||
                (isSpan ? value <= 0 : value < 0))
            {
                Report(InvalidPropertyValue, attribute, "Grid", attribute.Name.LocalName.Substring("Grid.".Length), attribute.Value);
                return true;
            }

            currentLines.Add(
                "global::Cerneala.UI.Layout.Panels.Grid." + method + "(" + variable + ", " +
                value.ToString(CultureInfo.InvariantCulture) + ");");
            return true;
        }

        // Attached properties backed by a UiProperty field of their owner type (Servo.Id, Panel.ZIndex).
        // Example: Panel.ZIndex="2" sets Panel.ZIndexProperty to the integer 2 on the element.
        private static readonly (string MarkupName, string OwnerMetadataName, string FieldName, string PropertyName, MarkupValueKind Kind)[] UiPropertyAttachedProperties =
        [
            ("Servo.Id", "Cerneala.UI.Servo.Servo", "IdProperty", "Id", MarkupValueKind.String),
            ("Panel.ZIndex", "Cerneala.UI.Layout.Panels.Panel", "ZIndexProperty", "ZIndex", MarkupValueKind.Integer),
            ("Canvas.Left", "Cerneala.UI.Layout.Panels.Canvas", "LeftProperty", "Left", MarkupValueKind.Float),
            ("Canvas.Top", "Cerneala.UI.Layout.Panels.Canvas", "TopProperty", "Top", MarkupValueKind.Float)
        ];

        // Any other Owner.X attribute the editor accepts (CernealaSemanticModel.BindAttachedAttribute): the owner
        // resolves like the editor resolves it and has a static SetX(element, T).
        //   - Owner.XProperty is a public static UiProperty<T>: handled like the table above (literal or binding).
        //     Example: for an owner Layering with "public static readonly UiProperty<int> LayerProperty",
        //     Layering.Layer="2" -> element.SetValue(global::Layering.LayerProperty, 2, ...).
        //   - Otherwise only a literal is valid: Tagger.Tag="x" -> global::TestInput.Tagger.SetTag(element, "x").
        //     A '$' binding on such a setter is rejected by the editor (CERNEALAUI004), and the generator reports
        //     the editor's diagnostics and stops before it emits, so it never reaches this method with one.
        // Returns false (the caller then reports CERNEALAUI003) when no such owner or member exists.
        private bool TryEmitDeclaredAttachedProperty(
            MarkupElement element,
            string variable,
            MarkupAttribute attribute)
        {
            string markupName = attribute.Name.LocalName;
            int separator = markupName.LastIndexOf('.');
            if (separator <= 0)
            {
                return false;
            }

            string ownerName = markupName.Substring(0, separator);
            string propertyName = markupName.Substring(separator + 1);
            INamedTypeSymbol? ownerType = ResolveAttachedPropertyOwnerType(ownerName);
            INamedTypeSymbol? elementType = GetElementTypeSymbol(element);
            INamedTypeSymbol? uiPropertyType = compilation.GetTypeByMetadataName("Cerneala.UI.Core.UiProperty`1");
            if (ownerType is null || elementType is null || uiPropertyType is null)
            {
                return false;
            }

            IFieldSymbol? propertyField = FindUiPropertyField(ownerType, propertyName + "Property", uiPropertyType);
            if (propertyField is { DeclaredAccessibility: Accessibility.Public, Type: INamedTypeSymbol fieldType } &&
                fieldType.TypeArguments.Length == 1)
            {
                ITypeSymbol valueType = fieldType.TypeArguments[0];
                EmitProperty(
                    element,
                    variable,
                    attribute,
                    new PropertySpec(
                        propertyName,
                        GetMarkupValueKind(valueType, constraintSource: null),
                        propertyField.ContainingType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat) +
                            "." + propertyField.Name,
                        valueType),
                    markupName);
                return true;
            }

            IMethodSymbol? setter = FindAttachedPropertySetter(ownerType, "Set" + propertyName, elementType);
            if (setter is null)
            {
                return false;
            }

            ITypeSymbol setterValueType = setter.Parameters[1].Type;
            PropertySpec spec = new(propertyName, GetMarkupValueKind(setterValueType, constraintSource: null), string.Empty, setterValueType);
            string value = attribute.Value;
            GeneratedExpression? expression = ParseLiteralValue(
                ownerName,
                propertyName,
                attribute,
                setterValueType.SpecialType == SpecialType.System_String ? UnescapeMarkupDollar(value) : value,
                spec);
            if (expression is not null)
            {
                currentLines.Add(
                    setter.ContainingType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat) + "." + setter.Name +
                    "(" + variable + ", " + expression.Code + ");");
            }

            return true;
        }

        private IMethodSymbol? FindAttachedPropertySetter(INamedTypeSymbol ownerType, string setterName, INamedTypeSymbol elementType)
        {
            for (INamedTypeSymbol? current = ownerType; current is not null; current = current.BaseType)
            {
                IMethodSymbol? setter = current.GetMembers(setterName)
                    .OfType<IMethodSymbol>()
                    .FirstOrDefault(candidate => candidate.IsStatic &&
                        candidate.MethodKind == MethodKind.Ordinary &&
                        candidate.Parameters.Length == 2 &&
                        IsAccessibleFromGeneratedCode(candidate) &&
                        compilation.ClassifyCommonConversion(elementType, candidate.Parameters[0].Type).IsImplicit);
                if (setter is not null)
                {
                    return setter;
                }
            }

            return null;
        }

        private bool TryEmitUiPropertyAttachedProperty(
            MarkupElement element,
            string variable,
            MarkupAttribute attribute)
        {
            int tableIndex = -1;
            for (int i = 0; i < UiPropertyAttachedProperties.Length; i++)
            {
                if (string.Equals(
                        attribute.Name.LocalName,
                        UiPropertyAttachedProperties[i].MarkupName,
                        StringComparison.Ordinal))
                {
                    tableIndex = i;
                    break;
                }
            }

            if (tableIndex < 0)
            {
                return false;
            }

            (string markupName, string ownerMetadataName, string fieldName, string propertyName, MarkupValueKind kind) =
                UiPropertyAttachedProperties[tableIndex];
            INamedTypeSymbol? ownerType = compilation.GetTypeByMetadataName(ownerMetadataName);
            IFieldSymbol? propertyField = ownerType?
                .GetMembers(fieldName)
                .OfType<IFieldSymbol>()
                .FirstOrDefault();
            if (propertyField?.Type is not INamedTypeSymbol fieldType || fieldType.TypeArguments.Length != 1)
            {
                Report(
                    UnsupportedProperty,
                    attribute,
                    element.Name.LocalName,
                    attribute.Name.LocalName);
                return true;
            }

            PropertySpec spec = new(
                propertyName,
                kind,
                propertyField.ContainingType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat) +
                    "." + propertyField.Name,
                fieldType.TypeArguments[0]);
            EmitProperty(
                element,
                variable,
                attribute,
                spec,
                markupName);
            return true;
        }

        private void EmitTextContent(MarkupElement element, string variable, string text)
        {
            switch (element.Name.LocalName)
            {
                case "TextBlock":
                    currentLines.Add(
                        variable + ".SetValue(global::Cerneala.UI.Controls.TextBlock.TextProperty, " + Literal(text) +
                        ", global::Cerneala.UI.Core.UiPropertyValueSource.MarkupBase);");
                    break;
                case "Button":
                    currentLines.Add(
                        variable + ".SetValue(global::Cerneala.UI.Controls.ContentControl.ContentProperty, (object?)" + Literal(text) +
                        ", global::Cerneala.UI.Core.UiPropertyValueSource.MarkupBase);");
                    break;
                default:
                    Report(UnsupportedProperty, element, element.Name.LocalName, "#text");
                    break;
            }
        }

        private void EmitChild(MarkupElement parent, string parentVariable, string childVariable, bool childIsInline = false)
        {
            INamedTypeSymbol? parentType = ResolveElementTypeSymbol(parent.Name.LocalName);
            if (childIsInline)
            {
                // An inline belongs to the Inlines of a TextBlock or of a Span, and to nothing else.
                if (parentType is not null && IsInlineHostType(parentType))
                {
                    currentLines.Add(parentVariable + ".Inlines.Add(" + childVariable + ");");
                }
                else
                {
                    Report(UnsupportedProperty, parent, parent.Name.LocalName, "#child");
                }

                return;
            }

            INamedTypeSymbol? panelType = compilation.GetTypeByMetadataName("Cerneala.UI.Layout.Panels.Panel");
            INamedTypeSymbol? itemsControlType = compilation.GetTypeByMetadataName("Cerneala.UI.Controls.ItemsControl");
            INamedTypeSymbol? decoratorType = compilation.GetTypeByMetadataName("Cerneala.UI.Controls.Decorator");
            INamedTypeSymbol? contentControlType = compilation.GetTypeByMetadataName("Cerneala.UI.Controls.ContentControl");
            INamedTypeSymbol? scrollViewerType = compilation.GetTypeByMetadataName("Cerneala.UI.Controls.ScrollViewer");
            INamedTypeSymbol? sceneType = compilation.GetTypeByMetadataName("Cerneala.UI.Controls.Scene2D");
            INamedTypeSymbol? spriteType = compilation.GetTypeByMetadataName("Cerneala.UI.Controls.Sprite2D");

            if (parentType is not null && spriteType is not null && IsOrDerivesFrom(parentType, spriteType))
            {
                currentLines.Add(parentVariable + ".Collider = " + childVariable + ";");
                return;
            }

            if (parentType is not null && sceneType is not null && IsOrDerivesFrom(parentType, sceneType))
            {
                currentLines.Add(parentVariable + ".Children.Add(" + childVariable + ");");
                return;
            }

            if (parentType is not null && panelType is not null && IsOrDerivesFrom(parentType, panelType))
            {
                currentLines.Add(parentVariable + ".Children.Add(" + childVariable + ");");
                return;
            }

            if (parentType is not null && itemsControlType is not null && IsOrDerivesFrom(parentType, itemsControlType))
            {
                currentLines.Add(parentVariable + ".Items.Add(" + childVariable + ");");
                return;
            }

            if (parentType is not null && decoratorType is not null && IsOrDerivesFrom(parentType, decoratorType))
            {
                currentLines.Add(parentVariable + ".Child = " + childVariable + ";");
                return;
            }

            if (parentType is not null && contentControlType is not null && IsOrDerivesFrom(parentType, contentControlType))
            {
                currentLines.Add(parentVariable + ".Content = " + childVariable + ";");
                return;
            }

            if (parentType is not null && scrollViewerType is not null && IsOrDerivesFrom(parentType, scrollViewerType))
            {
                currentLines.Add(parentVariable + ".Content = " + childVariable + ";");
                return;
            }

            Report(UnsupportedProperty, parent, parent.Name.LocalName, "#child");
        }
    }
}
