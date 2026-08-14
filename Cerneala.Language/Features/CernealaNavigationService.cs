using Cerneala.Language.Diagnostics;
using Cerneala.Language.Semantics;
using Cerneala.Language.Semantics.Symbols;
using Cerneala.Language.Text;
using Microsoft.CodeAnalysis.CSharp;

namespace Cerneala.Language.Features;

internal sealed class CernealaNavigationService
{
    public CernealaHoverInfo? GetHover(CernealaSemanticModel model, int offset)
    {
        CernealaSemanticSymbol? symbol = model.GetSymbolAt(offset);
        LanguageDiagnostic? diagnostic = model.Diagnostics
            .Where(candidate => candidate.Span.Contains(offset) ||
                candidate.Span.Length == 0 && candidate.Span.Start == offset)
            .OrderBy(candidate => candidate.Span.Length)
            .FirstOrDefault();
        if (symbol is null && diagnostic is null)
        {
            return null;
        }

        ILanguageMemberSymbol? member = symbol?.MemberSymbol;
        ILanguageTypeSymbol? type = symbol?.TypeSymbol;
        string signature = member?.Signature ?? (symbol is null
            ? diagnostic!.Id
            : IsTypeSymbol(symbol.Kind) && type is not null
                ? type.MetadataName
                : DisplayKind(symbol.Kind) + " " + symbol.Name + ": " + symbol.ValueType);
        return new CernealaHoverInfo(
            signature,
            symbol is null ? "diagnostic" : DisplayKind(symbol.Kind),
            member?.DeclaringTypeMetadataName,
            member is null ? type?.BaseType?.MetadataName : null,
            member?.DefaultValue,
            CernealaDocumentation.Extract(member?.DocumentationXml ?? type?.DocumentationXml),
            diagnostic is null ? null : ExplainDiagnostic(diagnostic),
            member?.AssemblyName ?? type?.AssemblyName,
            member?.IsDeprecated == true);
    }

    public IReadOnlyList<CernealaLocation> GetDefinitions(CernealaSemanticModel model, int offset)
    {
        CernealaSemanticSymbol? symbol = model.GetSymbolAt(offset);
        if (symbol is null)
        {
            return Array.Empty<CernealaLocation>();
        }

        if (TryGetLocalDefinition(model, symbol, out CernealaLocation? local))
        {
            return [local!];
        }

        IReadOnlyList<LanguageSourceLocation> locations = symbol.MemberSymbol?.Locations ??
            symbol.TypeSymbol?.Locations ?? Array.Empty<LanguageSourceLocation>();
        if (symbol.Kind == CernealaSemanticSymbolKind.RootType)
        {
            string pairedPath = model.Document.Path + ".cs";
            LanguageSourceLocation[] paired = locations.Where(location =>
                string.Equals(NormalizePath(location.Path), NormalizePath(pairedPath), StringComparison.OrdinalIgnoreCase))
                .ToArray();
            if (paired.Length > 0)
            {
                locations = paired;
            }
        }

        return PreferUserLocations(locations)
            .Select(location => new CernealaLocation(location.Path, location.Span))
            .ToArray();
    }

    public IReadOnlyList<CernealaLocation> GetReferences(
        CernealaSemanticModel model,
        IReadOnlyList<CernealaSemanticModel> workspaceModels,
        int offset,
        bool includeDeclaration,
        CancellationToken cancellationToken = default)
    {
        CernealaSemanticSymbol? symbol = model.GetSymbolAt(offset);
        SymbolIdentity? identity = symbol is null ? null : CreateIdentity(model, symbol);
        if (identity is null)
        {
            return Array.Empty<CernealaLocation>();
        }

        List<CernealaLocation> result = new();
        foreach (CernealaSemanticModel candidateModel in workspaceModels.Distinct())
        {
            cancellationToken.ThrowIfCancellationRequested();
            foreach (CernealaSemanticSymbol candidate in candidateModel.Symbols)
            {
                if (!Matches(candidateModel, candidate, identity) ||
                    !TryGetIdentitySpan(candidateModel, candidate, identity, out TextSpan span))
                {
                    continue;
                }

                bool declaration = IsDeclaration(candidateModel, candidate, identity);
                if (includeDeclaration || !declaration)
                {
                    AddDistinct(result, new CernealaLocation(candidateModel.Document.Path, span));
                }
            }
        }

        if (identity.Kind is IdentityKind.Type or IdentityKind.Member)
        {
            foreach (ILanguageCompilationSymbols compilation in workspaceModels
                .Select(candidate => candidate.NavigationCompilation)
                .Distinct())
            {
                cancellationToken.ThrowIfCancellationRequested();
                foreach (LanguageReferenceLocation reference in compilation.FindReferences(
                    identity.TypeMetadataName!,
                    identity.MemberName,
                    identity.MemberKind,
                    cancellationToken))
                {
                    if ((!includeDeclaration && reference.IsDefinition) ||
                        string.IsNullOrWhiteSpace(reference.Path) ||
                        IsGeneratedPath(reference.Path))
                    {
                        continue;
                    }

                    AddDistinct(result, new CernealaLocation(reference.Path, reference.Span));
                }
            }
        }

        return result
            .OrderBy(location => location.Path, StringComparer.OrdinalIgnoreCase)
            .ThenBy(location => location.Span.Start)
            .ToArray();
    }

    public IReadOnlyList<CernealaDocumentHighlight> GetDocumentHighlights(
        CernealaSemanticModel model,
        IReadOnlyList<CernealaSemanticModel> workspaceModels,
        int offset,
        CancellationToken cancellationToken = default)
    {
        IReadOnlyList<CernealaLocation> references = GetReferences(
            model,
            workspaceModels,
            offset,
            includeDeclaration: true,
            cancellationToken);
        return references
            .Where(location => string.Equals(
                NormalizePath(location.Path),
                NormalizePath(model.Document.Path),
                StringComparison.OrdinalIgnoreCase))
            .Select(location => new CernealaDocumentHighlight(
                location.Span,
                GetHighlightKind(model, location.Span)))
            .GroupBy(highlight => highlight.Span)
            .Select(group => group.First())
            .OrderBy(highlight => highlight.Span.Start)
            .ToArray();
    }

    public CernealaPrepareRenameResult PrepareRename(
        CernealaSemanticModel model,
        IReadOnlyList<CernealaSemanticModel> workspaceModels,
        int offset)
    {
        CernealaSemanticSymbol? symbol = model.GetSymbolAt(offset);
        SymbolIdentity? identity = symbol is null ? null : CreateIdentity(model, symbol);
        if (symbol is null || identity is null || !IsRenameable(identity, workspaceModels))
        {
            return new CernealaPrepareRenameResult(null, null, "The symbol cannot be renamed safely.");
        }

        if (!TryGetIdentitySpan(model, symbol, identity, out TextSpan span))
        {
            return new CernealaPrepareRenameResult(null, null, "The markup token does not map exactly to the resolved symbol.");
        }

        return new CernealaPrepareRenameResult(span, identity.OldName, null);
    }

    public CernealaRenameResult Rename(
        CernealaSemanticModel model,
        IReadOnlyList<CernealaSemanticModel> workspaceModels,
        int offset,
        string newName,
        CancellationToken cancellationToken = default)
    {
        CernealaSemanticSymbol? symbol = model.GetSymbolAt(offset);
        SymbolIdentity? identity = symbol is null ? null : CreateIdentity(model, symbol);
        if (symbol is null || identity is null || !IsRenameable(identity, workspaceModels))
        {
            return Failure("The symbol cannot be renamed safely.");
        }

        if (!TryGetIdentitySpan(model, symbol, identity, out _))
        {
            return Failure("The markup token does not map exactly to the resolved symbol.");
        }

        if (!IsValidName(identity, newName))
        {
            return Failure("The requested name is not valid for this symbol.");
        }

        if (string.Equals(identity.OldName, newName, StringComparison.Ordinal))
        {
            return Failure("The requested name is identical to the current name.");
        }

        string? conflict = FindRenameConflict(identity, workspaceModels, newName);
        if (conflict is not null)
        {
            return Failure(conflict);
        }

        List<CernealaTextEdit> edits = new();
        foreach (CernealaSemanticModel candidateModel in workspaceModels.Distinct())
        {
            cancellationToken.ThrowIfCancellationRequested();
            foreach (CernealaSemanticSymbol candidate in candidateModel.Symbols)
            {
                if (!Matches(candidateModel, candidate, identity))
                {
                    continue;
                }

                if (!TryGetIdentitySpan(candidateModel, candidate, identity, out TextSpan span))
                {
                    return Failure("A resolved reference cannot be mapped exactly for rename.");
                }

                AddDistinct(edits, new CernealaTextEdit(candidateModel.Document.Path, span, newName));
            }
        }

        if (identity.Kind is IdentityKind.Type or IdentityKind.Member)
        {
            foreach (ILanguageCompilationSymbols compilation in workspaceModels
                .Select(candidate => candidate.NavigationCompilation)
                .Distinct())
            {
                foreach (LanguageReferenceLocation reference in compilation.FindReferences(
                    identity.TypeMetadataName!,
                    identity.MemberName,
                    identity.MemberKind,
                    cancellationToken))
                {
                    if (string.IsNullOrWhiteSpace(reference.Path) || IsGeneratedPath(reference.Path))
                    {
                        continue;
                    }

                    AddDistinct(edits, new CernealaTextEdit(reference.Path, reference.Span, newName));
                }
            }
        }

        return edits.Count == 0
            ? Failure("No exact references were available for rename.")
            : new CernealaRenameResult(
                edits.OrderBy(edit => edit.Path, StringComparer.OrdinalIgnoreCase)
                    .ThenByDescending(edit => edit.Span.Start)
                    .ToArray(),
                null);
    }

    private static CernealaRenameResult Failure(string error) =>
        new(Array.Empty<CernealaTextEdit>(), error);

    private static SymbolIdentity? CreateIdentity(CernealaSemanticModel model, CernealaSemanticSymbol symbol)
    {
        if (symbol.MemberSymbol is ILanguageMemberSymbol member)
        {
            return new SymbolIdentity(
                IdentityKind.Member,
                member.AssemblyName,
                member.DeclaringTypeMetadataName,
                member.Name,
                member.Kind,
                null,
                member.Name);
        }

        if (TryGetLocalDefinition(model, symbol, out CernealaLocation? definition))
        {
            return new SymbolIdentity(
                IdentityKind.Local,
                null,
                null,
                null,
                null,
                definition,
                GetLocalDeclarationName(model, definition!) ?? symbol.Name);
        }

        if (IsTypeSymbol(symbol.Kind) && symbol.TypeSymbol is ILanguageTypeSymbol type)
        {
            return new SymbolIdentity(
                IdentityKind.Type,
                type.AssemblyName,
                type.MetadataName,
                null,
                null,
                null,
                type.Name);
        }

        return null;
    }

    private static bool Matches(
        CernealaSemanticModel model,
        CernealaSemanticSymbol symbol,
        SymbolIdentity identity)
    {
        if (identity.Kind == IdentityKind.Local)
        {
            return TryGetLocalDefinition(model, symbol, out CernealaLocation? definition) &&
                SameLocation(definition!, identity.Definition!);
        }

        if (identity.Kind == IdentityKind.Member && symbol.MemberSymbol is ILanguageMemberSymbol member)
        {
            return member.Kind == identity.MemberKind &&
                string.Equals(member.Name, identity.MemberName, StringComparison.Ordinal) &&
                string.Equals(member.DeclaringTypeMetadataName, identity.TypeMetadataName, StringComparison.Ordinal) &&
                string.Equals(member.AssemblyName, identity.AssemblyName, StringComparison.Ordinal);
        }

        return identity.Kind == IdentityKind.Type && symbol.MemberSymbol is null &&
            IsTypeSymbol(symbol.Kind) && symbol.TypeSymbol is ILanguageTypeSymbol type &&
            string.Equals(type.MetadataName, identity.TypeMetadataName, StringComparison.Ordinal) &&
            string.Equals(type.AssemblyName, identity.AssemblyName, StringComparison.Ordinal);
    }

    private static bool TryGetLocalDefinition(
        CernealaSemanticModel model,
        CernealaSemanticSymbol symbol,
        out CernealaLocation? definition)
    {
        if (symbol.DefinitionLocation is LanguageSourceLocation location &&
            !string.IsNullOrWhiteSpace(location.Path) &&
            IsLocalKind(symbol.Kind))
        {
            definition = new CernealaLocation(location.Path, location.Span);
            return true;
        }

        if (symbol.Kind is CernealaSemanticSymbolKind.ContentTemplate or
            CernealaSemanticSymbolKind.MotionParameter or CernealaSemanticSymbolKind.MotionHandle)
        {
            definition = new CernealaLocation(model.Document.Path, symbol.Span);
            return true;
        }

        definition = null;
        return false;
    }

    private static bool IsLocalKind(CernealaSemanticSymbolKind kind) => kind is
        CernealaSemanticSymbolKind.Name or CernealaSemanticSymbolKind.TemplatePart or
        CernealaSemanticSymbolKind.Resource or CernealaSemanticSymbolKind.ResourceReference or
        CernealaSemanticSymbolKind.Aspect or CernealaSemanticSymbolKind.AspectApplication or
        CernealaSemanticSymbolKind.MotionSpec or CernealaSemanticSymbolKind.MotionComposition or
        CernealaSemanticSymbolKind.MotionParameter or CernealaSemanticSymbolKind.MotionHandle or
        CernealaSemanticSymbolKind.PrismComposition or CernealaSemanticSymbolKind.PrismNode or
        CernealaSemanticSymbolKind.PrismParameter or CernealaSemanticSymbolKind.PrismValue or
        CernealaSemanticSymbolKind.BindingSource;

    private static bool IsTypeSymbol(CernealaSemanticSymbolKind kind) => kind is
        CernealaSemanticSymbolKind.RootType or CernealaSemanticSymbolKind.Element or
        CernealaSemanticSymbolKind.TypeReference;

    private static bool IsRenameable(
        SymbolIdentity identity,
        IReadOnlyList<CernealaSemanticModel> workspaceModels)
    {
        if (identity.Kind is IdentityKind.Type or IdentityKind.Member)
        {
            return true;
        }

        return identity.Definition is not null && workspaceModels.Any(model => model.Symbols.Any(symbol =>
            IsLocalDeclaration(symbol.Kind) &&
            string.Equals(NormalizePath(model.Document.Path), NormalizePath(identity.Definition.Path), StringComparison.OrdinalIgnoreCase) &&
            symbol.Span.Equals(identity.Definition.Span)));
    }

    private static bool IsLocalDeclaration(CernealaSemanticSymbolKind kind) => kind is
        CernealaSemanticSymbolKind.Name or CernealaSemanticSymbolKind.TemplatePart or
        CernealaSemanticSymbolKind.Resource or CernealaSemanticSymbolKind.ContentTemplate or
        CernealaSemanticSymbolKind.Aspect or CernealaSemanticSymbolKind.MotionSpec or
        CernealaSemanticSymbolKind.MotionComposition or CernealaSemanticSymbolKind.MotionParameter or
        CernealaSemanticSymbolKind.MotionHandle or CernealaSemanticSymbolKind.PrismComposition or
        CernealaSemanticSymbolKind.PrismNode or CernealaSemanticSymbolKind.PrismParameter;

    private static bool IsDeclaration(
        CernealaSemanticModel model,
        CernealaSemanticSymbol symbol,
        SymbolIdentity identity)
    {
        if (identity.Kind != IdentityKind.Local || !IsLocalDeclaration(symbol.Kind))
        {
            return false;
        }

        return string.Equals(NormalizePath(model.Document.Path), NormalizePath(identity.Definition!.Path), StringComparison.OrdinalIgnoreCase) &&
            symbol.Span.Equals(identity.Definition.Span);
    }

    private static string? GetLocalDeclarationName(CernealaSemanticModel queryModel, CernealaLocation definition)
    {
        if (string.Equals(NormalizePath(queryModel.Document.Path), NormalizePath(definition.Path), StringComparison.OrdinalIgnoreCase))
        {
            return queryModel.Symbols.FirstOrDefault(symbol =>
                IsLocalDeclaration(symbol.Kind) && symbol.Span.Equals(definition.Span))?.Name;
        }

        return null;
    }

    private static bool TryGetIdentitySpan(
        CernealaSemanticModel model,
        CernealaSemanticSymbol symbol,
        SymbolIdentity identity,
        out TextSpan span)
    {
        string text = model.Document.Text.Substring(symbol.Span);
        if (string.Equals(text, identity.OldName, StringComparison.Ordinal))
        {
            span = symbol.Span;
            return true;
        }

        if ((text.EndsWith(":" + identity.OldName, StringComparison.Ordinal) ||
            text.EndsWith("." + identity.OldName, StringComparison.Ordinal)) &&
            identity.OldName.Length <= symbol.Span.Length)
        {
            span = new TextSpan(symbol.Span.End - identity.OldName.Length, identity.OldName.Length);
            return true;
        }

        span = default;
        return false;
    }

    private static bool IsValidName(SymbolIdentity identity, string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        if (identity.Kind is IdentityKind.Type or IdentityKind.Member)
        {
            return SyntaxFacts.IsValidIdentifier(value);
        }

        return (char.IsLetter(value[0]) || value[0] == '_') &&
            value.Skip(1).All(character => char.IsLetterOrDigit(character) || character == '_');
    }

    private static string? FindRenameConflict(
        SymbolIdentity identity,
        IReadOnlyList<CernealaSemanticModel> workspaceModels,
        string newName)
    {
        if (identity.Kind == IdentityKind.Local)
        {
            bool duplicateSource = workspaceModels.Any(model =>
                string.Equals(NormalizePath(model.Document.Path), NormalizePath(identity.Definition!.Path), StringComparison.OrdinalIgnoreCase) &&
                model.Diagnostics.Any(diagnostic =>
                    diagnostic.Message.IndexOf("Duplicate", StringComparison.OrdinalIgnoreCase) >= 0 &&
                    diagnostic.Message.IndexOf(identity.OldName, StringComparison.Ordinal) >= 0));
            if (duplicateSource)
            {
                return "Rename is ambiguous because the declaration scope already contains duplicates.";
            }

            bool collision = workspaceModels.Any(model => model.Symbols.Any(symbol =>
                IsLocalDeclaration(symbol.Kind) &&
                string.Equals(symbol.Name, newName, StringComparison.Ordinal) &&
                (!string.Equals(NormalizePath(model.Document.Path), NormalizePath(identity.Definition!.Path), StringComparison.OrdinalIgnoreCase) ||
                    !symbol.Span.Equals(identity.Definition.Span))));
            return collision ? "The requested name already exists in a declarative scope." : null;
        }

        foreach (ILanguageCompilationSymbols compilation in workspaceModels
            .Select(model => model.NavigationCompilation)
            .Distinct())
        {
            if (identity.Kind == IdentityKind.Type)
            {
                ILanguageTypeSymbol? original = compilation.FindType(identity.TypeMetadataName!);
                if (original is not null && compilation.FindTypes(newName).Any(candidate =>
                    string.Equals(candidate.Namespace, original.Namespace, StringComparison.Ordinal) &&
                    !string.Equals(candidate.MetadataName, original.MetadataName, StringComparison.Ordinal)))
                {
                    return "A type with the requested name already exists in the same namespace.";
                }
            }
            else if (compilation.FindType(identity.TypeMetadataName!) is ILanguageTypeSymbol declaringType &&
                declaringType.GetMembers(newName).Any())
            {
                return "A member with the requested name already exists on the declaring type.";
            }
        }

        return null;
    }

    private static CernealaDocumentHighlightKind GetHighlightKind(CernealaSemanticModel model, TextSpan span)
    {
        CernealaSemanticSymbol? symbol = model.Symbols.FirstOrDefault(candidate => candidate.Span.Equals(span));
        if (symbol is null || IsLocalDeclaration(symbol.Kind))
        {
            return CernealaDocumentHighlightKind.Text;
        }

        return symbol.Kind is CernealaSemanticSymbolKind.Property or
            CernealaSemanticSymbolKind.AttachedProperty or CernealaSemanticSymbolKind.AspectAssignment or
            CernealaSemanticSymbolKind.MotionProperty or CernealaSemanticSymbolKind.PrismProperty
            ? CernealaDocumentHighlightKind.Write
            : CernealaDocumentHighlightKind.Read;
    }

    private static IReadOnlyList<LanguageSourceLocation> PreferUserLocations(
        IReadOnlyList<LanguageSourceLocation> locations)
    {
        LanguageSourceLocation[] valid = locations
            .Where(location => !string.IsNullOrWhiteSpace(location.Path))
            .ToArray();
        LanguageSourceLocation[] authored = valid.Where(location => !IsGeneratedPath(location.Path)).ToArray();
        return authored.Length > 0 ? authored : valid;
    }

    private static bool IsGeneratedPath(string path)
    {
        string normalized = NormalizePath(path);
        return normalized.EndsWith(".g.cs", StringComparison.OrdinalIgnoreCase) ||
            normalized.EndsWith(".g.i.cs", StringComparison.OrdinalIgnoreCase) ||
            normalized.IndexOf("/obj/", StringComparison.OrdinalIgnoreCase) >= 0 ||
            normalized.IndexOf("/generated/", StringComparison.OrdinalIgnoreCase) >= 0;
    }

    private static string ExplainDiagnostic(LanguageDiagnostic diagnostic)
    {
        if (diagnostic.Id.StartsWith("PRISM", StringComparison.Ordinal))
        {
            return diagnostic.Id + " identifies a Prism catalog, scope, or composition constraint at this token.";
        }

        if (diagnostic.Id is "CERNEALAUI007" or "CERNEALAUI012")
        {
            return diagnostic.Id + " identifies a typed binding or template-scope constraint at this token.";
        }

        if (diagnostic.Id.StartsWith("CERNEALAUI02", StringComparison.Ordinal))
        {
            return diagnostic.Id + " identifies a Motion semantic or lifecycle constraint at this token.";
        }

        return diagnostic.Id + " identifies a Cerneala markup contract that could not be proven at this token.";
    }

    private static string DisplayKind(CernealaSemanticSymbolKind kind) => kind.ToString();

    private static bool SameLocation(CernealaLocation left, CernealaLocation right) =>
        string.Equals(NormalizePath(left.Path), NormalizePath(right.Path), StringComparison.OrdinalIgnoreCase) &&
        left.Span.Equals(right.Span);

    private static void AddDistinct(ICollection<CernealaLocation> result, CernealaLocation location)
    {
        if (!result.Any(existing => SameLocation(existing, location)))
        {
            result.Add(location);
        }
    }

    private static void AddDistinct(ICollection<CernealaTextEdit> result, CernealaTextEdit edit)
    {
        if (!result.Any(existing =>
            string.Equals(NormalizePath(existing.Path), NormalizePath(edit.Path), StringComparison.OrdinalIgnoreCase) &&
            existing.Span.Equals(edit.Span)))
        {
            result.Add(edit);
        }
    }

    private static string NormalizePath(string path) => (path ?? string.Empty).Replace('\\', '/');

    private enum IdentityKind
    {
        Type,
        Member,
        Local
    }

    private sealed class SymbolIdentity
    {
        public SymbolIdentity(
            IdentityKind kind,
            string? assemblyName,
            string? typeMetadataName,
            string? memberName,
            LanguageMemberKind? memberKind,
            CernealaLocation? definition,
            string oldName)
        {
            Kind = kind;
            AssemblyName = assemblyName;
            TypeMetadataName = typeMetadataName;
            MemberName = memberName;
            MemberKind = memberKind;
            Definition = definition;
            OldName = oldName;
        }

        public IdentityKind Kind { get; }

        public string? AssemblyName { get; }

        public string? TypeMetadataName { get; }

        public string? MemberName { get; }

        public LanguageMemberKind? MemberKind { get; }

        public CernealaLocation? Definition { get; }

        public string OldName { get; }
    }
}
