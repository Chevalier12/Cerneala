using System;
using System.Collections.Generic;
using System.Collections.Immutable;
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
    private sealed class UserControlPair
    {
        public UserControlPair(
            INamedTypeSymbol typeSymbol,
            SyntaxTree syntaxTree,
            int lookupPosition,
            INamedTypeSymbol? viewModelType)
        {
            TypeSymbol = typeSymbol;
            SyntaxTree = syntaxTree;
            LookupPosition = lookupPosition;
            ViewModelType = viewModelType;
        }

        public INamedTypeSymbol TypeSymbol { get; }

        public SyntaxTree SyntaxTree { get; }

        public int LookupPosition { get; }

        public INamedTypeSymbol? ViewModelType { get; }
    }

    private readonly struct UserControlPairResolution
    {
        public UserControlPairResolution(bool hasCompanion, UserControlPair? pair)
        {
            HasCompanion = hasCompanion;
            Pair = pair;
        }

        public bool HasCompanion { get; }

        public UserControlPair? Pair { get; }
    }

    private static UserControlPairResolution ResolveUserControlPair(
        SourceProductionContext context,
        MarkupSource file,
        Compilation compilation)
    {
        string companionPath = file.Path + ".cs";
        SyntaxTree[] matchingTrees = compilation.SyntaxTrees
            .Where(tree => PathsEqual(tree.FilePath, companionPath))
            .ToArray();
        if (matchingTrees.Length == 0)
        {
            return new UserControlPairResolution(false, null);
        }

        if (matchingTrees.Length != 1)
        {
            ReportUserControlDiagnostic(context, file, "More than one companion C# syntax tree has the expected path.");
            return new UserControlPairResolution(true, null);
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
            ReportUserControlDiagnostic(context, file, $"The companion file must declare exactly one class named '{expectedName}'.");
            return new UserControlPairResolution(true, null);
        }

        ClassDeclarationSyntax declaration = declarations[0];
        SemanticModel semanticModel = compilation.GetSemanticModel(tree);
        if (semanticModel.GetDeclaredSymbol(declaration) is not INamedTypeSymbol typeSymbol)
        {
            ReportUserControlDiagnostic(context, file, $"Class '{expectedName}' could not be resolved by Roslyn.");
            return new UserControlPairResolution(true, null);
        }

        if (typeSymbol.ContainingType is not null || typeSymbol.Arity != 0)
        {
            ReportUserControlDiagnostic(context, file, "The companion class must be non-nested and non-generic.");
            return new UserControlPairResolution(true, null);
        }

        if (!declaration.Modifiers.Any(SyntaxKind.PartialKeyword) || declaration.Modifiers.Any(SyntaxKind.FileKeyword))
        {
            ReportUserControlDiagnostic(context, file, "The companion class must be a non-file-local partial class.");
            return new UserControlPairResolution(true, null);
        }

        if (typeSymbol.IsAbstract)
        {
            ReportUserControlDiagnostic(context, file, "The companion class must be concrete.");
            return new UserControlPairResolution(true, null);
        }

        INamedTypeSymbol? userControlType = compilation.GetTypeByMetadataName("Cerneala.UI.Controls.UserControl");
        INamedTypeSymbol? genericUserControlType = compilation.GetTypeByMetadataName("Cerneala.UI.Controls.UserControl`1");
        if (userControlType is null || genericUserControlType is null)
        {
            ReportUserControlDiagnostic(context, file, "The Cerneala UserControl runtime types are unavailable.");
            return new UserControlPairResolution(true, null);
        }

        INamedTypeSymbol? viewModelType = null;
        bool derivesFromUserControl = false;
        for (INamedTypeSymbol? current = typeSymbol.BaseType; current is not null; current = current.BaseType)
        {
            if (SymbolEqualityComparer.Default.Equals(current.OriginalDefinition, genericUserControlType))
            {
                derivesFromUserControl = true;
                viewModelType = current.TypeArguments[0] as INamedTypeSymbol;
                break;
            }

            if (SymbolEqualityComparer.Default.Equals(current, userControlType))
            {
                derivesFromUserControl = true;
                break;
            }
        }

        if (!derivesFromUserControl)
        {
            ReportUserControlDiagnostic(context, file, $"Class '{expectedName}' must derive from UserControl or UserControl<TViewModel>.");
            return new UserControlPairResolution(true, null);
        }

        if (typeSymbol.InstanceConstructors.Any(constructor => !constructor.IsImplicitlyDeclared))
        {
            ReportUserControlDiagnostic(context, file, "User-declared constructors are not supported; the markup generator owns construction in this version.");
            return new UserControlPairResolution(true, null);
        }

        return new UserControlPairResolution(
            true,
            new UserControlPair(typeSymbol, tree, declaration.SpanStart, viewModelType));
    }

    private static void GenerateUserControlFile(
        SourceProductionContext context,
        MarkupSource file,
        string className,
        Compilation compilation,
        UserControlPair pair)
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
        if (!string.Equals(document.Root.Name.LocalName, "UserControl", StringComparison.Ordinal))
        {
            ReportUserControlDiagnostic(context, file, "A paired markup document must use <UserControl> as its root wrapper.", document.Root);
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
                "DataType must exactly match the TViewModel argument declared by UserControl<TViewModel>."));
            return;
        }

        GenerationScope scope = new(context, file, document, compilation, dataType, pair);
        string? rootVariable = scope.EmitUserControlRoot(document.Root);
        if (scope.HasErrors)
        {
            return;
        }

        string typeCode = pair.TypeSymbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
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
        source.Append("    private static readonly global::Cerneala.UI.Controls.Templates.ComponentTemplate<")
            .Append(typeCode)
            .Append("> __CernealaGeneratedTemplate = new(")
            .Append(LiteralForGeneratedCode(pair.TypeSymbol.Name))
            .AppendLine(", static context => context.Owner.__CernealaCreateContent());");
        source.AppendLine();
        source.Append("    public ").Append(pair.TypeSymbol.Name).AppendLine("()");
        source.AppendLine("    {");
        source.AppendLine("        __CernealaInitialize();");
        source.AppendLine("    }");
        if (pair.ViewModelType is not null)
        {
            string viewModelCode = pair.ViewModelType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
            source.AppendLine();
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
        source.AppendLine("        ComponentTemplate = __CernealaGeneratedTemplate;");
        source.AppendLine("        ApplyTemplate();");
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

        foreach (GenerationScope.NamedElementMember member in scope.NamedElementMembers)
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

        source.AppendLine("}");

        string hintName = CreateHintName(file.Path, className).Replace("Factory.", "UserControl.");
        context.AddSource(hintName, SourceText.From(source.ToString(), Encoding.UTF8));
    }

    private static bool PathsEqual(string left, string right)
    {
        if (string.IsNullOrWhiteSpace(left) || string.IsNullOrWhiteSpace(right))
        {
            return false;
        }

        try
        {
            left = Path.GetFullPath(left);
            right = Path.GetFullPath(right);
        }
        catch (Exception)
        {
            left = left.Replace('\\', '/');
            right = right.Replace('\\', '/');
        }

        return string.Equals(left, right, StringComparison.OrdinalIgnoreCase);
    }

    private static void ReportUserControlDiagnostic(
        SourceProductionContext context,
        MarkupSource file,
        string message,
        object? locationSource = null)
    {
        context.ReportDiagnostic(Diagnostic.Create(
            InvalidUserControl,
            CreateLocation(file, locationSource ?? new object()),
            Path.GetFileName(file.Path),
            message));
    }

    private static string LiteralForGeneratedCode(string value)
    {
        return SymbolDisplay.FormatLiteral(value, quote: true);
    }

    private sealed partial class GenerationScope
    {
        public sealed class NamedElementMember
        {
            public NamedElementMember(string memberName, string typeCode, bool isConditional)
            {
                MemberName = memberName;
                TypeCode = typeCode;
                IsConditional = isConditional;
                CacheMemberName = "__CernealaCached_" + memberName;
            }

            public string MemberName { get; }

            public string TypeCode { get; }

            public bool IsConditional { get; }

            public string CacheMemberName { get; }
        }

        public string? EmitUserControlRoot(XElement root)
        {
            EmitRuntimeResources(root, "this");
            DirectiveParseResult parsed = GetDirectiveContent(root, allowAssignments: false, allowElements: true);
            if (parsed.Error is not null)
            {
                Report(InvalidDirective, parsed.ErrorSource ?? root, Path.GetFileName(file.Path), parsed.Error);
                return null;
            }

            List<DirectiveElementNode> directElements = parsed.Nodes.OfType<DirectiveElementNode>().ToList();
            if (directElements.Count > 1)
            {
                Report(InvalidUserControl, root, Path.GetFileName(file.Path), "The <UserControl> wrapper accepts at most one direct visual child.");
                return null;
            }

            if (parsed.Nodes.OfType<DirectiveWhenNode>().Any(ContainsElement))
            {
                Report(InvalidUserControl, root, Path.GetFileName(file.Path), "Conditional elements cannot be direct children of the <UserControl> wrapper; place them inside its permanent root container.");
                return null;
            }

            if (root.Attribute("Name") is not null)
            {
                Report(InvalidUserControl, root.Attribute("Name")!, Path.GetFileName(file.Path), "The <UserControl> wrapper represents this and cannot declare Name.");
                return null;
            }

            IReadOnlyList<AspectResource> aspects = ResolveAspects(root);
            ApplyAspects(root, "this", aspects);
            foreach (XAttribute attribute in root.Attributes().Where(attribute =>
                !attribute.IsNamespaceDeclaration && attribute.Name.LocalName is not "Aspect" and not "Name" and not "DataType"))
            {
                if (!TryEmitEventAttribute(root, "this", attribute, userControlPair!.TypeSymbol))
                {
                    EmitProperty(root, "this", attribute);
                }
            }

            ReactivePlan plan = new("this", "UserControl");
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
                    case DirectiveDefaultNode defaults:
                        Report(InvalidDirective, defaults.Source, Path.GetFileName(file.Path), "@default is valid only inside Aspect resources.");
                        break;
                    case DirectiveAssignmentNode assignment:
                        Report(InvalidDirective, assignment.Source, Path.GetFileName(file.Path), "Property assignments must be inside an @if block.");
                        break;
                    case DirectiveTextNode text:
                        Report(InvalidUserControl, text.Source, Path.GetFileName(file.Path), "The <UserControl> wrapper does not accept raw text content.");
                        break;
                }
            }

            EmitReactivePlan(plan, controlsContent: false);
            return directElements.Count == 0 ? null : EmitElement(directElements[0].Element);
        }

        private static bool ContainsElement(DirectiveWhenNode when)
        {
            foreach (DirectiveIfNode branch in when.Branches)
            {
                if (branch.Body.OfType<DirectiveElementNode>().Any() || branch.Body.OfType<DirectiveWhenNode>().Any(ContainsElement))
                {
                    return true;
                }
            }

            return false;
        }

        private INamedTypeSymbol? ResolveCustomElementType(string elementName)
        {
            if (userControlPair is null)
            {
                return null;
            }

            SemanticModel model = compilation.GetSemanticModel(userControlPair.SyntaxTree);
            INamedTypeSymbol? uiElementType = compilation.GetTypeByMetadataName("Cerneala.UI.Elements.UIElement");
            if (uiElementType is null)
            {
                return null;
            }

            INamedTypeSymbol[] candidates = model.LookupNamespacesAndTypes(userControlPair.LookupPosition, name: elementName)
                .Select(symbol => symbol is IAliasSymbol alias ? alias.Target : symbol)
                .OfType<INamedTypeSymbol>()
                .Where(type => type.TypeKind == TypeKind.Class && !type.IsAbstract)
                .Where(type => !SymbolEqualityComparer.Default.Equals(type, userControlPair.TypeSymbol))
                .Where(type => IsOrDerivesFrom(type, uiElementType))
                .Where(type => compilation.IsSymbolAccessibleWithin(type, userControlPair.TypeSymbol))
                .Where(HasAccessibleParameterlessConstructor)
                .Distinct<INamedTypeSymbol>(SymbolEqualityComparer.Default)
                .ToArray();
            return candidates.Length == 1 ? candidates[0] : null;
        }

        private bool HasAccessibleParameterlessConstructor(INamedTypeSymbol type)
        {
            return type.InstanceConstructors.Any(constructor =>
                constructor.Parameters.Length == 0 &&
                compilation.IsSymbolAccessibleWithin(constructor, userControlPair!.TypeSymbol));
        }

        private static bool IsOrDerivesFrom(INamedTypeSymbol type, INamedTypeSymbol expectedBase)
        {
            for (INamedTypeSymbol? current = type; current is not null; current = current.BaseType)
            {
                if (SymbolEqualityComparer.Default.Equals(current, expectedBase))
                {
                    return true;
                }
            }

            return false;
        }

        private bool TryEmitEventAttribute(
            XElement element,
            string variable,
            XAttribute attribute,
            INamedTypeSymbol? explicitType = null)
        {
            if (userControlPair is null)
            {
                return false;
            }

            INamedTypeSymbol? elementType = explicitType;
            if (elementType is null && !resolvedElementTypes.TryGetValue(element.Name.LocalName, out elementType))
            {
                return false;
            }

            IEventSymbol? eventSymbol = FindEvent(elementType, attribute.Name.LocalName);
            if (eventSymbol is null)
            {
                return false;
            }

            if (eventSymbol.AddMethod is null ||
                !compilation.IsSymbolAccessibleWithin(eventSymbol.AddMethod, userControlPair.TypeSymbol))
            {
                Report(InvalidEventHandler, attribute, attribute.Value.Trim(), element.Name.LocalName, attribute.Name.LocalName, "The event is not accessible from code-behind.");
                return true;
            }

            string handlerName = attribute.Value.Trim();
            if (!SyntaxFacts.IsValidIdentifier(handlerName) || eventSymbol.Type is not INamedTypeSymbol delegateType ||
                delegateType.DelegateInvokeMethod is not IMethodSymbol invokeMethod)
            {
                Report(InvalidEventHandler, attribute, handlerName, element.Name.LocalName, attribute.Name.LocalName, "The attribute must name a compatible instance method.");
                return true;
            }

            IMethodSymbol[] compatible = FindMethods(userControlPair.TypeSymbol, handlerName)
                .Where(method => compilation.IsSymbolAccessibleWithin(method, userControlPair.TypeSymbol))
                .Where(method => IsCompatibleEventHandler(method, invokeMethod))
                .ToArray();
            if (compatible.Length != 1)
            {
                string reason = compatible.Length == 0
                    ? "No compatible instance method was found."
                    : "More than one compatible overload was found.";
                Report(InvalidEventHandler, attribute, handlerName, element.Name.LocalName, attribute.Name.LocalName, reason);
                return true;
            }

            currentLines.Add(variable + "." + eventSymbol.Name + " += this." + handlerName + ";");
            return true;
        }

        private static IEventSymbol? FindEvent(INamedTypeSymbol type, string name)
        {
            for (INamedTypeSymbol? current = type; current is not null; current = current.BaseType)
            {
                IEventSymbol? found = current.GetMembers(name).OfType<IEventSymbol>().FirstOrDefault();
                if (found is not null)
                {
                    return found;
                }
            }

            return null;
        }

        private static IEnumerable<IMethodSymbol> FindMethods(INamedTypeSymbol type, string name)
        {
            for (INamedTypeSymbol? current = type; current is not null; current = current.BaseType)
            {
                foreach (IMethodSymbol method in current.GetMembers(name).OfType<IMethodSymbol>())
                {
                    yield return method;
                }
            }
        }

        private bool IsCompatibleEventHandler(IMethodSymbol method, IMethodSymbol invokeMethod)
        {
            if (method.IsStatic || method.IsGenericMethod || method.ReturnsVoid != invokeMethod.ReturnsVoid ||
                method.Parameters.Length != invokeMethod.Parameters.Length)
            {
                return false;
            }

            for (int index = 0; index < method.Parameters.Length; index++)
            {
                IParameterSymbol handlerParameter = method.Parameters[index];
                IParameterSymbol delegateParameter = invokeMethod.Parameters[index];
                if (handlerParameter.RefKind != delegateParameter.RefKind ||
                    !compilation.ClassifyConversion(delegateParameter.Type, handlerParameter.Type).IsImplicit)
                {
                    return false;
                }
            }

            return true;
        }

        private void RegisterNamedElement(string markupName, string memberName, string typeCode, XElement source)
        {
            if (userControlPair!.TypeSymbol.GetMembers(memberName).Length > 0)
            {
                Report(InvalidUserControl, source, Path.GetFileName(file.Path), $"Generated member '{memberName}' conflicts with a member declared in code-behind.");
                return;
            }

            bool conditional = conditionalMemberScopes.Count > 0;
            NamedElementMember member = new(memberName, typeCode, conditional);
            namedElementMembers.Add(member);
            currentLines.Add("this." + memberName + " = " + memberName + ";");
            if (conditional)
            {
                currentLines.Add("this." + member.CacheMemberName + " = " + memberName + ";");
                conditionalMemberScopes.Peek().Add(member);
            }
        }

        private string EmitConditionalContentExpression(int order, string factoryName)
        {
            if (!conditionalFactoryMembers.TryGetValue(factoryName, out IReadOnlyList<NamedElementMember>? members) || members.Count == 0)
            {
                return "new global::Cerneala.UI.Markup.MarkupConditionalContent(" + order + ", " + factoryName + ")";
            }

            string activated = string.Join(" ", members.Select(member =>
                "this." + member.MemberName + " = this." + member.CacheMemberName + ";"));
            string deactivated = string.Join(" ", members.Select(member =>
                "this." + member.MemberName + " = null;"));
            return "new global::Cerneala.UI.Markup.MarkupConditionalContent(" + order + ", " + factoryName +
                ", () => { " + activated + " }, () => { " + deactivated + " })";
        }
    }
}
