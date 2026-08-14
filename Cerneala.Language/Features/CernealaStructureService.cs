using Cerneala.Language.Semantics;
using Cerneala.Language.Syntax;
using Cerneala.Language.Syntax.Embedded;
using Cerneala.Language.Text;

namespace Cerneala.Language.Features;

internal sealed class CernealaStructureService
{
    public IReadOnlyList<CernealaSemanticToken> GetSemanticTokens(
        CernealaDocument document,
        CernealaSemanticModel? model,
        CancellationToken cancellationToken = default)
    {
        List<TokenCandidate> candidates = new();
        foreach (ElementSyntax element in document.Syntax.DescendantElements())
        {
            cancellationToken.ThrowIfCancellationRequested();
            CernealaSemanticTokenKind elementKind = element.Kind == SyntaxKind.PropertyElement
                ? CernealaSemanticTokenKind.Property
                : CernealaSemanticTokenKind.ElementType;
            AddCandidate(candidates, element.NameToken.Span, elementKind, 10);
            if (!element.CloseNameToken.IsMissing)
            {
                AddCandidate(candidates, element.CloseNameToken.Span, elementKind, 10);
            }

            foreach (AttributeSyntax attribute in element.Attributes.Where(attribute =>
                attribute.NameToken.Text == "xmlns" ||
                attribute.NameToken.Text.StartsWith("xmlns:", StringComparison.Ordinal)))
            {
                AddCandidate(candidates, attribute.NameToken.Span, CernealaSemanticTokenKind.Namespace, 80);
            }

            foreach (AttributeSyntax attribute in element.Attributes)
            {
                TextSpan content = AttributeContentSpan(attribute);
                if (content.Length == 0)
                {
                    continue;
                }

                EmbeddedParseResult<BindingValueSyntax> binding = BindingSyntaxParser.Parse(
                    document.Text.Substring(content),
                    content.Start);
                IEnumerable<BindingPathSyntax> paths = binding.Syntax.Binding is null
                    ? binding.Syntax.Fragments.OfType<BindingFragmentSyntax>().Select(fragment => fragment.Binding)
                    : [binding.Syntax.Binding];
                foreach (BindingPathSyntax path in paths)
                {
                    foreach ((BindingPathSegmentSyntax segment, int index) in path.Segments.Select(
                        (segment, index) => (segment, index)))
                    {
                        AddCandidate(
                            candidates,
                            segment.Span,
                            index == 0
                                ? CernealaSemanticTokenKind.BindingSource
                                : CernealaSemanticTokenKind.BindingMember,
                            90);
                    }
                }
            }
        }

        foreach (TextSyntax text in DescendantNodes(document.Syntax).OfType<TextSyntax>().Where(text =>
            text.Token.Text.IndexOf('@') >= 0))
        {
            EmbeddedParseResult<DirectiveDocumentSyntax> parsed = DirectiveSyntaxParser.Parse(
                text.Token.Text,
                text.Token.Span.Start);
            foreach (DirectiveSyntax directive in parsed.Syntax.Directives)
            {
                AddCandidate(candidates, directive.Span, CernealaSemanticTokenKind.Directive, 90);
            }
        }

        if (model is not null)
        {
            foreach (CernealaSemanticSymbol symbol in model.Symbols)
            {
                cancellationToken.ThrowIfCancellationRequested();
                if (TryMapToken(symbol.Kind, out CernealaSemanticTokenKind kind, out int priority))
                {
                    AddCandidate(
                        candidates,
                        symbol.Span,
                        kind,
                        priority,
                        IsDeclaration(symbol.Kind)
                            ? CernealaSemanticTokenModifiers.Declaration
                            : CernealaSemanticTokenModifiers.None);
                }
            }
        }

        List<TokenCandidate> selected = new();
        foreach (TokenCandidate candidate in candidates
            .Where(candidate => candidate.Span.Length > 0)
            .OrderByDescending(candidate => candidate.Priority)
            .ThenBy(candidate => candidate.Span.Length)
            .ThenBy(candidate => candidate.Span.Start))
        {
            if (!selected.Any(existing => Overlaps(existing.Span, candidate.Span)))
            {
                selected.Add(candidate);
            }
        }

        return selected.OrderBy(candidate => candidate.Span.Start)
            .ThenBy(candidate => candidate.Span.Length)
            .Select(candidate => new CernealaSemanticToken(
                candidate.Span,
                candidate.Kind,
                candidate.Modifiers))
            .ToArray();
    }

    public IReadOnlyList<CernealaOutlineSymbol> GetDocumentSymbols(
        CernealaDocument document,
        CernealaSemanticModel? model,
        CancellationToken cancellationToken = default)
    {
        ElementSyntax? root = document.Syntax.Children.OfType<ElementSyntax>().FirstOrDefault();
        return root is null
            ? Array.Empty<CernealaOutlineSymbol>()
            : BuildOutline(root, parentIsResources: false, isRoot: true, model, cancellationToken);
    }

    public IReadOnlyList<CernealaWorkspaceSymbol> GetWorkspaceSymbols(
        IReadOnlyList<CernealaSemanticModel> models,
        string query,
        CancellationToken cancellationToken = default)
    {
        List<CernealaWorkspaceSymbol> result = new();
        foreach (CernealaSemanticModel model in models)
        {
            foreach (IGrouping<TextSpan, CernealaSemanticSymbol> group in model.Symbols
                .Where(symbol => IsWorkspaceDeclaration(symbol.Kind))
                .GroupBy(symbol => symbol.Span))
            {
                cancellationToken.ThrowIfCancellationRequested();
                CernealaSemanticSymbol symbol = group
                    .OrderByDescending(candidate => OutlinePriority(candidate.Kind))
                    .First();
                if (query.Length > 0 &&
                    symbol.Name.IndexOf(query, StringComparison.OrdinalIgnoreCase) < 0)
                {
                    continue;
                }

                CernealaWorkspaceSymbol candidate = new(
                    symbol.Name,
                    symbol.Kind.ToString(),
                    MapOutlineKind(symbol.Kind),
                    model.Document.Path,
                    symbol.Span);
                if (!result.Any(existing =>
                    string.Equals(existing.Path, candidate.Path, StringComparison.OrdinalIgnoreCase) &&
                    existing.Span.Equals(candidate.Span) &&
                    string.Equals(existing.Name, candidate.Name, StringComparison.Ordinal)))
                {
                    result.Add(candidate);
                }
            }
        }

        return result.OrderBy(symbol => symbol.Name, StringComparer.OrdinalIgnoreCase)
            .ThenBy(symbol => symbol.Path, StringComparer.OrdinalIgnoreCase)
            .ThenBy(symbol => symbol.Span.Start)
            .ToArray();
    }

    public IReadOnlyList<CernealaFoldingRange> GetFoldingRanges(
        CernealaDocument document,
        CancellationToken cancellationToken = default)
    {
        List<CernealaFoldingRange> result = new();
        foreach (SyntaxNode child in document.Syntax.Children)
        {
            AddFoldingRanges(child, document, result, cancellationToken);
        }

        return result.Where(range => IsMultiline(document.Text, range.Span))
            .GroupBy(range => range.Span)
            .Select(group => group.OrderByDescending(range => range.Kind is not null).First())
            .OrderBy(range => range.Span.Start)
            .ThenByDescending(range => range.Span.Length)
            .ToArray();
    }

    public CernealaSelectionRange GetSelectionRange(
        CernealaDocument document,
        CernealaSemanticModel? model,
        int offset)
    {
        if (offset < 0 || offset > document.Text.Length)
        {
            throw new ArgumentOutOfRangeException(nameof(offset));
        }

        List<TextSpan> candidates = new();
        if (model is not null)
        {
            candidates.AddRange(model.Symbols
                .Where(symbol => ContainsOffset(symbol.Span, offset))
                .Select(symbol => symbol.Span));
        }

        SyntaxToken? token = document.Syntax.Tokens
            .Where(candidate => !candidate.IsMissing &&
                candidate.Kind is not SyntaxKind.WhitespaceToken and not SyntaxKind.EndOfFileToken &&
                ContainsOffset(candidate.Span, offset))
            .OrderBy(candidate => candidate.Span.Length)
            .FirstOrDefault();
        if (token is not null)
        {
            candidates.Add(token.Span);
        }

        foreach (ElementSyntax element in document.Syntax.DescendantElements().Where(element =>
            ContainsOffset(element.Span, offset)))
        {
            foreach (AttributeSyntax attribute in element.Attributes.Where(attribute =>
                ContainsOffset(attribute.Span, offset)))
            {
                TextSpan content = AttributeContentSpan(attribute);
                if (ContainsOffset(content, offset))
                {
                    candidates.Add(content);
                }

                candidates.Add(attribute.Span);
            }

            candidates.Add(element.Span);
        }

        candidates.Add(document.Syntax.Span);
        TextSpan[] ordered = candidates.Distinct()
            .OrderBy(span => span.Length)
            .ThenByDescending(span => span.Start)
            .ToArray();
        List<TextSpan> chain = new();
        foreach (TextSpan candidate in ordered)
        {
            if (chain.Count == 0 || ContainsSpan(candidate, chain[chain.Count - 1]) &&
                !candidate.Equals(chain[chain.Count - 1]))
            {
                chain.Add(candidate);
            }
        }

        CernealaSelectionRange? parent = null;
        for (int index = chain.Count - 1; index >= 0; index--)
        {
            parent = new CernealaSelectionRange(chain[index], parent);
        }

        return parent ?? new CernealaSelectionRange(document.Syntax.Span, null);
    }

    private static IReadOnlyList<CernealaOutlineSymbol> BuildOutline(
        ElementSyntax element,
        bool parentIsResources,
        bool isRoot,
        CernealaSemanticModel? model,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        string localName = LocalName(element.Name);
        bool isResources = element.Kind == SyntaxKind.PropertyElement && localName == "Resources";
        List<CernealaOutlineSymbol> children = new();
        foreach (ElementSyntax child in element.Children.OfType<ElementSyntax>())
        {
            children.AddRange(BuildOutline(child, isResources, isRoot: false, model, cancellationToken));
        }

        CernealaOutlineSymbol? current = CreateOutlineSymbol(
            element,
            parentIsResources,
            isResources,
            isRoot,
            model,
            children);
        return current is null ? children : [current];
    }

    private static CernealaOutlineSymbol? CreateOutlineSymbol(
        ElementSyntax element,
        bool parentIsResources,
        bool isResources,
        bool isRoot,
        CernealaSemanticModel? model,
        IReadOnlyList<CernealaOutlineSymbol> children)
    {
        if (isRoot)
        {
            CernealaSemanticSymbol? root = model?.Symbols.FirstOrDefault(symbol =>
                symbol.Kind == CernealaSemanticSymbolKind.RootType &&
                symbol.Span.Equals(element.NameToken.Span));
            return new CernealaOutlineSymbol(
                element.Name,
                root?.ValueType,
                CernealaOutlineSymbolKind.Root,
                element.Span,
                element.NameToken.Span,
                children);
        }

        if (isResources)
        {
            return new CernealaOutlineSymbol(
                element.Name,
                null,
                CernealaOutlineSymbolKind.ResourceGroup,
                element.Span,
                element.NameToken.Span,
                children);
        }

        string localName = LocalName(element.Name);
        AttributeSyntax? nameAttribute = element.Attributes.FirstOrDefault(attribute =>
            attribute.NameToken.Text == "Name");
        TextSpan nameSpan = nameAttribute is null ? element.NameToken.Span : AttributeContentSpan(nameAttribute);
        CernealaSemanticSymbol? declaration = model?.Symbols
            .Where(symbol => symbol.Span.Equals(nameSpan) && IsOutlineDeclaration(symbol.Kind))
            .OrderByDescending(symbol => OutlinePriority(symbol.Kind))
            .FirstOrDefault();
        if (declaration is not null)
        {
            return new CernealaOutlineSymbol(
                declaration.Name,
                declaration.ValueType,
                MapOutlineKind(declaration.Kind),
                element.Span,
                declaration.Span,
                children);
        }

        if (localName == "ContentTemplate")
        {
            return new CernealaOutlineSymbol(
                AttributeValue(element, "Key") ?? AttributeValue(element, "DataType") ?? "ContentTemplate",
                "ContentTemplate",
                CernealaOutlineSymbolKind.Template,
                element.Span,
                element.NameToken.Span,
                children);
        }

        string? name = AttributeValue(element, "Name");
        if (name is null)
        {
            return null;
        }

        CernealaOutlineSymbolKind kind = localName switch
        {
            "Aspect" => CernealaOutlineSymbolKind.Aspect,
            "MotionClip" or "MotionComposition" or "Tween" or "Spring" or "Decay" or
                "Keyframes" or "Repeat" or "PingPong" => CernealaOutlineSymbolKind.Motion,
            "PrismComposition" => CernealaOutlineSymbolKind.Prism,
            _ when parentIsResources => CernealaOutlineSymbolKind.Resource,
            _ => CernealaOutlineSymbolKind.Element
        };
        return new CernealaOutlineSymbol(name, localName, kind, element.Span, nameSpan, children);
    }

    private static void AddFoldingRanges(
        SyntaxNode node,
        CernealaDocument document,
        ICollection<CernealaFoldingRange> result,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (node is ElementSyntax element)
        {
            if (!element.IsSelfClosing)
            {
                result.Add(new CernealaFoldingRange(element.Span, null));
            }

            foreach (SyntaxNode child in element.Children)
            {
                AddFoldingRanges(child, document, result, cancellationToken);
            }

            return;
        }

        if (node is not TextSyntax text)
        {
            return;
        }

        if (text.Kind == SyntaxKind.Comment)
        {
            result.Add(new CernealaFoldingRange(text.Span, "comment"));
        }

        if (text.Token.Text.IndexOf('@') < 0 || text.Token.Text.IndexOf('{') < 0)
        {
            return;
        }

        EmbeddedParseResult<DirectiveDocumentSyntax> parsed = DirectiveSyntaxParser.Parse(
            text.Token.Text,
            text.Token.Span.Start);
        foreach (DirectiveBlockSyntax block in parsed.Syntax.Blocks)
        {
            result.Add(new CernealaFoldingRange(block.Span, "region"));
        }
    }

    private static void AddCandidate(
        ICollection<TokenCandidate> candidates,
        TextSpan span,
        CernealaSemanticTokenKind kind,
        int priority,
        CernealaSemanticTokenModifiers modifiers = CernealaSemanticTokenModifiers.None) =>
        candidates.Add(new TokenCandidate(span, kind, priority, modifiers));

    private static bool TryMapToken(
        CernealaSemanticSymbolKind symbol,
        out CernealaSemanticTokenKind kind,
        out int priority)
    {
        priority = 100;
        switch (symbol)
        {
            case CernealaSemanticSymbolKind.RootType:
            case CernealaSemanticSymbolKind.Element:
            case CernealaSemanticSymbolKind.TypeReference:
                kind = CernealaSemanticTokenKind.ElementType;
                return true;
            case CernealaSemanticSymbolKind.PropertyElement:
            case CernealaSemanticSymbolKind.Property:
                kind = CernealaSemanticTokenKind.Property;
                return true;
            case CernealaSemanticSymbolKind.AttachedProperty:
                kind = CernealaSemanticTokenKind.AttachedProperty;
                return true;
            case CernealaSemanticSymbolKind.Event:
                kind = CernealaSemanticTokenKind.Event;
                return true;
            case CernealaSemanticSymbolKind.BindingSource:
                kind = CernealaSemanticTokenKind.BindingSource;
                return true;
            case CernealaSemanticSymbolKind.BindingSegment:
            case CernealaSemanticSymbolKind.BindingMode:
                kind = CernealaSemanticTokenKind.BindingMember;
                return true;
            case CernealaSemanticSymbolKind.MotionDirective:
            case CernealaSemanticSymbolKind.MotionTarget:
            case CernealaSemanticSymbolKind.MotionEvent:
            case CernealaSemanticSymbolKind.MotionProperty:
            case CernealaSemanticSymbolKind.MotionSpec:
            case CernealaSemanticSymbolKind.MotionComposition:
            case CernealaSemanticSymbolKind.MotionLifecycle:
            case CernealaSemanticSymbolKind.MotionParameter:
            case CernealaSemanticSymbolKind.MotionHandle:
                kind = CernealaSemanticTokenKind.Motion;
                priority = 130;
                return true;
            case CernealaSemanticSymbolKind.PrismDirective:
            case CernealaSemanticSymbolKind.PrismComposition:
            case CernealaSemanticSymbolKind.PrismNode:
            case CernealaSemanticSymbolKind.PrismOperation:
            case CernealaSemanticSymbolKind.PrismProperty:
            case CernealaSemanticSymbolKind.PrismParameter:
            case CernealaSemanticSymbolKind.PrismValue:
                kind = CernealaSemanticTokenKind.Prism;
                priority = 140;
                return true;
            case CernealaSemanticSymbolKind.AspectCondition:
                kind = CernealaSemanticTokenKind.Directive;
                priority = 120;
                return true;
            case CernealaSemanticSymbolKind.Name:
            case CernealaSemanticSymbolKind.Resource:
            case CernealaSemanticSymbolKind.ResourceReference:
            case CernealaSemanticSymbolKind.ContentTemplate:
            case CernealaSemanticSymbolKind.TemplatePart:
            case CernealaSemanticSymbolKind.Aspect:
            case CernealaSemanticSymbolKind.AspectAssignment:
            case CernealaSemanticSymbolKind.AspectApplication:
                kind = CernealaSemanticTokenKind.Resource;
                priority = 120;
                return true;
            default:
                kind = default;
                return false;
        }
    }

    private static bool IsDeclaration(CernealaSemanticSymbolKind kind) => kind is
        CernealaSemanticSymbolKind.RootType or CernealaSemanticSymbolKind.Name or
        CernealaSemanticSymbolKind.Resource or CernealaSemanticSymbolKind.ContentTemplate or
        CernealaSemanticSymbolKind.TemplatePart or CernealaSemanticSymbolKind.Aspect or
        CernealaSemanticSymbolKind.MotionSpec or CernealaSemanticSymbolKind.MotionComposition or
        CernealaSemanticSymbolKind.MotionParameter or CernealaSemanticSymbolKind.MotionHandle or
        CernealaSemanticSymbolKind.PrismComposition or CernealaSemanticSymbolKind.PrismNode or
        CernealaSemanticSymbolKind.PrismParameter;

    private static bool IsWorkspaceDeclaration(CernealaSemanticSymbolKind kind) => IsDeclaration(kind);

    private static bool IsOutlineDeclaration(CernealaSemanticSymbolKind kind) => kind is
        CernealaSemanticSymbolKind.Name or CernealaSemanticSymbolKind.Resource or
        CernealaSemanticSymbolKind.ContentTemplate or CernealaSemanticSymbolKind.TemplatePart or
        CernealaSemanticSymbolKind.Aspect or CernealaSemanticSymbolKind.MotionSpec or
        CernealaSemanticSymbolKind.MotionComposition or CernealaSemanticSymbolKind.PrismComposition or
        CernealaSemanticSymbolKind.PrismNode;

    private static int OutlinePriority(CernealaSemanticSymbolKind kind) => kind switch
    {
        CernealaSemanticSymbolKind.PrismComposition or CernealaSemanticSymbolKind.PrismNode => 50,
        CernealaSemanticSymbolKind.MotionSpec or CernealaSemanticSymbolKind.MotionComposition => 40,
        CernealaSemanticSymbolKind.Aspect => 30,
        CernealaSemanticSymbolKind.ContentTemplate => 25,
        CernealaSemanticSymbolKind.Resource => 20,
        CernealaSemanticSymbolKind.Name => 10,
        CernealaSemanticSymbolKind.RootType => 5,
        _ => 0
    };

    private static CernealaOutlineSymbolKind MapOutlineKind(CernealaSemanticSymbolKind kind) => kind switch
    {
        CernealaSemanticSymbolKind.RootType => CernealaOutlineSymbolKind.Root,
        CernealaSemanticSymbolKind.ContentTemplate => CernealaOutlineSymbolKind.Template,
        CernealaSemanticSymbolKind.Aspect => CernealaOutlineSymbolKind.Aspect,
        CernealaSemanticSymbolKind.MotionSpec or CernealaSemanticSymbolKind.MotionComposition or
            CernealaSemanticSymbolKind.MotionParameter or CernealaSemanticSymbolKind.MotionHandle =>
            CernealaOutlineSymbolKind.Motion,
        CernealaSemanticSymbolKind.PrismComposition or CernealaSemanticSymbolKind.PrismNode or
            CernealaSemanticSymbolKind.PrismParameter => CernealaOutlineSymbolKind.Prism,
        CernealaSemanticSymbolKind.Name or CernealaSemanticSymbolKind.TemplatePart =>
            CernealaOutlineSymbolKind.Element,
        _ => CernealaOutlineSymbolKind.Resource
    };

    private static string? AttributeValue(ElementSyntax element, string name)
    {
        AttributeSyntax? attribute = element.Attributes.FirstOrDefault(candidate =>
            candidate.NameToken.Text == name);
        if (attribute is null || attribute.ValueToken.IsMissing)
        {
            return null;
        }

        string value = attribute.ValueToken.Text;
        return value.Length >= 2 && value[0] == value[value.Length - 1] && value[0] is '\'' or '"'
            ? value.Substring(1, value.Length - 2)
            : value;
    }

    private static TextSpan AttributeContentSpan(AttributeSyntax attribute)
    {
        string token = attribute.ValueToken.Text;
        bool quoted = token.Length > 0 && token[0] is '\'' or '"';
        int start = attribute.ValueToken.Span.Start + (quoted ? 1 : 0);
        int length = token.Length - (quoted && token.Length > 1 ? 2 : quoted ? 1 : 0);
        return new TextSpan(start, Math.Max(0, length));
    }

    private static bool IsMultiline(SourceText source, TextSpan span) =>
        source.GetLinePosition(span.Start).Line < source.GetLinePosition(span.End).Line;

    private static bool ContainsOffset(TextSpan span, int offset) =>
        span.Contains(offset) || span.Length == 0 && span.Start == offset ||
        offset == span.End && span.End == span.Start;

    private static bool ContainsSpan(TextSpan outer, TextSpan inner) =>
        outer.Start <= inner.Start && outer.End >= inner.End;

    private static bool Overlaps(TextSpan left, TextSpan right) =>
        left.Start < right.End && right.Start < left.End;

    private static string LocalName(string name)
    {
        int dot = name.LastIndexOf('.');
        int colon = name.LastIndexOf(':');
        int separator = Math.Max(dot, colon);
        return separator < 0 ? name : name.Substring(separator + 1);
    }

    private static IEnumerable<SyntaxNode> DescendantNodes(SyntaxNode node)
    {
        foreach (SyntaxNode child in node switch
        {
            DocumentSyntax document => document.Children,
            ElementSyntax element => element.Children,
            _ => Array.Empty<SyntaxNode>()
        })
        {
            yield return child;
            foreach (SyntaxNode descendant in DescendantNodes(child))
            {
                yield return descendant;
            }
        }
    }

    private sealed record TokenCandidate(
        TextSpan Span,
        CernealaSemanticTokenKind Kind,
        int Priority,
        CernealaSemanticTokenModifiers Modifiers);
}
