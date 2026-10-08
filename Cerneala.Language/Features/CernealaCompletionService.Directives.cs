using Cerneala.Language.Semantics;
using Cerneala.Language.Semantics.Symbols;
using Cerneala.Language.Syntax;
using Cerneala.Language.Text;

namespace Cerneala.Language.Features;

internal sealed partial class CernealaCompletionService
{
    private static void AddDirectiveCompletions(
        ICollection<CernealaCompletionItem> result,
        CompletionSite site,
        CernealaSemanticModel? model,
        ElementSyntax? element)
    {
        if (model?.GetCompletionElementType(element)?.MetadataName == "Cerneala.UI.Controls.Tile" || IsStaticTileCollider(model, element)) { return; }
        string statement = GetEmbeddedStatementPrefix(site.Source, site.Offset);
        if (TryAddTimbreCompletions(result, site, model, element, statement))
        {
            return;
        }

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
                    if (member.Kind == LanguageMemberKind.Property && member.CanWrite && !CernealaSemanticModel.IsRemovedTileMapSourceMember(targetType, member.Name))
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
        bool aspectBody = elementName == "Aspect" || element?.Name.EndsWith(".Aspect", StringComparison.Ordinal) == true;
        IEnumerable<string> keywords;
        if (elementName == "PrismClip" || IsInsideDirectiveBlock(site.Source, site.Offset, "@prism"))
        {
            keywords = CernealaLanguageFacts.PrismDirectiveKeywords;
        }
        else if (aspectBody || elementName == "MotionClip" ||
            IsInsideAnyDirective(site.Source, site.Offset, CernealaLanguageFacts.MotionDirectiveKeywords))
        {
            keywords = CernealaLanguageFacts.MotionDirectiveKeywords.Concat(["@default", "@template"]);
            string? innermost = FindInnermostDirectiveKeyword(site.Source, site.Offset);
            if (IsInsideDirective(site.Source, site.Offset, "@animate"))
            {
                keywords = ["@from", "@to"];
            }
            else if (IsInsideDirective(site.Source, site.Offset, "@keyframes"))
            {
                keywords = ["@animate"];
            }
            else if (elementName != "MotionClip" && innermost is "@on" or "@when" or "@if")
            {
                keywords = keywords.Concat(TimbreCommandKeywords);
            }
            else if (aspectBody && innermost is null)
            {
                // The Aspect's attachments are written at the top of its body.
                keywords = keywords.Concat(["@timbre", "@prism"]);
            }
        }
        else
        {
            keywords = supportsTemplateCollection
                ? ["@templates", "@run"]
                : ["@run"];
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

        if (context is null && elementName != "PrismClip")
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
            element?.Name.Split(':').Last() is "Aspect" or "MotionClip" or "PrismClip")
        {
            return true;
        }

        IEnumerable<string> directives = CernealaLanguageFacts.MotionDirectiveKeywords
            .Concat(CernealaLanguageFacts.PrismDirectiveKeywords)
            .Concat(["@default", "@template"]);
        return IsInsideAnyDirective(source, offset, directives);
    }

    private static string DirectiveInsertion(string keyword) => keyword switch
    {
        "@set" => "@set $self.Property = value;",
        "@run" => "@run $Clip();",
        "@cancel" => "@cancel handle;",
        "@parameter" => "@parameter Name: float = 0;",
        "@timbre" => "@timbre $Clip;",
        "@play" => "@play $self.timbre.Sound;",
        "@stop" => "@stop $self.timbre.Sound;",
        "@pause" => "@pause $self.timbre.Sound;",
        "@resume" => "@resume $self.timbre.Sound;",
        "@seek" => "@seek $self.timbre.Sound to 0s;",
        "@from" or "@to" => keyword + " { }",
        _ => keyword + " { }"
    };

}
