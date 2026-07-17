using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace Cerneala.SourceGen;

public sealed partial class UiMarkupGenerator
{
    private readonly struct ApplicationPairResolution
    {
        public ApplicationPairResolution(bool hasCompanion, INamedTypeSymbol? pair)
        {
            HasCompanion = hasCompanion;
            Pair = pair;
        }

        public bool HasCompanion { get; }

        public INamedTypeSymbol? Pair { get; }
    }

    private static ApplicationPairResolution ResolveApplicationPair(
        SourceProductionContext context,
        MarkupSource file,
        Compilation compilation)
    {
        ParsedDocument parsed = ParseDocument(file);
        bool applicationDocument = parsed.Document?.Root.Name.LocalName == "Application";

        SyntaxTree[] trees = compilation.SyntaxTrees
            .Where(tree => PathsEqual(tree.FilePath, file.Path + ".cs"))
            .ToArray();
        if (trees.Length != 1)
        {
            if (applicationDocument)
            {
                ReportApplicationDiagnostic(context, file, "The Application definition requires exactly one companion C# file.");
                return new ApplicationPairResolution(true, null);
            }

            return default;
        }

        string expectedName = Path.GetFileNameWithoutExtension(Path.GetFileNameWithoutExtension(file.Path));
        ClassDeclarationSyntax[] declarations = trees[0].GetRoot().DescendantNodes()
            .OfType<ClassDeclarationSyntax>()
            .Where(declaration => declaration.Identifier.ValueText == expectedName)
            .ToArray();
        if (declarations.Length != 1 ||
            compilation.GetSemanticModel(trees[0]).GetDeclaredSymbol(declarations[0]) is not INamedTypeSymbol type)
        {
            if (applicationDocument)
            {
                ReportApplicationDiagnostic(context, file, $"The companion file must declare exactly one class named '{expectedName}'.");
                return new ApplicationPairResolution(true, null);
            }

            return default;
        }

        INamedTypeSymbol? applicationType = compilation.GetTypeByMetadataName("Cerneala.UI.Application");
        if (applicationType is null || !ApplicationTypeDerivesFrom(type, applicationType))
        {
            if (applicationDocument)
            {
                ReportApplicationDiagnostic(context, file, $"Class '{expectedName}' must derive from Application.");
                return new ApplicationPairResolution(true, null);
            }

            return default;
        }

        if (!applicationDocument)
        {
            ReportApplicationDiagnostic(context, file, "A paired Application document must use <Application> as its root wrapper.", parsed.Document?.Root);
            return new ApplicationPairResolution(true, null);
        }

        if (!declarations[0].Modifiers.Any(SyntaxKind.PartialKeyword) ||
            type.IsAbstract ||
            type.InstanceConstructors.Any(constructor => !constructor.IsImplicitlyDeclared))
        {
            ReportApplicationDiagnostic(context, file, "The companion must be a concrete partial class without a user-declared constructor.");
            return new ApplicationPairResolution(true, null);
        }

        if (type.GetMembers("ConfigureServices").OfType<IMethodSymbol>().Any(method => method.IsStatic))
        {
            ReportApplicationDiagnostic(
                context,
                file,
                "The legacy static App.ConfigureServices hook cannot be combined with App : Application.");
            return new ApplicationPairResolution(true, null);
        }

        return new ApplicationPairResolution(true, type);
    }

    private static GenerationScope.ApplicationResourceCatalog? GenerateApplicationFile(
        SourceProductionContext context,
        MarkupSource file,
        string className,
        Compilation compilation,
        INamedTypeSymbol application)
    {
        ParsedDocument parsed = ParseDocument(file);
        XElement root = parsed.Document!.Root;
        XAttribute? startupAttribute = root.Attribute("StartupWindow");
        if (startupAttribute is null || string.IsNullOrWhiteSpace(startupAttribute.Value))
        {
            ReportApplicationStartupDiagnostic(context, file, root, "StartupWindow is required.");
            return null;
        }

        string startupName = startupAttribute.Value.Trim();
        INamedTypeSymbol[] candidates = ResolveApplicationStartupTypes(compilation, application, startupName);
        if (candidates.Length > 1)
        {
            ReportApplicationStartupDiagnostic(
                context,
                file,
                startupAttribute,
                $"StartupWindow '{startupAttribute.Value}' is ambiguous in the companion class scope.");
            return null;
        }

        if (candidates.Length == 0)
        {
            ReportApplicationStartupDiagnostic(
                context,
                file,
                startupAttribute,
                $"StartupWindow '{startupAttribute.Value}' is unknown or inaccessible.");
            return null;
        }

        INamedTypeSymbol startupType = candidates[0];
        INamedTypeSymbol? windowBase = compilation.GetTypeByMetadataName("Cerneala.UI.Controls.Window");
        if (SymbolEqualityComparer.Default.Equals(startupType, application))
        {
            ReportApplicationStartupDiagnostic(
                context,
                file,
                startupAttribute,
                "StartupWindow cannot refer to the Application class itself.");
            return null;
        }

        if (windowBase is null || !ApplicationTypeDerivesFrom(startupType, windowBase))
        {
            ReportApplicationStartupDiagnostic(
                context,
                file,
                startupAttribute,
                $"StartupWindow '{startupAttribute.Value}' must derive from Window.");
            return null;
        }

        if (startupType.IsAbstract || startupType.TypeKind != TypeKind.Class)
        {
            ReportApplicationStartupDiagnostic(
                context,
                file,
                startupAttribute,
                $"StartupWindow '{startupAttribute.Value}' must be concrete.");
            return null;
        }

        foreach (XAttribute attribute in root.Attributes().Where(attribute =>
            attribute.Name.LocalName is not "StartupWindow" and not "ShutdownMode"))
        {
            ReportApplicationDiagnostic(context, file, $"Attribute '{attribute.Name.LocalName}' is not valid on Application.", attribute);
            return null;
        }

        XElement? illegalChild = root.Elements()
            .FirstOrDefault(element => element.Name.LocalName != "Application.Resources");
        if (illegalChild is not null)
        {
            ReportApplicationDiagnostic(context, file, "Application does not accept visual child elements.", illegalChild);
            return null;
        }

        if (root.Nodes().OfType<XText>().Any(text => !string.IsNullOrWhiteSpace(text.Value)))
        {
            ReportApplicationDiagnostic(context, file, "Application does not accept directives or raw text content.", root);
            return null;
        }

        XElement? motionClip = root.Element("Application.Resources")?
            .Elements()
            .FirstOrDefault(element => element.Name.LocalName == "MotionClip");
        if (motionClip is not null)
        {
            ReportApplicationDiagnostic(
                context,
                file,
                "MotionClip is not valid in Application.Resources because Application has no visual namescope.",
                motionClip);
            return null;
        }

        GenerationScope scope = new(context, file, parsed.Document!, compilation, dataType: null);
        scope.EmitApplicationResources();
        if (scope.HasErrors)
        {
            return null;
        }

        string appCode = application.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
        string windowCode = startupType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
        string namespaceName = application.ContainingNamespace.IsGlobalNamespace
            ? string.Empty
            : application.ContainingNamespace.ToDisplayString();
        StringBuilder source = new();
        source.AppendLine("// <auto-generated />");
        source.AppendLine("#nullable enable");
        if (namespaceName.Length > 0)
        {
            source.Append("namespace ").Append(namespaceName).AppendLine(";");
            source.AppendLine();
        }

        source.Append("partial class ").Append(application.Name).AppendLine();
        source.AppendLine("{");
        source.Append("    public ").Append(application.Name).AppendLine("()");
        source.AppendLine("    {");
        source.AppendLine("        __CernealaInitialize();");
        source.AppendLine("    }");
        source.AppendLine();
        source.AppendLine("    private void __CernealaInitialize()");
        source.AppendLine("    {");
        foreach (string line in scope.Lines)
        {
            source.Append("        ").AppendLine(line);
        }

        if (root.Attribute("ShutdownMode") is XAttribute shutdown)
        {
            string[] validModes = ["OnLastWindowClose", "OnMainWindowClose", "OnExplicitShutdown"];
            string? mode = validModes.FirstOrDefault(candidate =>
                string.Equals(candidate, shutdown.Value.Trim(), StringComparison.Ordinal));
            if (mode is null)
            {
                ReportApplicationDiagnostic(
                    context,
                    file,
                    $"ShutdownMode '{shutdown.Value}' is invalid.",
                    shutdown);
                return null;
            }

            source.Append("        ShutdownMode = global::Cerneala.UI.ApplicationShutdownMode.")
                .Append(mode).AppendLine(";");
        }
        source.AppendLine("    }");
        source.AppendLine("}");
        source.AppendLine();
        source.AppendLine("internal static class __CernealaGeneratedApplicationStartup");
        source.AppendLine("{");
        source.AppendLine(compilation.GetEntryPoint(default) is null
            ? "    [global::System.STAThreadAttribute]\n    private static int Main(string[] args)\n    {\n        return global::Cerneala.UI.Hosting.Windows.GeneratedWindowApplication.Run(CreateDescriptor(), args);\n    }"
            : "    [global::System.Runtime.CompilerServices.ModuleInitializerAttribute]\n    internal static void Register()\n    {\n        global::Cerneala.UI.Hosting.Windows.GeneratedWindowApplication.RegisterStartup(CreateDescriptor());\n    }");
        source.AppendLine();
        source.AppendLine("    private static global::Cerneala.UI.Hosting.Windows.GeneratedWindowStartupDescriptor CreateDescriptor()");
        source.AppendLine("    {");
        source.AppendLine("        return new global::Cerneala.UI.Hosting.Windows.GeneratedWindowStartupDescriptor(");
        source.Append("            static () => new ").Append(appCode).AppendLine("(),");
        source.AppendLine("            ConfigureServices,");
        source.Append("            static provider => global::Microsoft.Extensions.DependencyInjection.ServiceProviderServiceExtensions.GetRequiredService<")
            .Append(windowCode).AppendLine(">(provider),");
        source.Append("            \"").Append(windowCode).AppendLine("\");");
        source.AppendLine("    }");
        source.AppendLine();
        source.AppendLine("    private static void ConfigureServices(global::Microsoft.Extensions.DependencyInjection.IServiceCollection services)");
        source.AppendLine("    {");
        INamedTypeSymbol? viewModel = ResolveWindowViewModel(compilation, startupType);
        if (viewModel is { TypeKind: TypeKind.Class, IsAbstract: false })
        {
            source.Append("        global::Microsoft.Extensions.DependencyInjection.ServiceCollectionServiceExtensions.AddTransient<")
                .Append(viewModel.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)).AppendLine(">(services);");
        }

        source.Append("        global::Microsoft.Extensions.DependencyInjection.ServiceCollectionServiceExtensions.AddTransient<")
            .Append(windowCode).AppendLine(">(services);");
        source.AppendLine("    }");
        source.AppendLine("}");
        context.AddSource(
            CreateHintName(file.Path, className).Replace("Factory.", "Application."),
            SourceText.From(source.ToString(), Encoding.UTF8));
        return scope.CreateApplicationResourceCatalog();
    }

    private static void ReportApplicationDiagnostic(
        SourceProductionContext context,
        MarkupSource file,
        string message,
        object? location = null)
    {
        context.ReportDiagnostic(Diagnostic.Create(
            InvalidApplication,
            CreateLocation(file, location ?? new object()),
            Path.GetFileName(file.Path),
            message));
    }

    private static void ReportApplicationStartupDiagnostic(
        SourceProductionContext context,
        MarkupSource file,
        object location,
        string message)
    {
        context.ReportDiagnostic(Diagnostic.Create(
            InvalidApplicationStartup,
            CreateLocation(file, location),
            Path.GetFileName(file.Path),
            message));
    }

    private static bool ApplicationTypeDerivesFrom(INamedTypeSymbol type, INamedTypeSymbol baseType)
    {
        for (INamedTypeSymbol? current = type; current is not null; current = current.BaseType)
        {
            if (SymbolEqualityComparer.Default.Equals(current, baseType))
            {
                return true;
            }
        }

        return false;
    }

    private static INamedTypeSymbol[] ResolveApplicationStartupTypes(
        Compilation compilation,
        INamedTypeSymbol application,
        string startupName)
    {
        string normalized = startupName.StartsWith("global::", StringComparison.Ordinal)
            ? startupName.Substring("global::".Length)
            : startupName;
        if (normalized.IndexOf(".", StringComparison.Ordinal) >= 0)
        {
            string simpleName = normalized.Split('.').Last();
            return compilation.GetSymbolsWithName(simpleName, SymbolFilter.Type)
                .OfType<INamedTypeSymbol>()
                .Where(type => string.Equals(
                    type.ToDisplayString(SymbolDisplayFormat.CSharpErrorMessageFormat),
                    normalized,
                    StringComparison.Ordinal))
                .Where(type => compilation.IsSymbolAccessibleWithin(type, application))
                .ToArray();
        }

        SyntaxReference? declarationReference = application.DeclaringSyntaxReferences.FirstOrDefault();
        if (declarationReference?.GetSyntax() is not ClassDeclarationSyntax declaration)
        {
            return [];
        }

        return compilation.GetSemanticModel(declaration.SyntaxTree)
            .LookupNamespacesAndTypes(declaration.SpanStart, name: normalized)
            .OfType<INamedTypeSymbol>()
            .Where(type => compilation.IsSymbolAccessibleWithin(type, application))
            .ToArray();
    }

    private static INamedTypeSymbol? ResolveWindowViewModel(Compilation compilation, INamedTypeSymbol windowType)
    {
        INamedTypeSymbol? genericWindow = compilation.GetTypeByMetadataName("Cerneala.UI.Controls.Window`1");
        for (INamedTypeSymbol? current = windowType.BaseType; current is not null; current = current.BaseType)
        {
            if (genericWindow is not null &&
                SymbolEqualityComparer.Default.Equals(current.OriginalDefinition, genericWindow))
            {
                return current.TypeArguments[0] as INamedTypeSymbol;
            }
        }

        return null;
    }
}
