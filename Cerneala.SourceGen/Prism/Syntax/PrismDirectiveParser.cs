using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml.Linq;
using Cerneala.SourceGen.Prism.Syntax;
using Microsoft.CodeAnalysis;

namespace Cerneala.SourceGen;

public sealed partial class UiMarkupGenerator
{
    private static readonly DiagnosticDescriptor PrismUnknownDirectiveDiagnostic = new(
        "PRISM1001",
        "Unknown Prism directive",
        "Prism markup in '{0}' is invalid: {1}",
        "Cerneala.Prism.Markup",
        DiagnosticSeverity.Error,
        true);

    private static readonly DiagnosticDescriptor PrismMissingDelimiterDiagnostic = new(
        "PRISM1002",
        "Missing Prism delimiter",
        "Prism markup in '{0}' is invalid: {1}",
        "Cerneala.Prism.Markup",
        DiagnosticSeverity.Error,
        true);

    private static readonly DiagnosticDescriptor PrismInvalidSyntaxDiagnostic = new(
        "PRISM1003",
        "Invalid Prism syntax",
        "Prism markup in '{0}' is invalid: {1}",
        "Cerneala.Prism.Markup",
        DiagnosticSeverity.Error,
        true);

    private static PrismValueSyntax ParsePrismValue(string text, DirectiveExpressionLocation location)
    {
        PrismValueKind kind;
        if (text.StartsWith("$", StringComparison.Ordinal))
        {
            kind = PrismValueKind.ResourceReference;
        }
        else if (text.Length >= 2 && text[0] == '"' && text[text.Length - 1] == '"')
        {
            kind = PrismValueKind.StringLiteral;
        }
        else if (text.StartsWith("#", StringComparison.Ordinal))
        {
            kind = PrismValueKind.ColorLiteral;
        }
        else if (text.Length >= 2 && text[0] == '(' && text[text.Length - 1] == ')')
        {
            kind = PrismValueKind.TupleLiteral;
        }
        else if (bool.TryParse(text, out _))
        {
            kind = PrismValueKind.BooleanLiteral;
        }
        else if (decimal.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out _))
        {
            kind = PrismValueKind.NumberLiteral;
        }
        else if (string.Equals(text, "null", StringComparison.OrdinalIgnoreCase))
        {
            kind = PrismValueKind.NullLiteral;
        }
        else
        {
            kind = PrismValueKind.Identifier;
        }

        return new PrismValueSyntax(text, kind, location);
    }

    private enum PrismBodyContext
    {
        InlineComposition,
        ResourceComposition,
        Layer,
        Group,
        Backdrop,
        Filter,
        Style,
        Mask
    }

    private sealed partial class DirectiveCursor
    {
        public DirectivePrismNode ParsePrismApplication()
        {
            XObject source = CurrentSource;
            DirectiveExpressionLocation directiveLocation =
                new(source, characterIndex, "@prism".Length);
            Consume("@prism");
            SkipWhitespace();

            if (AtEnd || CurrentElement is not null)
            {
                throw PrismInvalid("@prism must be followed by a resource reference or inline block.", directiveLocation);
            }

            if (Peek() == '$')
            {
                return new DirectivePrismNode(
                    ParsePrismResourceApplication(directiveLocation),
                    source);
            }

            DirectiveHeader header;
            try
            {
                header = ReadHeaderUntilBrace();
            }
            catch (DirectiveParseException)
            {
                throw PrismInvalid("@prism must be followed by a resource reference or inline block.", directiveLocation);
            }

            if (!string.IsNullOrWhiteSpace(header.Text))
            {
                throw PrismInvalid(
                    "@prism accepts no header before its inline block.",
                    TrimmedLocation(header));
            }

            IReadOnlyList<PrismMemberSyntax> members = ParsePrismMembers(
                PrismBodyContext.InlineComposition,
                stopAtClosingBrace: true,
                directiveLocation);
            PrismContainerSyntax composition = new(
                PrismContainerKind.Composition,
                null,
                null,
                members,
                directiveLocation);
            return new DirectivePrismNode(
                new PrismInlineApplicationSyntax(composition, directiveLocation),
                source);
        }

        public PrismCompositionResourceSyntax ParsePrismCompositionResource(
            XElement resource,
            string name,
            XAttribute nameAttribute)
        {
            DirectiveExpressionLocation resourceLocation = new(resource, 0);
            List<PrismMemberSyntax> members = [];
            foreach (XAttribute attribute in resource.Attributes()
                .Where(attribute => !attribute.IsNamespaceDeclaration && attribute.Name.LocalName != "Name"))
            {
                DirectiveExpressionLocation location =
                    new(attribute, 0, Math.Max(1, attribute.Value.Length));
                members.Add(new PrismAssignmentSyntax(
                    attribute.Name.LocalName,
                    location,
                    ParsePrismValue(attribute.Value.Trim(), location),
                    location));
            }

            members.AddRange(ParsePrismMembers(
                PrismBodyContext.ResourceComposition,
                stopAtClosingBrace: false,
                resourceLocation));
            PrismContainerSyntax composition = new(
                PrismContainerKind.Composition,
                name,
                new DirectiveExpressionLocation(nameAttribute, 0, Math.Max(1, name.Length)),
                members,
                resourceLocation);
            return new PrismCompositionResourceSyntax(
                name,
                new DirectiveExpressionLocation(nameAttribute, 0, Math.Max(1, name.Length)),
                composition,
                resource,
                resourceLocation);
        }

        private PrismResourceApplicationSyntax ParsePrismResourceApplication(
            DirectiveExpressionLocation directiveLocation)
        {
            XObject referenceSource = CurrentSource;
            int referenceOffset = characterIndex;
            Read();
            string resourceName = ReadIdentifier();
            if (resourceName.Length == 0)
            {
                throw PrismInvalid(
                    "@prism resource references require an identifier after '$'.",
                    new DirectiveExpressionLocation(referenceSource, referenceOffset));
            }

            DirectiveExpressionLocation resourceLocation = new(
                referenceSource,
                referenceOffset,
                resourceName.Length + 1);
            List<PrismAssignmentSyntax> arguments = [];
            SkipWhitespace();
            if (!AtEnd && CurrentElement is null && Peek() == '(')
            {
                Read();
                SkipWhitespace();
                if (!AtEnd && CurrentElement is null && Peek() == ')')
                {
                    Read();
                }
                else
                {
                    while (true)
                    {
                        arguments.Add(ParsePrismArgument(directiveLocation));
                        SkipWhitespace();
                        if (AtEnd || CurrentElement is not null)
                        {
                            throw PrismInvalid(
                                "@prism resource arguments are missing their closing ')'.",
                                directiveLocation);
                        }

                        if (Peek() == ')')
                        {
                            Read();
                            break;
                        }

                        DirectiveExpressionLocation separatorLocation = LocationAtCurrentCharacter();
                        if (Read() != ',')
                        {
                            throw PrismInvalid(
                                "@prism resource arguments must be separated by ','.",
                                separatorLocation);
                        }

                        SkipWhitespace();
                    }
                }
            }

            SkipWhitespace();
            if (AtEnd || CurrentElement is not null || Read() != ';')
            {
                throw PrismInvalid("@prism resource references must end with ';'.", resourceLocation);
            }

            return new PrismResourceApplicationSyntax(
                resourceName,
                resourceLocation,
                arguments,
                directiveLocation);
        }

        private PrismAssignmentSyntax ParsePrismArgument(
            DirectiveExpressionLocation directiveLocation)
        {
            SkipWhitespace();
            if (AtEnd || CurrentElement is not null)
            {
                throw PrismInvalid("@prism resource arguments require a name.", directiveLocation);
            }

            XObject nameSource = CurrentSource;
            int nameOffset = characterIndex;
            string name = ReadPrismPath();
            if (!IsPrismPath(name))
            {
                throw PrismInvalid(
                    "Invalid @prism resource argument name '" + name + "'.",
                    new DirectiveExpressionLocation(nameSource, nameOffset, Math.Max(1, name.Length)));
            }

            DirectiveExpressionLocation nameLocation =
                new(nameSource, nameOffset, name.Length);
            SkipWhitespace();
            if (AtEnd || CurrentElement is not null || Read() != '=')
            {
                throw PrismInvalid(
                    "@prism resource argument '" + name + "' requires '='.",
                    nameLocation);
            }

            SkipWhitespace();
            PrismValueSyntax value = ReadPrismArgumentValue(directiveLocation);
            return new PrismAssignmentSyntax(name, nameLocation, value, nameLocation);
        }

        private PrismValueSyntax ReadPrismArgumentValue(
            DirectiveExpressionLocation directiveLocation)
        {
            if (AtEnd || CurrentElement is not null)
            {
                throw PrismInvalid("@prism resource arguments require a value.", directiveLocation);
            }

            XObject valueSource = CurrentSource;
            int valueOffset = characterIndex;
            StringBuilder value = new();
            int parentheses = 0;
            bool quoted = false;
            bool escaped = false;
            while (!AtEnd && CurrentElement is null)
            {
                char character = Peek();
                if (!quoted && parentheses == 0 && (character == ',' || character == ')'))
                {
                    break;
                }

                character = Read();
                value.Append(character);
                if (escaped)
                {
                    escaped = false;
                }
                else if (quoted && character == '\\')
                {
                    escaped = true;
                }
                else if (character == '"')
                {
                    quoted = !quoted;
                }
                else if (!quoted && character == '(')
                {
                    parentheses++;
                }
                else if (!quoted && character == ')')
                {
                    parentheses--;
                }
            }

            string text = value.ToString().TrimEnd();
            if (text.Length == 0)
            {
                throw PrismInvalid("@prism resource arguments require a value.", directiveLocation);
            }

            if (quoted || parentheses != 0)
            {
                throw PrismInvalid(
                    "@prism resource argument value has an unclosed delimiter.",
                    new DirectiveExpressionLocation(valueSource, valueOffset, Math.Max(1, text.Length)));
            }

            DirectiveExpressionLocation location =
                new(valueSource, valueOffset, text.Length);
            return ParsePrismValue(text, location);
        }

        private IReadOnlyList<PrismMemberSyntax> ParsePrismMembers(
            PrismBodyContext context,
            bool stopAtClosingBrace,
            DirectiveExpressionLocation ownerLocation)
        {
            List<PrismMemberSyntax> members = [];
            while (true)
            {
                SkipWhitespace();
                if (AtEnd)
                {
                    if (stopAtClosingBrace)
                    {
                        throw new PrismSyntaxParseException(
                            PrismMissingDelimiterDiagnostic,
                            ContextDisplay(context) + " is missing its closing '}'.",
                            ownerLocation);
                    }

                    return members;
                }

                if (CurrentElement is not null)
                {
                    throw PrismInvalid(
                        "XML elements are not legal children of " + ContextDisplay(context) + ".",
                        CurrentSource);
                }

                if (Peek() == '}')
                {
                    if (!stopAtClosingBrace)
                    {
                        throw PrismInvalid(
                            "Unexpected closing '}' in " + ContextDisplay(context) + ".",
                            LocationAtCurrentCharacter());
                    }

                    Read();
                    return members;
                }

                if (Peek() == '@')
                {
                    DirectiveExpressionLocation directiveLocation;
                    string directive = ReadPrismDirective(out directiveLocation);
                    if (!PrismMarkupLanguage.IsDirective(directive))
                    {
                        throw new PrismSyntaxParseException(
                            PrismUnknownDirectiveDiagnostic,
                            "Unknown Prism directive '" + directive + "'. Exactly eight Prism directives are supported.",
                            directiveLocation);
                    }

                    if (!IsPrismChildAllowed(context, directive) &&
                        !RequiresPrismBinderNestingValidation(context, directive))
                    {
                        throw PrismInvalid(
                            directive + " is not allowed directly inside " + ContextDisplay(context) + ".",
                            directiveLocation);
                    }

                    members.Add(ParsePrismMember(directive, directiveLocation));
                    continue;
                }

                if (LooksLikeAssignment())
                {
                    members.Add(ParsePrismAssignment());
                    continue;
                }

                DirectiveExpressionLocation tokenLocation = LocationAtCurrentCharacter();
                string token = ReadPrismToken();
                throw PrismInvalid(
                    "Expected a Prism property assignment or directive, but found '" + token + "'.",
                    tokenLocation);
            }
        }

        private PrismMemberSyntax ParsePrismMember(
            string directive,
            DirectiveExpressionLocation directiveLocation)
        {
            return directive switch
            {
                "@parameter" => ParsePrismParameter(directiveLocation),
                "@layer" => ParsePrismContainer(PrismContainerKind.Layer, PrismBodyContext.Layer, directiveLocation),
                "@group" => ParsePrismContainer(PrismContainerKind.Group, PrismBodyContext.Group, directiveLocation),
                "@backdrop" => ParsePrismContainer(PrismContainerKind.Backdrop, PrismBodyContext.Backdrop, directiveLocation),
                "@filter" => ParsePrismOperation(PrismOperationKind.Filter, PrismBodyContext.Filter, directiveLocation),
                "@style" => ParsePrismOperation(PrismOperationKind.Style, PrismBodyContext.Style, directiveLocation),
                "@mask" => ParsePrismOperation(PrismOperationKind.Mask, PrismBodyContext.Mask, directiveLocation),
                _ => throw PrismInvalid(directive + " cannot be nested in a Prism body.", directiveLocation)
            };
        }

        private PrismContainerSyntax ParsePrismContainer(
            PrismContainerKind kind,
            PrismBodyContext context,
            DirectiveExpressionLocation directiveLocation)
        {
            DirectiveHeader header = ReadPrismBlockHeader(directiveLocation);
            string name = header.Text.Trim();
            DirectiveExpressionLocation? nameLocation = null;
            if (name.Length > 0)
            {
                nameLocation = TrimmedLocation(header);
                if (!IsIdentifier(name))
                {
                    throw PrismInvalid(
                        ContextDisplay(context) + " name '" + name + "' is not a valid identifier.",
                        nameLocation);
                }
            }

            IReadOnlyList<PrismMemberSyntax> members =
                ParsePrismMembers(context, stopAtClosingBrace: true, directiveLocation);
            return new PrismContainerSyntax(
                kind,
                name.Length == 0 ? null : name,
                nameLocation,
                members,
                directiveLocation);
        }

        private PrismOperationSyntax ParsePrismOperation(
            PrismOperationKind kind,
            PrismBodyContext context,
            DirectiveExpressionLocation directiveLocation)
        {
            DirectiveHeader header = ReadPrismBlockHeader(directiveLocation);
            string typeName = header.Text.Trim();
            DirectiveExpressionLocation? typeLocation = null;
            if (kind == PrismOperationKind.Mask)
            {
                if (typeName.Length > 0)
                {
                    throw PrismInvalid("@mask accepts no type name.", TrimmedLocation(header));
                }
            }
            else
            {
                typeLocation = TrimmedLocation(header);
                if (!IsIdentifier(typeName))
                {
                    throw PrismInvalid(
                        (kind == PrismOperationKind.Filter ? "@filter" : "@style") +
                        " requires one bare type identifier.",
                        typeLocation);
                }
            }

            IReadOnlyList<PrismMemberSyntax> members =
                ParsePrismMembers(context, stopAtClosingBrace: true, directiveLocation);
            return new PrismOperationSyntax(
                kind,
                typeName.Length == 0 ? null : typeName,
                typeLocation,
                members,
                directiveLocation);
        }

        private PrismParameterSyntax ParsePrismParameter(
            DirectiveExpressionLocation directiveLocation)
        {
            DirectiveHeader header = ReadPrismHeaderUntilSemicolon(directiveLocation);
            string text = header.Text;
            int colon = FindTopLevelCharacter(text, ':');
            if (colon <= 0)
            {
                throw PrismInvalid(
                    "@parameter requires 'Name: Type' and an optional default value.",
                    directiveLocation);
            }

            string name = text.Substring(0, colon).Trim();
            int nameOffset = IndexOfTrimmed(text, 0, colon);
            DirectiveExpressionLocation nameLocation = new(
                header.Source,
                header.Offset + nameOffset,
                Math.Max(1, name.Length));
            if (!IsIdentifier(name))
            {
                throw PrismInvalid("@parameter name '" + name + "' is not a valid identifier.", nameLocation);
            }

            int equals = FindTopLevelCharacter(text, '=', colon + 1);
            int typeEnd = equals < 0 ? text.Length : equals;
            string typeName = text.Substring(colon + 1, typeEnd - colon - 1).Trim();
            int typeOffset = IndexOfTrimmed(text, colon + 1, typeEnd);
            DirectiveExpressionLocation typeLocation = new(
                header.Source,
                header.Offset + typeOffset,
                Math.Max(1, typeName.Length));
            if (!IsIdentifier(typeName))
            {
                throw PrismInvalid("@parameter requires a bare type identifier.", typeLocation);
            }

            PrismValueSyntax? defaultValue = null;
            if (equals >= 0)
            {
                string defaultText = text.Substring(equals + 1).Trim();
                int defaultOffset = IndexOfTrimmed(text, equals + 1, text.Length);
                DirectiveExpressionLocation defaultLocation = new(
                    header.Source,
                    header.Offset + defaultOffset,
                    Math.Max(1, defaultText.Length));
                if (defaultText.Length == 0)
                {
                    throw PrismInvalid("@parameter default value cannot be empty.", defaultLocation);
                }

                defaultValue = ParsePrismValue(defaultText, defaultLocation);
            }

            return new PrismParameterSyntax(
                name,
                nameLocation,
                typeName,
                typeLocation,
                defaultValue,
                directiveLocation);
        }

        private PrismAssignmentSyntax ParsePrismAssignment()
        {
            try
            {
                DirectiveAssignmentNode assignment = ParseAssignment();
                DirectiveExpressionLocation valueLocation = new(
                    assignment.ValueLocation.Source,
                    assignment.ValueLocation.Offset,
                    Math.Max(1, assignment.Value.Length));
                return new PrismAssignmentSyntax(
                    assignment.PropertyName,
                    assignment.PropertyLocation,
                    ParsePrismValue(assignment.Value, valueLocation),
                    assignment.PropertyLocation);
            }
            catch (DirectiveParseException ex)
            {
                throw PrismInvalid(ex.Message, ex.LocationSource);
            }
        }

        private DirectiveHeader ReadPrismBlockHeader(
            DirectiveExpressionLocation directiveLocation)
        {
            try
            {
                return ReadHeaderUntilBrace();
            }
            catch (DirectiveParseException)
            {
                throw PrismInvalid("Prism directives with bodies must be followed by '{'.", directiveLocation);
            }
        }

        private DirectiveHeader ReadPrismHeaderUntilSemicolon(
            DirectiveExpressionLocation directiveLocation)
        {
            XObject source = CurrentSource;
            int offset = characterIndex;
            StringBuilder builder = new();
            int parentheses = 0;
            bool quoted = false;
            bool escaped = false;
            while (!AtEnd && CurrentElement is null)
            {
                char character = Read();
                if (escaped)
                {
                    builder.Append(character);
                    escaped = false;
                    continue;
                }

                if (quoted && character == '\\')
                {
                    builder.Append(character);
                    escaped = true;
                    continue;
                }

                if (character == '"')
                {
                    quoted = !quoted;
                    builder.Append(character);
                    continue;
                }

                if (!quoted && character == '(')
                {
                    parentheses++;
                }
                else if (!quoted && character == ')')
                {
                    parentheses--;
                }
                else if (!quoted && parentheses == 0 && character == ';')
                {
                    return new DirectiveHeader(builder.ToString(), source, offset);
                }

                builder.Append(character);
            }

            throw PrismInvalid("@parameter must end with ';'.", directiveLocation);
        }

        private string ReadPrismDirective(out DirectiveExpressionLocation location)
        {
            XObject source = CurrentSource;
            int offset = characterIndex;
            StringBuilder directive = new();
            directive.Append(Read());
            while (!AtEnd &&
                CurrentElement is null &&
                (char.IsLetterOrDigit(Peek()) || Peek() == '_'))
            {
                directive.Append(Read());
            }

            string text = directive.ToString();
            location = new DirectiveExpressionLocation(source, offset, Math.Max(1, text.Length));
            return text;
        }

        private string ReadPrismPath()
        {
            StringBuilder path = new();
            while (!AtEnd &&
                CurrentElement is null &&
                (char.IsLetterOrDigit(Peek()) || Peek() is '_' or '.'))
            {
                path.Append(Read());
            }

            return path.ToString();
        }

        private string ReadPrismToken()
        {
            StringBuilder token = new();
            while (!AtEnd &&
                CurrentElement is null &&
                !char.IsWhiteSpace(Peek()) &&
                Peek() is not '{' and not '}')
            {
                token.Append(Read());
            }

            return token.Length == 0 ? Peek().ToString() : token.ToString();
        }

        private DirectiveExpressionLocation LocationAtCurrentCharacter()
        {
            return new DirectiveExpressionLocation(CurrentSource, characterIndex);
        }

        private static DirectiveExpressionLocation TrimmedLocation(DirectiveHeader header)
        {
            int start = 0;
            while (start < header.Text.Length && char.IsWhiteSpace(header.Text[start]))
            {
                start++;
            }

            int end = header.Text.Length;
            while (end > start && char.IsWhiteSpace(header.Text[end - 1]))
            {
                end--;
            }

            return new DirectiveExpressionLocation(
                header.Source,
                header.Offset + start,
                Math.Max(1, end - start));
        }

        private static int IndexOfTrimmed(string text, int start, int end)
        {
            while (start < end && char.IsWhiteSpace(text[start]))
            {
                start++;
            }

            return start;
        }

        private static bool IsPrismPath(string path)
        {
            return path.Length > 0 &&
                path.Split('.').All(IsIdentifier);
        }

        private static bool IsPrismChildAllowed(
            PrismBodyContext context,
            string directive)
        {
            return directive switch
            {
                "@parameter" => context is
                    PrismBodyContext.ResourceComposition or
                    PrismBodyContext.Layer or
                    PrismBodyContext.Group or
                    PrismBodyContext.Backdrop,
                "@layer" => context is
                    PrismBodyContext.InlineComposition or
                    PrismBodyContext.ResourceComposition or
                    PrismBodyContext.Group,
                "@group" => context is
                    PrismBodyContext.InlineComposition or
                    PrismBodyContext.ResourceComposition or
                    PrismBodyContext.Group,
                "@backdrop" => context is
                    PrismBodyContext.InlineComposition or
                    PrismBodyContext.ResourceComposition,
                "@filter" or "@style" or "@mask" => context is
                    PrismBodyContext.Layer or
                    PrismBodyContext.Group or
                    PrismBodyContext.Backdrop,
                _ => false
            };
        }

        private static bool RequiresPrismBinderNestingValidation(
            PrismBodyContext context,
            string directive)
        {
            return (context is PrismBodyContext.Layer or PrismBodyContext.Group or PrismBodyContext.Backdrop) &&
                (directive is "@layer" or "@group" or "@backdrop");
        }

        private static string ContextDisplay(PrismBodyContext context)
        {
            return context switch
            {
                PrismBodyContext.InlineComposition => "@prism",
                PrismBodyContext.ResourceComposition => "PrismComposition",
                PrismBodyContext.Layer => "@layer",
                PrismBodyContext.Group => "@group",
                PrismBodyContext.Backdrop => "@backdrop",
                PrismBodyContext.Filter => "@filter",
                PrismBodyContext.Style => "@style",
                PrismBodyContext.Mask => "@mask",
                _ => "Prism block"
            };
        }

        private static PrismSyntaxParseException PrismInvalid(
            string message,
            object? locationSource)
        {
            return new PrismSyntaxParseException(
                PrismInvalidSyntaxDiagnostic,
                message,
                locationSource);
        }
    }

    private sealed partial class GenerationScope
    {
        private void ReadPrismComposition(ResourceScope scope, XElement resource)
        {
            string? name = RequiredName(resource);
            XAttribute? nameAttribute = resource.Attribute("Name");
            if (name is null || nameAttribute is null)
            {
                return;
            }

            DirectiveCursor cursor = new(resource.Nodes());
            try
            {
                scope.PrismCompositions.Add(
                    cursor.ParsePrismCompositionResource(resource, name, nameAttribute));
            }
            catch (PrismSyntaxParseException ex)
            {
                ReportPrismSyntaxDiagnostic(
                    new PrismSyntaxDiagnostic(
                        ex.Descriptor,
                        ex.Message,
                        ex.LocationSource ?? resource));
            }
        }

        private bool ReportPrismSyntaxDiagnostics(DirectiveParseResult result)
        {
            foreach (PrismSyntaxDiagnostic diagnostic in result.PrismDiagnostics)
            {
                ReportPrismSyntaxDiagnostic(diagnostic);
            }

            return result.PrismDiagnostics.Count > 0;
        }

        private void ReportPrismSyntaxDiagnostic(PrismSyntaxDiagnostic diagnostic)
        {
            Report(
                diagnostic.Descriptor,
                diagnostic.LocationSource,
                Path.GetFileName(file.Path),
                diagnostic.Message);
        }
    }
}
