using System;
using System.Collections.Immutable;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Cerneala.Language;
using Cerneala.Language.Diagnostics;
using Cerneala.Language.Semantics;
using Cerneala.Language.Semantics.Symbols;
using Cerneala.Language.Syntax;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;
using LanguageSourceText = Cerneala.Language.Text.SourceText;
using LanguageTextSpan = Cerneala.Language.Text.TextSpan;

namespace Cerneala.SourceGen;

[Generator]
public sealed partial class UiMarkupGenerator : IIncrementalGenerator
{
    private static readonly Dictionary<string, string> NamedColorNames = new(StringComparer.OrdinalIgnoreCase)
    {
        ["Transparent"] = "Transparent",
        ["AliceBlue"] = "AliceBlue",
        ["AntiqueWhite"] = "AntiqueWhite",
        ["Aqua"] = "Aqua",
        ["Aquamarine"] = "Aquamarine",
        ["Azure"] = "Azure",
        ["Beige"] = "Beige",
        ["Bisque"] = "Bisque",
        ["Black"] = "Black",
        ["BlanchedAlmond"] = "BlanchedAlmond",
        ["Blue"] = "Blue",
        ["BlueViolet"] = "BlueViolet",
        ["Brown"] = "Brown",
        ["BurlyWood"] = "BurlyWood",
        ["CadetBlue"] = "CadetBlue",
        ["Chartreuse"] = "Chartreuse",
        ["Chocolate"] = "Chocolate",
        ["Coral"] = "Coral",
        ["CornflowerBlue"] = "CornflowerBlue",
        ["Cornsilk"] = "Cornsilk",
        ["Crimson"] = "Crimson",
        ["Cyan"] = "Cyan",
        ["DarkBlue"] = "DarkBlue",
        ["DarkCyan"] = "DarkCyan",
        ["DarkGoldenrod"] = "DarkGoldenrod",
        ["DarkGray"] = "DarkGray",
        ["DarkGreen"] = "DarkGreen",
        ["DarkKhaki"] = "DarkKhaki",
        ["DarkMagenta"] = "DarkMagenta",
        ["DarkOliveGreen"] = "DarkOliveGreen",
        ["DarkOrange"] = "DarkOrange",
        ["DarkOrchid"] = "DarkOrchid",
        ["DarkRed"] = "DarkRed",
        ["DarkSalmon"] = "DarkSalmon",
        ["DarkSeaGreen"] = "DarkSeaGreen",
        ["DarkSlateBlue"] = "DarkSlateBlue",
        ["DarkSlateGray"] = "DarkSlateGray",
        ["DarkTurquoise"] = "DarkTurquoise",
        ["DarkViolet"] = "DarkViolet",
        ["DeepPink"] = "DeepPink",
        ["DeepSkyBlue"] = "DeepSkyBlue",
        ["DimGray"] = "DimGray",
        ["DodgerBlue"] = "DodgerBlue",
        ["Firebrick"] = "Firebrick",
        ["FloralWhite"] = "FloralWhite",
        ["ForestGreen"] = "ForestGreen",
        ["Fuchsia"] = "Fuchsia",
        ["Gainsboro"] = "Gainsboro",
        ["GhostWhite"] = "GhostWhite",
        ["Gold"] = "Gold",
        ["Goldenrod"] = "Goldenrod",
        ["Gray"] = "Gray",
        ["Green"] = "Green",
        ["GreenYellow"] = "GreenYellow",
        ["Honeydew"] = "Honeydew",
        ["HotPink"] = "HotPink",
        ["IndianRed"] = "IndianRed",
        ["Indigo"] = "Indigo",
        ["Ivory"] = "Ivory",
        ["Khaki"] = "Khaki",
        ["Lavender"] = "Lavender",
        ["LavenderBlush"] = "LavenderBlush",
        ["LawnGreen"] = "LawnGreen",
        ["LemonChiffon"] = "LemonChiffon",
        ["LightBlue"] = "LightBlue",
        ["LightCoral"] = "LightCoral",
        ["LightCyan"] = "LightCyan",
        ["LightGoldenrodYellow"] = "LightGoldenrodYellow",
        ["LightGray"] = "LightGray",
        ["LightGreen"] = "LightGreen",
        ["LightPink"] = "LightPink",
        ["LightSalmon"] = "LightSalmon",
        ["LightSeaGreen"] = "LightSeaGreen",
        ["LightSkyBlue"] = "LightSkyBlue",
        ["LightSlateGray"] = "LightSlateGray",
        ["LightSteelBlue"] = "LightSteelBlue",
        ["LightYellow"] = "LightYellow",
        ["Lime"] = "Lime",
        ["LimeGreen"] = "LimeGreen",
        ["Linen"] = "Linen",
        ["Magenta"] = "Magenta",
        ["Maroon"] = "Maroon",
        ["MediumAquamarine"] = "MediumAquamarine",
        ["MediumBlue"] = "MediumBlue",
        ["MediumOrchid"] = "MediumOrchid",
        ["MediumPurple"] = "MediumPurple",
        ["MediumSeaGreen"] = "MediumSeaGreen",
        ["MediumSlateBlue"] = "MediumSlateBlue",
        ["MediumSpringGreen"] = "MediumSpringGreen",
        ["MediumTurquoise"] = "MediumTurquoise",
        ["MediumVioletRed"] = "MediumVioletRed",
        ["MidnightBlue"] = "MidnightBlue",
        ["MintCream"] = "MintCream",
        ["MistyRose"] = "MistyRose",
        ["Moccasin"] = "Moccasin",
        ["NavajoWhite"] = "NavajoWhite",
        ["Navy"] = "Navy",
        ["OldLace"] = "OldLace",
        ["Olive"] = "Olive",
        ["OliveDrab"] = "OliveDrab",
        ["Orange"] = "Orange",
        ["OrangeRed"] = "OrangeRed",
        ["Orchid"] = "Orchid",
        ["PaleGoldenrod"] = "PaleGoldenrod",
        ["PaleGreen"] = "PaleGreen",
        ["PaleTurquoise"] = "PaleTurquoise",
        ["PaleVioletRed"] = "PaleVioletRed",
        ["PapayaWhip"] = "PapayaWhip",
        ["PeachPuff"] = "PeachPuff",
        ["Peru"] = "Peru",
        ["Pink"] = "Pink",
        ["Plum"] = "Plum",
        ["PowderBlue"] = "PowderBlue",
        ["Purple"] = "Purple",
        ["Red"] = "Red",
        ["RosyBrown"] = "RosyBrown",
        ["RoyalBlue"] = "RoyalBlue",
        ["SaddleBrown"] = "SaddleBrown",
        ["Salmon"] = "Salmon",
        ["SandyBrown"] = "SandyBrown",
        ["SeaGreen"] = "SeaGreen",
        ["SeaShell"] = "SeaShell",
        ["Sienna"] = "Sienna",
        ["Silver"] = "Silver",
        ["SkyBlue"] = "SkyBlue",
        ["SlateBlue"] = "SlateBlue",
        ["SlateGray"] = "SlateGray",
        ["Snow"] = "Snow",
        ["SpringGreen"] = "SpringGreen",
        ["SteelBlue"] = "SteelBlue",
        ["Tan"] = "Tan",
        ["Teal"] = "Teal",
        ["Thistle"] = "Thistle",
        ["Tomato"] = "Tomato",
        ["Turquoise"] = "Turquoise",
        ["Violet"] = "Violet",
        ["Wheat"] = "Wheat",
        ["White"] = "White",
        ["WhiteSmoke"] = "WhiteSmoke",
        ["Yellow"] = "Yellow",
        ["YellowGreen"] = "YellowGreen",
    };

    private static readonly DiagnosticDescriptor MalformedMarkup = SourceGeneratorDiagnosticAdapter.GetDescriptor("CERNEALAUI001");
    private static readonly DiagnosticDescriptor UnsupportedElement = SourceGeneratorDiagnosticAdapter.GetDescriptor("CERNEALAUI002");
    private static readonly DiagnosticDescriptor UnsupportedProperty = SourceGeneratorDiagnosticAdapter.GetDescriptor("CERNEALAUI003");
    private static readonly DiagnosticDescriptor InvalidPropertyValue = SourceGeneratorDiagnosticAdapter.GetDescriptor("CERNEALAUI004");
    private static readonly DiagnosticDescriptor InvalidDocumentShape = SourceGeneratorDiagnosticAdapter.GetDescriptor("CERNEALAUI005");
    private static readonly DiagnosticDescriptor InvalidDirective = SourceGeneratorDiagnosticAdapter.GetDescriptor("CERNEALAUI006");
    private static readonly DiagnosticDescriptor InvalidBindingSource = SourceGeneratorDiagnosticAdapter.GetDescriptor("CERNEALAUI007");
    private static readonly DiagnosticDescriptor InvalidUserControl = SourceGeneratorDiagnosticAdapter.GetDescriptor("CERNEALAUI008");
    private static readonly DiagnosticDescriptor InvalidEventHandler = SourceGeneratorDiagnosticAdapter.GetDescriptor("CERNEALAUI009");
    private static readonly DiagnosticDescriptor InvalidWindow = SourceGeneratorDiagnosticAdapter.GetDescriptor("CERNEALAUI010");
    private static readonly DiagnosticDescriptor InvalidWindowStartup = SourceGeneratorDiagnosticAdapter.GetDescriptor("CERNEALAUI011");
    private static readonly DiagnosticDescriptor InvalidComponentTemplate = SourceGeneratorDiagnosticAdapter.GetDescriptor("CERNEALAUI012");
    private static readonly DiagnosticDescriptor InvalidApplication = SourceGeneratorDiagnosticAdapter.GetDescriptor("CERNEALAUI013");
    private static readonly DiagnosticDescriptor InvalidApplicationStartup = SourceGeneratorDiagnosticAdapter.GetDescriptor("CERNEALAUI014");
    private static readonly DiagnosticDescriptor InvalidApplicationBackendSelection = SourceGeneratorDiagnosticAdapter.GetDescriptor("CERNEALAUI015");
    private static readonly DiagnosticDescriptor MotionSyntaxDiagnostic = SourceGeneratorDiagnosticAdapter.GetDescriptor("CERNEALAUI020");
    private static readonly DiagnosticDescriptor MotionTargetDiagnostic = SourceGeneratorDiagnosticAdapter.GetDescriptor("CERNEALAUI021");
    private static readonly DiagnosticDescriptor MotionEventDiagnostic = SourceGeneratorDiagnosticAdapter.GetDescriptor("CERNEALAUI022");
    private static readonly DiagnosticDescriptor MotionTypeDiagnostic = SourceGeneratorDiagnosticAdapter.GetDescriptor("CERNEALAUI023");
    private static readonly DiagnosticDescriptor MotionCompositionDiagnostic = SourceGeneratorDiagnosticAdapter.GetDescriptor("CERNEALAUI024");
    private static readonly DiagnosticDescriptor MotionLifecycleDiagnostic = SourceGeneratorDiagnosticAdapter.GetDescriptor("CERNEALAUI025");
    private static readonly DiagnosticDescriptor MotionCapabilityDiagnostic = SourceGeneratorDiagnosticAdapter.GetDescriptor("CERNEALAUI026");

    private enum MotionDiagnosticKind
    {
        Syntax,
        Target,
        Event,
        Type,
        Composition,
        Lifecycle,
        Capability
    }

    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        IncrementalValuesProvider<MarkupSource> markupFiles = context.AdditionalTextsProvider
            .Where(static file => CernealaDocumentPath.IsMarkupFile(file.Path))
            .Select(static (file, cancellationToken) => new MarkupSource(
                file.Path,
                file.GetText(cancellationToken)?.ToString()));

        IncrementalValueProvider<ImmutableArray<MarkupSource>> applicationFiles = markupFiles
            .Where(static file => file.Document?.Root.Name.LocalName == "Application")
            .Collect();
        IncrementalValueProvider<SemanticAnalysisContext> semanticContext = applicationFiles
            .Combine(context.CompilationProvider)
            .Select(static (input, _) => new SemanticAnalysisContext(input.Left, input.Right));
        IncrementalValuesProvider<SemanticMarkupSource> semanticMarkupFiles = markupFiles
            .Combine(semanticContext)
            .Select(static (input, cancellationToken) => AnalyzeMarkupFile(
                input.Left,
                input.Right,
                cancellationToken))
            .WithTrackingName("CernealaLanguageSemanticModel");

        context.RegisterSourceOutput(
            semanticMarkupFiles.Collect().Combine(context.CompilationProvider),
            static (sourceContext, input) => GenerateFiles(sourceContext, input.Left, input.Right));
    }

    private static SemanticMarkupSource AnalyzeMarkupFile(
        MarkupSource file,
        SemanticAnalysisContext context,
        CancellationToken cancellationToken)
    {
        MarkupSource[] semanticInputs = context.ApplicationFiles
            .Append(file)
            .GroupBy(candidate => candidate.Path, StringComparer.OrdinalIgnoreCase)
            .Select(group => group.Last())
            .ToArray();
        CernealaDocument[] languageDocuments = semanticInputs
            .Select(file => file.LanguageDocument)
            .OfType<CernealaDocument>()
            .ToArray();
        using CernealaCompilation languageCompilation = new(
            context.Symbols,
            languageDocuments,
            AnalysisMode.Build);
        SourceGeneratorSemanticModel semanticModel = SourceGeneratorSemanticModel.Create(
            languageCompilation.GetSemanticModel(file.Path, cancellationToken));
        return new SemanticMarkupSource(file, semanticModel);
    }

    private sealed class SemanticAnalysisContext
    {
        public SemanticAnalysisContext(ImmutableArray<MarkupSource> applicationFiles, Compilation compilation)
        {
            ApplicationFiles = applicationFiles;
            Symbols = new RoslynCompilationSymbols(compilation);
        }

        public ImmutableArray<MarkupSource> ApplicationFiles { get; }

        public RoslynCompilationSymbols Symbols { get; }
    }

    private static void GenerateFiles(SourceProductionContext context, ImmutableArray<SemanticMarkupSource> inputs, Compilation compilation)
    {
        ImmutableArray<MarkupSource> files = inputs
            .Select(input => new MarkupSource(input.Source.Path, input.Source.Text))
            .ToImmutableArray();
        IReadOnlyDictionary<string, SourceGeneratorSemanticModel> semanticModels = inputs.ToDictionary(
            input => input.Source.Path,
            input => input.SemanticModel,
            StringComparer.OrdinalIgnoreCase);
        string[] classNames = AssignClassNames(files);
        MarkupSource[] applicationDocuments = files
            .Where(file => file.Document?.Root.Name.LocalName == "Application")
            .ToArray();
        if (applicationDocuments.Length > 1 && compilation.Options.OutputKind != OutputKind.DynamicallyLinkedLibrary)
        {
            context.ReportDiagnostic(Diagnostic.Create(
                InvalidApplicationStartup,
                CreateLocation(applicationDocuments[1], new object()),
                Path.GetFileName(applicationDocuments[1].Path),
                "An executable project may contain only one Application definition."));
            return;
        }

        ApplicationPairResolution[] applicationPairs = files
            .Select(file => ResolveApplicationPair(context, file, compilation))
            .ToArray();
        int applicationCount = applicationPairs.Count(resolution => resolution.Pair is not null);
        bool hasApplicationDocument = applicationPairs.Any(resolution => resolution.HasCompanion);
        if (applicationCount > 1 && compilation.Options.OutputKind != OutputKind.DynamicallyLinkedLibrary)
        {
            context.ReportDiagnostic(Diagnostic.Create(
                InvalidApplicationStartup,
                Location.None,
                "<Application>",
                "An executable project may contain only one paired Application definition."));
            return;
        }

        int applicationIndex = Array.FindIndex(applicationPairs, resolution => resolution.Pair is not null);
        WindowPairResolution[] windowPairs = files
            .Select((file, index) => applicationPairs[index].HasCompanion
                ? default
                : ResolveWindowPair(context, file, compilation))
            .ToArray();
        int mainWindowCount = windowPairs.Count(resolution => resolution.Pair?.TypeSymbol.Name == "MainWindow");
        if (mainWindowCount > 1 && compilation.Options.OutputKind != OutputKind.DynamicallyLinkedLibrary)
        {
            context.ReportDiagnostic(Diagnostic.Create(
                InvalidWindowStartup,
                Location.None,
                "An executable project may contain only one paired Window class named 'MainWindow'."));
        }

        bool executable = compilation.Options.OutputKind != OutputKind.DynamicallyLinkedLibrary;
        int legacyStartupIndex = Array.FindIndex(
            windowPairs,
            resolution => resolution.Pair?.TypeSymbol.Name == "MainWindow");
        int startupIndex = executable && applicationIndex >= 0 && applicationCount == 1
            ? applicationIndex
            : executable && !hasApplicationDocument && mainWindowCount == 1
                ? legacyStartupIndex
                : -1;
        ApplicationBackendSelection? startupBackend = null;
        if (startupIndex >= 0)
        {
            Location fallbackLocation = CreateLocation(
                files[startupIndex],
                files[startupIndex].Document?.Root ?? new object());
            if (!TryResolveApplicationBackend(
                    context,
                    compilation,
                    fallbackLocation,
                    out ApplicationBackendSelection resolvedBackend))
            {
                return;
            }

            startupBackend = resolvedBackend;
        }

        GenerationScope.ApplicationResourceCatalog? applicationResources = null;
        if (applicationIndex >= 0 && applicationCount == 1)
        {
            applicationResources = GenerateApplicationFile(
                context,
                files[applicationIndex],
                classNames[applicationIndex],
                compilation,
                applicationPairs[applicationIndex].Pair!,
                startupBackend,
                semanticModels[files[applicationIndex].Path]);
        }

        for (int i = 0; i < files.Length; i++)
        {
            ApplicationPairResolution applicationPair = applicationPairs[i];
            if (applicationPair.HasCompanion)
            {
                continue;
            }

            WindowPairResolution windowPair = windowPairs[i];
            if (windowPair.HasCompanion)
            {
                if (windowPair.Pair is not null)
                {
                    bool generateStartup =
                        executable &&
                        !hasApplicationDocument &&
                        mainWindowCount == 1 &&
                        windowPair.Pair.TypeSymbol.Name == "MainWindow";
                    GenerateWindowFile(
                        context,
                        files[i],
                        classNames[i],
                        compilation,
                        windowPair.Pair,
                        generateStartup,
                        startupBackend,
                        applicationResources,
                        semanticModels[files[i].Path]);
                }

                continue;
            }

            UserControlPairResolution pair = ResolveUserControlPair(context, files[i], compilation);
            if (pair.HasCompanion)
            {
                if (pair.Pair is not null)
                {
                    GenerateUserControlFile(
                        context,
                        files[i],
                        classNames[i],
                        compilation,
                        pair.Pair,
                        applicationResources,
                        semanticModels[files[i].Path]);
                }

                continue;
            }

            GenerateFile(
                context,
                files[i],
                classNames[i],
                compilation,
                applicationResources,
                semanticModels[files[i].Path]);
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

    private static void GenerateFile(
        SourceProductionContext context,
        MarkupSource file,
        string className,
        Compilation compilation,
        GenerationScope.ApplicationResourceCatalog? applicationResources,
        SourceGeneratorSemanticModel semanticModel)
    {
        if (file.Text is null)
        {
            return;
        }

        if (!TryGetEmissionDocument(context, file, semanticModel, out EmissionMarkupDocument document))
        {
            return;
        }
        MarkupAttribute? nestedDataType = document.Root.Descendants()
            .Where(element => element.Name.LocalName != "ContentTemplate")
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

        GenerationScope scope = new(
            context,
            file,
            document,
            compilation,
            dataType,
            semanticModel,
            applicationResources: applicationResources);
        if (scope.HasErrors)
        {
            return;
        }

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
        foreach (string line in scope.PrismDeclarationLines)
        {
            source.Append("    ").AppendLine(line);
        }
        if (scope.PrismDeclarationLines.Count > 0)
        {
            source.AppendLine();
        }

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
        EmissionMarkupDocument document,
        Compilation compilation)
    {
        MarkupAttribute? attribute = document.Root.Attribute("DataType");
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

    private static bool TryGetEmissionDocument(
        SourceProductionContext context,
        MarkupSource file,
        SourceGeneratorSemanticModel semanticModel,
        out EmissionMarkupDocument document)
    {
        document = file.Document!;
        if (document is not null && !semanticModel.Diagnostics.Any(diagnostic =>
            diagnostic.Severity == LanguageDiagnosticSeverity.Error))
        {
            return true;
        }

        SourceText source = SourceText.From(file.Text ?? string.Empty, Encoding.UTF8);
        foreach (LanguageDiagnostic diagnostic in semanticModel.Diagnostics)
        {
            context.ReportDiagnostic(SourceGeneratorDiagnosticAdapter.ToDiagnostic(diagnostic, file.Path, source));
        }

        return false;
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
        return CreateIdentifier(CernealaDocumentPath.GetLogicalName(path));
    }

    private static string CreateDisambiguatedClassName(string path)
    {
        string? directoryName = Path.GetDirectoryName(path);
        string? parentName = string.IsNullOrEmpty(directoryName) ? null : Path.GetFileName(directoryName);
        string baseName = CernealaDocumentPath.GetLogicalName(path);
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
            if (text is null)
            {
                LanguageDocument = null;
                Document = null;
                return;
            }

            LanguageDocument = new CernealaDocument(
                path,
                LanguageSourceText.From(StripXmlDeclarationPreservingPositions(text)));
            ElementSyntax[] roots = LanguageDocument.Syntax.Children.OfType<ElementSyntax>().ToArray();
            Document = roots.Length == 1
                ? new EmissionMarkupDocument(MarkupElement.FromSyntax(roots[0]))
                : null;
        }

        public string Path { get; }

        public string? Text { get; }

        public CernealaDocument? LanguageDocument { get; }

        public EmissionMarkupDocument? Document { get; }
    }

    private readonly struct SemanticMarkupSource
    {
        public SemanticMarkupSource(MarkupSource source, SourceGeneratorSemanticModel semanticModel)
        {
            Source = source;
            SemanticModel = semanticModel;
        }

        public MarkupSource Source { get; }

        public SourceGeneratorSemanticModel SemanticModel { get; }
    }

    private sealed partial class GenerationScope
    {
        private readonly SourceProductionContext context;
        private readonly MarkupSource file;
        private readonly EmissionMarkupDocument document;
        private readonly Compilation compilation;
        private readonly SourceGeneratorSemanticModel semanticModel;
        private readonly INamedTypeSymbol? dataType;
        private readonly UserControlPair? userControlPair;
        private readonly ApplicationResourceCatalog? applicationResources;
        private readonly HashSet<string> reportedDiagnostics = new(StringComparer.Ordinal);
        private readonly bool reactiveDocument;
        private string? documentRootVariable;
        private int nextId;

        public GenerationScope(
            SourceProductionContext context,
            MarkupSource file,
            EmissionMarkupDocument document,
            Compilation compilation,
            INamedTypeSymbol? dataType,
            SourceGeneratorSemanticModel semanticModel,
            UserControlPair? userControlPair = null,
            ApplicationResourceCatalog? applicationResources = null)
        {
            this.context = context;
            this.file = file;
            this.document = document;
            this.compilation = compilation;
            this.dataType = dataType;
            this.semanticModel = semanticModel;
            this.userControlPair = userControlPair;
            this.applicationResources = applicationResources;
            currentLines = Lines;
            currentPostLines = PostLines;

            ReportSharedDiagnostics();
            if (HasErrors)
            {
                return;
            }

            ReadResources();
            ReadInlineAspects();
            ImportApplicationAspects();
            DirectiveParseResult[] elementDirectiveContent = document.Root
                .DescendantsAndSelf()
                .Select(element => GetDirectiveContent(
                    element,
                    DirectiveContentKind.Elements |
                    DirectiveContentKind.Templates |
                    DirectiveContentKind.Prism))
                .ToArray();
            reactiveDocument = allAspects.Any(aspect => aspect.Conditions.Count > 0) ||
                elementDirectiveContent.Any(content => content.HasDirectives);
            BindPrism();
            EmitAspectTemplates();
        }

        public List<string> Lines { get; } = new();

        public List<string> PostLines { get; } = new();

        public string? DataTypeCode => dataType?.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);

        private ITypeSymbol? CurrentDataType
        {
            get
            {
                if (contentTemplateDataTypes.Count > 0)
                {
                    int inheritedLocalDepth = contentTemplateLocalDataContextDepths.Peek();
                    return localDataContextTypes.Count > inheritedLocalDepth
                        ? localDataContextTypes.Peek()
                        : contentTemplateDataTypes.Peek();
                }

                return localDataContextTypes.Count > 0
                    ? localDataContextTypes.Peek()
                    : dataType;
            }
        }

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
            LayoutPoint,
            Color,
            Brush,
            ContentTemplate,
            Enum,
            Unsupported
        }

        private enum NamedSymbolKind
        {
            Element,
            Brush,
            Aspect,
            MotionSpec,
            MotionClip
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

        private sealed class GeneratedExpression
        {
            public GeneratedExpression(string code, MarkupValueKind kind, string? applicationResourceName = null)
            {
                Code = code;
                Kind = kind;
                ApplicationResourceName = applicationResourceName;
            }

            public string Code { get; }

            public MarkupValueKind Kind { get; }

            public string? ApplicationResourceName { get; }
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

        private sealed class BrushResource
        {
            public BrushResource(string name, string variable, string expression, MarkupElement source, string? colorExpression = null)
            {
                Name = name;
                Variable = variable;
                Expression = expression;
                ColorExpression = colorExpression;
                Source = source;
            }

            public string Name { get; }

            public string Variable { get; }

            public string Expression { get; }

            public string? ColorExpression { get; }

            public MarkupElement Source { get; }
        }

        private sealed class AspectResource
        {
            public AspectResource(
                string? name,
                string targetName,
                IReadOnlyList<AspectPropertyAssignment> assignments,
                IReadOnlyList<DirectiveWhenNode> conditions,
                IReadOnlyList<DirectiveOnNode> eventTriggers,
                MotionPresenceNode? presence,
                MotionLayoutNode? layout,
                IReadOnlyList<MotionScrollNode> scrolls,
                MotionDragNode? drag,
                MotionGesturePressNode? gesturePress,
                DirectiveTemplateNode? template,
                MarkupElement source,
                bool isInline = false)
            {
                Name = name;
                TargetName = targetName;
                Assignments = assignments;
                Conditions = conditions;
                EventTriggers = eventTriggers;
                Presence = presence;
                Layout = layout;
                Scrolls = scrolls;
                Drag = drag;
                GesturePress = gesturePress;
                Template = template;
                Source = source;
                IsInline = isInline;
            }

            public string? Name { get; }

            public string TargetName { get; }

            public IReadOnlyList<AspectPropertyAssignment> Assignments { get; }

            public IReadOnlyList<DirectiveWhenNode> Conditions { get; }

            public IReadOnlyList<DirectiveOnNode> EventTriggers { get; }

            public MotionPresenceNode? Presence { get; }

            public MotionLayoutNode? Layout { get; }

            public IReadOnlyList<MotionScrollNode> Scrolls { get; }

            public MotionDragNode? Drag { get; }

            public MotionGesturePressNode? GesturePress { get; }

            public DirectiveTemplateNode? Template { get; }

            public MarkupElement Source { get; }

            public bool IsInline { get; }

            public string? RuntimeVariable { get; set; }

            public string? RuntimeResourceVariable { get; set; }

            public bool BehaviorOwnedByPackage { get; set; }

            public ReactivePlan? ReactivePlan { get; set; }

            public List<string> ConditionKeyVariables { get; } = [];

            public bool ConditionKeysEmitted { get; set; }

            public IReadOnlyList<string>? BehaviorLines { get; set; }

            public IReadOnlyList<string>? BehaviorPostLines { get; set; }

            public IReadOnlyList<string> BehaviorLifetimeVariables { get; set; } = [];

            public string? TemplateVariable { get; set; }
        }

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

        private sealed class AspectPropertyAssignment
        {
            public AspectPropertyAssignment(string propertyName, string rawValue, bool isReference, MarkupObject source)
            {
                PropertyName = propertyName;
                RawValue = rawValue;
                IsReference = isReference;
                Source = source;
            }

            public string PropertyName { get; }

            public string RawValue { get; }

            public bool IsReference { get; }

            public MarkupObject Source { get; }
        }

        private sealed class ResourceScope
        {
            public ResourceScope(MarkupElement owner)
            {
                Owner = owner;
            }

            public MarkupElement Owner { get; }

            public Dictionary<string, NamedSymbol> NamedResources { get; } = new(StringComparer.Ordinal);

            public Dictionary<string, AspectResource> DefaultAspectsByTarget { get; } = new(StringComparer.Ordinal);

            public List<PrismCompositionResourceSyntax> PrismCompositions { get; } = [];

            public List<object> RuntimeResources { get; } = [];
        }

        public sealed class ApplicationResourceCatalog
        {
            private readonly HashSet<object> symbols;

            public ApplicationResourceCatalog(
                IReadOnlyDictionary<string, object> namedResources,
                IReadOnlyDictionary<string, object> defaultAspects,
                IReadOnlyDictionary<string, object> prismCompositions)
            {
                NamedResources = namedResources;
                DefaultAspects = defaultAspects;
                PrismCompositions = prismCompositions;
                symbols = new HashSet<object>(namedResources.Values);
            }

            public IReadOnlyDictionary<string, object> NamedResources { get; }

            public IReadOnlyDictionary<string, object> DefaultAspects { get; }

            public IReadOnlyDictionary<string, object> PrismCompositions { get; }

            public bool Contains(object symbol) => symbols.Contains(symbol);
        }

        private readonly struct ColorLiteral
        {
            public ColorLiteral(byte r, byte g, byte b, byte a)
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
                    ? "new global::Cerneala.Drawing.Color(" + R + ", " + G + ", " + B + ")"
                    : "new global::Cerneala.Drawing.Color(" + R + ", " + G + ", " + B + ", " + A + ")";
            }
        }

        private readonly Dictionary<string, NamedSymbol> symbols = new(StringComparer.Ordinal);
        private readonly Dictionary<MarkupElement, ResourceScope> resourceScopes = new();
        private readonly Dictionary<MarkupElement, ResourceScope> resourcePropertyScopes = new();
        private readonly Dictionary<MarkupElement, AspectResource> inlineAspects = new();
        private readonly List<AspectResource> allAspects = [];
        private readonly Dictionary<MarkupElement, DirectiveParseResult> directiveContent = new();
        private readonly Dictionary<string, INamedTypeSymbol> resolvedElementTypes = new(StringComparer.Ordinal);
        private readonly Dictionary<string, PropertySpec> resolvedProperties = new(StringComparer.Ordinal);
        private readonly Dictionary<string, IReadOnlyList<NamedElementMember>> conditionalFactoryMembers = new(StringComparer.Ordinal);
        private readonly Stack<List<NamedElementMember>> conditionalMemberScopes = new();
        private readonly Stack<TemplateEmissionContext> templateEmissionContexts = new();
        private readonly Stack<INamedTypeSymbol?> contentTemplateDataTypes = new();
        private readonly Stack<string> contentTemplateContextVariables = new();
        private readonly Stack<int> contentTemplateLocalDataContextDepths = new();
        private readonly Stack<ITypeSymbol> localDataContextTypes = new();
        private readonly Dictionary<DirectiveTemplateNode, IReadOnlyDictionary<string, MarkupElement>> templateParts = new();
        private readonly List<NamedElementMember> namedElementMembers = [];
        private List<string> currentLines;
        private List<string> currentPostLines;
        private int nextReactiveId;
        private int nextResourceId;
        private int nextTemplateId;

        private void ImportApplicationAspects()
        {
            if (applicationResources is null)
            {
                return;
            }

            IEnumerable<AspectResource> applicationAspects = applicationResources.NamedResources.Values
                .OfType<NamedSymbol>()
                .Select(symbol => symbol.Source)
                .OfType<AspectResource>();
            foreach (AspectResource aspect in applicationAspects)
            {
                if (!allAspects.Contains(aspect))
                {
                    allAspects.Add(aspect);
                }
            }
        }

        private void ReadResources()
        {
            MarkupElement[] owners = document.Root.DescendantsAndSelf().ToArray();
            foreach (MarkupElement owner in owners)
            {
                string expectedName = owner.Name.LocalName + ".Resources";
                MarkupElement[] resourceProperties = owner.Elements()
                    .Where(element => element.Name.LocalName.EndsWith(".Resources", StringComparison.Ordinal))
                    .ToArray();
                MarkupElement[] matching = resourceProperties
                    .Where(element => string.Equals(element.Name.LocalName, expectedName, StringComparison.Ordinal))
                    .ToArray();

                foreach (MarkupElement invalid in resourceProperties.Where(element => !matching.Contains(element)))
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
                    foreach (MarkupElement duplicate in matching.Skip(1))
                    {
                        duplicate.Remove();
                    }
                }

                if (matching.Length == 0)
                {
                    continue;
                }

                MarkupElement resources = matching[0];
                if (resources.HasAttributes || resources.Nodes().OfType<MarkupText>().Any(text => !string.IsNullOrWhiteSpace(text.Value)))
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
                foreach (MarkupElement resource in resources.Elements())
                {
                    switch (resource.Name.LocalName)
                    {
                        case "SolidColorBrush":
                        case "LinearGradientBrush":
                        case "RadialGradientBrush":
                        case "ImageBrush":
                        case "DrawingBrush":
                            ReadBrush(scope, resource);
                            break;
                        case "VisualBrush":
                            Report(InvalidDocumentShape, resource, Path.GetFileName(file.Path), "VisualBrush is runtime-only because its source is a live element.");
                            break;
                        case "Aspect":
                            ReadAspect(scope, resource);
                            break;
                        case "ContentTemplate":
                            Report(
                                InvalidDocumentShape,
                                resource,
                                Path.GetFileName(file.Path),
                                "ContentTemplate cannot be declared in Resources. " +
                                "Declare it inline on a template property or inside ItemsControl.Templates.");
                            break;
                        case "Tween":
                        case "Spring":
                            ReadMotionSpecResource(scope, resource);
                            break;
                        case "MotionClip":
                            ReadMotionClip(scope, resource);
                            break;
                        case "PrismComposition":
                            ReadPrismComposition(scope, resource);
                            break;
                        default:
                            Report(UnsupportedElement, resource, resource.Name.LocalName);
                            break;
                    }
                }

                resources.Remove();
            }
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

        private void ReadBrush(ResourceScope scope, MarkupElement resource)
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

            string? expression = resource.Name.LocalName switch
            {
                "SolidColorBrush" => BuildSolidColorBrushExpression(resource, out _),
                "LinearGradientBrush" => BuildLinearGradientBrushExpression(resource),
                "RadialGradientBrush" => BuildRadialGradientBrushExpression(resource),
                "ImageBrush" => BuildImageBrushExpression(resource),
                "DrawingBrush" => BuildDrawingBrushExpression(resource),
                _ => null
            };
            if (expression is null)
            {
                return;
            }

            _ = BuildSolidColorBrushExpression(resource, out string? colorExpression);

            string variable = CreateIdentifier(name) + "Resource" + nextResourceId.ToString(CultureInfo.InvariantCulture);
            nextResourceId++;
            BrushResource brush = new(name, variable, expression, resource, colorExpression);
            scope.NamedResources.Add(name, new NamedSymbol(name, NamedSymbolKind.Brush, brush));
            scope.RuntimeResources.Add(brush);
            string brushType = "global::Cerneala.UI.Media." + resource.Name.LocalName;
            string initializer = brush.Expression.StartsWith("new " + brushType, StringComparison.Ordinal)
                ? "new" + brush.Expression.Substring(("new " + brushType).Length)
                : brush.Expression;
            currentLines.Add(brushType + " " + variable + " = " + initializer + ";");
        }

        private string? BuildSolidColorBrushExpression(MarkupElement resource, out string? colorExpression)
        {
            colorExpression = ParseBrushColor(resource.Attribute("Color"));
            if (colorExpression is null)
            {
                if (resource.Name.LocalName == "SolidColorBrush")
                {
                    Report(InvalidPropertyValue, (object?)resource.Attribute("Color") ?? resource, "SolidColorBrush", "Color", resource.Attribute("Color")?.Value ?? string.Empty);
                }

                return null;
            }

            if (resource.Attribute("Opacity") is null)
            {
                return "new global::Cerneala.UI.Media.SolidColorBrush(" + colorExpression + ")";
            }

            string? opacity = ParseBrushFloat(resource, "Opacity", 1, value => value >= 0 && value <= 1);
            return opacity is null ? null : "new global::Cerneala.UI.Media.SolidColorBrush(" + colorExpression + ", " + opacity + ")";
        }

        private string? BuildLinearGradientBrushExpression(MarkupElement resource)
        {
            string? start = ParseBrushPoint(resource, "StartPoint");
            string? end = ParseBrushPoint(resource, "EndPoint");
            string? stops = ParseGradientStops(resource);
            string? opacity = ParseBrushFloat(resource, "Opacity", 1, value => value >= 0 && value <= 1);
            return start is null || end is null || stops is null || opacity is null
                ? null
                : "new global::Cerneala.UI.Media.LinearGradientBrush(" + start + ", " + end + ", " + stops + ", " + opacity + ")";
        }

        private string? BuildRadialGradientBrushExpression(MarkupElement resource)
        {
            string? center = ParseBrushPoint(resource, "Center");
            string? radiusX = ParseBrushFloat(resource, "RadiusX", 0, value => value > 0);
            string? radiusY = ParseBrushFloat(resource, "RadiusY", 0, value => value > 0);
            string? stops = ParseGradientStops(resource);
            string? opacity = ParseBrushFloat(resource, "Opacity", 1, value => value >= 0 && value <= 1);
            return center is null || radiusX is null || radiusY is null || stops is null || opacity is null
                ? null
                : "new global::Cerneala.UI.Media.RadialGradientBrush(" + center + ", " + radiusX + ", " + radiusY + ", " + stops + ", " + opacity + ")";
        }

        private string? BuildImageBrushExpression(MarkupElement resource)
        {
            string source = resource.Attribute("Source")?.Value.Trim() ?? string.Empty;
            if (source.Length == 0)
            {
                Report(InvalidPropertyValue, resource, "ImageBrush", "Source", source);
                return null;
            }

            string? tileArguments = ParseTileArguments(resource);
            return tileArguments is null
                ? null
                : "new global::Cerneala.UI.Media.ImageBrush(" + Literal(source) + ", " + tileArguments + ")";
        }

        private string? BuildDrawingBrushExpression(MarkupElement resource)
        {
            string? bounds = ParseBrushRect(resource, "ContentBounds");
            if (bounds is null)
            {
                return null;
            }

            List<string> commands = [];
            foreach (MarkupElement child in resource.Elements())
            {
                string? rect = ParseBrushRect(child, "Rect");
                string? color = ParseBrushColor(child.Attribute("Color"));
                if (rect is null || color is null || child.Name.LocalName is not ("FillRectangle" or "FillEllipse"))
                {
                    Report(UnsupportedElement, child, child.Name.LocalName);
                    return null;
                }

                commands.Add("global::Cerneala.Drawing.DrawCommand." + child.Name.LocalName + "(" + rect + ", " + color + ")");
            }

            if (commands.Count == 0)
            {
                Report(InvalidDocumentShape, resource, Path.GetFileName(file.Path), "DrawingBrush requires at least one drawing command.");
                return null;
            }

            string? tileArguments = ParseTileArguments(resource);
            return tileArguments is null
                ? null
                : "new global::Cerneala.UI.Media.DrawingBrush(new global::Cerneala.Drawing.DrawCommand[] { " +
                    string.Join(", ", commands) + " }, " + bounds + ", " + tileArguments + ")";
        }

        private string? ParseGradientStops(MarkupElement resource)
        {
            List<string> stops = [];
            foreach (MarkupElement stop in resource.Elements().Where(element => element.Name.LocalName == "GradientStop"))
            {
                string? offset = ParseBrushFloat(stop, "Offset", float.NaN, value => value >= 0 && value <= 1);
                string? color = ParseBrushColor(stop.Attribute("Color"));
                if (offset is null || color is null)
                {
                    Report(InvalidPropertyValue, stop, resource.Name.LocalName, "GradientStop", stop.ToString(MarkupSaveOptions.DisableFormatting));
                    return null;
                }

                stops.Add("new global::Cerneala.UI.Media.GradientStop(" + offset + ", " + color + ")");
            }

            if (stops.Count == 0)
            {
                Report(InvalidDocumentShape, resource, Path.GetFileName(file.Path), resource.Name.LocalName + " requires at least one GradientStop child.");
                return null;
            }

            return "new global::Cerneala.UI.Media.GradientStop[] { " + string.Join(", ", stops) + " }";
        }

        private string? ParseTileArguments(MarkupElement resource)
        {
            string stretch = resource.Attribute("Stretch")?.Value.Trim() ?? "Fill";
            string alignmentX = resource.Attribute("AlignmentX")?.Value.Trim() ?? "Center";
            string alignmentY = resource.Attribute("AlignmentY")?.Value.Trim() ?? "Center";
            string tileMode = resource.Attribute("TileMode")?.Value.Trim() ?? "None";
            string? viewport = resource.Attribute("Viewport") is null ? "null" : ParseBrushRect(resource, "Viewport");
            string? viewbox = resource.Attribute("Viewbox") is null ? "null" : ParseBrushRect(resource, "Viewbox");
            string? opacity = ParseBrushFloat(resource, "Opacity", 1, value => value >= 0 && value <= 1);
            if (!new[] { "None", "Fill", "Uniform", "UniformToFill" }.Contains(stretch) ||
                !new[] { "Left", "Center", "Right" }.Contains(alignmentX) ||
                !new[] { "Top", "Center", "Bottom" }.Contains(alignmentY) ||
                !new[] { "None", "Tile", "FlipX", "FlipY", "FlipXY" }.Contains(tileMode) ||
                viewport is null || viewbox is null || opacity is null)
            {
                Report(InvalidPropertyValue, resource, resource.Name.LocalName, "Tile", resource.ToString(MarkupSaveOptions.DisableFormatting));
                return null;
            }

            return "global::Cerneala.Drawing.DrawBrushStretch." + stretch + ", global::Cerneala.Drawing.DrawBrushAlignmentX." + alignmentX +
                ", global::Cerneala.Drawing.DrawBrushAlignmentY." + alignmentY + ", " + viewport + ", " + viewbox +
                ", global::Cerneala.Drawing.DrawTileMode." + tileMode + ", " + opacity;
        }

        private string? ParseBrushPoint(MarkupElement element, string attributeName)
        {
            string[] parts = (element.Attribute(attributeName)?.Value ?? string.Empty).Split(',').Select(value => value.Trim()).ToArray();
            if (parts.Length != 2 || !TryParseFiniteFloat(parts[0], out string? x) || !TryParseFiniteFloat(parts[1], out string? y))
            {
                Report(InvalidPropertyValue, (object?)element.Attribute(attributeName) ?? element, element.Name.LocalName, attributeName, element.Attribute(attributeName)?.Value ?? string.Empty);
                return null;
            }

            return "new global::Cerneala.Drawing.DrawPoint(" + x + ", " + y + ")";
        }

        private string? ParseBrushRect(MarkupElement element, string attributeName)
        {
            string[] parts = (element.Attribute(attributeName)?.Value ?? string.Empty).Split(',').Select(value => value.Trim()).ToArray();
            if (parts.Length != 4 || parts.Any(part => !float.TryParse(part, NumberStyles.Float, CultureInfo.InvariantCulture, out float value) || float.IsNaN(value) || float.IsInfinity(value)))
            {
                Report(InvalidPropertyValue, (object?)element.Attribute(attributeName) ?? element, element.Name.LocalName, attributeName, element.Attribute(attributeName)?.Value ?? string.Empty);
                return null;
            }

            return "new global::Cerneala.Drawing.DrawRect(" + string.Join(", ", parts.Select(part => float.Parse(part, CultureInfo.InvariantCulture).ToString("R", CultureInfo.InvariantCulture) + "f")) + ")";
        }

        private string? ParseBrushFloat(MarkupElement element, string attributeName, float defaultValue, Func<float, bool> validate)
        {
            MarkupAttribute? attribute = element.Attribute(attributeName);
            if (attribute is null && !float.IsNaN(defaultValue) && !float.IsInfinity(defaultValue))
            {
                return defaultValue.ToString("R", CultureInfo.InvariantCulture) + "f";
            }

            if (attribute is null || !float.TryParse(attribute.Value, NumberStyles.Float, CultureInfo.InvariantCulture, out float value) || float.IsNaN(value) || float.IsInfinity(value) || !validate(value))
            {
                Report(InvalidPropertyValue, (object?)attribute ?? element, element.Name.LocalName, attributeName, attribute?.Value ?? string.Empty);
                return null;
            }

            return value.ToString("R", CultureInfo.InvariantCulture) + "f";
        }

        private static bool TryParseFiniteFloat(string value, out string? expression)
        {
            if (float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out float parsed) && !float.IsNaN(parsed) && !float.IsInfinity(parsed))
            {
                expression = parsed.ToString("R", CultureInfo.InvariantCulture) + "f";
                return true;
            }

            expression = null;
            return false;
        }

        private static string? ParseBrushColor(MarkupAttribute? attribute)
        {
            if (attribute is null)
            {
                return null;
            }

            string value = attribute.Value.Trim();
            if (NamedColorNames.TryGetValue(value, out string? named))
            {
                return "global::Cerneala.Drawing.Color." + named;
            }

            return ParseHexColor(value) is ColorLiteral color ? color.ToExpression() : null;
        }

        private void ReadAspect(ResourceScope scope, MarkupElement resource)
        {
            MarkupAttribute? targetAttribute = resource.Attribute("TargetType");
            string targetName = targetAttribute?.Value ?? string.Empty;
            if (string.IsNullOrWhiteSpace(targetName))
            {
                Report(InvalidPropertyValue, resource, "Aspect", "TargetType", targetName);
                return;
            }

            targetName = targetName.Trim();
            if (ResolveAspectTargetType(targetName, resource) is null)
            {
                Report(UnsupportedElement, resource, targetName);
                return;
            }

            if (!TryParseAspectBody(
                resource,
                out List<AspectPropertyAssignment> assignments,
                out List<DirectiveWhenNode> conditions,
                out List<DirectiveOnNode> eventTriggers,
                out MotionPresenceNode? presence,
                out MotionLayoutNode? layout,
                out List<MotionScrollNode> scrolls,
                out MotionDragNode? drag,
                out MotionGesturePressNode? gesturePress,
                out DirectiveTemplateNode? template))
            {
                return;
            }

            string? name = resource.Attribute("Name")?.Value;
            string? trimmedName = string.IsNullOrWhiteSpace(name) ? null : name!.Trim();
            if (trimmedName is not null && IsReservedTemplateReference(trimmedName))
            {
                Report(
                    InvalidDocumentShape,
                    (MarkupObject?)resource.Attribute("Name") ?? resource,
                    Path.GetFileName(file.Path),
                    "Resource Name '" + trimmedName + "' is reserved by component templates.");
                return;
            }

            AspectResource aspect = new(
                trimmedName,
                targetName,
                assignments,
                conditions,
                eventTriggers,
                presence,
                layout,
                scrolls,
                drag,
                gesturePress,
                template,
                resource);
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
            MarkupElement[] owners = document.Root.DescendantsAndSelf().ToArray();
            INamedTypeSymbol? uiElementType = compilation.GetTypeByMetadataName("Cerneala.UI.Elements.UIElement");
            foreach (MarkupElement owner in owners.Where(element => !element.Name.LocalName.EndsWith(".Aspect", StringComparison.Ordinal)))
            {
                string expectedName = owner.Name.LocalName + ".Aspect";
                MarkupElement[] propertyElements = owner.Elements()
                    .Where(element => element.Name.LocalName.EndsWith(".Aspect", StringComparison.Ordinal))
                    .ToArray();
                MarkupElement[] matching = propertyElements
                    .Where(element => string.Equals(element.Name.LocalName, expectedName, StringComparison.Ordinal))
                    .ToArray();

                foreach (MarkupElement invalid in propertyElements.Where(element => !matching.Contains(element)))
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
                    foreach (MarkupElement duplicate in matching.Skip(1))
                    {
                        duplicate.Remove();
                    }
                }

                if (matching.Length == 0)
                {
                    continue;
                }

                MarkupElement inline = matching[0];
                if (owner.Attribute("Aspect") is MarkupAttribute aspectAttribute)
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

                MarkupElement aspectBody = inline;
                MarkupElement[] inlineDeclarations = inline.Elements()
                    .Where(element => element.Name.LocalName == "Aspect")
                    .ToArray();
                if (inlineDeclarations.Length == 1 &&
                    inline.Nodes().All(node => node is MarkupElement element
                        ? ReferenceEquals(element, inlineDeclarations[0])
                        : node is MarkupText text && string.IsNullOrWhiteSpace(text.Value)))
                {
                    aspectBody = inlineDeclarations[0];
                    if (aspectBody.HasAttributes)
                    {
                        Report(
                            InvalidDocumentShape,
                            aspectBody,
                            Path.GetFileName(file.Path),
                            "An inline Aspect declaration does not accept attributes.");
                    }
                }

                if (TryParseAspectBody(
                    aspectBody,
                    out List<AspectPropertyAssignment> assignments,
                    out List<DirectiveWhenNode> conditions,
                    out List<DirectiveOnNode> eventTriggers,
                    out MotionPresenceNode? presence,
                    out MotionLayoutNode? layout,
                    out List<MotionScrollNode> scrolls,
                    out MotionDragNode? drag,
                    out MotionGesturePressNode? gesturePress,
                    out DirectiveTemplateNode? template))
                {
                    AspectResource aspect = new(
                        null,
                        owner.Name.LocalName,
                        assignments,
                        conditions,
                        eventTriggers,
                        presence,
                        layout,
                        scrolls,
                        drag,
                        gesturePress,
                        template,
                        aspectBody,
                        isInline: true);
                    inlineAspects.Add(owner, aspect);
                    allAspects.Add(aspect);
                }

                inline.Remove();
            }
        }

        private bool TryParseAspectBody(
            MarkupElement source,
            out List<AspectPropertyAssignment> assignments,
            out List<DirectiveWhenNode> conditions,
            out List<DirectiveOnNode> eventTriggers,
            out MotionPresenceNode? presence,
            out MotionLayoutNode? layout,
            out List<MotionScrollNode> scrolls,
            out MotionDragNode? drag,
            out MotionGesturePressNode? gesturePress,
            out DirectiveTemplateNode? template)
        {
            assignments = [];
            conditions = [];
            eventTriggers = [];
            presence = null;
            layout = null;
            scrolls = [];
            drag = null;
            gesturePress = null;
            template = null;
            DirectiveParseResult parsed = ParseDirectiveContent(
                source,
                DirectiveContentKind.Assignments | DirectiveContentKind.Templates |
                DirectiveContentKind.MotionTriggers | DirectiveContentKind.MotionHandles |
                DirectiveContentKind.MotionPresence | DirectiveContentKind.MotionLayout | DirectiveContentKind.MotionScroll |
                DirectiveContentKind.MotionDrag | DirectiveContentKind.MotionGesture);
            if (parsed.Error is not null)
            {
                ReportMotion(ClassifyMotionParseError(parsed.Error), parsed.ErrorSource ?? source, parsed.Error);
                return false;
            }

            HashSet<string> motionHandles = new(StringComparer.Ordinal);
            foreach (DirectiveNode node in parsed.Nodes)
            {
                if (node is MotionHandleNode handle)
                {
                    if (!motionHandles.Add(handle.Name))
                    {
                        ReportMotion(MotionDiagnosticKind.Composition, handle.Source, "Duplicate Motion handle '" + handle.Name + "'.");
                        return false;
                    }

                    continue;
                }

                if (!ValidateMotionHandleUses(node, motionHandles))
                {
                    return false;
                }

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
                else if (node is DirectiveOnNode on)
                {
                    eventTriggers.Add(on);
                }
                else if (node is MotionPresenceNode declaredPresence)
                {
                    if (presence is not null)
                    {
                        ReportMotion(MotionDiagnosticKind.Lifecycle, declaredPresence.Source, "An Aspect may declare only one @presence block.");
                        return false;
                    }

                    presence = declaredPresence;
                }
                else if (node is MotionLayoutNode declaredLayout)
                {
                    if (layout is not null)
                    {
                        ReportMotion(MotionDiagnosticKind.Lifecycle, declaredLayout.Source, "An Aspect may declare only one @layout statement.");
                        return false;
                    }

                    layout = declaredLayout;
                }
                else if (node is MotionScrollNode scroll)
                {
                    scrolls.Add(scroll);
                }
                else if (node is MotionDragNode declaredDrag)
                {
                    if (drag is not null)
                    {
                        ReportMotion(MotionDiagnosticKind.Lifecycle, declaredDrag.Source, "An Aspect may declare only one @drag statement.");
                        return false;
                    }

                    drag = declaredDrag;
                }
                else if (node is MotionGesturePressNode declaredGesturePress)
                {
                    if (gesturePress is not null)
                    {
                        ReportMotion(MotionDiagnosticKind.Lifecycle, declaredGesturePress.Source, "An Aspect may declare only one @gesture press statement.");
                        return false;
                    }

                    gesturePress = declaredGesturePress;
                }
                else if (node is DirectiveTemplateNode declaredTemplate)
                {
                    if (template is not null)
                    {
                        Report(
                            InvalidComponentTemplate,
                            declaredTemplate.Source,
                            Path.GetFileName(file.Path),
                            "An Aspect may declare only one @template block.");
                        return false;
                    }

                    template = declaredTemplate;
                }
                else
                {
                    Report(InvalidDirective, node.Source, Path.GetFileName(file.Path), "Aspect bodies may contain only @default, @when, @on, @presence, @layout, @scroll, @drag, @gesture press and @template blocks.");
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

        private bool ValidateMotionHandleUses(DirectiveNode node, ISet<string> declaredHandles)
        {
            foreach (MotionExecutionNode execution in EnumerateMotionExecutions(node))
            {
                string? handleName = execution switch
                {
                    MotionRunNode run => run.HandleName,
                    MotionCancelNode cancel => cancel.HandleName,
                    _ => null
                };
                if (handleName is not null && !declaredHandles.Contains(handleName))
                {
                    Report(
                        InvalidDirective,
                        execution.Source,
                        Path.GetFileName(file.Path),
                        "Motion handle '" + handleName + "' is undeclared or used before its @handle declaration.");
                    return false;
                }
            }

            return true;
        }

        private static IEnumerable<MotionExecutionNode> EnumerateMotionExecutions(DirectiveNode node)
        {
            if (node is MotionExecutionNode execution)
            {
                yield return execution;
                if (execution is MotionCompositionNode composition)
                {
                    foreach (MotionExecutionNode child in composition.Children.SelectMany(EnumerateMotionExecutions))
                    {
                        yield return child;
                    }
                }

                yield break;
            }

            IEnumerable<DirectiveNode> children = node switch
            {
                DirectiveOnNode on => on.Body,
                DirectiveWhenNode condition when condition.BooleanBody is not null => condition.BooleanBody,
                DirectiveWhenNode condition => condition.Branches.SelectMany(branch => branch.Body),
                DirectiveIfNode branch => branch.Body,
                DirectiveDefaultNode defaults => defaults.Body,
                _ => []
            };
            foreach (MotionExecutionNode child in children.SelectMany(EnumerateMotionExecutions))
            {
                yield return child;
            }
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

        private IReadOnlyList<AspectPropertyAssignment> ParseAspectAssignments(MarkupElement aspect)
        {
            string text = string.Concat(aspect.Nodes().OfType<MarkupText>().Select(node => node.Value));
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

        private string? RequiredName(MarkupElement element)
        {
            string? name = element.Attribute("Name")?.Value;
            if (string.IsNullOrWhiteSpace(name))
            {
                Report(InvalidPropertyValue, element, element.Name.LocalName, "Name", name ?? string.Empty);
                return null;
            }

            name = name!.Trim();
            if (IsReservedTemplateReference(name))
            {
                Report(
                    InvalidDocumentShape,
                    (MarkupObject?)element.Attribute("Name") ?? element,
                    Path.GetFileName(file.Path),
                    "Resource Name '" + name + "' is reserved by component templates.");
                return null;
            }

            return name;
        }

        private bool AddSymbol(string name, NamedSymbolKind kind, object source, MarkupElement location)
        {
            if (IsReservedTemplateReference(name))
            {
                Report(
                    InvalidDocumentShape,
                    (MarkupObject?)location.Attribute("Name") ?? location,
                    Path.GetFileName(file.Path),
                    "Name '" + name + "' is reserved by component templates.");
                return false;
            }

            if (symbols.ContainsKey(name))
            {
                Report(InvalidDocumentShape, location, Path.GetFileName(file.Path), "Duplicate Name '" + name + "'.");
                return false;
            }

            symbols.Add(name, new NamedSymbol(name, kind, source));
            return true;
        }

        private static bool IsReservedTemplateReference(string name)
        {
            return string.Equals(name, "owner", StringComparison.Ordinal) ||
                string.Equals(name, "self", StringComparison.Ordinal) ||
                string.Equals(name, "root", StringComparison.Ordinal);
        }

        private static ColorLiteral? ParseHexColor(string value)
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
                return new ColorLiteral(r, g, b, 255);
            }

            if (value.Length == 9 &&
                TryByte(value.Substring(1, 2), out byte a) &&
                TryByte(value.Substring(3, 2), out byte rr) &&
                TryByte(value.Substring(5, 2), out byte gg) &&
                TryByte(value.Substring(7, 2), out byte bb))
            {
                return new ColorLiteral(rr, gg, bb, a);
            }

            return null;
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

        private void WithEmissionBuffers(List<string> lines, List<string> postLines, Action action)
        {
            List<string> previousLines = currentLines;
            List<string> previousPostLines = currentPostLines;
            currentLines = lines;
            currentPostLines = postLines;
            try
            {
                action();
            }
            finally
            {
                currentLines = previousLines;
                currentPostLines = previousPostLines;
            }
        }

        private bool IsControlType(INamedTypeSymbol? type)
        {
            INamedTypeSymbol? controlType = compilation.GetTypeByMetadataName("Cerneala.UI.Controls.Control");
            return type is not null && controlType is not null && IsOrDerivesFrom(type, controlType);
        }

        public string EmitElement(MarkupElement element)
        {
            string? requestedName = element.Attribute("Name")?.Value;
            string variable;
            TemplateEmissionContext? templateContext = templateEmissionContexts.Count == 0
                ? null
                : templateEmissionContexts.Peek();
            if (string.IsNullOrWhiteSpace(requestedName) || templateContext?.RegisterParts == true)
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

            currentLines.Add(typeName + " " + variable + " = new();");
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
            DirectiveParseResult parsedContent = GetDirectiveContent(
                element,
                DirectiveContentKind.Elements |
                DirectiveContentKind.Templates |
                DirectiveContentKind.Prism);
            if (ReportPrismSyntaxDiagnostics(parsedContent))
            {
                return variable;
            }

            if (parsedContent.Error is not null)
            {
                Report(InvalidDirective, parsedContent.ErrorSource ?? element, Path.GetFileName(file.Path), parsedContent.Error);
                return variable;
            }

            IReadOnlyList<AspectResource> aspects = ResolveAspects(element);
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

                    if (TryEmitGridAttachedProperty(variable, attribute))
                    {
                        continue;
                    }

                    if (TryEmitServoAttachedProperty(element, variable, attribute))
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

                if (parsedContent.HasDirectives)
                {
                    EmitReactiveContent(element, variable, parsedContent);
                }
                else
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

                                string childVariable = EmitElement(child.Element);
                                EmitChild(element, variable, childVariable);
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

                EmitPrismApplication(element, variable);
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

        private bool TryEmitServoAttachedProperty(
            MarkupElement element,
            string variable,
            MarkupAttribute attribute)
        {
            if (!string.Equals(
                    attribute.Name.LocalName,
                    "Servo.Id",
                    StringComparison.Ordinal))
            {
                return false;
            }

            INamedTypeSymbol? ownerType = compilation.GetTypeByMetadataName(
                "Cerneala.UI.Servo.Servo");
            IFieldSymbol? propertyField = ownerType?
                .GetMembers("IdProperty")
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
                "Id",
                MarkupValueKind.String,
                propertyField.ContainingType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat) +
                    "." + propertyField.Name,
                fieldType.TypeArguments[0]);
            EmitProperty(
                element,
                variable,
                attribute,
                spec,
                "Servo.Id",
                forceUiPropertyAssignment: true);
            return true;
        }

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
            if (propertyElement.Attributes().Any(attribute => !attribute.IsNamespaceDeclaration) ||
                propertyElement.Nodes().OfType<MarkupText>().Any(text => !string.IsNullOrWhiteSpace(text.Value)) ||
                children.Length != 1 ||
                children[0].Name.LocalName != "Scene2D")
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
                string? expression = brush.Name.LocalName switch
                {
                    "SolidColorBrush" => BuildSolidColorBrushExpression(brush, out _),
                    "LinearGradientBrush" => BuildLinearGradientBrushExpression(brush),
                    "RadialGradientBrush" => BuildRadialGradientBrushExpression(brush),
                    "ImageBrush" => BuildImageBrushExpression(brush),
                    "DrawingBrush" => BuildDrawingBrushExpression(brush),
                    _ => null
                };
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

        private void EmitRuntimeResources(MarkupElement owner, string ownerVariable)
        {
            if (!resourceScopes.TryGetValue(owner, out ResourceScope? scope))
            {
                return;
            }

            foreach (object resource in scope.RuntimeResources)
            {
                switch (resource)
                {
                    case BrushResource brush:
                        currentLines.Add(
                            ownerVariable + ".Resources.SetResource(new global::Cerneala.UI.Resources.ResourceId<global::Cerneala.UI.Media.Brush>(" +
                            Literal(brush.Name) + "), " + brush.Variable + ");");
                        break;
                    case AspectResource aspect:
                        string targetType = ResolveAspectTargetType(aspect.TargetName, aspect.Source)!;
                        string key = aspect.Name is null ? "typeof(" + targetType + ")" : Literal(aspect.Name);
                        if (aspect.Name is null)
                        {
                            EmitAspectPackageResource(ownerVariable, key, targetType, aspect);
                        }
                        else
                        {
                            EmitNamedElementAspectResource(ownerVariable, key, targetType, aspect);
                        }
                        break;
                }
            }
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

        private static bool HasRuntimeBehavior(AspectResource aspect) =>
            aspect.Conditions.Count > 0 ||
            aspect.EventTriggers.Count > 0 ||
            aspect.Presence is not null ||
            aspect.Layout is not null ||
            aspect.Scrolls.Count > 0 ||
            aspect.Drag is not null ||
            aspect.GesturePress is not null;

        private static bool SupportsPackageBehavior(AspectResource aspect) =>
            aspect.Presence is null &&
            aspect.Layout is null &&
            aspect.Scrolls.Count == 0 &&
            aspect.Drag is null &&
            aspect.GesturePress is null &&
            aspect.EventTriggers.Count == 0 &&
            !aspect.Conditions.Any(ContainsMotionExecution);

        private static bool HasMotionBehavior(AspectResource aspect) =>
            aspect.EventTriggers.Count > 0 ||
            aspect.Presence is not null ||
            aspect.Layout is not null ||
            aspect.Scrolls.Count > 0 ||
            aspect.Drag is not null ||
            aspect.GesturePress is not null ||
            aspect.Conditions.Any(ContainsMotionExecution);

        private bool PrepareAspectBehavior(string targetType, AspectResource aspect, bool includeMotion)
        {
            if (aspect.BehaviorLines is not null)
            {
                return true;
            }

            List<string> behaviorLines = [];
            List<string> behaviorPostLines = [];
            List<string> lifetimeVariables = [];
            bool resolved = true;
            MarkupElement targetElement = new(ResolveAspectTargetTypeSymbol(aspect.TargetName, aspect.Source)!.Name);
            WithEmissionBuffers(behaviorLines, behaviorPostLines, () =>
            {
                if (includeMotion && !ResolveMotionAspect(targetElement, "target", aspect))
                {
                    resolved = false;
                    return;
                }

                if (includeMotion)
                {
                    EmitMotionPresence(targetElement, "target", aspect);
                    EmitMotionLayout(targetElement, "target", aspect);
                    EmitMotionActivations(targetElement, "target", aspect, bindToElementAspect: false);
                }
                ReactivePlan plan = BuildAspectReactivePlan(aspect, "target", targetElement.Name.LocalName);
                if (!includeMotion)
                {
                    foreach (ReactiveRule rule in plan.Rules)
                    {
                        rule.Activations = [];
                    }
                }

                aspect.ReactivePlan = plan;
                while (aspect.ConditionKeyVariables.Count < plan.Rules.Count)
                {
                    string keyVariable = "aspectConditionKey" + nextResourceId.ToString(CultureInfo.InvariantCulture);
                    nextResourceId++;
                    aspect.ConditionKeyVariables.Add(keyVariable);
                }

                if (plan.Rules.Count > 0)
                {
                    string conditionBehavior = "aspectConditionBehavior" + nextResourceId.ToString(CultureInfo.InvariantCulture);
                    nextResourceId++;
                    currentLines.Add("global::System.IDisposable? " + conditionBehavior + " = null;");
                    lifetimeVariables.Add(conditionBehavior);
                    EmitReactivePlan(
                        plan,
                        controlsContent: plan.HasConditionalContent,
                        aspect.ConditionKeyVariables,
                        assignmentTarget: conditionBehavior);
                }
            });

            if (!resolved)
            {
                return false;
            }

            foreach (string line in behaviorLines.Concat(behaviorPostLines))
            {
                const string prefix = "global::System.IDisposable ";
                if (!line.StartsWith(prefix, StringComparison.Ordinal))
                {
                    continue;
                }

                int end = line.IndexOf(' ', prefix.Length);
                if (end > prefix.Length)
                {
                    lifetimeVariables.Add(line.Substring(prefix.Length, end - prefix.Length));
                }
            }

            aspect.BehaviorLines = behaviorLines;
            aspect.BehaviorPostLines = behaviorPostLines;
            aspect.BehaviorLifetimeVariables = lifetimeVariables.Distinct(StringComparer.Ordinal).ToArray();
            return true;
        }

        private void EmitAspectConditionKeys(AspectResource aspect, string diagnosticPrefix)
        {
            if (aspect.ConditionKeysEmitted)
            {
                return;
            }

            for (int index = 0; index < aspect.ConditionKeyVariables.Count; index++)
            {
                currentLines.Add(
                    "global::Cerneala.UI.Aspect.AspectConditionKey " + aspect.ConditionKeyVariables[index] +
                    " = new(" + Literal(diagnosticPrefix + ".condition." + index.ToString(CultureInfo.InvariantCulture)) + ");");
            }

            aspect.ConditionKeysEmitted = true;
        }

        private static bool ContainsMotionExecution(DirectiveWhenNode when) =>
            (when.BooleanBody is not null && ContainsMotionExecution(when.BooleanBody)) ||
            when.Branches.Any(branch => ContainsMotionExecution(branch.Body));

        private static bool ContainsMotionExecution(IReadOnlyList<DirectiveNode> nodes) =>
            nodes.Any(node =>
                node is MotionExecutionNode ||
                (node is DirectiveWhenNode when && ContainsMotionExecution(when)) ||
                (node is DirectiveIfNode branch && ContainsMotionExecution(branch.Body)));

        private void EmitAspectPackageResource(
            string ownerVariable,
            string key,
            string targetType,
            AspectResource aspect)
        {
            INamedTypeSymbol targetSymbol = ResolveAspectTargetTypeSymbol(aspect.TargetName, aspect.Source)!;
            bool packageOwnsBehavior = HasRuntimeBehavior(aspect) && SupportsPackageBehavior(aspect);
            if (HasRuntimeBehavior(aspect) && !PrepareAspectBehavior(
                targetType,
                aspect,
                includeMotion: packageOwnsBehavior))
            {
                return;
            }

            string resourceVariable = "aspectPackage" + nextResourceId.ToString(CultureInfo.InvariantCulture);
            nextResourceId++;
            string packageName = "Markup." + Path.GetFileNameWithoutExtension(file.Path) + "." +
                targetSymbol.Name + "." + resourceVariable;
            EmitAspectConditionKeys(aspect, packageName);
            List<string>? declarations = BuildAspectDeclarations(targetSymbol.Name, aspect);
            if (declarations is null)
            {
                return;
            }

            string declarationsCode = declarations.Count == 0
                ? "global::System.Array.Empty<global::Cerneala.UI.Aspect.AspectDeclaration>()"
                : "new global::Cerneala.UI.Aspect.AspectDeclaration[] { " + string.Join(", ", declarations) + " }";
            aspect.RuntimeResourceVariable = resourceVariable;
            currentLines.Add("global::Cerneala.UI.Aspect.AspectPackage " + resourceVariable + " =");
            currentLines.Add("    global::Cerneala.UI.Aspect.AspectPackage.Create(" + Literal(packageName) + ")");
            currentLines.Add(
                "    .Origin(new global::Cerneala.UI.Aspect.AspectOrigin(" +
                "global::Cerneala.UI.Aspect.AspectAuthoringKind.MarkupDefault, " +
                Literal(Path.GetFileName(file.Path)) + ", " + Literal(targetSymbol.Name) + "))");
            currentLines.Add("    .Components(components =>");
            currentLines.Add("    {");
            if (declarations.Count > 0)
            {
                currentLines.Add(
                    "        components.AddRule(new global::Cerneala.UI.Aspect.AspectRuleSet(" +
                    Literal(packageName + ".default") + ", global::Cerneala.UI.Aspect.AspectLayer.App, " +
                    "new global::Cerneala.UI.Aspect.AspectTarget(typeof(" + targetType + ")), " +
                    declarationsCode + ", 0));");
            }

            if (aspect.ReactivePlan is not null)
            {
                ReactiveRule[] rules = aspect.ReactivePlan.Rules.OrderBy(rule => rule.Order).ToArray();
                for (int index = 0; index < rules.Length; index++)
                {
                    List<string>? conditionalDeclarations = BuildConditionalAspectDeclarations(
                        targetSymbol.Name,
                        rules[index].Assignments);
                    if (conditionalDeclarations is null)
                    {
                        return;
                    }

                    string conditionalCode = conditionalDeclarations.Count == 0
                        ? "global::System.Array.Empty<global::Cerneala.UI.Aspect.AspectDeclaration>()"
                        : "new global::Cerneala.UI.Aspect.AspectDeclaration[] { " +
                            string.Join(", ", conditionalDeclarations) + " }";
                    currentLines.Add(
                        "        components.AddRule(new global::Cerneala.UI.Aspect.AspectRuleSet(" +
                        Literal(packageName + ".condition." + rules[index].Order.ToString(CultureInfo.InvariantCulture)) +
                        ", global::Cerneala.UI.Aspect.AspectLayer.App, new global::Cerneala.UI.Aspect.AspectTarget(" +
                        "typeof(" + targetType + "), conditions: new global::Cerneala.UI.Aspect.AspectCondition[] { " +
                        "global::Cerneala.UI.Aspect.AspectCondition.Signal(" + aspect.ConditionKeyVariables[index] + ") }), " +
                        conditionalCode + ", " + (rules[index].Order + 1).ToString(CultureInfo.InvariantCulture) + "));");
                }
            }

            if (packageOwnsBehavior && aspect.BehaviorLines is not null)
            {
                currentLines.Add("        components.AddBehavior(new global::Cerneala.UI.Aspect.AspectBehavior(");
                currentLines.Add("            typeof(" + targetType + "), element =>");
                currentLines.Add("            {");
                currentLines.Add("                if (element is not " + targetType + " target)");
                currentLines.Add("                {");
                currentLines.Add("                    return null;");
                currentLines.Add("                }");
                foreach (string line in aspect.BehaviorLines.Concat(aspect.BehaviorPostLines!))
                {
                    currentLines.Add("                " + line);
                }

                string lifetimes = aspect.BehaviorLifetimeVariables.Count == 0
                    ? "global::System.Array.Empty<global::System.IDisposable?>()"
                    : "new global::System.IDisposable?[] { " + string.Join(", ", aspect.BehaviorLifetimeVariables) + " }";
                currentLines.Add("                return global::Cerneala.UI.Markup.GeneratedMarkup.CombineLifetimes(" + lifetimes + ");");
                currentLines.Add("            }));");
                aspect.BehaviorOwnedByPackage = true;
            }

            currentLines.Add("    })");
            currentLines.Add("    .Build();");
            currentLines.Add(ownerVariable + ".Resources[" + key + "] = " + resourceVariable + ";");
        }

        private void EmitNamedElementAspectResource(
            string ownerVariable,
            string key,
            string targetType,
            AspectResource aspect)
        {
            INamedTypeSymbol targetSymbol = ResolveAspectTargetTypeSymbol(aspect.TargetName, aspect.Source)!;
            if (HasRuntimeBehavior(aspect) && !PrepareAspectBehavior(targetType, aspect, includeMotion: false))
            {
                return;
            }

            string diagnosticName = aspect.Name!;
            EmitAspectConditionKeys(aspect, diagnosticName);
            string? valuesCode = BuildElementAspectValues(targetSymbol.Name, aspect);
            if (valuesCode is null)
            {
                return;
            }

            string resourceVariable = "elementAspectResource" + nextResourceId.ToString(CultureInfo.InvariantCulture);
            nextResourceId++;
            aspect.RuntimeResourceVariable = resourceVariable;
            aspect.RuntimeVariable = resourceVariable;
            EmitElementAspectConstruction(resourceVariable, aspect.Name, targetType, targetSymbol.Name, valuesCode, aspect);
            currentLines.Add(ownerVariable + ".Resources[" + key + "] = " + resourceVariable + ";");
        }

        private void EmitElementAspectConstruction(
            string variable,
            string? name,
            string targetType,
            string elementName,
            string valuesCode,
            AspectResource aspect)
        {
            string? conditionsCode = BuildElementAspectConditionsCode(elementName, aspect);
            if (conditionsCode is null)
            {
                return;
            }

            currentLines.Add("global::Cerneala.UI.Aspect.ElementAspect " + variable + " = new(");
            currentLines.Add("    " + (name is null ? "null" : Literal(name)) + ", typeof(" + targetType + "), " + valuesCode + ",");
            currentLines.Add("    " + conditionsCode + ",");
            if (aspect.BehaviorLines is null)
            {
                currentLines.Add("    behaviorFactory: null,");
            }
            else
            {
                currentLines.Add("    element =>");
                currentLines.Add("    {");
                currentLines.Add("        if (element is not " + targetType + " target)");
                currentLines.Add("        {");
                currentLines.Add("            return null;");
                currentLines.Add("        }");
                foreach (string line in aspect.BehaviorLines.Concat(aspect.BehaviorPostLines!))
                {
                    currentLines.Add("        " + line);
                }

                string lifetimes = aspect.BehaviorLifetimeVariables.Count == 0
                    ? "global::System.Array.Empty<global::System.IDisposable?>()"
                    : "new global::System.IDisposable?[] { " + string.Join(", ", aspect.BehaviorLifetimeVariables) + " }";
                currentLines.Add("        return global::Cerneala.UI.Markup.GeneratedMarkup.CombineLifetimes(" + lifetimes + ");");
                currentLines.Add("    },");
            }

            currentLines.Add("    isConditional: " + (aspect.Conditions.Count > 0 ? "true" : "false") + ",");
            string authoringKind = aspect.Name is null ? "MarkupInline" : "MarkupNamed";
            currentLines.Add(
                "    origin: new global::Cerneala.UI.Aspect.AspectOrigin(" +
                "global::Cerneala.UI.Aspect.AspectAuthoringKind." + authoringKind + ", " +
                Literal(Path.GetFileName(file.Path)) + ", " +
                (aspect.Name is null ? "null" : Literal(aspect.Name)) + "));");
        }

        private string? BuildElementAspectConditionsCode(string elementName, AspectResource aspect)
        {
            if (aspect.ReactivePlan is null || aspect.ReactivePlan.Rules.Count == 0)
            {
                return "global::System.Array.Empty<global::Cerneala.UI.Aspect.ElementAspectCondition>()";
            }

            List<string> conditions = [];
            ReactiveRule[] rules = aspect.ReactivePlan.Rules.OrderBy(rule => rule.Order).ToArray();
            for (int index = 0; index < rules.Length; index++)
            {
                List<string>? values = BuildConditionalElementAspectValues(elementName, rules[index].Assignments);
                if (values is null)
                {
                    return null;
                }

                string valuesCode = values.Count == 0
                    ? "global::System.Array.Empty<global::Cerneala.UI.Aspect.ElementAspectValue>()"
                    : "new global::Cerneala.UI.Aspect.ElementAspectValue[] { " + string.Join(", ", values) + " }";
                conditions.Add(
                    "new global::Cerneala.UI.Aspect.ElementAspectCondition(" +
                    aspect.ConditionKeyVariables[index] + ", " + valuesCode + ", " +
                    rules[index].Order.ToString(CultureInfo.InvariantCulture) + ")");
            }

            return "new global::Cerneala.UI.Aspect.ElementAspectCondition[] { " + string.Join(", ", conditions) + " }";
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

        private IReadOnlyList<AspectResource> ResolveAspects(MarkupElement element)
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

            MarkupAttribute? aspectAttribute = element.Attribute("Aspect");
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

            INamedTypeSymbol? namedTargetType = ResolveAspectTargetTypeSymbol(namedAspect.TargetName, namedAspect.Source);
            INamedTypeSymbol? appliedElementType = ResolvePropertyOwnerType(elementName, ReferenceEquals(element, document.Root));
            if (namedTargetType is null || appliedElementType is null || !IsOrDerivesFrom(appliedElementType, namedTargetType))
            {
                Report(InvalidPropertyValue, aspectAttribute, elementName, "Aspect", aspectAttribute.Value);
                return resolved;
            }

            resolved.Add(namedAspect.RuntimeResourceVariable is null
                ? CloneAspectForApplication(namedAspect)
                : namedAspect);
            return resolved;
        }

        private static AspectResource CloneAspectForApplication(AspectResource aspect)
        {
            return new AspectResource(
                aspect.Name,
                aspect.TargetName,
                aspect.Assignments,
                aspect.Conditions,
                aspect.EventTriggers,
                aspect.Presence,
                aspect.Layout,
                aspect.Scrolls,
                aspect.Drag,
                aspect.GesturePress,
                aspect.Template,
                aspect.Source,
                isInline: true)
            {
                TemplateVariable = aspect.TemplateVariable
            };
        }

        private void ApplyAspects(MarkupElement element, string variable, IReadOnlyList<AspectResource> aspects)
        {
            foreach (AspectResource aspect in aspects)
            {
                if (aspect.Name is null && !aspect.IsInline)
                {
                    if (!aspect.BehaviorOwnedByPackage && HasRuntimeBehavior(aspect))
                    {
                        if (!ResolveMotionAspect(element, variable, aspect))
                        {
                            continue;
                        }

                        EmitMotionPresence(element, variable, aspect);
                        EmitMotionLayout(element, variable, aspect);
                        EmitMotionActivations(element, variable, aspect, bindToElementAspect: false);
                        if (aspect.Conditions.Count > 0)
                        {
                            if (aspect.ConditionKeyVariables.Count > 0)
                            {
                                ReactivePlan signalPlan = BuildAspectReactivePlan(aspect, variable, element.Name.LocalName);
                                foreach (ReactiveRule rule in signalPlan.Rules)
                                {
                                    rule.Activations = [];
                                }

                                signalPlan.Rules.RemoveAll(rule => rule.Assignments.Count == 0 && rule.Elements.Count == 0);
                                EmitReactivePlan(
                                    signalPlan,
                                    controlsContent: signalPlan.HasConditionalContent,
                                    aspectConditionKeys: aspect.ConditionKeyVariables,
                                    suppressValues: true);
                            }

                            ReactivePlan motionPlan = BuildAspectReactivePlan(aspect, variable, element.Name.LocalName);
                            motionPlan.Rules.RemoveAll(rule => rule.Activations.Count == 0 && rule.Elements.Count == 0);
                            EmitReactivePlan(
                                motionPlan,
                                controlsContent: motionPlan.HasConditionalContent,
                                suppressValues: true);
                        }
                    }

                    continue;
                }

                bool hasMotionBehavior = HasMotionBehavior(aspect);
                if (hasMotionBehavior)
                {
                    if (!ResolveMotionAspect(element, variable, aspect))
                    {
                        continue;
                    }

                    EmitMotionPresence(element, variable, aspect);
                    EmitMotionLayout(element, variable, aspect);
                    EmitMotionActivations(element, variable, aspect, bindToElementAspect: true);
                }

                if (aspect.RuntimeResourceVariable is not null)
                {
                    if (IsApplicationNamedAspect(aspect))
                    {
                        currentLines.Add(
                            "global::Cerneala.UI.Markup.GeneratedMarkup.AttachResource(" +
                            variable + ", " + variable + ", global::Cerneala.UI.Elements.UIElement.AspectProperty, " +
                            Literal(aspect.Name!) + ", global::Cerneala.UI.Core.UiPropertyValueSource.Local);");
                    }
                    else
                    {
                        aspect.RuntimeVariable = aspect.RuntimeResourceVariable;
                        currentLines.Add(variable + ".Aspect = " + aspect.RuntimeResourceVariable + ";");
                    }
                }
                else if (IsLocalAspect(aspect))
                {
                    EmitLocalAspect(element, variable, aspect);
                }
                else
                {
                    EmitAspectAssignments(element, variable, aspect);
                }

                if (hasMotionBehavior && aspect.Conditions.Count > 0)
                {
                    ReactivePlan motionPlan = BuildAspectReactivePlan(aspect, variable, element.Name.LocalName);
                    motionPlan.Rules.RemoveAll(rule => rule.Activations.Count == 0 && rule.Elements.Count == 0);
                    EmitReactivePlan(
                        motionPlan,
                        controlsContent: motionPlan.HasConditionalContent,
                        suppressValues: true);
                }

            }
        }

        public ApplicationResourceCatalog CreateApplicationResourceCatalog()
        {
            if (!resourceScopes.TryGetValue(document.Root, out ResourceScope? scope))
            {
                return new ApplicationResourceCatalog(
                    new Dictionary<string, object>(StringComparer.Ordinal),
                    new Dictionary<string, object>(StringComparer.Ordinal),
                    new Dictionary<string, object>(StringComparer.Ordinal));
            }

            IReadOnlyDictionary<string, object> prismCompositions =
                boundPrismResources.TryGetValue(
                    scope,
                    out Dictionary<string, BoundPrismComposition>? bound)
                    ? bound.ToDictionary(
                        pair => pair.Key,
                        pair => (object)pair.Value,
                        StringComparer.Ordinal)
                    : new Dictionary<string, object>(StringComparer.Ordinal);
            return new ApplicationResourceCatalog(
                scope.NamedResources.ToDictionary(pair => pair.Key, pair => (object)pair.Value, StringComparer.Ordinal),
                scope.DefaultAspectsByTarget.ToDictionary(pair => pair.Key, pair => (object)pair.Value, StringComparer.Ordinal),
                prismCompositions);
        }

        public void EmitApplicationResources()
        {
            EmitRuntimeResources(document.Root, "this");
        }

        private static bool IsLocalAspect(AspectResource aspect) => aspect.IsInline || aspect.Name is not null;

        private bool IsApplicationNamedAspect(AspectResource aspect) =>
            applicationResources is not null &&
            applicationResources.NamedResources.Values
                .OfType<NamedSymbol>()
                .Any(symbol => ReferenceEquals(symbol.Source, aspect));

        private void EmitLocalAspect(MarkupElement element, string variable, AspectResource aspect)
        {
            string elementName = element.Name.LocalName;
            string targetType = ResolveAspectTargetType(aspect.TargetName, aspect.Source)!;
            if (HasRuntimeBehavior(aspect) && !PrepareAspectBehavior(targetType, aspect, includeMotion: false))
            {
                return;
            }

            EmitAspectConditionKeys(aspect, (aspect.Name ?? "Inline") + "." + elementName);
            string? valuesCode = BuildElementAspectValues(elementName, aspect);
            if (valuesCode is null)
            {
                return;
            }

            string aspectVariable = "localAspect" + nextResourceId.ToString(CultureInfo.InvariantCulture);
            nextResourceId++;
            aspect.RuntimeVariable = aspectVariable;
            EmitElementAspectConstruction(aspectVariable, aspect.Name, targetType, elementName, valuesCode, aspect);
            currentLines.Add(variable + ".Aspect = " + aspectVariable + ";");
        }

        private string? BuildElementAspectValues(string elementName, AspectResource aspect)
        {
            List<string> values = [];
            foreach (AspectPropertyAssignment assignment in aspect.Assignments)
            {
                PropertySpec? spec = FindPropertySpec(elementName, assignment.PropertyName, isRoot: false);
                if (spec is null || !spec.Assignable)
                {
                    Report(UnsupportedProperty, assignment.Source, elementName, assignment.PropertyName);
                    return null;
                }

                GeneratedExpression? expression = assignment.IsReference
                    ? ResolveReferenceValue(elementName, assignment.PropertyName, assignment.RawValue, spec.ValueKind, assignment.Source)
                    : ParseAspectLiteralValue(elementName, assignment.PropertyName, assignment.RawValue, spec, assignment.Source);
                if (expression is null)
                {
                    return null;
                }

                values.Add(BuildElementAspectValueCode(
                    spec,
                    expression,
                    resourceName: null));
            }

            if (aspect.TemplateVariable is not null)
            {
                values.Add(
                    "new global::Cerneala.UI.Aspect.ElementAspectValue(" +
                    "global::Cerneala.UI.Controls.Control.ComponentTemplateProperty, " + aspect.TemplateVariable + ")");
            }

            return values.Count == 0
                ? "global::System.Array.Empty<global::Cerneala.UI.Aspect.ElementAspectValue>()"
                : "new global::Cerneala.UI.Aspect.ElementAspectValue[] { " + string.Join(", ", values) + " }";
        }

        private List<string>? BuildAspectDeclarations(string elementName, AspectResource aspect)
        {
            List<string> declarations = [];
            foreach (AspectPropertyAssignment assignment in aspect.Assignments)
            {
                PropertySpec? spec = FindPropertySpec(elementName, assignment.PropertyName, isRoot: false);
                if (spec is null || !spec.Assignable || !spec.IsUiProperty)
                {
                    Report(UnsupportedProperty, assignment.Source, elementName, assignment.PropertyName);
                    return null;
                }

                GeneratedExpression? expression = assignment.IsReference
                    ? ResolveReferenceValue(elementName, assignment.PropertyName, assignment.RawValue, spec.ValueKind, assignment.Source)
                    : ParseAspectLiteralValue(elementName, assignment.PropertyName, assignment.RawValue, spec, assignment.Source);
                if (expression is null)
                {
                    return null;
                }

                string valueCode = BuildAspectValueCode(
                    spec,
                    expression,
                    assignment.IsReference && spec.ValueKind == MarkupValueKind.Brush
                        ? assignment.RawValue
                        : expression.ApplicationResourceName);
                declarations.Add(
                    "new global::Cerneala.UI.Aspect.AspectDeclaration(" + spec.PropertyCode + ", " + valueCode +
                    ", diagnosticName: " + Literal(assignment.PropertyName) + ")");
            }

            if (aspect.TemplateVariable is not null)
            {
                declarations.Add(
                    "new global::Cerneala.UI.Aspect.AspectDeclaration(" +
                    "global::Cerneala.UI.Controls.Control.ComponentTemplateProperty, " +
                    "global::Cerneala.UI.Aspect.AspectValue<global::Cerneala.UI.Controls.Templates.ComponentTemplate?>.Literal(" +
                    aspect.TemplateVariable + "), diagnosticName: \"ComponentTemplate\")");
            }

            return declarations;
        }

        private List<string>? BuildConditionalAspectDeclarations(
            string elementName,
            IReadOnlyList<DirectiveAssignmentNode> assignments)
        {
            List<string> declarations = [];
            foreach (DirectiveAssignmentNode assignment in assignments)
            {
                PropertySpec? spec = FindPropertySpec(elementName, assignment.PropertyName, isRoot: false);
                if (spec is null || !spec.Assignable || !spec.IsUiProperty)
                {
                    Report(UnsupportedProperty, assignment.Source, elementName, assignment.PropertyName);
                    return null;
                }

                ParsedMarkupValue? binding = ParseMarkupBindingValue(
                    assignment.Value,
                    assignment: true,
                    stringTarget: spec.ValueType.SpecialType == SpecialType.System_String,
                    assignment.ValueLocation,
                    requireExplicitMode: true);
                if (binding is not null)
                {
                    Report(
                        InvalidBindingSource,
                        assignment.ValueLocation,
                        assignment.Value,
                        "Conditional Aspect assignment bindings must be lowered as computed Aspect values.");
                    return null;
                }

                GeneratedExpression? expression = ParseDirectiveValue(
                    elementName,
                    assignment.PropertyName,
                    assignment.Value,
                    spec,
                    assignment.Source,
                    "target");
                if (expression is null)
                {
                    return null;
                }

                declarations.Add(
                    "new global::Cerneala.UI.Aspect.AspectDeclaration(" + spec.PropertyCode + ", " +
                    BuildAspectValueCode(spec, expression, ConditionalResourceName(assignment, spec)) + ", diagnosticName: " +
                    Literal(assignment.PropertyName) + ")");
            }

            return declarations;
        }

        private List<string>? BuildConditionalElementAspectValues(
            string elementName,
            IReadOnlyList<DirectiveAssignmentNode> assignments)
        {
            List<string> values = [];
            foreach (DirectiveAssignmentNode assignment in assignments)
            {
                PropertySpec? spec = FindPropertySpec(elementName, assignment.PropertyName, isRoot: false);
                if (spec is null || !spec.Assignable || !spec.IsUiProperty)
                {
                    Report(UnsupportedProperty, assignment.Source, elementName, assignment.PropertyName);
                    return null;
                }

                ParsedMarkupValue? binding = ParseMarkupBindingValue(
                    assignment.Value,
                    assignment: true,
                    stringTarget: spec.ValueType.SpecialType == SpecialType.System_String,
                    assignment.ValueLocation,
                    requireExplicitMode: true);
                if (binding is not null)
                {
                    Report(
                        InvalidBindingSource,
                        assignment.ValueLocation,
                        assignment.Value,
                        "Conditional ElementAspect assignment bindings must be lowered as computed Aspect values.");
                    return null;
                }

                GeneratedExpression? expression = ParseDirectiveValue(
                    elementName,
                    assignment.PropertyName,
                    assignment.Value,
                    spec,
                    assignment.Source,
                    "target");
                if (expression is null)
                {
                    return null;
                }

                values.Add(BuildElementAspectValueCode(
                    spec,
                    expression,
                    resourceName: null));
            }

            return values;
        }

        private static string BuildAspectValueCode(
            PropertySpec spec,
            GeneratedExpression expression,
            string? resourceName)
        {
            string valueType = spec.ValueType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
            return resourceName is null
                ? "global::Cerneala.UI.Aspect.AspectValue<" + valueType + ">.Literal(" + expression.Code + ")"
                : "global::Cerneala.UI.Aspect.AspectValue<" + valueType + ">.Computed(" +
                    "context => context.Element.FindResource<" + valueType + ">(" +
                    Literal(resourceName) + "), " +
                    "global::System.Array.Empty<global::Cerneala.UI.Aspect.AspectToken>())";
        }

        private static string BuildElementAspectValueCode(
            PropertySpec spec,
            GeneratedExpression expression,
            string? resourceName)
        {
            string valueCode = resourceName is null
                ? expression.Code
                : BuildAspectValueCode(spec, expression, resourceName);
            return "new global::Cerneala.UI.Aspect.ElementAspectValue(" + spec.PropertyCode + ", " + valueCode + ")";
        }

        private static string? ConditionalResourceName(
            DirectiveAssignmentNode assignment,
            PropertySpec spec)
        {
            string value = assignment.Value.Trim();
            return spec.ValueKind == MarkupValueKind.Brush &&
                value.StartsWith("$", StringComparison.Ordinal) &&
                !LooksLikeBindingPath(value)
                ? value.Substring(1)
                : null;
        }

        private string ReadReferenceName(string elementName, string propertyName, MarkupAttribute attribute)
        {
            string value = attribute.Value.Trim();
            if (!value.StartsWith("$", StringComparison.Ordinal) || value.Length == 1)
            {
                Report(InvalidPropertyValue, attribute, elementName, propertyName, attribute.Value);
                return string.Empty;
            }

            return value.Substring(1);
        }

        private void EmitAspectAssignments(
            MarkupElement element,
            string variable,
            AspectResource aspect,
            string valueSource = "global::Cerneala.UI.Core.UiPropertyValueSource.AspectBase")
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

                if (expression.ApplicationResourceName is not null && spec.IsUiProperty)
                {
                    EmitApplicationResourceBinding(
                        variable,
                        spec,
                        expression.ApplicationResourceName,
                        valueSource);
                    continue;
                }

                currentLines.Add(spec.IsUiProperty
                    ? variable + ".SetValue(" + spec.PropertyCode + ", " + expression.Code +
                        ", " + valueSource + ");"
                    : variable + "." + spec.Name + " = " + expression.Code + ";");
            }

            if (aspect.TemplateVariable is not null)
            {
                currentLines.Add(
                    variable + ".SetValue(global::Cerneala.UI.Controls.Control.ComponentTemplateProperty, " +
                    aspect.TemplateVariable + ", " + valueSource + ");");
            }
        }

        private GeneratedExpression? ParseAspectLiteralValue(string elementName, string propertyName, string value, PropertySpec spec, MarkupObject source)
        {
            MarkupAttribute synthetic = new(propertyName, value);
            return ParseLiteralValue(elementName, propertyName, synthetic, value, spec);
        }

        private GeneratedExpression? ResolveReferenceValue(string elementName, string propertyName, string referenceName, MarkupValueKind targetKind, MarkupObject source)
        {
            if (!TryResolveResource(source, referenceName, out NamedSymbol symbol))
            {
                Report(InvalidPropertyValue, source, elementName, propertyName, "$" + referenceName);
                return null;
            }

            if (targetKind == MarkupValueKind.Brush && symbol.Source is BrushResource brushResource)
            {
                if (applicationResources?.Contains(symbol) == true)
                {
                    string code =
                        "((global::Cerneala.UI.Resources.IResourceProvider)global::Cerneala.UI.Application.Current!.Resources).GetResource(" +
                        "new global::Cerneala.UI.Resources.ResourceId<global::Cerneala.UI.Media.Brush>(" +
                        Literal(referenceName) + "))";
                    return new GeneratedExpression(code, MarkupValueKind.Brush, referenceName);
                }

                return new GeneratedExpression(brushResource.Variable, MarkupValueKind.Brush);
            }

            if (targetKind == MarkupValueKind.Color && symbol.Source is BrushResource brush && brush.ColorExpression is not null)
            {
                return new GeneratedExpression(brush.ColorExpression, MarkupValueKind.Color);
            }

            Report(InvalidPropertyValue, source, elementName, propertyName, "$" + referenceName);
            return null;
        }

        private bool TryResolveDefaultAspect(MarkupObject source, string targetName, out AspectResource aspect)
        {
            bool isRoot = source is MarkupElement element && ReferenceEquals(element, document.Root);
            INamedTypeSymbol? appliedElementType = ResolvePropertyOwnerType(targetName, isRoot);
            foreach (ResourceScope scope in EnumerateResourceScopes(source))
            {
                if (scope.DefaultAspectsByTarget.TryGetValue(targetName, out aspect))
                {
                    return true;
                }

                if (appliedElementType is not null && TryResolveNearestDefaultAspect(
                    scope.DefaultAspectsByTarget.Values,
                    appliedElementType,
                    out aspect))
                {
                    return true;
                }
            }

            aspect = null!;
            return false;
        }

        private bool TryResolveNearestDefaultAspect(
            IEnumerable<AspectResource> candidates,
            INamedTypeSymbol appliedElementType,
            out AspectResource aspect)
        {
            AspectResource? nearest = null;
            int nearestDistance = int.MaxValue;
            foreach (AspectResource candidate in candidates)
            {
                INamedTypeSymbol? candidateType = ResolveAspectTargetTypeSymbol(candidate.TargetName, candidate.Source);
                int distance = candidateType is null
                    ? -1
                    : GetBaseTypeDistance(appliedElementType, candidateType);
                if (distance < 0 || distance >= nearestDistance)
                {
                    continue;
                }

                nearest = candidate;
                nearestDistance = distance;
            }

            aspect = nearest!;
            return nearest is not null;
        }

        private static int GetBaseTypeDistance(INamedTypeSymbol type, INamedTypeSymbol candidateBaseType)
        {
            int distance = 0;
            for (INamedTypeSymbol? current = type; current is not null; current = current.BaseType)
            {
                if (SymbolEqualityComparer.Default.Equals(current, candidateBaseType))
                {
                    return distance;
                }

                distance++;
            }

            return -1;
        }

        private bool TryResolveResource(MarkupObject source, string name, out NamedSymbol symbol)
        {
            foreach (ResourceScope scope in EnumerateResourceScopes(source))
            {
                if (scope.NamedResources.TryGetValue(name, out symbol))
                {
                    return true;
                }
            }

            if (applicationResources is not null &&
                applicationResources.NamedResources.TryGetValue(name, out object? applicationSymbol) &&
                applicationSymbol is NamedSymbol typedSymbol)
            {
                symbol = typedSymbol;
                return true;
            }

            symbol = null!;
            return false;
        }

        private bool TryResolveObjectSymbol(MarkupObject source, string name, out NamedSymbol symbol)
        {
            if (symbols.TryGetValue(name, out symbol))
            {
                return true;
            }

            return TryResolveResource(source, name, out symbol);
        }

        private IEnumerable<ResourceScope> EnumerateResourceScopes(MarkupObject source)
        {
            MarkupElement? current = source switch
            {
                MarkupElement element => element,
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

        private void EmitProperty(
            MarkupElement element,
            string variable,
            MarkupAttribute attribute,
            PropertySpec? explicitSpec = null,
            string? explicitPropertyName = null,
            bool forceUiPropertyAssignment = false)
        {
            string elementName = element.Name.LocalName;
            string propertyName = explicitPropertyName ?? attribute.Name.LocalName;
            string value = attribute.Value;
            string trimmedValue = value.Trim();

            bool isRoot = ReferenceEquals(element, document.Root);
            PropertySpec? spec = explicitSpec ??
                FindPropertySpec(elementName, propertyName, isRoot) ??
                FindClrPropertySpec(elementName, propertyName, isRoot);
            if (spec is null)
            {
                if (!HasErrors)
                {
                    Report(UnsupportedProperty, attribute, elementName, propertyName);
                }

                return;
            }

            ParsedMarkupValue? parsedMarkup = ParseMarkupBindingValue(
                value,
                assignment: false,
                stringTarget: spec.ValueType.SpecialType == SpecialType.System_String,
                attribute);
            if (parsedMarkup?.Kind == ParsedMarkupValueKind.Invalid)
            {
                return;
            }

            if (!spec.Assignable)
            {
                if (templateEmissionContexts.Count > 0 && trimmedValue.StartsWith("$owner.", StringComparison.Ordinal))
                {
                    Report(
                        InvalidComponentTemplate,
                        attribute,
                        Path.GetFileName(file.Path),
                        "Template binding target '" + elementName + "." + propertyName + "' is read-only.");
                }
                else if (parsedMarkup is not null)
                {
                    Report(
                        InvalidBindingSource,
                        attribute,
                        parsedMarkup.Binding?.Path ?? trimmedValue,
                        "The target UI property is read-only.");
                }
                else if (!HasErrors)
                {
                    Report(UnsupportedProperty, attribute, elementName, propertyName);
                }

                return;
            }

            MarkupBindingToken? directReference = parsedMarkup?.Binding;
            if (directReference is not null &&
                directReference.ModeOffset < 0 &&
                LooksLikeBindingPath(trimmedValue))
            {
                GeneratedExpression? referenceExpression = ResolveDirectiveReferenceValue(
                    elementName,
                    propertyName,
                    directReference,
                    spec,
                    attribute,
                    variable);
                if (referenceExpression is null)
                {
                    return;
                }

                currentPostLines.Add((reactiveDocument || forceUiPropertyAssignment) && spec.IsUiProperty
                    ? variable + ".SetValue(" + spec.PropertyCode + ", " + referenceExpression.Code +
                        ", global::Cerneala.UI.Core.UiPropertyValueSource.MarkupBase);"
                    : variable + "." + spec.Name + " = " + referenceExpression.Code + ";");
                return;
            }

            if (parsedMarkup is not null)
            {
                if (!spec.IsUiProperty)
                {
                    Report(
                        InvalidBindingSource,
                        attribute,
                        trimmedValue,
                        "Bindings require a UiProperty-backed target; ordinary CLR properties support literal and resource values only.");
                    return;
                }

                BindingResolutionContext bindingContext = CreateBindingResolutionContext(
                    variable,
                    elementName,
                    ReferenceEquals(element, document.Root));

                MarkupBindingToken? direct = parsedMarkup.Binding;
                if (direct is not null && direct.Path.StartsWith("$owner.", StringComparison.Ordinal))
                {
                    BindingSourceDescriptor? sourceDescriptor = ResolveBindingSource(
                        bindingContext,
                        direct.Path,
                        attribute,
                        attribute);
                    if (sourceDescriptor is null)
                    {
                        return;
                    }

                    if (direct.Mode == MarkupBindingMode.TwoWay)
                    {
                        Report(InvalidBindingSource, attribute, trimmedValue, "$owner template bindings support OneWay only.");
                        return;
                    }

                    if (!SymbolEqualityComparer.Default.Equals(sourceDescriptor.ValueType, spec.ValueType))
                    {
                        Report(
                            InvalidComponentTemplate,
                            attribute,
                            Path.GetFileName(file.Path),
                            "Template binding '" + trimmedValue + "' has type '" + sourceDescriptor.ValueType.ToDisplayString() +
                            "', but '" + elementName + "." + propertyName + "' expects '" + spec.ValueType.ToDisplayString() + "'.");
                        return;
                    }

                    TemplateEmissionContext templateContext = templateEmissionContexts.Peek();
                    currentLines.Add(
                        templateContext.ContextVariable + ".Bind(" + sourceDescriptor.Property!.PropertyCode + ", " +
                        variable + ", " + spec.PropertyCode + ");");
                    return;
                }

                ResolvedMarkupValue? resolvedMarkup = ResolveMarkupValue(
                    bindingContext,
                    spec,
                    parsedMarkup,
                    attribute,
                    attribute);
                if (resolvedMarkup is null)
                {
                    return;
                }

                EmitMarkupBinding(
                    bindingContext,
                    spec,
                    resolvedMarkup,
                    elementName + "." + propertyName + " <- " + trimmedValue);
                return;
            }

            if (trimmedValue.StartsWith("$", StringComparison.Ordinal))
            {
                string resourceName = trimmedValue.EndsWith(":OneWay", StringComparison.Ordinal)
                    ? trimmedValue.Substring(1, trimmedValue.Length - ":OneWay".Length - 1)
                    : trimmedValue.Substring(1);
                GeneratedExpression? resourceExpression = ResolveReferenceValue(
                    elementName,
                    propertyName,
                    resourceName,
                    spec.ValueKind,
                    attribute);
                if (resourceExpression is not null)
                {
                    if (resourceExpression.ApplicationResourceName is not null && spec.IsUiProperty)
                    {
                        EmitApplicationResourceBinding(
                            variable,
                            spec,
                            resourceExpression.ApplicationResourceName,
                            "global::Cerneala.UI.Core.UiPropertyValueSource.MarkupBase");
                        return;
                    }

                    currentLines.Add((reactiveDocument || forceUiPropertyAssignment) && spec.IsUiProperty
                        ? variable + ".SetValue(" + spec.PropertyCode + ", " + resourceExpression.Code +
                            ", global::Cerneala.UI.Core.UiPropertyValueSource.MarkupBase);"
                        : variable + "." + spec.Name + " = " + resourceExpression.Code + ";");
                }

                return;
            }

            string literalValue = spec.ValueType.SpecialType == SpecialType.System_String
                ? UnescapeMarkupDollar(value)
                : value;
            GeneratedExpression? expression = ParseLiteralValue(elementName, propertyName, attribute, literalValue, spec);
            if (expression is null)
            {
                return;
            }

            currentLines.Add((reactiveDocument || forceUiPropertyAssignment) && spec.IsUiProperty
                ? variable + ".SetValue(" + spec.PropertyCode + ", " + expression.Code +
                    ", global::Cerneala.UI.Core.UiPropertyValueSource.MarkupBase);"
                : variable + "." + spec.Name + " = " + expression.Code + ";");
        }

        private void EmitApplicationResourceBinding(
            string variable,
            PropertySpec spec,
            string resourceName,
            string valueSource)
        {
            currentLines.Add(
                "global::Cerneala.UI.Markup.GeneratedMarkup.AttachResource(" +
                variable + ", " + variable + ", " + spec.PropertyCode + ", " +
                Literal(resourceName) + ", " + valueSource + ");");
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

        private GeneratedExpression? ParseLiteralValue(string elementName, string propertyName, MarkupAttribute attribute, string value, PropertySpec spec)
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
                MarkupValueKind.LayoutPoint => LayoutPoint(elementName, propertyName, attribute),
                MarkupValueKind.Color => Color(elementName, propertyName, attribute),
                MarkupValueKind.Brush => Brush(elementName, propertyName, attribute),
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

        private void EmitTextContent(MarkupElement element, string variable, string text)
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

        private void EmitChild(MarkupElement parent, string parentVariable, string childVariable)
        {
            INamedTypeSymbol? parentType = ResolveElementTypeSymbol(parent.Name.LocalName);
            INamedTypeSymbol? panelType = compilation.GetTypeByMetadataName("Cerneala.UI.Layout.Panels.Panel");
            INamedTypeSymbol? itemsControlType = compilation.GetTypeByMetadataName("Cerneala.UI.Controls.ItemsControl");
            INamedTypeSymbol? decoratorType = compilation.GetTypeByMetadataName("Cerneala.UI.Controls.Decorator");
            INamedTypeSymbol? contentControlType = compilation.GetTypeByMetadataName("Cerneala.UI.Controls.ContentControl");
            INamedTypeSymbol? scrollViewerType = compilation.GetTypeByMetadataName("Cerneala.UI.Controls.ScrollViewer");
            INamedTypeSymbol? sceneType = compilation.GetTypeByMetadataName("Cerneala.UI.Controls.Scene2D");

            if (parentType is not null && sceneType is not null && IsOrDerivesFrom(parentType, sceneType))
            {
                currentLines.Add(parentVariable + ".Children.Add(" + childVariable + ");");
                return;
            }

            if (parentType is not null && panelType is not null && IsOrDerivesFrom(parentType, panelType))
            {
                currentLines.Add(parentVariable + ".LogicalChildren.Add(" + childVariable + ");");
                currentLines.Add(parentVariable + ".VisualChildren.Add(" + childVariable + ");");
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

        private static string? ReadDirectText(MarkupElement element)
        {
            string text = string.Concat(element.Nodes().OfType<MarkupText>().Select(node => node.Value));
            return string.IsNullOrWhiteSpace(text) ? null : text.Trim();
        }

        private string? NonNegativeFloat(string elementName, string propertyName, MarkupAttribute attribute)
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
            MarkupAttribute attribute,
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

        private string? Bool(string elementName, string propertyName, MarkupAttribute attribute)
        {
            return bool.TryParse(attribute.Value, out bool parsed) ? (parsed ? "true" : "false") : Invalid(attribute, elementName, propertyName, attribute.Value);
        }

        private string? Float(string elementName, string propertyName, MarkupAttribute attribute)
        {
            string value = attribute.Value;
            return float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out float parsed) &&
                !float.IsNaN(parsed) &&
                !float.IsInfinity(parsed)
                ? parsed.ToString("R", CultureInfo.InvariantCulture) + "f"
                : Invalid(attribute, elementName, propertyName, value);
        }

        private string? Integer(string elementName, string propertyName, MarkupAttribute attribute, SpecialType type)
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

        private string? Double(string elementName, string propertyName, MarkupAttribute attribute)
        {
            string value = attribute.Value.Trim();
            return double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out double parsed) &&
                !double.IsNaN(parsed) && !double.IsInfinity(parsed)
                ? parsed.ToString("R", CultureInfo.InvariantCulture) + "d"
                : Invalid(attribute, elementName, propertyName, attribute.Value);
        }

        private string? Decimal(string elementName, string propertyName, MarkupAttribute attribute)
        {
            string value = attribute.Value.Trim();
            return decimal.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out decimal parsed)
                ? parsed.ToString(CultureInfo.InvariantCulture) + "m"
                : Invalid(attribute, elementName, propertyName, attribute.Value);
        }

        private string? PositiveFloat(string elementName, string propertyName, MarkupAttribute attribute)
        {
            string value = attribute.Value;
            return float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out float parsed) &&
                parsed > 0 &&
                !float.IsNaN(parsed) &&
                !float.IsInfinity(parsed)
                ? parsed.ToString("R", CultureInfo.InvariantCulture) + "f"
                : Invalid(attribute, elementName, propertyName, value);
        }

        private string? Thickness(string elementName, string propertyName, MarkupAttribute attribute)
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

        private string? NonNegativeThickness(string elementName, string propertyName, MarkupAttribute attribute)
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

        private string? LayoutPoint(string elementName, string propertyName, MarkupAttribute attribute)
        {
            string value = attribute.Value;
            string[] parts = value.Split(',').Select(part => part.Trim()).ToArray();
            if (parts.Length != 2)
            {
                return Invalid(attribute, elementName, propertyName, value);
            }

            string? x = FloatPart(elementName, propertyName, attribute, parts[0]);
            string? y = FloatPart(elementName, propertyName, attribute, parts[1]);
            return x is not null && y is not null
                ? "new global::Cerneala.UI.Layout.LayoutPoint(" + x + ", " + y + ")"
                : null;
        }

        private string? FloatPart(string elementName, string propertyName, MarkupAttribute attribute, string value)
        {
            return float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out float parsed) &&
                !float.IsNaN(parsed) &&
                !float.IsInfinity(parsed)
                ? parsed.ToString("R", CultureInfo.InvariantCulture) + "f"
                : Invalid(attribute, elementName, propertyName, attribute.Value);
        }

        private string? NonNegativeFloatPart(string elementName, string propertyName, MarkupAttribute attribute, string value)
        {
            return float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out float parsed) &&
                parsed >= 0 &&
                !float.IsNaN(parsed) &&
                !float.IsInfinity(parsed)
                ? parsed.ToString("R", CultureInfo.InvariantCulture) + "f"
                : Invalid(attribute, elementName, propertyName, attribute.Value);
        }

        private string? Color(string elementName, string propertyName, MarkupAttribute attribute)
        {
            string value = attribute.Value;
            if (NamedColorNames.TryGetValue(value, out string? namedColor))
            {
                return "global::Cerneala.Drawing.Color." + namedColor;
            }

            if (ParseHexColor(value) is ColorLiteral hexColor)
            {
                return hexColor.ToExpression();
            }

            string[] parts = value.Split(',').Select(part => part.Trim()).ToArray();
            if (parts.Length is 3 or 4 &&
                byte.TryParse(parts[0], NumberStyles.Integer, CultureInfo.InvariantCulture, out byte r) &&
                byte.TryParse(parts[1], NumberStyles.Integer, CultureInfo.InvariantCulture, out byte g) &&
                byte.TryParse(parts[2], NumberStyles.Integer, CultureInfo.InvariantCulture, out byte b) &&
                (parts.Length == 3 || byte.TryParse(parts[3], NumberStyles.Integer, CultureInfo.InvariantCulture, out _)))
            {
                string alpha = parts.Length == 4 ? ", " + parts[3] : string.Empty;
                return "new global::Cerneala.Drawing.Color(" + r + ", " + g + ", " + b + alpha + ")";
            }

            return Invalid(attribute, elementName, propertyName, value);
        }

        private string? Brush(string elementName, string propertyName, MarkupAttribute attribute)
        {
            string? color = Color(elementName, propertyName, attribute);
            return color is null ? null : "new global::Cerneala.UI.Media.SolidColorBrush(" + color + ")";
        }

        private string? Invalid(MarkupAttribute attribute, string elementName, string propertyName, string value)
        {
            Report(InvalidPropertyValue, attribute, elementName, propertyName, value);
            return null;
        }

        private void Report(DiagnosticDescriptor descriptor, object locationSource, params object[] args)
        {
            Location location = CreateLocation(file, locationSource);
            Diagnostic diagnostic = Diagnostic.Create(descriptor, location, args);
            string key = descriptor.Id + "|" + location.SourceSpan.Start.ToString(CultureInfo.InvariantCulture) +
                "|" + location.SourceSpan.Length.ToString(CultureInfo.InvariantCulture) +
                "|" + diagnostic.GetMessage(CultureInfo.InvariantCulture);
            if (!reportedDiagnostics.Add(key))
            {
                return;
            }

            HasErrors = true;
            context.ReportDiagnostic(diagnostic);
        }

        private void ReportSharedDiagnostics()
        {
            SourceText source = SourceText.From(file.Text ?? string.Empty, Encoding.UTF8);
            foreach (LanguageDiagnostic diagnostic in semanticModel.Diagnostics)
            {
                Diagnostic hostDiagnostic = SourceGeneratorDiagnosticAdapter.ToDiagnostic(diagnostic, file.Path, source);
                string key = diagnostic.Id + "|" + diagnostic.Span.Start.ToString(CultureInfo.InvariantCulture) +
                    "|" + diagnostic.Span.Length.ToString(CultureInfo.InvariantCulture) +
                    "|" + diagnostic.Message;
                if (!reportedDiagnostics.Add(key))
                {
                    continue;
                }

                HasErrors |= hostDiagnostic.Severity == DiagnosticSeverity.Error;
                context.ReportDiagnostic(hostDiagnostic);
            }
        }

        private void ReportMotion(MotionDiagnosticKind kind, object locationSource, string message)
        {
            DiagnosticDescriptor descriptor = kind switch
            {
                MotionDiagnosticKind.Syntax => MotionSyntaxDiagnostic,
                MotionDiagnosticKind.Target => MotionTargetDiagnostic,
                MotionDiagnosticKind.Event => MotionEventDiagnostic,
                MotionDiagnosticKind.Type => MotionTypeDiagnostic,
                MotionDiagnosticKind.Composition => MotionCompositionDiagnostic,
                MotionDiagnosticKind.Lifecycle => MotionLifecycleDiagnostic,
                MotionDiagnosticKind.Capability => MotionCapabilityDiagnostic,
                _ => MotionSyntaxDiagnostic
            };
            Report(descriptor, locationSource, Path.GetFileName(file.Path), message);
        }

        private static MotionDiagnosticKind ClassifyMotionParseError(string message)
        {
            if (message.Contains("@parallel", StringComparison.Ordinal) ||
                message.Contains("@sequence", StringComparison.Ordinal) ||
                message.Contains("child execution", StringComparison.Ordinal) ||
                message.Contains("composition", StringComparison.OrdinalIgnoreCase))
            {
                return MotionDiagnosticKind.Composition;
            }

            if (message.Contains("@presence", StringComparison.Ordinal) ||
                message.Contains("@layout", StringComparison.Ordinal) ||
                message.Contains("@scroll", StringComparison.Ordinal) ||
                message.Contains("@drag", StringComparison.Ordinal) ||
                message.Contains("@gesture", StringComparison.Ordinal))
            {
                return MotionDiagnosticKind.Lifecycle;
            }

            return message.Contains("Unsupported", StringComparison.OrdinalIgnoreCase) ||
                message.Contains("does not support", StringComparison.OrdinalIgnoreCase)
                ? MotionDiagnosticKind.Capability
                : MotionDiagnosticKind.Syntax;
        }
    }

    private static Location CreateLocation(MarkupSource file, object locationSource)
    {
        if (locationSource is DirectiveExpressionLocation expressionLocation)
        {
            return CreateLocation(file, expressionLocation);
        }

        if (locationSource is MarkupObject markupObject)
        {
            return CreateLocation(file, markupObject);
        }

        if (locationSource is LanguageTextSpan span)
        {
            return CreateLocation(file, span);
        }

        return Location.Create(file.Path, TextSpan.FromBounds(0, 0), new LinePositionSpan(new LinePosition(0, 0), new LinePosition(0, 0)));
    }

    private static Location CreateLocation(MarkupSource file, DirectiveExpressionLocation location)
    {
        int start = Math.Min(file.Text?.Length ?? 0, location.Source.Span.Start + Math.Max(0, location.Offset));
        return CreateLocation(file, new LanguageTextSpan(start, Math.Max(0, location.Length)));
    }

    private static Location CreateLocation(MarkupSource file, MarkupObject markupObject) =>
        CreateLocation(file, markupObject.Span);

    private static Location CreateLocation(MarkupSource file, LanguageTextSpan languageSpan)
    {
        SourceText sourceText = SourceText.From(file.Text ?? string.Empty, Encoding.UTF8);
        int start = Math.Max(0, Math.Min(sourceText.Length, languageSpan.Start));
        int end = Math.Max(start, Math.Min(sourceText.Length, languageSpan.End));
        TextSpan span = TextSpan.FromBounds(start, end);
        return Location.Create(file.Path, span, sourceText.Lines.GetLinePositionSpan(span));
    }

    private static Location CreateLocation(MarkupSource file, int oneBasedLine, int oneBasedColumn, int length = 0)
    {
        SourceText sourceText = SourceText.From(file.Text ?? string.Empty, Encoding.UTF8);
        int line = Math.Max(0, Math.Min(sourceText.Lines.Count - 1, oneBasedLine - 1));
        int column = Math.Max(0, oneBasedColumn - 1);
        int start = Math.Min(sourceText.Length, sourceText.Lines[line].Start + column);
        int end = Math.Min(sourceText.Length, start + Math.Max(0, length));
        TextSpan span = TextSpan.FromBounds(start, end);
        return Location.Create(file.Path, span, sourceText.Lines.GetLinePositionSpan(span));
    }
}
