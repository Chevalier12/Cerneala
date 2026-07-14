using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Linq;
using Microsoft.CodeAnalysis;

namespace Cerneala.SourceGen;

public sealed partial class UiMarkupGenerator
{
    private sealed partial class GenerationScope
    {
        private enum MarkupBindingMode
        {
            OneWay,
            TwoWay
        }

        private enum ParsedMarkupValueKind
        {
            Invalid,
            DirectBinding,
            Interpolation
        }

        private enum BindingSourceKind
        {
            DataPath,
            UiProperty,
            TemplatePartProperty,
            Object
        }

        private sealed class MarkupBindingToken
        {
            public MarkupBindingToken(string path, MarkupBindingMode mode, int modeOffset)
            {
                Path = path;
                Mode = mode;
                ModeOffset = modeOffset;
            }

            public string Path { get; }

            public MarkupBindingMode Mode { get; }

            public int ModeOffset { get; }
        }

        private abstract class MarkupInterpolationFragment
        {
        }

        private sealed class MarkupLiteralFragment : MarkupInterpolationFragment
        {
            public MarkupLiteralFragment(string text)
            {
                Text = text;
            }

            public string Text { get; }
        }

        private sealed class MarkupSourceFragment : MarkupInterpolationFragment
        {
            public MarkupSourceFragment(MarkupBindingToken token)
            {
                Token = token;
            }

            public MarkupBindingToken Token { get; }
        }

        private sealed class ParsedMarkupValue
        {
            private ParsedMarkupValue(
                ParsedMarkupValueKind kind,
                MarkupBindingToken? binding,
                IReadOnlyList<MarkupInterpolationFragment>? fragments)
            {
                Kind = kind;
                Binding = binding;
                Fragments = fragments ?? [];
            }

            public ParsedMarkupValueKind Kind { get; }

            public MarkupBindingToken? Binding { get; }

            public IReadOnlyList<MarkupInterpolationFragment> Fragments { get; }

            public static ParsedMarkupValue Invalid() => new(ParsedMarkupValueKind.Invalid, null, null);

            public static ParsedMarkupValue Direct(MarkupBindingToken binding) =>
                new(ParsedMarkupValueKind.DirectBinding, binding, null);

            public static ParsedMarkupValue Interpolation(IReadOnlyList<MarkupInterpolationFragment> fragments) =>
                new(ParsedMarkupValueKind.Interpolation, null, fragments);
        }

        private sealed class BindingResolutionContext
        {
            public BindingResolutionContext(
                string ownerVariable,
                string elementName,
                bool isRoot,
                TemplateEmissionContext? templateContext,
                bool validateClrObservability = false)
            {
                OwnerVariable = ownerVariable;
                ElementName = elementName;
                IsRoot = isRoot;
                TemplateContext = templateContext;
                ValidateClrObservability = validateClrObservability;
            }

            public string OwnerVariable { get; }

            public string ElementName { get; }

            public bool IsRoot { get; }

            public TemplateEmissionContext? TemplateContext { get; }

            public bool ValidateClrObservability { get; }
        }

        private sealed class DataPathSegmentDescriptor
        {
            public DataPathSegmentDescriptor(ITypeSymbol ownerType, IPropertySymbol property)
            {
                OwnerType = ownerType;
                Property = property;
            }

            public ITypeSymbol OwnerType { get; }

            public IPropertySymbol Property { get; }
        }

        private sealed class BindingSourceDescriptor
        {
            public BindingSourceDescriptor(
                string canonicalExpression,
                BindingSourceKind kind,
                ITypeSymbol valueType,
                string ownerCode,
                PropertySpec? property = null,
                IReadOnlyList<DataPathSegmentDescriptor>? dataSegments = null,
                string? partName = null,
                bool canWrite = false)
            {
                CanonicalExpression = canonicalExpression;
                Kind = kind;
                ValueType = valueType;
                OwnerCode = ownerCode;
                Property = property;
                DataSegments = dataSegments ?? [];
                PartName = partName;
                CanWrite = canWrite;
            }

            public string CanonicalExpression { get; }

            public BindingSourceKind Kind { get; }

            public ITypeSymbol ValueType { get; }

            public string OwnerCode { get; }

            public PropertySpec? Property { get; }

            public IReadOnlyList<DataPathSegmentDescriptor> DataSegments { get; }

            public string? PartName { get; }

            public bool CanWrite { get; }

            public string? SourceProjectionCode { get; set; }
        }

        private sealed class ResolvedMarkupValue
        {
            public ResolvedMarkupValue(
                ParsedMarkupValue parsed,
                IReadOnlyList<BindingSourceDescriptor> sources)
            {
                Parsed = parsed;
                Sources = sources;
            }

            public ParsedMarkupValue Parsed { get; }

            public IReadOnlyList<BindingSourceDescriptor> Sources { get; }
        }

        private ParsedMarkupValue? ParseMarkupBindingValue(
            string rawValue,
            bool assignment,
            bool stringTarget,
            object diagnosticSource)
        {
            string value = rawValue.Trim();
            bool quoted = assignment && value.Length >= 2 && value[0] == '"' && value[value.Length - 1] == '"';
            if (quoted)
            {
                string content = value.Substring(1, value.Length - 2);
                return stringTarget
                    ? ParseMarkupInterpolation(content, rejectPurePath: true, contentOffset: 1, diagnosticSource)
                    : null;
            }

            if (value.StartsWith("$", StringComparison.Ordinal))
            {
                BindingTokenParseResult tokenResult = ParseBindingToken(value, 0);
                if (tokenResult.Token is not null && tokenResult.Length == value.Length)
                {
                    return ParsedMarkupValue.Direct(tokenResult.Token);
                }

                if (LooksLikeBindingPath(value))
                {
                    Report(
                        InvalidBindingSource,
                        OffsetDiagnosticSource(diagnosticSource, tokenResult.ErrorOffset),
                        value,
                        tokenResult.Error ?? "A binding must be one unquoted path token with an optional final :OneWay or :TwoWay suffix.");
                    return ParsedMarkupValue.Invalid();
                }
            }

            if (!assignment && stringTarget)
            {
                return ParseMarkupInterpolation(value, rejectPurePath: false, contentOffset: 0, diagnosticSource);
            }

            return null;
        }

        private ParsedMarkupValue? ParseMarkupInterpolation(
            string value,
            bool rejectPurePath,
            int contentOffset,
            object diagnosticSource)
        {
            if (rejectPurePath)
            {
                BindingTokenParseResult purePath = ParseBindingToken(value, 0);
                if (purePath.Token is not null && purePath.Length == value.Length)
                {
                    Report(
                        InvalidBindingSource,
                        diagnosticSource,
                        value,
                        "A quoted value containing only one path is ambiguous; write the binding unquoted.");
                    return ParsedMarkupValue.Invalid();
                }
            }

            List<MarkupInterpolationFragment> fragments = [];
            StringBuilder literal = new();
            for (int index = 0; index < value.Length;)
            {
                if (value[index] == '\\' && index + 1 < value.Length && value[index + 1] == '$')
                {
                    literal.Append('$');
                    index += 2;
                    continue;
                }

                if (value[index] != '$')
                {
                    literal.Append(value[index]);
                    index++;
                    continue;
                }

                BindingTokenParseResult tokenResult = ParseBindingToken(value, index);
                if (tokenResult.Token is null || tokenResult.Length == 0)
                {
                    if (tokenResult.Error is not null)
                    {
                        Report(
                            InvalidBindingSource,
                            OffsetDiagnosticSource(diagnosticSource, contentOffset + index + tokenResult.ErrorOffset),
                            value.Substring(index, tokenResult.Length),
                            tokenResult.Error);
                        return ParsedMarkupValue.Invalid();
                    }

                    literal.Append('$');
                    index++;
                    continue;
                }

                if (tokenResult.Token.ModeOffset >= 0)
                {
                    Report(
                        InvalidBindingSource,
                        OffsetDiagnosticSource(
                            diagnosticSource,
                            contentOffset + index + tokenResult.Token.ModeOffset),
                        value.Substring(index, tokenResult.Length),
                        "Binding modes are not allowed inside an interpolated string.");
                    return ParsedMarkupValue.Invalid();
                }

                FlushLiteral(fragments, literal);
                fragments.Add(new MarkupSourceFragment(tokenResult.Token));
                index += tokenResult.Length;
            }

            FlushLiteral(fragments, literal);
            int sourceCount = fragments.OfType<MarkupSourceFragment>().Count();
            if (sourceCount == 0)
            {
                return null;
            }

            if (rejectPurePath && sourceCount == 1 &&
                fragments.All(fragment => fragment is MarkupSourceFragment ||
                    fragment is MarkupLiteralFragment literalFragment && literalFragment.Text.Length == 0))
            {
                Report(
                    InvalidBindingSource,
                    diagnosticSource,
                    value,
                    "A quoted value containing only one path is ambiguous; write the binding unquoted.");
                return ParsedMarkupValue.Invalid();
            }

            return ParsedMarkupValue.Interpolation(fragments);
        }

        private static void FlushLiteral(
            ICollection<MarkupInterpolationFragment> fragments,
            StringBuilder literal)
        {
            if (literal.Length == 0)
            {
                return;
            }

            fragments.Add(new MarkupLiteralFragment(literal.ToString()));
            literal.Clear();
        }

        private readonly struct BindingTokenParseResult
        {
            public BindingTokenParseResult(
                MarkupBindingToken? token,
                int length,
                string? error = null,
                int errorOffset = 0)
            {
                Token = token;
                Length = length;
                Error = error;
                ErrorOffset = errorOffset;
            }

            public MarkupBindingToken? Token { get; }

            public int Length { get; }

            public string? Error { get; }

            public int ErrorOffset { get; }
        }

        private static BindingTokenParseResult ParseBindingToken(string text, int start)
        {
            if (start >= text.Length || text[start] != '$')
            {
                return default;
            }

            int index = start + 1;
            while (index < text.Length && IsBindingPathCharacter(text[index]))
            {
                index++;
            }

            string path = text.Substring(start, index - start);
            if (!IsLexicallyValidBindingPath(path))
            {
                return default;
            }

            int modeOffset = -1;
            MarkupBindingMode mode = MarkupBindingMode.OneWay;
            if (index < text.Length && text[index] == ':')
            {
                modeOffset = index - start;
                int modeStart = ++index;
                while (index < text.Length && char.IsLetter(text[index]))
                {
                    index++;
                }

                string modeText = text.Substring(modeStart, index - modeStart);
                if (string.Equals(modeText, "OneWay", StringComparison.Ordinal))
                {
                    mode = MarkupBindingMode.OneWay;
                }
                else if (string.Equals(modeText, "TwoWay", StringComparison.Ordinal))
                {
                    mode = MarkupBindingMode.TwoWay;
                }
                else
                {
                    return new BindingTokenParseResult(
                        null,
                        index - start,
                        "Unknown binding mode '" + modeText + "'. Expected OneWay or TwoWay.",
                        modeOffset);
                }
            }

            return new BindingTokenParseResult(
                new MarkupBindingToken(path, mode, modeOffset),
                index - start);
        }

        private static object OffsetDiagnosticSource(object diagnosticSource, int offset)
        {
            if (offset <= 0)
            {
                return diagnosticSource;
            }

            return diagnosticSource is DirectiveExpressionLocation location
                ? new DirectiveExpressionLocation(location.Source, location.Offset + offset)
                : diagnosticSource;
        }

        private static bool IsBindingPathCharacter(char value)
        {
            return char.IsLetterOrDigit(value) || value is '_' or '.' or '$';
        }

        private static bool IsLexicallyValidBindingPath(string path)
        {
            if (path.Length < 2 || path[0] != '$' || path.EndsWith(".", StringComparison.Ordinal))
            {
                return false;
            }

            string[] segments = path.Split('.');
            if (!IsBindingIdentifier(segments[0].Substring(1)))
            {
                return false;
            }

            for (int index = 1; index < segments.Length; index++)
            {
                string segment = segments[index];
                if (segment.StartsWith("$", StringComparison.Ordinal))
                {
                    segment = segment.Substring(1);
                }

                if (!IsBindingIdentifier(segment))
                {
                    return false;
                }
            }

            return segments[0] == "$DataContext" || segments.Length >= 2;
        }

        private static bool IsBindingIdentifier(string value)
        {
            if (value.Length == 0 || !(char.IsLetter(value[0]) || value[0] == '_'))
            {
                return false;
            }

            return value.Skip(1).All(character => char.IsLetterOrDigit(character) || character == '_');
        }

        private static bool LooksLikeBindingPath(string value)
        {
            return value.StartsWith("$DataContext", StringComparison.Ordinal) ||
                value.StartsWith("$self.", StringComparison.Ordinal) ||
                value.StartsWith("$owner.", StringComparison.Ordinal) ||
                value.IndexOf('.') > 1;
        }

        private static string UnescapeMarkupDollar(string value)
        {
            return value.Replace("\\$", "$");
        }

        private ResolvedMarkupValue? ResolveMarkupValue(
            BindingResolutionContext resolutionContext,
            PropertySpec target,
            ParsedMarkupValue parsed,
            XObject source,
            object diagnosticSource)
        {
            if (parsed.Kind == ParsedMarkupValueKind.Invalid)
            {
                return null;
            }

            if (parsed.Kind == ParsedMarkupValueKind.DirectBinding)
            {
                MarkupBindingToken token = parsed.Binding!;
                BindingSourceDescriptor? descriptor = ResolveBindingSource(
                    resolutionContext,
                    token.Path,
                    source,
                    diagnosticSource);
                if (descriptor is null || !ValidateBindingCompatibility(
                    descriptor,
                    target,
                    token.Mode,
                    resolutionContext,
                    diagnosticSource))
                {
                    return null;
                }

                return new ResolvedMarkupValue(parsed, [descriptor]);
            }

            Dictionary<string, BindingSourceDescriptor> unique = new(StringComparer.Ordinal);
            foreach (MarkupSourceFragment fragment in parsed.Fragments.OfType<MarkupSourceFragment>())
            {
                BindingSourceDescriptor? descriptor = ResolveBindingSource(
                    resolutionContext,
                    fragment.Token.Path,
                    source,
                    diagnosticSource);
                if (descriptor is null)
                {
                    return null;
                }

                descriptor.SourceProjectionCode = StandardStringProjectionCode;
                if (!unique.ContainsKey(descriptor.CanonicalExpression))
                {
                    unique.Add(descriptor.CanonicalExpression, descriptor);
                }
            }

            return new ResolvedMarkupValue(parsed, unique.Values.ToArray());
        }

        private bool ValidateBindingCompatibility(
            BindingSourceDescriptor source,
            PropertySpec target,
            MarkupBindingMode mode,
            BindingResolutionContext resolutionContext,
            object diagnosticSource)
        {
            if (!target.Assignable)
            {
                Report(InvalidBindingSource, diagnosticSource, source.CanonicalExpression, "The target UI property is read-only.");
                return false;
            }

            if (source.Property is not null &&
                string.Equals(source.OwnerCode, resolutionContext.OwnerVariable, StringComparison.Ordinal) &&
                string.Equals(source.Property.PropertyCode, target.PropertyCode, StringComparison.Ordinal))
            {
                Report(InvalidBindingSource, diagnosticSource, source.CanonicalExpression, "A UI property cannot bind directly to itself.");
                return false;
            }

            bool sameType = SymbolEqualityComparer.Default.Equals(source.ValueType, target.ValueType);
            bool stringProjection = target.ValueType.SpecialType == SpecialType.System_String && mode == MarkupBindingMode.OneWay;
            if (!sameType && target.ValueType.SpecialType == SpecialType.System_String && mode == MarkupBindingMode.TwoWay)
            {
                Report(
                    InvalidBindingSource,
                    diagnosticSource,
                    source.CanonicalExpression,
                    "Automatic source-to-string projection is OneWay only. TwoWay requires a string source; inverse parsing requires an explicit converter, which markup bindings do not support yet.");
                return false;
            }

            if (!sameType && !stringProjection)
            {
                Report(
                    InvalidBindingSource,
                    diagnosticSource,
                    source.CanonicalExpression,
                    "Source type '" + source.ValueType.ToDisplayString() + "' is not compatible with target type '" +
                    target.ValueType.ToDisplayString() + "'.");
                return false;
            }

            if (mode == MarkupBindingMode.TwoWay && !source.CanWrite)
            {
                Report(InvalidBindingSource, diagnosticSource, source.CanonicalExpression, "TwoWay requires a writable source endpoint.");
                return false;
            }

            source.SourceProjectionCode = stringProjection ? StandardStringProjectionCode : null;
            return true;
        }

        private const string StandardStringProjectionCode =
            "value => global::Cerneala.UI.Markup.GeneratedMarkup.FormatStringValue(value)";

        private BindingSourceDescriptor? ResolveBindingSource(
            BindingResolutionContext resolutionContext,
            string expression,
            XObject source,
            object diagnosticSource)
        {
            if (expression.IndexOf(':') >= 0)
            {
                Report(InvalidBindingSource, diagnosticSource, expression, "Binding modes are not allowed in reactive conditions.");
                return null;
            }

            if (expression.StartsWith("$DataContext", StringComparison.Ordinal))
            {
                return ResolveDataBindingSource(resolutionContext, expression, diagnosticSource);
            }

            if (expression.IndexOf(".parts.", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return ResolveTemplatePartBindingSource(expression, source, diagnosticSource);
            }

            if (expression.StartsWith("$owner.", StringComparison.Ordinal))
            {
                if (resolutionContext.TemplateContext is null)
                {
                    Report(InvalidBindingSource, diagnosticSource, expression, "$owner is available only inside @template.");
                    return null;
                }

                string propertyName = expression.Substring("$owner.".Length);
                PropertySpec? ownerSpec = propertyName.IndexOf('.') < 0
                    ? FindPropertySpec(resolutionContext.TemplateContext.OwnerType, propertyName)
                    : null;
                if (ownerSpec is null)
                {
                    Report(InvalidBindingSource, diagnosticSource, expression, "No supported UI property with this name exists on the template owner.");
                    return null;
                }

                return UiPropertyDescriptor(
                    expression,
                    resolutionContext.TemplateContext.OwnerVariable,
                    ownerSpec);
            }

            if (expression.StartsWith("$self.", StringComparison.Ordinal))
            {
                string propertyName = expression.Substring("$self.".Length);
                PropertySpec? selfSpec = propertyName.IndexOf('.') < 0
                    ? FindPropertySpec(resolutionContext.ElementName, propertyName, resolutionContext.IsRoot)
                    : null;
                if (selfSpec is null)
                {
                    Report(
                        InvalidBindingSource,
                        diagnosticSource,
                        expression,
                        resolutionContext.TemplateContext is null
                            ? "No supported UI property with this name exists on the current element."
                            : "No supported UI property with this name exists on the current template element.");
                    return null;
                }

                return UiPropertyDescriptor(expression, resolutionContext.OwnerVariable, selfSpec);
            }

            if (expression.StartsWith("$", StringComparison.Ordinal))
            {
                string reference = expression.Substring(1);
                int separator = reference.IndexOf('.');
                if (separator >= 0)
                {
                    string referenceName = reference.Substring(0, separator);
                    string propertyName = reference.Substring(separator + 1);
                    if (propertyName.Length == 0 || propertyName.IndexOf('.') >= 0 ||
                        !TryResolveNamedElement(source, referenceName, out NamedElementReference owner))
                    {
                        Report(InvalidBindingSource, diagnosticSource, expression, "Unknown named element or invalid UI property path.");
                        return null;
                    }

                    if (!IsInSameBindingNameScope(source, owner.Element))
                    {
                        Report(InvalidBindingSource, diagnosticSource, expression, "The named element is outside the current name scope.");
                        return null;
                    }

                    INamedTypeSymbol? ownerType = ResolvePropertyOwnerType(
                        owner.Element.Name.LocalName,
                        ReferenceEquals(owner.Element, document.Root));
                    PropertySpec? namedSpec = ownerType is null ? null : FindPropertySpec(ownerType, propertyName);
                    if (namedSpec is null)
                    {
                        Report(InvalidBindingSource, diagnosticSource, expression, "No supported UI property with this name exists on the named element.");
                        return null;
                    }

                    return UiPropertyDescriptor(expression, owner.Code, namedSpec);
                }

                if (!TryResolveObjectSymbol(source, reference, out NamedSymbol symbol))
                {
                    Report(InvalidBindingSource, diagnosticSource, expression, "Unknown local reference.");
                    return null;
                }

                string? objectCode = symbol.Source switch
                {
                    NamedElementReference element => element.Code,
                    string variable => variable,
                    BrushResource brush => brush.Variable,
                    _ => null
                };
                if (objectCode is null)
                {
                    Report(InvalidBindingSource, diagnosticSource, expression, "The referenced symbol is not an observable object.");
                    return null;
                }

                return new BindingSourceDescriptor(
                    expression,
                    BindingSourceKind.Object,
                    compilation.GetSpecialType(SpecialType.System_Object),
                    objectCode);
            }

            PropertySpec? spec;
            string ownerCode;
            if (resolutionContext.TemplateContext is not null)
            {
                spec = FindPropertySpec(resolutionContext.TemplateContext.OwnerType, expression);
                ownerCode = resolutionContext.TemplateContext.OwnerVariable;
            }
            else
            {
                spec = FindPropertySpec(resolutionContext.ElementName, expression, resolutionContext.IsRoot);
                ownerCode = resolutionContext.OwnerVariable;
            }

            if (spec is null)
            {
                Report(
                    InvalidBindingSource,
                    diagnosticSource,
                    expression,
                    resolutionContext.TemplateContext is null
                        ? "No supported UI property with this name exists on the current element."
                        : "No supported UI property with this name exists on the template owner.");
                return null;
            }

            return UiPropertyDescriptor(expression, ownerCode, spec);
        }

        private BindingSourceDescriptor? ResolveDataBindingSource(
            BindingResolutionContext resolutionContext,
            string expression,
            object diagnosticSource)
        {
            if (dataType is null)
            {
                Report(InvalidBindingSource, diagnosticSource, expression, "DataType is required on the root element.");
                return null;
            }

            string suffix = expression.Substring("$DataContext".Length);
            if (suffix.Length > 0 && !suffix.StartsWith(".", StringComparison.Ordinal))
            {
                Report(InvalidBindingSource, diagnosticSource, expression, "$DataContext must be followed by dot-separated CLR properties.");
                return null;
            }

            string[] names = suffix.Length == 0 ? [] : suffix.Substring(1).Split('.');
            if (names.Any(name => name.Length == 0))
            {
                Report(InvalidBindingSource, diagnosticSource, expression, "A DataContext path contains an empty segment.");
                return null;
            }

            ITypeSymbol currentType = dataType;
            List<DataPathSegmentDescriptor> segments = [];
            foreach (string propertyName in names)
            {
                ITypeSymbol ownerType = UnwrapNullable(currentType);
                if (resolutionContext.ValidateClrObservability && !IsObservablePathOwner(ownerType))
                {
                    Report(
                        InvalidBindingSource,
                        diagnosticSource,
                        expression,
                        "CLR path owner '" + ownerType.ToDisplayString() + "' must implement INotifyPropertyChanged.");
                    return null;
                }

                IPropertySymbol? property = FindReadablePathProperty(ownerType, propertyName);
                if (property is null)
                {
                    Report(
                        InvalidBindingSource,
                        diagnosticSource,
                        expression,
                        "Readable property '" + propertyName + "' was not found on '" + ownerType.ToDisplayString() + "'.");
                    return null;
                }

                segments.Add(new DataPathSegmentDescriptor(ownerType, property));
                currentType = property.Type;
            }

            bool canWrite = segments.Count > 0 && IsWritablePathProperty(segments[segments.Count - 1].Property);
            return new BindingSourceDescriptor(
                expression,
                BindingSourceKind.DataPath,
                currentType,
                resolutionContext.OwnerVariable,
                dataSegments: segments,
                canWrite: canWrite);
        }

        private BindingSourceDescriptor? ResolveTemplatePartBindingSource(
            string expression,
            XObject source,
            object diagnosticSource)
        {
            string[] segments = expression.Split('.');
            if (segments.Length != 4 || !segments[0].StartsWith("$", StringComparison.Ordinal) ||
                segments[0].Length == 1 || segments[1] != "parts" ||
                !segments[2].StartsWith("$", StringComparison.Ordinal) || segments[2].Length == 1 ||
                segments[3].Length == 0)
            {
                Report(InvalidBindingSource, diagnosticSource, expression, "Template parts use $control.parts.$part.Property; 'parts' is lowercase.");
                return null;
            }

            string ownerName = segments[0].Substring(1);
            string partName = segments[2].Substring(1);
            if (!TryResolveNamedElement(source, ownerName, out NamedElementReference owner))
            {
                Report(InvalidBindingSource, diagnosticSource, expression, "Unknown named control.");
                return null;
            }

            INamedTypeSymbol? ownerType = ResolvePropertyOwnerType(
                owner.Element.Name.LocalName,
                ReferenceEquals(owner.Element, document.Root));
            if (!IsControlType(ownerType))
            {
                Report(InvalidBindingSource, diagnosticSource, expression, "Template parts can be accessed only on a Control.");
                return null;
            }

            DirectiveParseResult content = GetDirectiveContent(owner.Element, DirectiveContentKind.Elements | DirectiveContentKind.Templates);
            DirectiveTemplateNode? template = content.Nodes.OfType<DirectiveTemplateNode>().SingleOrDefault();
            if (template is null)
            {
                template = ResolveAspects(owner.Element).Select(aspect => aspect.Template).LastOrDefault(candidate => candidate is not null);
            }

            IReadOnlyDictionary<string, XElement>? parts = null;
            if (template is not null && !templateParts.TryGetValue(template, out parts))
            {
                parts = template.Root.DescendantsAndSelf()
                    .Where(element => !string.IsNullOrWhiteSpace(element.Attribute("Name")?.Value))
                    .GroupBy(element => element.Attribute("Name")!.Value.Trim(), StringComparer.Ordinal)
                    .Where(group => group.Count() == 1)
                    .ToDictionary(group => group.Key, group => group.Single(), StringComparer.Ordinal);
            }

            if (parts is null || !parts.TryGetValue(partName, out XElement? part))
            {
                Report(InvalidBindingSource, diagnosticSource, expression, "The named control template has no part named '" + partName + "'.");
                return null;
            }

            INamedTypeSymbol? partType = ResolveElementTypeSymbol(part.Name.LocalName);
            PropertySpec? spec = partType is null ? null : FindPropertySpec(partType, segments[3]);
            if (spec is null)
            {
                Report(InvalidBindingSource, diagnosticSource, expression, "No supported UI property with this name exists on the template part.");
                return null;
            }

            return new BindingSourceDescriptor(
                expression,
                BindingSourceKind.TemplatePartProperty,
                spec.ValueType,
                owner.Code,
                spec,
                partName: partName,
                canWrite: spec.Assignable);
        }

        private BindingSourceDescriptor UiPropertyDescriptor(
            string expression,
            string ownerCode,
            PropertySpec spec)
        {
            return new BindingSourceDescriptor(
                expression,
                BindingSourceKind.UiProperty,
                spec.ValueType,
                ownerCode,
                spec,
                canWrite: spec.Assignable);
        }

        private bool TryResolveNamedElement(
            XObject source,
            string name,
            out NamedElementReference reference)
        {
            if (symbols.TryGetValue(name, out NamedSymbol? symbol) &&
                symbol.Kind == NamedSymbolKind.Element &&
                symbol.Source is NamedElementReference existing)
            {
                reference = existing;
                return true;
            }

            XElement[] candidates = document.Root.DescendantsAndSelf()
                .Where(element => string.Equals(element.Attribute("Name")?.Value?.Trim(), name, StringComparison.Ordinal))
                .Where(element => !IsResourceElement(element) && !IsTemplatePartElement(element))
                .ToArray();
            if (candidates.Length != 1)
            {
                reference = null!;
                return false;
            }

            string code = userControlPair is null ? CreateIdentifier(name) : "this." + CreateIdentifier(name);
            reference = new NamedElementReference(code, candidates[0]);
            return true;
        }

        private bool IsInSameBindingNameScope(XObject source, XElement candidate)
        {
            XElement? sourceElement = source as XElement ?? source.Parent;
            return ReferenceEquals(
                FindContainingTemplateRoot(sourceElement),
                FindContainingTemplateRoot(candidate));
        }

        private XElement? FindContainingTemplateRoot(XElement? element)
        {
            if (element is null)
            {
                return null;
            }

            foreach (DirectiveTemplateNode template in templateParts.Keys)
            {
                if (ReferenceEquals(template.Root, element) ||
                    template.Root.Descendants().Any(descendant => ReferenceEquals(descendant, element)))
                {
                    return template.Root;
                }
            }

            foreach (XElement owner in document.Root.DescendantsAndSelf())
            {
                DirectiveParseResult content = GetDirectiveContent(
                    owner,
                    DirectiveContentKind.Elements | DirectiveContentKind.Templates);
                foreach (DirectiveTemplateNode template in content.Nodes.OfType<DirectiveTemplateNode>())
                {
                    if (ReferenceEquals(template.Root, element) ||
                        template.Root.Descendants().Any(descendant => ReferenceEquals(descendant, element)))
                    {
                        return template.Root;
                    }
                }
            }

            return null;
        }

        private static bool IsResourceElement(XElement element)
        {
            return element.Ancestors().Any(ancestor => ancestor.Name.LocalName.EndsWith(".Resources", StringComparison.Ordinal));
        }

        private bool IsTemplatePartElement(XElement element)
        {
            if (templateParts.Values.Any(parts => parts.Values.Any(part =>
                ReferenceEquals(part, element) || part.Descendants().Any(descendant => ReferenceEquals(descendant, element)))))
            {
                return true;
            }

            foreach (XElement owner in document.Root.DescendantsAndSelf())
            {
                DirectiveParseResult content = GetDirectiveContent(owner, DirectiveContentKind.Elements | DirectiveContentKind.Templates);
                foreach (DirectiveTemplateNode template in content.Nodes.OfType<DirectiveTemplateNode>())
                {
                    if (ReferenceEquals(template.Root, element) ||
                        template.Root.Descendants().Any(descendant => ReferenceEquals(descendant, element)))
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        private IPropertySymbol? FindReadablePathProperty(ITypeSymbol ownerType, string propertyName)
        {
            for (INamedTypeSymbol? current = ownerType as INamedTypeSymbol; current is not null; current = current.BaseType)
            {
                IPropertySymbol? property = current.GetMembers(propertyName)
                    .OfType<IPropertySymbol>()
                    .FirstOrDefault(candidate => !candidate.IsStatic && candidate.Parameters.Length == 0 &&
                        candidate.GetMethod is not null && IsAccessibleFromGeneratedCode(candidate.GetMethod));
                if (property is not null)
                {
                    return property;
                }
            }

            return null;
        }

        private bool IsWritablePathProperty(IPropertySymbol property)
        {
            return property.SetMethod is not null && IsAccessibleFromGeneratedCode(property.SetMethod);
        }

        private bool IsObservablePathOwner(ITypeSymbol ownerType)
        {
            INamedTypeSymbol? uiObjectType = compilation.GetTypeByMetadataName("Cerneala.UI.Core.UiObject");
            if (ownerType is INamedTypeSymbol namedOwner && uiObjectType is not null && IsOrDerivesFrom(namedOwner, uiObjectType))
            {
                return true;
            }

            INamedTypeSymbol? notifyType = compilation.GetTypeByMetadataName("System.ComponentModel.INotifyPropertyChanged");
            return notifyType is not null && ownerType is INamedTypeSymbol owner &&
                (SymbolEqualityComparer.Default.Equals(owner, notifyType) ||
                    owner.AllInterfaces.Any(candidate => SymbolEqualityComparer.Default.Equals(candidate, notifyType)));
        }

        private void EmitMarkupBinding(
            BindingResolutionContext resolutionContext,
            PropertySpec target,
            ResolvedMarkupValue resolved,
            string description)
        {
            Dictionary<string, ObservationEmission> observations = EmitResolvedObservations(resolved.Sources);
            string attachExpression;
            if (resolved.Parsed.Kind == ParsedMarkupValueKind.DirectBinding)
            {
                MarkupBindingToken token = resolved.Parsed.Binding!;
                BindingSourceDescriptor source = resolved.Sources[0];
                ObservationEmission observation = observations[source.CanonicalExpression];
                attachExpression =
                    "global::Cerneala.UI.Markup.GeneratedMarkup.AttachPropertyBinding<" + target.ValueTypeCode + ">(" +
                    resolutionContext.OwnerVariable + ", " + resolutionContext.OwnerVariable + ", " +
                    target.PropertyCode + ", " + observation.Name + ", " + BindingModeCode(token.Mode) + ", " +
                    ProjectionCode(source, target) + ", " + Literal(description) + ")";
            }
            else
            {
                attachExpression =
                    "global::Cerneala.UI.Markup.GeneratedMarkup.AttachInterpolatedStringBinding(" +
                    resolutionContext.OwnerVariable + ", " + resolutionContext.OwnerVariable + ", " +
                    target.PropertyCode + ", " + ObservationArrayCode(observations.Values) + ", () => " +
                    ComposeInterpolationCode(resolved.Parsed, observations) + ", " + Literal(description) + ")";
            }

            currentPostLines.Add(resolutionContext.TemplateContext is null
                ? attachExpression + ";"
                : resolutionContext.TemplateContext.ContextVariable + ".RegisterLifetime(" + attachExpression + ");");
        }

        private string EmitConditionalMarkupBinding(
            BindingResolutionContext resolutionContext,
            PropertySpec target,
            ResolvedMarkupValue resolved,
            string description)
        {
            Dictionary<string, ObservationEmission> observations = EmitResolvedObservations(resolved.Sources);
            if (resolved.Parsed.Kind == ParsedMarkupValueKind.DirectBinding)
            {
                MarkupBindingToken token = resolved.Parsed.Binding!;
                BindingSourceDescriptor source = resolved.Sources[0];
                ObservationEmission observation = observations[source.CanonicalExpression];
                return
                    "global::Cerneala.UI.Markup.GeneratedMarkup.CreateConditionalPropertyBinding<" + target.ValueTypeCode + ">(" +
                    resolutionContext.OwnerVariable + ", " + target.PropertyCode + ", " + observation.Name + ", " +
                    BindingModeCode(token.Mode) + ", " + ProjectionCode(source, target) + ", " + Literal(description) + ")";
            }

            return
                "global::Cerneala.UI.Markup.GeneratedMarkup.CreateConditionalInterpolatedStringBinding(" +
                resolutionContext.OwnerVariable + ", " + target.PropertyCode + ", " +
                ObservationArrayCode(observations.Values) + ", () => " +
                ComposeInterpolationCode(resolved.Parsed, observations) + ", " + Literal(description) + ")";
        }

        private Dictionary<string, ObservationEmission> EmitResolvedObservations(
            IEnumerable<BindingSourceDescriptor> sources)
        {
            Dictionary<string, ObservationEmission> observations = new(StringComparer.Ordinal);
            foreach (BindingSourceDescriptor source in sources)
            {
                string name = "observation" + nextReactiveId.ToString(System.Globalization.CultureInfo.InvariantCulture);
                nextReactiveId++;
                observations.Add(
                    source.CanonicalExpression,
                    EmitObservationDescriptor(currentPostLines, null, name, source));
            }

            return observations;
        }

        private static string BindingModeCode(MarkupBindingMode mode)
        {
            return mode == MarkupBindingMode.TwoWay
                ? "global::Cerneala.UI.Data.BindingMode.TwoWay"
                : "global::Cerneala.UI.Data.BindingMode.OneWay";
        }

        private static string ProjectionCode(BindingSourceDescriptor source, PropertySpec target)
        {
            return source.SourceProjectionCode ?? "value => (" + target.ValueTypeCode + ")value!";
        }

        private static string ObservationArrayCode(IEnumerable<ObservationEmission> observations)
        {
            return "new global::Cerneala.UI.Markup.MarkupObservation[] { " +
                string.Join(", ", observations.Select(observation => observation.Name)) + " }";
        }

        private string ComposeInterpolationCode(
            ParsedMarkupValue parsed,
            IReadOnlyDictionary<string, ObservationEmission> observations)
        {
            List<string> parts = [];
            foreach (MarkupInterpolationFragment fragment in parsed.Fragments)
            {
                switch (fragment)
                {
                    case MarkupLiteralFragment literal:
                        parts.Add(Literal(literal.Text));
                        break;
                    case MarkupSourceFragment source:
                        parts.Add(
                            "global::Cerneala.UI.Markup.GeneratedMarkup.FormatStringValue(" +
                            observations[source.Token.Path].Name + ".Value)");
                        break;
                }
            }

            return parts.Count switch
            {
                0 => "string.Empty",
                1 => parts[0],
                _ => "string.Concat(new string[] { " + string.Join(", ", parts) + " })"
            };
        }

        private BindingResolutionContext CreateBindingResolutionContext(
            string ownerVariable,
            string elementName,
            bool isRoot)
        {
            return new BindingResolutionContext(
                ownerVariable,
                elementName,
                isRoot,
                templateEmissionContexts.Count == 0 ? null : templateEmissionContexts.Peek(),
                validateClrObservability: true);
        }
    }
}
