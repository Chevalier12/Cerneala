using Cerneala.UI.Aspect;

namespace Cerneala.UI.Detective;

public sealed record AspectTokenTrace(
    AspectToken Token,
    string ProviderName,
    object? RawValue,
    object? ResolvedValue);
