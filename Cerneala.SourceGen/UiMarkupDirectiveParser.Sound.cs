using System;

namespace Cerneala.SourceGen;

public sealed partial class UiMarkupGenerator
{
    // A Sound action statement. Its meaning comes from the bound
    // Cerneala.Language Sound model of the owning Aspect; the cursor records
    // only the statement boundary and source order.
    private sealed class SoundActionNode : DirectiveNode
    {
        public SoundActionNode(string keyword, int sequence, MarkupObject source) : base(source)
        {
            Keyword = keyword;
            Sequence = sequence;
        }

        public string Keyword { get; }

        // Source order of the statement among the Sound/@cancel statements of
        // its directive body; it matches the bound model's document order.
        public int Sequence { get; }
    }

    private sealed partial class DirectiveCursor
    {
        private static readonly string[] soundActionKeywords = ["@sound", "@pause", "@resume", "@seek"];

        private int actionSequence;

        private bool TryParseSoundDirective(DirectiveContentKind allowedContent, out DirectiveNode? node)
        {
            node = null;
            if (StartsWith("@modifier"))
            {
                throw Error("@modifier is allowed only inside SoundClip.");
            }

            foreach (string keyword in soundActionKeywords)
            {
                if (!StartsWith(keyword))
                {
                    continue;
                }

                if (!Allows(allowedContent, DirectiveContentKind.SoundActions))
                {
                    throw Error(keyword + " is allowed only inside an Aspect @on, @when or @if body.");
                }

                MarkupObject source = CurrentSource;
                Consume(keyword);
                _ = ReadMotionStatement();
                node = new SoundActionNode(keyword, actionSequence++, source);
                return true;
            }

            return false;
        }

        private static DirectiveContentKind SoundContent(DirectiveContentKind allowedContent) =>
            (allowedContent & (DirectiveContentKind.MotionTriggers | DirectiveContentKind.SoundActions)) != 0
                ? DirectiveContentKind.SoundActions
                : DirectiveContentKind.None;
    }
}
