namespace Cerneala.UI.Aspect;

public sealed class AspectSlotPath
{
    public AspectSlotPath(AspectSlot slot, string? diagnosticPath = null)
    {
        Slot = slot ?? throw new ArgumentNullException(nameof(slot));
        DiagnosticPath = string.IsNullOrWhiteSpace(diagnosticPath) ? null : diagnosticPath;
    }

    public AspectSlot Slot { get; }

    public string? DiagnosticPath { get; }

    public override string ToString()
    {
        return DiagnosticPath is null
            ? Slot.ToString()
            : $"{Slot} ({DiagnosticPath})";
    }
}
