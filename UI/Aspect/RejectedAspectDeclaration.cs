namespace Cerneala.UI.Aspect;

public sealed class RejectedAspectDeclaration
{
    public RejectedAspectDeclaration(AspectDeclaration rejected, AspectDeclaration winningDeclaration, string reason)
    {
        Rejected = rejected ?? throw new ArgumentNullException(nameof(rejected));
        WinningDeclaration = winningDeclaration ?? throw new ArgumentNullException(nameof(winningDeclaration));
        Reason = reason ?? string.Empty;
    }

    public AspectDeclaration Rejected { get; }

    public AspectDeclaration WinningDeclaration { get; }

    public string Reason { get; }
}
