namespace Cerneala.UI.Aspect;

public sealed class RejectedAspectDeclaration
{
    public RejectedAspectDeclaration(
        AspectRuleSet rejectedRule,
        AspectDeclaration rejected,
        AspectRuleSet winningRule,
        AspectDeclaration winningDeclaration,
        string reason)
    {
        RejectedRule = rejectedRule ?? throw new ArgumentNullException(nameof(rejectedRule));
        Rejected = rejected ?? throw new ArgumentNullException(nameof(rejected));
        WinningRule = winningRule ?? throw new ArgumentNullException(nameof(winningRule));
        WinningDeclaration = winningDeclaration ?? throw new ArgumentNullException(nameof(winningDeclaration));
        Reason = reason ?? string.Empty;
    }

    public AspectRuleSet RejectedRule { get; }

    public AspectDeclaration Rejected { get; }

    public AspectRuleSet WinningRule { get; }

    public AspectDeclaration WinningDeclaration { get; }

    public string Reason { get; }
}
