namespace Cerneala.UI.Aspect;

public sealed record AspectTokenTrace(
    AspectToken Token,
    string ProviderName,
    object? RawValue,
    object? ResolvedValue);
