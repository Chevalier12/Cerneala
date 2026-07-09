namespace Cerneala.UI.Input;

public sealed record StylusInputPoint(
    int Id,
    float X,
    float Y,
    StylusInputAction Action,
    float Pressure = 0.5f,
    bool IsInRange = true,
    string? Button = null);
