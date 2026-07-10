using System;
using System.Collections.Immutable;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml;
using System.Xml.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;

namespace Cerneala.SourceGen;

[Generator]
public sealed partial class UiMarkupGenerator : IIncrementalGenerator
{
    private const string FragmentWrapperStart = "<__CernealaFragment>";
    private const string FragmentWrapperEnd = "</__CernealaFragment>";

    private static readonly DiagnosticDescriptor MalformedMarkup = new(
        "CERNEALAUI001",
        "Malformed UI markup",
        "Markup file '{0}' could not be parsed: {1}",
        "Cerneala.UiMarkup",
        DiagnosticSeverity.Error,
        true);

    private static readonly DiagnosticDescriptor UnsupportedElement = new(
        "CERNEALAUI002",
        "Unsupported UI markup element",
        "Markup element '{0}' is not supported by the source generator",
        "Cerneala.UiMarkup",
        DiagnosticSeverity.Error,
        true);

    private static readonly DiagnosticDescriptor UnsupportedProperty = new(
        "CERNEALAUI003",
        "Unsupported UI markup property",
        "Markup property '{0}.{1}' is not supported by the source generator",
        "Cerneala.UiMarkup",
        DiagnosticSeverity.Error,
        true);

    private static readonly DiagnosticDescriptor InvalidPropertyValue = new(
        "CERNEALAUI004",
        "Invalid UI markup property value",
        "Markup property '{0}.{1}' has invalid value '{2}'",
        "Cerneala.UiMarkup",
        DiagnosticSeverity.Error,
        true);

    private static readonly DiagnosticDescriptor InvalidDocumentShape = new(
        "CERNEALAUI005",
        "Invalid UI markup document shape",
        "Markup file '{0}' has invalid document shape: {1}",
        "Cerneala.UiMarkup",
        DiagnosticSeverity.Error,
        true);

    private static readonly DiagnosticDescriptor InvalidDirective = new(
        "CERNEALAUI006",
        "Invalid UI markup directive",
        "Markup directive in '{0}' is invalid: {1}",
        "Cerneala.UiMarkup",
        DiagnosticSeverity.Error,
        true);

    private static readonly DiagnosticDescriptor InvalidBindingSource = new(
        "CERNEALAUI007",
        "Invalid UI markup binding source",
        "Markup binding source '{0}' is invalid: {1}",
        "Cerneala.UiMarkup",
        DiagnosticSeverity.Error,
        true);

    private static readonly DiagnosticDescriptor InvalidUserControl = new(
        "CERNEALAUI008",
        "Invalid UserControl declaration",
        "UserControl markup file '{0}' is invalid: {1}",
        "Cerneala.UiMarkup",
        DiagnosticSeverity.Error,
        true);

    private static readonly DiagnosticDescriptor InvalidEventHandler = new(
        "CERNEALAUI009",
        "Invalid markup event handler",
        "Event handler '{0}' for '{1}.{2}' is invalid: {3}",
        "Cerneala.UiMarkup",
        DiagnosticSeverity.Error,
        true);

    private static readonly DiagnosticDescriptor InvalidWindow = new(
        "CERNEALAUI010",
        "Invalid Window declaration",
        "Window markup file '{0}' is invalid: {1}",
        "Cerneala.UiMarkup",
        DiagnosticSeverity.Error,
        true);

    private static readonly DiagnosticDescriptor InvalidWindowStartup = new(
        "CERNEALAUI011",
        "Invalid Window application startup",
        "Generated Window startup is invalid: {0}",
        "Cerneala.UiMarkup",
        DiagnosticSeverity.Error,
        true);

    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        IncrementalValuesProvider<MarkupSource> markupFiles = context.AdditionalTextsProvider
            .Where(static file => IsMarkupFile(file))
            .Select(static (file, cancellationToken) => new MarkupSource(
                file.Path,
                file.GetText(cancellationToken)?.ToString()));

        context.RegisterSourceOutput(
            markupFiles.Collect().Combine(context.CompilationProvider),
            static (sourceContext, input) => GenerateFiles(sourceContext, input.Left, input.Right));
    }

    private static bool IsMarkupFile(AdditionalText file)
    {
        return file.Path.EndsWith(".cui.xml", StringComparison.OrdinalIgnoreCase);
    }

    private static void GenerateFiles(SourceProductionContext context, ImmutableArray<MarkupSource> files, Compilation compilation)
    {
        string[] classNames = AssignClassNames(files);
        WindowPairResolution[] windowPairs = files
            .Select(file => ResolveWindowPair(context, file, compilation))
            .ToArray();
        int mainWindowCount = windowPairs.Count(resolution => resolution.Pair?.TypeSymbol.Name == "MainWindow");
        if (mainWindowCount > 1 && compilation.Options.OutputKind != OutputKind.DynamicallyLinkedLibrary)
        {
            context.ReportDiagnostic(Diagnostic.Create(
                InvalidWindowStartup,
                Location.None,
                "An executable project may contain only one paired Window class named 'MainWindow'."));
        }

        for (int i = 0; i < files.Length; i++)
        {
            WindowPairResolution windowPair = windowPairs[i];
            if (windowPair.HasCompanion)
            {
                if (windowPair.Pair is not null)
                {
                    bool generateStartup = mainWindowCount == 1 && windowPair.Pair.TypeSymbol.Name == "MainWindow";
                    GenerateWindowFile(context, files[i], classNames[i], compilation, windowPair.Pair, generateStartup);
                }

                continue;
            }

            UserControlPairResolution pair = ResolveUserControlPair(context, files[i], compilation);
            if (pair.HasCompanion)
            {
                if (pair.Pair is not null)
                {
                    GenerateUserControlFile(context, files[i], classNames[i], compilation, pair.Pair);
                }

                continue;
            }

            GenerateFile(context, files[i], classNames[i], compilation);
        }
    }

    private static string[] AssignClassNames(ImmutableArray<MarkupSource> files)
    {
        string[] classNames = files.Select(file => CreateClassName(file.Path)).ToArray();
        foreach (var group in classNames.Select((name, index) => new { name, index }).GroupBy(item => item.name, StringComparer.Ordinal))
        {
            if (group.Count() == 1)
            {
                continue;
            }

            foreach (int index in group.Select(item => item.index))
            {
                classNames[index] = CreateDisambiguatedClassName(files[index].Path);
            }
        }

        foreach (var group in classNames.Select((name, index) => new { name, index }).GroupBy(item => item.name, StringComparer.Ordinal))
        {
            if (group.Count() == 1)
            {
                continue;
            }

            foreach (int index in group.Select(item => item.index))
            {
                classNames[index] = classNames[index] + "_" + Fnv1a32(files[index].Path.Replace('\\', '/').ToUpperInvariant()).ToString("x8", CultureInfo.InvariantCulture);
            }
        }

        return classNames;
    }

    private static void GenerateFile(SourceProductionContext context, MarkupSource file, string className, Compilation compilation)
    {
        if (file.Text is null)
        {
            return;
        }

        ParsedDocument parsed = ParseDocument(file);
        if (parsed.Diagnostic is not null)
        {
            context.ReportDiagnostic(parsed.Diagnostic);
            return;
        }

        MarkupDocument document = parsed.Document!;
        XAttribute? nestedDataType = document.Root.Descendants()
            .Select(element => element.Attribute("DataType"))
            .FirstOrDefault(attribute => attribute is not null);
        if (nestedDataType is not null)
        {
            context.ReportDiagnostic(Diagnostic.Create(
                InvalidBindingSource,
                CreateLocation(file, nestedDataType),
                nestedDataType.Value,
                "DataType is allowed only on the root UI element."));
            return;
        }

        INamedTypeSymbol? dataType = ResolveDataType(context, file, document, compilation);
        if (document.Root.Attribute("DataType") is not null && dataType is null)
        {
            return;
        }

        GenerationScope scope = new(context, file, document, compilation, dataType);
        string rootVariable = scope.EmitElement(document.Root);
        if (scope.HasErrors)
        {
            return;
        }

        StringBuilder source = new();
        source.AppendLine("// <auto-generated />");
        source.AppendLine("#nullable enable");
        source.AppendLine("namespace Cerneala.GeneratedUi;");
        source.AppendLine();
        source.Append("public static partial class ").Append(className).AppendLine("Factory");
        source.AppendLine("{");
        source.AppendLine("    public static global::Cerneala.UI.Elements.UIElement Create()");
        source.AppendLine("    {");
        source.AppendLine("        return CreateCore(null);");
        source.AppendLine("    }");
        if (dataType is not null)
        {
            source.AppendLine();
            source.Append("    public static global::Cerneala.UI.Elements.UIElement Create(").Append(scope.DataTypeCode).AppendLine(" dataContext)");
            source.AppendLine("    {");
            source.AppendLine("        return CreateCore(dataContext);");
            source.AppendLine("    }");
        }

        source.AppendLine();
        source.AppendLine("    private static global::Cerneala.UI.Elements.UIElement CreateCore(object? dataContext)");
        source.AppendLine("    {");
        foreach (string line in scope.Lines)
        {
            source.Append("        ").AppendLine(line);
        }

        source.Append("        ").Append(rootVariable).AppendLine(".DataContext = dataContext;");
        foreach (string line in scope.PostLines)
        {
            source.Append("        ").AppendLine(line);
        }

        source.Append("        return ").Append(rootVariable).AppendLine(";");
        source.AppendLine("    }");
        source.AppendLine();
        source.AppendLine("    public static global::Cerneala.UI.Markup.GeneratedUiFactory AsGeneratedFactory()");
        source.AppendLine("    {");
        source.AppendLine("        return new global::Cerneala.UI.Markup.GeneratedUiFactory(Create);");
        source.AppendLine("    }");
        if (dataType is not null)
        {
            source.AppendLine();
            source.Append("    public static global::Cerneala.UI.Markup.GeneratedUiFactory AsGeneratedFactory(").Append(scope.DataTypeCode).AppendLine(" dataContext)");
            source.AppendLine("    {");
            source.AppendLine("        return new global::Cerneala.UI.Markup.GeneratedUiFactory(() => Create(dataContext));");
            source.AppendLine("    }");
        }
        source.AppendLine("}");

        string hintName = CreateHintName(file.Path, className);
        context.AddSource(hintName, SourceText.From(source.ToString(), Encoding.UTF8));
    }

    private static INamedTypeSymbol? ResolveDataType(
        SourceProductionContext context,
        MarkupSource file,
        MarkupDocument document,
        Compilation compilation)
    {
        XAttribute? attribute = document.Root.Attribute("DataType");
        if (attribute is null)
        {
            return null;
        }

        string metadataName = attribute.Value.Trim();
        if (metadataName.StartsWith("global::", StringComparison.Ordinal))
        {
            metadataName = metadataName.Substring("global::".Length);
        }

        INamedTypeSymbol? type = compilation.GetTypeByMetadataName(metadataName);
        if (type is null || type.DeclaredAccessibility is not Accessibility.Public and not Accessibility.Internal)
        {
            context.ReportDiagnostic(Diagnostic.Create(
                InvalidBindingSource,
                CreateLocation(file, attribute),
                attribute.Value,
                "DataType must name an accessible type in the current compilation."));
            return null;
        }

        return type;
    }

    private static ParsedDocument ParseDocument(MarkupSource file)
    {
        try
        {
            List<XElement> elements = [];
            string markup = StripXmlDeclarationPreservingPositions(file.Text ?? string.Empty);
            char comparatorPlaceholder = FindDirectiveComparatorPlaceholder(markup);
            markup = ProtectDirectiveComparators(markup, comparatorPlaceholder);
            XDocument fragment = XDocument.Parse(
                FragmentWrapperStart + markup + FragmentWrapperEnd,
                LoadOptions.SetLineInfo | LoadOptions.PreserveWhitespace);
            RestoreDirectiveComparators(fragment, comparatorPlaceholder);

            foreach (XNode node in fragment.Root!.Nodes())
            {
                if (node is XElement element)
                {
                    elements.Add(element);
                    continue;
                }

                if (node is XText text && !string.IsNullOrWhiteSpace(text.Value))
                {
                    return new ParsedDocument(
                        null,
                        Diagnostic.Create(
                            MalformedMarkup,
                            CreateLocation(file, text),
                            Path.GetFileName(file.Path),
                            "Markup must contain exactly one UI root element."));
                }
            }

            XElement? topLevelResources = elements.FirstOrDefault(element => element.Name.LocalName == "Resources");
            if (topLevelResources is not null)
            {
                return InvalidShape(
                    file,
                    topLevelResources,
                    "Top-level Resources is not supported; declare resources through <RootType.Resources> on a UI element.");
            }

            if (elements.Count != 1)
            {
                return new ParsedDocument(
                    null,
                    Diagnostic.Create(
                        MalformedMarkup,
                        CreateLocation(file, elements.FirstOrDefault() ?? new XElement("Missing")),
                        Path.GetFileName(file.Path),
                        "Markup must contain exactly one UI root element."));
            }

            return new ParsedDocument(new MarkupDocument(elements[0]), null);
        }
        catch (XmlException ex)
        {
            return new ParsedDocument(null, Diagnostic.Create(MalformedMarkup, CreateLocation(file, ex.LineNumber, ex.LinePosition), Path.GetFileName(file.Path), ex.Message));
        }
    }

    private static string ProtectDirectiveComparators(string markup, char placeholder)
    {
        return Regex.Replace(
            markup,
            "(@if\\s+value\\s*)<",
            match => match.Groups[1].Value + placeholder,
            RegexOptions.CultureInvariant);
    }

    private static void RestoreDirectiveComparators(XDocument document, char placeholder)
    {
        foreach (XText text in document.DescendantNodes().OfType<XText>())
        {
            if (text.Value.IndexOf(placeholder) >= 0)
            {
                text.Value = text.Value.Replace(placeholder, '<');
            }
        }
    }

    private static char FindDirectiveComparatorPlaceholder(string markup)
    {
        for (char candidate = '\uE000'; candidate <= '\uF8FF'; candidate++)
        {
            if (markup.IndexOf(candidate) < 0)
            {
                return candidate;
            }
        }

        throw new XmlException("Markup exhausts the private-use characters reserved for directive parsing.");
    }

    private static ParsedDocument InvalidShape(MarkupSource file, object locationSource, string message)
    {
        return new ParsedDocument(null, Diagnostic.Create(InvalidDocumentShape, CreateLocation(file, locationSource), Path.GetFileName(file.Path), message));
    }

    private static string StripXmlDeclarationPreservingPositions(string text)
    {
        if (!text.StartsWith("<?xml", StringComparison.OrdinalIgnoreCase))
        {
            return text;
        }

        int end = text.IndexOf("?>", StringComparison.Ordinal);
        if (end < 0)
        {
            return text;
        }

        StringBuilder builder = new(text);
        for (int i = 0; i < end + 2; i++)
        {
            if (builder[i] != '\r' && builder[i] != '\n')
            {
                builder[i] = ' ';
            }
        }

        return builder.ToString();
    }

    private static string CreateClassName(string path)
    {
        string rawName = Path.GetFileNameWithoutExtension(Path.GetFileNameWithoutExtension(path));
        return CreateIdentifier(rawName);
    }

    private static string CreateDisambiguatedClassName(string path)
    {
        string? directoryName = Path.GetDirectoryName(path);
        string? parentName = string.IsNullOrEmpty(directoryName) ? null : Path.GetFileName(directoryName);
        string baseName = Path.GetFileNameWithoutExtension(Path.GetFileNameWithoutExtension(path));
        return string.IsNullOrEmpty(parentName)
            ? CreateClassName(path)
            : CreateIdentifier(parentName + "-" + baseName);
    }

    private static string CreateHintName(string path, string className)
    {
        string stableSuffix = Fnv1a32(path.Replace('\\', '/').ToUpperInvariant()).ToString("x8", CultureInfo.InvariantCulture);
        return className + "Factory." + stableSuffix + ".g.cs";
    }

    private static string CreateIdentifier(string rawName)
    {
        StringBuilder builder = new();
        bool capitalizeNext = true;
        foreach (char character in rawName)
        {
            if (!char.IsLetterOrDigit(character) && character != '_')
            {
                capitalizeNext = true;
                continue;
            }

            char value = builder.Length == 0 && char.IsDigit(character) ? '_' : character;
            builder.Append(capitalizeNext ? char.ToUpperInvariant(value) : value);
            capitalizeNext = false;
        }

        return builder.Length == 0 ? "GeneratedUi" : builder.ToString();
    }

    private static uint Fnv1a32(string value)
    {
        const uint offset = 2166136261;
        const uint prime = 16777619;
        uint hash = offset;
        foreach (char character in value)
        {
            hash ^= character;
            hash *= prime;
        }

        return hash;
    }

    private readonly struct MarkupSource
    {
        public MarkupSource(string path, string? text)
        {
            Path = path;
            Text = text;
        }

        public string Path { get; }

        public string? Text { get; }
    }

    private sealed class MarkupDocument
    {
        public MarkupDocument(XElement root)
        {
            Root = root;
        }

        public XElement Root { get; }
    }

    private sealed class ParsedDocument
    {
        public ParsedDocument(MarkupDocument? document, Diagnostic? diagnostic)
        {
            Document = document;
            Diagnostic = diagnostic;
        }

        public MarkupDocument? Document { get; }

        public Diagnostic? Diagnostic { get; }
    }

    private sealed partial class GenerationScope
    {
        private readonly SourceProductionContext context;
        private readonly MarkupSource file;
        private readonly MarkupDocument document;
        private readonly Compilation compilation;
        private readonly INamedTypeSymbol? dataType;
        private readonly UserControlPair? userControlPair;
        private readonly bool reactiveDocument;
        private int nextId;

        public GenerationScope(
            SourceProductionContext context,
            MarkupSource file,
            MarkupDocument document,
            Compilation compilation,
            INamedTypeSymbol? dataType,
            UserControlPair? userControlPair = null)
        {
            this.context = context;
            this.file = file;
            this.document = document;
            this.compilation = compilation;
            this.dataType = dataType;
            this.userControlPair = userControlPair;
            currentLines = Lines;
            currentPostLines = PostLines;

            ReadResources();
            ReadInlineAspects();
            reactiveDocument = allAspects.Any(aspect => aspect.Conditions.Count > 0) ||
                document.Root.DescendantsAndSelf().Any(element =>
                    GetDirectiveContent(element, allowAssignments: false, allowElements: true).HasDirectives);
        }

        public List<string> Lines { get; } = new();

        public List<string> PostLines { get; } = new();

        public string? DataTypeCode => dataType?.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);

        public bool HasErrors { get; private set; }

        public IReadOnlyList<NamedElementMember> NamedElementMembers => namedElementMembers;

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
            DrawColor,
            Enum,
            Unsupported
        }

        private enum NamedSymbolKind
        {
            Element,
            SolidColorBrush,
            Aspect
        }

        private sealed class PropertySpec
        {
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

            public ITypeSymbol ValueType { get; }

            public string ValueTypeCode => ValueType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);

            public ITypeSymbol LiteralType => UnwrapNullable(ValueType);

            public bool Assignable { get; }
        }

        private sealed class GeneratedExpression
        {
            public GeneratedExpression(string code, MarkupValueKind kind)
            {
                Code = code;
                Kind = kind;
            }

            public string Code { get; }

            public MarkupValueKind Kind { get; }
        }

        private sealed class NamedSymbol
        {
            public NamedSymbol(string name, NamedSymbolKind kind, object source)
            {
                Name = name;
                Kind = kind;
                Source = source;
            }

            public string Name { get; }

            public NamedSymbolKind Kind { get; }

            public object Source { get; }
        }

        private sealed class SolidColorBrushResource
        {
            public SolidColorBrushResource(string name, string variable, string colorExpression, DrawColorLiteral color, XElement source)
            {
                Name = name;
                Variable = variable;
                ColorExpression = colorExpression;
                Color = color;
                Source = source;
            }

            public string Name { get; }

            public string Variable { get; }

            public string ColorExpression { get; }

            public DrawColorLiteral Color { get; }

            public XElement Source { get; }
        }

        private sealed class AspectResource
        {
            public AspectResource(
                string? name,
                string targetName,
                IReadOnlyList<AspectPropertyAssignment> assignments,
                IReadOnlyList<DirectiveWhenNode> conditions,
                XElement source,
                bool isInline = false)
            {
                Name = name;
                TargetName = targetName;
                Assignments = assignments;
                Conditions = conditions;
                Source = source;
                IsInline = isInline;
            }

            public string? Name { get; }

            public string TargetName { get; }

            public IReadOnlyList<AspectPropertyAssignment> Assignments { get; }

            public IReadOnlyList<DirectiveWhenNode> Conditions { get; }

            public XElement Source { get; }

            public bool IsInline { get; }

            public string? RuntimeVariable { get; set; }
        }

        private sealed class AspectPropertyAssignment
        {
            public AspectPropertyAssignment(string propertyName, string rawValue, bool isReference, XObject source)
            {
                PropertyName = propertyName;
                RawValue = rawValue;
                IsReference = isReference;
                Source = source;
            }

            public string PropertyName { get; }

            public string RawValue { get; }

            public bool IsReference { get; }

            public XObject Source { get; }
        }

        private sealed class ResourceScope
        {
            public ResourceScope(XElement owner)
            {
                Owner = owner;
            }

            public XElement Owner { get; }

            public Dictionary<string, NamedSymbol> NamedResources { get; } = new(StringComparer.Ordinal);

            public Dictionary<string, AspectResource> DefaultAspectsByTarget { get; } = new(StringComparer.Ordinal);

            public List<object> RuntimeResources { get; } = [];
        }

        private readonly struct DrawColorLiteral
        {
            public DrawColorLiteral(byte r, byte g, byte b, byte a)
            {
                R = r;
                G = g;
                B = b;
                A = a;
            }

            public byte R { get; }

            public byte G { get; }

            public byte B { get; }

            public byte A { get; }

            public string ToExpression()
            {
                return A == 255
                    ? "new global::Cerneala.Drawing.DrawColor(" + R + ", " + G + ", " + B + ")"
                    : "new global::Cerneala.Drawing.DrawColor(" + R + ", " + G + ", " + B + ", " + A + ")";
            }
        }

        private readonly Dictionary<string, NamedSymbol> symbols = new(StringComparer.Ordinal);
        private readonly Dictionary<XElement, ResourceScope> resourceScopes = new();
        private readonly Dictionary<XElement, ResourceScope> resourcePropertyScopes = new();
        private readonly Dictionary<XElement, AspectResource> inlineAspects = new();
        private readonly List<AspectResource> allAspects = [];
        private readonly Dictionary<XElement, DirectiveParseResult> directiveContent = new();
        private readonly Dictionary<string, INamedTypeSymbol> resolvedElementTypes = new(StringComparer.Ordinal);
        private readonly Dictionary<string, PropertySpec> resolvedProperties = new(StringComparer.Ordinal);
        private readonly Dictionary<string, IReadOnlyList<NamedElementMember>> conditionalFactoryMembers = new(StringComparer.Ordinal);
        private readonly Stack<List<NamedElementMember>> conditionalMemberScopes = new();
        private readonly List<NamedElementMember> namedElementMembers = [];
        private List<string> currentLines;
        private List<string> currentPostLines;
        private int nextReactiveId;
        private int nextResourceId;

        private void ReadResources()
        {
            XElement[] owners = document.Root.DescendantsAndSelf().ToArray();
            foreach (XElement owner in owners)
            {
                string expectedName = owner.Name.LocalName + ".Resources";
                XElement[] resourceProperties = owner.Elements()
                    .Where(element => element.Name.LocalName.EndsWith(".Resources", StringComparison.Ordinal))
                    .ToArray();
                XElement[] matching = resourceProperties
                    .Where(element => string.Equals(element.Name.LocalName, expectedName, StringComparison.Ordinal))
                    .ToArray();

                foreach (XElement invalid in resourceProperties.Where(element => !matching.Contains(element)))
                {
                    Report(
                        InvalidDocumentShape,
                        invalid,
                        Path.GetFileName(file.Path),
                        "Resource property element '" + invalid.Name.LocalName + "' must match its owner tag '" + expectedName + "'.");
                    invalid.Remove();
                }

                if (matching.Length > 1)
                {
                    Report(
                        InvalidDocumentShape,
                        matching[1],
                        Path.GetFileName(file.Path),
                        "Element '" + owner.Name.LocalName + "' may declare only one Resources property element.");
                    foreach (XElement duplicate in matching.Skip(1))
                    {
                        duplicate.Remove();
                    }
                }

                if (matching.Length == 0)
                {
                    continue;
                }

                XElement resources = matching[0];
                if (resources.HasAttributes || resources.Nodes().OfType<XText>().Any(text => !string.IsNullOrWhiteSpace(text.Value)))
                {
                    Report(
                        InvalidDocumentShape,
                        resources,
                        Path.GetFileName(file.Path),
                        "A Resources property element accepts only resource declarations.");
                }

                ResourceScope scope = new(owner);
                resourceScopes.Add(owner, scope);
                resourcePropertyScopes.Add(resources, scope);
                foreach (XElement resource in resources.Elements())
                {
                    switch (resource.Name.LocalName)
                    {
                        case "SolidColorBrush":
                            ReadSolidColorBrush(scope, resource);
                            break;
                        case "Aspect":
                            ReadAspect(scope, resource);
                            break;
                        default:
                            Report(UnsupportedElement, resource, resource.Name.LocalName);
                            break;
                    }
                }

                resources.Remove();
            }
        }

        private void ReadSolidColorBrush(ResourceScope scope, XElement resource)
        {
            string? name = RequiredName(resource);
            if (name is null)
            {
                return;
            }

            if (scope.NamedResources.ContainsKey(name))
            {
                Report(InvalidDocumentShape, resource, Path.GetFileName(file.Path), "Duplicate resource Name '" + name + "' in the same scope.");
                return;
            }

            XAttribute? colorAttribute = resource.Attribute("Color");
            if (colorAttribute is null || ParseHexColor(colorAttribute.Value) is not DrawColorLiteral color)
            {
                Report(InvalidPropertyValue, (object?)colorAttribute ?? resource, "SolidColorBrush", "Color", colorAttribute?.Value ?? string.Empty);
                return;
            }

            string variable = CreateIdentifier(name) + "Resource" + nextResourceId.ToString(CultureInfo.InvariantCulture);
            nextResourceId++;
            SolidColorBrushResource brush = new(name, variable, color.ToExpression(), color, resource);
            scope.NamedResources.Add(name, new NamedSymbol(name, NamedSymbolKind.SolidColorBrush, brush));
            scope.RuntimeResources.Add(brush);
            currentLines.Add("global::Cerneala.UI.Media.SolidColorBrush " + variable + " = new(" + brush.ColorExpression + ");");
        }

        private void ReadAspect(ResourceScope scope, XElement resource)
        {
            string targetName = resource.Attribute("Target")?.Value ?? string.Empty;
            if (string.IsNullOrWhiteSpace(targetName))
            {
                Report(InvalidPropertyValue, resource, "Aspect", "Target", targetName);
                return;
            }

            targetName = targetName.Trim();
            if (ResolveElementType(targetName) is null)
            {
                Report(UnsupportedElement, resource, targetName);
                return;
            }

            if (!TryParseAspectBody(resource, out List<AspectPropertyAssignment> assignments, out List<DirectiveWhenNode> conditions))
            {
                return;
            }

            string? name = resource.Attribute("Name")?.Value;
            AspectResource aspect = new(string.IsNullOrWhiteSpace(name) ? null : name, targetName, assignments, conditions, resource);
            allAspects.Add(aspect);
            if (aspect.Name is null)
            {
                if (scope.DefaultAspectsByTarget.ContainsKey(targetName))
                {
                    Report(InvalidDocumentShape, resource, Path.GetFileName(file.Path), "Duplicate unnamed Aspect for target '" + targetName + "' in the same resource scope.");
                    return;
                }

                scope.DefaultAspectsByTarget.Add(targetName, aspect);
                scope.RuntimeResources.Add(aspect);
                return;
            }

            if (scope.NamedResources.ContainsKey(aspect.Name))
            {
                Report(InvalidDocumentShape, resource, Path.GetFileName(file.Path), "Duplicate resource Name '" + aspect.Name + "' in the same scope.");
                return;
            }

            scope.NamedResources.Add(aspect.Name, new NamedSymbol(aspect.Name, NamedSymbolKind.Aspect, aspect));
            scope.RuntimeResources.Add(aspect);
        }

        private void ReadInlineAspects()
        {
            XElement[] owners = document.Root.DescendantsAndSelf().ToArray();
            INamedTypeSymbol? uiElementType = compilation.GetTypeByMetadataName("Cerneala.UI.Elements.UIElement");
            foreach (XElement owner in owners.Where(element => !element.Name.LocalName.EndsWith(".Aspect", StringComparison.Ordinal)))
            {
                string expectedName = owner.Name.LocalName + ".Aspect";
                XElement[] propertyElements = owner.Elements()
                    .Where(element => element.Name.LocalName.EndsWith(".Aspect", StringComparison.Ordinal))
                    .ToArray();
                XElement[] matching = propertyElements
                    .Where(element => string.Equals(element.Name.LocalName, expectedName, StringComparison.Ordinal))
                    .ToArray();

                foreach (XElement invalid in propertyElements.Where(element => !matching.Contains(element)))
                {
                    Report(
                        InvalidDocumentShape,
                        invalid,
                        Path.GetFileName(file.Path),
                        "Aspect property element '" + invalid.Name.LocalName + "' must match its owner tag '" + expectedName + "'.");
                    invalid.Remove();
                }

                if (matching.Length > 1)
                {
                    Report(
                        InvalidDocumentShape,
                        matching[1],
                        Path.GetFileName(file.Path),
                        "Element '" + owner.Name.LocalName + "' may declare only one Aspect property element.");
                    foreach (XElement duplicate in matching.Skip(1))
                    {
                        duplicate.Remove();
                    }
                }

                if (matching.Length == 0)
                {
                    continue;
                }

                XElement inline = matching[0];
                if (owner.Attribute("Aspect") is XAttribute aspectAttribute)
                {
                    Report(
                        InvalidDocumentShape,
                        aspectAttribute,
                        Path.GetFileName(file.Path),
                        "Element '" + owner.Name.LocalName + "' cannot combine an Aspect attribute with an inline Aspect property element.");
                }

                INamedTypeSymbol? ownerType = ResolvePropertyOwnerType(owner.Name.LocalName, ReferenceEquals(owner, document.Root));
                if (uiElementType is null || ownerType is null || !IsOrDerivesFrom(ownerType, uiElementType))
                {
                    Report(UnsupportedProperty, inline, owner.Name.LocalName, "Aspect");
                    inline.Remove();
                    continue;
                }

                if (inline.HasAttributes)
                {
                    Report(
                        InvalidDocumentShape,
                        inline,
                        Path.GetFileName(file.Path),
                        "An inline Aspect property element does not accept attributes.");
                }

                if (TryParseAspectBody(inline, out List<AspectPropertyAssignment> assignments, out List<DirectiveWhenNode> conditions))
                {
                    AspectResource aspect = new(null, owner.Name.LocalName, assignments, conditions, inline, isInline: true);
                    inlineAspects.Add(owner, aspect);
                    allAspects.Add(aspect);
                }

                inline.Remove();
            }
        }

        private bool TryParseAspectBody(
            XElement source,
            out List<AspectPropertyAssignment> assignments,
            out List<DirectiveWhenNode> conditions)
        {
            assignments = [];
            conditions = [];
            DirectiveParseResult parsed = ParseDirectiveContent(source, allowAssignments: true, allowElements: false);
            if (parsed.Error is not null)
            {
                Report(InvalidDirective, parsed.ErrorSource ?? source, Path.GetFileName(file.Path), parsed.Error);
                return false;
            }

            foreach (DirectiveNode node in parsed.Nodes)
            {
                if (node is DirectiveDefaultNode defaults)
                {
                    foreach (DirectiveNode child in defaults.Body)
                    {
                        if (child is DirectiveAssignmentNode assignment)
                        {
                            assignments.Add(ToAspectAssignment(assignment));
                        }
                        else if (child is DirectiveWhenNode nestedWhen)
                        {
                            conditions.Add(nestedWhen);
                        }
                        else
                        {
                            Report(InvalidDirective, child.Source, Path.GetFileName(file.Path), "@default may contain only property assignments or @when blocks.");
                            return false;
                        }
                    }
                }
                else if (node is DirectiveWhenNode when)
                {
                    conditions.Add(when);
                }
                else
                {
                    Report(InvalidDirective, node.Source, Path.GetFileName(file.Path), "Aspect bodies may contain only @default and @when blocks.");
                    return false;
                }
            }

            IGrouping<string, AspectPropertyAssignment>? duplicate = assignments
                .GroupBy(assignment => assignment.PropertyName, StringComparer.Ordinal)
                .FirstOrDefault(group => group.Count() > 1);
            if (duplicate is not null)
            {
                Report(
                    InvalidDocumentShape,
                    duplicate.Skip(1).First().Source,
                    Path.GetFileName(file.Path),
                    "Aspect assigns property '" + duplicate.Key + "' more than once in @default.");
                return false;
            }

            return !HasErrors;
        }

        private static AspectPropertyAssignment ToAspectAssignment(DirectiveAssignmentNode assignment)
        {
            string value = assignment.Value.Trim();
            bool isReference = value.StartsWith("$", StringComparison.Ordinal);
            if (isReference)
            {
                value = value.Substring(1);
            }

            if (value.Length >= 2 && value[0] == '"' && value[value.Length - 1] == '"')
            {
                value = value.Substring(1, value.Length - 2);
            }

            return new AspectPropertyAssignment(assignment.PropertyName, value, isReference, assignment.Source);
        }

        private IReadOnlyList<AspectPropertyAssignment> ParseAspectAssignments(XElement aspect)
        {
            string text = string.Concat(aspect.Nodes().OfType<XText>().Select(node => node.Value));
            int start = text.IndexOf('{');
            int end = text.LastIndexOf('}');
            if (start < 0 || end <= start)
            {
                Report(InvalidPropertyValue, aspect, "Aspect", "#body", text.Trim());
                return [];
            }

            string body = text.Substring(start + 1, end - start - 1);
            List<AspectPropertyAssignment> assignments = [];
            foreach (string rawStatement in body.Split(';'))
            {
                string statement = rawStatement.Trim();
                if (statement.Length == 0)
                {
                    continue;
                }

                int equals = statement.IndexOf('=');
                if (equals <= 0 || equals == statement.Length - 1)
                {
                    Report(InvalidPropertyValue, aspect, "Aspect", "#body", statement);
                    return [];
                }

                string propertyName = statement.Substring(0, equals).Trim();
                string value = statement.Substring(equals + 1).Trim();
                bool isReference = value.StartsWith("$", StringComparison.Ordinal);
                if (isReference)
                {
                    value = value.Substring(1);
                }

                if (value.Length >= 2 && value[0] == '"' && value[value.Length - 1] == '"')
                {
                    value = value.Substring(1, value.Length - 2);
                }

                assignments.Add(new AspectPropertyAssignment(propertyName, value, isReference, aspect));
            }

            return assignments;
        }

        private string? RequiredName(XElement element)
        {
            string? name = element.Attribute("Name")?.Value;
            if (string.IsNullOrWhiteSpace(name))
            {
                Report(InvalidPropertyValue, element, element.Name.LocalName, "Name", name ?? string.Empty);
                return null;
            }

            return name;
        }

        private bool AddSymbol(string name, NamedSymbolKind kind, object source, XElement location)
        {
            if (symbols.ContainsKey(name))
            {
                Report(InvalidDocumentShape, location, Path.GetFileName(file.Path), "Duplicate Name '" + name + "'.");
                return false;
            }

            symbols.Add(name, new NamedSymbol(name, kind, source));
            return true;
        }

        private static DrawColorLiteral? ParseHexColor(string value)
        {
            if (value.Length != 7 && value.Length != 9)
            {
                return null;
            }

            if (value[0] != '#')
            {
                return null;
            }

            static bool TryByte(string text, out byte parsed)
            {
                return byte.TryParse(text, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out parsed);
            }

            if (value.Length == 7 &&
                TryByte(value.Substring(1, 2), out byte r) &&
                TryByte(value.Substring(3, 2), out byte g) &&
                TryByte(value.Substring(5, 2), out byte b))
            {
                return new DrawColorLiteral(r, g, b, 255);
            }

            if (value.Length == 9 &&
                TryByte(value.Substring(1, 2), out byte a) &&
                TryByte(value.Substring(3, 2), out byte rr) &&
                TryByte(value.Substring(5, 2), out byte gg) &&
                TryByte(value.Substring(7, 2), out byte bb))
            {
                return new DrawColorLiteral(rr, gg, bb, a);
            }

            return null;
        }

        public string EmitElement(XElement element)
        {
            string? requestedName = element.Attribute("Name")?.Value;
            string variable;
            if (string.IsNullOrWhiteSpace(requestedName))
            {
                variable = "element" + nextId.ToString(CultureInfo.InvariantCulture);
                nextId++;
            }
            else
            {
                string symbolName = requestedName!.Trim();
                variable = CreateIdentifier(symbolName);
                string referenceCode = userControlPair is null ? variable : "this." + variable;
                if (!AddSymbol(symbolName, NamedSymbolKind.Element, referenceCode, element))
                {
                    variable = "element" + nextId.ToString(CultureInfo.InvariantCulture);
                    nextId++;
                }
            }

            string? typeName = ResolveElementType(element.Name.LocalName);

            if (typeName is null)
            {
                Report(UnsupportedElement, element, element.Name.LocalName);
                return variable;
            }

            currentLines.Add(typeName + " " + variable + " = new();");
            EmitRuntimeResources(element, variable);
            if (!string.IsNullOrWhiteSpace(requestedName) && userControlPair is not null)
            {
                RegisterNamedElement(requestedName!.Trim(), variable, typeName, element);
            }
            DirectiveParseResult parsedContent = GetDirectiveContent(element, allowAssignments: false, allowElements: true);
            if (parsedContent.Error is not null)
            {
                Report(InvalidDirective, parsedContent.ErrorSource ?? element, Path.GetFileName(file.Path), parsedContent.Error);
                return variable;
            }

            IReadOnlyList<AspectResource> aspects = ResolveAspects(element);
            ApplyAspects(element, variable, aspects);
            foreach (XAttribute attribute in element.Attributes().Where(attribute => !attribute.IsNamespaceDeclaration && attribute.Name.LocalName is not "Aspect" and not "Name" and not "DataType"))
            {
                if (TryEmitEventAttribute(element, variable, attribute))
                {
                    continue;
                }

                EmitProperty(element, variable, attribute);
            }

            if (parsedContent.HasDirectives || aspects.Any(aspect => aspect.Conditions.Count > 0))
            {
                EmitReactiveContent(element, variable, parsedContent, aspects);
            }
            else
            {
                string? directText = ReadDirectText(element);
                if (directText is not null)
                {
                    EmitTextContent(element, variable, directText);
                }

                foreach (XElement child in element.Elements())
                {
                    string childVariable = EmitElement(child);
                    EmitChild(element, variable, childVariable);
                }
            }

            return variable;
        }

        private void EmitRuntimeResources(XElement owner, string ownerVariable)
        {
            if (!resourceScopes.TryGetValue(owner, out ResourceScope? scope))
            {
                return;
            }

            foreach (object resource in scope.RuntimeResources)
            {
                switch (resource)
                {
                    case SolidColorBrushResource brush:
                        currentLines.Add(ownerVariable + ".Resources[" + Literal(brush.Name) + "] = " + brush.Variable + ";");
                        break;
                    case AspectResource aspect:
                        string targetType = ResolveElementType(aspect.TargetName)!;
                        string key = aspect.Name is null ? "typeof(" + targetType + ")" : Literal(aspect.Name);
                        string properties = string.Join(", ", aspect.Assignments.Select(assignment => Literal(assignment.PropertyName)));
                        currentLines.Add(
                            ownerVariable + ".Resources[" + key + "] = new global::Cerneala.UI.Markup.MarkupAspectResource(" +
                            (aspect.Name is null ? "null" : Literal(aspect.Name)) + ", typeof(" + targetType + "), new string[] { " +
                            properties + " }, " + (aspect.Conditions.Count > 0 ? "true" : "false") + ");");
                        break;
                }
            }
        }

        private string? ResolveElementType(string elementName)
        {
            INamedTypeSymbol? type = ResolveElementTypeSymbol(elementName);
            if (type is null)
            {
                return null;
            }

            resolvedElementTypes[elementName] = type;
            return type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
        }

        private INamedTypeSymbol? ResolveElementTypeSymbol(string elementName)
        {
            if (resolvedElementTypes.TryGetValue(elementName, out INamedTypeSymbol? resolved))
            {
                return resolved;
            }

            INamedTypeSymbol? type = compilation.GetTypeByMetadataName("Cerneala.UI.Controls." + elementName);
            INamedTypeSymbol? uiElementType = compilation.GetTypeByMetadataName("Cerneala.UI.Elements.UIElement");
            INamedTypeSymbol? windowType = compilation.GetTypeByMetadataName("Cerneala.UI.Controls.Window");
            if (type is null || type.TypeKind != TypeKind.Class || type.IsAbstract ||
                uiElementType is null || !IsOrDerivesFrom(type, uiElementType) ||
                (windowType is not null && IsOrDerivesFrom(type, windowType)))
            {
                type = ResolveCustomElementType(elementName);
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

        private IReadOnlyList<AspectResource> ResolveAspects(XElement element)
        {
            string elementName = element.Name.LocalName;
            List<AspectResource> resolved = [];
            if (TryResolveDefaultAspect(element, elementName, out AspectResource defaultAspect))
            {
                resolved.Add(defaultAspect);
            }

            if (inlineAspects.TryGetValue(element, out AspectResource? inlineAspect))
            {
                resolved.Add(inlineAspect);
                return resolved;
            }

            XAttribute? aspectAttribute = element.Attribute("Aspect");
            if (aspectAttribute is null)
            {
                return resolved;
            }

            string referenceName = ReadReferenceName(elementName, "Aspect", aspectAttribute);
            if (referenceName.Length == 0)
            {
                return resolved;
            }

            if (!TryResolveResource(aspectAttribute, referenceName, out NamedSymbol symbol) ||
                symbol.Source is not AspectResource namedAspect)
            {
                Report(InvalidPropertyValue, aspectAttribute, elementName, "Aspect", aspectAttribute.Value);
                return resolved;
            }

            if (!string.Equals(namedAspect.TargetName, elementName, StringComparison.Ordinal))
            {
                Report(InvalidPropertyValue, aspectAttribute, elementName, "Aspect", aspectAttribute.Value);
                return resolved;
            }

            resolved.Add(namedAspect);
            return resolved;
        }

        private void ApplyAspects(XElement element, string variable, IReadOnlyList<AspectResource> aspects)
        {
            foreach (AspectResource aspect in aspects)
            {
                if (aspect.IsInline)
                {
                    EmitInlineAspect(element, variable, aspect);
                }
                else
                {
                    EmitAspectAssignments(element, variable, aspect);
                }
            }
        }

        private void EmitInlineAspect(XElement element, string variable, AspectResource aspect)
        {
            string elementName = element.Name.LocalName;
            List<string> values = [];
            foreach (AspectPropertyAssignment assignment in aspect.Assignments)
            {
                PropertySpec? spec = FindPropertySpec(elementName, assignment.PropertyName, ReferenceEquals(element, document.Root));
                if (spec is null || !spec.Assignable)
                {
                    Report(UnsupportedProperty, assignment.Source, elementName, assignment.PropertyName);
                    return;
                }

                GeneratedExpression? expression = assignment.IsReference
                    ? ResolveReferenceValue(elementName, assignment.PropertyName, assignment.RawValue, spec.ValueKind, assignment.Source)
                    : ParseAspectLiteralValue(elementName, assignment.PropertyName, assignment.RawValue, spec, assignment.Source);
                if (expression is null)
                {
                    return;
                }

                values.Add(
                    "new global::Cerneala.UI.Aspect.ElementAspectValue(" + spec.PropertyCode + ", " + expression.Code + ")");
            }

            string valuesCode = values.Count == 0
                ? "global::System.Array.Empty<global::Cerneala.UI.Aspect.ElementAspectValue>()"
                : "new global::Cerneala.UI.Aspect.ElementAspectValue[] { " + string.Join(", ", values) + " }";
            string aspectVariable = "inlineAspect" + nextResourceId.ToString(CultureInfo.InvariantCulture);
            nextResourceId++;
            aspect.RuntimeVariable = aspectVariable;
            currentLines.Add(
                "global::Cerneala.UI.Aspect.ElementAspect " + aspectVariable +
                " = new(" + valuesCode + ", " + (aspect.Conditions.Count > 0 ? "true" : "false") + ");");
            currentLines.Add(variable + ".Aspect = " + aspectVariable + ";");
        }

        private string ReadReferenceName(string elementName, string propertyName, XAttribute attribute)
        {
            string value = attribute.Value.Trim();
            if (!value.StartsWith("$", StringComparison.Ordinal) || value.Length == 1)
            {
                Report(InvalidPropertyValue, attribute, elementName, propertyName, attribute.Value);
                return string.Empty;
            }

            return value.Substring(1);
        }

        private void EmitAspectAssignments(XElement element, string variable, AspectResource aspect)
        {
            string elementName = element.Name.LocalName;
            foreach (AspectPropertyAssignment assignment in aspect.Assignments)
            {
                PropertySpec? spec = FindPropertySpec(elementName, assignment.PropertyName, ReferenceEquals(element, document.Root));
                if (spec is null)
                {
                    Report(UnsupportedProperty, assignment.Source, elementName, assignment.PropertyName);
                    return;
                }

                GeneratedExpression? expression = assignment.IsReference
                    ? ResolveReferenceValue(elementName, assignment.PropertyName, assignment.RawValue, spec.ValueKind, assignment.Source)
                    : ParseAspectLiteralValue(elementName, assignment.PropertyName, assignment.RawValue, spec, assignment.Source);

                if (expression is null)
                {
                    return;
                }

                currentLines.Add(reactiveDocument
                    ? variable + ".SetValue(" + spec.PropertyCode + ", " + expression.Code +
                        ", global::Cerneala.UI.Core.UiPropertyValueSource.AspectBase);"
                    : variable + "." + spec.Name + " = " + expression.Code + ";");
            }
        }

        private GeneratedExpression? ParseAspectLiteralValue(string elementName, string propertyName, string value, PropertySpec spec, XObject source)
        {
            XAttribute synthetic = new(propertyName, value);
            return ParseLiteralValue(elementName, propertyName, synthetic, value, spec);
        }

        private GeneratedExpression? ResolveReferenceValue(string elementName, string propertyName, string referenceName, MarkupValueKind targetKind, XObject source)
        {
            if (!TryResolveResource(source, referenceName, out NamedSymbol symbol))
            {
                Report(InvalidPropertyValue, source, elementName, propertyName, "$" + referenceName);
                return null;
            }

            if (targetKind == MarkupValueKind.DrawColor && symbol.Source is SolidColorBrushResource brush)
            {
                return new GeneratedExpression(brush.ColorExpression, MarkupValueKind.DrawColor);
            }

            Report(InvalidPropertyValue, source, elementName, propertyName, "$" + referenceName);
            return null;
        }

        private bool TryResolveDefaultAspect(XObject source, string targetName, out AspectResource aspect)
        {
            foreach (ResourceScope scope in EnumerateResourceScopes(source))
            {
                if (scope.DefaultAspectsByTarget.TryGetValue(targetName, out aspect))
                {
                    return true;
                }
            }

            aspect = null!;
            return false;
        }

        private bool TryResolveResource(XObject source, string name, out NamedSymbol symbol)
        {
            foreach (ResourceScope scope in EnumerateResourceScopes(source))
            {
                if (scope.NamedResources.TryGetValue(name, out symbol))
                {
                    return true;
                }
            }

            symbol = null!;
            return false;
        }

        private bool TryResolveObjectSymbol(XObject source, string name, out NamedSymbol symbol)
        {
            if (symbols.TryGetValue(name, out symbol))
            {
                return true;
            }

            return TryResolveResource(source, name, out symbol);
        }

        private IEnumerable<ResourceScope> EnumerateResourceScopes(XObject source)
        {
            XElement? current = source switch
            {
                XElement element => element,
                _ => source.Parent
            };

            while (current is not null)
            {
                if (resourcePropertyScopes.TryGetValue(current, out ResourceScope? declarationScope))
                {
                    yield return declarationScope;
                    current = declarationScope.Owner.Parent;
                    continue;
                }

                if (resourceScopes.TryGetValue(current, out ResourceScope? scope))
                {
                    yield return scope;
                }

                current = current.Parent;
            }
        }

        private void EmitProperty(XElement element, string variable, XAttribute attribute)
        {
            string elementName = element.Name.LocalName;
            string propertyName = attribute.Name.LocalName;
            string value = attribute.Value;

            PropertySpec? spec = FindPropertySpec(elementName, propertyName, ReferenceEquals(element, document.Root));
            if (spec is null || !spec.Assignable)
            {
                if (!HasErrors)
                {
                    Report(UnsupportedProperty, attribute, elementName, propertyName);
                }

                return;
            }

            GeneratedExpression? expression = ParseLiteralValue(elementName, propertyName, attribute, value, spec);
            if (expression is null)
            {
                return;
            }

            currentLines.Add(reactiveDocument
                ? variable + ".SetValue(" + spec.PropertyCode + ", " + expression.Code +
                    ", global::Cerneala.UI.Core.UiPropertyValueSource.MarkupBase);"
                : variable + "." + spec.Name + " = " + expression.Code + ";");
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
            INamedTypeSymbol? uiPropertyType = compilation.GetTypeByMetadataName("Cerneala.UI.Core.UiProperty`1");
            if (elementType is null || uiPropertyType is null)
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
            resolved = new PropertySpec(
                propertyName,
                kind,
                propertyField.ContainingType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat) + "." + propertyField.Name,
                valueType,
                assignable);
            resolvedProperties.Add(cacheKey, resolved);
            return resolved;
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

            if (typeName == "Cerneala.Drawing.DrawColor")
            {
                return MarkupValueKind.DrawColor;
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

        private GeneratedExpression? ParseLiteralValue(string elementName, string propertyName, XAttribute attribute, string value, PropertySpec spec)
        {
            MarkupValueKind kind = spec.ValueKind;
            string? code = kind switch
            {
                MarkupValueKind.String when !string.IsNullOrWhiteSpace(value) => Literal(value),
                MarkupValueKind.Bool => Bool(elementName, propertyName, attribute),
                MarkupValueKind.Float => Float(elementName, propertyName, attribute),
                MarkupValueKind.Integer => Integer(elementName, propertyName, attribute, spec.LiteralType.SpecialType),
                MarkupValueKind.Double => Double(elementName, propertyName, attribute),
                MarkupValueKind.Decimal => Decimal(elementName, propertyName, attribute),
                MarkupValueKind.NonNegativeFloat => NonNegativeFloat(elementName, propertyName, attribute),
                MarkupValueKind.PositiveFloat => PositiveFloat(elementName, propertyName, attribute),
                MarkupValueKind.Thickness => Thickness(elementName, propertyName, attribute),
                MarkupValueKind.NonNegativeThickness => NonNegativeThickness(elementName, propertyName, attribute),
                MarkupValueKind.DrawColor => Color(elementName, propertyName, attribute),
                MarkupValueKind.Enum => EnumValue(elementName, propertyName, attribute, spec.LiteralType),
                _ => null
            };

            if (code is null)
            {
                if (kind is MarkupValueKind.String or MarkupValueKind.Unsupported)
                {
                    Report(InvalidPropertyValue, attribute, elementName, propertyName, value);
                }

                return null;
            }

            return new GeneratedExpression(code, kind);
        }

        private void EmitTextContent(XElement element, string variable, string text)
        {
            switch (element.Name.LocalName)
            {
                case "TextBlock":
                    currentLines.Add(reactiveDocument
                        ? variable + ".SetValue(global::Cerneala.UI.Controls.TextBlock.TextProperty, " + Literal(text) +
                            ", global::Cerneala.UI.Core.UiPropertyValueSource.MarkupBase);"
                        : variable + ".Text = " + Literal(text) + ";");
                    break;
                case "Button":
                    currentLines.Add(reactiveDocument
                        ? variable + ".SetValue(global::Cerneala.UI.Controls.ContentControl.ContentProperty, (object?)" + Literal(text) +
                            ", global::Cerneala.UI.Core.UiPropertyValueSource.MarkupBase);"
                        : variable + ".Content = " + Literal(text) + ";");
                    break;
                default:
                    Report(UnsupportedProperty, element, element.Name.LocalName, "#text");
                    break;
            }
        }

        private void EmitChild(XElement parent, string parentVariable, string childVariable)
        {
            switch (parent.Name.LocalName)
            {
                case "Panel":
                case "StackPanel":
                    currentLines.Add(parentVariable + ".LogicalChildren.Add(" + childVariable + ");");
                    currentLines.Add(parentVariable + ".VisualChildren.Add(" + childVariable + ");");
                    break;
                case "Border":
                    currentLines.Add(parentVariable + ".Child = " + childVariable + ";");
                    break;
                case "Button":
                    currentLines.Add(parentVariable + ".Content = " + childVariable + ";");
                    break;
                default:
                    Report(UnsupportedProperty, parent, parent.Name.LocalName, "#child");
                    break;
            }
        }

        private static string? ReadDirectText(XElement element)
        {
            string text = string.Concat(element.Nodes().OfType<XText>().Select(node => node.Value));
            return string.IsNullOrWhiteSpace(text) ? null : text.Trim();
        }

        private string? NonNegativeFloat(string elementName, string propertyName, XAttribute attribute)
        {
            string? code = Float(elementName, propertyName, attribute);
            if (code is null || !float.TryParse(attribute.Value, NumberStyles.Float, CultureInfo.InvariantCulture, out float value) || value < 0)
            {
                return code is null ? null : Invalid(attribute, elementName, propertyName, attribute.Value);
            }

            return code;
        }

        private string? EnumValue(
            string elementName,
            string propertyName,
            XAttribute attribute,
            ITypeSymbol enumType)
        {
            string value = attribute.Value.Trim();
            IFieldSymbol? member = enumType.GetMembers()
                .OfType<IFieldSymbol>()
                .FirstOrDefault(candidate => candidate.HasConstantValue &&
                    string.Equals(candidate.Name, value, StringComparison.OrdinalIgnoreCase));
            return member is null
                ? Invalid(attribute, elementName, propertyName, attribute.Value)
                : enumType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat) + "." + member.Name;
        }

        private static string Literal(string value)
        {
            StringBuilder builder = new();
            builder.Append('"');
            foreach (char character in value)
            {
                builder.Append(character switch
                {
                    '\\' => "\\\\",
                    '"' => "\\\"",
                    '\0' => "\\0",
                    '\a' => "\\a",
                    '\b' => "\\b",
                    '\f' => "\\f",
                    '\n' => "\\n",
                    '\r' => "\\r",
                    '\t' => "\\t",
                    '\v' => "\\v",
                    _ when char.IsControl(character) => "\\u" + ((int)character).ToString("x4", CultureInfo.InvariantCulture),
                    _ => character.ToString()
                });
            }

            builder.Append('"');
            return builder.ToString();
        }

        private string? Bool(string elementName, string propertyName, XAttribute attribute)
        {
            return bool.TryParse(attribute.Value, out bool parsed) ? (parsed ? "true" : "false") : Invalid(attribute, elementName, propertyName, attribute.Value);
        }

        private string? Float(string elementName, string propertyName, XAttribute attribute)
        {
            string value = attribute.Value;
            return float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out float parsed) &&
                !float.IsNaN(parsed) &&
                !float.IsInfinity(parsed)
                ? parsed.ToString("R", CultureInfo.InvariantCulture) + "f"
                : Invalid(attribute, elementName, propertyName, value);
        }

        private string? Integer(string elementName, string propertyName, XAttribute attribute, SpecialType type)
        {
            string value = attribute.Value.Trim();
            bool valid = type switch
            {
                SpecialType.System_Byte => byte.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out _),
                SpecialType.System_SByte => sbyte.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out _),
                SpecialType.System_Int16 => short.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out _),
                SpecialType.System_UInt16 => ushort.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out _),
                SpecialType.System_Int32 => int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out _),
                SpecialType.System_UInt32 => uint.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out _),
                SpecialType.System_Int64 => long.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out _),
                SpecialType.System_UInt64 => ulong.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out _),
                _ => false
            };
            return valid ? value : Invalid(attribute, elementName, propertyName, attribute.Value);
        }

        private string? Double(string elementName, string propertyName, XAttribute attribute)
        {
            string value = attribute.Value.Trim();
            return double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out double parsed) &&
                !double.IsNaN(parsed) && !double.IsInfinity(parsed)
                ? parsed.ToString("R", CultureInfo.InvariantCulture) + "d"
                : Invalid(attribute, elementName, propertyName, attribute.Value);
        }

        private string? Decimal(string elementName, string propertyName, XAttribute attribute)
        {
            string value = attribute.Value.Trim();
            return decimal.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out decimal parsed)
                ? parsed.ToString(CultureInfo.InvariantCulture) + "m"
                : Invalid(attribute, elementName, propertyName, attribute.Value);
        }

        private string? PositiveFloat(string elementName, string propertyName, XAttribute attribute)
        {
            string value = attribute.Value;
            return float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out float parsed) &&
                parsed > 0 &&
                !float.IsNaN(parsed) &&
                !float.IsInfinity(parsed)
                ? parsed.ToString("R", CultureInfo.InvariantCulture) + "f"
                : Invalid(attribute, elementName, propertyName, value);
        }

        private string? Thickness(string elementName, string propertyName, XAttribute attribute)
        {
            string value = attribute.Value;
            string[] parts = value.Split(',').Select(part => part.Trim()).ToArray();
            if (parts.Length == 1 && FloatPart(elementName, propertyName, attribute, parts[0]) is string uniform)
            {
                return "new global::Cerneala.UI.Layout.Thickness(" + uniform + ")";
            }

            if (parts.Length == 1)
            {
                return null;
            }

            if (parts.Length == 4)
            {
                string? left = FloatPart(elementName, propertyName, attribute, parts[0]);
                string? top = FloatPart(elementName, propertyName, attribute, parts[1]);
                string? right = FloatPart(elementName, propertyName, attribute, parts[2]);
                string? bottom = FloatPart(elementName, propertyName, attribute, parts[3]);
                if (left is not null && top is not null && right is not null && bottom is not null)
                {
                    return "new global::Cerneala.UI.Layout.Thickness(" + left + ", " + top + ", " + right + ", " + bottom + ")";
                }

                return null;
            }

            return Invalid(attribute, elementName, propertyName, value);
        }

        private string? NonNegativeThickness(string elementName, string propertyName, XAttribute attribute)
        {
            string value = attribute.Value;
            string[] parts = value.Split(',').Select(part => part.Trim()).ToArray();
            if (parts.Length == 1 && NonNegativeFloatPart(elementName, propertyName, attribute, parts[0]) is string uniform)
            {
                return "new global::Cerneala.UI.Layout.Thickness(" + uniform + ")";
            }

            if (parts.Length == 1)
            {
                return null;
            }

            if (parts.Length == 4)
            {
                string? left = NonNegativeFloatPart(elementName, propertyName, attribute, parts[0]);
                string? top = NonNegativeFloatPart(elementName, propertyName, attribute, parts[1]);
                string? right = NonNegativeFloatPart(elementName, propertyName, attribute, parts[2]);
                string? bottom = NonNegativeFloatPart(elementName, propertyName, attribute, parts[3]);
                if (left is not null && top is not null && right is not null && bottom is not null)
                {
                    return "new global::Cerneala.UI.Layout.Thickness(" + left + ", " + top + ", " + right + ", " + bottom + ")";
                }

                return null;
            }

            return Invalid(attribute, elementName, propertyName, value);
        }

        private string? FloatPart(string elementName, string propertyName, XAttribute attribute, string value)
        {
            return float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out float parsed) &&
                !float.IsNaN(parsed) &&
                !float.IsInfinity(parsed)
                ? parsed.ToString("R", CultureInfo.InvariantCulture) + "f"
                : Invalid(attribute, elementName, propertyName, attribute.Value);
        }

        private string? NonNegativeFloatPart(string elementName, string propertyName, XAttribute attribute, string value)
        {
            return float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out float parsed) &&
                parsed >= 0 &&
                !float.IsNaN(parsed) &&
                !float.IsInfinity(parsed)
                ? parsed.ToString("R", CultureInfo.InvariantCulture) + "f"
                : Invalid(attribute, elementName, propertyName, attribute.Value);
        }

        private string? Color(string elementName, string propertyName, XAttribute attribute)
        {
            string value = attribute.Value;
            if (string.Equals(value, "Transparent", StringComparison.OrdinalIgnoreCase))
            {
                return "global::Cerneala.Drawing.DrawColor.Transparent";
            }

            if (string.Equals(value, "White", StringComparison.OrdinalIgnoreCase))
            {
                return "global::Cerneala.Drawing.DrawColor.White";
            }

            if (string.Equals(value, "Black", StringComparison.OrdinalIgnoreCase))
            {
                return "global::Cerneala.Drawing.DrawColor.Black";
            }

            string[] parts = value.Split(',').Select(part => part.Trim()).ToArray();
            if (parts.Length is 3 or 4 &&
                byte.TryParse(parts[0], NumberStyles.Integer, CultureInfo.InvariantCulture, out byte r) &&
                byte.TryParse(parts[1], NumberStyles.Integer, CultureInfo.InvariantCulture, out byte g) &&
                byte.TryParse(parts[2], NumberStyles.Integer, CultureInfo.InvariantCulture, out byte b) &&
                (parts.Length == 3 || byte.TryParse(parts[3], NumberStyles.Integer, CultureInfo.InvariantCulture, out _)))
            {
                string alpha = parts.Length == 4 ? ", " + parts[3] : string.Empty;
                return "new global::Cerneala.Drawing.DrawColor(" + r + ", " + g + ", " + b + alpha + ")";
            }

            return Invalid(attribute, elementName, propertyName, value);
        }

        private string? Invalid(XAttribute attribute, string elementName, string propertyName, string value)
        {
            Report(InvalidPropertyValue, attribute, elementName, propertyName, value);
            return null;
        }

        private void Report(DiagnosticDescriptor descriptor, object locationSource, params object[] args)
        {
            HasErrors = true;
            context.ReportDiagnostic(Diagnostic.Create(descriptor, CreateLocation(file, locationSource), args));
        }
    }

    private static Location CreateLocation(MarkupSource file, object locationSource)
    {
        if (locationSource is XObject xmlObject)
        {
            return CreateLocation(file, xmlObject);
        }

        return Location.Create(file.Path, TextSpan.FromBounds(0, 0), new LinePositionSpan(new LinePosition(0, 0), new LinePosition(0, 0)));
    }

    private static Location CreateLocation(MarkupSource file, XObject xmlObject)
    {
        if (xmlObject is IXmlLineInfo lineInfo && lineInfo.HasLineInfo())
        {
            return CreateLocation(file, lineInfo.LineNumber, lineInfo.LinePosition);
        }

        return CreateLocation(file, 1, 1);
    }

    private static Location CreateLocation(MarkupSource file, int oneBasedLine, int oneBasedColumn)
    {
        SourceText sourceText = SourceText.From(file.Text ?? string.Empty, Encoding.UTF8);
        int line = Math.Max(0, Math.Min(sourceText.Lines.Count - 1, oneBasedLine - 1));
        int adjustedColumn = oneBasedColumn;
        if (oneBasedLine == 1 && adjustedColumn > FragmentWrapperStart.Length)
        {
            adjustedColumn -= FragmentWrapperStart.Length;
        }

        int column = Math.Max(0, adjustedColumn - 1);
        int start = Math.Min(sourceText.Length, sourceText.Lines[line].Start + column);
        LinePosition position = sourceText.Lines.GetLinePosition(start);
        return Location.Create(file.Path, TextSpan.FromBounds(start, start), new LinePositionSpan(position, position));
    }
}
