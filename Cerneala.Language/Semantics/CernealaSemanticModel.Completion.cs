using Cerneala.Language.Semantics.Symbols;
using Cerneala.Language.Syntax;

namespace Cerneala.Language.Semantics;

internal sealed partial class CernealaSemanticModel
{
    internal ILanguageCompilationSymbols CompletionCompilation => compilation;

    internal ElementSyntax? FindCompletionElement(int offset) => document.Syntax.DescendantElements()
        .Where(element => element.Span.Start <= offset && offset <= element.Span.End)
        .OrderBy(element => element.Span.Length)
        .FirstOrDefault() ?? document.Syntax.DescendantElements()
        .Where(element => element.NameToken.Span.Start <= offset)
        .OrderByDescending(element => element.NameToken.Span.Start)
        .FirstOrDefault();

    internal ElementSyntax? GetCompletionParent(ElementSyntax element) =>
        parents.TryGetValue(element, out ElementSyntax? parent) ? parent : null;

    internal ILanguageTypeSymbol? GetCompletionElementType(ElementSyntax? element)
    {
        if (element is null)
        {
            return null;
        }

        if (resourceElements.TryGetValue(element, out ResourceDefinition? resource) &&
            resource.TargetType is not null)
        {
            return resource.TargetType;
        }

        return GetElementType(element, ReferenceEquals(element, root));
    }

    internal ILanguageTypeSymbol? GetCompletionDataType(ElementSyntax? element)
    {
        if (element is null)
        {
            return InferRootDataType();
        }

        if (templateContexts.TryGetValue(element, out SemanticTemplateContext? template) && template.DataType is not null)
        {
            return template.DataType;
        }

        ElementSyntax? current = element;
        while (current is not null)
        {
            if (elementDataTypes.TryGetValue(current, out ILanguageTypeSymbol? dataType) && dataType is not null)
            {
                return dataType;
            }

            current = parents.TryGetValue(current, out ElementSyntax? parent) ? parent : null;
        }

        return InferRootDataType();
    }

    internal ILanguageTypeSymbol? GetCompletionBindingSourceType(ElementSyntax? element, string sourceName)
    {
        if (element is null)
        {
            return null;
        }

        sourceName = sourceName.TrimStart('$');
        if (sourceName == "DataContext")
        {
            return GetCompletionDataType(element);
        }

        if (sourceName == "root")
        {
            return rootType;
        }

        if (sourceName == "self")
        {
            return GetCompletionElementType(element);
        }

        if (sourceName == "owner")
        {
            return templateContexts.TryGetValue(element, out SemanticTemplateContext? template)
                ? template.OwnerType
                : null;
        }

        if (FindNamedElement(element, sourceName) is NamedElementDefinition named)
        {
            return named.Type;
        }

        ResourceDefinition? resource = FindResource(element, sourceName);
        return resource?.Kind == ResourceKind.Aspect ? resource.TargetType : resource?.Type;
    }

    internal IReadOnlyList<CompletionScopedSymbol> GetCompletionSources(ElementSyntax? element)
    {
        if (element is null)
        {
            return Array.Empty<CompletionScopedSymbol>();
        }

        Dictionary<string, CompletionScopedSymbol> result = new(StringComparer.Ordinal)
        {
            ["DataContext"] = new("DataContext", "binding", GetCompletionDataType(element)),
            ["root"] = new("root", "binding", rootType),
            ["self"] = new("self", "binding", GetCompletionElementType(element))
        };
        if (templateContexts.TryGetValue(element, out SemanticTemplateContext? template))
        {
            result["owner"] = new CompletionScopedSymbol("owner", "binding", template.OwnerType);
        }

        SemanticNameScope? nameScope = nameScopes.TryGetValue(element, out SemanticNameScope? direct)
            ? direct
            : nameScopes.Values.FirstOrDefault(scope => scope.Template is null);
        if (nameScope is not null)
        {
            foreach (NamedElementDefinition named in nameScope.Elements.Values)
            {
                if (!result.ContainsKey(named.Name))
                {
                    result.Add(named.Name, new CompletionScopedSymbol(named.Name, "element", named.Type));
                }
            }
        }

        ElementSyntax? current = element;
        while (current is not null)
        {
            if (resourceScopes.TryGetValue(current, out SemanticResourceScope? resourceScope))
            {
                foreach (ResourceDefinition resource in resourceScope.NamedResources.Values)
                {
                    if (!result.ContainsKey(resource.Name!))
                    {
                        result.Add(resource.Name!, new CompletionScopedSymbol(
                            resource.Name!,
                            resource.Kind.ToString(),
                            resource.Kind == ResourceKind.Aspect ? resource.TargetType : resource.Type));
                    }
                }
            }

            current = parents.TryGetValue(current, out ElementSyntax? parent) ? parent : null;
        }

        foreach (ResourceDefinition resource in applicationResources.Values)
        {
            if (!result.ContainsKey(resource.Name!))
            {
                result.Add(resource.Name!, new CompletionScopedSymbol(
                    resource.Name!,
                    resource.Kind.ToString(),
                    resource.Kind == ResourceKind.Aspect ? resource.TargetType : resource.Type));
            }
        }

        return result.Values.OrderBy(value => value.Name, StringComparer.Ordinal).ToArray();
    }

    internal IReadOnlyList<string> GetCompletionMotionHandles(ElementSyntax? element, int offset)
    {
        ElementSyntax? scope = element;
        while (scope is not null && !string.Equals(
            scope.Name.Split(':').Last(),
            "Aspect",
            StringComparison.Ordinal))
        {
            scope = parents.TryGetValue(scope, out ElementSyntax? parent) ? parent : null;
        }

        if (scope is null)
        {
            return Array.Empty<string>();
        }

        return symbols.Where(symbol =>
                symbol.Kind == CernealaSemanticSymbolKind.MotionHandle &&
                symbol.Span.Start < offset &&
                scope.Span.Contains(symbol.Span.Start) &&
                symbol.DefinitionLocation is LanguageSourceLocation definition &&
                definition.Span.Equals(symbol.Span))
            .Select(symbol => symbol.Name)
            .Distinct(StringComparer.Ordinal)
            .OrderBy(name => name, StringComparer.Ordinal)
            .ToArray();
    }

    internal IReadOnlyList<CompletionNamespaceAlias> GetCompletionAliases() => aliases
        .Select(pair => new CompletionNamespaceAlias(pair.Key, pair.Value.Namespace, pair.Value.Assembly))
        .OrderBy(alias => alias.Prefix, StringComparer.Ordinal)
        .ToArray();

    internal string? GetCompletionContentProperty(ILanguageTypeSymbol? type) =>
        type is null ? null : ResolveContentPropertyName(type);

    internal bool IsInTemplate(ElementSyntax? element) =>
        element is not null && templateContexts.ContainsKey(element);

    internal IReadOnlyList<CompletionParameterDefinition> GetCompletionCallParameters(string name)
    {
        MotionClipDefinition? clip = motionClips.Values.FirstOrDefault(candidate =>
            string.Equals(candidate.Resource.Name, name, StringComparison.Ordinal));
        if (clip is not null)
        {
            return clip.Parameters.Values
                .Select(parameter => new CompletionParameterDefinition(
                    parameter.Name,
                    parameter.TypeName,
                    parameter.DefaultValue is null))
                .ToArray();
        }

        PrismCompositionDefinition? composition = prismCompositions.Values.FirstOrDefault(candidate =>
            string.Equals(candidate.Name, name, StringComparison.Ordinal));
        return composition is null
            ? Array.Empty<CompletionParameterDefinition>()
            : composition.Parameters.Values
                .Select(parameter => new CompletionParameterDefinition(
                    parameter.Name,
                    parameter.TypeName,
                    parameter.DefaultValue is null))
                .ToArray();
    }

    internal CernealaResolvedSymbol? ResolveCompletionSymbol(string typeMetadataName, string? memberName)
    {
        ILanguageTypeSymbol? type = compilation.FindType(typeMetadataName);
        if (type is null)
        {
            return null;
        }

        if (memberName is null)
        {
            return new CernealaResolvedSymbol(
                type.MetadataName,
                null,
                type.DocumentationXml,
                false,
                type.AssemblyName);
        }

        ILanguageMemberSymbol? member = type.GetMembers(memberName).FirstOrDefault();
        return member is null
            ? null
            : new CernealaResolvedSymbol(
                member.Signature,
                member.DeclaringTypeMetadataName,
                member.DocumentationXml,
                member.IsDeprecated,
                member.AssemblyName);
    }
}

internal sealed record CompletionScopedSymbol(string Name, string Kind, ILanguageTypeSymbol? Type);

internal sealed record CompletionNamespaceAlias(string Prefix, string Namespace, string Assembly);

internal sealed record CompletionParameterDefinition(string Name, string TypeName, bool Required);

internal sealed record CernealaResolvedSymbol(
    string Signature,
    string? DeclaringType,
    string? DocumentationXml,
    bool IsDeprecated,
    string AssemblyName);
