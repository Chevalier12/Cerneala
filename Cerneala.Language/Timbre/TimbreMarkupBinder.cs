using System.Globalization;
using System.Net;
using Cerneala.Language.Syntax.Embedded;
using Cerneala.Language.Text;
using Cerneala.Timbre.Catalog;

namespace Cerneala.Language.Timbre;

internal sealed class BoundTimbreParameter
{
    public BoundTimbreParameter(string name, TextSpan nameSpan)
    {
        Name = name;
        NameSpan = nameSpan;
    }

    public string Name { get; }

    public TextSpan NameSpan { get; }

    public float DefaultValue { get; internal set; }

    // Intersection of the ranges of every modifier input the parameter feeds.
    public float Minimum { get; internal set; } = float.NegativeInfinity;

    public float Maximum { get; internal set; } = float.PositiveInfinity;

    // True while every input the parameter feeds is measured in seconds, so
    // duration literals such as 120ms are meaningful for it.
    public bool AcceptsDuration { get; internal set; }

    internal bool FeedsInput { get; set; }

    public bool Contains(float value) =>
        !float.IsNaN(value) && !float.IsInfinity(value) && value >= Minimum && value <= Maximum;
}

internal sealed class BoundTimbreModifierInput
{
    public BoundTimbreModifierInput(string name, float value, BoundTimbreParameter? parameter)
    {
        Name = name;
        Value = value;
        Parameter = parameter;
    }

    public string Name { get; }

    public float Value { get; }

    public BoundTimbreParameter? Parameter { get; }
}

internal sealed class BoundTimbreModifier
{
    public BoundTimbreModifier(string kind, IReadOnlyList<BoundTimbreModifierInput> inputs)
    {
        Kind = kind;
        Inputs = inputs;
    }

    public string Kind { get; }

    // Inputs written in markup, in catalog order; omitted inputs keep the
    // catalog default of the core modifier.
    public IReadOnlyList<BoundTimbreModifierInput> Inputs { get; }
}

internal sealed class BoundTimbreClip
{
    public BoundTimbreClip(
        string? name,
        string? source,
        float volume,
        bool loop,
        IReadOnlyList<BoundTimbreParameter> parameters,
        IReadOnlyList<BoundTimbreModifier> modifiers,
        bool isValid)
    {
        Name = name;
        Source = source;
        Volume = volume;
        Loop = loop;
        Parameters = parameters;
        Modifiers = modifiers;
        IsValid = isValid;
    }

    public string? Name { get; }

    public string? Source { get; }

    public float Volume { get; }

    public bool Loop { get; }

    public IReadOnlyList<BoundTimbreParameter> Parameters { get; }

    public IReadOnlyList<BoundTimbreModifier> Modifiers { get; }

    public bool IsValid { get; }

    public BoundTimbreParameter? FindParameter(string name) =>
        Parameters.FirstOrDefault(parameter => string.Equals(parameter.Name, name, StringComparison.Ordinal));
}

internal sealed class BoundTimbreArgument
{
    public BoundTimbreArgument(string name, float value)
    {
        Name = name;
        Value = value;
    }

    public string Name { get; }

    public float Value { get; }
}

internal sealed class BoundTimbreAction
{
    public BoundTimbreAction(
        TimbreActionKind kind,
        TextSpan keywordSpan,
        string? handleName,
        string? clipName = null,
        BoundTimbreClip? clip = null,
        float? volume = null,
        bool? loop = null,
        IReadOnlyList<BoundTimbreArgument>? arguments = null,
        long seekTicks = 0)
    {
        Kind = kind;
        KeywordSpan = keywordSpan;
        HandleName = handleName;
        ClipName = clipName;
        Clip = clip;
        Volume = volume;
        Loop = loop;
        Arguments = arguments ?? Array.Empty<BoundTimbreArgument>();
        SeekTicks = seekTicks;
    }

    public TimbreActionKind Kind { get; }

    public TextSpan KeywordSpan { get; }

    public string? HandleName { get; }

    public string? ClipName { get; }

    public BoundTimbreClip? Clip { get; }

    public float? Volume { get; }

    public bool? Loop { get; }

    public IReadOnlyList<BoundTimbreArgument> Arguments { get; }

    public long SeekTicks { get; }
}

internal enum TimbreHandleKind
{
    Unused,
    Timbre,
    Motion
}

// A float parameter exposed by every TimbreClip a Timbre handle can play, with
// the intersection of its ranges in those clips.
internal sealed class BoundTimbreHandleParameter
{
    public BoundTimbreHandleParameter(string name, float minimum, float maximum)
    {
        Name = name;
        Minimum = minimum;
        Maximum = maximum;
    }

    public string Name { get; }

    public float Minimum { get; }

    public float Maximum { get; }

    public bool Contains(float value) =>
        !float.IsNaN(value) && !float.IsInfinity(value) && value >= Minimum && value <= Maximum;
}

internal sealed class BoundTimbreAspect
{
    public BoundTimbreAspect(
        int elementStart,
        IReadOnlyList<BoundTimbreAction> actions,
        IReadOnlyDictionary<string, TimbreHandleKind> handles,
        IReadOnlyDictionary<string, IReadOnlyList<BoundTimbreHandleParameter>>? handleParameters = null)
    {
        ElementStart = elementStart;
        Actions = actions;
        Handles = handles;
        HandleParameters = handleParameters ?? new Dictionary<string, IReadOnlyList<BoundTimbreHandleParameter>>();
    }

    // Span.Start of the <Aspect> resource or <Owner.Aspect> property element.
    public int ElementStart { get; }

    // Timbre actions of the Aspect in document order, including @cancel of
    // Timbre handles; Motion @cancel is not listed.
    public IReadOnlyList<BoundTimbreAction> Actions { get; }

    public IReadOnlyDictionary<string, TimbreHandleKind> Handles { get; }

    // Typed `$self.timbre.Handle.Parameter` schema of each Timbre handle: the
    // custom parameters common to every clip started in it. Volume is
    // intrinsic and not listed.
    public IReadOnlyDictionary<string, IReadOnlyList<BoundTimbreHandleParameter>> HandleParameters { get; }

    public BoundTimbreHandleParameter? FindHandleParameter(string handle, string name) =>
        HandleParameters.TryGetValue(handle, out IReadOnlyList<BoundTimbreHandleParameter>? parameters)
            ? parameters.FirstOrDefault(parameter => string.Equals(parameter.Name, name, StringComparison.Ordinal))
            : null;
}

internal sealed class TimbreMarkupModel
{
    public static TimbreMarkupModel Empty { get; } = new(
        new Dictionary<int, BoundTimbreClip>(),
        new Dictionary<int, BoundTimbreAspect>());

    public TimbreMarkupModel(
        IReadOnlyDictionary<int, BoundTimbreClip> clips,
        IReadOnlyDictionary<int, BoundTimbreAspect> aspects)
    {
        Clips = clips;
        Aspects = aspects;
    }

    // Bound TimbreClip resources keyed by the Span.Start of their element.
    public IReadOnlyDictionary<int, BoundTimbreClip> Clips { get; }

    public IReadOnlyDictionary<int, BoundTimbreAspect> Aspects { get; }
}

// Single build-time owner of TimbreClip/@timbre validity. Every rule comes from
// TimbreCatalog, the file the core runtime compiles; Language and SourceGen
// both consume the bound result instead of re-validating.
internal static class TimbreMarkupBinder
{
    public const string ReferenceId = "CERNEALAUI031";
    public const string ValueId = "CERNEALAUI032";
    public const string ContextId = "CERNEALAUI033";

    private static readonly string[] ClipProperties = ["Source", "Volume", "Loop"];

    public static BoundTimbreClip BindClip(
        string? name,
        TimbreClipBodySyntax body,
        TextSpan clipSpan,
        ICollection<EmbeddedDiagnostic> diagnostics)
    {
        int initialCount = diagnostics.Count;
        foreach (EmbeddedDiagnostic diagnostic in body.Diagnostics)
        {
            diagnostics.Add(diagnostic);
        }

        foreach (TimbreDirectiveReference directive in body.ForeignDirectives)
        {
            diagnostics.Add(new EmbeddedDiagnostic(ContextId, ContextMessage(directive.Keyword), directive.Span));
        }

        string? source = null;
        float volume = TimbreCatalog.Volume.DefaultValue;
        bool loop = false;
        HashSet<string> assigned = new(StringComparer.Ordinal);
        List<BoundTimbreParameter> parameters = new();
        List<(BoundTimbreParameter Parameter, TimbreParameterSyntax Syntax)> declarations = new();
        List<BoundTimbreModifier> modifiers = new();
        foreach (object statement in body.Statements)
        {
            switch (statement)
            {
                case TimbreValueSyntax property:
                    BindClipProperty(property, assigned, diagnostics, ref source, ref volume, ref loop);
                    break;
                case TimbreParameterSyntax declaration:
                    if (parameters.Any(parameter => parameter.Name == declaration.Name))
                    {
                        diagnostics.Add(new EmbeddedDiagnostic(ReferenceId, "Duplicate TimbreClip parameter '" + declaration.Name + "'.", declaration.NameSpan));
                        break;
                    }

                    if (declaration.TypeName is not ("float" or "System.Single"))
                    {
                        diagnostics.Add(new EmbeddedDiagnostic(ValueId, "Timbre parameter '" + declaration.Name + "' must have type float.", declaration.TypeSpan));
                        break;
                    }

                    BoundTimbreParameter parameter = new(declaration.Name, declaration.NameSpan);
                    parameters.Add(parameter);
                    declarations.Add((parameter, declaration));
                    break;
                case TimbreModifierSyntax modifier:
                    if (BindModifier(modifier, parameters, diagnostics) is BoundTimbreModifier bound)
                    {
                        modifiers.Add(bound);
                    }

                    break;
            }
        }

        foreach ((BoundTimbreParameter parameter, TimbreParameterSyntax syntax) in declarations)
        {
            if (TryParseParameterValue(parameter, syntax.Value, syntax.ValueSpan, diagnostics, out float value))
            {
                parameter.DefaultValue = value;
            }
        }

        if (source is null && !assigned.Contains("Source"))
        {
            diagnostics.Add(new EmbeddedDiagnostic(ValueId, "TimbreClip requires Source.", clipSpan));
        }

        return new BoundTimbreClip(name, source, volume, loop, parameters, modifiers, diagnostics.Count == initialCount);
    }

    public static BoundTimbreAction BindPlay(
        TimbreActionSyntax syntax,
        BoundTimbreClip clip,
        ICollection<EmbeddedDiagnostic> diagnostics)
    {
        float? volume = null;
        bool? loop = null;
        List<BoundTimbreArgument> arguments = new();
        HashSet<string> supplied = new(StringComparer.Ordinal);
        foreach (TimbreValueSyntax argument in syntax.Arguments)
        {
            if (!supplied.Add(argument.Name))
            {
                diagnostics.Add(new EmbeddedDiagnostic(ReferenceId, "Duplicate @timbre argument '" + argument.Name + "'.", argument.NameSpan));
                continue;
            }

            if (argument.Name == "Volume")
            {
                if (TryParseVolume(argument, diagnostics, out float parsedVolume))
                {
                    volume = parsedVolume;
                }

                continue;
            }

            if (argument.Name == "Loop")
            {
                if (TryParseLoop(argument, diagnostics, out bool parsedLoop))
                {
                    loop = parsedLoop;
                }

                continue;
            }

            BoundTimbreParameter? parameter = clip.FindParameter(argument.Name);
            if (parameter is null)
            {
                diagnostics.Add(new EmbeddedDiagnostic(
                    ReferenceId,
                    "TimbreClip '" + (syntax.ClipName ?? clip.Name) + "' has no parameter '" + argument.Name + "'.",
                    argument.NameSpan));
                continue;
            }

            if (TryParseParameterValue(parameter, argument.Value, argument.ValueSpan, diagnostics, out float value))
            {
                arguments.Add(new BoundTimbreArgument(parameter.Name, value));
            }
        }

        return new BoundTimbreAction(
            TimbreActionKind.Play,
            syntax.KeywordSpan,
            syntax.HandleName,
            syntax.ClipName,
            clip,
            volume,
            loop,
            arguments);
    }

    public static bool TryBindSeek(TimbreActionSyntax syntax, ICollection<EmbeddedDiagnostic> diagnostics, out long ticks)
    {
        if (TryParseDuration(syntax.SeekValue ?? string.Empty, out double seconds) && seconds >= 0)
        {
            ticks = (long)Math.Round(seconds * TimeSpan.TicksPerSecond);
            return true;
        }

        diagnostics.Add(new EmbeddedDiagnostic(ValueId, "@seek requires a non-negative duration such as 30s or 500ms.", syntax.SeekSpan));
        ticks = 0;
        return false;
    }

    public static string ContextMessage(string keyword) => keyword switch
    {
        "@modifier" => "@modifier is allowed only inside TimbreClip.",
        _ => keyword + " is allowed only inside an Aspect @on, @when or @if body."
    };

    public static bool TryParseDuration(string text, out double seconds)
    {
        text = text.Trim();
        int unit = text.EndsWith("ms", StringComparison.Ordinal) ? 2 :
            text.EndsWith("s", StringComparison.Ordinal) ? 1 : 0;
        if (unit > 0 &&
            double.TryParse(text.Substring(0, text.Length - unit), NumberStyles.Float, CultureInfo.InvariantCulture, out double value) &&
            !double.IsNaN(value) && !double.IsInfinity(value))
        {
            seconds = unit == 2 ? value / 1000d : value;
            return true;
        }

        seconds = 0;
        return false;
    }

    public static string FormatNumber(float value) => value.ToString(CultureInfo.InvariantCulture);

    private static void BindClipProperty(
        TimbreValueSyntax property,
        ISet<string> assigned,
        ICollection<EmbeddedDiagnostic> diagnostics,
        ref string? source,
        ref float volume,
        ref bool loop)
    {
        if (!ClipProperties.Contains(property.Name, StringComparer.Ordinal))
        {
            diagnostics.Add(new EmbeddedDiagnostic(
                ReferenceId,
                "TimbreClip has no property '" + property.Name + "'; expected Source, Volume or Loop.",
                property.NameSpan));
            return;
        }

        if (!assigned.Add(property.Name))
        {
            diagnostics.Add(new EmbeddedDiagnostic(ReferenceId, "Duplicate TimbreClip property '" + property.Name + "'.", property.NameSpan));
            return;
        }

        switch (property.Name)
        {
            case "Source":
                string value = property.Value;
                string path = value.Length >= 2 && value[0] == '"' && value[value.Length - 1] == '"'
                    ? WebUtility.HtmlDecode(value.Substring(1, value.Length - 2))
                    : string.Empty;
                if (path.Trim().Length == 0 || path.IndexOf("://", StringComparison.Ordinal) >= 0)
                {
                    diagnostics.Add(new EmbeddedDiagnostic(
                        ValueId,
                        "Source must be a non-empty local file path in quotes; URIs are not supported.",
                        property.ValueSpan));
                    return;
                }

                source = path;
                return;
            case "Volume":
                if (TryParseVolume(property, diagnostics, out float parsedVolume))
                {
                    volume = parsedVolume;
                }

                return;
            default:
                if (TryParseLoop(property, diagnostics, out bool parsedLoop))
                {
                    loop = parsedLoop;
                }

                return;
        }
    }

    private static BoundTimbreModifier? BindModifier(
        TimbreModifierSyntax syntax,
        IReadOnlyList<BoundTimbreParameter> declared,
        ICollection<EmbeddedDiagnostic> diagnostics)
    {
        TimbreCatalogInput[]? catalog = TimbreCatalog.GetModifierInputs(syntax.Kind);
        if (catalog is null)
        {
            diagnostics.Add(new EmbeddedDiagnostic(
                ReferenceId,
                "Unknown sound modifier '" + syntax.Kind + "'; expected " + string.Join(" or ", TimbreCatalog.ModifierNames) + ".",
                syntax.KindSpan));
            return null;
        }

        Dictionary<string, BoundTimbreModifierInput> inputs = new(StringComparer.Ordinal);
        bool valid = true;
        foreach (TimbreValueSyntax input in syntax.Inputs)
        {
            TimbreCatalogInput? definition = catalog.FirstOrDefault(candidate => candidate.Name == input.Name);
            if (definition is null)
            {
                diagnostics.Add(new EmbeddedDiagnostic(
                    ReferenceId,
                    "Timbre modifier '" + syntax.Kind + "' has no input '" + input.Name + "'.",
                    input.NameSpan));
                valid = false;
                continue;
            }

            if (inputs.ContainsKey(input.Name))
            {
                diagnostics.Add(new EmbeddedDiagnostic(
                    ReferenceId,
                    "Duplicate input '" + input.Name + "' in sound modifier '" + syntax.Kind + "'.",
                    input.NameSpan));
                valid = false;
                continue;
            }

            string value = input.Value.Trim();
            if (TimbreMarkupSyntax.IsIdentifier(value))
            {
                BoundTimbreParameter? parameter = declared.FirstOrDefault(candidate => candidate.Name == value);
                if (parameter is null)
                {
                    diagnostics.Add(new EmbeddedDiagnostic(
                        ReferenceId,
                        "Timbre parameter '" + value + "' is undeclared or used before its declaration.",
                        input.ValueSpan));
                    valid = false;
                    continue;
                }

                parameter.AcceptsDuration = (parameter.AcceptsDuration || !parameter.FeedsInput) && definition.Unit == "s";
                parameter.FeedsInput = true;
                parameter.Minimum = Math.Max(parameter.Minimum, definition.Minimum);
                parameter.Maximum = Math.Min(parameter.Maximum, definition.Maximum);
                inputs.Add(input.Name, new BoundTimbreModifierInput(input.Name, 0f, parameter));
                continue;
            }

            if (TryParseInputValue(definition, input, diagnostics, out float literal))
            {
                inputs.Add(input.Name, new BoundTimbreModifierInput(input.Name, literal, null));
            }
            else
            {
                valid = false;
            }
        }

        if (!valid)
        {
            return null;
        }

        return new BoundTimbreModifier(
            syntax.Kind,
            catalog.Where(definition => inputs.ContainsKey(definition.Name))
                .Select(definition => inputs[definition.Name])
                .ToArray());
    }

    private static bool TryParseInputValue(
        TimbreCatalogInput definition,
        TimbreValueSyntax input,
        ICollection<EmbeddedDiagnostic> diagnostics,
        out float value)
    {
        string text = input.Value.Trim();
        bool parsed;
        if (TryParseDuration(text, out double seconds))
        {
            if (definition.Unit != "s")
            {
                diagnostics.Add(new EmbeddedDiagnostic(
                    ValueId,
                    "Duration literal '" + text + "' is valid only for inputs measured in seconds.",
                    input.ValueSpan));
                value = 0;
                return false;
            }

            value = (float)seconds;
            parsed = true;
        }
        else
        {
            parsed = TryParseFloat(text, out value);
        }

        if (parsed && definition.Contains(value))
        {
            return true;
        }

        diagnostics.Add(new EmbeddedDiagnostic(
            ValueId,
            definition.Owner + "." + definition.Name + " must be a finite number within " +
            FormatNumber(definition.Minimum) + "–" + FormatNumber(definition.Maximum) + " " + definition.Unit + ".",
            input.ValueSpan));
        return false;
    }

    private static bool TryParseParameterValue(
        BoundTimbreParameter parameter,
        string text,
        TextSpan span,
        ICollection<EmbeddedDiagnostic> diagnostics,
        out float value)
    {
        text = text.Trim();
        bool parsed;
        if (TryParseDuration(text, out double seconds))
        {
            parsed = parameter.FeedsInput && parameter.AcceptsDuration;
            value = (float)seconds;
            if (!parsed)
            {
                diagnostics.Add(new EmbeddedDiagnostic(
                    ValueId,
                    "Duration literal '" + text + "' is valid only for parameters that feed inputs measured in seconds.",
                    span));
                return false;
            }
        }
        else if (!TryParseFloat(text, out value))
        {
            diagnostics.Add(new EmbeddedDiagnostic(ValueId, "Timbre parameter '" + parameter.Name + "' value '" + text + "' is not a finite float.", span));
            return false;
        }

        if (parameter.Contains(value))
        {
            return true;
        }

        diagnostics.Add(new EmbeddedDiagnostic(
            ValueId,
            "Timbre parameter '" + parameter.Name + "' value " + FormatNumber(value) + " is outside the range " +
            FormatNumber(parameter.Minimum) + "–" + FormatNumber(parameter.Maximum) + " of the modifier inputs it feeds.",
            span));
        return false;
    }

    private static bool TryParseVolume(TimbreValueSyntax syntax, ICollection<EmbeddedDiagnostic> diagnostics, out float volume)
    {
        if (TryParseFloat(syntax.Value.Trim(), out volume) && TimbreCatalog.Volume.Contains(volume))
        {
            return true;
        }

        diagnostics.Add(new EmbeddedDiagnostic(
            ValueId,
            "Volume must be a finite number within " + FormatNumber(TimbreCatalog.Volume.Minimum) + "–" +
            FormatNumber(TimbreCatalog.Volume.Maximum) + ".",
            syntax.ValueSpan));
        return false;
    }

    private static bool TryParseLoop(TimbreValueSyntax syntax, ICollection<EmbeddedDiagnostic> diagnostics, out bool loop)
    {
        switch (syntax.Value.Trim())
        {
            case "true":
                loop = true;
                return true;
            case "false":
                loop = false;
                return true;
            default:
                diagnostics.Add(new EmbeddedDiagnostic(ValueId, "Loop must be true or false.", syntax.ValueSpan));
                loop = false;
                return false;
        }
    }

    private static bool TryParseFloat(string text, out float value) =>
        float.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out value) &&
        !float.IsNaN(value) && !float.IsInfinity(value);
}
