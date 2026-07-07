using Cerneala.UI.Motion.Diagnostics;
using Cerneala.UI.Motion.Interpolation;
using Cerneala.UI.Motion.Specs;

namespace Cerneala.UI.Motion.Core;

/// <summary>
/// Owns active motion nodes. Cross-thread animation requests must be marshaled
/// through the platform UI dispatcher before calling mutation APIs.
/// </summary>
public sealed class MotionGraph
{
    private readonly MotionThreadGuard threadGuard;
    private readonly ValueMixerRegistry mixers;
    private readonly ReducedMotionPolicy reducedMotion;
    private readonly MotionDiagnostics? diagnostics;
    private readonly List<MotionNode> nodes = [];
    private readonly List<MotionNode> pendingAdds = [];
    private readonly List<MotionNode> pendingRemoves = [];
    private bool isTicking;

    public MotionGraph(MotionThreadGuard threadGuard)
        : this(threadGuard, CreateDefaultMixers(), ReducedMotionPolicy.Default, diagnostics: null)
    {
    }

    public MotionGraph(
        MotionThreadGuard threadGuard,
        ValueMixerRegistry mixers,
        ReducedMotionPolicy reducedMotion,
        MotionDiagnostics? diagnostics = null)
    {
        this.threadGuard = threadGuard ?? throw new ArgumentNullException(nameof(threadGuard));
        this.mixers = mixers ?? throw new ArgumentNullException(nameof(mixers));
        this.reducedMotion = reducedMotion ?? throw new ArgumentNullException(nameof(reducedMotion));
        this.diagnostics = diagnostics;
    }

    public bool HasActiveMotion => nodes.Count > 0 || pendingAdds.Count > 0;

    public int ActiveNodeCount => nodes.Count + pendingAdds.Count;

    internal MotionDiagnostics? Diagnostics => diagnostics;

    public MotionValue<T> CreateValue<T>(T initial, ValueMixer<T>? mixer = null)
    {
        threadGuard.VerifyAccess();
        return new MotionValue<T>(this, mixer ?? mixers.Resolve<T>(), initial);
    }

    public MotionFrameResult Tick(MotionFrame frame)
    {
        threadGuard.VerifyAccess();
        ApplyPendingChanges();
        if (nodes.Count == 0)
        {
            return MotionFrameResult.Empty(frame);
        }

        int sampled = 0;
        int valuesChanged = 0;
        int propertyWrites = 0;
        int completed = 0;
        int renderInvalidations = 0;
        int layoutInvalidations = 0;
        int skippedByReducedMotion = 0;

        isTicking = true;
        try
        {
            for (int i = 0; i < nodes.Count; i++)
            {
                MotionNode node = nodes[i];
                if (pendingRemoves.Contains(node))
                {
                    continue;
                }

                sampled++;
                MotionNodeTickResult result = node.Tick(frame);
                valuesChanged += result.ValuesChanged;
                propertyWrites += result.PropertyWrites;
                completed += result.Completed ? 1 : 0;
                renderInvalidations += result.RenderInvalidations;
                layoutInvalidations += result.LayoutInvalidations;
                skippedByReducedMotion += result.SkippedByReducedMotion;

                if (result.Completed)
                {
                    Unregister(node);
                }
            }
        }
        finally
        {
            isTicking = false;
        }

        ApplyPendingChanges();
        bool needsAnotherFrame = nodes.Count > 0 || pendingAdds.Count > 0;
        return new MotionFrameResult(
            frame,
            needsAnotherFrame,
            1,
            sampled,
            valuesChanged,
            propertyWrites,
            completed,
            renderInvalidations,
            layoutInvalidations,
            skippedByReducedMotion);
    }

    public void Register(MotionNode node)
    {
        threadGuard.VerifyAccess();
        ArgumentNullException.ThrowIfNull(node);
        if (isTicking)
        {
            pendingRemoves.Remove(node);
            if (node.IsRegistered || pendingAdds.Contains(node))
            {
                return;
            }

            pendingAdds.Add(node);
            return;
        }

        if (node.IsRegistered || pendingAdds.Contains(node))
        {
            return;
        }

        AddNode(node);
    }

    public void Unregister(MotionNode node)
    {
        threadGuard.VerifyAccess();
        ArgumentNullException.ThrowIfNull(node);
        if (isTicking)
        {
            pendingAdds.Remove(node);
            if (node.IsRegistered && !pendingRemoves.Contains(node))
            {
                pendingRemoves.Add(node);
            }

            return;
        }

        RemoveNode(node);
    }

    internal MotionSpecContext CreateSpecContext(string? debugName)
    {
        return new MotionSpecContext(reducedMotion, mixers, diagnostics, TimeSpan.Zero, debugName);
    }

    internal void VerifyAccess()
    {
        threadGuard.VerifyAccess();
    }

    private void ApplyPendingChanges()
    {
        if (pendingRemoves.Count > 0)
        {
            foreach (MotionNode node in pendingRemoves)
            {
                RemoveNode(node);
            }

            pendingRemoves.Clear();
        }

        if (pendingAdds.Count > 0)
        {
            foreach (MotionNode node in pendingAdds)
            {
                AddNode(node);
            }

            pendingAdds.Clear();
        }
    }

    private void AddNode(MotionNode node)
    {
        if (node.IsRegistered)
        {
            return;
        }

        node.IsRegistered = true;
        nodes.Add(node);
        node.OnRegistered(this);
    }

    private void RemoveNode(MotionNode node)
    {
        if (!node.IsRegistered)
        {
            return;
        }

        if (nodes.Remove(node))
        {
            node.IsRegistered = false;
            node.OnUnregistered();
        }
    }

    private static ValueMixerRegistry CreateDefaultMixers()
    {
        ValueMixerRegistry registry = new();
        registry.RegisterBuiltIns();
        return registry;
    }
}
