namespace Cerneala.UI.Detective;

public readonly record struct MotionGraphSnapshot(
    int ActiveNodeCount,
    int ActivePropertyBindings,
    int ActiveLayoutMotions,
    int ActivePresenceExits,
    int ValuesSampledThisFrame,
    int PropertiesWrittenThisFrame,
    bool NeedsAnotherFrame);
