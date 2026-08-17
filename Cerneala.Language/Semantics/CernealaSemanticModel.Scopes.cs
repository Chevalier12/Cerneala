using System.Globalization;
using Cerneala.Language.Semantics.Symbols;
using Cerneala.Language.Syntax;
using Cerneala.Language.Text;

namespace Cerneala.Language.Semantics;

internal sealed partial class CernealaSemanticModel
{
    private readonly Dictionary<ElementSyntax, ILanguageTypeSymbol?> itemsSourceTypes = new();
    private readonly Dictionary<ElementSyntax, HashSet<string>> contentTemplateKeys = new();
    private readonly Dictionary<ElementSyntax, ResourceDefinition> inlineAspectProperties = new();

    private void BuildScopeGraph(ElementSyntax documentRoot, CancellationToken cancellationToken)
    {
        BuildParentMap(documentRoot, parent: null);
        foreach (ElementSyntax element in document.Syntax.DescendantElements())
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (element.Kind != SyntaxKind.PropertyElement && !IsSpecialElement(element))
            {
                elementTypes[element] = ResolveElementType(element, ReferenceEquals(element, documentRoot));
            }
        }

        CollectResourceScopes();
        CollectInlineAspectDefinitions();
        CollectApplicationResources();
        BuildTemplateContexts();
        BuildNameScopes();
    }

    private void CollectInlineAspectDefinitions()
    {
        foreach (ElementSyntax property in document.Syntax.DescendantElements().Where(element =>
            element.Kind == SyntaxKind.PropertyElement && element.Name.EndsWith(".Aspect", StringComparison.Ordinal)))
        {
            if (!parents.TryGetValue(property, out ElementSyntax? owner) || owner is null)
            {
                continue;
            }

            ElementSyntax[] declarations = property.Children.OfType<ElementSyntax>()
                .Where(element => element.Name == "Aspect")
                .ToArray();
            if (declarations.Length > 1)
            {
                AddShapeDiagnostic(property.NameToken.Span, "An inline Aspect property element accepts at most one Aspect declaration.");
                continue;
            }

            ElementSyntax body = declarations.Length == 1 ? declarations[0] : property;
            ILanguageTypeSymbol? targetType = GetElementType(owner, ReferenceEquals(owner, root));
            ResourceDefinition definition = new(
                name: null,
                ResourceKind.Aspect,
                type: null,
                targetType,
                body,
                document.Path,
                property.NameToken.Span,
                isApplication: false);
            inlineAspectProperties[property] = definition;
            resourceElements[property] = definition;
            if (!ReferenceEquals(body, property))
            {
                resourceElements[body] = definition;
            }

            if (FindAttribute(owner, "Aspect") is AttributeSyntax aspectAttribute)
            {
                AddShapeDiagnostic(
                    aspectAttribute.NameToken.Span,
                    "Element '" + owner.Name + "' cannot combine an Aspect attribute with an inline Aspect property element.");
            }
        }
    }

    private void BuildParentMap(ElementSyntax element, ElementSyntax? parent)
    {
        parents[element] = parent;
        foreach (ElementSyntax child in element.Children.OfType<ElementSyntax>())
        {
            BuildParentMap(child, element);
        }
    }

    private void CollectResourceScopes()
    {
        foreach (ElementSyntax property in document.Syntax.DescendantElements()
            .Where(IsResourcePropertyElement)
            .OrderBy(element => element.Span.Start))
        {
            if (!parents.TryGetValue(property, out ElementSyntax? owner) || owner is null)
            {
                continue;
            }

            string expectedName = owner.Name + ".Resources";
            if (!string.Equals(property.Name, expectedName, StringComparison.Ordinal))
            {
                AddShapeDiagnostic(
                    property.NameToken.Span,
                    "Resource property element '" + property.Name + "' must match its owner tag '" + expectedName + "'.");
                continue;
            }

            if (resourceScopes.ContainsKey(owner))
            {
                AddShapeDiagnostic(
                    property.NameToken.Span,
                    "Element '" + owner.Name + "' may declare only one Resources property element.");
                continue;
            }

            SemanticResourceScope scope = new(owner);
            resourceScopes.Add(owner, scope);
            foreach (ElementSyntax resourceElement in property.Children.OfType<ElementSyntax>())
            {
                ResourceDefinition definition = CreateResourceDefinition(
                    resourceElement,
                    document.Path,
                    aliases,
                    isApplication: root?.Name == "Application");
                resourceElements[resourceElement] = definition;
                if (definition.Kind == ResourceKind.Aspect && definition.Name is null && definition.TargetType is not null)
                {
                    if (scope.DefaultAspects.ContainsKey(definition.TargetType.MetadataName))
                    {
                        AddShapeDiagnostic(
                            resourceElement.NameToken.Span,
                            "Duplicate unnamed Aspect for target '" + definition.TargetType.Name + "' in the same resource scope.");
                    }
                    else
                    {
                        scope.DefaultAspects.Add(definition.TargetType.MetadataName, definition);
                    }

                    continue;
                }

                if (definition.Name is null)
                {
                    continue;
                }

                if (scope.NamedResources.ContainsKey(definition.Name))
                {
                    AddShapeDiagnostic(
                        definition.NameSpan,
                        "Duplicate resource Name '" + definition.Name + "' in the same scope.");
                    continue;
                }

                scope.NamedResources.Add(definition.Name, definition);
            }
        }
    }

    private void CollectApplicationResources()
    {
        foreach (CernealaDocument candidate in documents)
        {
            ElementSyntax? applicationRoot = candidate.Syntax.Children.OfType<ElementSyntax>().SingleOrDefault();
            if (applicationRoot?.Name != "Application")
            {
                continue;
            }

            Dictionary<string, NamespaceAlias> applicationAliases = new(StringComparer.Ordinal);
            ReadAliases(applicationRoot, applicationAliases);
            foreach (ElementSyntax property in candidate.Syntax.DescendantElements().Where(IsResourcePropertyElement))
            {
                foreach (ElementSyntax resource in property.Children.OfType<ElementSyntax>())
                {
                    ResourceDefinition definition = CreateResourceDefinition(
                        resource,
                        candidate.Path,
                        applicationAliases,
                        isApplication: true);
                    if (definition.Kind == ResourceKind.Aspect && definition.Name is null && definition.TargetType is not null)
                    {
                        if (!applicationDefaultAspects.Any(existing =>
                            existing.TargetType?.MetadataName == definition.TargetType.MetadataName))
                        {
                            applicationDefaultAspects.Add(definition);
                        }

                        continue;
                    }

                    if (definition.Name is not null && !applicationResources.ContainsKey(definition.Name))
                    {
                        applicationResources.Add(definition.Name, definition);
                    }
                }
            }
        }
    }

    private ResourceDefinition CreateResourceDefinition(
        ElementSyntax element,
        string sourcePath,
        IReadOnlyDictionary<string, NamespaceAlias> namespaceAliases,
        bool isApplication)
    {
        ResourceKind kind = element.Name switch
        {
            "Aspect" => ResourceKind.Aspect,
            "SolidColorBrush" or "LinearGradientBrush" or "RadialGradientBrush" or "ImageBrush" or "DrawingBrush" => ResourceKind.Brush,
            "ContentTemplate" => ResourceKind.ContentTemplate,
            "Tween" or "Spring" => ResourceKind.MotionSpec,
            "MotionClip" => ResourceKind.MotionClip,
            "PrismComposition" => ResourceKind.PrismComposition,
            _ => ResourceKind.Unsupported
        };
        AttributeSyntax? nameAttribute = FindAttribute(element, "Name");
        string? name = nameAttribute is null ? null : Unquote(nameAttribute.ValueToken.Text).Trim();
        if (name?.Length == 0)
        {
            name = null;
        }

        ILanguageTypeSymbol? type = kind switch
        {
            ResourceKind.Brush => compilation.FindType("Cerneala.UI.Media." + element.Name),
            ResourceKind.MotionSpec or ResourceKind.MotionClip or ResourceKind.PrismComposition =>
                compilation.FindType("System.Object"),
            _ => null
        };
        ILanguageTypeSymbol? targetType = null;
        if ((kind == ResourceKind.Aspect || kind == ResourceKind.MotionClip) &&
            FindAttribute(element, "TargetType") is AttributeSyntax targetAttribute)
        {
            targetType = ResolveTargetTypeReference(
                Unquote(targetAttribute.ValueToken.Text),
                namespaceAliases);
        }

        TextSpan nameSpan = nameAttribute is null ? element.NameToken.Span : AttributeContentSpan(nameAttribute);
        return new ResourceDefinition(
            name,
            kind,
            type,
            targetType,
            element,
            sourcePath,
            nameSpan,
            isApplication);
    }

    private void BuildTemplateContexts()
    {
        string source = document.Text.ToString();
        ElementSyntax[] elements = document.Syntax.DescendantElements().ToArray();
        foreach (DirectiveBlock block in FindDirectiveBlocks(source, "@template"))
        {
            ElementSyntax? owner = elements
                .Where(element => element.Span.Start <= block.KeywordSpan.Start && element.Span.End >= block.BodySpan.End)
                .OrderBy(element => element.Span.Length)
                .FirstOrDefault();
            if (owner is null)
            {
                continue;
            }

            ILanguageTypeSymbol? ownerType = resourceElements.TryGetValue(owner, out ResourceDefinition? aspect) &&
                aspect.Kind == ResourceKind.Aspect
                ? aspect.TargetType
                : GetElementType(owner, ReferenceEquals(owner, root));
            SemanticTemplateContext context = new(owner, ownerType, dataType: null, isContentTemplate: false, block.BodySpan);
            foreach (ElementSyntax element in elements.Where(element =>
                element.NameToken.Span.Start >= block.BodySpan.Start && element.NameToken.Span.Start < block.BodySpan.End))
            {
                templateContexts[element] = context;
            }
        }

        foreach (ElementSyntax templateElement in elements.Where(element => element.Name == "ContentTemplate"))
        {
            ILanguageTypeSymbol? dataType = FindAttribute(templateElement, "DataType") is AttributeSyntax attribute
                ? ResolveTypeReference(attribute)
                : null;
            ElementSyntax? owner = FindNearestElementOwner(templateElement);
            SemanticTemplateContext context = new(
                owner,
                owner is null ? null : GetElementType(owner, ReferenceEquals(owner, root)),
                dataType,
                isContentTemplate: true,
                templateElement.Span);
            foreach (ElementSyntax descendant in Descendants(templateElement))
            {
                templateContexts[descendant] = context;
            }
        }
    }

    private void BuildNameScopes()
    {
        SemanticNameScope documentScope = new("document", template: null);
        Dictionary<SemanticTemplateContext, SemanticNameScope> templateScopes = new();
        foreach (ElementSyntax element in document.Syntax.DescendantElements().OrderBy(candidate => candidate.Span.Start))
        {
            if (element.Kind == SyntaxKind.PropertyElement || resourceElements.ContainsKey(element) || element.Name == "ContentTemplate")
            {
                continue;
            }

            SemanticNameScope scope;
            if (templateContexts.TryGetValue(element, out SemanticTemplateContext? template))
            {
                if (!templateScopes.TryGetValue(template, out scope!))
                {
                    scope = new SemanticNameScope("template@" + template.Span.Start, template);
                    templateScopes.Add(template, scope);
                }
            }
            else
            {
                scope = documentScope;
            }

            nameScopes[element] = scope;
            AttributeSyntax? nameAttribute = FindAttribute(element, "Name");
            if (nameAttribute is null)
            {
                continue;
            }

            string name = Unquote(nameAttribute.ValueToken.Text).Trim();
            if (name.Length == 0)
            {
                AddShapeDiagnostic(AttributeContentSpan(nameAttribute), "Element Name cannot be empty.");
                continue;
            }

            if (template?.IsContentTemplate == true)
            {
                AddShapeDiagnostic(
                    AttributeContentSpan(nameAttribute),
                    "Named visual elements inside ContentTemplate are not supported because each realization owns a separate namescope.");
                continue;
            }

            if (scope.Elements.ContainsKey(name))
            {
                TextSpan span = AttributeContentSpan(nameAttribute);
                string message = "Duplicate element Name '" + name + "' in the same name scope.";
                if (template is not null && !template.IsContentTemplate)
                {
                    AddDiagnostic("CERNEALAUI012", span, Path.GetFileName(document.Path), message);
                }
                else
                {
                    AddShapeDiagnostic(span, message);
                }

                continue;
            }

            ILanguageTypeSymbol? type = GetElementType(element, ReferenceEquals(element, root));
            NamedElementDefinition definition = new(name, element, type, AttributeContentSpan(nameAttribute));
            scope.Elements.Add(name, definition);
            if (template is not null)
            {
                template.Parts[name] = definition;
            }

            symbols.Add(new CernealaSemanticSymbol(
                template is null ? CernealaSemanticSymbolKind.Name : CernealaSemanticSymbolKind.TemplatePart,
                name,
                type?.MetadataName ?? "System.Object",
                definition.Span,
                type,
                definitionLocation: new LanguageSourceLocation(document.Path, definition.Span)));
        }
    }

    private void BindResourceElement(
        ResourceDefinition resource,
        ILanguageTypeSymbol? dataType,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        switch (resource.Kind)
        {
            case ResourceKind.Aspect:
                BindAspectResource(resource, dataType, cancellationToken);
                return;
            case ResourceKind.ContentTemplate:
                AddShapeDiagnostic(
                    resource.Element.NameToken.Span,
                    "ContentTemplate cannot be declared in Resources. Declare it inline on a template property or inside ItemsControl.Templates.");
                return;
            case ResourceKind.MotionSpec:
            case ResourceKind.MotionClip:
            case ResourceKind.PrismComposition:
                BindEmbeddedResource(resource, cancellationToken);
                return;
            case ResourceKind.Unsupported:
                AddDiagnostic("CERNEALAUI002", resource.Element.NameToken.Span, resource.Element.Name);
                return;
        }

        symbols.Add(new CernealaSemanticSymbol(
            CernealaSemanticSymbolKind.Resource,
            resource.Name ?? resource.Element.Name,
            resource.Type?.MetadataName ?? "System.Object",
            resource.NameSpan,
            resource.Type,
            definitionLocation: resource.Location));
    }

    private void BindContentTemplate(
        ElementSyntax template,
        ILanguageTypeSymbol? inheritedDataType,
        CancellationToken cancellationToken)
    {
        AttributeSyntax? dataTypeAttribute = FindAttribute(template, "DataType");
        ILanguageTypeSymbol? dataType = dataTypeAttribute is null ? null : ResolveTypeReference(dataTypeAttribute);
        ElementSyntax? owner = FindNearestElementOwner(template);
        if (dataType is null && owner is not null && itemsSourceTypes.TryGetValue(owner, out ILanguageTypeSymbol? inferred))
        {
            dataType = inferred;
        }

        if (dataTypeAttribute is not null)
        {
            if (dataType is null)
            {
                AddBindingDiagnostic(
                    Unquote(dataTypeAttribute.ValueToken.Text),
                    AttributeContentSpan(dataTypeAttribute),
                    "ContentTemplate DataType must name an accessible type in the current compilation.");
            }
            else
            {
                symbols.Add(new CernealaSemanticSymbol(
                    CernealaSemanticSymbolKind.TypeReference,
                    dataType.Name,
                    dataType.MetadataName,
                    AttributeContentSpan(dataTypeAttribute),
                    dataType,
                    definitionLocation: dataType.Locations.FirstOrDefault()));
            }
        }

        string? name = FindAttribute(template, "Name") is AttributeSyntax nameAttribute
            ? Unquote(nameAttribute.ValueToken.Text).Trim()
            : null;
        string? key = FindAttribute(template, "Key") is AttributeSyntax keyAttribute
            ? Unquote(keyAttribute.ValueToken.Text)
            : null;
        string identity = (dataType?.MetadataName ?? "<null>") + "\0" + key;
        if (parents.TryGetValue(template, out ElementSyntax? collection) && collection is not null)
        {
            if (!contentTemplateKeys.TryGetValue(collection, out HashSet<string>? identities))
            {
                identities = new HashSet<string>(StringComparer.Ordinal);
                contentTemplateKeys.Add(collection, identities);
            }

            if (!identities.Add(identity))
            {
                AddShapeDiagnostic(template.NameToken.Span, "Duplicate ContentTemplate DataType/Key selection in ItemsControl.Templates.");
            }
        }

        foreach (AttributeSyntax attribute in template.Attributes.Where(attribute =>
            attribute.NameToken.Text is not "Name" and not "DataType" and not "Key" and not "Priority"))
        {
            AddShapeDiagnostic(attribute.NameToken.Span, "ContentTemplate supports only Name, DataType, Key, and Priority attributes.");
        }

        if (FindAttribute(template, "Priority") is AttributeSyntax priority &&
            !int.TryParse(Unquote(priority.ValueToken.Text), NumberStyles.Integer, CultureInfo.InvariantCulture, out _))
        {
            AddDiagnostic("CERNEALAUI004", priority.ValueToken.Span, "ContentTemplate", "Priority", Unquote(priority.ValueToken.Text));
        }

        symbols.Add(new CernealaSemanticSymbol(
            CernealaSemanticSymbolKind.ContentTemplate,
            name ?? key ?? dataType?.Name ?? "ContentTemplate",
            dataType?.MetadataName ?? "System.Object",
            template.NameToken.Span,
            dataType,
            value: key));
        if (templateContexts.Values.FirstOrDefault(context => context.Span.Equals(template.Span)) is SemanticTemplateContext context)
        {
            context.DataType = dataType ?? inheritedDataType;
        }

        ElementSyntax[] roots = template.Children.OfType<ElementSyntax>().ToArray();
        if (roots.Length != 1)
        {
            AddShapeDiagnostic(template.NameToken.Span, "ContentTemplate requires exactly one visual root.");
            return;
        }

        BindElement(roots[0], parentType: null, isRoot: false, dataType ?? inheritedDataType, cancellationToken);
    }

    private ElementSyntax? FindNearestElementOwner(ElementSyntax element)
    {
        ElementSyntax? current = parents.TryGetValue(element, out ElementSyntax? parent) ? parent : null;
        while (current is not null)
        {
            if (current.Kind != SyntaxKind.PropertyElement && current.Name != "ContentTemplate" && !resourceElements.ContainsKey(current))
            {
                return current;
            }

            current = parents.TryGetValue(current, out parent) ? parent : null;
        }

        return null;
    }

    private NamedElementDefinition? FindNamedElement(ElementSyntax source, string name)
    {
        SemanticNameScope? scope = nameScopes.TryGetValue(source, out SemanticNameScope? direct)
            ? direct
            : nameScopes
                .Where(pair => !templateContexts.ContainsKey(pair.Key))
                .Select(pair => pair.Value)
                .FirstOrDefault();
        return scope is not null && scope.Elements.TryGetValue(name, out NamedElementDefinition? definition)
            ? definition
            : null;
    }

    private ResourceDefinition? FindResource(ElementSyntax source, string name)
    {
        ElementSyntax? current = source;
        while (current is not null)
        {
            if (resourceScopes.TryGetValue(current, out SemanticResourceScope? scope) &&
                scope.NamedResources.TryGetValue(name, out ResourceDefinition? definition))
            {
                return definition;
            }

            current = parents.TryGetValue(current, out ElementSyntax? parent) ? parent : null;
        }

        return applicationResources.TryGetValue(name, out ResourceDefinition? application) ? application : null;
    }

    private ResourceDefinition? FindDefaultAspect(ElementSyntax source, ILanguageTypeSymbol type)
    {
        ElementSyntax? current = source;
        while (current is not null)
        {
            if (resourceScopes.TryGetValue(current, out SemanticResourceScope? scope))
            {
                ResourceDefinition? local = scope.DefaultAspects.Values.FirstOrDefault(candidate =>
                    candidate.TargetType is not null && type.IsOrDerivesFrom(candidate.TargetType.MetadataName));
                if (local is not null)
                {
                    return local;
                }
            }

            current = parents.TryGetValue(current, out ElementSyntax? parent) ? parent : null;
        }

        return applicationDefaultAspects.FirstOrDefault(candidate =>
            candidate.TargetType is not null && type.IsOrDerivesFrom(candidate.TargetType.MetadataName));
    }

    private void ApplyDefaultAspect(ElementSyntax element, ILanguageTypeSymbol type)
    {
        ResourceDefinition? aspect = FindDefaultAspect(element, type);
        if (aspect is null)
        {
            return;
        }

        symbols.Add(new CernealaSemanticSymbol(
            CernealaSemanticSymbolKind.AspectApplication,
            aspect.Name ?? aspect.TargetType?.Name ?? "Aspect",
            aspect.TargetType?.MetadataName ?? "System.Object",
            element.NameToken.Span,
            aspect.TargetType,
            definitionLocation: aspect.Location));
    }

    private static bool IsResourcePropertyElement(ElementSyntax element) =>
        element.Kind == SyntaxKind.PropertyElement && element.Name.EndsWith(".Resources", StringComparison.Ordinal);

    private bool IsSpecialElement(ElementSyntax element) =>
        element.Name is "Aspect" or "ContentTemplate" or "SolidColorBrush" or "LinearGradientBrush" or
            "RadialGradientBrush" or "ImageBrush" or "DrawingBrush" or "Tween" or "Spring" or
            "MotionClip" or "PrismComposition" || IsResourcePropertyElement(element);

    private static IEnumerable<ElementSyntax> Descendants(ElementSyntax element)
    {
        foreach (ElementSyntax child in element.Children.OfType<ElementSyntax>())
        {
            yield return child;
            foreach (ElementSyntax descendant in Descendants(child))
            {
                yield return descendant;
            }
        }
    }

    private static IReadOnlyList<DirectiveBlock> FindDirectiveBlocks(string source, string keyword)
    {
        List<DirectiveBlock> blocks = new();
        int search = 0;
        while ((search = source.IndexOf(keyword, search, StringComparison.Ordinal)) >= 0)
        {
            int opening = source.IndexOf('{', search + keyword.Length);
            if (opening < 0)
            {
                break;
            }

            int depth = 1;
            bool quoted = false;
            char quote = '\0';
            int position = opening + 1;
            for (; position < source.Length && depth > 0; position++)
            {
                char character = source[position];
                if (quoted)
                {
                    if (character == quote && source[position - 1] != '\\')
                    {
                        quoted = false;
                    }

                    continue;
                }

                if (character is '\'' or '"')
                {
                    quoted = true;
                    quote = character;
                }
                else if (character == '{')
                {
                    depth++;
                }
                else if (character == '}')
                {
                    depth--;
                }
            }

            int closing = depth == 0 ? position - 1 : source.Length;
            blocks.Add(new DirectiveBlock(
                new TextSpan(search, keyword.Length),
                new TextSpan(opening + 1, closing - opening - 1)));
            search = Math.Max(search + keyword.Length, closing);
        }

        return blocks;
    }

    private sealed class SemanticNameScope
    {
        public SemanticNameScope(string id, SemanticTemplateContext? template)
        {
            Id = id;
            Template = template;
        }

        public string Id { get; }

        public SemanticTemplateContext? Template { get; }

        public Dictionary<string, NamedElementDefinition> Elements { get; } = new(StringComparer.Ordinal);
    }

    private sealed class NamedElementDefinition
    {
        public NamedElementDefinition(string name, ElementSyntax element, ILanguageTypeSymbol? type, TextSpan span)
        {
            Name = name;
            Element = element;
            Type = type;
            Span = span;
        }

        public string Name { get; }

        public ElementSyntax Element { get; }

        public ILanguageTypeSymbol? Type { get; }

        public TextSpan Span { get; }
    }

    private sealed class SemanticResourceScope
    {
        public SemanticResourceScope(ElementSyntax owner)
        {
            Owner = owner;
        }

        public ElementSyntax Owner { get; }

        public Dictionary<string, ResourceDefinition> NamedResources { get; } = new(StringComparer.Ordinal);

        public Dictionary<string, ResourceDefinition> DefaultAspects { get; } = new(StringComparer.Ordinal);
    }

    private enum ResourceKind
    {
        Brush,
        Aspect,
        ContentTemplate,
        MotionSpec,
        MotionClip,
        PrismComposition,
        Unsupported
    }

    private sealed class ResourceDefinition
    {
        public ResourceDefinition(
            string? name,
            ResourceKind kind,
            ILanguageTypeSymbol? type,
            ILanguageTypeSymbol? targetType,
            ElementSyntax element,
            string path,
            TextSpan nameSpan,
            bool isApplication)
        {
            Name = name;
            Kind = kind;
            Type = type;
            TargetType = targetType;
            Element = element;
            Path = path;
            NameSpan = nameSpan;
            IsApplication = isApplication;
        }

        public string? Name { get; }

        public ResourceKind Kind { get; }

        public ILanguageTypeSymbol? Type { get; }

        public ILanguageTypeSymbol? TargetType { get; }

        public ElementSyntax Element { get; }

        public string Path { get; }

        public TextSpan NameSpan { get; }

        public bool IsApplication { get; }

        public LanguageSourceLocation Location => new(Path, NameSpan);
    }

    private sealed class SemanticTemplateContext
    {
        public SemanticTemplateContext(
            ElementSyntax? ownerElement,
            ILanguageTypeSymbol? ownerType,
            ILanguageTypeSymbol? dataType,
            bool isContentTemplate,
            TextSpan span)
        {
            OwnerElement = ownerElement;
            OwnerType = ownerType;
            DataType = dataType;
            IsContentTemplate = isContentTemplate;
            Span = span;
        }

        public ElementSyntax? OwnerElement { get; }

        public ILanguageTypeSymbol? OwnerType { get; }

        public ILanguageTypeSymbol? DataType { get; set; }

        public bool IsContentTemplate { get; }

        public TextSpan Span { get; }

        public Dictionary<string, NamedElementDefinition> Parts { get; } = new(StringComparer.Ordinal);
    }

    private readonly struct DirectiveBlock
    {
        public DirectiveBlock(TextSpan keywordSpan, TextSpan bodySpan)
        {
            KeywordSpan = keywordSpan;
            BodySpan = bodySpan;
        }

        public TextSpan KeywordSpan { get; }

        public TextSpan BodySpan { get; }
    }
}
