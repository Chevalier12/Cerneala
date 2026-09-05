using System.Numerics;

namespace Cerneala.Drawing.Prism.Graph;

internal readonly record struct PrismAnalyzedScope
{
    internal PrismAnalyzedScope(
        int scopeIndex,
        int beginCommandIndex,
        int endCommandIndex,
        int depth,
        int? parentScopeIndex,
        PrismDrawScope scope,
        DrawRect bounds,
        Matrix3x2 effectiveTransform,
        PrismDependencyStamp dependencyStamp,
        PrismGraphCapabilities requiredCapabilities,
        int requiredSurfaceCount)
    {
        ScopeIndex = scopeIndex;
        BeginCommandIndex = beginCommandIndex;
        EndCommandIndex = endCommandIndex;
        Depth = depth;
        ParentScopeIndex = parentScopeIndex;
        Scope = scope;
        Bounds = bounds;
        EffectiveTransform = effectiveTransform;
        DependencyStamp = dependencyStamp;
        RequiredCapabilities = requiredCapabilities;
        RequiredSurfaceCount = requiredSurfaceCount;
    }

    public int ScopeIndex { get; }

    public int BeginCommandIndex { get; }

    public int EndCommandIndex { get; }

    public int Depth { get; }

    public int? ParentScopeIndex { get; }

    public PrismDrawScope Scope { get; }

    public DrawRect Bounds { get; }

    public Matrix3x2 EffectiveTransform { get; }

    public PrismDependencyStamp DependencyStamp { get; }

    public PrismGraphCapabilities RequiredCapabilities { get; }

    public int RequiredSurfaceCount { get; }
}
