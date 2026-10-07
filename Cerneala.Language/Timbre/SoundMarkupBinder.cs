using System.Globalization;
using System.Net;
using Cerneala.Language.Syntax.Embedded;
using Cerneala.Language.Text;
using Cerneala.Timbre.Catalog;

namespace Cerneala.Language.Timbre;

internal sealed class BoundSoundParameter
{
    public BoundSoundParameter(string name, TextSpan nameSpan)
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

internal sealed class BoundSoundModifierInput
{
    public BoundSoundModifierInput(string name, float value, BoundSoundParameter? parameter)
    {
        Name = name;
        Value = value;
        Parameter = parameter;
    }

    public string Name { get; }

    public float Value { get; }

    public BoundSoundParameter? Parameter { get; }
}

internal sealed class BoundSoundModifier
{
    public BoundSoundModifier(string kind, IReadOnlyList<BoundSoundModifierInput> inputs)
    {
        Kind = kind;
        Inputs = inputs;
    }

    public string Kind { get; }

    // Inputs written in markup, in catalog order; omitted inputs keep the
    // catalog default of the core modifier.
    public IReadOnlyList<BoundSoundModifierInput> Inputs { get; }
}

internal sealed class BoundSoundClip
{
    public BoundSoundClip(
        string? name,
        string? source,
        float volume,
        bool loop,
        IReadOnlyList<BoundSoundParameter> parameters,
        IReadOnlyList<BoundSoundModifier> modifiers,
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

    public IReadOnlyList<BoundSoundParameter> Parameters { get; }

    public IReadOnlyList<BoundSoundModifier> Modifiers { get; }

    public bool IsValid { get; }

    public BoundSoundParameter? FindParameter(string name) =>
        Parameters.FirstOrDefault(parameter => string.Equals(parameter.Name, name, StringComparison.Ordinal));
}

internal sealed class BoundSoundArgument
{
    public BoundSoundArgument(string name, float value)
    {
        Name = name;
        Value = value;
    }

    public string Name { get; }

    public float Value { get; }
}

internal sealed class BoundSoundAction
{
    public BoundSoundAction(
        SoundActionKind kind,
        TextSpan keywordSpan,
        string? handleName,
        string? clipName = null,
        BoundSoundClip? clip = null,
        float? volume = null,
        bool? loop = null,
        IReadOnlyList<BoundSoundArgument>? arguments = null,
        long seekTicks = 0)
    {
        Kind = kind;
        KeywordSpan = keywordSpan;
        HandleName = handleName;
        ClipName = clipName;
        Clip = clip;
        Volume = volume;
        Loop = loop;
        Arguments = arguments ?? Array.Empty<BoundSoundArgument>();
        SeekTicks = seekTicks;
    }

    public SoundActionKind Kind { get; }

    public TextSpan KeywordSpan { get; }

    public string? HandleName { get; }

    public string? ClipName { get; }

    public BoundSoundClip? Clip { get; }

    public float? Volume { get; }

    public bool? Loop { get; }

    public IReadOnlyList<BoundSoundArgument> Arguments { get; }

    public long SeekTicks { get; }
}

internal enum SoundHandleKind
{
    Unused,
    Sound,
    Motion
}

internal sealed class BoundSoundAspect
{
    public BoundSoundAspect(
        int elementStart,
        IReadOnlyList<BoundSoundAction> actions,
        IReadOnlyDictionary<string, SoundHandleKind> handles)
    {
        ElementStart = elementStart;
        Actions = actions;
        Handles = handles;
    }

    // Span.Start of the <Aspect> resource or <Owner.Aspect> property element.
    public int ElementStart { get; }

    // Sound actions of the Aspect in document order, including @cancel of
    // Sound handles; Motion @cancel is not listed.
    public IReadOnlyList<BoundSoundAction> Actions { get; }

    public IReadOnlyDictionary<string, SoundHandleKind> Handles { get; }
}

internal sealed class SoundMarkupModel
{
    public static SoundMarkupModel Empty { get; } = new(
        new Dictionary<int, BoundSoundClip>(),
        new Dictionary<int, BoundSoundAspect>());

    public SoundMarkupModel(
        IReadOnlyDictionary<int, BoundSoundClip> clips,
        IReadOnlyDictionary<int, BoundSoundAspect> aspects)
    {
        Clips = clips;
        Aspects = aspects;
    }

    // Bound SoundClip resources keyed by the Span.Start of their element.
    public IReadOnlyDictionary<int, BoundSoundClip> Clips { get; }

    public IReadOnlyDictionary<int, BoundSoundAspect> Aspects { get; }
}

// Single build-time owner of SoundClip/@sound validity. Every rule comes from
// TimbreCatalog, the file the core runtime compiles; Language and SourceGen
// both consume the bound result instead of re-validating.
internal static class SoundMarkupBinder
{
    public const string ReferenceId = "CERNEALAUI031";
    public const string ValueId = "CERNEALAUI032";
    public const string ContextId = "CERNEALAUI033";

    private static readonly string[] ClipProperties = ["Source", "Volume", "Loop"];

    public static BoundSoundClip BindClip(
        string? name,
        SoundClipBodySyntax body,
        TextSpan clipSpan,
        ICollection<EmbeddedDiagnostic> diagnostics)
    {
        int initialCount = diagnostics.Count;
        foreach (EmbeddedDiagnostic diagnostic in body.Diagnostics)
        {
            diagnostics.Add(diagnostic);
        }

        foreach (SoundDirectiveReference directive in body.ForeignDirectives)
        {
            diagnostics.Add(new EmbeddedDiagnostic(ContextId, ContextMessage(directive.Keyword), directive.Span));
        }

        string? source = null;
        float volume = TimbreCatalog.Volume.DefaultValue;
        bool loop = false;
        HashSet<string> assigned = new(StringComparer.Ordinal);
        List<BoundSoundParameter> parameters = new();
        List<(BoundSoundParameter Parameter, SoundParameterSyntax Syntax)> declarations = new();
        List<BoundSoundModifier> modifiers = new();
        foreach (object statement in body.Statements)
        {
            switch (statement)
            {
                case SoundValueSyntax property:
                    BindClipProperty(property, assigned, diagnostics, ref source, ref volume, ref loop);
                    break;
                case SoundParameterSyntax declaration:
                    if (parameters.Any(parameter => parameter.Name == declaration.Name))
                    {
                        diagnostics.Add(new EmbeddedDiagnostic(ReferenceId, "Duplicate SoundClip parameter '" + declaration.Name + "'.", declaration.NameSpan));
                        break;
                    }

                    if (declaration.TypeName is not ("float" or "System.Single"))
                    {
                        diagnostics.Add(new EmbeddedDiagnostic(ValueId, "Sound parameter '" + declaration.Name + "' must have type float.", declaration.TypeSpan));
                        break;
                    }

                    BoundSoundParameter parameter = new(declaration.Name, declaration.NameSpan);
                    parameters.Add(parameter);
                    declarations.Add((parameter, declaration));
                    break;
                case SoundModifierSyntax modifier:
                    if (BindModifier(modifier, parameters, diagnostics) is BoundSoundModifier bound)
                    {
                        modifiers.Add(bound);
                    }

                    break;
            }
        }

        foreach ((BoundSoundParameter parameter, SoundParameterSyntax syntax) in declarations)
        {
            if (TryParseParameterValue(parameter, syntax.Value, syntax.ValueSpan, diagnostics, out float value))
            {
                parameter.DefaultValue = value;
            }
        }

        if (source is null && !assigned.Contains("Source"))
        {
            diagnostics.Add(new EmbeddedDiagnostic(ValueId, "SoundClip requires Source.", clipSpan));
        }

        return new BoundSoundClip(name, source, volume, loop, parameters, modifiers, diagnostics.Count == initialCount);
    }

    public static BoundSoundAction BindPlay(
        SoundActionSyntax syntax,
        BoundSoundClip clip,
        ICollection<EmbeddedDiagnostic> diagnostics)
    {
        float? volume = null;
        bool? loop = null;
        List<BoundSoundArgument> arguments = new();
        HashSet<string> supplied = new(StringComparer.Ordinal);
        foreach (SoundValueSyntax argument in syntax.Arguments)
        {
            if (!supplied.Add(argument.Name))
            {
                diagnostics.Add(new EmbeddedDiagnostic(ReferenceId, "Duplicate @sound argument '" + argument.Name + "'.", argument.NameSpan));
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

            BoundSoundParameter? parameter = clip.FindParameter(argument.Name);
            if (parameter is null)
            {
                diagnostics.Add(new EmbeddedDiagnostic(
                    ReferenceId,
                    "SoundClip '" + (syntax.ClipName ?? clip.Name) + "' has no parameter '" + argument.Name + "'.",
                    argument.NameSpan));
                continue;
            }

            if (TryParseParameterValue(parameter, argument.Value, argument.ValueSpan, diagnostics, out float value))
            {
                arguments.Add(new BoundSoundArgument(parameter.Name, value));
            }
        }

        return new BoundSoundAction(
            SoundActionKind.Play,
            syntax.KeywordSpan,
            syntax.HandleName,
            syntax.ClipName,
            clip,
            volume,
            loop,
            arguments);
    }

    public static bool TryBindSeek(SoundActionSyntax syntax, ICollection<EmbeddedDiagnostic> diagnostics, out long ticks)
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
        "@modifier" => "@modifier is allowed only inside SoundClip.",
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
        SoundValueSyntax property,
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
                "SoundClip has no property '" + property.Name + "'; expected Source, Volume or Loop.",
                property.NameSpan));
            return;
        }

        if (!assigned.Add(property.Name))
        {
            diagnostics.Add(new EmbeddedDiagnostic(ReferenceId, "Duplicate SoundClip property '" + property.Name + "'.", property.NameSpan));
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

    private static BoundSoundModifier? BindModifier(
        SoundModifierSyntax syntax,
        IReadOnlyList<BoundSoundParameter> declared,
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

        Dictionary<string, BoundSoundModifierInput> inputs = new(StringComparer.Ordinal);
        bool valid = true;
        foreach (SoundValueSyntax input in syntax.Inputs)
        {
            TimbreCatalogInput? definition = catalog.FirstOrDefault(candidate => candidate.Name == input.Name);
            if (definition is null)
            {
                diagnostics.Add(new EmbeddedDiagnostic(
                    ReferenceId,
                    "Sound modifier '" + syntax.Kind + "' has no input '" + input.Name + "'.",
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
            if (SoundMarkupSyntax.IsIdentifier(value))
            {
                BoundSoundParameter? parameter = declared.FirstOrDefault(candidate => candidate.Name == value);
                if (parameter is null)
                {
                    diagnostics.Add(new EmbeddedDiagnostic(
                        ReferenceId,
                        "Sound parameter '" + value + "' is undeclared or used before its declaration.",
                        input.ValueSpan));
                    valid = false;
                    continue;
                }

                parameter.AcceptsDuration = (parameter.AcceptsDuration || !parameter.FeedsInput) && definition.Unit == "s";
                parameter.FeedsInput = true;
                parameter.Minimum = Math.Max(parameter.Minimum, definition.Minimum);
                parameter.Maximum = Math.Min(parameter.Maximum, definition.Maximum);
                inputs.Add(input.Name, new BoundSoundModifierInput(input.Name, 0f, parameter));
                continue;
            }

            if (TryParseInputValue(definition, input, diagnostics, out float literal))
            {
                inputs.Add(input.Name, new BoundSoundModifierInput(input.Name, literal, null));
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

        return new BoundSoundModifier(
            syntax.Kind,
            catalog.Where(definition => inputs.ContainsKey(definition.Name))
                .Select(definition => inputs[definition.Name])
                .ToArray());
    }

    private static bool TryParseInputValue(
        TimbreCatalogInput definition,
        SoundValueSyntax input,
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
        BoundSoundParameter parameter,
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
            diagnostics.Add(new EmbeddedDiagnostic(ValueId, "Sound parameter '" + parameter.Name + "' value '" + text + "' is not a finite float.", span));
            return false;
        }

        if (parameter.Contains(value))
        {
            return true;
        }

        diagnostics.Add(new EmbeddedDiagnostic(
            ValueId,
            "Sound parameter '" + parameter.Name + "' value " + FormatNumber(value) + " is outside the range " +
            FormatNumber(parameter.Minimum) + "–" + FormatNumber(parameter.Maximum) + " of the modifier inputs it feeds.",
            span));
        return false;
    }

    private static bool TryParseVolume(SoundValueSyntax syntax, ICollection<EmbeddedDiagnostic> diagnostics, out float volume)
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

    private static bool TryParseLoop(SoundValueSyntax syntax, ICollection<EmbeddedDiagnostic> diagnostics, out bool loop)
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
