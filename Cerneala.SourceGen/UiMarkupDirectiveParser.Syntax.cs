using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace Cerneala.SourceGen;

public sealed partial class UiMarkupGenerator
{
    [Flags]
    private enum DirectiveContentKind
    {
        None = 0,
        Assignments = 1,
        Elements = 2,
        Templates = 4,
        MotionTriggers = 8,
        MotionExecutions = 16,
        MotionParameters = 32,
        MotionHandles = 64,
        MotionPresence = 128,
        MotionLayout = 256,
        MotionScroll = 512,
        MotionDrag = 1024,
        MotionGesture = 2048,
        Prism = 4096,
        TimbreActions = 8192
    }

    private abstract class DirectiveNode
    {
        protected DirectiveNode(MarkupObject source)
        {
            Source = source;
        }

        public MarkupObject Source { get; }
    }

    private sealed class DirectiveElementNode : DirectiveNode
    {
        public DirectiveElementNode(MarkupElement element) : base(element)
        {
            Element = element;
        }

        public MarkupElement Element { get; }
    }

    private sealed class DirectiveTextNode : DirectiveNode
    {
        public DirectiveTextNode(string text, MarkupObject source) : base(source)
        {
            Text = text;
        }

        public string Text { get; }
    }

    private sealed class DirectiveAssignmentNode : DirectiveNode
    {
        public DirectiveAssignmentNode(
            string propertyName,
            string value,
            MarkupObject source,
            DirectiveExpressionLocation propertyLocation,
            DirectiveExpressionLocation valueLocation) : base(source)
        {
            PropertyName = propertyName;
            Value = value;
            PropertyLocation = propertyLocation;
            ValueLocation = valueLocation;
        }

        public string PropertyName { get; }

        public string Value { get; }

        public DirectiveExpressionLocation PropertyLocation { get; }

        public DirectiveExpressionLocation ValueLocation { get; }
    }

    private sealed class DirectivePrismNode : DirectiveNode
    {
        public DirectivePrismNode(PrismApplicationSyntax application, MarkupObject source) : base(source)
        {
            Application = application;
        }

        public PrismApplicationSyntax Application { get; }
    }

    private sealed class DirectiveDefaultNode : DirectiveNode
    {
        public DirectiveDefaultNode(IReadOnlyList<DirectiveNode> body, MarkupObject source) : base(source)
        {
            Body = body;
        }

        public IReadOnlyList<DirectiveNode> Body { get; }
    }

    private sealed class DirectiveTemplateNode : DirectiveNode
    {
        public DirectiveTemplateNode(MarkupElement root, MarkupObject source) : base(source)
        {
            Root = root;
        }

        public MarkupElement Root { get; }
    }

    private sealed class DirectiveTemplatesNode : DirectiveNode
    {
        public DirectiveTemplatesNode(IReadOnlyList<MarkupElement> templates, MarkupObject source) : base(source)
        {
            Templates = templates;
        }

        public IReadOnlyList<MarkupElement> Templates { get; }
    }

    private sealed class DirectiveExpressionLocation
    {
        public DirectiveExpressionLocation(MarkupObject source, int offset, int length = 1)
        {
            Source = source;
            Offset = offset;
            Length = length;
        }

        public MarkupObject Source { get; }

        public int Offset { get; }

        public int Length { get; }
    }

    private abstract class DirectiveExpression
    {
        protected DirectiveExpression(DirectiveExpressionLocation location)
        {
            Location = location;
        }

        public DirectiveExpressionLocation Location { get; }
    }

    private sealed class DirectiveSourceExpression : DirectiveExpression
    {
        public DirectiveSourceExpression(string text, DirectiveExpressionLocation location) : base(location)
        {
            Text = text;
        }

        public string Text { get; }
    }

    private sealed class DirectiveValueExpression : DirectiveExpression
    {
        public DirectiveValueExpression(DirectiveExpressionLocation location) : base(location)
        {
        }
    }

    private sealed class DirectiveLiteralExpression : DirectiveExpression
    {
        public DirectiveLiteralExpression(string text, DirectiveExpressionLocation location) : base(location)
        {
            Text = text;
        }

        public string Text { get; }
    }

    private sealed class DirectiveComparisonExpression : DirectiveExpression
    {
        public DirectiveComparisonExpression(
            DirectiveExpression left,
            string comparator,
            DirectiveExpression right,
            DirectiveExpressionLocation location) : base(location)
        {
            Left = left;
            Comparator = comparator;
            Right = right;
        }

        public DirectiveExpression Left { get; }

        public string Comparator { get; }

        public DirectiveExpression Right { get; }
    }

    private enum DirectiveLogicalOperator
    {
        And,
        Or
    }

    private sealed class DirectiveLogicalExpression : DirectiveExpression
    {
        public DirectiveLogicalExpression(
            DirectiveExpression left,
            DirectiveLogicalOperator @operator,
            DirectiveExpression right,
            DirectiveExpressionLocation location) : base(location)
        {
            Left = left;
            Operator = @operator;
            Right = right;
        }

        public DirectiveExpression Left { get; }

        public DirectiveLogicalOperator Operator { get; }

        public DirectiveExpression Right { get; }
    }

    private sealed class DirectiveGroupExpression : DirectiveExpression
    {
        public DirectiveGroupExpression(DirectiveExpression inner, DirectiveExpressionLocation location) : base(location)
        {
            Inner = inner;
        }

        public DirectiveExpression Inner { get; }
    }

    private sealed class DirectiveWhenNode : DirectiveNode
    {
        public DirectiveWhenNode(
            DirectiveExpression expression,
            IReadOnlyList<DirectiveIfNode> branches,
            IReadOnlyList<DirectiveNode>? booleanBody,
            MarkupObject source) : base(source)
        {
            Expression = expression;
            Branches = branches;
            BooleanBody = booleanBody;
        }

        public DirectiveExpression Expression { get; }

        public IReadOnlyList<DirectiveIfNode> Branches { get; }

        public IReadOnlyList<DirectiveNode>? BooleanBody { get; }
    }

    private sealed class DirectiveIfNode : DirectiveNode
    {
        public DirectiveIfNode(DirectiveExpression expression, IReadOnlyList<DirectiveNode> body, MarkupObject source) : base(source)
        {
            Expression = expression;
            Body = body;
        }

        public DirectiveExpression Expression { get; }

        public IReadOnlyList<DirectiveNode> Body { get; }
    }

    private sealed class DirectiveParseResult
    {
        public DirectiveParseResult(
            IReadOnlyList<DirectiveNode> nodes,
            string? error,
            object? errorSource,
            IReadOnlyList<PrismSyntaxDiagnostic>? prismDiagnostics = null)
        {
            Nodes = nodes;
            Error = error;
            ErrorSource = errorSource;
            PrismDiagnostics = prismDiagnostics ?? [];
        }

        public IReadOnlyList<DirectiveNode> Nodes { get; }

        public string? Error { get; }

        public object? ErrorSource { get; }

        public IReadOnlyList<PrismSyntaxDiagnostic> PrismDiagnostics { get; }

        public bool HasDirectives => Nodes.Any(ContainsDirective);

        private static bool ContainsDirective(DirectiveNode node)
        {
            return node is DirectiveWhenNode or DirectiveDefaultNode or DirectiveOnNode;
        }
    }

    private sealed class DirectiveParseException : Exception
    {
        public DirectiveParseException(string message, object? locationSource) : base(message)
        {
            LocationSource = locationSource;
        }

        public object? LocationSource { get; }
    }
}
