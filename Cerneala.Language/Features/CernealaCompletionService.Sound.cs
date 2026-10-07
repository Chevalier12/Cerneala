using Cerneala.Language.Semantics;
using Cerneala.Language.Syntax;
using Cerneala.Timbre.Catalog;

namespace Cerneala.Language.Features;

internal sealed partial class CernealaCompletionService
{
    private static readonly string[] SoundActionKeywords = ["@sound", "@pause", "@resume", "@seek"];

    private static readonly string[] SoundClipProperties = ["Source", "Volume", "Loop"];

    // SoundClip bodies and Sound action statements. Returns true when the
    // site belongs to the Sound language and no other completion applies.
    private static bool TryAddSoundCompletions(
        ICollection<CernealaCompletionItem> result,
        CompletionSite site,
        CernealaSemanticModel? model,
        ElementSyntax? element,
        string statement)
    {
        string elementName = element?.Name.Split(':').Last() ?? string.Empty;
        string lexicalName = FindUnclosedElementName(site.Source, site.Offset)?.Split(':').Last() ?? string.Empty;
        if (elementName == "SoundClip" || lexicalName == "SoundClip")
        {
            AddSoundClipBodyCompletions(result, site, model, elementName == "SoundClip" ? element : null, statement);
            return true;
        }

        string trimmed = statement.TrimStart();
        string? keyword = SoundActionKeywords.FirstOrDefault(candidate =>
            trimmed.StartsWith(candidate, StringComparison.Ordinal) &&
            (trimmed.Length == candidate.Length || char.IsWhiteSpace(trimmed[candidate.Length])));
        if (keyword is null || trimmed.Length == keyword.Length)
        {
            return false;
        }

        string rest = trimmed.Substring(keyword.Length).TrimStart();
        if (keyword != "@sound")
        {
            string[] words = rest.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries);
            bool trailingSpace = rest.Length > 0 && char.IsWhiteSpace(rest[rest.Length - 1]);
            int editing = trailingSpace ? words.Length : Math.Max(0, words.Length - 1);
            if (editing == 0)
            {
                AddSoundHandles(result, site, model, element);
            }
            else if (keyword == "@seek" && editing == 1)
            {
                Add(result, "to", "to ", site.WordSpan, CernealaCompletionItemKind.Keyword, "seek target", "00");
            }
            else if (keyword == "@seek" && editing == 2 && words[1] == "to")
            {
                foreach (string value in new[] { "0s", "500ms", "30s" })
                {
                    Add(result, value, value, site.WordSpan, CernealaCompletionItemKind.Value, "duration", "00");
                }
            }

            return true;
        }

        int asIndex = rest.LastIndexOf(" as ", StringComparison.Ordinal);
        if (asIndex >= 0 && rest.Substring(asIndex + 4).All(IsIdentifierCharacter))
        {
            AddSoundHandles(result, site, model, element);
            return true;
        }

        FunctionCall? call = FindFunctionCall(site.Source, site.Offset);
        if (call is not null && model is not null)
        {
            int open = statement.LastIndexOf('(');
            string argument = statement.Substring(open + 1).Split(',').Last();
            int equals = argument.IndexOf('=');
            if (equals >= 0)
            {
                if (argument.Substring(0, equals).Trim() == "Loop")
                {
                    AddBooleanValues(result, site);
                }

                return true;
            }

            foreach (CompletionParameterDefinition parameter in
                model.GetCompletionSoundParameters(element, call.Name) ?? Array.Empty<CompletionParameterDefinition>())
            {
                Add(result, parameter.Name, parameter.Name + " = ", site.WordSpan,
                    CernealaCompletionItemKind.Parameter, parameter.TypeName, "00");
            }

            return true;
        }

        if (FindReferenceSite(site.Source, site.Offset) is ReferenceSite reference && model is not null)
        {
            AddScopedReferenceCompletions(
                result,
                model.GetCompletionSources(element),
                reference.ReplacementSpan,
                source => source.Kind == "SoundClip");
            return true;
        }

        return false;
    }

    private static void AddSoundClipBodyCompletions(
        ICollection<CernealaCompletionItem> result,
        CompletionSite site,
        CernealaSemanticModel? model,
        ElementSyntax? clip,
        string statement)
    {
        string trimmed = statement.TrimStart();
        if (IsInsideDirective(site.Source, site.Offset, "@modifier"))
        {
            int modifierStart = site.Source.LastIndexOf("@modifier", Math.Max(0, site.Offset - 1), StringComparison.Ordinal);
            string header = site.Source.Substring(modifierStart + "@modifier".Length);
            string kind = new(header.TrimStart().TakeWhile(IsIdentifierCharacter).ToArray());
            int equals = trimmed.IndexOf('=');
            if (equals >= 0)
            {
                foreach (string parameter in model?.GetCompletionSoundClipParameters(clip, site.Offset) ?? Array.Empty<string>())
                {
                    Add(result, parameter, parameter, site.WordSpan, CernealaCompletionItemKind.Variable, "Sound parameter", "00");
                }

                return;
            }

            foreach (TimbreCatalogInput input in TimbreCatalog.GetModifierInputs(kind) ?? Array.Empty<TimbreCatalogInput>())
            {
                Add(result, input.Name, input.Name + " = ", site.WordSpan, CernealaCompletionItemKind.Property,
                    kind + "." + input.Name + " (" + input.Unit + ")", "00");
            }

            return;
        }

        if (trimmed.StartsWith("@modifier", StringComparison.Ordinal))
        {
            foreach (string modifier in TimbreCatalog.ModifierNames)
            {
                Add(result, modifier, modifier + " { }", site.WordSpan, CernealaCompletionItemKind.Function, "sound modifier", "00");
            }

            return;
        }

        if (trimmed.StartsWith("@parameter", StringComparison.Ordinal))
        {
            if (trimmed.IndexOf(':') >= 0 && trimmed.IndexOf('=') < 0)
            {
                Add(result, "float", "float = ", site.WordSpan, CernealaCompletionItemKind.Keyword, "sound parameter type", "00");
            }

            return;
        }

        if (site.WordPrefix.StartsWith("@", StringComparison.Ordinal))
        {
            Add(result, "@parameter", "@parameter Name: float = 0;", site.WordSpan, CernealaCompletionItemKind.Keyword, "Sound directive", "00");
            Add(result, "@modifier", "@modifier LowPass { }", site.WordSpan, CernealaCompletionItemKind.Keyword, "Sound directive", "00");
            return;
        }

        int assignment = trimmed.IndexOf('=');
        if (assignment >= 0)
        {
            if (trimmed.Substring(0, assignment).Trim() == "Loop")
            {
                AddBooleanValues(result, site);
            }

            return;
        }

        foreach (string property in SoundClipProperties)
        {
            Add(result, property, property + " = ", site.WordSpan, CernealaCompletionItemKind.Property, "SoundClip", "00");
        }
    }

    private static void AddSoundHandles(
        ICollection<CernealaCompletionItem> result,
        CompletionSite site,
        CernealaSemanticModel? model,
        ElementSyntax? element)
    {
        foreach (string handle in model?.GetCompletionMotionHandles(element, site.Offset) ?? Array.Empty<string>())
        {
            Add(result, handle, handle, site.WordSpan, CernealaCompletionItemKind.Variable, "handle", "00");
        }
    }

    private static void AddBooleanValues(ICollection<CernealaCompletionItem> result, CompletionSite site)
    {
        Add(result, "true", "true", site.WordSpan, CernealaCompletionItemKind.Value, "System.Boolean", "00");
        Add(result, "false", "false", site.WordSpan, CernealaCompletionItemKind.Value, "System.Boolean", "00");
    }
}
