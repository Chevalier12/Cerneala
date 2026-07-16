using System.Collections.Generic;
using System.Xml.Linq;

namespace Cerneala.SourceGen;

public sealed partial class UiMarkupGenerator
{
    private abstract class MotionSyntaxNode
    {
        protected MotionSyntaxNode(DirectiveExpressionLocation location)
        {
            Location = location;
        }

        public DirectiveExpressionLocation Location { get; }
    }

    private abstract class MotionExecutionNode : DirectiveNode
    {
        protected MotionExecutionNode(XObject source) : base(source)
        {
        }
    }

    private sealed class DirectiveOnNode : DirectiveNode
    {
        public DirectiveOnNode(string eventName, IReadOnlyList<MotionExecutionNode> body, XObject source) : base(source)
        {
            EventName = eventName;
            Body = body;
        }

        public string EventName { get; }

        public IReadOnlyList<MotionExecutionNode> Body { get; }
    }

    private sealed class MotionAnimateNode : MotionExecutionNode
    {
        public MotionAnimateNode(
            MotionSpecSyntax? defaultSpec,
            IReadOnlyList<MotionOptionSyntax> options,
            IReadOnlyList<MotionAssignmentSyntax> from,
            IReadOnlyList<MotionAssignmentSyntax> to,
            XObject source) : base(source)
        {
            DefaultSpec = defaultSpec;
            Options = options;
            From = from;
            To = to;
        }

        public MotionSpecSyntax? DefaultSpec { get; }

        public IReadOnlyList<MotionOptionSyntax> Options { get; }

        public IReadOnlyList<MotionAssignmentSyntax> From { get; }

        public IReadOnlyList<MotionAssignmentSyntax> To { get; }
    }

    private enum MotionCompositionKind
    {
        Parallel,
        Sequence
    }

    private sealed class MotionCompositionNode : MotionExecutionNode
    {
        public MotionCompositionNode(
            MotionCompositionKind kind,
            IReadOnlyList<MotionExecutionNode> children,
            XObject source) : base(source)
        {
            Kind = kind;
            Children = children;
        }

        public MotionCompositionKind Kind { get; }

        public IReadOnlyList<MotionExecutionNode> Children { get; }
    }

    private sealed class MotionRunNode : MotionExecutionNode
    {
        public MotionRunNode(
            string clipName,
            IReadOnlyList<MotionRunArgumentSyntax> arguments,
            string? handleName,
            XObject source) : base(source)
        {
            ClipName = clipName;
            Arguments = arguments;
            HandleName = handleName;
        }

        public string ClipName { get; }

        public IReadOnlyList<MotionRunArgumentSyntax> Arguments { get; }

        public string? HandleName { get; }
    }

    private sealed class MotionCancelNode : MotionExecutionNode
    {
        public MotionCancelNode(string handleName, XObject source) : base(source)
        {
            HandleName = handleName;
        }

        public string HandleName { get; }
    }

    private sealed class MotionHandleNode : DirectiveNode
    {
        public MotionHandleNode(string name, XObject source) : base(source)
        {
            Name = name;
        }

        public string Name { get; }
    }

    private sealed class MotionRunArgumentSyntax : MotionSyntaxNode
    {
        public MotionRunArgumentSyntax(string name, string value, DirectiveExpressionLocation location) : base(location)
        {
            Name = name;
            Value = value;
        }

        public string Name { get; }

        public string Value { get; }
    }

    private sealed class MotionParameterNode : DirectiveNode
    {
        public MotionParameterNode(
            string name,
            string typeName,
            string? defaultValue,
            DirectiveExpressionLocation location,
            XObject source) : base(source)
        {
            Name = name;
            TypeName = typeName;
            DefaultValue = defaultValue;
            Location = location;
        }

        public string Name { get; }

        public string TypeName { get; }

        public string? DefaultValue { get; }

        public DirectiveExpressionLocation Location { get; }
    }

    private sealed class MotionOptionSyntax : MotionSyntaxNode
    {
        public MotionOptionSyntax(string name, MotionValueSyntax value, DirectiveExpressionLocation location) : base(location)
        {
            Name = name;
            Value = value;
        }

        public string Name { get; }

        public MotionValueSyntax Value { get; }
    }

    private sealed class MotionAssignmentSyntax : MotionSyntaxNode
    {
        public MotionAssignmentSyntax(
            string target,
            MotionValueSyntax value,
            MotionSpecSyntax? spec,
            DirectiveExpressionLocation location) : base(location)
        {
            Target = target;
            Value = value;
            Spec = spec;
        }

        public string Target { get; }

        public MotionValueSyntax Value { get; }

        public MotionSpecSyntax? Spec { get; }
    }

    private abstract class MotionValueSyntax : MotionSyntaxNode
    {
        protected MotionValueSyntax(DirectiveExpressionLocation location) : base(location)
        {
        }
    }

    private sealed class MotionCurrentValueSyntax : MotionValueSyntax
    {
        public MotionCurrentValueSyntax(DirectiveExpressionLocation location) : base(location)
        {
        }
    }

    private sealed class MotionAtomValueSyntax : MotionValueSyntax
    {
        public MotionAtomValueSyntax(string text, DirectiveExpressionLocation location) : base(location)
        {
            Text = text;
        }

        public string Text { get; }
    }

    private sealed class MotionConditionalValueSyntax : MotionValueSyntax
    {
        public MotionConditionalValueSyntax(
            DirectiveExpression condition,
            MotionValueSyntax whenTrue,
            MotionValueSyntax whenFalse,
            DirectiveExpressionLocation location) : base(location)
        {
            Condition = condition;
            WhenTrue = whenTrue;
            WhenFalse = whenFalse;
        }

        public DirectiveExpression Condition { get; }

        public MotionValueSyntax WhenTrue { get; }

        public MotionValueSyntax WhenFalse { get; }
    }

    private abstract class MotionSpecSyntax : MotionSyntaxNode
    {
        protected MotionSpecSyntax(DirectiveExpressionLocation location) : base(location)
        {
        }
    }

    private sealed class MotionResourceSpecSyntax : MotionSpecSyntax
    {
        public MotionResourceSpecSyntax(string name, DirectiveExpressionLocation location) : base(location)
        {
            Name = name;
        }

        public string Name { get; }
    }

    private sealed class MotionParameterSpecSyntax : MotionSpecSyntax
    {
        public MotionParameterSpecSyntax(string name, DirectiveExpressionLocation location) : base(location)
        {
            Name = name;
        }

        public string Name { get; }
    }

    private sealed class MotionInlineSpecSyntax : MotionSpecSyntax
    {
        public MotionInlineSpecSyntax(
            string kind,
            IReadOnlyList<MotionSpecArgumentSyntax> arguments,
            DirectiveExpressionLocation location) : base(location)
        {
            Kind = kind;
            Arguments = arguments;
        }

        public string Kind { get; }

        public IReadOnlyList<MotionSpecArgumentSyntax> Arguments { get; }
    }

    private sealed class MotionSpecArgumentSyntax : MotionSyntaxNode
    {
        public MotionSpecArgumentSyntax(string text, MotionDurationSyntax? duration, DirectiveExpressionLocation location) : base(location)
        {
            Text = text;
            Duration = duration;
        }

        public string Text { get; }

        public MotionDurationSyntax? Duration { get; }
    }

    private sealed class MotionDurationSyntax : MotionSyntaxNode
    {
        public MotionDurationSyntax(decimal value, string unit, DirectiveExpressionLocation location) : base(location)
        {
            Value = value;
            Unit = unit;
        }

        public decimal Value { get; }

        public string Unit { get; }
    }
}
