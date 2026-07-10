using System;
using System.Collections.Immutable;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
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
        for (int i = 0; i < files.Length; i++)
        {
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
            XDocument fragment = XDocument.Parse(
                FragmentWrapperStart + StripXmlDeclarationPreservingPositions(file.Text ?? string.Empty) + FragmentWrapperEnd,
                LoadOptions.SetLineInfo | LoadOptions.PreserveWhitespace);

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

            XElement? resources = null;
            List<XElement> roots = [];
            foreach (XElement element in elements)
            {
                if (element.Name.LocalName == "Resources")
                {
                    if (resources is not null)
                    {
                        return InvalidShape(file, element, "Only one top-level Resources element is allowed.");
                    }

                    resources = element;
                    continue;
                }

                roots.Add(element);
            }

            if (roots.Count != 1)
            {
                return new ParsedDocument(
                    null,
                    Diagnostic.Create(
                        MalformedMarkup,
                        CreateLocation(file, roots.FirstOrDefault() ?? resources ?? new XElement("Missing")),
                        Path.GetFileName(file.Path),
                        "Markup must contain exactly one UI root element."));
            }

            return new ParsedDocument(new MarkupDocument(resources, roots[0]), null);
        }
        catch (XmlException ex)
        {
            return new ParsedDocument(null, Diagnostic.Create(MalformedMarkup, CreateLocation(file, ex.LineNumber, ex.LinePosition), Path.GetFileName(file.Path), ex.Message));
        }
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
        public MarkupDocument(XElement? resources, XElement root)
        {
            Resources = resources;
            Root = root;
        }

        public XElement? Resources { get; }

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
            reactiveDocument = namedAspects.Values.Any(aspect => aspect.Conditions.Count > 0) ||
                defaultAspectsByTarget.Values.Any(aspect => aspect.Conditions.Count > 0) ||
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
            PositiveFloat,
            Thickness,
            NonNegativeThickness,
            DrawColor
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
                Func<string, bool> appliesToElement,
                MarkupValueKind valueKind,
                string propertyCode,
                bool assignable = true)
            {
                Name = name;
                AppliesToElement = appliesToElement;
                ValueKind = valueKind;
                PropertyCode = propertyCode;
                Assignable = assignable;
            }

            public string Name { get; }

            public Func<string, bool> AppliesToElement { get; }

            public MarkupValueKind ValueKind { get; }

            public string PropertyCode { get; }

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
                XElement source)
            {
                Name = name;
                TargetName = targetName;
                Assignments = assignments;
                Conditions = conditions;
                Source = source;
            }

            public string? Name { get; }

            public string TargetName { get; }

            public IReadOnlyList<AspectPropertyAssignment> Assignments { get; }

            public IReadOnlyList<DirectiveWhenNode> Conditions { get; }

            public XElement Source { get; }
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
        private readonly Dictionary<string, SolidColorBrushResource> solidColorBrushes = new(StringComparer.Ordinal);
        private readonly Dictionary<string, AspectResource> namedAspects = new(StringComparer.Ordinal);
        private readonly Dictionary<string, AspectResource> defaultAspectsByTarget = new(StringComparer.Ordinal);
        private readonly Dictionary<XElement, DirectiveParseResult> directiveContent = new();
        private readonly Dictionary<string, INamedTypeSymbol> resolvedElementTypes = new(StringComparer.Ordinal);
        private readonly Dictionary<string, IReadOnlyList<NamedElementMember>> conditionalFactoryMembers = new(StringComparer.Ordinal);
        private readonly Stack<List<NamedElementMember>> conditionalMemberScopes = new();
        private readonly List<NamedElementMember> namedElementMembers = [];
        private List<string> currentLines;
        private List<string> currentPostLines;
        private int nextReactiveId;

        private static readonly PropertySpec[] PropertySpecs =
        [
            new("Text", element => element == "TextBlock", MarkupValueKind.String, "global::Cerneala.UI.Controls.TextBlock.TextProperty"),
            new("Content", element => element == "Button", MarkupValueKind.String, "global::Cerneala.UI.Controls.ContentControl.ContentProperty"),
            new("IsEnabled", _ => true, MarkupValueKind.Bool, "global::Cerneala.UI.Elements.UIElement.IsEnabledProperty"),
            new("IsVisible", _ => true, MarkupValueKind.Bool, "global::Cerneala.UI.Elements.UIElement.IsVisibleProperty"),
            new("Margin", _ => true, MarkupValueKind.Thickness, "global::Cerneala.UI.Elements.UIElement.MarginProperty"),
            new("Background", IsControlElement, MarkupValueKind.DrawColor, "global::Cerneala.UI.Controls.Control.BackgroundProperty"),
            new("Foreground", IsControlElement, MarkupValueKind.DrawColor, "global::Cerneala.UI.Controls.Control.ForegroundProperty"),
            new("BorderColor", IsControlElement, MarkupValueKind.DrawColor, "global::Cerneala.UI.Controls.Control.BorderColorProperty"),
            new("BorderThickness", IsControlElement, MarkupValueKind.NonNegativeThickness, "global::Cerneala.UI.Controls.Control.BorderThicknessProperty"),
            new("Padding", IsControlElement, MarkupValueKind.NonNegativeThickness, "global::Cerneala.UI.Controls.Control.PaddingProperty"),
            new("FontFamily", IsControlElement, MarkupValueKind.String, "global::Cerneala.UI.Controls.Control.FontFamilyProperty"),
            new("FontSize", IsControlElement, MarkupValueKind.PositiveFloat, "global::Cerneala.UI.Controls.Control.FontSizeProperty"),
            new("IsMouseOver", _ => true, MarkupValueKind.Bool, "global::Cerneala.UI.Elements.UIElement.IsPointerOverProperty", assignable: false),
            new("IsPointerOver", _ => true, MarkupValueKind.Bool, "global::Cerneala.UI.Elements.UIElement.IsPointerOverProperty", assignable: false)
        ];

        private void ReadResources()
        {
            if (document.Resources is null)
            {
                return;
            }

            foreach (XElement resource in document.Resources.Elements())
            {
                switch (resource.Name.LocalName)
                {
                    case "SolidColorBrush":
                        ReadSolidColorBrush(resource);
                        break;
                    case "Aspect":
                        ReadAspect(resource);
                        break;
                    case "Resources":
                        Report(InvalidDocumentShape, resource, Path.GetFileName(file.Path), "Nested Resources declarations are not supported.");
                        break;
                    default:
                        Report(UnsupportedElement, resource, resource.Name.LocalName);
                        break;
                }
            }
        }

        private void ReadSolidColorBrush(XElement resource)
        {
            string? name = RequiredName(resource);
            if (name is null)
            {
                return;
            }

            XAttribute? colorAttribute = resource.Attribute("Color");
            if (colorAttribute is null || ParseHexColor(colorAttribute.Value) is not DrawColorLiteral color)
            {
                Report(InvalidPropertyValue, (object?)colorAttribute ?? resource, "SolidColorBrush", "Color", colorAttribute?.Value ?? string.Empty);
                return;
            }

            string variable = CreateIdentifier(name);
            SolidColorBrushResource brush = new(name, variable, color.ToExpression(), color, resource);
            if (!AddSymbol(name, NamedSymbolKind.SolidColorBrush, brush, resource))
            {
                return;
            }

            solidColorBrushes[name] = brush;
            currentLines.Add("global::Cerneala.UI.Media.SolidColorBrush " + variable + " = new(" + brush.ColorExpression + ");");
        }

        private void ReadAspect(XElement resource)
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

            string? name = resource.Attribute("Name")?.Value;
            DirectiveParseResult parsed = ParseDirectiveContent(resource, allowAssignments: true, allowElements: false);
            if (parsed.Error is not null)
            {
                Report(InvalidDirective, parsed.ErrorSource ?? resource, Path.GetFileName(file.Path), parsed.Error);
                return;
            }

            List<AspectPropertyAssignment> assignments = [];
            List<DirectiveWhenNode> conditions = [];
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
                            return;
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
                    return;
                }
            }

            if (HasErrors)
            {
                return;
            }

            AspectResource aspect = new(string.IsNullOrWhiteSpace(name) ? null : name, targetName, assignments, conditions, resource);
            if (aspect.Name is null)
            {
                if (defaultAspectsByTarget.ContainsKey(targetName))
                {
                    Report(InvalidDocumentShape, resource, Path.GetFileName(file.Path), "Duplicate unnamed Aspect for target '" + targetName + "'.");
                    return;
                }

                defaultAspectsByTarget.Add(targetName, aspect);
                return;
            }

            if (!AddSymbol(aspect.Name, NamedSymbolKind.Aspect, aspect, resource))
            {
                return;
            }

            namedAspects.Add(aspect.Name, aspect);
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

        private string? ResolveElementType(string elementName)
        {
            string? metadataName = elementName switch
            {
                "Panel" => "Cerneala.UI.Controls.Panel",
                "StackPanel" => "Cerneala.UI.Controls.StackPanel",
                "Border" => "Cerneala.UI.Controls.Border",
                "Button" => "Cerneala.UI.Controls.Button",
                "TextBlock" => "Cerneala.UI.Controls.TextBlock",
                "UserControl" => "Cerneala.UI.Controls.UserControl",
                _ => null
            };

            INamedTypeSymbol? type = metadataName is null
                ? ResolveCustomElementType(elementName)
                : compilation.GetTypeByMetadataName(metadataName);
            if (type is null)
            {
                return null;
            }

            resolvedElementTypes[elementName] = type;
            return type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
        }

        private IReadOnlyList<AspectResource> ResolveAspects(XElement element)
        {
            string elementName = element.Name.LocalName;
            List<AspectResource> resolved = [];
            if (defaultAspectsByTarget.TryGetValue(elementName, out AspectResource? defaultAspect))
            {
                resolved.Add(defaultAspect);
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

            if (!namedAspects.TryGetValue(referenceName, out AspectResource? namedAspect))
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
                EmitAspectAssignments(element.Name.LocalName, variable, aspect);
            }
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

        private void EmitAspectAssignments(string elementName, string variable, AspectResource aspect)
        {
            foreach (AspectPropertyAssignment assignment in aspect.Assignments)
            {
                PropertySpec? spec = FindPropertySpec(elementName, assignment.PropertyName);
                if (spec is null)
                {
                    Report(UnsupportedProperty, assignment.Source, elementName, assignment.PropertyName);
                    return;
                }

                GeneratedExpression? expression = assignment.IsReference
                    ? ResolveReferenceValue(elementName, assignment.PropertyName, assignment.RawValue, spec.ValueKind, assignment.Source)
                    : ParseAspectLiteralValue(elementName, assignment.PropertyName, assignment.RawValue, spec.ValueKind, assignment.Source);

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

        private GeneratedExpression? ParseAspectLiteralValue(string elementName, string propertyName, string value, MarkupValueKind kind, XObject source)
        {
            XAttribute synthetic = new(propertyName, value);
            return ParseLiteralValue(elementName, propertyName, synthetic, value, kind);
        }

        private GeneratedExpression? ResolveReferenceValue(string elementName, string propertyName, string referenceName, MarkupValueKind targetKind, XObject source)
        {
            if (!symbols.TryGetValue(referenceName, out NamedSymbol? symbol))
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

        private void EmitProperty(XElement element, string variable, XAttribute attribute)
        {
            string elementName = element.Name.LocalName;
            string propertyName = attribute.Name.LocalName;
            string value = attribute.Value;

            PropertySpec? spec = FindPropertySpec(elementName, propertyName);
            if (spec is null || !spec.Assignable)
            {
                if (!HasErrors)
                {
                    Report(UnsupportedProperty, attribute, elementName, propertyName);
                }

                return;
            }

            GeneratedExpression? expression = ParseLiteralValue(elementName, propertyName, attribute, value, spec.ValueKind);
            if (expression is null)
            {
                return;
            }

            currentLines.Add(reactiveDocument
                ? variable + ".SetValue(" + spec.PropertyCode + ", " + expression.Code +
                    ", global::Cerneala.UI.Core.UiPropertyValueSource.MarkupBase);"
                : variable + "." + spec.Name + " = " + expression.Code + ";");
        }

        private static PropertySpec? FindPropertySpec(string elementName, string propertyName)
        {
            return PropertySpecs.FirstOrDefault(spec => spec.Name == propertyName && spec.AppliesToElement(elementName));
        }

        private GeneratedExpression? ParseLiteralValue(string elementName, string propertyName, XAttribute attribute, string value, MarkupValueKind kind)
        {
            string? code = kind switch
            {
                MarkupValueKind.String when !string.IsNullOrWhiteSpace(value) => Literal(value),
                MarkupValueKind.Bool => Bool(elementName, propertyName, attribute),
                MarkupValueKind.Float => Float(elementName, propertyName, attribute),
                MarkupValueKind.PositiveFloat => PositiveFloat(elementName, propertyName, attribute),
                MarkupValueKind.Thickness => Thickness(elementName, propertyName, attribute),
                MarkupValueKind.NonNegativeThickness => NonNegativeThickness(elementName, propertyName, attribute),
                MarkupValueKind.DrawColor => Color(elementName, propertyName, attribute),
                _ => null
            };

            if (code is null)
            {
                if (kind == MarkupValueKind.String)
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

        private static bool IsControlElement(string elementName)
        {
            return elementName is "Border" or "Button" or "TextBlock" or "UserControl";
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
