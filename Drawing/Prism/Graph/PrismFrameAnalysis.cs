using System.Collections.Immutable;

namespace Cerneala.Drawing.Prism.Graph;

public sealed class PrismFrameAnalysis
{
    private readonly DrawCommandList sourceCommands;

    internal PrismFrameAnalysis(
        DrawCommandList sourceCommands,
        long commandListVersion,
        ImmutableArray<PrismAnalyzedScope> scopes,
        PrismGraphCapabilities requiredCapabilities,
        int requiredSurfaceCount,
        PrismBackdropRequirement? backdropRequirement)
    {
        this.sourceCommands = sourceCommands;
        CommandListVersion = commandListVersion;
        Scopes = scopes;
        RequiredCapabilities = requiredCapabilities;
        RequiredSurfaceCount = requiredSurfaceCount;
        BackdropRequirement = backdropRequirement;
    }

    public long CommandListVersion { get; }

    public ImmutableArray<PrismAnalyzedScope> Scopes { get; }

    public PrismGraphCapabilities RequiredCapabilities { get; }

    public int RequiredSurfaceCount { get; }

    public PrismBackdropRequirement? BackdropRequirement { get; }

    public bool RequiresBackdrop => BackdropRequirement is not null;

    public void EnsureCurrent(DrawCommandList commands)
    {
        ArgumentNullException.ThrowIfNull(commands);
        if (!ReferenceEquals(sourceCommands, commands) ||
            commands.Version != CommandListVersion)
        {
            throw new InvalidOperationException(
                "The Prism frame analysis does not match the current draw command list.");
        }

        int? staleScopeIndex = GetStaleScopeIndex();
        if (staleScopeIndex is int scopeIndex)
        {
            throw new InvalidOperationException(
                $"The Prism frame analysis for scope {scopeIndex} is stale.");
        }
    }

    internal void EnsureCurrent()
    {
        EnsureCurrent(sourceCommands);
    }

    internal int? GetStaleScopeIndex()
    {
        if (sourceCommands.Version != CommandListVersion)
        {
            return null;
        }

        foreach (PrismAnalyzedScope analyzedScope in Scopes)
        {
            PrismDrawScope scope = analyzedScope.Scope;
            PrismDependencyStamp stamp = analyzedScope.DependencyStamp;
            if (scope.StructuralVersion != stamp.StructuralVersion ||
                scope.ValueVersion != stamp.ValueVersion)
            {
                return analyzedScope.ScopeIndex;
            }
        }

        return null;
    }
}
