using System.Globalization;
using Cerneala.Language.Diagnostics;
using Cerneala.Language.Semantics.Symbols;
using Cerneala.Language.Syntax;
using Cerneala.Language.Text;

namespace Cerneala.Language.Semantics;

internal sealed partial class CernealaSemanticModel : IDisposable
{
    private static readonly string[] BuiltInNamespaces =
    [
        "Cerneala.UI.Controls",
        "Cerneala.UI.Controls.Primitives",
        "Cerneala.UI.Controls.Shapes",
        "Cerneala.UI.Elements",
        "Cerneala.UI.Layout.Panels",
        "Cerneala.UI.Media",
        "Cerneala.UI.Automation"
    ];

    private readonly CernealaDocument document;
    private readonly IReadOnlyCollection<CernealaDocument> documents;
    private readonly ILanguageCompilationSymbols compilation;
    private readonly AnalysisMode mode;
    private readonly List<CernealaSemanticSymbol> symbols = new();
    private readonly List<LanguageDiagnostic> diagnostics = new();
    private readonly Dictionary<string, NamespaceAlias> aliases = new(StringComparer.Ordinal);
    private readonly Dictionary<ElementSyntax, ElementSyntax?> parents = new();
    private readonly Dictionary<ElementSyntax, ILanguageTypeSymbol?> elementTypes = new();
    private readonly Dictionary<ElementSyntax, ILanguageTypeSymbol?> elementDataTypes = new();
    private readonly Dictionary<ElementSyntax, SemanticNameScope> nameScopes = new();
    private readonly Dictionary<ElementSyntax, SemanticResourceScope> resourceScopes = new();
    private readonly Dictionary<ElementSyntax, ResourceDefinition> resourceElements = new();
    private readonly Dictionary<ElementSyntax, SemanticTemplateContext> templateContexts = new();
    private readonly Dictionary<string, ResourceDefinition> applicationResources = new(StringComparer.Ordinal);
    private readonly List<ResourceDefinition> applicationDefaultAspects = new();
    private ElementSyntax? root;
    private ILanguageTypeSymbol? rootType;
    private bool disposed;

    public CernealaSemanticModel(
        CernealaDocument document,
        IReadOnlyCollection<CernealaDocument> documents,
        ILanguageCompilationSymbols compilation,
        AnalysisMode mode,
        CancellationToken cancellationToken)
    {
        this.document = document;
        this.documents = documents;
        this.compilation = compilation;
        this.mode = mode;
        Bind(cancellationToken);
        symbols.Sort(CompareSymbols);
        diagnostics.Sort((left, right) =>
        {
            int position = left.Span.Start.CompareTo(right.Span.Start);
            return position != 0 ? position : string.CompareOrdinal(left.Id, right.Id);
        });
    }

    public CernealaDocument Document => document;

    internal ILanguageCompilationSymbols Compilation => compilation;

    internal IReadOnlyCollection<CernealaDocument> Documents => documents;

    public IReadOnlyList<CernealaSemanticSymbol> Symbols
    {
        get
        {
            ThrowIfDisposed();
            return symbols;
        }
    }

    public IReadOnlyList<LanguageDiagnostic> Diagnostics
    {
        get
        {
            ThrowIfDisposed();
            return diagnostics;
        }
    }

    public CernealaSemanticSymbol? GetSymbolAt(int offset)
    {
        ThrowIfDisposed();
        CernealaSemanticSymbol? symbol = symbols
            .Where(symbol => symbol.Span.Contains(offset) ||
                symbol.Span.Length == 0 && symbol.Span.Start == offset)
            .OrderBy(symbol => symbol.Span.Length)
            .ThenByDescending(symbol => IsQuerySpecificKind(symbol.Kind))
            .ThenBy(symbol => symbol.Kind)
            .FirstOrDefault();
        if (symbol is not null || offset < 0 || offset >= document.Text.Length || document.Text[offset] != '$')
        {
            return symbol;
        }

        return symbols
            .Where(candidate => candidate.Span.Start == offset + 1 && IsDollarReferenceKind(candidate.Kind))
            .OrderBy(candidate => candidate.Span.Length)
            .ThenBy(candidate => candidate.Kind)
            .FirstOrDefault();
    }

    public ILanguageTypeSymbol? GetTypeAt(int offset) => GetSymbolAt(offset)?.TypeSymbol;

    public void Dispose()
    {
        disposed = true;
        symbols.Clear();
        diagnostics.Clear();
        aliases.Clear();
        parents.Clear();
        elementTypes.Clear();
        elementDataTypes.Clear();
        nameScopes.Clear();
        resourceScopes.Clear();
        resourceElements.Clear();
        templateContexts.Clear();
        applicationResources.Clear();
        applicationDefaultAspects.Clear();
        inlineAspectProperties.Clear();
        itemsSourceTypes.Clear();
        contentTemplateKeys.Clear();
        motionSpecs.Clear();
        motionClips.Clear();
        prismCompositions.Clear();
        prismApplications.Clear();
        boundEmbeddedResources.Clear();
        boundPrismApplications.Clear();
    }

    private void Bind(CancellationToken cancellationToken)
    {
        ElementSyntax[] roots = document.Syntax.Children.OfType<ElementSyntax>().ToArray();
        ElementSyntax? topLevelResources = roots.FirstOrDefault(element =>
            element.Name.Split(':').Last() == "Resources");
        if (topLevelResources is not null)
        {
            AddShapeDiagnostic(
                topLevelResources.Span,
                "Top-level Resources is not supported; declare resources through <RootType.Resources> on a UI element.");
            return;
        }

        TextSyntax? topLevelText = document.Syntax.Children
            .OfType<TextSyntax>()
            .FirstOrDefault(text =>
                text.Kind is not SyntaxKind.Comment &&
                !string.IsNullOrWhiteSpace(text.Token.Text));
        if (topLevelText is not null || roots.Length != 1)
        {
            AddMalformedMarkupDiagnostic(
                topLevelText?.Span ?? roots.FirstOrDefault()?.Span ?? new TextSpan(0, 0),
                "Markup must contain exactly one UI root element.");
            return;
        }

        foreach (SyntaxDiagnostic syntaxDiagnostic in document.Syntax.Diagnostics)
        {
            AddMalformedMarkupDiagnostic(syntaxDiagnostic.Span, syntaxDiagnostic.Message);
        }

        cancellationToken.ThrowIfCancellationRequested();
        root = roots[0];
        ReadAliases(root, aliases);
        BuildScopeGraph(root, cancellationToken);
        rootType = GetElementType(root, isRoot: true);
        PrepareMotionPrismResources(cancellationToken);
        ILanguageTypeSymbol? dataType = BindDataTypeAttribute(root, InferRootDataType());
        BindElement(root, parentType: null, isRoot: true, dataType, cancellationToken);
    }

    private void BindElement(
        ElementSyntax element,
        ILanguageTypeSymbol? parentType,
        bool isRoot,
        ILanguageTypeSymbol? dataType,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        elementDataTypes[element] = dataType;
        if (element.HasMissingTokens)
        {
            return;
        }

        if (inlineAspectProperties.TryGetValue(element, out ResourceDefinition? inlineAspect))
        {
            BindPropertyElement(element, parentType);
            BindAspectResource(inlineAspect, dataType, cancellationToken);
            return;
        }

        if (resourceElements.TryGetValue(element, out ResourceDefinition? resource))
        {
            BindResourceElement(resource, dataType, cancellationToken);
            return;
        }

        if (element.Name == "ContentTemplate")
        {
            BindContentTemplate(element, dataType, cancellationToken);
            return;
        }

        if (element.Kind == SyntaxKind.PropertyElement)
        {
            ILanguageMemberSymbol? property = BindPropertyElement(element, parentType);
            ILanguageTypeSymbol? propertyDataType = dataType;
            foreach (ElementSyntax child in element.Children.OfType<ElementSyntax>())
            {
                BindElement(child, property?.ValueType, isRoot: false, propertyDataType, cancellationToken);
            }

            return;
        }

        ILanguageTypeSymbol? type = GetElementType(element, isRoot);
        if (type is null && parentType is not null)
        {
            ILanguageTypeSymbol? candidate = ResolveMarkupType(element.Name);
            ILanguageTypeSymbol? itemType = parentType.CollectionElementType;
            if (candidate is not null &&
                (candidate.IsOrDerivesFrom(parentType.MetadataName.TrimEnd('?')) ||
                 itemType is not null && candidate.IsOrDerivesFrom(itemType.MetadataName.TrimEnd('?'))))
            {
                type = candidate;
                elementTypes[element] = candidate;
            }
        }

        if (type is null)
        {
            AddDiagnostic("CERNEALAUI002", element.NameToken.Span, element.Name);
            return;
        }

        AddContentOwner(element);
        symbols.Add(new CernealaSemanticSymbol(
            isRoot ? CernealaSemanticSymbolKind.RootType : CernealaSemanticSymbolKind.Element,
            element.Name,
            type.MetadataName,
            element.NameToken.Span,
            type,
            contentPropertyName: ResolveContentPropertyName(type)));

        ILanguageTypeSymbol? childDataType = dataType;
        foreach (AttributeSyntax attribute in element.Attributes)
        {
            ILanguageTypeSymbol? assignedDataType = BindAttribute(type, element, attribute, childDataType);
            if (attribute.NameToken.Text == "DataContext" && assignedDataType is not null)
            {
                childDataType = assignedDataType;
            }
        }

        BindElementEmbeddedSemantics(element, type, childDataType, cancellationToken);
        ApplyDefaultAspect(element, type);
        foreach (ElementSyntax child in element.Children.OfType<ElementSyntax>())
        {
            BindElement(child, type, isRoot: false, childDataType, cancellationToken);
        }
    }

    private ILanguageMemberSymbol? BindPropertyElement(ElementSyntax element, ILanguageTypeSymbol? parentType)
    {
        int separator = element.Name.LastIndexOf('.');
        string propertyName = separator < 0 ? element.Name : element.Name.Substring(separator + 1);
        ILanguageMemberSymbol? member = FindProperty(parentType, propertyName);
        if (member is null)
        {
            AddDiagnostic(
                "CERNEALAUI003",
                element.NameToken.Span,
                parentType?.Name ?? element.Name.Substring(0, Math.Max(0, separator)),
                propertyName);
            return null;
        }

        symbols.Add(new CernealaSemanticSymbol(
            CernealaSemanticSymbolKind.PropertyElement,
            propertyName,
            member.ValueTypeMetadataName,
            element.NameToken.Span,
            member.ValueType ?? parentType,
            member,
            isWritable: member.CanWrite));
        return member;
    }

    private ILanguageTypeSymbol? BindAttribute(
        ILanguageTypeSymbol type,
        ElementSyntax element,
        AttributeSyntax attribute,
        ILanguageTypeSymbol? dataType)
    {
        string name = attribute.NameToken.Text;
        if (name == "xmlns" || name.StartsWith("xmlns:", StringComparison.Ordinal) || name is "Name" or "DataType")
        {
            return null;
        }

        if (ReferenceEquals(element, root) && element.Name == "Application" &&
            name is "StartupWindow" or "ShutdownMode")
        {
            return null;
        }

        if (name == "Aspect")
        {
            BindAspectApplication(type, element, attribute, dataType);
            return null;
        }

        if (name == "MotionClip")
        {
            AddMotionDiagnostic(
                "CERNEALAUI020",
                AttributeContentSpan(attribute),
                "MotionClip resources cannot be assigned directly to a control; use @run instead.");
            return null;
        }

        string value = Unquote(attribute.ValueToken.Text);
        int separator = name.LastIndexOf('.');
        if (separator > 0)
        {
            BindAttachedAttribute(type, element, attribute, name, value, separator, dataType);
            return null;
        }

        ILanguageMemberSymbol? member = type.GetMembers(name)
            .FirstOrDefault(candidate => candidate.Kind is LanguageMemberKind.Property or LanguageMemberKind.Event);
        if (member is null)
        {
            AddDiagnostic("CERNEALAUI003", attribute.NameToken.Span, type.Name, name);
            return null;
        }

        if (member.Kind == LanguageMemberKind.Event)
        {
            symbols.Add(new CernealaSemanticSymbol(
                CernealaSemanticSymbolKind.Event,
                name,
                member.ValueTypeMetadataName,
                attribute.NameToken.Span,
                member.ValueType ?? type,
                member,
                value));
            return null;
        }

        if (TryBindPropertyValue(type, element, attribute, member, value, dataType, out ILanguageTypeSymbol? resultType))
        {
            symbols.Add(new CernealaSemanticSymbol(
                CernealaSemanticSymbolKind.Property,
                name,
                member.ValueTypeMetadataName,
                attribute.NameToken.Span,
                member.ValueType ?? type,
                member,
                value,
                isWritable: member.CanWrite));
            return resultType;
        }

        if (!TryConvertLiteral(value, member, out object? converted))
        {
            AddDiagnostic("CERNEALAUI004", attribute.ValueToken.Span, type.Name, name, value);
            return null;
        }

        symbols.Add(new CernealaSemanticSymbol(
            CernealaSemanticSymbolKind.Property,
            name,
            member.ValueTypeMetadataName,
            attribute.NameToken.Span,
            member.ValueType ?? type,
            member,
            converted,
            isWritable: member.CanWrite));
        return null;
    }

    private void BindAttachedAttribute(
        ILanguageTypeSymbol elementType,
        ElementSyntax element,
        AttributeSyntax attribute,
        string name,
        string value,
        int separator,
        ILanguageTypeSymbol? dataType)
    {
        string ownerName = name.Substring(0, separator);
        string propertyName = name.Substring(separator + 1);
        ILanguageTypeSymbol? owner = ResolveMarkupType(ownerName);
        ILanguageMemberSymbol? member = owner?.GetMembers("Set" + propertyName)
            .FirstOrDefault(candidate => candidate.Kind == LanguageMemberKind.Method && candidate.IsStatic);
        if (member is null)
        {
            AddDiagnostic("CERNEALAUI003", attribute.NameToken.Span, ownerName, propertyName);
            return;
        }

        if (TryBindPropertyValue(owner!, element, attribute, member, value, dataType, out _))
        {
            symbols.Add(new CernealaSemanticSymbol(
                CernealaSemanticSymbolKind.AttachedProperty,
                name,
                member.ValueTypeMetadataName,
                attribute.NameToken.Span,
                member.ValueType ?? elementType,
                member,
                value,
                isWritable: true));
            return;
        }

        if (!TryConvertLiteral(value, member, out object? converted))
        {
            AddDiagnostic("CERNEALAUI004", attribute.ValueToken.Span, ownerName, propertyName, value);
            return;
        }

        symbols.Add(new CernealaSemanticSymbol(
            CernealaSemanticSymbolKind.AttachedProperty,
            name,
            member.ValueTypeMetadataName,
            attribute.NameToken.Span,
            member.ValueType ?? elementType,
            member,
            converted,
            isWritable: true));
    }

    private void AddContentOwner(ElementSyntax element)
    {
        if (!parents.TryGetValue(element, out ElementSyntax? parent) || parent is null ||
            parent.Kind == SyntaxKind.PropertyElement || resourceElements.ContainsKey(element))
        {
            return;
        }

        ILanguageTypeSymbol? ownerType = GetElementType(parent, ReferenceEquals(parent, root));
        string? propertyName = ownerType is null ? null : ResolveContentPropertyName(ownerType);
        ILanguageMemberSymbol? member = propertyName is null ? null : FindProperty(ownerType, propertyName);
        if (propertyName is null)
        {
            return;
        }

        symbols.Add(new CernealaSemanticSymbol(
            CernealaSemanticSymbolKind.ContentOwner,
            propertyName,
            member?.ValueTypeMetadataName ?? "System.Object",
            element.NameToken.Span,
            member?.ValueType ?? ownerType,
            member,
            parent.Name));
    }

    private ILanguageTypeSymbol? GetElementType(ElementSyntax element, bool isRoot)
    {
        if (elementTypes.TryGetValue(element, out ILanguageTypeSymbol? type))
        {
            return type;
        }

        type = ResolveElementType(element, isRoot);
        elementTypes[element] = type;
        return type;
    }

    private ILanguageTypeSymbol? ResolveElementType(ElementSyntax element, bool isRoot)
    {
        ILanguageTypeSymbol? valueType = ResolveValueElementType(element);
        if (valueType is not null)
        {
            return valueType;
        }

        if (isRoot && element.Name is "Application" or "Window" or "UserControl")
        {
            string pairPath = CernealaDocumentPath.GetCompanionPath(document.Path);
            string expectedName = CernealaDocumentPath.GetLogicalName(document.Path);

            ILanguageTypeSymbol? pair = compilation.FindDeclaredTypeForFile(pairPath, expectedName);
            string expectedBase = element.Name switch
            {
                "Application" => "Cerneala.UI.Application",
                "Window" => "Cerneala.UI.Controls.Window",
                _ => "Cerneala.UI.Controls.UserControl"
            };
            if (pair is not null && pair.IsOrDerivesFrom(expectedBase))
            {
                return pair;
            }

            return compilation.FindType(expectedBase);
        }

        int separator = element.Name.IndexOf(':');
        if (separator > 0)
        {
            string prefix = element.Name.Substring(0, separator);
            string localName = element.Name.Substring(separator + 1);
            if (aliases.TryGetValue(prefix, out NamespaceAlias? alias))
            {
                ILanguageTypeSymbol? aliased = compilation.FindType(alias.Namespace + "." + localName);
                return aliased is not null &&
                    (alias.Assembly.Length == 0 || string.Equals(alias.Assembly, aliased.AssemblyName, StringComparison.Ordinal)) &&
                    IsUsableElementType(aliased)
                    ? aliased
                    : null;
            }

            return null;
        }

        return ResolveUnqualifiedType(element.Name) ?? ResolveBuiltInMarkupType(element.Name);
    }

    private ILanguageTypeSymbol? ResolveValueElementType(ElementSyntax element)
    {
        if (!parents.TryGetValue(element, out ElementSyntax? propertyElement) ||
            propertyElement?.Kind != SyntaxKind.PropertyElement ||
            !parents.TryGetValue(propertyElement, out ElementSyntax? owner) || owner is null)
        {
            return null;
        }

        ILanguageTypeSymbol? ownerType = GetElementType(owner, ReferenceEquals(owner, root));
        int separator = propertyElement.Name.LastIndexOf('.');
        string propertyName = separator < 0
            ? propertyElement.Name
            : propertyElement.Name.Substring(separator + 1);
        ILanguageTypeSymbol? expected = FindProperty(ownerType, propertyName)?.ValueType;
        ILanguageTypeSymbol? itemType = expected?.CollectionElementType;
        if (itemType is not null && string.Equals(itemType.Name, element.Name, StringComparison.Ordinal))
        {
            return itemType;
        }

        ILanguageTypeSymbol? builtIn = ResolveMarkupType(element.Name);
        if (builtIn is not null && expected is not null &&
            (builtIn.IsOrDerivesFrom(expected.MetadataName.TrimEnd('?')) ||
             itemType is not null && builtIn.IsOrDerivesFrom(itemType.MetadataName.TrimEnd('?'))))
        {
            return builtIn;
        }

        ILanguageTypeSymbol[] candidates = compilation.FindTypes(element.Name)
            .Where(candidate => expected is not null &&
                (candidate.IsOrDerivesFrom(expected.MetadataName.TrimEnd('?')) ||
                 itemType is not null && candidate.IsOrDerivesFrom(itemType.MetadataName.TrimEnd('?'))))
            .OrderBy(candidate => candidate.MetadataName, StringComparer.Ordinal)
            .ToArray();
        return candidates.Length == 1 ? candidates[0] : null;
    }

    private ILanguageTypeSymbol? InferRootDataType()
    {
        if (root?.Name is not ("Window" or "UserControl"))
        {
            return null;
        }

        for (ILanguageTypeSymbol? current = rootType; current is not null; current = current.BaseType)
        {
            if (string.Equals(current.Name, root.Name, StringComparison.Ordinal) && current.TypeArguments.Count == 1)
            {
                return current.TypeArguments[0];
            }
        }

        return null;
    }

    private ILanguageTypeSymbol? ResolveUnqualifiedType(string name)
    {
        if (name == "Application")
        {
            return compilation.FindType("Cerneala.UI.Application");
        }

        foreach (string @namespace in BuiltInNamespaces)
        {
            ILanguageTypeSymbol? builtIn = compilation.FindType(@namespace + "." + name);
            if (IsUsableElementType(builtIn))
            {
                return builtIn;
            }
        }

        return null;
    }

    private ILanguageTypeSymbol? ResolveMarkupType(string name)
    {
        ILanguageTypeSymbol? builtIn = ResolveBuiltInMarkupType(name);
        if (builtIn is not null)
        {
            return builtIn;
        }

        ILanguageTypeSymbol[] custom = compilation.FindTypes(name)
            .OrderBy(type => type.MetadataName, StringComparer.Ordinal)
            .ToArray();
        return custom.Length == 1 ? custom[0] : null;
    }

    private ILanguageTypeSymbol? ResolveBuiltInMarkupType(string name)
    {
        foreach (string @namespace in BuiltInNamespaces)
        {
            ILanguageTypeSymbol? type = compilation.FindType(@namespace + "." + name);
            if (type is not null)
            {
                return type;
            }
        }

        return null;
    }

    private ILanguageTypeSymbol? ResolveTypeReference(AttributeSyntax attribute) =>
        ResolveTypeReference(Unquote(attribute.ValueToken.Text), aliases);

    private ILanguageTypeSymbol? ResolveTargetTypeReference(AttributeSyntax attribute) =>
        ResolveTargetTypeReference(Unquote(attribute.ValueToken.Text), aliases);

    private ILanguageTypeSymbol? ResolveTargetTypeReference(
        string reference,
        IReadOnlyDictionary<string, NamespaceAlias> namespaceAliases)
    {
        reference = reference.Trim();
        int separator = reference.IndexOf(':');
        return separator > 0
            ? ResolveTypeReference(reference, namespaceAliases)
            : ResolveUnqualifiedTargetType(reference);
    }

    private ILanguageTypeSymbol? ResolveUnqualifiedTargetType(string name)
    {
        foreach (string @namespace in BuiltInNamespaces)
        {
            ILanguageTypeSymbol? builtIn = compilation.FindType(@namespace + "." + name);
            if (builtIn is not null && builtIn.IsClass &&
                builtIn.IsOrDerivesFrom("Cerneala.UI.Elements.UIElement"))
            {
                return builtIn;
            }
        }

        return null;
    }

    private ILanguageTypeSymbol? ResolveTypeReference(
        string reference,
        IReadOnlyDictionary<string, NamespaceAlias> namespaceAliases)
    {
        reference = reference.Trim();
        if (reference.StartsWith("global::", StringComparison.Ordinal))
        {
            reference = reference.Substring("global::".Length);
        }

        int separator = reference.IndexOf(':');
        if (separator > 0 && namespaceAliases.TryGetValue(reference.Substring(0, separator), out NamespaceAlias? alias))
        {
            ILanguageTypeSymbol? type = compilation.FindType(alias.Namespace + "." + reference.Substring(separator + 1));
            return type is not null &&
                (alias.Assembly.Length == 0 || string.Equals(alias.Assembly, type.AssemblyName, StringComparison.Ordinal))
                ? type
                : null;
        }

        ILanguageTypeSymbol? exact = compilation.FindType(reference);
        if (exact is not null)
        {
            return exact;
        }

        ILanguageTypeSymbol[] matches = compilation.FindTypes(reference)
            .OrderBy(type => type.MetadataName, StringComparer.Ordinal)
            .ToArray();
        return matches.Length == 1 ? matches[0] : null;
    }

    private ILanguageTypeSymbol? BindDataTypeAttribute(ElementSyntax element, ILanguageTypeSymbol? inherited)
    {
        AttributeSyntax? attribute = FindAttribute(element, "DataType");
        if (attribute is null)
        {
            return inherited;
        }

        ILanguageTypeSymbol? type = ResolveTypeReference(attribute);
        TextSpan span = AttributeContentSpan(attribute);
        if (type is null)
        {
            AddBindingDiagnostic(Unquote(attribute.ValueToken.Text), span, "DataType must name an accessible type in the current compilation.");
            return inherited;
        }

        symbols.Add(new CernealaSemanticSymbol(
            CernealaSemanticSymbolKind.TypeReference,
            type.Name,
            type.MetadataName,
            span,
            type,
            definitionLocation: type.Locations.FirstOrDefault()));
        return type;
    }

    private static bool IsUsableElementType(ILanguageTypeSymbol? type) =>
        type is not null && type.IsClass && !type.IsAbstract &&
        type.HasAccessibleParameterlessConstructor &&
        type.IsOrDerivesFrom("Cerneala.UI.Elements.UIElement");

    private static string? ResolveContentPropertyName(ILanguageTypeSymbol type)
    {
        if (type.ContentPropertyName is not null)
        {
            return type.ContentPropertyName;
        }

        if (type.IsOrDerivesFrom("Cerneala.UI.Controls.ContentControl") ||
            type.IsOrDerivesFrom("Cerneala.UI.Controls.Window") ||
            type.IsOrDerivesFrom("Cerneala.UI.Controls.UserControl"))
        {
            return "Content";
        }

        if (type.IsOrDerivesFrom("Cerneala.UI.Controls.Decorator"))
        {
            return "Child";
        }

        return type.IsOrDerivesFrom("Cerneala.UI.Controls.Panel") ||
            type.IsOrDerivesFrom("Cerneala.UI.Layout.Panels.Panel")
            ? "Children"
            : null;
    }

    private static void ReadAliases(ElementSyntax root, IDictionary<string, NamespaceAlias> destination)
    {
        foreach (AttributeSyntax attribute in root.Attributes)
        {
            string name = attribute.NameToken.Text;
            if (!name.StartsWith("xmlns:", StringComparison.Ordinal))
            {
                continue;
            }

            string value = Unquote(attribute.ValueToken.Text);
            if (!value.StartsWith("clr-namespace:", StringComparison.Ordinal))
            {
                continue;
            }

            string specification = value.Substring("clr-namespace:".Length);
            string[] parts = specification.Split(';');
            string assembly = parts.Skip(1)
                .FirstOrDefault(part => part.StartsWith("assembly=", StringComparison.Ordinal))?
                .Substring("assembly=".Length) ?? string.Empty;
            destination[name.Substring("xmlns:".Length)] = new NamespaceAlias(parts[0], assembly);
        }
    }

    private static ILanguageMemberSymbol? FindProperty(ILanguageTypeSymbol? type, string name) =>
        type?.GetMembers(name).FirstOrDefault(candidate => candidate.Kind == LanguageMemberKind.Property);

    private static AttributeSyntax? FindAttribute(ElementSyntax element, string name) =>
        element.Attributes.FirstOrDefault(attribute => attribute.NameToken.Text == name);

    private static bool TryConvertLiteral(string value, ILanguageMemberSymbol member, out object? converted)
    {
        string type = member.ValueTypeMetadataName.TrimEnd('?');
        if (member.EnumValues.Count > 0)
        {
            string? enumValue = member.EnumValues.FirstOrDefault(candidate =>
                string.Equals(candidate, value, StringComparison.OrdinalIgnoreCase));
            converted = enumValue;
            return enumValue is not null;
        }

        if (type is "bool" or "System.Boolean")
        {
            bool success = bool.TryParse(value, out bool parsed);
            converted = parsed;
            return success;
        }

        if (type is "int" or "System.Int32")
        {
            bool success = int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out int parsed);
            converted = parsed;
            return success;
        }

        if (type is "float" or "System.Single")
        {
            bool success = float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out float parsed) &&
                !float.IsNaN(parsed) && !float.IsInfinity(parsed);
            converted = parsed;
            return success;
        }

        if (type is "double" or "System.Double")
        {
            bool success = double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out double parsed) &&
                !double.IsNaN(parsed) && !double.IsInfinity(parsed);
            converted = parsed;
            return success;
        }

        converted = value;
        return true;
    }

    private void AddDiagnostic(string id, TextSpan span, params object[] arguments) =>
        diagnostics.Add(new LanguageDiagnostic(CernealaDiagnosticCatalog.Get(id), span, mode, arguments));

    private void AddShapeDiagnostic(TextSpan span, string message) =>
        AddDiagnostic("CERNEALAUI005", span, Path.GetFileName(document.Path), message);

    private void AddMalformedMarkupDiagnostic(TextSpan span, string message) =>
        AddDiagnostic("CERNEALAUI001", span, Path.GetFileName(document.Path), message);

    private void AddBindingDiagnostic(string path, TextSpan span, string message) =>
        AddDiagnostic("CERNEALAUI007", span, path, message);

    private static TextSpan AttributeContentSpan(AttributeSyntax attribute)
    {
        string token = attribute.ValueToken.Text;
        bool quoted = token.Length > 0 && token[0] is '\'' or '"';
        int start = attribute.ValueToken.Span.Start + (quoted ? 1 : 0);
        int length = token.Length - (quoted ? 2 : 0);
        return new TextSpan(start, Math.Max(0, length));
    }

    private static string Unquote(string value) =>
        value.Length >= 2 && value[0] == value[value.Length - 1] && value[0] is '\'' or '"'
            ? value.Substring(1, value.Length - 2)
            : value;

    private static int CompareSymbols(CernealaSemanticSymbol left, CernealaSemanticSymbol right)
    {
        int position = left.Span.Start.CompareTo(right.Span.Start);
        if (position != 0)
        {
            return position;
        }

        int length = left.Span.Length.CompareTo(right.Span.Length);
        if (length != 0)
        {
            return length;
        }

        int kind = left.Kind.CompareTo(right.Kind);
        return kind != 0 ? kind : string.CompareOrdinal(left.Name, right.Name);
    }

    private static bool IsQuerySpecificKind(CernealaSemanticSymbolKind kind) => kind is
        CernealaSemanticSymbolKind.BindingSource or CernealaSemanticSymbolKind.BindingSegment or
        CernealaSemanticSymbolKind.BindingMode or CernealaSemanticSymbolKind.ResourceReference or
        CernealaSemanticSymbolKind.TypeReference or CernealaSemanticSymbolKind.Aspect or
        CernealaSemanticSymbolKind.AspectAssignment or CernealaSemanticSymbolKind.AspectCondition or
        CernealaSemanticSymbolKind.AspectConditionProperty or
        CernealaSemanticSymbolKind.AspectApplication or CernealaSemanticSymbolKind.MotionDirective or
        CernealaSemanticSymbolKind.MotionTarget or CernealaSemanticSymbolKind.MotionEvent or
        CernealaSemanticSymbolKind.MotionProperty or CernealaSemanticSymbolKind.MotionSpec or
        CernealaSemanticSymbolKind.MotionComposition or CernealaSemanticSymbolKind.MotionLifecycle or
        CernealaSemanticSymbolKind.MotionParameter or CernealaSemanticSymbolKind.MotionHandle or
        CernealaSemanticSymbolKind.PrismDirective or CernealaSemanticSymbolKind.PrismComposition or
        CernealaSemanticSymbolKind.PrismNode or CernealaSemanticSymbolKind.PrismOperation or
        CernealaSemanticSymbolKind.PrismProperty or CernealaSemanticSymbolKind.PrismParameter or
        CernealaSemanticSymbolKind.PrismValue;

    private static bool IsDollarReferenceKind(CernealaSemanticSymbolKind kind) => kind is
        CernealaSemanticSymbolKind.ResourceReference or CernealaSemanticSymbolKind.BindingSource;

    private void ThrowIfDisposed()
    {
        if (disposed)
        {
            throw new ObjectDisposedException(nameof(CernealaSemanticModel));
        }
    }

    private sealed class NamespaceAlias
    {
        public NamespaceAlias(string @namespace, string assembly)
        {
            Namespace = @namespace;
            Assembly = assembly;
        }

        public string Namespace { get; }

        public string Assembly { get; }
    }
}
