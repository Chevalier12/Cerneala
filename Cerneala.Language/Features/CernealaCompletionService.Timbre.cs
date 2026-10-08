using System.Text.RegularExpressions;
using Cerneala.Language.Semantics;
using Cerneala.Language.Syntax;
using Cerneala.Language.Text;
using Cerneala.Timbre.Catalog;

namespace Cerneala.Language.Features;

internal sealed partial class CernealaCompletionService
{
    private static readonly string[] TimbreCommandKeywords = ["@play", "@stop", "@pause", "@resume", "@seek"];

    private static readonly string[] TimbreSoundProperties = ["Source", "Volume", "Loop", "AutoPlay"];

    // TimbreClip bodies, inline `@timbre { … }` blocks, `@timbre $Clip(…)` and
    // the Timbre commands. Returns true when the site belongs to the Timbre
    // language and no other completion applies.
    private static bool TryAddTimbreCompletions(
        ICollection<CernealaCompletionItem> result,
        CompletionSite site,
        CernealaSemanticModel? model,
        ElementSyntax? element,
        string statement)
    {
        // A statement at the start of an element's content begins after its tag.
        int markupEnd = statement.LastIndexOf('>');
        if (markupEnd >= 0 && statement.IndexOf('<') >= 0)
        {
            statement = statement.Substring(markupEnd + 1);
        }

        string elementName = element?.Name.Split(':').Last() ?? string.Empty;
        string lexicalName = FindUnclosedElementName(site.Source, site.Offset)?.Split(':').Last() ?? string.Empty;
        if (elementName == "TimbreClip" || lexicalName == "TimbreClip")
        {
            AddTimbreClipBodyCompletions(result, site, model, elementName == "TimbreClip" ? element : null, statement);
            return true;
        }

        if (IsInsideDirectiveBlock(site.Source, site.Offset, "@timbre"))
        {
            AddTimbreClipBodyCompletions(result, site, model, clip: null, statement);
            return true;
        }

        if (TryAddTimbreMotionTargetCompletions(result, site, model, element, statement))
        {
            return true;
        }

        string trimmed = statement.TrimStart();
        if (trimmed.StartsWith("@timbre", StringComparison.Ordinal) && trimmed.Length > "@timbre".Length)
        {
            return TryAddTimbreAttachmentCompletions(result, site, model, element, statement);
        }

        string? keyword = TimbreCommandKeywords.FirstOrDefault(candidate =>
            trimmed.StartsWith(candidate, StringComparison.Ordinal) &&
            (trimmed.Length == candidate.Length || char.IsWhiteSpace(trimmed[candidate.Length])));
        if (keyword is null || trimmed.Length == keyword.Length)
        {
            return false;
        }

        string rest = trimmed.Substring(keyword.Length).TrimStart();
        string[] words = rest.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries);
        bool trailingSpace = rest.Length > 0 && char.IsWhiteSpace(rest[rest.Length - 1]);
        int editing = trailingSpace ? words.Length : Math.Max(0, words.Length - 1);
        if (editing == 0)
        {
            foreach (string sound in model?.GetCompletionTimbreMotionSounds(element) ?? Array.Empty<string>())
            {
                string path = "$self.timbre." + sound;
                Add(result, path, path, site.WordSpan, CernealaCompletionItemKind.Variable, "Timbre sound", "00");
            }
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

    // `@timbre $|` completes TimbreClip resources and `@timbre $Clip(|`
    // the clip parameters it can set.
    private static bool TryAddTimbreAttachmentCompletions(
        ICollection<CernealaCompletionItem> result,
        CompletionSite site,
        CernealaSemanticModel? model,
        ElementSyntax? element,
        string statement)
    {
        FunctionCall? call = FindFunctionCall(site.Source, site.Offset);
        if (call is not null && model is not null)
        {
            string argument = statement.Substring(statement.LastIndexOf('(') + 1).Split(',').Last();
            if (argument.IndexOf('=') >= 0)
            {
                return true;
            }

            foreach (CompletionParameterDefinition parameter in
                model.GetCompletionTimbreParameters(element, call.Name) ?? Array.Empty<CompletionParameterDefinition>())
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
                source => source.Kind == "TimbreClip");
            return true;
        }

        return false;
    }

    // `$self.timbre.` completes the sounds of the Aspect's @timbre and
    // `$self.timbre.Sound.` the properties Motion can animate on it.
    private static bool TryAddTimbreMotionTargetCompletions(
        ICollection<CernealaCompletionItem> result,
        CompletionSite site,
        CernealaSemanticModel? model,
        ElementSyntax? element,
        string statement)
    {
        string token = statement.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries).LastOrDefault() ?? string.Empty;
        if (!token.StartsWith("$self.timbre.", StringComparison.Ordinal) || char.IsWhiteSpace(statement[statement.Length - 1]))
        {
            return false;
        }

        string[] segments = token.Split('.');
        string partial = segments[segments.Length - 1];
        TextSpan span = new(site.Offset - partial.Length, partial.Length);
        if (segments.Length == 3)
        {
            foreach (string sound in model?.GetCompletionTimbreMotionSounds(element) ?? Array.Empty<string>())
            {
                Add(result, sound, sound, span, CernealaCompletionItemKind.Variable, "Timbre sound", "00");
            }
        }
        else if (segments.Length == 4)
        {
            foreach (string parameter in model?.GetCompletionTimbreMotionParameters(element, segments[2]) ?? Array.Empty<string>())
            {
                Add(result, parameter, parameter, span, CernealaCompletionItemKind.Property, "System.Single", "00");
            }
        }

        return true;
    }

    // A TimbreClip body (or `@timbre { … }` block): `@parameter` and `@sound`
    // at its top; Source, Volume, Loop, AutoPlay and `@modifier` inside a
    // `@sound`.
    private static void AddTimbreClipBodyCompletions(
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
                IEnumerable<string> parameters = clip is not null
                    ? model?.GetCompletionTimbreClipParameters(clip, site.Offset) ?? Array.Empty<string>()
                    : InlineTimbreParameters(site);
                foreach (string parameter in parameters)
                {
                    Add(result, parameter, parameter, site.WordSpan, CernealaCompletionItemKind.Variable, "Timbre parameter", "00");
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

        bool insideSound = IsInsideDirectiveBlock(site.Source, site.Offset, "@sound");
        if (site.WordPrefix.StartsWith("@", StringComparison.Ordinal))
        {
            if (insideSound)
            {
                Add(result, "@modifier", "@modifier LowPass { }", site.WordSpan, CernealaCompletionItemKind.Keyword, "Timbre directive", "00");
            }
            else
            {
                Add(result, "@parameter", "@parameter Name: float = 0;", site.WordSpan, CernealaCompletionItemKind.Keyword, "Timbre directive", "00");
                Add(result, "@sound", "@sound Name { Source = \"\"; }", site.WordSpan, CernealaCompletionItemKind.Keyword, "Timbre directive", "00");
            }

            return;
        }

        if (!insideSound)
        {
            return;
        }

        int assignment = trimmed.IndexOf('=');
        if (assignment >= 0)
        {
            if (trimmed.Substring(0, assignment).Trim() is "Loop" or "AutoPlay")
            {
                AddBooleanValues(result, site);
            }

            return;
        }

        foreach (string property in TimbreSoundProperties)
        {
            Add(result, property, property + " = ", site.WordSpan, CernealaCompletionItemKind.Property, "@sound", "00");
        }
    }

    // The parameters an inline `@timbre { … }` block declares before the
    // caret, read lexically: the Aspect being edited is not bound yet.
    private static IEnumerable<string> InlineTimbreParameters(CompletionSite site)
    {
        int start = site.Source.LastIndexOf("@timbre", Math.Max(0, site.Offset - 1), StringComparison.Ordinal);
        return start < 0
            ? Array.Empty<string>()
            : Regex.Matches(site.Source.Substring(start, site.Offset - start), @"@parameter\s+([A-Za-z_][A-Za-z0-9_]*)")
                .Cast<Match>()
                .Select(match => match.Groups[1].Value)
                .Distinct(StringComparer.Ordinal)
                .ToArray();
    }

    private static void AddBooleanValues(ICollection<CernealaCompletionItem> result, CompletionSite site)
    {
        Add(result, "true", "true", site.WordSpan, CernealaCompletionItemKind.Value, "System.Boolean", "00");
        Add(result, "false", "false", site.WordSpan, CernealaCompletionItemKind.Value, "System.Boolean", "00");
    }
}
