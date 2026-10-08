using System;

namespace Cerneala.SourceGen;

public sealed partial class UiMarkupGenerator
{
    // A Timbre action statement. Its meaning comes from the bound
    // Cerneala.Language Timbre model of the owning Aspect; the cursor records
    // only the statement boundary and source order.
    private sealed class TimbreActionNode : DirectiveNode
    {
        public TimbreActionNode(string keyword, int sequence, MarkupObject source) : base(source)
        {
            Keyword = keyword;
            Sequence = sequence;
        }

        public string Keyword { get; }

        // Source order of the statement among the Timbre/@cancel statements of
        // its directive body; it matches the bound model's document order.
        public int Sequence { get; }
    }

    private sealed partial class DirectiveCursor
    {
        private static readonly string[] timbreActionKeywords = ["@timbre", "@pause", "@resume", "@seek"];

        private int actionSequence;

        private bool TryParseTimbreDirective(DirectiveContentKind allowedContent, out DirectiveNode? node)
        {
            node = null;
            if (StartsWith("@modifier"))
            {
                throw Error("@modifier is allowed only inside TimbreClip.");
            }

            foreach (string keyword in timbreActionKeywords)
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

        private static DirectiveContentKind TimbreContent(DirectiveContentKind allowedContent) =>
            (allowedContent & (DirectiveContentKind.MotionTriggers | DirectiveContentKind.TimbreActions)) != 0
                ? DirectiveContentKind.TimbreActions
                : DirectiveContentKind.None;
    }
}
