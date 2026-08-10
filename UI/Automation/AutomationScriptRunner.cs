using System.Text.Json;
using Cerneala.UI.Input;

namespace Cerneala.UI.Automation;

public static class AutomationScriptRunner
{
    public static void RunFile(AutomationSession session, string path)
    {
        ArgumentNullException.ThrowIfNull(session);
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        string fullPath = Path.GetFullPath(path);
        RunJson(session, File.ReadAllText(fullPath), Path.GetDirectoryName(fullPath));
    }

    public static void RunJson(
        AutomationSession session,
        string json,
        string? baseDirectory = null)
    {
        ArgumentNullException.ThrowIfNull(session);
        ArgumentException.ThrowIfNullOrWhiteSpace(json);
        using JsonDocument document = JsonDocument.Parse(json);
        JsonElement root = document.RootElement;
        if (!root.TryGetProperty("steps", out JsonElement steps) ||
            steps.ValueKind != JsonValueKind.Array)
        {
            throw new InvalidDataException("An automation script requires a 'steps' array.");
        }

        int index = 0;
        foreach (JsonElement step in steps.EnumerateArray())
        {
            try
            {
                ExecuteStep(session, step, baseDirectory);
            }
            catch (Exception exception) when (exception is not InvalidDataException)
            {
                throw new InvalidOperationException(
                    $"Automation step {index} failed: {exception.Message}",
                    exception);
            }

            index++;
        }
    }

    private static void ExecuteStep(
        AutomationSession session,
        JsonElement step,
        string? baseDirectory)
    {
        string action = RequiredString(step, "action");
        switch (action.ToLowerInvariant())
        {
            case "click":
                RequiredTarget(session, step).Click();
                return;
            case "presskey":
                OptionalTarget(session, step)?.Click();
                session.PressKey(
                    ParseEnum<InputKey>(RequiredString(step, "key"), "key"),
                    ParseModifiers(step));
                return;
            case "sendtext":
                OptionalTarget(session, step)?.Click();
                session.SendText(RequiredString(step, "text"));
                return;
            case "screenshot":
                string path = RequiredString(step, "path");
                if (!Path.IsPathRooted(path) && !string.IsNullOrWhiteSpace(baseDirectory))
                {
                    path = Path.Combine(baseDirectory, path);
                }

                session.SaveScreenshot(path);
                return;
            default:
                throw new InvalidDataException($"Unknown automation action '{action}'.");
        }
    }

    private static AutomationElement RequiredTarget(
        AutomationSession session,
        JsonElement step)
    {
        return OptionalTarget(session, step) ??
            throw new InvalidDataException(
                "This automation action requires either 'automationId' or 'xpath'.");
    }

    private static AutomationElement? OptionalTarget(
        AutomationSession session,
        JsonElement step)
    {
        string? automationId = OptionalString(step, "automationId");
        string? xpath = OptionalString(step, "xpath");
        if (automationId is not null && xpath is not null)
        {
            throw new InvalidDataException(
                "An automation step cannot specify both 'automationId' and 'xpath'.");
        }

        if (automationId is not null)
        {
            return session.FindByAutomationId(automationId);
        }

        return xpath is not null ? session.FindByXPath(xpath) : null;
    }

    private static AutomationModifiers ParseModifiers(JsonElement step)
    {
        if (!step.TryGetProperty("modifiers", out JsonElement value))
        {
            return AutomationModifiers.None;
        }

        if (value.ValueKind == JsonValueKind.String)
        {
            return ParseEnum<AutomationModifiers>(value.GetString()!, "modifiers");
        }

        if (value.ValueKind != JsonValueKind.Array)
        {
            throw new InvalidDataException("'modifiers' must be a string or an array of strings.");
        }

        AutomationModifiers result = AutomationModifiers.None;
        foreach (JsonElement item in value.EnumerateArray())
        {
            if (item.ValueKind != JsonValueKind.String)
            {
                throw new InvalidDataException("Every modifier must be a string.");
            }

            result |= ParseEnum<AutomationModifiers>(item.GetString()!, "modifiers");
        }

        return result;
    }

    private static T ParseEnum<T>(string text, string propertyName)
        where T : struct, Enum
    {
        if (Enum.TryParse(text, ignoreCase: true, out T value) && Enum.IsDefined(value))
        {
            return value;
        }

        throw new InvalidDataException($"'{text}' is not a valid {propertyName} value.");
    }

    private static string RequiredString(JsonElement element, string propertyName)
    {
        return OptionalString(element, propertyName) ??
            throw new InvalidDataException($"Automation step requires a non-empty '{propertyName}'.");
    }

    private static string? OptionalString(JsonElement element, string propertyName)
    {
        if (!element.TryGetProperty(propertyName, out JsonElement value) ||
            value.ValueKind == JsonValueKind.Null)
        {
            return null;
        }

        if (value.ValueKind != JsonValueKind.String)
        {
            throw new InvalidDataException($"'{propertyName}' must be a string.");
        }

        string? text = value.GetString();
        return string.IsNullOrWhiteSpace(text) ? null : text;
    }
}
