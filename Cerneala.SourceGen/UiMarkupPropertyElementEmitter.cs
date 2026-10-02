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

        private void EmitGridDefinitionElements(MarkupElement owner, string ownerVariable)
        {
            if (owner.Name.LocalName != "Grid")
            {
                return;
            }

            EmitGridDefinitions(owner, ownerVariable, "ColumnDefinitions", "ColumnDefinition", "Width");
            EmitGridDefinitions(owner, ownerVariable, "RowDefinitions", "RowDefinition", "Height");
        }

        private void EmitGridDefinitions(
            MarkupElement owner,
            string ownerVariable,
            string collectionName,
            string definitionName,
            string lengthPropertyName)
        {
            string propertyElementName = "Grid." + collectionName;
            MarkupElement[] propertyElements = owner.Elements(propertyElementName).ToArray();
            if (propertyElements.Length > 1)
            {
                Report(
                    InvalidDocumentShape,
                    propertyElements[1],
                    Path.GetFileName(file.Path),
                    propertyElementName + " may be declared only once.");
                return;
            }

            if (propertyElements.Length == 0)
            {
                return;
            }

            MarkupElement propertyElement = propertyElements[0];
            if (propertyElement.Attributes().Any(attribute => !attribute.IsNamespaceDeclaration) ||
                propertyElement.Nodes().OfType<MarkupText>().Any(text => !string.IsNullOrWhiteSpace(text.Value)))
            {
                Report(
                    InvalidDocumentShape,
                    propertyElement,
                    Path.GetFileName(file.Path),
                    propertyElementName + " accepts only " + definitionName + " children.");
                return;
            }

            foreach (MarkupElement definition in propertyElement.Elements())
            {
                if (definition.Name.LocalName != definitionName || definition.Elements().Any() ||
                    definition.Nodes().OfType<MarkupText>().Any(text => !string.IsNullOrWhiteSpace(text.Value)) ||
                    definition.Attributes().Any(attribute =>
                        !attribute.IsNamespaceDeclaration && attribute.Name.LocalName != lengthPropertyName))
                {
                    Report(
                        InvalidDocumentShape,
                        definition,
                        Path.GetFileName(file.Path),
                        propertyElementName + " accepts only empty " + definitionName + " children with an optional " +
                        lengthPropertyName + " attribute.");
                    continue;
                }

                MarkupAttribute? lengthAttribute = definition.Attribute(lengthPropertyName);
                string? length = lengthAttribute is null
                    ? "global::Cerneala.UI.Layout.Panels.GridLength.Star"
                    : ParseGridLength(definitionName, lengthPropertyName, lengthAttribute);
                if (length is not null)
                {
                    currentLines.Add(
                        ownerVariable + "." + collectionName + ".Add(new global::Cerneala.UI.Layout.Panels." +
                        definitionName + "(" + length + "));"
                    );
                }
            }
        }

        private string? ParseGridLength(string definitionName, string propertyName, MarkupAttribute attribute)
        {
            string value = attribute.Value.Trim();
            if (string.Equals(value, "Auto", StringComparison.OrdinalIgnoreCase))
            {
                return "global::Cerneala.UI.Layout.Panels.GridLength.Auto";
            }

            bool star = value.EndsWith("*", StringComparison.Ordinal);
            string numeric = star ? value.Substring(0, value.Length - 1).Trim() : value;
            if (star && numeric.Length == 0)
            {
                return "global::Cerneala.UI.Layout.Panels.GridLength.Star";
            }

            if (!float.TryParse(numeric, NumberStyles.Float, CultureInfo.InvariantCulture, out float parsed) ||
                parsed < 0 || float.IsNaN(parsed) || float.IsInfinity(parsed))
            {
                Report(InvalidPropertyValue, attribute, definitionName, propertyName, attribute.Value);
                return null;
            }

            string literal = parsed.ToString("R", CultureInfo.InvariantCulture) + "f";
            return star
                ? "global::Cerneala.UI.Layout.Panels.GridLength.Stars(" + literal + ")"
                : "global::Cerneala.UI.Layout.Panels.GridLength.Pixels(" + literal + ")";
        }

        private bool IsNonContentPropertyElement(MarkupElement owner, MarkupElement child)
        {
            return IsBrushPropertyElement(owner, child) ||
                GetContentTemplatePropertyName(owner, child) is not null ||
                IsRenderSurfaceSceneElement(owner, child) ||
                IsLegacyTemplatesPropertyElement(owner, child) ||
                IsItemsControlItemsPanelElement(owner, child) ||
                (owner.Name.LocalName == "Grid" &&
                    child.Name.LocalName is "Grid.ColumnDefinitions" or "Grid.RowDefinitions");
        }

        private bool IsItemsControlItemsPanelElement(MarkupElement owner, MarkupElement child)
        {
            string ownerName = owner.Name.LocalName;
            if (child.Name.LocalName != ownerName + ".ItemsPanel")
            {
                return false;
            }

            INamedTypeSymbol? ownerType = ResolvePropertyOwnerType(
                ownerName,
                isRoot: ReferenceEquals(owner, document.Root));
            INamedTypeSymbol? itemsControlType = compilation.GetTypeByMetadataName(
                "Cerneala.UI.Controls.ItemsControl");
            return ownerType is not null &&
                itemsControlType is not null &&
                IsOrDerivesFrom(ownerType, itemsControlType);
        }

        private bool IsLegacyTemplatesPropertyElement(MarkupElement owner, MarkupElement child)
        {
            string ownerName = owner.Name.LocalName;
            if (child.Name.LocalName != ownerName + ".Templates")
            {
                return false;
            }

            return SupportsTemplateCollection(owner);
        }

        private bool SupportsTemplateCollection(MarkupElement owner)
        {
            string ownerName = owner.Name.LocalName;
            INamedTypeSymbol? ownerType = ResolvePropertyOwnerType(
                ownerName,
                isRoot: ReferenceEquals(owner, document.Root));
            INamedTypeSymbol? itemsControlType = compilation.GetTypeByMetadataName(
                "Cerneala.UI.Controls.ItemsControl");
            INamedTypeSymbol? sceneItemsType = compilation.GetTypeByMetadataName(
                "Cerneala.UI.Controls.SceneItems2D");
            return ownerType is not null &&
                ((itemsControlType is not null && IsOrDerivesFrom(ownerType, itemsControlType)) ||
                 (sceneItemsType is not null && IsOrDerivesFrom(ownerType, sceneItemsType)));
        }

        private bool IsRenderSurfaceSceneElement(MarkupElement owner, MarkupElement child)
        {
            string ownerName = owner.Name.LocalName;
            if (child.Name.LocalName != ownerName + ".Scene")
            {
                return false;
            }

            INamedTypeSymbol? ownerType = ResolvePropertyOwnerType(
                ownerName,
                isRoot: ReferenceEquals(owner, document.Root));
            INamedTypeSymbol? renderSurfaceType = compilation.GetTypeByMetadataName(
                "Cerneala.UI.Controls.RenderSurface2D");
            return ownerType is not null &&
                renderSurfaceType is not null &&
                IsOrDerivesFrom(ownerType, renderSurfaceType);
        }

        private void EmitRenderSurfaceSceneElement(MarkupElement owner, string ownerVariable)
        {
            MarkupElement[] propertyElements = owner.Elements()
                .Where(child => IsRenderSurfaceSceneElement(owner, child))
                .ToArray();
            if (propertyElements.Length == 0)
            {
                return;
            }

            if (propertyElements.Length > 1)
            {
                Report(
                    InvalidDocumentShape,
                    propertyElements[1],
                    Path.GetFileName(file.Path),
                    owner.Name.LocalName + ".Scene may be declared only once.");
                return;
            }

            MarkupElement propertyElement = propertyElements[0];
            MarkupElement[] children = propertyElement.Elements().ToArray();
            string? sceneTypeName = children.Length == 1 ? ResolveElementType(children[0]) : null;
            INamedTypeSymbol? childType = sceneTypeName is null ? null : resolvedElementTypes[children[0].Name.Value];
            INamedTypeSymbol? sceneType = compilation.GetTypeByMetadataName("Cerneala.UI.Controls.Scene2D");
            if (propertyElement.Attributes().Any(attribute => !attribute.IsNamespaceDeclaration) ||
                propertyElement.Nodes().OfType<MarkupText>().Any(text => !string.IsNullOrWhiteSpace(text.Value)) ||
                children.Length != 1 ||
                childType is null || sceneType is null || !IsOrDerivesFrom(childType, sceneType))
            {
                Report(
                    InvalidDocumentShape,
                    propertyElement,
                    Path.GetFileName(file.Path),
                    owner.Name.LocalName + ".Scene requires exactly one Scene2D child.");
                return;
            }

            string sceneVariable = EmitElement(children[0]);
            currentLines.Add(ownerVariable + ".Scene = " + sceneVariable + ";");
        }

        private void ReportLegacyTemplatesPropertyElements(MarkupElement owner)
        {
            MarkupElement[] propertyElements = owner.Elements()
                .Where(child => IsLegacyTemplatesPropertyElement(owner, child))
                .ToArray();
            foreach (MarkupElement propertyElement in propertyElements)
            {
                Report(
                    InvalidDocumentShape,
                    propertyElement,
                    Path.GetFileName(file.Path),
                    propertyElement.Name.LocalName + " is not supported; use @templates { ... }.");
            }
        }

        private void EmitTemplatesDirective(
            MarkupElement owner,
            string ownerVariable,
            DirectiveTemplatesNode directive)
        {
            if (!SupportsTemplateCollection(owner))
            {
                Report(
                    InvalidDocumentShape,
                    directive.Source,
                    Path.GetFileName(file.Path),
                    "@templates is valid only inside ItemsControl-derived controls and SceneItems2D.");
                return;
            }

            if (directive.Templates.Any(template => template.Name.LocalName != "ContentTemplate"))
            {
                Report(
                    InvalidDocumentShape,
                    directive.Source,
                    Path.GetFileName(file.Path),
                    "@templates accepts only ContentTemplate elements.");
                return;
            }

            foreach (MarkupElement templateElement in directive.Templates)
            {
                ContentTemplateResource? template = ParseContentTemplate(templateElement);
                if (template is null)
                {
                    continue;
                }

                currentLines.Add(
                    "global::Cerneala.UI.Controls.Templates.ContentTemplate " + template.Variable + " = null!;");
                EmitContentTemplate(template, template.Variable);
                currentLines.Add(ownerVariable + ".Templates.Add(" + template.Variable + ");");
            }
        }

        private void EmitItemsControlItemsPanelElement(MarkupElement owner, string ownerVariable)
        {
            MarkupElement[] propertyElements = owner.Elements()
                .Where(child => IsItemsControlItemsPanelElement(owner, child))
                .ToArray();
            if (propertyElements.Length == 0)
            {
                return;
            }

            if (propertyElements.Length > 1)
            {
                Report(
                    InvalidDocumentShape,
                    propertyElements[1],
                    Path.GetFileName(file.Path),
                    owner.Name.LocalName + ".ItemsPanel may be declared only once.");
                return;
            }

            MarkupElement propertyElement = propertyElements[0];
            MarkupElement[] panels = propertyElement.Elements().ToArray();
            if (propertyElement.Attributes().Any(attribute => !attribute.IsNamespaceDeclaration) ||
                propertyElement.Nodes().OfType<MarkupText>().Any(text => !string.IsNullOrWhiteSpace(text.Value)) ||
                panels.Length != 1)
            {
                Report(
                    InvalidDocumentShape,
                    propertyElement,
                    Path.GetFileName(file.Path),
                    propertyElement.Name.LocalName + " requires exactly one Panel child.");
                return;
            }

            MarkupElement panel = panels[0];
            INamedTypeSymbol? panelType = ResolveElementTypeSymbol(panel.Name.LocalName);
            INamedTypeSymbol? panelBaseType = compilation.GetTypeByMetadataName(
                "Cerneala.UI.Layout.Panels.Panel");
            if (panelType is null || panelBaseType is null || !IsOrDerivesFrom(panelType, panelBaseType))
            {
                Report(
                    InvalidDocumentShape,
                    panel,
                    Path.GetFileName(file.Path),
                    propertyElement.Name.LocalName + " requires a child derived from Panel.");
                return;
            }

            string panelVariable = EmitElement(panel);
            currentLines.Add(ownerVariable + ".ItemsPanel = " + panelVariable + ";");
        }

        private string? GetContentTemplatePropertyName(MarkupElement owner, MarkupElement child)
        {
            string prefix = owner.Name.LocalName + ".";
            if (!child.Name.LocalName.StartsWith(prefix, StringComparison.Ordinal))
            {
                return null;
            }

            string propertyName = child.Name.LocalName.Substring(prefix.Length);
            PropertySpec? property = FindPropertySpec(
                owner.Name.LocalName,
                propertyName,
                isRoot: ReferenceEquals(owner, document.Root));
            return property?.ValueKind == MarkupValueKind.ContentTemplate && property.Assignable
                ? propertyName
                : null;
        }

        private void EmitContentTemplatePropertyElement(MarkupElement owner, string ownerVariable)
        {
            foreach (IGrouping<string, MarkupElement> propertyGroup in owner.Elements()
                .Select(child => new { Element = child, PropertyName = GetContentTemplatePropertyName(owner, child) })
                .Where(item => item.PropertyName is not null)
                .GroupBy(item => item.PropertyName!, item => item.Element, StringComparer.Ordinal))
            {
                string propertyName = propertyGroup.Key;
                MarkupElement[] propertyElements = propertyGroup.ToArray();
                if (propertyElements.Length > 1 || owner.Attribute(propertyName) is not null)
                {
                    Report(
                        InvalidDocumentShape,
                        propertyElements[0],
                        Path.GetFileName(file.Path),
                        owner.Name.LocalName + "." + propertyName + " may be assigned only once.");
                    continue;
                }

                MarkupElement propertyElement = propertyElements[0];
                if (propertyElement.Attributes().Any(attribute => !attribute.IsNamespaceDeclaration) ||
                    propertyElement.Nodes().OfType<MarkupText>().Any(text => !string.IsNullOrWhiteSpace(text.Value)))
                {
                    Report(
                        InvalidDocumentShape,
                        propertyElement,
                        Path.GetFileName(file.Path),
                        propertyElement.Name.LocalName + " accepts exactly one ContentTemplate child.");
                    continue;
                }

                MarkupElement[] templates = propertyElement.Elements().ToArray();
                if (templates.Length != 1 || templates[0].Name.LocalName != "ContentTemplate")
                {
                    Report(
                        InvalidDocumentShape,
                        propertyElement,
                        Path.GetFileName(file.Path),
                        propertyElement.Name.LocalName + " requires exactly one ContentTemplate child.");
                    continue;
                }

                ContentTemplateResource? template = ParseContentTemplate(templates[0]);
                if (template is not null)
                {
                    EmitContentTemplate(template, ownerVariable + "." + propertyName);
                }
            }
        }

        private void EmitBrushPropertyElement(MarkupElement owner, string ownerVariable)
        {
            foreach (IGrouping<string, MarkupElement> propertyGroup in owner.Elements()
                .Select(child => new { Element = child, PropertyName = GetBrushPropertyName(owner, child) })
                .Where(item => item.PropertyName is not null)
                .GroupBy(item => item.PropertyName!, item => item.Element, StringComparer.Ordinal))
            {
                string propertyName = propertyGroup.Key;
                MarkupElement[] propertyElements = propertyGroup.ToArray();
                if (propertyElements.Length > 1 || owner.Attribute(propertyName) is not null)
                {
                    Report(InvalidDocumentShape, propertyElements[0], Path.GetFileName(file.Path),
                        owner.Name.LocalName + "." + propertyName + " may be assigned only once.");
                    continue;
                }

                MarkupElement propertyElement = propertyElements[0];
                MarkupElement[] brushes = propertyElement.Elements().ToArray();
                if (brushes.Length != 1)
                {
                    Report(InvalidDocumentShape, propertyElement, Path.GetFileName(file.Path),
                        propertyElement.Name.LocalName + " requires exactly one brush child.");
                    continue;
                }

                MarkupElement brush = brushes[0];
                string? expression = BuildBrushExpression(brush);
                if (expression is null)
                {
                    if (brush.Name.LocalName is not ("SolidColorBrush" or "LinearGradientBrush" or "RadialGradientBrush" or "ImageBrush" or "DrawingBrush"))
                    {
                        Report(UnsupportedElement, brush, brush.Name.LocalName);
                    }

                    continue;
                }

                currentLines.Add(ownerVariable + "." + propertyName + " = " + expression + ";");
            }
        }

        private bool IsBrushPropertyElement(MarkupElement owner, MarkupElement child)
        {
            return GetBrushPropertyName(owner, child) is not null;
        }

        private string? GetBrushPropertyName(MarkupElement owner, MarkupElement child)
        {
            string prefix = owner.Name.LocalName + ".";
            if (!child.Name.LocalName.StartsWith(prefix, StringComparison.Ordinal))
            {
                return null;
            }

            string propertyName = child.Name.LocalName.Substring(prefix.Length);
            PropertySpec? property = FindPropertySpec(
                owner.Name.LocalName,
                propertyName,
                isRoot: ReferenceEquals(owner, document.Root));
            return property?.ValueKind == MarkupValueKind.Brush && property.Assignable ? propertyName : null;
        }
    }
}
