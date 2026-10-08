using System;

namespace Cerneala.SourceGen;

public sealed partial class UiMarkupGenerator
{
    // A Timbre command (`@play`, `@stop`, `@pause`, `@resume`, `@seek`). Its
    // meaning comes from the bound Cerneala.Language Timbre model of the
    // owning Aspect; the cursor records only the statement boundary and
    // source order.
    private sealed class TimbreActionNode : DirectiveNode
    {
        public TimbreActionNode(string keyword, int sequence, MarkupObject source) : base(source)
        {
            Keyword = keyword;
            Sequence = sequence;
        }

        public string Keyword { get; }

        // Source order of the command among the Timbre commands of its
        // Aspect; it matches the bound model's document order.
        public int Sequence { get; }
    }

    // The `@timbre` attachment of an Aspect body; bound by Cerneala.Language.
    private sealed class TimbreAttachmentNode : DirectiveNode
    {
        public TimbreAttachmentNode(MarkupObject source) : base(source)
        {
        }
    }

    private sealed partial class DirectiveCursor
    {
        private static readonly string[] timbreCommandKeywords = ["@play", "@stop", "@pause", "@resume", "@seek"];

        private int actionSequence;

        private bool TryParseTimbreDirective(DirectiveContentKind allowedContent, out DirectiveNode? node)
        {
            node = null;
            if (StartsWith("@modifier"))
            {
                throw Error("@modifier is allowed only inside @sound.");
            }

            if (StartsWith("@sound"))
            {
                throw Error("@sound is allowed only inside TimbreClip or an Aspect's @timbre block.");
            }

            if (StartsWith("@timbre"))
            {
                if (!Allows(allowedContent, DirectiveContentKind.TimbreAttachment))
                {
                    throw Error("@timbre is written only at the top of an Aspect body.");
                }

                MarkupObject source = CurrentSource;
                Consume("@timbre");
                SkipWhitespace();
                if (Peek() == '{')
                {
                    Read();
                    SkipBalancedBlock();
                }
                else
                {
                    _ = ReadMotionStatement();
                }

                node = new TimbreAttachmentNode(source);
                return true;
            }

            foreach (string keyword in timbreCommandKeywords)
            {
                if (!StartsWith(keyword))
                {
                    continue;
                }

                if (!Allows(allowedContent, DirectiveContentKind.TimbreActions))
                {
                    throw Error(keyword + " is allowed only inside an Aspect @on, @when or @if body.");
                }

                MarkupObject source = CurrentSource;
                Consume(keyword);
                _ = ReadMotionStatement();
                node = new TimbreActionNode(keyword, actionSequence++, source);
                return true;
            }

            return false;
        }

        // Skips to just after the '}' matching an already consumed '{'.
        private void SkipBalancedBlock()
        {
            int depth = 1;
            bool quoted = false;
            while (depth > 0)
            {
                if (AtEnd)
                {
                    throw Error("Missing closing '}'.");
                }

                if (CurrentElement is not null)
                {
                    AdvanceSegment();
                    continue;
                }

                char character = Read();
                if (quoted)
                {
                    quoted = character != '"';
                }
                else if (character == '"')
                {
                    quoted = true;
                }
                else if (character == '{')
                {
                    depth++;
                }
                else if (character == '}')
                {
                    depth--;
                }
            }
        }

        private static DirectiveContentKind TimbreContent(DirectiveContentKind allowedContent) =>
            (allowedContent & (DirectiveContentKind.MotionTriggers | DirectiveContentKind.TimbreActions)) != 0
                ? DirectiveContentKind.TimbreActions
                : DirectiveContentKind.None;
    }
}
