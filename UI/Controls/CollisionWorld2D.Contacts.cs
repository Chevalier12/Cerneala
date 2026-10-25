using System.Runtime.CompilerServices;

namespace Cerneala.UI.Controls;

// Contact events (Collider2D.Entered and Exited). The reported contacts live on the colliders;
// the world only decides which colliders to re-evaluate and computes the difference.
public sealed partial class CollisionWorld2D
{
    // Colliders that have an entry in this world and at least one contact handler.
    private readonly HashSet<Collider2D> trackedEntries = new(ReferenceEqualityComparer.Instance);
    private readonly HashSet<ColliderPair> evaluatedPairs = [];
    private readonly List<Collider2D> contactPartners = [];
    // Colliders to re-evaluate at the next delivery, in the order they changed. Delivery swaps
    // them with the spare pair, so changes made by handlers wait for the next delivery.
    private List<Collider2D> dirtyColliders = [];
    private HashSet<Collider2D> dirtySet = new(ReferenceEqualityComparer.Instance);
    private List<Collider2D> spareDirtyColliders = [];
    private HashSet<Collider2D> spareDirtySet = new(ReferenceEqualityComparer.Instance);
    private bool contactMarkingSuspended;

    // True while a change waits for the next delivery; it may end without any event, so hosts must still deliver.
    internal bool HasPendingContacts => dirtyColliders.Count > 0;

    // Called when the collider's first contact handler is added or its last one is removed.
    internal void OnContactTrackingChanged(Collider2D collider, bool tracked)
    {
        if (!tracked)
        {
            trackedEntries.Remove(collider);
            return;
        }

        if (entriesByCollider.ContainsKey(collider)) { trackedEntries.Add(collider); }
        MarkContactDirty(collider);
    }

    // Marks a collider for the next delivery. A scene without a simulation context never delivers,
    // and a world without any handler has nothing to report, so neither pays for the mark.
    internal void MarkContactDirty(Collider2D collider)
    {
        if (contactMarkingSuspended ||
            owner.SimulationContext is null ||
            (trackedEntries.Count == 0 && !collider.HasContactState))
        {
            return;
        }

        if (dirtySet.Add(collider)) { dirtyColliders.Add(collider); }
    }

    private void OnContactEntryAdded(CollisionEntry2D entry)
    {
        entry.Collider.ContactOrdinal = entry.Ordinal;
        if (entry.Collider.HasContactHandlers) { trackedEntries.Add(entry.Collider); }
        MarkContactDirty(entry.Collider);
    }

    private void OnContactEntryRemoved(Collider2D collider)
    {
        trackedEntries.Remove(collider);
        MarkContactDirty(collider);
    }

    // Entries are rebuilt without per-entry marks: only colliders with contact state can raise events.
    private void BeginContactRebuild()
    {
        trackedEntries.Clear();
        contactMarkingSuspended = true;
    }

    private void EndContactRebuild()
    {
        contactMarkingSuspended = false;
        MarkEntriesWithContactState();
    }

    // The entries are about to vanish without a Remove, so contacts that depend on them are re-evaluated.
    private void ForgetContactEntries()
    {
        MarkEntriesWithContactState();
        trackedEntries.Clear();
    }

    private void MarkEntriesWithContactState()
    {
        if (owner.SimulationContext is null) { return; }

        foreach (Collider2D collider in entriesByCollider.Keys)
        {
            if (collider.HasContactState) { MarkContactDirty(collider); }
        }
    }

    // Phase 1 of a delivery: computes the contact difference for the colliders that changed, updates the
    // reported contacts and queues the events. No handler runs here.
    internal void PrepareContacts(List<PendingContact2D> events, int worldOrder)
    {
        if (dirtyColliders.Count == 0) { return; }

        EnsureCurrent();
        List<Collider2D> batch = dirtyColliders;
        HashSet<Collider2D> batchSet = dirtySet;
        dirtyColliders = spareDirtyColliders;
        dirtySet = spareDirtySet;
        spareDirtyColliders = batch;
        spareDirtySet = batchSet;
        try
        {
            foreach (Collider2D collider in batch)
            {
                PrepareCollider(collider, events, worldOrder);
            }
        }
        finally
        {
            batch.Clear();
            batchSet.Clear();
            evaluatedPairs.Clear();
        }
    }

    private void PrepareCollider(Collider2D collider, List<PendingContact2D> events, int worldOrder)
    {
        entriesByCollider.TryGetValue(collider, out CollisionEntry2D? entry);
        bool hasHandlers = collider.HasContactHandlers;
        if (entry is not null && (hasHandlers || trackedEntries.Count > 0))
        {
            // A collider without handlers only matters to the colliders that listen.
            // Contact within Epsilon counts, so the query reaches as far as the narrow phase does.
            broadphase.Query(IncludeContactFringe(entry.Geometry.SceneBounds), candidates);
            broadphaseCandidateCount += candidates.Count;
            foreach (int id in candidates)
            {
                CollisionEntry2D? target = GetEntry(id);
                if (target is not null &&
                    (hasHandlers || trackedEntries.Contains(target.Collider)))
                {
                    EvaluateContact(entry, target, events, worldOrder);
                }
            }
        }

        // Contacts reported earlier may have ended where the broadphase can no longer see them.
        contactPartners.Clear();
        collider.CopyContactPartners(contactPartners);
        for (int index = 0; index < contactPartners.Count; index++)
        {
            Collider2D partner = contactPartners[index];
            entriesByCollider.TryGetValue(partner, out CollisionEntry2D? partnerEntry);
            EvaluateContact(entry, collider, partnerEntry, partner, events, worldOrder);
        }
    }

    private void EvaluateContact(
        CollisionEntry2D first,
        CollisionEntry2D second,
        List<PendingContact2D> events,
        int worldOrder) =>
        EvaluateContact(first, first.Collider, second, second.Collider, events, worldOrder);

    private void EvaluateContact(
        CollisionEntry2D? firstEntry,
        Collider2D first,
        CollisionEntry2D? secondEntry,
        Collider2D second,
        List<PendingContact2D> events,
        int worldOrder)
    {
        if (ReferenceEquals(first, second) || !evaluatedPairs.Add(new ColliderPair(first, second))) { return; }

        bool touching;
        if (firstEntry is not null && secondEntry is not null)
        {
            touching = IntersectsEntries(firstEntry, secondEntry);
        }
        else if (BelongsToAnotherDeliveringScene(first, second))
        {
            // That scene's world owns the pair, and both colliders were marked there.
            return;
        }
        else
        {
            // At least one collider stopped participating or left this scene.
            touching = false;
        }

        UpdateContact(first, firstEntry, second, secondEntry, touching, events, worldOrder);
        UpdateContact(second, secondEntry, first, firstEntry, touching, events, worldOrder);
    }

    private bool BelongsToAnotherDeliveringScene(Collider2D first, Collider2D second)
    {
        Scene2D? root = SceneGeometry2D.FindRootScene(first);
        return root is not null &&
            !ReferenceEquals(root, owner) &&
            ReferenceEquals(root, SceneGeometry2D.FindRootScene(second)) &&
            root.SimulationContext is not null;
    }

    private static void UpdateContact(
        Collider2D receiver,
        CollisionEntry2D? receiverEntry,
        Collider2D other,
        CollisionEntry2D? otherEntry,
        bool touching,
        List<PendingContact2D> events,
        int worldOrder)
    {
        // A body never reports a Vision: only the sensor learns about the touch, with the body as Other.
        // Two sensors never touch (IntersectsEntries), so a sensor receiver always has a body as Other.
        bool report = touching && receiver.HasContactHandlers && (receiver.IsVisionSensor || !other.IsVisionSensor);
        if (report == receiver.ReportsContactWith(other)) { return; }

        if (report) { receiver.LinkReportedContact(other); }
        else { receiver.UnlinkReportedContact(other); }
        events.Add(new PendingContact2D(
            receiver,
            other,
            report,
            worldOrder,
            receiverEntry?.Ordinal ?? receiver.ContactOrdinal,
            otherEntry?.Ordinal ?? other.ContactOrdinal,
            events.Count));
    }

    // Phase 2 of a delivery: raises the queued events in a fixed order. A handler that throws does not
    // stop the others; the failures are thrown together afterwards. The buffer is empty on return.
    internal static void RaiseContacts(List<PendingContact2D> events)
    {
        if (events.Count == 0) { return; }

        events.Sort(PendingContact2D.Compare);
        List<Exception>? failures = null;
        for (int index = 0; index < events.Count; index++)
        {
            PendingContact2D pending = events[index];
            try
            {
                if (pending.IsEntered) { pending.Receiver.RaiseEntered(pending.Other); }
                else { pending.Receiver.RaiseExited(pending.Other); }
            }
            catch (Exception failure) { (failures ??= []).Add(failure); }
        }

        events.Clear();
        if (failures is not null) { throw new AggregateException(failures); }
    }

    // An unordered pair of colliders, compared by reference.
    private readonly struct ColliderPair : IEquatable<ColliderPair>
    {
        private readonly Collider2D first;
        private readonly Collider2D second;

        internal ColliderPair(Collider2D first, Collider2D second)
        {
            this.first = first;
            this.second = second;
        }

        public bool Equals(ColliderPair other) =>
            (ReferenceEquals(first, other.first) && ReferenceEquals(second, other.second)) ||
            (ReferenceEquals(first, other.second) && ReferenceEquals(second, other.first));

        public override bool Equals(object? obj) => obj is ColliderPair other && Equals(other);

        public override int GetHashCode() =>
            RuntimeHelpers.GetHashCode(first) ^ RuntimeHelpers.GetHashCode(second);
    }
}

// A contact event queued by CollisionWorld2D.PrepareContacts and raised by RaiseContacts.
internal readonly record struct PendingContact2D(
    Collider2D Receiver,
    Collider2D Other,
    bool IsEntered,
    int WorldOrder,
    long ReceiverOrdinal,
    long OtherOrdinal,
    int Sequence)
{
    // Every Exited before every Entered, then the world, the receiver's and the other collider's
    // attachment order, and finally the order of discovery. The keys make a total order.
    internal static int Compare(PendingContact2D left, PendingContact2D right)
    {
        int kind = left.IsEntered.CompareTo(right.IsEntered);
        if (kind != 0) { return kind; }

        int world = left.WorldOrder.CompareTo(right.WorldOrder);
        if (world != 0) { return world; }

        int receiver = left.ReceiverOrdinal.CompareTo(right.ReceiverOrdinal);
        if (receiver != 0) { return receiver; }

        int other = left.OtherOrdinal.CompareTo(right.OtherOrdinal);
        return other != 0 ? other : left.Sequence.CompareTo(right.Sequence);
    }
}
