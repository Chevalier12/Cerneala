using Cerneala.Language.Semantics;
using Cerneala.Language.Semantics.Symbols;
using Cerneala.Language.Syntax;
using Cerneala.Language.Text;

namespace Cerneala.Language.Features;

internal sealed class CernealaCompletionService
{
    private static readonly string[] StandaloneElements =
    [
        "Application", "Window", "UserControl", "Grid", "StackPanel", "Canvas", "Border", "Overlay",
        "Button", "CheckBox", "RadioButton", "ComboBox", "ComboBoxItem", "ListBox", "ListBoxItem",
        "TextBlock", "TextBox", "PasswordBox", "Label", "Image", "SvgImage", "ScrollViewer",
        "ItemsControl", "TabControl", "TabItem", "Slider", "ProgressBar", "ColorPicker", "InkCanvas"
    ];

    private static readonly string[] StandaloneAttributes =
    [
        "Name", "DataType", "Aspect", "Width", "Height", "MinWidth", "MinHeight", "MaxWidth", "MaxHeight",
        "Margin", "Padding", "HorizontalAlignment", "VerticalAlignment", "Visibility", "IsEnabled",
        "Background", "Foreground", "BorderBrush", "BorderThickness", "FontSize", "Text", "Content"
    ];

    private static readonly IReadOnlyDictionary<string, string[]> SpecialAttributes =
        new Dictionary<string, string[]>(StringComparer.Ordinal)
        {
            ["Aspect"] = ["Name", "TargetType"],
            ["ContentTemplate"] = ["Name", "DataType", "Key", "Priority"],
            ["Tween"] = ["Name", "Duration", "Delay", "Easing", "FillMode"],
            ["Spring"] = ["Name", "Stiffness", "Damping", "Mass", "RestSpeed", "RestDelta", "VelocityMode"],
            ["MotionClip"] = ["Name", "TargetType"],
            ["PrismComposition"] = ["Name"]
        };

    private static readonly string[] TargetPropertyDirectiveKeywords =
        ["@default", "@when", "@if", "@from", "@to", "@set", "@scroll"];

    public IReadOnlyList<CernealaCompletionItem> GetCompletions(
        CernealaDocument document,
        CernealaSemanticModel? model,
        int offset,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        string source = document.Text.ToString();
        offset = Clamp(offset, 0, source.Length);
        CompletionSite site = CompletionSite.Classify(source, offset);
        List<CernealaCompletionItem> result = new();
        ElementSyntax? element = model?.FindCompletionElement(offset);

        if (site.Kind == CompletionSiteKind.Element)
        {
            AddElementCompletions(result, site, model, element, cancellationToken);
        }
        else if (site.Kind == CompletionSiteKind.Attribute)
        {
            AddAttributeCompletions(result, site, model, element, cancellationToken);
        }
        else if (site.Kind == CompletionSiteKind.AttributeValue)
        {
            if (site.ValuePrefix.IndexOf('$') >= 0)
            {
                AddBindingCompletions(result, site, model, element);
            }
            else
            {
                AddValueCompletions(result, site, model, element, cancellationToken);
            }
        }
        else
        {
            AddDirectiveCompletions(result, site, model, element);
        }

        return result
            .GroupBy(item => (item.Label, item.InsertText, item.ReplacementSpan))
            .Select(group => group.OrderBy(item => item.SortText, StringComparer.Ordinal).First())
            .OrderBy(item => item.SortText, StringComparer.Ordinal)
            .ThenBy(item => item.Label, StringComparer.Ordinal)
            .ToArray();
    }

    public CernealaResolvedCompletion? Resolve(
        CernealaSemanticModel model,
        string typeMetadataName,
        string? memberName)
    {
        CernealaResolvedSymbol? symbol = model.ResolveCompletionSymbol(typeMetadataName, memberName);
        return symbol is null
            ? null
            : new CernealaResolvedCompletion(
                symbol.Signature,
                symbol.DeclaringType,
                CernealaDocumentation.Extract(symbol.DocumentationXml),
                symbol.IsDeprecated,
                symbol.AssemblyName);
    }

    public CernealaSignatureHelp? GetSignatureHelp(
        CernealaDocument document,
        int offset,
        CernealaSemanticModel? model = null)
    {
        string source = document.Text.ToString();
        offset = Clamp(offset, 0, source.Length);
        CompletionSite site = CompletionSite.Classify(source, offset);
        CernealaSignatureHelp? attributeValueHelp = GetAttributeValueSignatureHelp(site, model);
        if (attributeValueHelp is not null)
        {
            return attributeValueHelp;
        }

        FunctionCall? call = FindFunctionCall(source, offset);
        if (call is null)
        {
            return null;
        }

        IReadOnlyList<LanguageArgumentFact> motionArguments =
            CernealaLanguageFacts.FindMotionCallArguments(call.Name);
        string[]? parameters = motionArguments.Count > 0
            ? motionArguments.Select(argument => argument.Name).ToArray()
            : null;
        IReadOnlyList<LanguageArgumentFact> prismArguments = CernealaLanguageFacts.FindPrismProperties(call.Name);
        if (parameters is null && prismArguments.Count > 0)
        {
            parameters = prismArguments.Select(argument => argument.Name).ToArray();
        }

        IReadOnlyList<CompletionParameterDefinition> scopedArguments =
            model?.GetCompletionCallParameters(call.Name) ?? Array.Empty<CompletionParameterDefinition>();
        if (parameters is null && scopedArguments.Count > 0)
        {
            parameters = scopedArguments.Select(argument => argument.Name).ToArray();
        }

        if (parameters is null)
        {
            return null;
        }

        int activeParameter = Clamp(call.ActiveParameter, 0, Math.Max(0, parameters.Length - 1));
        CernealaSignature signature = new(
            call.Name + "(" + string.Join(", ", parameters) + ")",
            parameters.Select(parameter => new CernealaSignatureParameter(parameter)).ToArray());
        return new CernealaSignatureHelp([signature], 0, activeParameter);
    }

    private static CernealaSignatureHelp? GetAttributeValueSignatureHelp(
        CompletionSite site,
        CernealaSemanticModel? model)
    {
        if (site.Kind != CompletionSiteKind.AttributeValue || model is null ||
            site.ValuePrefix.Any(character => !char.IsWhiteSpace(character) &&
                !char.IsDigit(character) && character is not ('+' or '-' or '.' or ',')))
        {
            return null;
        }

        ElementSyntax? element = model.FindCompletionElement(site.Offset);
        ILanguageMemberSymbol? member = FindTargetMember(model, element, site.AttributeName);
        if (member is null ||
            !member.ValueTypeMetadataName.TrimEnd('?').EndsWith("Thickness", StringComparison.Ordinal))
        {
            return null;
        }

        CernealaSignature uniform = new(
            "Thickness(uniform)",
            [new CernealaSignatureParameter("uniform", "The same value for all four sides.")]);
        CernealaSignature components = new(
            "Thickness(left, top, right, bottom)",
            [
                new CernealaSignatureParameter("left", "Space on the left side."),
                new CernealaSignatureParameter("top", "Space on the top side."),
                new CernealaSignatureParameter("right", "Space on the right side."),
                new CernealaSignatureParameter("bottom", "Space on the bottom side.")
            ]);
        int commaCount = site.ValuePrefix.Count(character => character == ',');
        int activeSignature = commaCount == 0 ? 0 : 1;
        int activeParameter = activeSignature == 0 ? 0 : Clamp(commaCount, 0, 3);
        return new CernealaSignatureHelp([uniform, components], activeSignature, activeParameter);
    }

    private static void AddElementCompletions(
        ICollection<CernealaCompletionItem> result,
        CompletionSite site,
        CernealaSemanticModel? model,
        ElementSyntax? current,
        CancellationToken cancellationToken)
    {
        if (site.IsClosingTag)
        {
            string? name = FindUnclosedElementName(site.Source, site.Offset);
            if (name is not null)
            {
                Add(result, name, name, site.WordSpan, CernealaCompletionItemKind.Element, "closing element", "00");
            }

            return;
        }

        ElementSyntax? parent = current;
        if (current is not null && current.NameToken.Span.Start >= site.TagStart)
        {
            parent = model?.GetCompletionParent(current);
        }

        string? propertyOwnerPrefix = site.WordPrefix.Contains('.')
            ? site.WordPrefix.Substring(0, site.WordPrefix.LastIndexOf('.') + 1)
            : null;
        if (propertyOwnerPrefix is not null)
        {
            ILanguageTypeSymbol? ownerType = model?.GetCompletionElementType(parent);
            foreach (ILanguageMemberSymbol member in ownerType?.GetMembers() ?? Array.Empty<ILanguageMemberSymbol>())
            {
                if (member.Kind != LanguageMemberKind.Property || !member.CanRead)
                {
                    continue;
                }

                string label = propertyOwnerPrefix + member.Name;
                string insert = site.TagHasClose ? label : label + "></" + label + ">";
                Add(result, label, insert, site.WordSpan, CernealaCompletionItemKind.Property,
                    member.ValueTypeMetadataName, "01", ownerType!.MetadataName, member.Name);
            }

            return;
        }

        if (model is null)
        {
            foreach (string name in StandaloneElements)
            {
                Add(result, name, ElementInsertion(site, name), site.WordSpan,
                    CernealaCompletionItemKind.Element, "Cerneala element", "10");
            }

            return;
        }

        ILanguageTypeSymbol? parentType = model.GetCompletionElementType(parent);
        ILanguageTypeSymbol? expected = null;
        if (parent?.Kind == SyntaxKind.PropertyElement)
        {
            ElementSyntax? owner = model.GetCompletionParent(parent);
            ILanguageTypeSymbol? ownerType = model.GetCompletionElementType(owner);
            string propertyName = parent.Name.Split('.').Last();
            expected = ownerType?.GetMembers(propertyName)
                .FirstOrDefault(member => member.Kind == LanguageMemberKind.Property)?.ValueType;
        }
        else if (parentType is not null && model.GetCompletionContentProperty(parentType) is string contentProperty)
        {
            expected = parentType.GetMembers(contentProperty)
                .FirstOrDefault(member => member.Kind == LanguageMemberKind.Property)?.ValueType;
        }

        ILanguageTypeSymbol? expectedItem = expected?.CollectionElementType;
        bool rootSite = parent is null;
        foreach (ILanguageTypeSymbol type in model.CompletionCompilation.GetTypes())
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (!IsElementType(type) || !IsExpectedType(type, expected, expectedItem))
            {
                continue;
            }

            if (rootSite && type.Name is not ("Application" or "Window" or "UserControl") &&
                !type.IsOrDerivesFrom("Cerneala.UI.Controls.Window") &&
                !type.IsOrDerivesFrom("Cerneala.UI.Controls.UserControl"))
            {
                continue;
            }

            string? label = GetMarkupTypeName(type, model.GetCompletionAliases());
            if (label is null)
            {
                continue;
            }

            Add(result, label, ElementInsertion(site, label), site.WordSpan,
                CernealaCompletionItemKind.Element, type.MetadataName, "10", type.MetadataName);
        }

        if (parent?.Name.EndsWith(".Resources", StringComparison.Ordinal) == true)
        {
            foreach (string special in new[] { "Aspect", "SolidColorBrush", "LinearGradientBrush", "RadialGradientBrush", "ImageBrush", "DrawingBrush", "Tween", "Spring", "MotionClip", "PrismComposition" })
            {
                Add(result, special, ElementInsertion(site, special), site.WordSpan,
                    CernealaCompletionItemKind.Element, "resource", "00");
            }
        }
        else if (parent?.Name.EndsWith(".Templates", StringComparison.Ordinal) == true)
        {
            Add(result, "ContentTemplate", ElementInsertion(site, "ContentTemplate"), site.WordSpan,
                CernealaCompletionItemKind.Element, "content template", "00");
        }
    }

    private static void AddAttributeCompletions(
        ICollection<CernealaCompletionItem> result,
        CompletionSite site,
        CernealaSemanticModel? model,
        ElementSyntax? element,
        CancellationToken cancellationToken)
    {
        HashSet<string> used = ReadAttributeNames(site.Source, site.TagStart, site.Offset);
        ILanguageTypeSymbol? type = model?.GetCompletionElementType(element);
        IEnumerable<string> special = element is not null && SpecialAttributes.TryGetValue(element.Name.Split(':').Last(), out string[]? values)
            ? values
            : ["Name", "DataType", "Aspect"];
        foreach (string name in special)
        {
            if (!used.Contains(name))
            {
                Add(result, name, name + "=\"\"", site.WordSpan, CernealaCompletionItemKind.Property,
                    "Cerneala attribute", "00");
            }
        }

        if (site.IsRootTag && !used.Contains("xmlns"))
        {
            Add(result, "xmlns", "xmlns=\"clr-namespace:Cerneala.UI.Controls;assembly=Cerneala\"",
                site.WordSpan, CernealaCompletionItemKind.Property, "default CLR namespace", "00");
            Add(result, "xmlns:alias", "xmlns:alias=\"clr-namespace:\"", site.WordSpan,
                CernealaCompletionItemKind.Property, "CLR namespace alias", "00");
        }

        IEnumerable<ILanguageMemberSymbol> members = type?.GetMembers() ?? Array.Empty<ILanguageMemberSymbol>();
        if (model is null)
        {
            foreach (string name in StandaloneAttributes)
            {
                if (!used.Contains(name))
                {
                    Add(result, name, name + "=\"\"", site.WordSpan,
                        CernealaCompletionItemKind.Property, "Cerneala attribute", "10");
                }
            }
        }
        else
        {
            foreach (ILanguageMemberSymbol member in members)
            {
                cancellationToken.ThrowIfCancellationRequested();
                if (member.Kind is not (LanguageMemberKind.Property or LanguageMemberKind.Event) ||
                    member.IsStatic || used.Contains(member.Name) ||
                    member.Kind == LanguageMemberKind.Property && !member.CanWrite)
                {
                    continue;
                }

                CernealaCompletionItemKind kind = member.Kind == LanguageMemberKind.Event
                    ? CernealaCompletionItemKind.Event
                    : CernealaCompletionItemKind.Property;
                Add(result, member.Name, member.Name + "=\"\"", site.WordSpan, kind,
                    member.ValueTypeMetadataName, "10", type!.MetadataName, member.Name);
            }

            foreach (ILanguageTypeSymbol owner in model.CompletionCompilation.GetTypes().Where(candidate =>
                candidate.Namespace.StartsWith("Cerneala.UI", StringComparison.Ordinal)))
            {
                cancellationToken.ThrowIfCancellationRequested();
                foreach (ILanguageMemberSymbol setter in owner.GetMembers().Where(member =>
                    member.Kind == LanguageMemberKind.Method && member.IsStatic &&
                    member.Name.StartsWith("Set", StringComparison.Ordinal) && member.Name.Length > 3 &&
                    member.Parameters.Count >= 2))
                {
                    string label = owner.Name + "." + setter.Name.Substring(3);
                    if (used.Contains(label) || type is not null && !ParameterAccepts(setter.Parameters[0], type))
                    {
                        continue;
                    }

                    Add(result, label, label + "=\"\"", site.WordSpan,
                        CernealaCompletionItemKind.Property, setter.ValueTypeMetadataName, "20",
                        owner.MetadataName, setter.Name);
                }
            }
        }
    }

    private static void AddValueCompletions(
        ICollection<CernealaCompletionItem> result,
        CompletionSite site,
        CernealaSemanticModel? model,
        ElementSyntax? element,
        CancellationToken cancellationToken)
    {
        string attributeName = site.AttributeName ?? string.Empty;
        if (attributeName is "DataType" or "TargetType" || attributeName.StartsWith("xmlns", StringComparison.Ordinal))
        {
            AddTypeAndNamespaceValues(result, site, model, attributeName, cancellationToken);
            return;
        }

        if (attributeName == "Aspect")
        {
            foreach (CompletionScopedSymbol source in model?.GetCompletionSources(element) ?? Array.Empty<CompletionScopedSymbol>())
            {
                if (source.Kind == "Aspect" && (element is null || source.Type is null ||
                    model!.GetCompletionElementType(element)?.IsOrDerivesFrom(source.Type.MetadataName) == true))
                {
                    Add(result, "$" + source.Name, "$" + source.Name, site.ValueWordSpan,
                        CernealaCompletionItemKind.Resource, "Aspect", "00", source.Type?.MetadataName);
                }
            }
        }

        if (element is not null)
        {
            foreach (string value in GetSpecialValues(element.Name.Split(':').Last(), attributeName))
            {
                Add(result, value, value, site.ValueWordSpan, CernealaCompletionItemKind.Value,
                    attributeName, "00");
            }
        }

        ILanguageTypeSymbol? ownerType = model?.GetCompletionElementType(element);
        string memberName = attributeName.Contains('.') ? "Set" + attributeName.Split('.').Last() : attributeName;
        ILanguageMemberSymbol? member = ownerType?.GetMembers(memberName).FirstOrDefault(candidate =>
            candidate.Kind is LanguageMemberKind.Property or LanguageMemberKind.Event or LanguageMemberKind.Method);
        if (member is null && model is not null && attributeName.Contains('.'))
        {
            string ownerName = attributeName.Substring(0, attributeName.LastIndexOf('.'));
            member = model.CompletionCompilation.FindTypes(ownerName)
                .SelectMany(candidate => candidate.GetMembers(memberName))
                .FirstOrDefault();
        }

        foreach (string value in GetMemberValues(member))
        {
            Add(result, value, value, site.ValueWordSpan, CernealaCompletionItemKind.Value,
                member?.ValueTypeMetadataName ?? "value", "10");
        }

        foreach (CompletionScopedSymbol source in model?.GetCompletionSources(element) ?? Array.Empty<CompletionScopedSymbol>())
        {
            if (source.Kind is "Brush" or "MotionSpec" or "MotionClip" or "PrismComposition")
            {
                Add(result, "$" + source.Name, "$" + source.Name, site.ValueWordSpan,
                    CernealaCompletionItemKind.Resource, source.Kind, "20", source.Type?.MetadataName);
            }
        }
    }

    private static void AddTypeAndNamespaceValues(
        ICollection<CernealaCompletionItem> result,
        CompletionSite site,
        CernealaSemanticModel? model,
        string attributeName,
        CancellationToken cancellationToken)
    {
        if (model is null)
        {
            if (attributeName.StartsWith("xmlns", StringComparison.Ordinal))
            {
                Add(result, "Cerneala controls", "clr-namespace:Cerneala.UI.Controls;assembly=Cerneala",
                    site.ValueWordSpan, CernealaCompletionItemKind.Value, "CLR namespace", "00");
            }

            return;
        }

        if (attributeName.StartsWith("xmlns", StringComparison.Ordinal))
        {
            string namespacePrefix = GetClrNamespacePrefix(site.ValuePrefix);
            foreach ((string ns, string assembly) in model.CompletionCompilation.GetTypes()
                .Where(type => IsCompletableNamespace(type.Namespace, namespacePrefix))
                .Select(type => (type.Namespace, type.AssemblyName))
                .Distinct()
                .OrderBy(value => value.Namespace, StringComparer.Ordinal))
            {
                cancellationToken.ThrowIfCancellationRequested();
                string insertion = "clr-namespace:" + ns + (assembly.Length == 0 ? string.Empty : ";assembly=" + assembly);
                Add(result, ns, insertion, site.ValueWordSpan,
                    CernealaCompletionItemKind.Value, assembly, "10");
            }

            return;
        }

        foreach (ILanguageTypeSymbol type in model.CompletionCompilation.GetTypes())
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (type.Accessibility is not (LanguageAccessibility.Public or LanguageAccessibility.Internal) ||
                attributeName == "TargetType" && !type.IsOrDerivesFrom("Cerneala.UI.Elements.UIElement"))
            {
                continue;
            }

            string? label = GetMarkupTypeName(type, model.GetCompletionAliases());
            if (label is null)
            {
                continue;
            }

            Add(result, label, label, site.ValueWordSpan, CernealaCompletionItemKind.Type,
                type.MetadataName, "10", type.MetadataName);
        }
    }

    private static string GetClrNamespacePrefix(string valuePrefix)
    {
        const string marker = "clr-namespace:";
        if (!valuePrefix.StartsWith(marker, StringComparison.Ordinal))
        {
            return string.Empty;
        }

        string specification = valuePrefix.Substring(marker.Length);
        int assemblySeparator = specification.IndexOf(';');
        return (assemblySeparator < 0 ? specification : specification.Substring(0, assemblySeparator)).Trim();
    }

    private static bool IsCompletableNamespace(string namespaceName, string prefix) =>
        namespaceName.Length > 0 &&
        namespaceName[0] != '<' &&
        namespaceName.StartsWith(prefix, StringComparison.Ordinal);

    private static void AddBindingCompletions(
        ICollection<CernealaCompletionItem> result,
        CompletionSite site,
        CernealaSemanticModel? model,
        ElementSyntax? element)
    {
        if (model is null || site.Binding is null)
        {
            return;
        }

        BindingSite binding = site.Binding;
        if (binding.IsMode)
        {
            ILanguageMemberSymbol? target = FindTargetMember(model, element, site.AttributeName);
            if (!binding.IsDirect || target?.CanWrite != true)
            {
                return;
            }

            Add(result, "OneWay", "OneWay", binding.ReplacementSpan,
                CernealaCompletionItemKind.Value, "binding mode", "00");
            if (IsBindingEndpointWritable(model, element, binding.Segments))
            {
                Add(result, "TwoWay", "TwoWay", binding.ReplacementSpan,
                    CernealaCompletionItemKind.Value, "binding mode", "00");
            }

            return;
        }

        if (binding.Segments.Count == 1)
        {
            foreach (CompletionScopedSymbol source in model.GetCompletionSources(element))
            {
                string label = "$" + source.Name;
                Add(result, label, label, binding.ReplacementSpan,
                    source.Kind == "element" ? CernealaCompletionItemKind.Variable : CernealaCompletionItemKind.Resource,
                    source.Type?.MetadataName ?? source.Kind, "00", source.Type?.MetadataName);
            }

            return;
        }

        string sourceName = binding.Segments[0].TrimStart('$');
        ILanguageTypeSymbol? currentType = model.GetCompletionBindingSourceType(element, sourceName);
        bool allowChain = sourceName == "DataContext";
        for (int index = 1; index < binding.Segments.Count - 1 && currentType is not null; index++)
        {
            if (!allowChain && index > 1)
            {
                return;
            }

            string segment = binding.Segments[index].TrimStart('$');
            currentType = currentType.GetMembers(segment)
                .FirstOrDefault(member => member.Kind == LanguageMemberKind.Property && member.CanRead)?.ValueType;
        }

        if (currentType is null || !allowChain && binding.Segments.Count > 2)
        {
            return;
        }

        foreach (ILanguageMemberSymbol member in currentType.GetMembers()
            .Where(member => member.Kind == LanguageMemberKind.Property && member.CanRead)
            .GroupBy(member => member.Name, StringComparer.Ordinal)
            .Select(group => group.First()))
        {
            Add(result, member.Name, member.Name, binding.ReplacementSpan,
                CernealaCompletionItemKind.Property, member.ValueTypeMetadataName, "10",
                currentType.MetadataName, member.Name);
        }
    }

    private static bool IsBindingEndpointWritable(
        CernealaSemanticModel model,
        ElementSyntax? element,
        IReadOnlyList<string> segments)
    {
        if (segments.Count < 2)
        {
            return false;
        }

        ILanguageTypeSymbol? current = model.GetCompletionBindingSourceType(element, segments[0]);
        ILanguageMemberSymbol? endpoint = null;
        for (int index = 1; index < segments.Count && current is not null; index++)
        {
            endpoint = current.GetMembers(segments[index].TrimStart('$'))
                .FirstOrDefault(member => member.Kind == LanguageMemberKind.Property && member.CanRead);
            current = endpoint?.ValueType;
        }

        return endpoint?.CanWrite == true;
    }

    private static void AddDirectiveCompletions(
        ICollection<CernealaCompletionItem> result,
        CompletionSite site,
        CernealaSemanticModel? model,
        ElementSyntax? element)
    {
        string statement = GetEmbeddedStatementPrefix(site.Source, site.Offset);
        if (IsMotionHandleCompletionSite(statement) && model is not null)
        {
            foreach (string handle in model.GetCompletionMotionHandles(element, site.Offset))
            {
                Add(result, handle, handle, site.WordSpan,
                    CernealaCompletionItemKind.Variable, "Motion handle", "00");
            }

            return;
        }

        ReferenceMemberSite? memberReference = FindReferenceMemberSite(site.Source, site.Offset);
        if (memberReference is not null && model is not null &&
            IsDirectiveReferenceContext(site.Source, site.Offset, statement, element))
        {
            AddDirectiveReferenceMemberCompletions(result, model, element, memberReference);
            return;
        }

        ReferenceSite? reference = FindReferenceSite(site.Source, site.Offset);
        if (reference is not null && model is not null &&
            IsDirectiveReferenceContext(site.Source, site.Offset, statement, element))
        {
            AddDirectiveReferenceCompletions(result, model, element, reference);
            return;
        }

        if (model is not null &&
            TryGetReactiveExpressionOperandContext(statement, out bool includeWhenValue))
        {
            ILanguageTypeSymbol? targetType = model.GetCompletionElementType(element);
            foreach (ILanguageMemberSymbol member in
                targetType?.GetMembers() ?? Array.Empty<ILanguageMemberSymbol>())
            {
                if (member.Kind == LanguageMemberKind.Property && member.CanRead)
                {
                    Add(result, member.Name, member.Name, site.WordSpan,
                        CernealaCompletionItemKind.Property, member.ValueTypeMetadataName, "00",
                        targetType!.MetadataName, member.Name);
                }
            }

            if (includeWhenValue)
            {
                Add(result, "value", "value", site.WordSpan,
                    CernealaCompletionItemKind.Variable, "Current @when value", "00");
            }

            return;
        }

        if (model is not null && IsOnEventNameCompletionSite(statement))
        {
            ILanguageTypeSymbol? targetType = model.GetCompletionElementType(element);
            foreach (ILanguageMemberSymbol member in
                targetType?.GetMembers() ?? Array.Empty<ILanguageMemberSymbol>())
            {
                if (member.Kind == LanguageMemberKind.Event && !member.IsStatic)
                {
                    Add(result, member.Name, member.Name, site.WordSpan,
                        CernealaCompletionItemKind.Event, member.ValueTypeMetadataName, "00",
                        targetType!.MetadataName, member.Name);
                }
            }

            return;
        }

        FunctionCall? call = FindFunctionCall(site.Source, site.Offset);
        if (call is not null)
        {
            IReadOnlyList<LanguageArgumentFact> motionArguments =
                CernealaLanguageFacts.FindMotionCallArguments(call.Name);
            if (call.ActiveParameter < motionArguments.Count)
            {
                LanguageArgumentFact activeArgument = motionArguments[call.ActiveParameter];
                foreach (string value in activeArgument.AllowedValues.Distinct(StringComparer.Ordinal))
                {
                    Add(result, value, value, site.WordSpan,
                        CernealaCompletionItemKind.Value, activeArgument.ValueType, "00");
                }
            }

            IReadOnlyList<LanguageArgumentFact> arguments = CernealaLanguageFacts.FindPrismProperties(call.Name);
            foreach (LanguageArgumentFact argument in arguments)
            {
                Add(result, argument.Name, argument.Name + ": ", site.WordSpan,
                    CernealaCompletionItemKind.Parameter, argument.ValueType, argument.Required ? "00" : "10");
            }

            IEnumerable<string> allowedValues = call.ActiveParameter < arguments.Count
                ? arguments[call.ActiveParameter].AllowedValues
                : Array.Empty<string>();
            foreach (string value in allowedValues.Distinct(StringComparer.Ordinal))
            {
                Add(result, value, value, site.WordSpan, CernealaCompletionItemKind.Value, "Prism symbol", "20");
            }


            foreach (CompletionParameterDefinition argument in
                model?.GetCompletionCallParameters(call.Name) ?? Array.Empty<CompletionParameterDefinition>())
            {
                Add(result, argument.Name, argument.Name + ": ", site.WordSpan,
                    CernealaCompletionItemKind.Parameter, argument.TypeName, argument.Required ? "00" : "10");
            }

            return;
        }

        if (TryAddPrismCompletions(result, site, element, statement))
        {
            return;
        }

        if (IsInsideDirective(site.Source, site.Offset, "@animate") &&
            !IsInsideDirective(site.Source, site.Offset, "@from") &&
            !IsInsideDirective(site.Source, site.Offset, "@to"))
        {
            int equals = statement.LastIndexOf('=');
            if (equals >= 0)
            {
                string optionName = statement.Substring(0, equals).Trim().Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries).LastOrDefault() ?? string.Empty;
                LanguageArgumentFact? option = CernealaLanguageFacts.MotionOptions.FirstOrDefault(candidate => candidate.Name == optionName);
                foreach (string value in option?.AllowedValues ?? Array.Empty<string>())
                {
                    Add(result, value, value, site.WordSpan,
                        CernealaCompletionItemKind.Value, option!.ValueType, "00");
                }

                return;
            }

            int animateStart = site.Source.LastIndexOf("@animate", Math.Max(0, site.Offset - 1), StringComparison.Ordinal);
            int bodyStart = animateStart < 0 ? -1 : site.Source.IndexOf('{', animateStart + "@animate".Length);
            string bodyPrefix = bodyStart < 0 || bodyStart >= site.Offset
                ? string.Empty
                : site.Source.Substring(bodyStart + 1, site.Offset - bodyStart - 1);
            foreach (LanguageArgumentFact option in CernealaLanguageFacts.MotionOptions.Where(candidate =>
                bodyPrefix.IndexOf(candidate.Name + " =", StringComparison.Ordinal) < 0))
            {
                Add(result, option.Name, option.Name + " = ", site.WordSpan,
                    CernealaCompletionItemKind.Parameter, option.ValueType, "00");
            }

            return;
        }

        if (model is not null &&
            !site.WordPrefix.StartsWith("@", StringComparison.Ordinal) &&
            TargetPropertyDirectiveKeywords.Contains(
            FindInnermostDirectiveKeyword(site.Source, site.Offset),
            StringComparer.Ordinal))
        {
            ILanguageTypeSymbol? targetType = model.GetCompletionElementType(element);
            int equals = statement.LastIndexOf('=');
            if (equals < 0)
            {
                foreach (ILanguageMemberSymbol member in targetType?.GetMembers() ?? Array.Empty<ILanguageMemberSymbol>())
                {
                    if (member.Kind == LanguageMemberKind.Property && member.CanWrite)
                    {
                        Add(result, member.Name, member.Name + " = ", site.WordSpan,
                            CernealaCompletionItemKind.Property, member.ValueTypeMetadataName, "00",
                            targetType!.MetadataName, member.Name);
                    }
                }
            }
            else
            {
                string propertyName = statement.Substring(0, equals).Trim().Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries).LastOrDefault() ?? string.Empty;
                ILanguageMemberSymbol? member = targetType?.GetMembers(propertyName)
                    .FirstOrDefault(candidate => candidate.Kind == LanguageMemberKind.Property);
                foreach (string value in GetMemberValues(member))
                {
                    Add(result, value, value, site.WordSpan,
                        CernealaCompletionItemKind.Value, member!.ValueTypeMetadataName, "00");
                }
            }

            return;
        }

        if (statement.IndexOf("with ", StringComparison.Ordinal) >= 0 ||
            statement.IndexOf("spec ", StringComparison.Ordinal) >= 0)
        {
            foreach (string kind in CernealaLanguageFacts.MotionSpecKinds)
            {
                Add(result, kind, kind + "()", site.WordSpan,
                    CernealaCompletionItemKind.Function, "motion spec", "00");
            }

            foreach (CompletionScopedSymbol source in model?.GetCompletionSources(element) ?? Array.Empty<CompletionScopedSymbol>())
            {
                if (source.Kind == "MotionSpec")
                {
                    Add(result, "$" + source.Name, "$" + source.Name, site.WordSpan,
                        CernealaCompletionItemKind.Resource, "motion spec", "10");
                }
            }

            return;
        }

        if (!site.WordPrefix.StartsWith("@", StringComparison.Ordinal))
        {
            return;
        }

        string elementName = element?.Name.Split(':').Last() ?? string.Empty;
        ILanguageTypeSymbol? elementType = model?.GetCompletionElementType(element);
        bool supportsTemplateCollection = elementType is not null &&
            (elementType.IsOrDerivesFrom("Cerneala.UI.Controls.ItemsControl") ||
             elementType.IsOrDerivesFrom("Cerneala.UI.Controls.SceneItems2D"));
        IEnumerable<string> keywords;
        if (elementName == "PrismComposition" || IsInsideDirective(site.Source, site.Offset, "@prism"))
        {
            keywords = CernealaLanguageFacts.PrismDirectiveKeywords;
        }
        else if (elementName is "Aspect" or "MotionClip" ||
            IsInsideAnyDirective(site.Source, site.Offset, CernealaLanguageFacts.MotionDirectiveKeywords))
        {
            keywords = CernealaLanguageFacts.MotionDirectiveKeywords.Concat(["@default", "@template"]);
            if (IsInsideDirective(site.Source, site.Offset, "@animate"))
            {
                keywords = ["@from", "@to"];
            }
            else if (IsInsideDirective(site.Source, site.Offset, "@keyframes"))
            {
                keywords = ["@animate"];
            }
        }
        else
        {
            keywords = supportsTemplateCollection
                ? ["@templates", "@prism", "@run"]
                : ["@prism", "@run"];
        }

        foreach (string keyword in keywords.Distinct(StringComparer.Ordinal))
        {
            string insertion = DirectiveInsertion(keyword);
            Add(result, keyword, insertion, site.WordSpan,
                CernealaCompletionItemKind.Keyword, "Cerneala directive", "00");
        }
    }

    private static bool TryAddPrismCompletions(
        ICollection<CernealaCompletionItem> result,
        CompletionSite site,
        ElementSyntax? element,
        string statement)
    {
        PrismCompletionContext? context = FindPrismCompletionContext(site.Source, site.Offset);
        string elementName = element?.Name.Split(':').Last() ?? string.Empty;
        string lexicalElementName = FindUnclosedElementName(site.Source, site.Offset)?.Split(':').Last() ?? string.Empty;
        if (lexicalElementName.Length > 0)
        {
            elementName = lexicalElementName;
        }

        if (context is null && elementName != "PrismComposition")
        {
            return false;
        }

        if (context is null)
        {
            int markupEnd = statement.LastIndexOf('>');
            if (markupEnd >= 0)
            {
                statement = statement.Substring(markupEnd + 1);
            }
        }

        string ownerKind = context?.Kind ?? "composition";
        string? operationKind = FindPrismOperationSymbolCompletionKind(statement);
        if (operationKind is not null)
        {
            if (ownerKind is "layer" or "group")
            {
                foreach (string symbol in CernealaLanguageFacts.GetPrismSymbols(operationKind))
                {
                    Add(result, symbol, symbol, site.WordSpan,
                        CernealaCompletionItemKind.Value, "Prism " + operationKind, "00");
                }
            }

            return true;
        }

        IReadOnlyList<LanguageArgumentFact> properties = GetPrismCompletionProperties(context, ownerKind);
        int equals = statement.LastIndexOf('=');
        if (equals >= 0)
        {
            string propertyName = statement.Substring(0, equals).Trim()
                .Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries)
                .LastOrDefault() ?? string.Empty;
            LanguageArgumentFact? property = properties.FirstOrDefault(candidate => candidate.Name == propertyName);
            foreach (string value in GetPrismPropertyValues(property))
            {
                Add(result, value, value, site.WordSpan,
                    CernealaCompletionItemKind.Value, property!.ValueType, "00");
            }

            return true;
        }

        bool directivePrefix = site.WordPrefix.StartsWith("@", StringComparison.Ordinal);
        if (!directivePrefix)
        {
            foreach (LanguageArgumentFact property in properties)
            {
                Add(result, property.Name, property.Name + " = ", site.WordSpan,
                    CernealaCompletionItemKind.Property, property.ValueType, property.Required ? "00" : "10");
            }
        }

        foreach (string keyword in GetPrismChildDirectives(ownerKind))
        {
            Add(result, keyword, PrismDirectiveInsertion(keyword), site.WordSpan,
                CernealaCompletionItemKind.Keyword, "Prism directive", "00");
        }

        return true;
    }

    private static IReadOnlyList<LanguageArgumentFact> GetPrismCompletionProperties(
        PrismCompletionContext? context,
        string ownerKind) =>
        CernealaLanguageFacts.GetPrismProperties(ownerKind)
            .Concat(context?.Symbol is null
                ? Array.Empty<LanguageArgumentFact>()
                : CernealaLanguageFacts.GetPrismProperties(ownerKind, context.Symbol))
            .GroupBy(property => property.Name, StringComparer.Ordinal)
            .Select(group => group.Last())
            .ToArray();

    private static IEnumerable<string> GetPrismPropertyValues(LanguageArgumentFact? property)
    {
        if (property is null)
        {
            return Array.Empty<string>();
        }

        if (property.AllowedValues.Count > 0)
        {
            return property.AllowedValues.Distinct(StringComparer.Ordinal);
        }

        return property.ValueType is "bool" or "System.Boolean"
            ? ["true", "false"]
            : Array.Empty<string>();
    }

    private static IReadOnlyList<string> GetPrismChildDirectives(string ownerKind) => ownerKind switch
    {
        "composition" => ["@parameter", "@layer", "@group"],
        "layer" => ["@parameter", "@filter", "@style", "@mask"],
        "group" => ["@parameter", "@layer", "@group", "@filter", "@style", "@mask"],
        _ => Array.Empty<string>()
    };

    private static string PrismDirectiveInsertion(string keyword) => keyword switch
    {
        "@layer" => "@layer Name { }",
        "@group" => "@group Name { }",
        "@filter" or "@style" => keyword + " ",
        "@mask" => "@mask { }",
        _ => DirectiveInsertion(keyword)
    };

    private static string? FindPrismOperationSymbolCompletionKind(string statement)
    {
        string trimmed = statement.TrimStart();
        foreach (string kind in new[] { "filter", "style" })
        {
            string keyword = "@" + kind;
            if (trimmed.StartsWith(keyword, StringComparison.Ordinal) &&
                trimmed.Length > keyword.Length &&
                char.IsWhiteSpace(trimmed[keyword.Length]) &&
                trimmed.IndexOfAny(['{', '}', '=', ';', '(', ')']) < 0)
            {
                return kind;
            }
        }

        return null;
    }

    private static PrismCompletionContext? FindPrismCompletionContext(string source, int offset)
    {
        Stack<PrismCompletionContext?> blocks = new();
        bool quoted = false;
        char quote = '\0';
        for (int index = 0; index < offset; index++)
        {
            char character = source[index];
            if (quoted)
            {
                if (character == quote && (index == 0 || source[index - 1] != '\\'))
                {
                    quoted = false;
                }

                continue;
            }

            if (character is '\'' or '"')
            {
                quoted = true;
                quote = character;
            }
            else if (character == '{')
            {
                blocks.Push(CreatePrismCompletionContext(source, index));
            }
            else if (character == '}' && blocks.Count > 0)
            {
                blocks.Pop();
            }
        }

        return blocks.Count == 0 ? null : blocks.Peek();
    }

    private static PrismCompletionContext? CreatePrismCompletionContext(string source, int openingBrace)
    {
        string? keyword = FindDirectiveKeywordBeforeBody(source, openingBrace);
        string? kind = keyword switch
        {
            "@prism" => "composition",
            "@layer" => "layer",
            "@group" => "group",
            "@filter" => "filter",
            "@style" => "style",
            "@mask" => "mask",
            _ => null
        };
        if (kind is null)
        {
            return null;
        }

        string? symbol = null;
        if (kind is "filter" or "style")
        {
            int at = source.LastIndexOf('@', Math.Max(0, openingBrace - 1));
            int symbolStart = at + keyword!.Length;
            while (symbolStart < openingBrace && char.IsWhiteSpace(source[symbolStart]))
            {
                symbolStart++;
            }

            int symbolEnd = symbolStart;
            while (symbolEnd < openingBrace && IsIdentifierCharacter(source[symbolEnd]))
            {
                symbolEnd++;
            }

            if (symbolEnd > symbolStart)
            {
                symbol = source.Substring(symbolStart, symbolEnd - symbolStart);
            }
        }

        return new PrismCompletionContext(kind, symbol);
    }

    private static void AddDirectiveReferenceCompletions(
        ICollection<CernealaCompletionItem> result,
        CernealaSemanticModel model,
        ElementSyntax? element,
        ReferenceSite reference)
    {
        IReadOnlyList<CompletionScopedSymbol> sources = model.GetCompletionSources(element);
        AddScopedReferenceCompletions(result, sources, reference.ReplacementSpan, _ => true);
    }

    private static void AddDirectiveReferenceMemberCompletions(
        ICollection<CernealaCompletionItem> result,
        CernealaSemanticModel model,
        ElementSyntax? element,
        ReferenceMemberSite reference)
    {
        ILanguageTypeSymbol? currentType =
            model.GetCompletionBindingSourceType(element, reference.OwnerSegments[0]);
        for (int index = 1; index < reference.OwnerSegments.Count && currentType is not null; index++)
        {
            currentType = currentType.GetMembers(reference.OwnerSegments[index])
                .FirstOrDefault(member =>
                    member.Kind == LanguageMemberKind.Property && !member.IsStatic && member.CanRead)?.ValueType;
        }

        if (currentType is null)
        {
            return;
        }

        foreach (ILanguageMemberSymbol member in currentType.GetMembers()
            .Where(member => !member.IsStatic && IsReferenceMemberCompletionCandidate(member))
            .GroupBy(member => member.Name, StringComparer.Ordinal)
            .Select(group => group.First()))
        {
            CernealaCompletionItemKind kind = member.Kind switch
            {
                LanguageMemberKind.Event => CernealaCompletionItemKind.Event,
                LanguageMemberKind.Method => CernealaCompletionItemKind.Function,
                LanguageMemberKind.Field => CernealaCompletionItemKind.Variable,
                _ => CernealaCompletionItemKind.Property
            };
            Add(result, member.Name, member.Name, reference.ReplacementSpan, kind,
                member.ValueTypeMetadataName, "10", currentType.MetadataName, member.Name);
        }
    }

    private static bool IsReferenceMemberCompletionCandidate(ILanguageMemberSymbol member) =>
        member.Kind != LanguageMemberKind.Property || member.CanRead || member.CanWrite;

    private static void AddScopedReferenceCompletions(
        ICollection<CernealaCompletionItem> result,
        IEnumerable<CompletionScopedSymbol> sources,
        TextSpan replacementSpan,
        Func<CompletionScopedSymbol, bool> predicate)
    {
        foreach (CompletionScopedSymbol source in sources.Where(predicate))
        {
            string label = "$" + source.Name;
            CernealaCompletionItemKind kind = source.Kind is "binding" or "element"
                ? CernealaCompletionItemKind.Variable
                : CernealaCompletionItemKind.Resource;
            Add(result, label, label, replacementSpan, kind,
                source.Type?.MetadataName ?? source.Kind, "00", source.Type?.MetadataName);
        }
    }

    private static bool IsDirectiveReferenceContext(
        string source,
        int offset,
        string statement,
        ElementSyntax? element)
    {
        if (IsInsideBraceBody(source, offset) || statement.IndexOf('@') >= 0 ||
            element?.Name.Split(':').Last() is "Aspect" or "MotionClip" or "PrismComposition")
        {
            return true;
        }

        IEnumerable<string> directives = CernealaLanguageFacts.MotionDirectiveKeywords
            .Concat(CernealaLanguageFacts.PrismDirectiveKeywords)
            .Concat(["@default", "@template"]);
        return IsInsideAnyDirective(source, offset, directives);
    }

    private static bool IsInsideBraceBody(string source, int offset)
    {
        int closingDepth = 0;
        for (int index = offset - 1; index >= 0; index--)
        {
            if (source[index] == '}')
            {
                closingDepth++;
            }
            else if (source[index] == '{')
            {
                if (closingDepth == 0)
                {
                    return true;
                }

                closingDepth--;
            }
        }

        return false;
    }

    private static ReferenceSite? FindReferenceSite(string source, int offset)
    {
        int identifierStart = offset;
        while (identifierStart > 0 && IsIdentifierCharacter(source[identifierStart - 1]))
        {
            identifierStart--;
        }

        if (identifierStart <= 0 || source[identifierStart - 1] != '$')
        {
            return null;
        }

        int referenceStart = identifierStart - 1;
        return new ReferenceSite(new TextSpan(referenceStart, offset - referenceStart));
    }

    private static ReferenceMemberSite? FindReferenceMemberSite(string source, int offset)
    {
        int memberStart = offset;
        while (memberStart > 0 && IsIdentifierCharacter(source[memberStart - 1]))
        {
            memberStart--;
        }

        if (memberStart <= 0 || source[memberStart - 1] != '.')
        {
            return null;
        }

        int ownerEnd = memberStart - 1;
        int ownerStart = ownerEnd;
        while (ownerStart > 0 &&
            (IsIdentifierCharacter(source[ownerStart - 1]) || source[ownerStart - 1] == '.'))
        {
            ownerStart--;
        }

        if (ownerStart <= 0 || source[ownerStart - 1] != '$')
        {
            return null;
        }

        string[] ownerSegments = source.Substring(ownerStart, ownerEnd - ownerStart).Split('.');
        if (ownerSegments.Length == 0 || ownerSegments.Any(segment => segment.Length == 0))
        {
            return null;
        }

        return new ReferenceMemberSite(
            ownerSegments,
            new TextSpan(memberStart, offset - memberStart));
    }

    private static ILanguageMemberSymbol? FindTargetMember(
        CernealaSemanticModel model,
        ElementSyntax? element,
        string? attributeName)
    {
        if (attributeName is null)
        {
            return null;
        }

        ILanguageTypeSymbol? type = model.GetCompletionElementType(element);
        return type?.GetMembers(attributeName)
            .FirstOrDefault(member => member.Kind == LanguageMemberKind.Property);
    }

    private static IEnumerable<string> GetSpecialValues(string elementName, string attributeName) =>
        (elementName, attributeName) switch
        {
            ("Tween", "Duration") => ["100ms", "250ms", "1s"],
            ("Tween", "Delay") => ["0ms", "100ms", "0.5s"],
            ("Tween", "Easing") => ["Linear", "Standard", "Emphasized", "EaseIn", "EaseOut", "EaseInOut", "Sharp"],
            ("Tween", "FillMode") => ["None", "Backwards", "Forwards", "Both"],
            ("Spring", "VelocityMode") => ["Preserve", "Reset"],
            ("ContentTemplate", "Priority") => ["0", "1", "10"],
            _ => Array.Empty<string>()
        };

    private static IEnumerable<string> GetMemberValues(ILanguageMemberSymbol? member)
    {
        if (member is null)
        {
            return Array.Empty<string>();
        }

        if (member.EnumValues.Count > 0)
        {
            return member.EnumValues;
        }

        string type = member.ValueTypeMetadataName.TrimEnd('?');
        if (type is "bool" or "System.Boolean")
        {
            return ["true", "false"];
        }

        if (type is "int" or "System.Int32" or "float" or "System.Single" or "double" or "System.Double")
        {
            return ["0", "1"];
        }

        if (type.EndsWith("Thickness", StringComparison.Ordinal))
        {
            return ["0", "8", "8,4", "8,4,8,4"];
        }

        if (type.EndsWith("Color", StringComparison.Ordinal) || type.EndsWith("Brush", StringComparison.Ordinal))
        {
            return ["#FFFFFFFF", "#FF000000", "Transparent", "White", "Black"];
        }

        return Array.Empty<string>();
    }

    private static string DirectiveInsertion(string keyword) => keyword switch
    {
        "@set" => "@set $self.Property = value;",
        "@run" => "@run $Clip();",
        "@cancel" => "@cancel handle;",
        "@parameter" => "@parameter Name: float = 0;",
        "@from" or "@to" => keyword + " { }",
        _ => keyword + " { }"
    };

    private static string ElementInsertion(CompletionSite site, string label) =>
        site.TagHasClose ? label : label + " />";

    private static bool IsElementType(ILanguageTypeSymbol type) =>
        type.IsClass && !type.IsAbstract && type.HasAccessibleParameterlessConstructor &&
        type.Accessibility is LanguageAccessibility.Public or LanguageAccessibility.Internal &&
        type.IsOrDerivesFrom("Cerneala.UI.Elements.UIElement");

    private static bool IsExpectedType(
        ILanguageTypeSymbol type,
        ILanguageTypeSymbol? expected,
        ILanguageTypeSymbol? expectedItem)
    {
        if (expected is null || expected.MetadataName.TrimEnd('?') is "object" or "System.Object")
        {
            return true;
        }

        return type.IsOrDerivesFrom(expected.MetadataName.TrimEnd('?')) ||
            expectedItem is not null && type.IsOrDerivesFrom(expectedItem.MetadataName.TrimEnd('?'));
    }

    private static bool ParameterAccepts(LanguageParameterSymbol parameter, ILanguageTypeSymbol type) =>
        parameter.TypeMetadataName.TrimEnd('?') is "object" or "System.Object" ||
        type.IsOrDerivesFrom(parameter.TypeMetadataName.TrimEnd('?')) ||
        type.IsOrImplements(parameter.TypeMetadataName.TrimEnd('?'));

    private static string? GetMarkupTypeName(
        ILanguageTypeSymbol type,
        IReadOnlyList<CompletionNamespaceAlias> aliases)
    {
        if (type.Namespace == "Cerneala.UI" ||
            type.Namespace.StartsWith("Cerneala.UI.", StringComparison.Ordinal))
        {
            return type.Name;
        }

        CompletionNamespaceAlias? alias = aliases.FirstOrDefault(candidate =>
            string.Equals(candidate.Namespace, type.Namespace, StringComparison.Ordinal) &&
            (candidate.Assembly.Length == 0 || string.Equals(candidate.Assembly, type.AssemblyName, StringComparison.Ordinal)));
        return alias is null ? null : alias.Prefix + ":" + type.Name;
    }

    private static HashSet<string> ReadAttributeNames(string source, int tagStart, int offset)
    {
        HashSet<string> names = new(StringComparer.Ordinal);
        int position = tagStart + 1;
        while (position < offset && !char.IsWhiteSpace(source[position]))
        {
            position++;
        }

        while (position < offset)
        {
            while (position < offset && char.IsWhiteSpace(source[position]))
            {
                position++;
            }

            int start = position;
            while (position < offset && IsMarkupNameCharacter(source[position]))
            {
                position++;
            }

            if (position > start)
            {
                names.Add(source.Substring(start, position - start));
            }

            while (position < offset && source[position] is not ('\'' or '"'))
            {
                position++;
            }

            if (position < offset)
            {
                char quote = source[position++];
                while (position < offset && source[position] != quote)
                {
                    position++;
                }

                position = Math.Min(offset, position + 1);
            }
        }

        return names;
    }

    private static string? FindUnclosedElementName(string source, int offset)
    {
        List<string> stack = new();
        int position = 0;
        while (position < offset)
        {
            int opening = source.IndexOf('<', position);
            if (opening < 0 || opening >= offset)
            {
                break;
            }

            int end = source.IndexOf('>', opening + 1);
            if (end < 0 || end >= offset)
            {
                break;
            }

            string tag = source.Substring(opening + 1, end - opening - 1).Trim();
            if (tag.StartsWith("/", StringComparison.Ordinal))
            {
                if (stack.Count > 0)
                {
                    stack.RemoveAt(stack.Count - 1);
                }
            }
            else if (!tag.StartsWith("!", StringComparison.Ordinal) && !tag.StartsWith("?", StringComparison.Ordinal) &&
                !tag.EndsWith("/", StringComparison.Ordinal))
            {
                stack.Add(tag.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries)[0]);
            }

            position = end + 1;
        }

        return stack.LastOrDefault();
    }

    private static FunctionCall? FindFunctionCall(string source, int offset)
    {
        int depth = 0;
        int commas = 0;
        bool quoted = false;
        char quote = '\0';
        for (int index = offset - 1; index >= 0; index--)
        {
            char character = source[index];
            if (quoted)
            {
                if (character == quote && (index == 0 || source[index - 1] != '\\'))
                {
                    quoted = false;
                }

                continue;
            }

            if (character is '\'' or '"')
            {
                quoted = true;
                quote = character;
            }
            else if (character == ')')
            {
                depth++;
            }
            else if (character == '(')
            {
                if (depth > 0)
                {
                    depth--;
                    continue;
                }

                int end = index;
                int start = end;
                while (start > 0 && IsIdentifierCharacter(source[start - 1]))
                {
                    start--;
                }

                return end == start ? null : new FunctionCall(source.Substring(start, end - start), commas);
            }
            else if (character == ',' && depth == 0)
            {
                commas++;
            }
            else if (character is '{' or '}' or ';' && depth == 0)
            {
                return null;
            }
        }

        return null;
    }

    private static bool IsInsideAnyDirective(string source, int offset, IEnumerable<string> keywords) =>
        keywords.Any(keyword => IsInsideDirective(source, offset, keyword));

    private static string? FindInnermostDirectiveKeyword(string source, int offset)
    {
        Stack<string?> blocks = new();
        bool quoted = false;
        char quote = '\0';
        for (int index = 0; index < offset; index++)
        {
            char character = source[index];
            if (quoted)
            {
                if (character == quote && (index == 0 || source[index - 1] != '\\'))
                {
                    quoted = false;
                }

                continue;
            }

            if (character is '\'' or '"')
            {
                quoted = true;
                quote = character;
            }
            else if (character == '{')
            {
                blocks.Push(FindDirectiveKeywordBeforeBody(source, index));
            }
            else if (character == '}' && blocks.Count > 0)
            {
                blocks.Pop();
            }
        }

        return blocks.Count == 0 ? null : blocks.Peek();
    }

    private static string? FindDirectiveKeywordBeforeBody(string source, int openingBrace)
    {
        int at = source.LastIndexOf('@', Math.Max(0, openingBrace - 1));
        if (at < 0)
        {
            return null;
        }

        for (int index = at + 1; index < openingBrace; index++)
        {
            if (source[index] is '{' or '}' or ';' or '<' or '>')
            {
                return null;
            }
        }

        int end = at + 1;
        while (end < openingBrace && IsIdentifierCharacter(source[end]))
        {
            end++;
        }

        return end == at + 1 ? null : source.Substring(at, end - at);
    }

    private static string GetEmbeddedStatementPrefix(string source, int offset)
    {
        int start = offset;
        while (start > 0 && source[start - 1] is not ('\r' or '\n' or '{' or '}' or ';'))
        {
            start--;
        }

        return source.Substring(start, offset - start);
    }

    private static bool IsMotionHandleCompletionSite(string statement)
    {
        string trimmed = statement.TrimStart();
        if (trimmed.StartsWith("@cancel", StringComparison.Ordinal))
        {
            string suffix = trimmed.Substring("@cancel".Length).TrimStart();
            return suffix.All(IsIdentifierCharacter);
        }

        if (!trimmed.StartsWith("@run", StringComparison.Ordinal))
        {
            return false;
        }

        int separator = trimmed.LastIndexOf(" as ", StringComparison.Ordinal);
        return separator >= 0 && trimmed.Substring(separator + 4).All(IsIdentifierCharacter);
    }

    private static bool TryGetReactiveExpressionOperandContext(
        string statement,
        out bool includeWhenValue)
    {
        int whenStart = FindLastDirectiveKeyword(statement, "@when");
        int ifStart = FindLastDirectiveKeyword(statement, "@if");
        int keywordStart = Math.Max(whenStart, ifStart);
        string? keyword = keywordStart == whenStart && whenStart >= 0
            ? "@when"
            : ifStart >= 0 ? "@if" : null;
        includeWhenValue = keyword == "@if";
        return keyword is not null &&
            IsReactiveExpressionOperandSite(
                statement.Substring(keywordStart + keyword.Length));
    }

    private static bool IsOnEventNameCompletionSite(string statement)
    {
        int onStart = FindLastDirectiveKeyword(statement, "@on");
        if (onStart < 0)
        {
            return false;
        }

        string candidate = statement.Substring(onStart + "@on".Length).TrimStart();
        return candidate.All(IsIdentifierCharacter);
    }

    private static bool IsReactiveExpressionOperandSite(string expression)
    {
        string rightTrimmed = expression.TrimEnd();
        if (rightTrimmed.Length == 0)
        {
            return true;
        }

        if (rightTrimmed.EndsWith("(", StringComparison.Ordinal) ||
            EndsWithComparisonOperator(rightTrimmed))
        {
            return true;
        }

        int wordStart = rightTrimmed.Length;
        while (wordStart > 0 && IsIdentifierCharacter(rightTrimmed[wordStart - 1]))
        {
            wordStart--;
        }

        string lastWord = rightTrimmed.Substring(wordStart);
        bool hasTrailingWhitespace = rightTrimmed.Length < expression.Length;
        if (hasTrailingWhitespace)
        {
            return lastWord is "and" or "or";
        }

        string beforeWord = rightTrimmed.Substring(0, wordStart).TrimEnd();
        return beforeWord.Length == 0 ||
            beforeWord.EndsWith("(", StringComparison.Ordinal) ||
            EndsWithComparisonOperator(beforeWord) ||
            EndsWithLogicalOperator(beforeWord);
    }

    private static bool EndsWithComparisonOperator(string text) =>
        text.EndsWith("==", StringComparison.Ordinal) ||
        text.EndsWith("!=", StringComparison.Ordinal) ||
        text.EndsWith("<=", StringComparison.Ordinal) ||
        text.EndsWith(">=", StringComparison.Ordinal) ||
        text.EndsWith("<", StringComparison.Ordinal) ||
        text.EndsWith(">", StringComparison.Ordinal);

    private static bool EndsWithLogicalOperator(string text)
    {
        int end = text.Length;
        int start = end;
        while (start > 0 && IsIdentifierCharacter(text[start - 1]))
        {
            start--;
        }

        string word = text.Substring(start, end - start);
        return word is "and" or "or";
    }

    private static int FindLastDirectiveKeyword(string text, string keyword)
    {
        int start = text.LastIndexOf(keyword, StringComparison.Ordinal);
        if (start < 0)
        {
            return -1;
        }

        int end = start + keyword.Length;
        bool validStart = start == 0 || char.IsWhiteSpace(text[start - 1]) ||
            text[start - 1] is '>' or '{' or '}';
        bool validEnd = end == text.Length || char.IsWhiteSpace(text[end]);
        return validStart && validEnd ? start : -1;
    }

    private static bool IsInsideDirective(string source, int offset, string keyword)
    {
        int start = source.LastIndexOf(keyword, Math.Max(0, offset - 1), StringComparison.Ordinal);
        if (start < 0)
        {
            return false;
        }

        int opening = source.IndexOf('{', start + keyword.Length);
        if (opening < 0 || opening >= offset)
        {
            return false;
        }

        int depth = 1;
        for (int index = opening + 1; index < offset; index++)
        {
            depth += source[index] == '{' ? 1 : source[index] == '}' ? -1 : 0;
        }

        return depth > 0;
    }

    private static void Add(
        ICollection<CernealaCompletionItem> result,
        string label,
        string insertText,
        TextSpan replacementSpan,
        CernealaCompletionItemKind kind,
        string detail,
        string sortText,
        string? typeMetadataName = null,
        string? memberName = null) => result.Add(new CernealaCompletionItem(
            label,
            insertText,
            replacementSpan,
            kind,
            detail,
            sortText + label,
            typeMetadataName,
            memberName));

    private static bool IsMarkupNameCharacter(char character) =>
        char.IsLetterOrDigit(character) || character is '_' or ':' or '.' or '-';

    private static bool IsIdentifierCharacter(char character) =>
        char.IsLetterOrDigit(character) || character == '_';

    private sealed record FunctionCall(string Name, int ActiveParameter);

    private sealed record ReferenceSite(TextSpan ReplacementSpan);

    private sealed record ReferenceMemberSite(
        IReadOnlyList<string> OwnerSegments,
        TextSpan ReplacementSpan);

    private enum CompletionSiteKind
    {
        Element,
        Attribute,
        AttributeValue,
        Directive
    }

    private sealed class CompletionSite
    {
        private CompletionSite(
            string source,
            int offset,
            CompletionSiteKind kind,
            int tagStart,
            bool tagHasClose,
            bool isClosingTag,
            bool isRootTag,
            TextSpan wordSpan,
            string wordPrefix,
            int lineStart,
            string? attributeName,
            string valuePrefix,
            TextSpan valueWordSpan,
            BindingSite? binding)
        {
            Source = source;
            Offset = offset;
            Kind = kind;
            TagStart = tagStart;
            TagHasClose = tagHasClose;
            IsClosingTag = isClosingTag;
            IsRootTag = isRootTag;
            WordSpan = wordSpan;
            WordPrefix = wordPrefix;
            LineStart = lineStart;
            AttributeName = attributeName;
            ValuePrefix = valuePrefix;
            ValueWordSpan = valueWordSpan;
            Binding = binding;
        }

        public string Source { get; }
        public int Offset { get; }
        public CompletionSiteKind Kind { get; }
        public int TagStart { get; }
        public bool TagHasClose { get; }
        public bool IsClosingTag { get; }
        public bool IsRootTag { get; }
        public TextSpan WordSpan { get; }
        public string WordPrefix { get; }
        public int LineStart { get; }
        public string? AttributeName { get; }
        public string ValuePrefix { get; }
        public TextSpan ValueWordSpan { get; }
        public BindingSite? Binding { get; }

        public static CompletionSite Classify(string source, int offset)
        {
            int lineStart = source.LastIndexOf('\n', Math.Max(0, offset - 1));
            lineStart = lineStart < 0 ? 0 : lineStart + 1;
            int wordStart = offset;
            while (wordStart > lineStart && (IsMarkupNameCharacter(source[wordStart - 1]) || source[wordStart - 1] == '@'))
            {
                wordStart--;
            }

            TextSpan wordSpan = new(wordStart, offset - wordStart);
            string wordPrefix = source.Substring(wordStart, offset - wordStart);
            int tagStart = source.LastIndexOf('<', Math.Max(0, offset - 1));
            int previousEnd = source.LastIndexOf('>', Math.Max(0, offset - 1));
            bool insideTag = tagStart >= 0 && tagStart > previousEnd;
            if (!insideTag)
            {
                return new CompletionSite(source, offset, CompletionSiteKind.Directive, -1, false, false, false,
                    wordSpan, wordPrefix, lineStart, null, string.Empty, wordSpan, null);
            }

            int tagEnd = source.IndexOf('>', offset);
            bool tagHasClose = tagEnd >= 0;
            bool closing = tagStart + 1 < source.Length && source[tagStart + 1] == '/';
            int nameStart = tagStart + (closing ? 2 : 1);
            int nameEnd = nameStart;
            while (nameEnd < source.Length && IsMarkupNameCharacter(source[nameEnd]))
            {
                nameEnd++;
            }

            bool elementSite = offset <= nameEnd &&
                !source.Substring(nameStart, Math.Max(0, offset - nameStart)).Any(char.IsWhiteSpace);
            bool rootTag = source.Substring(0, tagStart).All(character => char.IsWhiteSpace(character) || character == '\uFEFF');
            if (elementSite)
            {
                TextSpan elementWord = new(nameStart, Math.Max(0, offset - nameStart));
                return new CompletionSite(source, offset, CompletionSiteKind.Element, tagStart, tagHasClose, closing, rootTag,
                    elementWord, source.Substring(elementWord.Start, elementWord.Length), lineStart, null, string.Empty,
                    elementWord, null);
            }

            bool quoted = false;
            char quote = '\0';
            int valueStart = -1;
            for (int index = nameEnd; index < offset; index++)
            {
                if (!quoted && source[index] is '\'' or '"')
                {
                    quoted = true;
                    quote = source[index];
                    valueStart = index + 1;
                }
                else if (quoted && source[index] == quote)
                {
                    quoted = false;
                    valueStart = -1;
                }
            }

            if (quoted && valueStart >= 0)
            {
                string? attributeName = FindAttributeName(source, valueStart - 1, tagStart);
                int valueWordStart = offset;
                while (valueWordStart > valueStart && !char.IsWhiteSpace(source[valueWordStart - 1]) &&
                    source[valueWordStart - 1] is not ('(' or ',' or '='))
                {
                    valueWordStart--;
                }

                TextSpan valueWordSpan = new(valueWordStart, offset - valueWordStart);
                string valuePrefix = source.Substring(valueStart, offset - valueStart);
                BindingSite? binding = BindingSite.Parse(source, valueStart, offset, valuePrefix);
                return new CompletionSite(source, offset, CompletionSiteKind.AttributeValue, tagStart, tagHasClose, false,
                    rootTag, wordSpan, wordPrefix, lineStart, attributeName, valuePrefix, valueWordSpan, binding);
            }

            int attributeStart = offset;
            while (attributeStart > nameEnd && IsMarkupNameCharacter(source[attributeStart - 1]))
            {
                attributeStart--;
            }

            TextSpan attributeWord = new(attributeStart, offset - attributeStart);
            return new CompletionSite(source, offset, CompletionSiteKind.Attribute, tagStart, tagHasClose, false, rootTag,
                attributeWord, source.Substring(attributeWord.Start, attributeWord.Length), lineStart, null,
                string.Empty, attributeWord, null);
        }

        private static string? FindAttributeName(string source, int quoteIndex, int tagStart)
        {
            int equals = quoteIndex - 1;
            while (equals > tagStart && char.IsWhiteSpace(source[equals]))
            {
                equals--;
            }

            if (equals <= tagStart || source[equals] != '=')
            {
                return null;
            }

            int end = equals;
            int start = end;
            while (start > tagStart && IsMarkupNameCharacter(source[start - 1]))
            {
                start--;
            }

            return source.Substring(start, end - start);
        }
    }

    private sealed class BindingSite
    {
        private BindingSite(
            IReadOnlyList<string> segments,
            TextSpan replacementSpan,
            bool isMode,
            bool isDirect)
        {
            Segments = segments;
            ReplacementSpan = replacementSpan;
            IsMode = isMode;
            IsDirect = isDirect;
        }

        public IReadOnlyList<string> Segments { get; }
        public TextSpan ReplacementSpan { get; }
        public bool IsMode { get; }
        public bool IsDirect { get; }

        public static BindingSite? Parse(string source, int valueStart, int offset, string valuePrefix)
        {
            int expressionStart = valuePrefix.LastIndexOf('$');
            while (expressionStart > 0 && valuePrefix[expressionStart - 1] == '.')
            {
                int previous = valuePrefix.LastIndexOf('$', expressionStart - 2);
                if (previous < 0)
                {
                    break;
                }

                expressionStart = previous;
            }

            if (expressionStart < 0)
            {
                return null;
            }

            string expression = valuePrefix.Substring(expressionStart);
            int firstNonWhitespace = 0;
            while (firstNonWhitespace < valuePrefix.Length && char.IsWhiteSpace(valuePrefix[firstNonWhitespace]))
            {
                firstNonWhitespace++;
            }

            bool isDirect = expressionStart == firstNonWhitespace;
            int mode = expression.LastIndexOf(':');
            if (mode >= 0 && expression.IndexOf(' ', mode) < 0)
            {
                TextSpan replacement = new(valueStart + expressionStart + mode + 1, expression.Length - mode - 1);
                string path = expression.Substring(0, mode);
                return new BindingSite(path.Split('.'), replacement, true, isDirect);
            }

            string[] segments = expression.Split('.');
            int segmentStart = expression.LastIndexOf('.') + 1;
            if (segments.Length == 1)
            {
                segmentStart = 0;
            }

            TextSpan span = new(valueStart + expressionStart + segmentStart, expression.Length - segmentStart);
            if (segments.Length == 1)
            {
                span = new TextSpan(valueStart + expressionStart, expression.Length);
            }

            return new BindingSite(segments, span, false, isDirect);
        }
    }

    private sealed class PrismCompletionContext
    {
        public PrismCompletionContext(string kind, string? symbol)
        {
            Kind = kind;
            Symbol = symbol;
        }

        public string Kind { get; }

        public string? Symbol { get; }
    }

    private static int Clamp(int value, int minimum, int maximum) =>
        value < minimum ? minimum : value > maximum ? maximum : value;
}
