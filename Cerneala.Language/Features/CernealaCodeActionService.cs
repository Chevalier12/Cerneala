using System.Text;
using Cerneala.Language.Diagnostics;
using Cerneala.Language.Semantics;
using Cerneala.Language.Semantics.Symbols;
using Cerneala.Language.Syntax;
using Cerneala.Language.Text;

namespace Cerneala.Language.Features;

internal sealed class CernealaCodeActionService
{
    public IReadOnlyList<CernealaCodeAction> GetCodeActions(
        CernealaDocument document,
        CernealaSemanticModel? model,
        TextSpan range,
        IReadOnlyList<CernealaCodeActionDiagnostic> requestedDiagnostics,
        IReadOnlyList<CernealaAdditionalDocument> additionalDocuments,
        bool includeFixAll,
        CancellationToken cancellationToken = default)
    {
        List<CernealaCodeAction> actions = new();
        AddClosingDelimiterActions(document, model, range, actions, cancellationToken);
        if (model is not null)
        {
            AddNamespaceAndTypoActions(model, range, actions, cancellationToken);
            AddEventHandlerActions(model, range, requestedDiagnostics, additionalDocuments, actions);
            AddPropertyElementConversions(model, range, actions, cancellationToken);
        }

        CernealaCodeAction[] distinct = actions
            .GroupBy(action => ActionKey(action), StringComparer.Ordinal)
            .Select(group => group.First())
            .OrderByDescending(action => action.IsPreferred)
            .ThenBy(action => action.Title, StringComparer.Ordinal)
            .ToArray();
        if (!includeFixAll)
        {
            return distinct;
        }

        CernealaCodeAction? fixAll = CreateFixAll(distinct);
        return fixAll is null ? distinct : distinct.Append(fixAll).ToArray();
    }

    private static void AddClosingDelimiterActions(
        CernealaDocument document,
        CernealaSemanticModel? model,
        TextSpan range,
        ICollection<CernealaCodeAction> destination,
        CancellationToken cancellationToken)
    {
        foreach (ElementSyntax element in document.Syntax.DescendantElements().Where(element =>
            !element.IsSelfClosing && element.CloseNameToken.IsMissing && Intersects(element.Span, range)))
        {
            cancellationToken.ThrowIfCancellationRequested();
            CernealaTextEdit edit = new(
                document.Path,
                new TextSpan(element.Span.End, 0),
                "</" + element.Name + ">");
            CernealaCodeAction action = new(
                "Add closing tag </" + element.Name + ">",
                "quickfix",
                true,
                ["CERNEALAUI001"],
                [edit]);
            destination.Add(action);
        }

        foreach (ElementSyntax element in document.Syntax.DescendantElements().Where(element =>
            !element.IsSelfClosing && !element.CloseNameToken.IsMissing &&
            element.CloseGreaterThanToken.IsMissing && Intersects(element.Span, range)))
        {
            CernealaCodeAction action = new(
                "Add missing > delimiter",
                "quickfix",
                true,
                ["CERNEALAUI001"],
                [new CernealaTextEdit(document.Path, new TextSpan(element.CloseGreaterThanToken.Span.Start, 0), ">")]);
            destination.Add(action);
        }
    }

    private static void AddNamespaceAndTypoActions(
        CernealaSemanticModel model,
        TextSpan range,
        ICollection<CernealaCodeAction> destination,
        CancellationToken cancellationToken)
    {
        CernealaDocument document = model.Document;
        foreach (LanguageDiagnostic diagnostic in model.Diagnostics.Where(diagnostic =>
            (diagnostic.Id is "CERNEALAUI002" or "CERNEALAUI003") &&
            Intersects(diagnostic.Span, range)))
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (diagnostic.Id == "CERNEALAUI002")
            {
                ElementSyntax? element = document.Syntax.DescendantElements().FirstOrDefault(candidate =>
                    candidate.NameToken.Span.Equals(diagnostic.Span));
                if (element is null)
                {
                    continue;
                }

                int separator = element.Name.IndexOf(':');
                if (separator > 0)
                {
                    AddNamespaceAliasAction(model, element, separator, destination, cancellationToken);
                }
                else
                {
                    AddElementTypoAction(model, element, destination, cancellationToken);
                }

                continue;
            }

            AddPropertyTypoAction(model, diagnostic, destination, cancellationToken);
        }
    }

    private static void AddNamespaceAliasAction(
        CernealaSemanticModel model,
        ElementSyntax element,
        int separator,
        ICollection<CernealaCodeAction> destination,
        CancellationToken cancellationToken)
    {
        string prefix = element.Name.Substring(0, separator);
        string localName = element.Name.Substring(separator + 1);
        ElementSyntax? root = model.Document.Syntax.Children.OfType<ElementSyntax>().SingleOrDefault();
        if (root is null || root.Attributes.Any(attribute => attribute.NameToken.Text == "xmlns:" + prefix))
        {
            return;
        }

        ILanguageTypeSymbol[] candidates = model.Compilation.FindTypes(localName)
            .Where(IsUsableElementType)
            .OrderBy(type => type.MetadataName, StringComparer.Ordinal)
            .ToArray();
        if (candidates.Length != 1)
        {
            return;
        }

        ILanguageTypeSymbol type = candidates[0];
        string specification = "clr-namespace:" + type.Namespace + ";assembly=" + type.AssemblyName;
        CernealaCodeAction action = new(
            "Add xmlns:" + prefix + " for " + type.MetadataName,
            "quickfix",
            true,
            ["CERNEALAUI002"],
            [new CernealaTextEdit(
                model.Document.Path,
                new TextSpan(root.OpenEndToken.Span.Start, 0),
                " xmlns:" + prefix + "=\"" + specification + "\"")]);
        destination.Add(action);
    }

    private static void AddElementTypoAction(
        CernealaSemanticModel model,
        ElementSyntax element,
        ICollection<CernealaCodeAction> destination,
        CancellationToken cancellationToken)
    {
        string? replacement = UniqueClosest(
            element.Name,
            model.Compilation.GetTypes().Where(IsUsableElementType).Select(type => type.Name));
        if (replacement is null)
        {
            return;
        }

        List<CernealaTextEdit> edits =
        [
            new CernealaTextEdit(model.Document.Path, element.NameToken.Span, replacement)
        ];
        if (!element.CloseNameToken.IsMissing)
        {
            edits.Add(new CernealaTextEdit(model.Document.Path, element.CloseNameToken.Span, replacement));
        }

        CernealaCodeAction action = new(
            "Change element to " + replacement,
            "quickfix",
            true,
            ["CERNEALAUI002"],
            edits);
        destination.Add(action);
    }

    private static void AddPropertyTypoAction(
        CernealaSemanticModel model,
        LanguageDiagnostic diagnostic,
        ICollection<CernealaCodeAction> destination,
        CancellationToken cancellationToken)
    {
        ElementSyntax? owner = model.Document.Syntax.DescendantElements().FirstOrDefault(element =>
            element.Attributes.Any(attribute => attribute.NameToken.Span.Equals(diagnostic.Span)));
        AttributeSyntax? attribute = owner?.Attributes.FirstOrDefault(candidate =>
            candidate.NameToken.Span.Equals(diagnostic.Span));
        ILanguageTypeSymbol? ownerType = owner is null
            ? null
            : model.Symbols.FirstOrDefault(symbol =>
                symbol.Span.Equals(owner.NameToken.Span) &&
                symbol.Kind is CernealaSemanticSymbolKind.Element or CernealaSemanticSymbolKind.RootType)?.TypeSymbol;
        if (attribute is null || ownerType is null)
        {
            return;
        }

        string? replacement = UniqueClosest(
            attribute.NameToken.Text,
            ownerType.GetMembers().Where(member =>
                member.Kind is LanguageMemberKind.Property or LanguageMemberKind.Event)
                .Select(member => member.Name));
        if (replacement is null)
        {
            return;
        }

        CernealaCodeAction action = new(
            "Change property to " + replacement,
            "quickfix",
            true,
            [diagnostic.Id],
            [new CernealaTextEdit(model.Document.Path, attribute.NameToken.Span, replacement)]);
        destination.Add(action);
    }

    private static void AddEventHandlerActions(
        CernealaSemanticModel model,
        TextSpan range,
        IReadOnlyList<CernealaCodeActionDiagnostic> requestedDiagnostics,
        IReadOnlyList<CernealaAdditionalDocument> additionalDocuments,
        ICollection<CernealaCodeAction> destination)
    {
        bool requested = requestedDiagnostics.Any(diagnostic =>
            diagnostic.Id == "CERNEALAUI009" && Intersects(diagnostic.Span, range));
        CernealaSemanticSymbol? root = model.Symbols.FirstOrDefault(symbol =>
            symbol.Kind == CernealaSemanticSymbolKind.RootType);
        if (root?.TypeSymbol is null)
        {
            return;
        }

        Dictionary<string, SourceText> sources = additionalDocuments.ToDictionary(
            document => Path.GetFullPath(document.Path),
            document => document.Text,
            StringComparer.OrdinalIgnoreCase);
        foreach (CernealaSemanticSymbol symbol in model.Symbols.Where(symbol =>
            symbol.Kind == CernealaSemanticSymbolKind.Event &&
            (requested || Intersects(symbol.Span, range))))
        {
            string? handler = symbol.Value as string;
            if (handler is null || string.IsNullOrWhiteSpace(handler) || !IsIdentifier(handler) ||
                root.TypeSymbol.GetMembers(handler).Any(member => member.Kind == LanguageMemberKind.Method))
            {
                continue;
            }

            LanguageSourceLocation location = root.TypeSymbol.Locations.FirstOrDefault(candidate =>
                sources.ContainsKey(Path.GetFullPath(candidate.Path)));
            if (string.IsNullOrEmpty(location.Path) ||
                !sources.TryGetValue(Path.GetFullPath(location.Path), out SourceText? source) ||
                !TryFindTypeClosingBrace(source.ToString(), location.Span, out int insertion))
            {
                continue;
            }

            ILanguageMemberSymbol? invoke = symbol.MemberSymbol?.ValueType?.GetMembers("Invoke")
                .FirstOrDefault(member => member.Kind == LanguageMemberKind.Method);
            string parameters = string.Join(", ", (invoke?.Parameters ?? [])
                .Select(parameter => FormatType(parameter.TypeMetadataName) + " " + parameter.Name));
            string newline = source.ToString().Contains("\r\n", StringComparison.Ordinal) ? "\r\n" : "\n";
            string indent = LineIndent(source.ToString(), insertion);
            string memberIndent = indent + "    ";
            string bodyIndent = memberIndent + "    ";
            string method = newline + memberIndent + "private void " + handler + "(" + parameters + ")" + newline +
                memberIndent + "{" + newline + bodyIndent + newline + memberIndent + "}" + newline + indent;
            destination.Add(new CernealaCodeAction(
                "Create event handler " + handler,
                "quickfix",
                true,
                ["CERNEALAUI009"],
                [new CernealaTextEdit(location.Path, new TextSpan(insertion, 0), method)]));
        }
    }

    private static void AddPropertyElementConversions(
        CernealaSemanticModel model,
        TextSpan range,
        ICollection<CernealaCodeAction> destination,
        CancellationToken cancellationToken)
    {
        foreach (ElementSyntax propertyElement in model.Document.Syntax.DescendantElements().Where(element =>
            element.Kind == SyntaxKind.PropertyElement && Intersects(element.Span, range)))
        {
            ElementSyntax? owner = FindParent(model.Document.Syntax, propertyElement);
            TextSyntax[] values = propertyElement.Children.OfType<TextSyntax>()
                .Where(text => !string.IsNullOrWhiteSpace(text.Token.Text))
                .ToArray();
            if (owner is null || values.Length != 1 || propertyElement.Children.OfType<ElementSyntax>().Any())
            {
                continue;
            }

            CernealaSemanticSymbol? property = model.Symbols.FirstOrDefault(symbol =>
                symbol.Kind == CernealaSemanticSymbolKind.PropertyElement &&
                symbol.Span.Equals(propertyElement.NameToken.Span) && symbol.MemberSymbol is not null);
            if (property?.MemberSymbol is null)
            {
                continue;
            }

            string value = values[0].Token.Text.Trim();
            if (value.Length == 0 || value.Contains('<') || value.Contains('>'))
            {
                continue;
            }

            string attribute = " " + property.Name + "=\"" + EscapeAttribute(value) + "\"";
            CernealaTextEdit[] edits =
            [
                new CernealaTextEdit(model.Document.Path, new TextSpan(owner.OpenEndToken.Span.Start, 0), attribute),
                new CernealaTextEdit(model.Document.Path, propertyElement.Span, string.Empty)
            ];
            if (!HasOverlaps(edits) && PreservesOrImprovesDiagnostics(model, edits, cancellationToken))
            {
                destination.Add(new CernealaCodeAction(
                    "Convert " + propertyElement.Name + " to an attribute",
                    "refactor.rewrite",
                    false,
                    [],
                    edits));
            }
        }
    }

    private static CernealaCodeAction? CreateFixAll(IReadOnlyList<CernealaCodeAction> actions)
    {
        CernealaCodeAction[] fixes = actions.Where(action =>
            action.Kind == "quickfix" && action.IsPreferred && action.DiagnosticIds.Count > 0)
            .ToArray();
        if (fixes.Length < 2)
        {
            return null;
        }

        CernealaTextEdit[] edits = fixes.SelectMany(action => action.Edits)
            .GroupBy(edit => (Path.GetFullPath(edit.Path), edit.Span, edit.NewText))
            .Select(group => group.First())
            .ToArray();
        if (HasOverlaps(edits))
        {
            return null;
        }

        return new CernealaCodeAction(
            "Fix all independent Cerneala diagnostics",
            "source.fixAll.cerneala",
            false,
            fixes.SelectMany(action => action.DiagnosticIds).Distinct(StringComparer.Ordinal).ToArray(),
            edits);
    }

    private static bool PreservesOrImprovesDiagnostics(
        CernealaSemanticModel model,
        IReadOnlyList<CernealaTextEdit> edits,
        CancellationToken cancellationToken)
    {
        using CernealaSemanticModel updated = Rebind(model, edits, cancellationToken);
        return updated.Diagnostics.Count <= model.Diagnostics.Count &&
            !updated.Diagnostics.Any(diagnostic => diagnostic.Id == "CERNEALAUI001");
    }

    private static CernealaSemanticModel Rebind(
        CernealaSemanticModel model,
        IReadOnlyList<CernealaTextEdit> edits,
        CancellationToken cancellationToken)
    {
        string text = model.Document.Text.ToString();
        foreach (CernealaTextEdit edit in edits.Where(edit =>
            string.Equals(edit.Path, model.Document.Path, StringComparison.OrdinalIgnoreCase))
            .OrderByDescending(edit => edit.Span.Start))
        {
            text = text.Substring(0, edit.Span.Start) + edit.NewText + text.Substring(edit.Span.End);
        }

        CernealaDocument document = new(model.Document.Path, SourceText.From(text, model.Document.Version + 1));
        CernealaDocument[] documents = model.Documents
            .Where(candidate => !string.Equals(candidate.Path, document.Path, StringComparison.OrdinalIgnoreCase))
            .Append(document)
            .ToArray();
        return new CernealaSemanticModel(
            document,
            documents,
            model.Compilation,
            AnalysisMode.Editor,
            cancellationToken);
    }

    private static ElementSyntax? FindParent(DocumentSyntax document, ElementSyntax child) =>
        document.DescendantElements().FirstOrDefault(candidate => candidate.Children.Contains(child));

    private static string? UniqueClosest(string value, IEnumerable<string> candidates)
    {
        (string Name, int Distance)[] ranked = candidates.Distinct(StringComparer.Ordinal)
            .Where(candidate => !string.Equals(candidate, value, StringComparison.Ordinal))
            .Select(candidate => (candidate, Levenshtein(value, candidate)))
            .OrderBy(candidate => candidate.Item2)
            .ThenBy(candidate => candidate.candidate, StringComparer.Ordinal)
            .Select(candidate => (candidate.candidate, candidate.Item2))
            .ToArray();
        return ranked.Length > 0 && ranked[0].Distance <= 2 &&
            (ranked.Length == 1 || ranked[1].Distance > ranked[0].Distance)
            ? ranked[0].Name
            : null;
    }

    private static int Levenshtein(string left, string right)
    {
        int[] previous = Enumerable.Range(0, right.Length + 1).ToArray();
        int[] current = new int[right.Length + 1];
        for (int leftIndex = 1; leftIndex <= left.Length; leftIndex++)
        {
            current[0] = leftIndex;
            for (int rightIndex = 1; rightIndex <= right.Length; rightIndex++)
            {
                int substitution = char.ToUpperInvariant(left[leftIndex - 1]) == char.ToUpperInvariant(right[rightIndex - 1])
                    ? 0
                    : 1;
                current[rightIndex] = Math.Min(
                    Math.Min(current[rightIndex - 1] + 1, previous[rightIndex] + 1),
                    previous[rightIndex - 1] + substitution);
            }

            (previous, current) = (current, previous);
        }

        return previous[right.Length];
    }

    private static bool IsUsableElementType(ILanguageTypeSymbol type) =>
        type.IsClass && !type.IsAbstract && type.HasAccessibleParameterlessConstructor &&
        type.IsOrDerivesFrom("Cerneala.UI.Elements.UIElement");

    private static bool HasOverlaps(IReadOnlyList<CernealaTextEdit> edits)
    {
        foreach (IGrouping<string, CernealaTextEdit> group in edits.GroupBy(
            edit => Path.GetFullPath(edit.Path),
            StringComparer.OrdinalIgnoreCase))
        {
            CernealaTextEdit[] ordered = group.OrderBy(edit => edit.Span.Start).ThenBy(edit => edit.Span.Length).ToArray();
            for (int index = 1; index < ordered.Length; index++)
            {
                TextSpan previous = ordered[index - 1].Span;
                TextSpan current = ordered[index].Span;
                if (previous.End > current.Start || previous.Length == 0 && current.Length == 0 &&
                    previous.Start == current.Start)
                {
                    return true;
                }
            }
        }

        return false;
    }

    private static bool TryFindTypeClosingBrace(string text, TextSpan typeName, out int closingBrace)
    {
        int opening = text.IndexOf('{', typeName.End);
        if (opening < 0)
        {
            closingBrace = 0;
            return false;
        }

        int depth = 0;
        for (int index = opening; index < text.Length; index++)
        {
            if (text[index] == '{')
            {
                depth++;
            }
            else if (text[index] == '}' && --depth == 0)
            {
                closingBrace = index;
                return true;
            }
        }

        closingBrace = 0;
        return false;
    }

    private static string LineIndent(string text, int offset)
    {
        int start = offset;
        while (start > 0 && text[start - 1] is not ('\r' or '\n'))
        {
            start--;
        }

        int end = start;
        while (end < offset && text[end] is ' ' or '\t')
        {
            end++;
        }

        return text.Substring(start, end - start);
    }

    private static string FormatType(string metadataName) => metadataName
        .Replace('+', '.')
        .Replace("?", string.Empty);

    private static string EscapeAttribute(string value) => value
        .Replace("&", "&amp;")
        .Replace("\"", "&quot;")
        .Replace("<", "&lt;")
        .Replace(">", "&gt;");

    private static bool IsIdentifier(string value) => value.Length > 0 &&
        (char.IsLetter(value[0]) || value[0] == '_') &&
        value.Skip(1).All(character => char.IsLetterOrDigit(character) || character == '_');

    private static bool Intersects(TextSpan left, TextSpan right) =>
        left.Start <= right.End && right.Start <= left.End;

    private static string ActionKey(CernealaCodeAction action) => action.Title + "|" + string.Join(
        ";",
        action.Edits.OrderBy(edit => edit.Path, StringComparer.OrdinalIgnoreCase)
            .ThenBy(edit => edit.Span.Start)
            .Select(edit => edit.Path + ":" + edit.Span.Start + ":" + edit.Span.Length + ":" + edit.NewText));
}
