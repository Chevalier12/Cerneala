using System.Numerics;
using Cerneala.Drawing;
using Cerneala.UI.Elements;
using Cerneala.UI.Prism.Runtime;
using Cerneala.UI.Resources;

namespace Cerneala.UI.Controls;

// Parent-space bounds of a container's scene children, kept contiguous so a
// per-frame traversal culls without touching every child object. A sprite
// entry changes only when that sprite reports a geometry change; any other
// node type is marked live and evaluated exactly on every traversal.
internal sealed class SceneChildBoundsIndex2D
{
    // Entries spanning more cells than this are visited on every traversal.
    private const int MaxCellsPerEntry = 64;

    private Entry[] entries = [];
    private int count;
    private readonly List<int> dirty = [];
    private bool rebuildRequired = true;
    private ImageResolutionStamp stamp;

    // A uniform grid over each entry's union of bounds narrows a traversal to
    // the entries near its query. It is only a conservative filter: callers
    // still test every visited entry exactly, in ascending slot order.
    private readonly Dictionary<long, List<int>> cells = [];
    private float cellSize = 256;
    // Entries without finite bounds, not prunable, live or oversized.
    private ulong[] always = [];
    // Entries a traversal must visit even when culled: they may hold images,
    // or have a pending park evaluation.
    private ulong[] attention = [];
    // Parked animations need a visit only when the caller may reactivate them.
    private ulong[] parked = [];
    private ulong[] candidates = [];
    // Only the grid contribution is reused. Attention and parked obligations
    // are merged live by the enumerator, never cached with the cell query.
    private bool candidateCellsCurrent;
    private int candidateX0, candidateY0, candidateX1, candidateY1;

    // Whether the owner evaluates culled children for animation parking.
    private readonly bool parksAnimations;

    internal SceneChildBoundsIndex2D(bool parksAnimations = false) => this.parksAnimations = parksAnimations;

    internal void Invalidate() => rebuildRequired = true;

    internal void MarkDirty(SceneNode2D child)
    {
        int index = child.ContainerSlot;
        if (rebuildRequired || (uint)index >= (uint)count || !ReferenceEquals(entries[index].Node, child))
        {
            return;
        }

        ref Entry entry = ref entries[index];
        if (!entry.Dirty)
        {
            entry.Dirty = true;
            dirty.Add(index);
        }
    }

    internal Span<Entry> Refresh(SceneNode2D owner)
    {
        ImageResolutionStamp current = ImageResourceResolutionEpoch.Capture(owner.Root);
        if (!current.IsSet || stamp != current)
        {
            // Image residency, resources or ancestry changed; sizes may differ.
            rebuildRequired = true;
            stamp = current;
        }

        if (rebuildRequired)
        {
            Rebuild(owner.LogicalChildren);
        }
        else
        {
            for (int position = 0; position < dirty.Count; position++)
            {
                int slot = dirty[position];
                ref Entry entry = ref entries[slot];
                RemoveFromGrid(slot);
                entry.Measure();
                entry.Dirty = false;
                AddToGrid(slot);
                SetBit(attention, slot);
            }
        }

        dirty.Clear();
        return entries.AsSpan(0, count);
    }

    // Visits, in ascending slot order, every entry whose bounds may intersect
    // the query, every always-visited entry and every entry needing
    // attention. Callers that cannot reactivate an offscreen animation may
    // omit its parked-only obligation. Unknown bounds visit every entry.
    // Call Refresh first.
    internal CandidateEnumerator Query(SceneBounds2D bounds, bool includeParkedAnimations = true)
    {
        int words = WordCount(count);
        bool all = bounds.Kind == SceneBoundsKind.Unknown;
        if (bounds.Kind == SceneBoundsKind.Empty)
        {
            Array.Clear(candidates, 0, words);
            candidateCellsCurrent = false;
        }
        if (!all && bounds.Kind == SceneBoundsKind.Known && !TryAddCells(bounds.Bounds))
        {
            all = true;
        }
        return new CandidateEnumerator(this, all, words, includeParkedAnimations);
    }

    internal ref struct CandidateEnumerator
    {
        private readonly SceneChildBoundsIndex2D index;
        private readonly bool all;
        private readonly int words;
        private readonly bool includeParkedAnimations;
        private int word = -1;
        private ulong bits;
        private int previous = -1;

        internal CandidateEnumerator(SceneChildBoundsIndex2D index, bool all, int words, bool includeParkedAnimations)
        {
            this.index = index;
            this.all = all;
            this.words = words;
            this.includeParkedAnimations = includeParkedAnimations;
        }

        public int Current { get; private set; }

        public readonly CandidateEnumerator GetEnumerator() => this;

        public bool MoveNext()
        {
            // The caller has processed the previous entry; keep it for later
            // traversals only while it still needs a visit when culled.
            if (previous >= 0)
            {
                index.UpdateAttention(previous);
            }

            while (bits == 0)
            {
                if (++word >= words)
                {
                    previous = -1;
                    return false;
                }
                bits = all
                    ? ulong.MaxValue
                    : index.candidates[word] | index.attention[word] | index.always[word] |
                        (includeParkedAnimations ? index.parked[word] : 0);
                if (word == words - 1 && (index.count & 63) != 0)
                {
                    bits &= (1UL << (index.count & 63)) - 1;
                }
            }

            int slot = (word << 6) + BitOperations.TrailingZeroCount(bits);
            bits &= bits - 1;
            // Conservatively keep it until the caller has processed it, so a
            // traversal that ends early never drops an entry holding images.
            SetBit(index.attention, slot);
            previous = slot;
            Current = slot;
            return true;
        }
    }

    private void UpdateAttention(int slot)
    {
        ref Entry entry = ref entries[slot];
        if (entry.MayHoldResources || (parksAnimations && !entry.ParkEvaluated))
        {
            SetBit(attention, slot);
        }
        else
        {
            attention[slot >> 6] &= ~(1UL << (slot & 63));
        }
        if (entry.AnimationParkHint) { SetBit(parked, slot); }
        else { ClearBit(parked, slot); }
    }

    private bool TryAddCells(DrawRect rect)
    {
        if (!TryGetCellRange(rect, out int x0, out int y0, out int x1, out int y1))
        {
            return false;
        }
        if (candidateCellsCurrent && candidateX0 == x0 && candidateY0 == y0 &&
            candidateX1 == x1 && candidateY1 == y1)
        {
            return true;
        }
        Array.Clear(candidates, 0, WordCount(count));
        for (int y = y0; y <= y1; y++)
        {
            for (int x = x0; x <= x1; x++)
            {
                if (cells.TryGetValue(CellKey(x, y), out List<int>? slots))
                {
                    foreach (int slot in slots)
                    {
                        SetBit(candidates, slot);
                    }
                }
            }
        }
        candidateX0 = x0; candidateY0 = y0; candidateX1 = x1; candidateY1 = y1;
        candidateCellsCurrent = true;
        return true;
    }

    // Includes one cell of margin so rounding at a cell edge never excludes
    // an entry the exact test would visit.
    private bool TryGetCellRange(DrawRect rect, out int x0, out int y0, out int x1, out int y1)
    {
        const float limit = 1 << 28;
        float left = MathF.Floor(rect.X / cellSize) - 1, top = MathF.Floor(rect.Y / cellSize) - 1;
        float right = MathF.Floor(rect.Right / cellSize) + 1, bottom = MathF.Floor(rect.Bottom / cellSize) + 1;
        if (!(MathF.Abs(left) < limit && MathF.Abs(top) < limit && MathF.Abs(right) < limit && MathF.Abs(bottom) < limit) ||
            (right - left + 1) * (bottom - top + 1) > 1 << 16)
        {
            x0 = y0 = x1 = y1 = 0;
            return false;
        }
        x0 = (int)left; y0 = (int)top; x1 = (int)right; y1 = (int)bottom;
        return true;
    }

    private void AddToGrid(int slot)
    {
        candidateCellsCurrent = false;
        ref Entry entry = ref entries[slot];
        entry.GridCells = false;
        ClearBit(always, slot);
        if (entry.IsLive || !entry.CanPrune || !TryGetGridBounds(entry, out DrawRect? rect))
        {
            SetBit(always, slot);
            return;
        }
        if (rect is not DrawRect bounds)
        {
            // Empty in every kind: no traversal can intersect it.
            return;
        }
        if (!TryGetCellRange(bounds, out int x0, out int y0, out int x1, out int y1) ||
            (long)(x1 - x0 + 1) * (y1 - y0 + 1) > MaxCellsPerEntry)
        {
            SetBit(always, slot);
            return;
        }
        entry.GridCells = true;
        entry.CellX0 = x0; entry.CellY0 = y0; entry.CellX1 = x1; entry.CellY1 = y1;
        for (int y = y0; y <= y1; y++)
        {
            for (int x = x0; x <= x1; x++)
            {
                long key = CellKey(x, y);
                if (!cells.TryGetValue(key, out List<int>? slots))
                {
                    cells[key] = slots = [];
                }
                slots.Add(slot);
            }
        }
    }

    private void RemoveFromGrid(int slot)
    {
        candidateCellsCurrent = false;
        ref Entry entry = ref entries[slot];
        ClearBit(always, slot);
        if (!entry.GridCells)
        {
            return;
        }
        entry.GridCells = false;
        for (int y = entry.CellY0; y <= entry.CellY1; y++)
        {
            for (int x = entry.CellX0; x <= entry.CellX1; x++)
            {
                if (cells.TryGetValue(CellKey(x, y), out List<int>? slots))
                {
                    int at = slots.IndexOf(slot);
                    if (at >= 0)
                    {
                        slots[at] = slots[^1];
                        slots.RemoveAt(slots.Count - 1);
                    }
                }
            }
        }
    }

    // The union of every known bound kind; false when any kind is unknown,
    // null when every kind is empty.
    private static bool TryGetGridBounds(in Entry entry, out DrawRect? bounds)
    {
        bounds = null;
        return Include(entry.VisibleBounds, ref bounds) &&
            Include(entry.HitTestBounds, ref bounds) &&
            Include(entry.PresentationBounds, ref bounds);

        static bool Include(SceneBounds2D value, ref DrawRect? union)
        {
            switch (value.Kind)
            {
                case SceneBoundsKind.Unknown:
                    return false;
                case SceneBoundsKind.Known:
                    DrawRect rect = value.Bounds;
                    if (!float.IsFinite(rect.X) || !float.IsFinite(rect.Y) ||
                        !float.IsFinite(rect.Right) || !float.IsFinite(rect.Bottom))
                    {
                        return false;
                    }
                    if (union is DrawRect current)
                    {
                        float left = MathF.Min(current.X, rect.X), top = MathF.Min(current.Y, rect.Y);
                        float right = MathF.Max(current.Right, rect.Right), bottom = MathF.Max(current.Bottom, rect.Bottom);
                        union = new DrawRect(left, top, right - left, bottom - top);
                    }
                    else
                    {
                        union = rect;
                    }
                    return true;
                default:
                    return true;
            }
        }
    }

    private static long CellKey(int x, int y) => ((long)x << 32) | (uint)y;

    private static int WordCount(int bitCount) => (bitCount + 63) >> 6;

    private static void SetBit(ulong[] bits, int slot) => bits[slot >> 6] |= 1UL << (slot & 63);

    private static void ClearBit(ulong[] bits, int slot) => bits[slot >> 6] &= ~(1UL << (slot & 63));

    // Returns the refreshed entry for a child of this container, if indexed.
    internal bool TryGetEntry(SceneNode2D owner, SceneNode2D child, out Entry entry)
    {
        Span<Entry> current = Refresh(owner);
        int index = child.ContainerSlot;
        if ((uint)index < (uint)current.Length && ReferenceEquals(current[index].Node, child))
        {
            entry = current[index];
            return true;
        }

        entry = default;
        return false;
    }

    // Only a child whose hit-test bounds contain the point, or that owns a hit
    // collider, can be hit; the others need not be evaluated.
    internal List<SceneNode2D> CollectInputCandidates(
        SceneNode2D owner,
        DrawPoint point,
        IReadOnlySet<SceneNode2D>? colliderPaths)
    {
        List<SceneNode2D> result = [];
        Span<Entry> current = Refresh(owner);
        int words = WordCount(count);
        Array.Clear(candidates, 0, words);
        candidateCellsCurrent = false;
        bool all = !TryAddCells(new DrawRect(point.X, point.Y, 0, 0));
        // Collider paths below may add candidates outside these grid cells.
        candidateCellsCurrent = false;
        if (colliderPaths is { Count: > 0 })
        {
            foreach (SceneNode2D node in colliderPaths)
            {
                int slot = node.ContainerSlot;
                if ((uint)slot < (uint)current.Length && ReferenceEquals(current[slot].Node, node))
                {
                    SetBit(candidates, slot);
                }
            }
        }

        for (int word = 0; word < words; word++)
        {
            ulong bits = all ? ulong.MaxValue : candidates[word] | always[word];
            if (word == words - 1 && (count & 63) != 0)
            {
                bits &= (1UL << (count & 63)) - 1;
            }
            while (bits != 0)
            {
                int slot = (word << 6) + BitOperations.TrailingZeroCount(bits);
                bits &= bits - 1;
                ref Entry entry = ref current[slot];
                // Test the contiguous bounds first; only then touch the node.
                SceneNode2D node = entry.Node;
                if ((entry.IsLive || MayContain(entry.HitTestBounds, point) ||
                     (colliderPaths is { Count: > 0 } && colliderPaths.Contains(node))) &&
                    node.ParticipatesInInputRoute)
                {
                    result.Add(node);
                }
            }
        }
        return result;
    }

    // True only when bounds are known and lie outside visible by more than
    // the rounding a child-space test could introduce.
    internal static bool IsClearlyOutside(SceneBounds2D bounds, SceneBounds2D visible)
    {
        if (visible.Kind == SceneBoundsKind.Empty || bounds.Kind == SceneBoundsKind.Empty) { return true; }
        if (visible.Kind == SceneBoundsKind.Unknown || bounds.Kind == SceneBoundsKind.Unknown) { return false; }
        DrawRect content = bounds.Bounds;
        DrawRect view = visible.Bounds;
        float tolerance = Tolerance(content, view);
        return content.X > view.Right + tolerance || content.Right < view.X - tolerance ||
            content.Y > view.Bottom + tolerance || content.Bottom < view.Y - tolerance;
    }

    // True only when known bounds overlap visible by more than that rounding.
    internal static bool IsClearlyIntersecting(SceneBounds2D bounds, SceneBounds2D visible)
    {
        if (visible.Kind != SceneBoundsKind.Known || bounds.Kind != SceneBoundsKind.Known) { return false; }
        DrawRect content = bounds.Bounds;
        DrawRect view = visible.Bounds;
        float tolerance = Tolerance(content, view);
        return content.X < view.Right - tolerance && content.Right > view.X + tolerance &&
            content.Y < view.Bottom - tolerance && content.Bottom > view.Y + tolerance;
    }

    // A presentation check of a clearly visible, effect-free sprite whose
    // resident image is held would change nothing.
    // A settled sprite keeps holding its image until its geometry changes, its
    // container releases it, another release occurs in the scene, or image
    // resolution changes; until then its result is reused without a visit.
    internal static bool IsPresentationSettled(ref Entry entry, SceneBounds2D childVisible, RenderSurface2D? surface)
    {
        if (!entry.CanPrune || !IsClearlyIntersecting(entry.PresentationBounds, childVisible))
        {
            return false;
        }

        ImageResolutionStamp epoch = ImageResourceResolutionEpoch.Capture(surface?.Root);
        long generation = surface?.SceneImageReleaseGeneration ?? 0;
        if (entry.PresentationSettled && epoch.IsSet && entry.SettledEpoch == epoch && entry.SettledGeneration == generation)
        {
            return true;
        }

        entry.PresentationSettled = entry.Node is Sprite2D { HasCurrentResidentImage: true };
        entry.SettledEpoch = epoch;
        entry.SettledGeneration = generation;
        return entry.PresentationSettled;
    }

    // Releases a culled child's images, keeping settled state consistent.
    internal static void Release(ref Entry entry, RenderSurface2D? surface)
    {
        if (!entry.IsLive && !entry.MayHoldResources)
        {
            return;
        }

        entry.PresentationSettled = false;
        if (!entry.IsLive && surface is not null)
        {
            surface.ReleaseTrackedLeaf(entry.Node);
        }
        else
        {
            entry.Node.ReleaseRenderCaches();
        }
        entry.MayHoldResources = false;
    }

    private static bool MayContain(SceneBounds2D bounds, DrawPoint point)
    {
        if (bounds.Kind != SceneBoundsKind.Known) { return false; }
        DrawRect rect = bounds.Bounds;
        float tolerance = 1e-3f + 1e-5f * MathF.Max(MathF.Abs(point.X), MathF.Abs(point.Y));
        return point.X >= rect.X - tolerance && point.X <= rect.Right + tolerance &&
            point.Y >= rect.Y - tolerance && point.Y <= rect.Bottom + tolerance;
    }

    private static float Tolerance(DrawRect content, DrawRect view)
    {
        float magnitude = MathF.Max(
            MathF.Max(MathF.Abs(content.X), MathF.Abs(content.Right)),
            MathF.Max(MathF.Max(MathF.Abs(content.Y), MathF.Abs(content.Bottom)),
                MathF.Max(MathF.Max(MathF.Abs(view.X), MathF.Abs(view.Right)),
                    MathF.Max(MathF.Abs(view.Y), MathF.Abs(view.Bottom)))));
        return 1e-3f + 1e-5f * magnitude;
    }

    private void Rebuild(UIElementCollection children)
    {
        for (int index = 0; index < count; index++)
        {
            entries[index].Node.ContainerSlot = -1;
        }

        count = 0;
        if (entries.Length < children.Count)
        {
            Array.Resize(ref entries, Math.Max(children.Count, entries.Length * 2));
        }

        for (int index = 0; index < children.Count; index++)
        {
            if (children[index] is not SceneNode2D child)
            {
                continue;
            }

            child.ContainerSlot = count;
            // Resources held before this rebuild are unknown; release once if culled.
            entries[count] = new Entry(child)
            {
                MayHoldResources = true,
                AnimationParkHint = child.IsAnimationParked
            };
            entries[count].Measure();
            count++;
        }

        Array.Clear(entries, count, entries.Length - count);
        RebuildGrid();
        rebuildRequired = false;
    }

    // Whether any child is a debug overlay, as of the last refresh.
    internal bool HasOverlay { get; private set; }

    private void RebuildGrid()
    {
        candidateCellsCurrent = false;
        cells.Clear();
        HasOverlay = false;
        for (int slot = 0; slot < count; slot++)
        {
            HasOverlay |= entries[slot].IsOverlay;
        }
        int words = WordCount(count);
        if (always.Length < words)
        {
            int length = Math.Max(words, always.Length * 2);
            always = new ulong[length];
            attention = new ulong[length];
            parked = new ulong[length];
            candidates = new ulong[length];
        }
        Array.Clear(always);
        Array.Clear(candidates);
        Array.Clear(parked);
        // Every rebuilt entry may hold images from before; visit each once.
        Array.Fill(attention, ulong.MaxValue, 0, words);

        // Cells a few entries wide keep a viewport query to few cells while
        // placing each entry in at most a handful of them.
        double extent = 0;
        int known = 0;
        for (int slot = 0; slot < count; slot++)
        {
            if (TryGetGridBounds(entries[slot], out DrawRect? bounds) && bounds is DrawRect rect)
            {
                extent += Math.Max(rect.Width, rect.Height);
                known++;
            }
        }
        cellSize = known == 0 ? 256 : (float)Math.Clamp(extent / known * 4, 8, 4096);
        for (int slot = 0; slot < count; slot++)
        {
            AddToGrid(slot);
        }
    }

    internal struct Entry(SceneNode2D node)
    {
        internal SceneNode2D Node { get; } = node;

        // Debug overlays are drawn as a post-pass, never as ordered content.
        internal bool IsOverlay { get; } = node is Scene2DDebugOverlay;

        // Drawn (and culling) bounds, as SceneNode2D.GetLocalBounds reports them.
        internal SceneBounds2D VisibleBounds { get; private set; }

        internal SceneBounds2D HitTestBounds { get; private set; }

        // Declared bounds used by presentation culling; empty when hidden.
        internal SceneBounds2D PresentationBounds { get; private set; }

        // Other node types can change bounds without reporting; test them live.
        internal bool IsLive { get; private set; }

        // An axis-aligned sprite without its own effect culls identically in
        // parent space, so a clearly separated one can be skipped.
        internal bool CanPrune { get; private set; }

        internal int Layer { get; private set; }

        internal bool Dirty { get; set; }

        internal bool MayHoldResources { get; set; }

        internal bool PresentationSettled { get; set; }

        internal ImageResolutionStamp SettledEpoch { get; set; }

        internal long SettledGeneration { get; set; }

        // Parking was attempted since this child was last drawn or changed.
        internal bool ParkEvaluated { get; set; }

        // This container parked the child's animation (the node is authoritative).
        internal bool AnimationParkHint { get; set; }

        // The grid cells holding this entry, when it is placed in cells.
        internal bool GridCells { get; set; }
        internal int CellX0 { get; set; }
        internal int CellY0 { get; set; }
        internal int CellX1 { get; set; }
        internal int CellY1 { get; set; }

        internal void Measure()
        {
            ParkEvaluated = false;
            if (Node is not Sprite2D sprite)
            {
                IsLive = true;
                return;
            }

            PresentationSettled = false;
            Matrix3x2 transform = sprite.GetLocalTransform();
            VisibleBounds = SceneGeometry2D.TransformBounds(sprite.GetLocalBounds(), transform);
            HitTestBounds = SceneGeometry2D.TransformBounds(sprite.GetHitTestLocalBounds(), transform);
            PresentationBounds = SceneGeometry2D.TransformBounds(sprite.GetPresentationLocalBounds(), transform);
            Layer = sprite.OrderLayer;
            CanPrune = transform.M12 == 0 && transform.M21 == 0 && transform.M11 > 0 && transform.M22 > 0 &&
                !PrismAttachment.TryGetRenderState(sprite, out _, out _);
        }
    }
}
