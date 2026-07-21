using System.Collections.Immutable;
using System.Numerics;
using Cerneala.Drawing.Prism.Catalog;
using Cerneala.UI.Prism.Definitions;
using Cerneala.UI.Prism.Runtime;

namespace Cerneala.Drawing.Prism.Graph;

internal sealed class PrismFrameAnalyzer
{
    private const ulong DependencyOffset = 14695981039346656037UL;
    private const ulong DependencyPrime = 1099511628211UL;

    public PrismFrameAnalysis Analyze(DrawCommandList commands)
    {
        ArgumentNullException.ThrowIfNull(commands);

        long commandListVersion = commands.Version;
        int commandCount = commands.Count;
        List<ScopeBuilder> scopes = [];
        List<OpenScope> openScopes = [];
        List<ClipState> clips = [];
        ImmutableArray<int>.Builder backdropScopeIndices = ImmutableArray.CreateBuilder<int>();
        PrismGraphCapabilities frameCapabilities = PrismGraphCapabilities.None;
        int frameSurfaceCount = 0;

        for (int commandIndex = 0; commandIndex < commandCount; commandIndex++)
        {
            DrawCommand command = commands[commandIndex];
            switch (command.Kind)
            {
                case DrawCommandKind.PushClip:
                {
                    DrawRect clip = clips.Count == 0
                        ? command.Rect
                        : Intersect(clips[^1].Bounds, command.Rect);
                    clips.Add(new ClipState(commandIndex, clip));
                    break;
                }

                case DrawCommandKind.PopClip:
                    if (clips.Count == 0)
                    {
                        throw new InvalidOperationException(
                            $"PopClip at command index {commandIndex} has no matching PushClip.");
                    }
                    if (openScopes.Count > 0 &&
                        clips.Count <= openScopes[^1].ClipDepth)
                    {
                        int beginCommandIndex =
                            scopes[openScopes[^1].ScopeIndex].BeginCommandIndex;
                        throw new InvalidOperationException(
                            $"PopClip at command index {commandIndex} crosses BeginPrism at command index {beginCommandIndex}.");
                    }
                    clips.RemoveAt(clips.Count - 1);
                    break;

                case DrawCommandKind.BeginPrism:
                {
                    PrismDrawScope scope = command.PrismScope ??
                        throw new InvalidOperationException(
                            $"BeginPrism at command index {commandIndex} has no scope payload.");
                    DrawRect bounds = TransformBounds(scope.ControlBounds, scope.EffectiveTransform);
                    if (clips.Count > 0)
                    {
                        bounds = Intersect(bounds, clips[^1].Bounds);
                    }

                    bool requiresBackdrop = RequiresBackdrop(scope, bounds);
                    CapabilityEstimate estimate = EstimateCapabilities(
                        scope.Definition,
                        requiresBackdrop);
                    if (IsEmpty(bounds))
                    {
                        estimate = default;
                    }

                    int scopeIndex = scopes.Count;
                    int? parentScopeIndex = openScopes.Count == 0
                        ? null
                        : openScopes[^1].ScopeIndex;
                    scopes.Add(
                        new ScopeBuilder(
                            scopeIndex,
                            commandIndex,
                            openScopes.Count,
                            parentScopeIndex,
                            scope,
                            bounds,
                            estimate.Capabilities,
                            estimate.RequiredSurfaceCount));
                    openScopes.Add(new OpenScope(scopeIndex, clips.Count));
                    frameCapabilities |= estimate.Capabilities;
                    frameSurfaceCount = checked(frameSurfaceCount + estimate.RequiredSurfaceCount);

                    if (requiresBackdrop)
                    {
                        backdropScopeIndices.Add(scopeIndex);
                    }
                    break;
                }

                case DrawCommandKind.EndPrism:
                    if (openScopes.Count == 0)
                    {
                        throw new InvalidOperationException(
                            $"EndPrism at command index {commandIndex} has no matching BeginPrism.");
                    }

                    OpenScope completed = openScopes[^1];
                    if (clips.Count != completed.ClipDepth)
                    {
                        int beginCommandIndex =
                            scopes[completed.ScopeIndex].BeginCommandIndex;
                        throw new InvalidOperationException(
                            $"EndPrism at command index {commandIndex} crosses a clip opened after BeginPrism at command index {beginCommandIndex}.");
                    }
                    openScopes.RemoveAt(openScopes.Count - 1);
                    ScopeBuilder completedScope = scopes[completed.ScopeIndex];
                    completedScope.EndCommandIndex = commandIndex;
                    completedScope.DescendantVersion = completed.DescendantVersion;
                    if (openScopes.Count > 0)
                    {
                        OpenScope parent = openScopes[^1];
                        parent.DescendantVersion = MixDependency(
                            parent.DescendantVersion,
                            completedScope.CreateDependencyStamp());
                    }
                    break;
            }
        }

        if (openScopes.Count > 0)
        {
            int beginCommandIndex = scopes[openScopes[^1].ScopeIndex].BeginCommandIndex;
            throw new InvalidOperationException(
                $"BeginPrism at command index {beginCommandIndex} has no matching EndPrism.");
        }

        if (clips.Count > 0)
        {
            throw new InvalidOperationException(
                $"PushClip at command index {clips[^1].CommandIndex} has no matching PopClip.");
        }

        if (commands.Version != commandListVersion || commands.Count != commandCount)
        {
            throw new InvalidOperationException(
                "The draw command list changed while its Prism frame analysis was being built.");
        }

        ImmutableArray<PrismAnalyzedScope>.Builder analyzedScopes =
            ImmutableArray.CreateBuilder<PrismAnalyzedScope>(scopes.Count);
        foreach (ScopeBuilder scope in scopes)
        {
            analyzedScopes.Add(scope.Build());
        }

        PrismBackdropRequirement? backdropRequirement = backdropScopeIndices.Count == 0
            ? null
            : new PrismBackdropRequirement(backdropScopeIndices.ToImmutable());
        PrismFrameAnalysis analysis = new(
            commands,
            commandListVersion,
            analyzedScopes.MoveToImmutable(),
            frameCapabilities,
            frameSurfaceCount,
            backdropRequirement);
        analysis.EnsureCurrent(commands);
        return analysis;
    }

    private static bool RequiresBackdrop(PrismDrawScope scope, DrawRect bounds)
    {
        PrismBackdropState? backdrop = scope.Instance.Backdrop;
        return !IsEmpty(bounds) &&
            backdrop is { Visible: true, Opacity: > 0 };
    }

    private static CapabilityEstimate EstimateCapabilities(
        PrismCompositionDefinition definition,
        bool includeBackdrop)
    {
        PrismGraphCapabilities capabilities =
            PrismGraphCapabilities.ControlCapture |
            PrismGraphCapabilities.ColorConversion;
        int surfaceCount = 1;
        foreach (PrismNodeDefinition node in definition.Nodes)
        {
            EstimateNode(
                node,
                includeBackdrop,
                ref capabilities,
                ref surfaceCount);
        }

        return new CapabilityEstimate(capabilities, surfaceCount);
    }

    private static void EstimateNode(
        PrismNodeDefinition node,
        bool includeBackdrop,
        ref PrismGraphCapabilities capabilities,
        ref int surfaceCount)
    {
        switch (node)
        {
            case PrismLayerDefinition layer:
                surfaceCount = checked(surfaceCount + 2);
                EstimateOperations(
                    layer.Filters.Length,
                    layer.Styles.Length,
                    layer.Mask is not null,
                    ref capabilities,
                    ref surfaceCount);
                if (layer.ClipToBelow)
                {
                    capabilities |= PrismGraphCapabilities.Clipping;
                    surfaceCount = checked(surfaceCount + 1);
                }
                if (layer.BlendMode != PrismBlendMode.Normal)
                {
                    capabilities |= PrismGraphCapabilities.AdvancedBlending;
                }
                break;

            case PrismGroupDefinition group:
                capabilities |= PrismGraphCapabilities.GroupProcessing;
                surfaceCount = checked(surfaceCount + 1);
                if (group.BlendMode != PrismBlendMode.PassThrough)
                {
                    capabilities |= PrismGraphCapabilities.GroupIsolation;
                    surfaceCount = checked(surfaceCount + 1);
                }
                if (group.BlendMode is not PrismBlendMode.Normal and not PrismBlendMode.PassThrough)
                {
                    capabilities |= PrismGraphCapabilities.AdvancedBlending;
                }
                foreach (PrismNodeDefinition child in group.Children)
                {
                    EstimateNode(
                        child,
                        includeBackdrop,
                        ref capabilities,
                        ref surfaceCount);
                }
                EstimateOperations(
                    group.Filters.Length,
                    group.Styles.Length,
                    group.Mask is not null,
                    ref capabilities,
                    ref surfaceCount);
                break;

            case PrismBackdropDefinition backdrop:
                if (!includeBackdrop)
                {
                    break;
                }
                capabilities |= PrismGraphCapabilities.BackdropInput;
                surfaceCount = checked(surfaceCount + 1);
                EstimateOperations(
                    backdrop.Filters.Length,
                    backdrop.Styles.Length,
                    backdrop.Mask is not null,
                    ref capabilities,
                    ref surfaceCount);
                break;
        }
    }

    private static void EstimateOperations(
        int filterCount,
        int styleCount,
        bool hasMask,
        ref PrismGraphCapabilities capabilities,
        ref int surfaceCount)
    {
        if (filterCount > 0)
        {
            capabilities |= PrismGraphCapabilities.FilterProcessing;
            surfaceCount = checked(surfaceCount + filterCount);
        }
        if (styleCount > 0)
        {
            capabilities |= PrismGraphCapabilities.StyleProcessing;
            surfaceCount = checked(surfaceCount + styleCount);
        }
        if (hasMask)
        {
            capabilities |= PrismGraphCapabilities.MaskProcessing;
            surfaceCount = checked(surfaceCount + 1);
        }
    }

    private static DrawRect TransformBounds(DrawRect bounds, Matrix3x2 transform)
    {
        Vector2 topLeft = Vector2.Transform(new Vector2(bounds.X, bounds.Y), transform);
        Vector2 topRight = Vector2.Transform(new Vector2(bounds.Right, bounds.Y), transform);
        Vector2 bottomLeft = Vector2.Transform(new Vector2(bounds.X, bounds.Bottom), transform);
        Vector2 bottomRight = Vector2.Transform(new Vector2(bounds.Right, bounds.Bottom), transform);
        float left = MathF.Min(MathF.Min(topLeft.X, topRight.X), MathF.Min(bottomLeft.X, bottomRight.X));
        float top = MathF.Min(MathF.Min(topLeft.Y, topRight.Y), MathF.Min(bottomLeft.Y, bottomRight.Y));
        float right = MathF.Max(MathF.Max(topLeft.X, topRight.X), MathF.Max(bottomLeft.X, bottomRight.X));
        float bottom = MathF.Max(MathF.Max(topLeft.Y, topRight.Y), MathF.Max(bottomLeft.Y, bottomRight.Y));
        return new DrawRect(left, top, MathF.Max(0, right - left), MathF.Max(0, bottom - top));
    }

    private static DrawRect Intersect(DrawRect left, DrawRect right)
    {
        float x = MathF.Max(left.X, right.X);
        float y = MathF.Max(left.Y, right.Y);
        float intersectionRight = MathF.Min(left.Right, right.Right);
        float intersectionBottom = MathF.Min(left.Bottom, right.Bottom);
        return new DrawRect(
            x,
            y,
            MathF.Max(0, intersectionRight - x),
            MathF.Max(0, intersectionBottom - y));
    }

    private static bool IsEmpty(DrawRect bounds) =>
        bounds.Width <= 0 || bounds.Height <= 0;

    private static long MixDependency(long aggregate, PrismDependencyStamp stamp)
    {
        ulong hash = aggregate == 0 ? DependencyOffset : unchecked((ulong)aggregate);
        hash = Mix(hash, stamp.CacheOwnerToken.Value);
        hash = Mix(hash, stamp.StructuralVersion.Value);
        hash = Mix(hash, stamp.ValueVersion.Value);
        hash = Mix(hash, stamp.VisualContentVersion);
        hash = Mix(hash, stamp.DescendantVersion);
        return unchecked((long)hash);
    }

    private static ulong Mix(ulong hash, long value) =>
        unchecked((hash ^ (ulong)value) * DependencyPrime);

    private readonly record struct CapabilityEstimate(
        PrismGraphCapabilities Capabilities,
        int RequiredSurfaceCount);

    private readonly record struct ClipState(int CommandIndex, DrawRect Bounds);

    private sealed class OpenScope
    {
        public OpenScope(int scopeIndex, int clipDepth)
        {
            ScopeIndex = scopeIndex;
            ClipDepth = clipDepth;
        }

        public int ScopeIndex { get; }

        public int ClipDepth { get; }

        public long DescendantVersion { get; set; }
    }

    private sealed class ScopeBuilder
    {
        public ScopeBuilder(
            int scopeIndex,
            int beginCommandIndex,
            int depth,
            int? parentScopeIndex,
            PrismDrawScope scope,
            DrawRect bounds,
            PrismGraphCapabilities requiredCapabilities,
            int requiredSurfaceCount)
        {
            ScopeIndex = scopeIndex;
            BeginCommandIndex = beginCommandIndex;
            Depth = depth;
            ParentScopeIndex = parentScopeIndex;
            Scope = scope;
            Bounds = bounds;
            RequiredCapabilities = requiredCapabilities;
            RequiredSurfaceCount = requiredSurfaceCount;
        }

        public int ScopeIndex { get; }

        public int BeginCommandIndex { get; }

        public int EndCommandIndex { get; set; } = -1;

        public int Depth { get; }

        public int? ParentScopeIndex { get; }

        public PrismDrawScope Scope { get; }

        public DrawRect Bounds { get; }

        public PrismGraphCapabilities RequiredCapabilities { get; }

        public int RequiredSurfaceCount { get; }

        public long DescendantVersion { get; set; }

        public PrismDependencyStamp CreateDependencyStamp() =>
            new(
                Scope.CacheOwnerToken,
                Scope.StructuralVersion,
                Scope.ValueVersion,
                Scope.VisualContentVersion,
                DescendantVersion);

        public PrismAnalyzedScope Build() =>
            new(
                ScopeIndex,
                BeginCommandIndex,
                EndCommandIndex,
                Depth,
                ParentScopeIndex,
                Scope,
                Bounds,
                CreateDependencyStamp(),
                RequiredCapabilities,
                RequiredSurfaceCount);
    }
}
