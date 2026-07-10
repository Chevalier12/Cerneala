using System;
using System.Collections.Generic;
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
    private readonly struct WindowPairResolution
    {
        public WindowPairResolution(bool hasCompanion, UserControlPair? pair)
        {
            HasCompanion = hasCompanion;
            Pair = pair;
        }

        public bool HasCompanion { get; }

        public UserControlPair? Pair { get; }
    }

    private static WindowPairResolution ResolveWindowPair(
        SourceProductionContext context,
        MarkupSource file,
        Compilation compilation)
    {
        bool windowDocument = file.Text is not null &&
            ParseDocument(file).Document?.Root.Name.LocalName == "Window";
        string companionPath = file.Path + ".cs";
        SyntaxTree[] matchingTrees = compilation.SyntaxTrees
            .Where(tree => PathsEqual(tree.FilePath, companionPath))
            .ToArray();
        if (matchingTrees.Length != 1)
        {
            if (windowDocument && matchingTrees.Length > 1)
            {
                ReportWindowDiagnostic(context, file, "More than one companion C# syntax tree has the expected path.");
                return new WindowPairResolution(true, null);
            }

            return new WindowPairResolution(false, null);
        }

        SyntaxTree tree = matchingTrees[0];
        string expectedName = Path.GetFileNameWithoutExtension(Path.GetFileNameWithoutExtension(file.Path));
        ClassDeclarationSyntax[] declarations = tree.GetRoot()
            .DescendantNodes()
            .OfType<ClassDeclarationSyntax>()
            .Where(declaration => declaration.Identifier.ValueText == expectedName)
            .ToArray();
        if (declarations.Length != 1)
        {
            if (windowDocument)
            {
                ReportWindowDiagnostic(context, file, $"The companion file must declare exactly one class named '{expectedName}'.");
                return new WindowPairResolution(true, null);
            }

            return new WindowPairResolution(false, null);
        }

        ClassDeclarationSyntax declaration = declarations[0];
        SemanticModel semanticModel = compilation.GetSemanticModel(tree);
        if (semanticModel.GetDeclaredSymbol(declaration) is not INamedTypeSymbol typeSymbol)
        {
            if (windowDocument)
            {
                ReportWindowDiagnostic(context, file, $"Class '{expectedName}' could not be resolved by Roslyn.");
                return new WindowPairResolution(true, null);
            }

            return new WindowPairResolution(false, null);
        }

        INamedTypeSymbol? windowType = compilation.GetTypeByMetadataName("Cerneala.UI.Controls.Window");
        INamedTypeSymbol? genericWindowType = compilation.GetTypeByMetadataName("Cerneala.UI.Controls.Window`1");
        if (windowType is null || genericWindowType is null)
        {
            return new WindowPairResolution(false, null);
        }

        INamedTypeSymbol? viewModelType = null;
        bool derivesFromWindow = false;
        for (INamedTypeSymbol? current = typeSymbol.BaseType; current is not null; current = current.BaseType)
        {
            if (SymbolEqualityComparer.Default.Equals(current.OriginalDefinition, genericWindowType))
            {
                derivesFromWindow = true;
                viewModelType = current.TypeArguments[0] as INamedTypeSymbol;
                break;
            }

            if (SymbolEqualityComparer.Default.Equals(current, windowType))
            {
                derivesFromWindow = true;
                break;
            }
        }

        if (!derivesFromWindow)
        {
            if (windowDocument)
            {
                ReportWindowDiagnostic(context, file, $"Class '{expectedName}' must derive from Window or Window<TViewModel>.");
                return new WindowPairResolution(true, null);
            }

            return new WindowPairResolution(false, null);
        }

        if (typeSymbol.ContainingType is not null || typeSymbol.Arity != 0)
        {
            ReportWindowDiagnostic(context, file, "The companion class must be non-nested and non-generic.");
            return new WindowPairResolution(true, null);
        }

        if (!declaration.Modifiers.Any(SyntaxKind.PartialKeyword) || declaration.Modifiers.Any(SyntaxKind.FileKeyword))
        {
            ReportWindowDiagnostic(context, file, "The companion class must be a non-file-local partial class.");
            return new WindowPairResolution(true, null);
        }

        if (typeSymbol.IsAbstract)
        {
            ReportWindowDiagnostic(context, file, "The companion class must be concrete.");
            return new WindowPairResolution(true, null);
        }

        if (typeSymbol.InstanceConstructors.Any(constructor => !constructor.IsImplicitlyDeclared))
        {
            ReportWindowDiagnostic(context, file, "User-declared constructors are not supported; the markup generator owns construction in this version.");
            return new WindowPairResolution(true, null);
        }

        return new WindowPairResolution(
            true,
            new UserControlPair(typeSymbol, tree, declaration.SpanStart, viewModelType, isWindow: true));
    }

    private static void GenerateWindowFile(
        SourceProductionContext context,
        MarkupSource file,
        string className,
        Compilation compilation,
        UserControlPair pair,
        bool generateStartup)
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
        if (!string.Equals(document.Root.Name.LocalName, "Window", StringComparison.Ordinal))
        {
            ReportWindowDiagnostic(context, file, "A paired Window document must use <Window> as its root wrapper.", document.Root);
            return;
        }

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

        INamedTypeSymbol? declaredDataType = ResolveDataType(context, file, document, compilation);
        if (document.Root.Attribute("DataType") is not null && declaredDataType is null)
        {
            return;
        }

        INamedTypeSymbol? dataType = pair.ViewModelType ?? declaredDataType;
        if (pair.ViewModelType is not null && declaredDataType is not null &&
            !SymbolEqualityComparer.Default.Equals(pair.ViewModelType, declaredDataType))
        {
            context.ReportDiagnostic(Diagnostic.Create(
                InvalidBindingSource,
                CreateLocation(file, document.Root.Attribute("DataType")!),
                document.Root.Attribute("DataType")!.Value,
                "DataType must exactly match the TViewModel argument declared by Window<TViewModel>."));
            return;
        }

        GenerationScope scope = new(context, file, document, compilation, dataType, pair);
        string? rootVariable = scope.EmitWindowRoot(document.Root);
        if (scope.HasErrors)
        {
            return;
        }

        string namespaceName = pair.TypeSymbol.ContainingNamespace.IsGlobalNamespace
            ? string.Empty
            : pair.TypeSymbol.ContainingNamespace.ToDisplayString();
        StringBuilder source = new();
        source.AppendLine("// <auto-generated />");
        source.AppendLine("#nullable enable");
        if (namespaceName.Length > 0)
        {
            source.Append("namespace ").Append(namespaceName).AppendLine(";");
            source.AppendLine();
        }

        source.Append("partial class ").Append(pair.TypeSymbol.Name).AppendLine();
        source.AppendLine("{");
        if (pair.ViewModelType is null)
        {
            source.Append("    public ").Append(pair.TypeSymbol.Name).AppendLine("()");
            source.AppendLine("    {");
            source.AppendLine("        __CernealaInitialize();");
            source.AppendLine("    }");
        }
        else
        {
            string viewModelCode = pair.ViewModelType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
            source.Append("    public ").Append(pair.TypeSymbol.Name).Append('(').Append(viewModelCode).AppendLine(" viewModel)");
            source.AppendLine("    {");
            source.AppendLine("        global::System.ArgumentNullException.ThrowIfNull(viewModel);");
            source.AppendLine("        DataContext = viewModel;");
            source.AppendLine("        __CernealaInitialize();");
            source.AppendLine("    }");
        }

        source.AppendLine();
        source.AppendLine("    private void __CernealaInitialize()");
        source.AppendLine("    {");
        source.AppendLine("        Content = __CernealaCreateContent();");
        source.AppendLine("    }");
        source.AppendLine();
        source.AppendLine("    private global::Cerneala.UI.Elements.UIElement? __CernealaCreateContent()");
        source.AppendLine("    {");
        foreach (string line in scope.Lines)
        {
            source.Append("        ").AppendLine(line);
        }

        foreach (string line in scope.PostLines)
        {
            source.Append("        ").AppendLine(line);
        }

        source.Append("        return ").Append(rootVariable ?? "null").AppendLine(";");
        source.AppendLine("    }");
        AppendNamedElementMembers(source, scope.NamedElementMembers);
        source.AppendLine("}");

        if (generateStartup && compilation.Options.OutputKind != OutputKind.DynamicallyLinkedLibrary)
        {
            AppendWindowStartup(context, source, compilation, pair, namespaceName);
        }

        string hintName = CreateHintName(file.Path, className).Replace("Factory.", "Window.");
        context.AddSource(hintName, SourceText.From(source.ToString(), Encoding.UTF8));
    }

    private static void AppendNamedElementMembers(
        StringBuilder source,
        IReadOnlyList<GenerationScope.NamedElementMember> members)
    {
        foreach (GenerationScope.NamedElementMember member in members)
        {
            source.AppendLine();
            source.Append("    private ").Append(member.TypeCode);
            if (member.IsConditional)
            {
                source.Append("? ").Append(member.MemberName).AppendLine(" { get; set; }");
                source.Append("    private ").Append(member.TypeCode).Append("? ").Append(member.CacheMemberName).AppendLine(" { get; set; }");
            }
            else
            {
                source.Append(' ').Append(member.MemberName).AppendLine(" { get; set; } = null!;");
            }
        }
    }

    private static void AppendWindowStartup(
        SourceProductionContext context,
        StringBuilder source,
        Compilation compilation,
        UserControlPair pair,
        string namespaceName)
    {
        IMethodSymbol? appHook = ResolveAppHook(context, compilation, pair, namespaceName);
        if (appHook is null && HasInvalidAppHook(compilation, namespaceName))
        {
            return;
        }

        string windowCode = pair.TypeSymbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
        source.AppendLine();
        source.AppendLine("internal static class __CernealaGeneratedWindowStartup");
        source.AppendLine("{");
        if (compilation.GetEntryPoint(default) is null)
        {
            source.AppendLine("    [global::System.STAThreadAttribute]");
            source.AppendLine("    private static void Main()");
            source.AppendLine("    {");
            source.AppendLine("        global::Cerneala.UI.Hosting.Windows.GeneratedWindowApplication.Run(CreateDescriptor());");
            source.AppendLine("    }");
        }
        else
        {
            source.AppendLine("    [global::System.Runtime.CompilerServices.ModuleInitializerAttribute]");
            source.AppendLine("    internal static void Register()");
            source.AppendLine("    {");
            source.AppendLine("        global::Cerneala.UI.Hosting.Windows.GeneratedWindowApplication.RegisterStartup(CreateDescriptor());");
            source.AppendLine("    }");
        }

        source.AppendLine();
        source.AppendLine("    private static global::Cerneala.UI.Hosting.Windows.GeneratedWindowStartupDescriptor CreateDescriptor()");
        source.AppendLine("    {");
        source.AppendLine("        return new global::Cerneala.UI.Hosting.Windows.GeneratedWindowStartupDescriptor(");
        source.AppendLine("            ConfigureServices,");
        source.Append("            static provider => global::Microsoft.Extensions.DependencyInjection.ServiceProviderServiceExtensions.GetRequiredService<")
            .Append(windowCode).AppendLine(">(provider));");
        source.AppendLine("    }");
        source.AppendLine();
        source.AppendLine("    private static void ConfigureServices(global::Microsoft.Extensions.DependencyInjection.IServiceCollection services)");
        source.AppendLine("    {");
        if (pair.ViewModelType is not null)
        {
            string viewModelCode = pair.ViewModelType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
            if (pair.ViewModelType.TypeKind == TypeKind.Class && !pair.ViewModelType.IsAbstract)
            {
                source.Append("        global::Microsoft.Extensions.DependencyInjection.ServiceCollectionServiceExtensions.AddTransient<")
                    .Append(viewModelCode).AppendLine(">(services);");
            }

            source.Append("        global::Microsoft.Extensions.DependencyInjection.ServiceCollectionServiceExtensions.AddTransient<")
                .Append(windowCode).Append(">(services, static provider => new ").Append(windowCode)
                .Append("(global::Microsoft.Extensions.DependencyInjection.ServiceProviderServiceExtensions.GetRequiredService<")
                .Append(viewModelCode).AppendLine(">(provider)));");
        }
        else
        {
            source.Append("        global::Microsoft.Extensions.DependencyInjection.ServiceCollectionServiceExtensions.AddTransient<")
                .Append(windowCode).AppendLine(">(services);");
        }

        if (appHook is not null)
        {
            string appCode = appHook.ContainingType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
            source.Append("        ").Append(appCode).AppendLine(".ConfigureServices(services);");
        }

        source.AppendLine("    }");
        source.AppendLine("}");
    }

    private static IMethodSymbol? ResolveAppHook(
        SourceProductionContext context,
        Compilation compilation,
        UserControlPair pair,
        string namespaceName)
    {
        string metadataName = namespaceName.Length == 0 ? "App" : namespaceName + ".App";
        INamedTypeSymbol? appType = compilation.GetTypeByMetadataName(metadataName);
        if (appType is null)
        {
            return null;
        }

        INamedTypeSymbol? serviceCollectionType = compilation.GetTypeByMetadataName("Microsoft.Extensions.DependencyInjection.IServiceCollection");
        IMethodSymbol[] namedMethods = appType.GetMembers("ConfigureServices").OfType<IMethodSymbol>().ToArray();
        IMethodSymbol[] compatible = namedMethods
            .Where(method => method.IsStatic && method.ReturnsVoid && method.Arity == 0 && method.Parameters.Length == 1)
            .Where(method => SymbolEqualityComparer.Default.Equals(method.Parameters[0].Type, serviceCollectionType))
            .Where(method => method.DeclaredAccessibility is Accessibility.Public or Accessibility.Internal)
            .ToArray();
        if (compatible.Length == 1)
        {
            return compatible[0];
        }

        if (namedMethods.Length > 0)
        {
            context.ReportDiagnostic(Diagnostic.Create(
                InvalidWindowStartup,
                pair.TypeSymbol.Locations.FirstOrDefault() ?? Location.None,
                "App.ConfigureServices must be a unique accessible static void method with one IServiceCollection parameter."));
        }

        return null;
    }

    private static bool HasInvalidAppHook(Compilation compilation, string namespaceName)
    {
        string metadataName = namespaceName.Length == 0 ? "App" : namespaceName + ".App";
        return compilation.GetTypeByMetadataName(metadataName)?.GetMembers("ConfigureServices").Length > 0;
    }

    private static void ReportWindowDiagnostic(
        SourceProductionContext context,
        MarkupSource file,
        string message,
        object? locationSource = null)
    {
        context.ReportDiagnostic(Diagnostic.Create(
            InvalidWindow,
            CreateLocation(file, locationSource ?? new object()),
            Path.GetFileName(file.Path),
            message));
    }

    private sealed partial class GenerationScope
    {
        public string? EmitWindowRoot(XElement root)
        {
            EmitRuntimeResources(root, "this");
            DirectiveParseResult parsed = GetDirectiveContent(
                root,
                DirectiveContentKind.Elements | DirectiveContentKind.Templates);
            if (parsed.Error is not null)
            {
                Report(InvalidDirective, parsed.ErrorSource ?? root, Path.GetFileName(file.Path), parsed.Error);
                return null;
            }

            List<DirectiveElementNode> directElements = parsed.Nodes.OfType<DirectiveElementNode>().ToList();
            if (directElements.Count > 1)
            {
                Report(InvalidWindow, root, Path.GetFileName(file.Path), "The <Window> wrapper accepts at most one direct visual child.");
                return null;
            }

            if (parsed.Nodes.OfType<DirectiveWhenNode>().Any(ContainsElement))
            {
                Report(InvalidWindow, root, Path.GetFileName(file.Path), "Conditional elements cannot be direct children of the <Window> wrapper; place them inside its permanent root container.");
                return null;
            }

            if (root.Attribute("Name") is not null)
            {
                Report(InvalidWindow, root.Attribute("Name")!, Path.GetFileName(file.Path), "The <Window> wrapper represents this and cannot declare Name.");
                return null;
            }

            IReadOnlyList<AspectResource> aspects = ResolveAspects(root);
            ApplyAspects(root, "this", aspects);
            DirectiveTemplateNode[] templates = parsed.Nodes.OfType<DirectiveTemplateNode>().ToArray();
            if (templates.Length > 1)
            {
                Report(
                    InvalidComponentTemplate,
                    templates[1].Source,
                    Path.GetFileName(file.Path),
                    "A Window may declare only one @template block.");
            }
            else if (templates.Length == 1)
            {
                EmitDirectTemplate(root, "this", templates[0], ownerIsRoot: true);
            }

            foreach (XAttribute attribute in root.Attributes().Where(attribute =>
                !attribute.IsNamespaceDeclaration && attribute.Name.LocalName is not "Aspect" and not "Name" and not "DataType"))
            {
                if (!TryEmitEventAttribute(root, "this", attribute, userControlPair!.TypeSymbol))
                {
                    EmitProperty(root, "this", attribute);
                }
            }

            ReactivePlan plan = new("this", "Window", isRoot: true);
            foreach (AspectResource aspect in aspects)
            {
                foreach (DirectiveWhenNode when in aspect.Conditions)
                {
                    CollectWhen(plan, when, "true", "global::Cerneala.UI.Core.UiPropertyValueSource.AspectVisualState");
                }
            }

            foreach (DirectiveNode node in parsed.Nodes)
            {
                switch (node)
                {
                    case DirectiveWhenNode directiveWhen:
                        CollectWhen(plan, directiveWhen, "true", "global::Cerneala.UI.Core.UiPropertyValueSource.MarkupConditional");
                        break;
                    case DirectiveElementNode _:
                        break;
                    case DirectiveTemplateNode _:
                        break;
                    case DirectiveDefaultNode defaults:
                        Report(InvalidDirective, defaults.Source, Path.GetFileName(file.Path), "@default is valid only inside Aspect resources.");
                        break;
                    case DirectiveAssignmentNode assignment:
                        Report(InvalidDirective, assignment.Source, Path.GetFileName(file.Path), "Property assignments must be inside an @if block.");
                        break;
                    case DirectiveTextNode text:
                        Report(InvalidWindow, text.Source, Path.GetFileName(file.Path), "The <Window> wrapper does not accept raw text content.");
                        break;
                }
            }

            EmitReactivePlan(plan, controlsContent: false);
            return directElements.Count == 0 ? null : EmitElement(directElements[0].Element);
        }
    }
}
