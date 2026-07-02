using Cerneala.UI.Core;
using Cerneala.UI.Elements;

namespace Cerneala.UI.Invalidation;

public sealed class InvalidationRequest
{
    public InvalidationRequest(
        UIElement target,
        InvalidationFlags flags,
        string reason,
        UiProperty? sourceProperty = null,
        InvalidationFlags? resourceEffects = null,
        bool affectsIntrinsicSize = true)
    {
        Target = target ?? throw new ArgumentNullException(nameof(target));
        Flags = flags;
        Reason = string.IsNullOrWhiteSpace(reason)
            ? throw new ArgumentException("Invalidation reason cannot be empty.", nameof(reason))
            : reason;
        SourceProperty = sourceProperty;
        ResourceEffects = resourceEffects;
        AffectsIntrinsicSize = affectsIntrinsicSize;
    }

    public UIElement Target { get; }

    public InvalidationFlags Flags { get; }

    public string Reason { get; }

    public UiProperty? SourceProperty { get; }

    public InvalidationFlags? ResourceEffects { get; }

    public bool AffectsIntrinsicSize { get; }
}
