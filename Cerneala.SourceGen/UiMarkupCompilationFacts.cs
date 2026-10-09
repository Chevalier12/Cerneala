using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using Cerneala.Language;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Cerneala.SourceGen;

public sealed partial class UiMarkupGenerator
{
    // Index C# declarations once per compilation, not once per markup document.
    // This traversal skips type/member bodies; binding/emission remain downstream
    // of the value-equatable, document-specific facts.
    private sealed class CompilationDeclarationIndex
    {
        private readonly Dictionary<string, ImmutableArray<SyntaxTree>> companions = new(StringComparer.Ordinal);
        private readonly Dictionary<INamedTypeSymbol, MarkupCompilationFacts.TypeDeclarationFacts> typeFacts = new(SymbolEqualityComparer.Default);

        public CompilationDeclarationIndex(Compilation compilation, CancellationToken cancellationToken)
        {
            Compilation = compilation;
            References = compilation.References.ToImmutableArray();
            Types = compilation.GetSymbolsWithName(static _ => true, SymbolFilter.Type, cancellationToken)
                .OfType<INamedTypeSymbol>().ToLookup(type => type.Name, StringComparer.Ordinal);
            ScopeDirectives = compilation.SyntaxTrees.SelectMany(tree => tree.GetRoot(cancellationToken)
                .DescendantNodes(static node => node is CompilationUnitSyntax or BaseNamespaceDeclarationSyntax)
                .Where(node => node is UsingDirectiveSyntax or ExternAliasDirectiveSyntax)
                .Select(directive => (Tree: tree, Directive: directive))).ToArray();
        }

        public Compilation Compilation { get; }
        public ImmutableArray<MetadataReference> References { get; }
        public ILookup<string, INamedTypeSymbol> Types { get; }
        public (SyntaxTree Tree, SyntaxNode Directive)[] ScopeDirectives { get; }

        public ImmutableArray<SyntaxTree> CompanionTrees(string markupPath)
        {
            lock (companions)
            {
                if (!companions.TryGetValue(markupPath, out ImmutableArray<SyntaxTree> trees))
                {
                    string path = CernealaDocumentPath.GetCompanionPath(markupPath);
                    trees = Compilation.SyntaxTrees.Where(tree => PathsEqual(tree.FilePath, path)).ToImmutableArray();
                    companions.Add(markupPath, trees);
                }
                return trees;
            }
        }

        public MarkupCompilationFacts.TypeDeclarationFacts TypeFacts(INamedTypeSymbol type, CancellationToken cancellationToken)
        {
            // A nullable/constructed use belongs to an edge, not to the shared
            // declaration node. Canonicalize before populating the node cache.
            type = (INamedTypeSymbol)type.OriginalDefinition.WithNullableAnnotation(NullableAnnotation.None);
            lock (typeFacts)
            {
                if (!typeFacts.TryGetValue(type, out MarkupCompilationFacts.TypeDeclarationFacts? facts))
                {
                    facts = MarkupCompilationFacts.TypeDeclarationFacts.Create(this, type, cancellationToken);
                    typeFacts.Add(type, facts);
                }
                return facts;
            }
        }
    }

    // The Compilation is a transport for the existing Roslyn-based binder/emitter,
    // not an equality key. Only declaration facts reachable from these documents
    // may invalidate them. Include the documents themselves so newly introduced
    // markup always gets the current compilation, even after an unrelated edit.
    private sealed class MarkupCompilationFacts : IEquatable<MarkupCompilationFacts>
    {
        private static readonly Regex Identifier = new(@"[\p{L}\p{Nl}_][\p{L}\p{Nl}\p{Nd}\p{Pc}\p{Mn}\p{Mc}\p{Cf}]*", RegexOptions.CultureInvariant);
        // Keep fixed Roslyn lookups in the binder/emitter represented here too.
        // Exact metadata names avoid depending on unrelated classes that happen
        // to be named Control, Color, Object, etc. in another namespace.
        private static readonly string[] InfrastructureMetadataNames =
        [
            "Cerneala.UI.Application", "Cerneala.UI.Controls.Window", "Cerneala.UI.Controls.Window`1",
            "Cerneala.UI.Controls.UserControl", "Cerneala.UI.Controls.UserControl`1", "Cerneala.UI.Controls.Scene2D",
            "Cerneala.UI.Elements.UIElement", "Cerneala.UI.Controls.Control", "Cerneala.UI.Controls.ItemsControl",
            "Cerneala.UI.Controls.SceneItems2D", "Cerneala.UI.Controls.RenderSurface2D", "Cerneala.UI.Layout.Panels.Panel",
            "Cerneala.UI.Controls.Decorator", "Cerneala.UI.Controls.ContentControl", "Cerneala.UI.Controls.ScrollViewer",
            "Cerneala.UI.Controls.Sprite2D", "Cerneala.UI.Servo.Servo", "Cerneala.UI.Core.UiProperty`1",
            "Cerneala.UI.Core.UiObject", "Cerneala.UI.Media.Transform", "System.ComponentModel.INotifyPropertyChanged",
            "Microsoft.Extensions.DependencyInjection.IServiceCollection", ApplicationBackendAttributeMetadataName,
            "Cerneala.Drawing.Color", "Cerneala.UI.Prism.Runtime.PrismBlendRange", "System.Numerics.Vector4",
            "Cerneala.UI.Prism.Definitions.PrismResourceId", "Cerneala.Drawing.Prism.Catalog.PrismColorProfile",
            "Cerneala.Drawing.Prism.Catalog.PrismBlendMode", "Cerneala.UI.Prism.Definitions.PrismMaskChannel",
            "Cerneala.UI.Prism.Runtime.PrismBlendChannels", "Cerneala.UI.Prism.Runtime.PrismKnockout",
            "Cerneala.UI.Prism.Runtime.PrismBlendIfChannel", "System.Object", "System.Boolean", "System.Single", "System.Int32"
        ];
        private static readonly SymbolDisplayFormat SignatureFormat = new(
            globalNamespaceStyle: SymbolDisplayGlobalNamespaceStyle.Included,
            typeQualificationStyle: SymbolDisplayTypeQualificationStyle.NameAndContainingTypesAndNamespaces,
            genericsOptions: SymbolDisplayGenericsOptions.IncludeTypeParameters |
                SymbolDisplayGenericsOptions.IncludeTypeConstraints | SymbolDisplayGenericsOptions.IncludeVariance,
            memberOptions: SymbolDisplayMemberOptions.IncludeAccessibility |
                SymbolDisplayMemberOptions.IncludeModifiers | SymbolDisplayMemberOptions.IncludeType |
                SymbolDisplayMemberOptions.IncludeContainingType | SymbolDisplayMemberOptions.IncludeParameters |
                SymbolDisplayMemberOptions.IncludeConstantValue | SymbolDisplayMemberOptions.IncludeExplicitInterface,
            parameterOptions: SymbolDisplayParameterOptions.IncludeType | SymbolDisplayParameterOptions.IncludeName |
                SymbolDisplayParameterOptions.IncludeParamsRefOut | SymbolDisplayParameterOptions.IncludeDefaultValue |
                SymbolDisplayParameterOptions.IncludeOptionalBrackets,
            propertyStyle: SymbolDisplayPropertyStyle.ShowReadWriteDescriptor,
            miscellaneousOptions: SymbolDisplayMiscellaneousOptions.IncludeNullableReferenceTypeModifier |
                SymbolDisplayMiscellaneousOptions.EscapeKeywordIdentifiers);

        private readonly ImmutableArray<string> facts;
        private readonly ImmutableArray<MetadataReference> references;

        private MarkupCompilationFacts(ImmutableArray<MarkupSource> files, CompilationDeclarationIndex declarations,
            ImmutableArray<string> facts)
        {
            Files = files;
            Compilation = declarations.Compilation;
            this.facts = facts;
            references = declarations.References;
        }

        public ImmutableArray<MarkupSource> Files { get; }
        public Compilation Compilation { get; }

        public static ImmutableArray<string> GetTypeNames(string? text) => Identifier.Matches(text ?? string.Empty)
            .Cast<Match>().Select(match => match.Value).Distinct(StringComparer.Ordinal).ToImmutableArray();

        public static MarkupCompilationFacts Create(ImmutableArray<MarkupSource> files,
            CompilationDeclarationIndex declarations, CancellationToken cancellationToken, bool forEmission = false)
        {
            Compilation compilation = declarations.Compilation;
            HashSet<string> names = new(StringComparer.Ordinal);
            foreach (MarkupSource file in files)
            {
                // This is deliberately a superset: identifiers in element names,
                // aliases, DataType, StartupWindow, directives and expressions can
                // all name a type. Unknown names remain dependencies: adding that
                // type later changes the candidate set below.
                names.UnionWith(file.TypeNames);
                names.Add(CernealaDocumentPath.GetLogicalName(file.Path));
            }

            FactsBuilder builder = new(declarations, cancellationToken);
            foreach (string metadataName in InfrastructureMetadataNames)
                builder.Type(compilation.GetTypeByMetadataName(metadataName));
            foreach (INamedTypeSymbol type in names.SelectMany(name => declarations.Types[name])
                .OrderBy(type => type.ToDisplayString(), StringComparer.Ordinal))
                builder.Type(type);

            HashSet<SyntaxTree> companionTrees = new(files.SelectMany(file => declarations.CompanionTrees(file.Path)));
            foreach (var (tree, directive) in declarations.ScopeDirectives)
            {
                // LookupNamespacesAndTypes for Application startup uses companion
                // scope. Global usings can change it from any C# file.
                if (directive is UsingDirectiveSyntax { GlobalKeyword.RawKind: not 0 } || companionTrees.Contains(tree))
                    builder.Add("using", tree.FilePath, directive.WithoutTrivia().ToString());
            }
            foreach (SyntaxTree tree in compilation.SyntaxTrees)
            {
                cancellationToken.ThrowIfCancellationRequested();
                if (!companionTrees.Contains(tree))
                    continue;
                builder.Add("companion", tree.FilePath);
                SemanticModel model = compilation.GetSemanticModel(tree);
                foreach (TypeDeclarationSyntax declaration in tree.GetRoot(cancellationToken)
                    .DescendantNodes().OfType<TypeDeclarationSyntax>())
                {
                    builder.Add("declaration", declaration.RawKind.ToString(CultureInfo.InvariantCulture),
                        declaration.Identifier.ValueText, declaration.Modifiers.ToString());
                    ITypeSymbol? type = model.GetDeclaredSymbol(declaration, cancellationToken) as ITypeSymbol;
                    builder.Type(type);
                    if (type is not null && compilation.Options.OutputKind != OutputKind.DynamicallyLinkedLibrary)
                    {
                        string namespaceName = type.ContainingNamespace.IsGlobalNamespace
                            ? string.Empty : type.ContainingNamespace.ToDisplayString() + ".";
                        builder.Type(compilation.GetTypeByMetadataName(namespaceName + "App"));
                        // ResolveAppHook reports its invalid-hook diagnostic on
                        // the legacy startup window's first type location. No
                        // other type/member definition location is emitted.
                        if (forEmission && type.Name == "MainWindow" && files.Any(file =>
                            file.Document?.Root.Name.LocalName == "Window" &&
                            CernealaDocumentPath.GetLogicalName(file.Path) == type.Name &&
                            declarations.CompanionTrees(file.Path).Contains(tree)))
                            builder.DiagnosticLocation(type.Locations.FirstOrDefault());
                    }
                }
            }

            builder.Symbol(compilation.GetEntryPoint(cancellationToken));
            builder.Attributes(compilation.Assembly.GetAttributes(), includeLocation:
                forEmission && compilation.Options.OutputKind != OutputKind.DynamicallyLinkedLibrary);
            builder.Add("assembly", compilation.Assembly.Identity.ToString());
            return new MarkupCompilationFacts(files, declarations, builder.Facts.ToImmutableArray());
        }

        public bool Equals(MarkupCompilationFacts? other) => other is not null &&
            Compilation.Options.Equals(other.Compilation.Options) &&
            references.SequenceEqual(other.references) &&
            Files.Length == other.Files.Length && Files.Select(file => (file.Path, file.Text))
                .SequenceEqual(other.Files.Select(file => (file.Path, file.Text))) &&
            facts.SequenceEqual(other.facts, StringComparer.Ordinal);

        public override bool Equals(object? obj) => obj is MarkupCompilationFacts other && Equals(other);
        public override int GetHashCode() => facts.Length;

        // Each node contains only its own declaration facts plus dependency
        // edges. This avoids recursively expanding/caching constructed types and
        // makes graph equality independent of cache population/traversal history.
        public sealed class TypeDeclarationFacts
        {
            private TypeDeclarationFacts(ImmutableArray<string> facts, ImmutableArray<ITypeSymbol> dependencies)
            {
                Facts = facts;
                Dependencies = dependencies;
            }
            public ImmutableArray<string> Facts { get; }
            public ImmutableArray<ITypeSymbol> Dependencies { get; }
            public static TypeDeclarationFacts Create(CompilationDeclarationIndex declarations, INamedTypeSymbol type,
                CancellationToken cancellationToken)
            {
                FactsBuilder builder = new(declarations, cancellationToken, collectDependencies: true);
                builder.AppendDeclaration(type);
                return new TypeDeclarationFacts(builder.Facts.ToImmutableArray(), builder.Dependencies.ToImmutableArray());
            }
        }

        private sealed class FactsBuilder(CompilationDeclarationIndex declarations, CancellationToken cancellationToken,
            bool collectDependencies = false)
        {
            private readonly Compilation compilation = declarations.Compilation;
            private readonly HashSet<ITypeSymbol> visited = new(SymbolEqualityComparer.Default);
            public List<string> Facts { get; } = new();
            public List<ITypeSymbol> Dependencies { get; } = new();

            // Separate length-prefixed values, rather than hashes, preserve exact
            // equality even for arbitrary string constants and attribute values.
            public void Add(params string?[] values)
            {
                Facts.Add(values.Length.ToString(CultureInfo.InvariantCulture) + ":" + string.Concat(values.Select(value =>
                    value is null ? "-1:" : value.Length.ToString(CultureInfo.InvariantCulture) + ":" + value)));
            }

            public void Type(ITypeSymbol? type)
            {
                if (collectDependencies)
                {
                    if (type is not null) Dependencies.Add(type);
                    return;
                }
                if (type is null || !visited.Add(type))
                    return;
                cancellationToken.ThrowIfCancellationRequested();
                if (type is IArrayTypeSymbol array)
                {
                    Type(array.ElementType);
                    return;
                }
                if (type is IPointerTypeSymbol pointer)
                {
                    Type(pointer.PointedAtType);
                    return;
                }
                if (type is ITypeParameterSymbol parameter)
                {
                    foreach (ITypeSymbol constraint in parameter.ConstraintTypes)
                        Type(constraint);
                    return;
                }
                if (type is not INamedTypeSymbol named)
                    return;
                foreach (ITypeSymbol argument in named.TypeArguments)
                    Type(argument);
                // Metadata facts are owned by the immutable references above.
                // Inspect original source definitions only; expanding constructed
                // members can recurse forever (e.g. Node<T>.Next: Node<Node<T>>).
                if (!SymbolEqualityComparer.Default.Equals(named.ContainingAssembly, compilation.Assembly))
                    return;
                if (!SymbolEqualityComparer.Default.Equals(named, named.OriginalDefinition))
                {
                    Type(named.OriginalDefinition);
                    return;
                }
                TypeDeclarationFacts node = declarations.TypeFacts(named, cancellationToken);
                Facts.AddRange(node.Facts);
                foreach (ITypeSymbol dependency in node.Dependencies) Type(dependency);
            }

            public void AppendDeclaration(INamedTypeSymbol named)
            {
                cancellationToken.ThrowIfCancellationRequested();
                Symbol(named);
                Add("type", named.TypeKind.ToString(), named.IsAbstract.ToString(), named.IsSealed.ToString(),
                    named.IsStatic.ToString(), named.BaseType?.ToDisplayString(SignatureFormat));
                Type(named.ContainingType);
                Type(named.BaseType);
                foreach (INamedTypeSymbol implemented in named.Interfaces)
                {
                    Add("interface", implemented.ToDisplayString(SignatureFormat));
                    Type(implemented);
                }
                foreach (SyntaxReference syntax in named.DeclaringSyntaxReferences)
                {
                    if (syntax.GetSyntax(cancellationToken) is TypeDeclarationSyntax declaration)
                        Add("modifiers", declaration.Modifiers.ToString());
                }
                foreach (ISymbol member in named.GetMembers())
                {
                    Symbol(member);
                    switch (member)
                    {
                        case INamedTypeSymbol nested: Type(nested); break;
                        case IPropertySymbol property:
                            Type(property.Type);
                            Symbol(property.GetMethod);
                            Symbol(property.SetMethod);
                            foreach (IParameterSymbol index in property.Parameters) Symbol(index);
                            break;
                        case IFieldSymbol field:
                            Type(field.Type);
                            Add("constant", field.HasConstantValue.ToString(),
                                Convert.ToString(field.ConstantValue, CultureInfo.InvariantCulture));
                            break;
                        case IEventSymbol @event: Type(@event.Type); break;
                        case IMethodSymbol method:
                            Type(method.ReturnType);
                            foreach (IParameterSymbol argument in method.Parameters) Symbol(argument);
                            foreach (ITypeParameterSymbol methodParameter in method.TypeParameters) Type(methodParameter);
                            break;
                    }
                }
            }

            public void Symbol(ISymbol? symbol)
            {
                if (symbol is null)
                {
                    Add("no-symbol");
                    return;
                }
                Add("symbol", symbol.Kind.ToString(), symbol.ToDisplayString(SignatureFormat),
                    symbol.DeclaredAccessibility.ToString(), symbol.IsImplicitlyDeclared.ToString());
                // Documentation and definition positions belong to language
                // hover/navigation. SourceGen consumes neither; retaining them
                // here would invalidate output on comments or whitespace alone.
                Attributes(symbol.GetAttributes());
                if (symbol is IMethodSymbol method)
                    Add("method", method.MethodKind.ToString(), method.IsInitOnly.ToString(), method.RefKind.ToString(),
                        method.ReturnType.ToDisplayString(SignatureFormat));
                if (symbol is IParameterSymbol parameter)
                    Type(parameter.Type);
            }

            public void Attributes(ImmutableArray<AttributeData> attributes, bool includeLocation = false)
            {
                Add("attributes", attributes.Length.ToString(CultureInfo.InvariantCulture));
                foreach (AttributeData attribute in attributes)
                {
                    Add("attribute", attribute.AttributeClass?.ToDisplayString(SignatureFormat),
                        attribute.ConstructorArguments.Length.ToString(CultureInfo.InvariantCulture),
                        attribute.NamedArguments.Length.ToString(CultureInfo.InvariantCulture));
                    Type(attribute.AttributeClass);
                    foreach (TypedConstant argument in attribute.ConstructorArguments) Constant(argument);
                    foreach (KeyValuePair<string, TypedConstant> argument in attribute.NamedArguments)
                    {
                        Add(argument.Key);
                        Constant(argument.Value);
                    }
                    if (includeLocation && SymbolEqualityComparer.Default.Equals(attribute.AttributeClass,
                        compilation.GetTypeByMetadataName(ApplicationBackendAttributeMetadataName)) &&
                        attribute.ApplicationSyntaxReference is { } syntax)
                        Location(syntax.GetSyntax(cancellationToken).GetLocation());
                }
            }

            private void Constant(TypedConstant constant)
            {
                Add("argument", constant.Kind.ToString(), constant.Type?.ToDisplayString(SignatureFormat),
                    constant.IsNull.ToString());
                Type(constant.Type);
                if (constant.Kind == TypedConstantKind.Array)
                {
                    Add("array-length", constant.IsNull ? null : constant.Values.Length.ToString(CultureInfo.InvariantCulture));
                    if (!constant.IsNull)
                        foreach (TypedConstant value in constant.Values) Constant(value);
                }
                else if (constant.Value is ITypeSymbol type)
                {
                    Add(type.ToDisplayString(SignatureFormat));
                    Type(type);
                }
                else
                    Add(Convert.ToString(constant.Value, CultureInfo.InvariantCulture));
            }

            private void Location(Location location)
            {
                FileLinePositionSpan lines = location.GetLineSpan();
                FileLinePositionSpan mapped = location.GetMappedLineSpan();
                Add("location", lines.Path, location.SourceSpan.ToString(), lines.Span.ToString(), mapped.Path, mapped.Span.ToString());
            }

            public void DiagnosticLocation(Location? location)
            {
                if (location is { IsInSource: true })
                    Location(location);
            }
        }
    }
}
