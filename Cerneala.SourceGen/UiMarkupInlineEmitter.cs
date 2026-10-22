using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;

namespace Cerneala.SourceGen;

public sealed partial class UiMarkupGenerator
{
    private sealed partial class GenerationScope
    {
        private const string InlineDirectiveContentMessage = "Inline elements are not supported inside directive content.";

        private const string InlineHostDirectivesMessage = "Directives are not supported on inline elements.";

        // A piece of the content of an inline host, in document order: a text, or a child element.
        private sealed class InlinePiece
        {
            public InlinePiece(string? text, MarkupElement? element)
            {
                Text = text;
                Element = element;
            }

            public string? Text { get; set; }

            public MarkupElement? Element { get; }
        }

        private readonly Dictionary<MarkupElement, List<InlinePiece>> normalizedInlinePieces = [];

        private bool DerivesFromMetadataName(INamedTypeSymbol type, string metadataName)
        {
            INamedTypeSymbol? baseType = compilation.GetTypeByMetadataName(metadataName);
            return baseType is not null && IsOrDerivesFrom(type, baseType);
        }

        // The elements whose content is generated from the raw text and elements of the markup instead of the directive
        // parser: a Run (its text), a Span (its inlines) and a TextBlock that holds at least one inline element. A TextBlock
        // with only text keeps the Text property.
        private bool HasInlineContent(MarkupElement element, INamedTypeSymbol type)
        {
            if (DerivesFromMetadataName(type, "Cerneala.UI.Documents.Run"))
            {
                return true;
            }

            if (!IsInlineHostType(type))
            {
                return false;
            }

            return DerivesFromMetadataName(type, "Cerneala.UI.Documents.Span") || element.Elements().Any(IsInlineElement);
        }

        private void EmitInlineContent(
            MarkupElement element,
            INamedTypeSymbol type,
            string variable,
            DirectiveParseResult parsed)
        {
            // The directive parser consumes directives together with the elements inside them, so the raw content
            // cannot be read when a directive is present. The inlines are created by code, not by reactive rules.
            DirectiveNode? directive = parsed.Nodes.FirstOrDefault(node => node is not DirectiveTextNode and not DirectiveElementNode);
            if (directive is not null)
            {
                Report(
                    InvalidDirective,
                    directive.Source,
                    Path.GetFileName(file.Path),
                    element.Elements().Any(IsInlineElement) ? InlineDirectiveContentMessage : InlineHostDirectivesMessage);
                return;
            }

            List<InlinePiece> pieces = GetNormalizedInlinePieces(element);
            if (DerivesFromMetadataName(type, "Cerneala.UI.Documents.Run"))
            {
                EmitRunText(element, variable, pieces);
                return;
            }

            foreach (InlinePiece piece in pieces)
            {
                if (piece.Text is not null)
                {
                    if (piece.Text.Length == 0)
                    {
                        continue;
                    }

                    currentLines.Add(
                        variable + ".Inlines.Add(new global::Cerneala.UI.Documents.Run(" + Literal(piece.Text) + "));");
                    continue;
                }

                bool childIsInline = IsInlineElement(piece.Element!);
                string childVariable = EmitElement(piece.Element!, allowInline: childIsInline);
                EmitChild(element, variable, childVariable, childIsInline);
            }
        }

        private void EmitRunText(MarkupElement run, string variable, List<InlinePiece> pieces)
        {
            if (pieces.Any(piece => piece.Element is not null))
            {
                Report(UnsupportedProperty, run, run.Name.LocalName, "#child");
                return;
            }

            string text = string.Concat(pieces.Select(piece => piece.Text));
            if (text.Length == 0)
            {
                return;
            }

            currentLines.Add(
                variable + ".SetValue(global::Cerneala.UI.Documents.Run.TextProperty, " + Literal(text) +
                ", global::Cerneala.UI.Core.UiPropertyValueSource.MarkupBase);");
        }

        // The raw content of the element: adjacent text nodes are one text (a comment does not split it), and the
        // property elements that other emitters consume (TextBlock.Foreground) are not content.
        private List<InlinePiece> ReadInlinePieces(MarkupElement element)
        {
            List<InlinePiece> pieces = [];
            StringBuilder text = new();
            void FlushText()
            {
                if (text.Length > 0)
                {
                    pieces.Add(new InlinePiece(text.ToString(), null));
                    text.Clear();
                }
            }

            foreach (MarkupNode node in element.Nodes())
            {
                if (node is MarkupText markupText)
                {
                    text.Append(markupText.Value);
                }
                else if (node is MarkupElement child && !IsNonContentPropertyElement(element, child))
                {
                    FlushText();
                    pieces.Add(new InlinePiece(null, child));
                }
            }

            FlushText();
            return pieces;
        }

        // The pieces of a host after the whitespace rules. The rules run once over the whole content of the outermost host
        // (the TextBlock), across the boundaries of the nested hosts, and the result is kept for every nested host.
        private List<InlinePiece> GetNormalizedInlinePieces(MarkupElement host)
        {
            if (!normalizedInlinePieces.TryGetValue(host, out List<InlinePiece>? pieces))
            {
                List<InlinePiece> flat = [];
                CollectInlinePieces(host, flat);
                NormalizeInlineWhitespace(flat);
                pieces = normalizedInlinePieces[host];
            }

            return pieces;
        }

        // Reads the pieces of the host and of every nested host (Run content, Span, Bold, Italic) in document order. The
        // flat list holds the same piece objects as the per-host lists.
        private void CollectInlinePieces(MarkupElement host, List<InlinePiece> flat)
        {
            List<InlinePiece> pieces = ReadInlinePieces(host);
            normalizedInlinePieces[host] = pieces;
            foreach (InlinePiece piece in pieces)
            {
                flat.Add(piece);
                INamedTypeSymbol? childType = piece.Element is null ? null : GetElementTypeSymbol(piece.Element);
                if (childType is not null && IsInlineType(childType) && HasInlineContent(piece.Element!, childType))
                {
                    CollectInlinePieces(piece.Element!, flat);
                }
            }
        }

        // The whitespace rules of WPF over the flat content: a run of spaces, tabs and new lines is one space, also across
        // elements (the space stays with the earlier piece); there is none at the very start and the very end, and none
        // next to a LineBreak. A text that is left empty stays in the list with an empty value and produces no Run.
        private void NormalizeInlineWhitespace(List<InlinePiece> flat)
        {
            InlinePiece? last = null;
            bool afterSpace = true;
            foreach (InlinePiece piece in flat)
            {
                if (piece.Text is not null)
                {
                    string text = CollapseInlineWhitespace(piece.Text);
                    if (afterSpace && text.StartsWith(" ", StringComparison.Ordinal))
                    {
                        text = text.Substring(1);
                    }

                    piece.Text = text;
                    if (text.Length > 0)
                    {
                        last = piece;
                        afterSpace = text[text.Length - 1] == ' ';
                    }
                }
                else if (IsLineBreakPiece(piece))
                {
                    TrimTrailingSpace(last);
                    afterSpace = true;
                }
                else if (piece.Element!.Attribute("Text") is { Value.Length: > 0 } textAttribute)
                {
                    // A Run with a Text attribute is text that is never altered, but it is part of the flow: the
                    // whitespace after it is kept.
                    last = null;
                    afterSpace = textAttribute.Value[textAttribute.Value.Length - 1] == ' ';
                }
            }

            TrimTrailingSpace(last);
        }

        private static void TrimTrailingSpace(InlinePiece? piece)
        {
            if (piece?.Text is { Length: > 0 } text && text[text.Length - 1] == ' ')
            {
                piece.Text = text.Substring(0, text.Length - 1);
            }
        }

        private bool IsLineBreakPiece(InlinePiece piece)
        {
            if (piece.Element is null)
            {
                return false;
            }

            INamedTypeSymbol? type = GetElementTypeSymbol(piece.Element);
            return type is not null && DerivesFromMetadataName(type, "Cerneala.UI.Documents.LineBreak");
        }

        private static string CollapseInlineWhitespace(string text)
        {
            StringBuilder builder = new(text.Length);
            bool inWhitespace = false;
            foreach (char character in text)
            {
                if (character is ' ' or '\t' or '\r' or '\n')
                {
                    if (!inWhitespace)
                    {
                        builder.Append(' ');
                        inWhitespace = true;
                    }
                }
                else
                {
                    builder.Append(character);
                    inWhitespace = false;
                }
            }

            return builder.ToString();
        }
    }
}
