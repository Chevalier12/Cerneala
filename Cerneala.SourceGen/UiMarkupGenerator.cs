using System;
using System.Collections.Immutable;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
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
    private static readonly DiagnosticDescriptor InvalidSceneComponent = SourceGeneratorDiagnosticAdapter.GetDescriptor("CERNEALAUI017");
    private static readonly DiagnosticDescriptor InvalidEventHandler = SourceGeneratorDiagnosticAdapter.GetDescriptor("CERNEALAUI009");
    private static readonly DiagnosticDescriptor InvalidWindow = SourceGeneratorDiagnosticAdapter.GetDescriptor("CERNEALAUI010");
    private static readonly DiagnosticDescriptor InvalidWindowStartup = SourceGeneratorDiagnosticAdapter.GetDescriptor("CERNEALAUI011");
    private static readonly DiagnosticDescriptor InvalidComponentTemplate = SourceGeneratorDiagnosticAdapter.GetDescriptor("CERNEALAUI012");
    private static readonly DiagnosticDescriptor InvalidApplication = SourceGeneratorDiagnosticAdapter.GetDescriptor("CERNEALAUI013");
    private static readonly DiagnosticDescriptor InvalidApplicationStartup = SourceGeneratorDiagnosticAdapter.GetDescriptor("CERNEALAUI014");
    private static readonly DiagnosticDescriptor InvalidApplicationBackendSelection = SourceGeneratorDiagnosticAdapter.GetDescriptor("CERNEALAUI015");
    private static readonly DiagnosticDescriptor InvalidSpriteAnimation = SourceGeneratorDiagnosticAdapter.GetDescriptor("CERNEALAUI016");
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

            MarkupComponentPairResolution pair = ResolveMarkupComponentPair(context, files[i], compilation);
            if (pair.HasCompanion)
            {
                if (pair.Pair is not null)
                {
                    if (pair.Pair.IsScene)
                    {
                        GenerateSceneComponentFile(
                            context, files[i], classNames[i], compilation, pair.Pair,
                            applicationResources, semanticModels[files[i].Path]);
                    }
                    else
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
        private readonly MarkupComponentPair? userControlPair;
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
            MarkupComponentPair? userControlPair = null,
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
                    DirectiveContentKind.Templates))
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

        private readonly Dictionary<string, NamedSymbol> symbols = new(StringComparer.Ordinal);
        private readonly Dictionary<MarkupElement, ResourceScope> resourceScopes = new();
        private readonly Dictionary<MarkupElement, ResourceScope> resourcePropertyScopes = new();
        private readonly Dictionary<MarkupElement, AspectResource> inlineAspects = new();
        private readonly List<AspectResource> allAspects = [];

        // Aspects of Application resources, bound by the Application document.
        private readonly HashSet<AspectResource> importedAspects = new();
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
