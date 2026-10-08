using Cerneala.Language.Semantics;
using Cerneala.Language.Semantics.Symbols;
using Cerneala.Language.Syntax;
using Cerneala.Language.Text;

namespace Cerneala.Language.Features;

internal sealed partial class CernealaCompletionService
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
            ["PrismComposition"] = ["Name"],
            ["TimbreClip"] = ["Name"]
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

        if (parameters is null && model?.GetCompletionTimbreParameters(model.FindCompletionElement(offset), call.Name) is { } timbreArguments)
        {
            parameters = timbreArguments.Select(argument => argument.Name).ToArray();
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
            if (ownerType?.MetadataName == "Cerneala.UI.Controls.Tile" || IsStaticTileCollider(model, parent)) { return; }
            foreach (ILanguageMemberSymbol member in ownerType?.GetMembers() ?? Array.Empty<ILanguageMemberSymbol>())
            {
                if (member.Kind != LanguageMemberKind.Property || !member.CanRead || CernealaSemanticModel.IsRemovedTileMapSourceMember(ownerType, member.Name))
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
        ElementSyntax? colliderOwner = parent?.Kind == SyntaxKind.PropertyElement && parent.Name.Split('.').Last() == "Collider"
            ? model.GetCompletionParent(parent) : parent;
        ILanguageTypeSymbol? colliderOwnerType = model.GetCompletionElementType(colliderOwner);
        bool staticTileOwner = colliderOwnerType?.MetadataName == "Cerneala.UI.Controls.Tile";
        bool alreadyHasCollider = colliderOwner?.Children.OfType<ElementSyntax>().Any(child =>
            child.Kind == SyntaxKind.PropertyElement && child.Name.Split('.').Last() == "Collider"
                ? child.Children.OfType<ElementSyntax>().Any(nested =>
                    model.GetCompletionElementType(nested)?.IsOrDerivesFrom("Cerneala.UI.Controls.Collider2D") == true)
                : model.GetCompletionElementType(child)?.IsOrDerivesFrom("Cerneala.UI.Controls.Collider2D") == true) == true;
        bool acceptsColliders = !alreadyHasCollider &&
            (staticTileOwner || CernealaSemanticModel.IsLiveColliderOwner(colliderOwnerType));
        if (parentType?.MetadataName == "Cerneala.UI.Controls.TileMap2D" && parent?.Kind != SyntaxKind.PropertyElement)
        {
            Add(result, "Tile", ElementInsertion(site, "Tile"), site.WordSpan,
                CernealaCompletionItemKind.Element, "Image placement", "00", "Cerneala.UI.Controls.Tile");
            return;
        }
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
            bool collider = type.IsOrDerivesFrom("Cerneala.UI.Controls.Collider2D");
            if (!IsElementType(type) || !IsExpectedType(type, expected, expectedItem) ||
                collider && !acceptsColliders || staticTileOwner && !collider)
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
            foreach (string special in new[] { "Aspect", "SolidColorBrush", "LinearGradientBrush", "RadialGradientBrush", "ImageBrush", "DrawingBrush", "Tween", "Spring", "MotionClip", "PrismComposition", "TimbreClip" })
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
        if (type?.MetadataName == "Cerneala.UI.Controls.Tile")
        {
            foreach (ILanguageMemberSymbol member in type.GetMembers().Where(member =>
                member.Kind == LanguageMemberKind.Property && member.Name is "Image" or "X" or "Y" or "Width" or "Height"))
            {
                if (used.Contains(member.Name)) { continue; }
                Add(result, member.Name, member.Name + "=\"\"", site.WordSpan, CernealaCompletionItemKind.Property,
                    member.ValueTypeMetadataName, "00", type.MetadataName, member.Name);
            }
            foreach (string name in new[] { "ImageWidth", "ImageHeight" })
            {
                if (used.Contains(name)) { continue; }
                Add(result, name, name + "=\"\"", site.WordSpan, CernealaCompletionItemKind.Property,
                    "Positive intrinsic image dimension; source-catalog metadata", "00");
            }
            return;
        }
        if (IsStaticTileCollider(model, element))
        {
            foreach (ILanguageMemberSymbol member in type!.GetMembers().Where(member =>
                member.Kind == LanguageMemberKind.Property && CernealaSemanticModel.IsTileColliderAttribute(type, member.Name)))
            {
                if (used.Contains(member.Name)) { continue; }
                Add(result, member.Name, member.Name + "=\"\"", site.WordSpan, CernealaCompletionItemKind.Property,
                    member.ValueTypeMetadataName, "00", type.MetadataName, member.Name);
            }
            return;
        }
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
                    CernealaSemanticModel.IsRemovedTileMapSourceMember(type, member.Name) ||
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
        if (IsStaticTileCollider(model, element))
        {
            foreach (string value in GetMemberValues(FindTargetMember(model!, element, attributeName)))
            {
                Add(result, value, value, site.ValueWordSpan, CernealaCompletionItemKind.Value, "literal collider value", "00");
            }
            return;
        }
        if (model?.GetCompletionElementType(element)?.MetadataName == "Cerneala.UI.Controls.Tile")
        {
            if (attributeName == "Image")
            {
                AddTileImageCompletions(result, model, element, site.ValueWordSpan);
            }
            else if (attributeName is "X" or "Y" or "Width" or "Height" || CernealaSemanticModel.IsTileImageSizeAttribute(attributeName))
            {
                foreach (string value in CernealaSemanticModel.IsTileImageSizeAttribute(attributeName) ? new[] { "1", "32" } : new[] { "0", "1" })
                {
                    Add(result, value, value, site.ValueWordSpan, CernealaCompletionItemKind.Value, "float", "00");
                }
            }
            return;
        }
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
        if (IsStaticTileCollider(model, element)) { return; }
        if (model.GetCompletionElementType(element)?.MetadataName == "Cerneala.UI.Controls.Tile")
        {
            if (site.AttributeName == "Image" && !binding.IsMode && binding.Segments.Count == 1)
            {
                AddTileImageCompletions(result, model, element, binding.ReplacementSpan);
            }
            return;
        }
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

    private static string ElementInsertion(CompletionSite site, string label) =>
        site.TagHasClose ? label : label + " />";

    private static bool IsElementType(ILanguageTypeSymbol type) =>
        type.IsClass && !type.IsAbstract && type.HasAccessibleParameterlessConstructor &&
        type.Accessibility is LanguageAccessibility.Public or LanguageAccessibility.Internal &&
        type.IsOrDerivesFrom("Cerneala.UI.Elements.UIElement");

    private static bool IsStaticTileCollider(CernealaSemanticModel? model, ElementSyntax? element) =>
        model?.GetCompletionElementType(element)?.IsOrDerivesFrom("Cerneala.UI.Controls.Collider2D") == true &&
        model.GetCompletionElementType(model.GetCompletionParent(element!))?.MetadataName == "Cerneala.UI.Controls.Tile";

    private static void AddTileImageCompletions(ICollection<CernealaCompletionItem> result,
        CernealaSemanticModel model, ElementSyntax? element, TextSpan span)
    {
        foreach (CompletionScopedSymbol source in model.GetCompletionSources(element))
        {
            if (source.Type?.MetadataName != "Cerneala.UI.Resources.ImageResource") { continue; }
            Add(result, "$" + source.Name, "$" + source.Name, span,
                CernealaCompletionItemKind.Resource, "ImageResource", "00", source.Type.MetadataName);
        }
    }

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

    private static int Clamp(int value, int minimum, int maximum) =>
        value < minimum ? minimum : value > maximum ? maximum : value;
}
