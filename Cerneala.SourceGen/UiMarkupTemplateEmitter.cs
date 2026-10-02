using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using Cerneala.Language;
using Microsoft.CodeAnalysis;

namespace Cerneala.SourceGen;

public sealed partial class UiMarkupGenerator
{
    private sealed partial class GenerationScope
    {

        private sealed class ContentTemplateResource
        {
            public ContentTemplateResource(
                string? name,
                string generatedName,
                INamedTypeSymbol? dataType,
                string? key,
                int priority,
                string variable,
                MarkupElement root,
                MarkupElement source)
            {
                Name = name;
                GeneratedName = generatedName;
                DataType = dataType;
                Key = key;
                Priority = priority;
                Variable = variable;
                Root = root;
                Source = source;
            }

            public string? Name { get; }

            public string GeneratedName { get; }

            public INamedTypeSymbol? DataType { get; }

            public string? DataTypeCode => DataType?.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);

            public string? Key { get; }

            public int Priority { get; }

            public string Variable { get; }

            public MarkupElement Root { get; }

            public MarkupElement Source { get; }
        }

        private sealed class TemplateEmissionContext
        {
            public TemplateEmissionContext(
                string contextVariable,
                string ownerVariable,
                string ownerElementName,
                MarkupElement? ownerElement,
                INamedTypeSymbol ownerType,
                bool ownerIsRoot,
                bool registerParts)
            {
                ContextVariable = contextVariable;
                OwnerVariable = ownerVariable;
                OwnerElementName = ownerElementName;
                OwnerElement = ownerElement;
                OwnerType = ownerType;
                OwnerIsRoot = ownerIsRoot;
                RegisterParts = registerParts;
            }

            public string ContextVariable { get; }

            public string OwnerVariable { get; }

            public string OwnerElementName { get; }

            public MarkupElement? OwnerElement { get; }

            public INamedTypeSymbol OwnerType { get; }

            public bool OwnerIsRoot { get; }

            public bool RegisterParts { get; }

            public HashSet<string> PartNames { get; } = new(StringComparer.Ordinal);

            public Dictionary<string, MarkupElement> Parts { get; } = new(StringComparer.Ordinal);
        }

        private ContentTemplateResource? ParseContentTemplate(MarkupElement resource)
        {
            MarkupAttribute? unsupportedAttribute = resource.Attributes()
                .FirstOrDefault(attribute => !attribute.IsNamespaceDeclaration &&
                    attribute.Name.LocalName is not "Name" and not "DataType" and not "Key" and not "Priority");
            if (unsupportedAttribute is not null)
            {
                Report(
                    InvalidDocumentShape,
                    unsupportedAttribute,
                    Path.GetFileName(file.Path),
                    "ContentTemplate supports only Name, DataType, Key, and Priority attributes.");
                return null;
            }

            if (resource.Nodes().OfType<MarkupText>().Any(text => !string.IsNullOrWhiteSpace(text.Value)))
            {
                Report(
                    InvalidDocumentShape,
                    resource,
                    Path.GetFileName(file.Path),
                    "ContentTemplate accepts exactly one visual root and no text content.");
                return null;
            }

            MarkupElement[] roots = resource.Elements().ToArray();
            if (roots.Length != 1)
            {
                Report(
                    InvalidDocumentShape,
                    resource,
                    Path.GetFileName(file.Path),
                    "ContentTemplate requires exactly one visual root.");
                return null;
            }

            MarkupAttribute? namedElement = roots[0].DescendantsAndSelf()
                .Select(element => element.Attribute("Name"))
                .FirstOrDefault(attribute => attribute is not null);
            if (namedElement is not null)
            {
                Report(
                    InvalidDocumentShape,
                    namedElement,
                    Path.GetFileName(file.Path),
                    "Named visual elements inside ContentTemplate are not supported because each realization owns a separate namescope.");
                return null;
            }

            string? name = resource.Attribute("Name")?.Value.Trim();
            if (name is not null && name.Length == 0)
            {
                Report(
                    InvalidDocumentShape,
                    resource.Attribute("Name")!,
                    Path.GetFileName(file.Path),
                    "ContentTemplate Name cannot be empty.");
                return null;
            }

            INamedTypeSymbol? dataType = null;
            MarkupAttribute? dataTypeAttribute = resource.Attribute("DataType");
            if (dataTypeAttribute is not null)
            {
                dataType = ResolveMarkupTypeReference(dataTypeAttribute);
                if (dataType is null || !IsAccessibleFromGeneratedCode(dataType))
                {
                    Report(
                        InvalidBindingSource,
                        dataTypeAttribute,
                        dataTypeAttribute.Value,
                        "ContentTemplate DataType must name an accessible type in the current compilation.");
                    return null;
                }

            }

            int priority = 0;
            MarkupAttribute? priorityAttribute = resource.Attribute("Priority");
            if (priorityAttribute is not null &&
                !int.TryParse(priorityAttribute.Value.Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out priority))
            {
                Report(
                    InvalidPropertyValue,
                    priorityAttribute,
                    "ContentTemplate",
                    "Priority",
                    priorityAttribute.Value);
                return null;
            }

            int id = nextResourceId++;
            string generatedName = name ??
                CernealaDocumentPath.GetLogicalName(file.Path) +
                ".ContentTemplate." + id.ToString(CultureInfo.InvariantCulture);
            return new ContentTemplateResource(
                name,
                generatedName,
                dataType,
                resource.Attribute("Key")?.Value,
                priority,
                "contentTemplate" + id.ToString(CultureInfo.InvariantCulture),
                roots[0],
                resource);
        }

        private void EmitAspectTemplates()
        {
            List<(AspectResource Aspect, INamedTypeSymbol OwnerType)> templates = [];
            foreach (AspectResource aspect in allAspects.Where(candidate => candidate.Template is not null))
            {
                INamedTypeSymbol? ownerType = ResolveAspectTargetTypeSymbol(aspect.TargetName, aspect.Source);
                if (!IsControlType(ownerType))
                {
                    Report(
                        InvalidComponentTemplate,
                        aspect.Template!.Source,
                        Path.GetFileName(file.Path),
                        "@template may be declared only for a type derived from Control; '" + aspect.TargetName + "' is not a Control.");
                    continue;
                }

                string variable = "aspectTemplate" + nextTemplateId.ToString(CultureInfo.InvariantCulture);
                nextTemplateId++;
                aspect.TemplateVariable = variable;
                templates.Add((aspect, ownerType!));
                currentLines.Add(
                    "global::Cerneala.UI.Controls.Templates.ComponentTemplate<" +
                    ownerType!.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat) + "> " + variable + " = null!;");
            }

            foreach ((AspectResource aspect, INamedTypeSymbol ownerType) in templates)
            {
                EmitComponentTemplate(
                    aspect.TemplateVariable!,
                    aspect.TargetName,
                    ownerElement: null,
                    ownerType,
                    ownerIsRoot: false,
                    aspect.Template!,
                    registerParts: true);
            }
        }

        private void EmitDirectTemplate(
            MarkupElement owner,
            string ownerVariable,
            DirectiveTemplateNode template,
            bool ownerIsRoot)
        {
            INamedTypeSymbol? ownerType = ResolvePropertyOwnerType(owner.Name.LocalName, ownerIsRoot);
            if (!IsControlType(ownerType))
            {
                Report(
                    InvalidComponentTemplate,
                    template.Source,
                    Path.GetFileName(file.Path),
                    "@template may be declared only on an element derived from Control; '" + owner.Name.LocalName + "' is not a Control.");
                return;
            }

            EmitComponentTemplate(
                ownerVariable + ".ComponentTemplate",
                owner.Name.LocalName,
                owner,
                ownerType!,
                ownerIsRoot,
                template,
                registerParts: true);
        }

        private void EmitComponentTemplate(
            string assignmentTarget,
            string ownerElementName,
            MarkupElement? ownerElement,
            INamedTypeSymbol ownerType,
            bool ownerIsRoot,
            DirectiveTemplateNode template,
            bool registerParts)
        {
            int templateId = nextTemplateId++;
            string contextVariable = "templateContext" + templateId.ToString(CultureInfo.InvariantCulture);
            List<string> factoryLines = [];
            List<string> factoryPostLines = [];
            string rootVariable = string.Empty;
            TemplateEmissionContext emissionContext = new(
                contextVariable,
                contextVariable + ".Owner",
                ownerElementName,
                ownerElement,
                ownerType,
                ownerIsRoot,
                registerParts);

            WithEmissionBuffers(factoryLines, factoryPostLines, () =>
            {
                templateEmissionContexts.Push(emissionContext);
                try
                {
                    rootVariable = EmitElement(template.Root);
                }
                finally
                {
                    templateEmissionContexts.Pop();
                }
            });

            templateParts[template] = new Dictionary<string, MarkupElement>(emissionContext.Parts, StringComparer.Ordinal);

            string typeCode = ownerType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
            string generatedName = CernealaDocumentPath.GetLogicalName(file.Path) +
                "." + ownerElementName + ".Template." + templateId.ToString(CultureInfo.InvariantCulture);
            currentLines.Add(assignmentTarget + " = new global::Cerneala.UI.Controls.Templates.ComponentTemplate<" + typeCode + ">(");
            currentLines.Add("    " + Literal(generatedName) + ",");
            currentLines.Add("    " + contextVariable + " =>");
            currentLines.Add("    {");
            foreach (string line in factoryLines)
            {
                currentLines.Add("        " + line);
            }

            foreach (string line in factoryPostLines)
            {
                currentLines.Add("        " + line);
            }

            currentLines.Add("        return " + rootVariable + ";");
            currentLines.Add("    });");
        }

        private bool IsControlType(INamedTypeSymbol? type)
        {
            INamedTypeSymbol? controlType = compilation.GetTypeByMetadataName("Cerneala.UI.Controls.Control");
            return type is not null && controlType is not null && IsOrDerivesFrom(type, controlType);
        }

        private bool ValidateDeclaredTemplatePartType(
            TemplateEmissionContext templateContext,
            string partName,
            MarkupElement element)
        {
            const string templatePartAttributeName = "Cerneala.UI.Controls.Templates.TemplatePartAttribute";
            for (INamedTypeSymbol? current = templateContext.OwnerType; current is not null; current = current.BaseType)
            {
                foreach (AttributeData attribute in current.GetAttributes())
                {
                    if (attribute.AttributeClass?.ToDisplayString() != templatePartAttributeName ||
                        attribute.ConstructorArguments.Length != 2 ||
                        attribute.ConstructorArguments[0].Value is not string declaredName ||
                        !string.Equals(declaredName, partName, StringComparison.Ordinal) ||
                        attribute.ConstructorArguments[1].Value is not INamedTypeSymbol expectedType)
                    {
                        continue;
                    }

                    INamedTypeSymbol? actualType = ResolveElementTypeSymbol(element.Name.LocalName);
                    if (actualType is null || IsOrDerivesFrom(actualType, expectedType))
                    {
                        return true;
                    }

                    Report(
                        InvalidComponentTemplate,
                        (MarkupObject?)element.Attribute("Name") ?? element,
                        Path.GetFileName(file.Path),
                        "Template part Name '" + partName + "' on '" + templateContext.OwnerElementName +
                        "' expects type '" + expectedType.ToDisplayString() + "', but element '" +
                        element.Name.LocalName + "' has type '" + actualType.ToDisplayString() + "'.");
                    return false;
                }
            }

            return true;
        }

        private void EmitContentTemplate(ContentTemplateResource template, string assignmentTarget)
        {
            int templateId = nextTemplateId++;
            string contextVariable = "contentTemplateContext" + templateId.ToString(CultureInfo.InvariantCulture);
            List<string> factoryLines = [];
            List<string> factoryPostLines = [];
            string rootVariable = string.Empty;
            WithEmissionBuffers(factoryLines, factoryPostLines, () =>
            {
                contentTemplateLocalDataContextDepths.Push(localDataContextTypes.Count);
                contentTemplateDataTypes.Push(template.DataType);
                contentTemplateContextVariables.Push(contextVariable);
                try
                {
                    rootVariable = EmitElement(template.Root);
                }
                finally
                {
                    contentTemplateContextVariables.Pop();
                    contentTemplateDataTypes.Pop();
                    contentTemplateLocalDataContextDepths.Pop();
                }
            });

            currentLines.Add(assignmentTarget + " = new global::Cerneala.UI.Controls.Templates.ContentTemplate(");
            currentLines.Add("    " + Literal(template.GeneratedName) + ",");
            currentLines.Add("    " + (template.DataTypeCode is null ? "null" : "typeof(" + template.DataTypeCode + ")") + ",");
            currentLines.Add("    " + (template.Key is null ? "null" : Literal(template.Key)) + ",");
            currentLines.Add("    " + template.Priority.ToString(CultureInfo.InvariantCulture) + ",");
            currentLines.Add("    " + contextVariable + " =>");
            currentLines.Add("    {");
            foreach (string line in factoryLines)
            {
                currentLines.Add("        " + line);
            }

            if (template.Root.Attribute("DataContext") is null)
            {
                currentLines.Add("        " + rootVariable + ".DataContext = " + contextVariable + ".Data;");
            }
            foreach (string line in factoryPostLines)
            {
                currentLines.Add("        " + line);
            }

            currentLines.Add("        return " + rootVariable + ";");
            currentLines.Add("    });");
        }
    }
}
