using System.Collections;
using System.Collections.Specialized;
using System.Collections.ObjectModel;
using System.Reflection;
using System.Runtime.ExceptionServices;
using Cerneala.Drawing;
using Cerneala.UI.Controls;
using Cerneala.UI.Controls.Templates;
using Cerneala.UI.Core;
using Cerneala.UI.Elements;
using Cerneala.UI.Hosting;
using Cerneala.UI.Hosting.Windowing;
using Cerneala.UI.Input;
using Cerneala.UI.Layout;
using Cerneala.UI.Rendering;
using Cerneala.UI.Servo;
using ServoApi = Cerneala.UI.Servo.Servo;

namespace Cerneala.Tests.Controls;

using Scene2D = global::Cerneala.UI.Controls.Scene2D;

public sealed class SceneItems2DSimpleCollectionContractTests
{
    [Fact]
    public void ItemsSourceAcceptsPlainAndObservableEnumerables()
    {
        PropertyInfo property = GetEnumerableItemsSourceProperty();
        Assert.True(property.CanWrite);

        using CollectionFixture fixture = new();
        fixture.Items.Templates.Add(ModelTemplate());
        SetSource(fixture.Items, new[] { new Model("plain") });
        fixture.Attach();
        Assert.Single(fixture.Nodes);

        SetSource(fixture.Items, new ObservableCollection<Model> { new("observable") });
        Assert.Single(fixture.Nodes);
        Assert.Equal("observable", ((Model)fixture.Nodes[0].Model!).Id);
    }

    [Fact]
    public void PlainSourceIsSnapshottedOnceAndRefreshIsTheExplicitResetBoundary()
    {
        Model a = new("a");
        Model b = new("b");
        CountingEnumerable<Model> source = new([a]);
        using CollectionFixture fixture = new();
        fixture.Items.Templates.Add(ModelTemplate());

        SetSource(fixture.Items, source);
        Assert.Equal(1, source.EnumerationCount);
        Assert.Empty(fixture.Items.LogicalChildren);

        fixture.Attach();
        TrackingNode original = Assert.Single(fixture.Nodes);
        Assert.Equal(1, source.EnumerationCount);
        source.Values.Add(b);
        fixture.Tick();
        Assert.Same(original, Assert.Single(fixture.Nodes));

        fixture.Detach();
        Assert.Empty(fixture.Nodes);
        Assert.Equal(0, fixture.Items.RealizedItemCount);
        fixture.Attach();
        Assert.Equal(1, source.EnumerationCount);
        Assert.Single(fixture.Nodes);

        fixture.Items.Refresh();
        Assert.Equal(2, source.EnumerationCount);
        Assert.Equal(2, fixture.Nodes.Length);
        Assert.NotSame(original, fixture.Nodes[0]);
    }

    [Fact]
    public void OneShotPlainSourceIsNotConsumedAgainByAttachOrReattach()
    {
        OneShotEnumerable<Model> source = new([new Model("once")]);
        using CollectionFixture fixture = new();
        fixture.Items.Templates.Add(ModelTemplate());
        SetSource(fixture.Items, source);
        Assert.Equal(1, source.EnumerationCount);

        fixture.Attach();
        Assert.Single(fixture.Nodes);
        fixture.Detach();
        Assert.Empty(fixture.Nodes);
        Assert.Equal(0, fixture.Items.RealizedItemCount);
        fixture.Attach();
        Assert.Single(fixture.Nodes);
        Assert.Equal(1, source.EnumerationCount);
    }

    [Fact]
    public void ObservableDeltasKeepUnaffectedOccurrenceNodesEvenForDuplicateReferences()
    {
        Model repeated = new("same object");
        Model middle = new("middle");
        Model inserted = new("inserted");
        Model replacement = new("replacement");
        ObservableCollection<Model> source = new() { repeated, middle, repeated };
        using CollectionFixture fixture = new();
        fixture.Items.Templates.Add(ModelTemplate());
        SetSource(fixture.Items, source);
        fixture.Attach();
        TrackingNode[] initial = fixture.Nodes;
        Assert.Equal(3, initial.Length);
        Assert.NotSame(initial[0], initial[2]);

        source.Move(0, 2);
        Assert.Equal(new[] { initial[1], initial[2], initial[0] }, fixture.Nodes);

        source.Insert(1, inserted);
        TrackingNode insertedNode = fixture.Nodes[1];
        Assert.Equal(new[] { initial[1], insertedNode, initial[2], initial[0] }, fixture.Nodes);

        source.RemoveAt(2);
        Assert.Equal(new[] { initial[1], insertedNode, initial[0] }, fixture.Nodes);
        Assert.Equal(1, initial[2].DetachCount);

        source[1] = replacement;
        TrackingNode replacementNode = fixture.Nodes[1];
        Assert.Equal(new[] { initial[1], replacementNode, initial[0] }, fixture.Nodes);
        Assert.NotSame(insertedNode, replacementNode);
        Assert.Equal(1, insertedNode.DetachCount);
        Assert.All(fixture.Nodes, node => Assert.Equal(-1, node.TemplateIndex));
    }

    [Fact]
    public void ObservableNotificationsEnumeratePostEventStateOnceAndInvalidIndicesReset()
    {
        Model a = new("a");
        Model b = new("b");
        Model c = new("c");
        Model d = new("d");
        EventSource<Model> source = new([a, b]);
        using CollectionFixture fixture = new();
        fixture.Items.Templates.Add(ModelTemplate());
        SetSource(fixture.Items, source);
        Assert.Equal(1, source.EnumerationCount);
        Assert.Equal(0, source.SubscriberCount);
        fixture.Attach();
        Assert.Equal(2, source.EnumerationCount);
        Assert.Equal(1, source.SubscriberCount);
        int enumerations = source.EnumerationCount;
        TrackingNode[] original = fixture.Nodes;

        source.Add(c);
        Assert.Equal(++enumerations, source.EnumerationCount);
        Assert.Same(original[0], fixture.Nodes[0]);
        source.Move(0, 2);
        Assert.Equal(++enumerations, source.EnumerationCount);
        Assert.Same(original[0], fixture.Nodes[2]);
        source.RemoveAt(1);
        Assert.Equal(++enumerations, source.EnumerationCount);
        source.Replace(0, d);
        Assert.Equal(++enumerations, source.EnumerationCount);

        TrackingNode beforeReset = fixture.Nodes[0];
        source.Reset();
        Assert.Equal(++enumerations, source.EnumerationCount);
        Assert.NotSame(beforeReset, fixture.Nodes[0]);

        TrackingNode beforeRange = fixture.Nodes[0];
        source.AddRange([a, b]);
        Assert.Equal(++enumerations, source.EnumerationCount);
        Assert.Same(beforeRange, fixture.Nodes[0]);

        TrackingNode beforeInvalidIndex = fixture.Nodes[0];
        source.AddRangeWithInvalidIndex([a, b]);
        Assert.Equal(++enumerations, source.EnumerationCount);
        Assert.Equal(source.Values.Count, fixture.Nodes.Length);
        Assert.NotSame(beforeInvalidIndex, fixture.Nodes[0]);

        fixture.Detach();
        Assert.Equal(0, source.SubscriberCount);
        Assert.Equal(enumerations, source.EnumerationCount);
        Assert.Empty(fixture.Nodes);
        Assert.Equal(0, fixture.Items.RealizedItemCount);
        fixture.Attach();
        Assert.Equal(1, source.SubscriberCount);
        Assert.Equal(enumerations + 1, source.EnumerationCount);
    }

    [Fact]
    public void ResetSourceReplacementAndTemplateReplacementRecreateOccurrences()
    {
        Model a = new("a");
        Model b = new("b");
        ObservableCollection<Model> source = new() { a, b };
        using CollectionFixture fixture = new();
        fixture.Items.Templates.Add(ModelTemplate());
        SetSource(fixture.Items, source);
        fixture.Attach();
        TrackingNode[] beforeReset = fixture.Nodes;

        source.Clear();
        Assert.Empty(fixture.Nodes);
        source.Add(a);
        TrackingNode afterReset = Assert.Single(fixture.Nodes);
        Assert.NotSame(beforeReset[0], afterReset);

        SetSource(fixture.Items, new[] { a });
        TrackingNode afterSourceReplacement = Assert.Single(fixture.Nodes);
        Assert.NotSame(afterReset, afterSourceReplacement);

        fixture.Items.Templates[0] = ModelTemplate("replacement-template");
        TrackingNode afterTemplateReplacement = Assert.Single(fixture.Nodes);
        Assert.NotSame(afterSourceReplacement, afterTemplateReplacement);
        Assert.Equal(1, afterSourceReplacement.DetachCount);
    }

    [Fact]
    public void OccurrenceDataContextAndTemplateIndexStayBoundToValueNotPosition()
    {
        Model a = new("a");
        Model b = new("b");
        ObservableCollection<Model> source = new() { a, b };
        using CollectionFixture fixture = new();
        fixture.Items.Templates.Add(ModelTemplate());
        SetSource(fixture.Items, source);
        fixture.Attach();

        Assert.Same(a, fixture.Nodes[0].DataContext);
        Assert.Same(b, fixture.Nodes[1].DataContext);
        Assert.All(fixture.Nodes, node => Assert.Equal(-1, node.TemplateIndex));

        source.Move(0, 1);
        Assert.Same(b, fixture.Nodes[0].DataContext);
        Assert.Same(a, fixture.Nodes[1].DataContext);
        Assert.All(fixture.Nodes, node => Assert.Equal(-1, node.TemplateIndex));
    }

    [Fact]
    public void NullIsARealOccurrenceWithItsOwnNullCapableTemplate()
    {
        object value = new();
        using CollectionFixture fixture = new();
        fixture.Items.Templates.Add(new ContentTemplate("null", dataType: null, key: null, priority: 0,
            factory: context => new TrackingNode(context.Data, context.Index)));
        fixture.Items.Templates.Add(new ContentTemplate<object>("object", null, 0,
            context => new TrackingNode(context.Data, context.Index)));
        SetSource(fixture.Items, new object?[] { null, value, null });
        fixture.Attach();

        TrackingNode[] nodes = fixture.Nodes;
        Assert.Equal(3, nodes.Length);
        Assert.Null(nodes[0].Model);
        Assert.Same(value, nodes[1].Model);
        Assert.Null(nodes[2].Model);
        Assert.NotSame(nodes[0], nodes[2]);
        Assert.All(nodes, node => Assert.Equal(-1, node.TemplateIndex));
    }

    [Fact]
    public void NullWithoutNullCapableTemplateFailsWithoutNullDereference()
    {
        using CollectionFixture fixture = new();
        fixture.Items.Templates.Add(new ContentTemplate<object>("object", null, 0,
            context => new TrackingNode(context.Data, context.Index)));
        SetSource(fixture.Items, new object?[] { null });

        Exception attachmentError = Assert.ThrowsAny<Exception>(fixture.Attach);
        IReadOnlyList<Exception> leaves = attachmentError is AggregateException aggregate
            ? aggregate.Flatten().InnerExceptions
            : [attachmentError];
        InvalidOperationException templateError = Assert.IsType<InvalidOperationException>(Assert.Single(leaves));
        Assert.Contains("template", templateError.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void ObservableSourceResnapshotsOnReattachAndOwnerRelayCanApplyDetachedChanges()
    {
        Model a = new("a");
        Model b = new("b");
        ObservableCollection<Model> source = new() { a };
        using CollectionFixture fixture = new();
        fixture.Items.Templates.Add(ModelTemplate());
        SetSource(fixture.Items, source);
        fixture.Attach();
        Assert.Single(fixture.Nodes);

        fixture.Detach();
        Assert.Empty(fixture.Nodes);
        source.Add(b);
        Assert.Empty(fixture.Nodes);
        fixture.Attach();
        Assert.Equal(new object?[] { a, b }, fixture.Nodes.Select(node => node.Model).ToArray());

        Model c = new("c");
        Task posting = Task.Run(() => fixture.Root.Relay.Post(() => source.Add(c)));
        Assert.True(SpinWait.SpinUntil(() => posting.IsCompleted, TimeSpan.FromSeconds(5)),
            "Owner relay posting did not complete.");
        Assert.Null(posting.Exception);
        fixture.Tick();
        Assert.Equal(new object?[] { a, b, c }, fixture.Nodes.Select(node => node.Model).ToArray());
    }

    [Fact]
    public void AttachedSourceMutationAssignmentAndRefreshRejectOffOwnerCalls()
    {
        ObservableCollection<Model> source = new() { new("a") };
        using CollectionFixture fixture = new();
        fixture.Items.Templates.Add(ModelTemplate());
        SetSource(fixture.Items, source);
        fixture.Attach();
        TrackingNode original = Assert.Single(fixture.Nodes);

        Assert.NotNull(RunOnWorker(() => SetSource(fixture.Items, new[] { new Model("b") })));
        Assert.NotNull(RunOnWorker(fixture.Items.Refresh));
        Assert.NotNull(RunOnWorker(() => source.Add(new Model("c"))));
        Assert.Same(original, Assert.Single(fixture.Nodes));

        fixture.Detach();
        Assert.NotNull(RunOnWorker(() => SetSource(fixture.Items, new[] { new Model("detached") })));
        Assert.NotNull(RunOnWorker(fixture.Items.Refresh));
    }

    [Fact]
    public void TemplatePreflightFailureKeepsCommittedTreeAndNewSourceCanNotifyRetry()
    {
        Model a = new("a");
        Model b = new("b");
        EventSource<Model> previousSource = new([a]);
        EventSource<Model> requestedSource = new([a, b]);
        bool fail = false;
        using CollectionFixture fixture = new();
        fixture.Items.Templates.Add(new ContentTemplate<Model>("failure", null, 0, context =>
        {
            if (fail && ReferenceEquals(context.Data, b))
                throw new InvalidOperationException("candidate factory failed");
            return new TrackingNode(context.Data, context.Index);
        }));
        SetSource(fixture.Items, previousSource);
        fixture.Attach();
        TrackingNode committed = Assert.Single(fixture.Nodes);
        Assert.Equal(1, previousSource.SubscriberCount);

        fail = true;
        InvalidOperationException error = Assert.Throws<InvalidOperationException>(() => SetSource(fixture.Items, requestedSource));
        Assert.Equal("candidate factory failed", error.Message);
        Assert.Equal(0, previousSource.SubscriberCount);
        Assert.Equal(1, requestedSource.SubscriberCount);
        Assert.Same(committed, Assert.Single(fixture.Nodes));
        Assert.Equal(0, committed.DetachCount);
        Assert.Equal(1, fixture.Items.RealizedItemCount);

        fail = false;
        requestedSource.Reset();
        Assert.Equal(2, fixture.Nodes.Length);
        Assert.NotSame(committed, fixture.Nodes[0]);
    }

    [Fact]
    public async Task TemplateEditAfterFailedRebindCommitsRequestedOneShotSnapshot()
    {
        using CollectionFixture fixture = new();
        fixture.Items.Templates.Add(new ContentTemplate<int>("integer", null, 0,
            context => new TrackingNode(context.Data, context.Index)));
        SetSource(fixture.Items, new[] { 1 });
        fixture.Attach();
        TrackingNode committed = Assert.Single(fixture.Nodes);

        OneShotEnumerable<string> requested = new(["b"]);
        InvalidOperationException failure = Assert.Throws<InvalidOperationException>(() =>
            SetSource(fixture.Items, requested));
        Assert.Contains("template", failure.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Same(requested, fixture.Items.ItemsSource);
        Assert.Equal(1, requested.EnumerationCount);
        Assert.Same(committed, Assert.Single(fixture.Nodes));
        Assert.Equal(0, committed.DetachCount);
        Assert.NotNull(fixture.Items.CollisionReadinessError);

        fixture.Items.Templates.Add(new ContentTemplate<string>("string", null, 0,
            context => new TrackingNode(context.Data, context.Index)));

        TrackingNode recovered = Assert.Single(fixture.Nodes);
        Assert.Equal("b", recovered.Model);
        Assert.Equal("b", recovered.DataContext);
        Assert.NotSame(committed, recovered);
        Assert.Equal(1, committed.DetachCount);
        Assert.Equal(1, requested.EnumerationCount);
        Assert.Null(fixture.Items.CollisionReadinessError);

        ValueTask<SceneCollisionRegion2D> preparation = fixture.Scene.CollisionWorld
            .PrepareRegionAsync(new DrawRect(0, 0, 10, 10));
        Assert.True(SpinWait.SpinUntil(() =>
        {
            fixture.Tick();
            return preparation.IsCompleted;
        }, TimeSpan.FromSeconds(5)), "Recovered collection collision preparation did not complete.");
        using SceneCollisionRegion2D region = await preparation;
    }

    [Fact]
    public void TemplateEditAfterFailedSourceEnumerationKeepsOldTreeUnreadyUntilExplicitRefresh()
    {
        using CollectionFixture fixture = new();
        fixture.Items.Templates.Add(new ContentTemplate<int>("integer", null, 0,
            context => new TrackingNode(context.Data, context.Index)));
        SetSource(fixture.Items, new[] { 1 });
        fixture.Attach();
        TrackingNode committed = Assert.Single(fixture.Nodes);

        CountingEnumerable<string> requested = new(["b"]) { ThrowOnEnumeration = true };
        InvalidOperationException failure = Assert.Throws<InvalidOperationException>(() =>
            SetSource(fixture.Items, requested));
        Assert.Equal("source enumeration failed", failure.Message);
        Assert.Equal(1, requested.EnumerationCount);
        Assert.NotNull(fixture.Items.CollisionReadinessError);

        requested.ThrowOnEnumeration = false;
        fixture.Items.Templates.Add(new ContentTemplate<string>("string", null, 0,
            context => new TrackingNode(context.Data, context.Index)));
        Assert.Same(committed, Assert.Single(fixture.Nodes));
        Assert.Equal(1, requested.EnumerationCount);
        Assert.NotNull(fixture.Items.CollisionReadinessError);

        fixture.Items.Refresh();
        Assert.Equal(2, requested.EnumerationCount);
        Assert.Equal("b", Assert.Single(fixture.Nodes).Model);
        Assert.Null(fixture.Items.CollisionReadinessError);
    }

    [Fact]
    public void TemplateEditAfterFailedRefreshCannotMarkStaleSameSourceSnapshotReady()
    {
        CountingEnumerable<int> source = new([1]);
        using CollectionFixture fixture = new();
        fixture.Items.Templates.Add(new ContentTemplate<int>("integer", null, 0,
            context => new TrackingNode(context.Data, context.Index)));
        SetSource(fixture.Items, source);
        fixture.Attach();
        TrackingNode committed = Assert.Single(fixture.Nodes);
        Assert.Equal(1, source.EnumerationCount);

        source.Values.Add(2);
        source.ThrowOnEnumeration = true;
        InvalidOperationException failure = Assert.Throws<InvalidOperationException>(fixture.Items.Refresh);
        Assert.Equal("source enumeration failed", failure.Message);
        Assert.Same(committed, Assert.Single(fixture.Nodes));
        Assert.NotNull(fixture.Items.CollisionReadinessError);

        source.ThrowOnEnumeration = false;
        fixture.Items.Templates[0] = new ContentTemplate<int>("replacement", null, 0,
            context => new TrackingNode(context.Data, context.Index));
        Assert.Same(committed, Assert.Single(fixture.Nodes));
        Assert.Equal(2, source.EnumerationCount);
        Assert.NotNull(fixture.Items.CollisionReadinessError);

        fixture.Items.Refresh();
        Assert.Equal(3, source.EnumerationCount);
        Assert.Equal(new object?[] { 1, 2 }, fixture.Nodes.Select(node => node.Model).ToArray());
        Assert.Null(fixture.Items.CollisionReadinessError);
    }

    [Fact]
    public void NeverAdoptedCandidateCleanupErrorIsReportedWithPrimaryPreflightFailure()
    {
        Model committedModel = new("committed");
        Model stagedModel = new("staged");
        Model failingModel = new("failing");
        CacheReleaseFaultNode? staged = null;
        using CollectionFixture fixture = new();
        fixture.Items.Templates.Add(new ContentTemplate<Model>("cleanup", null, 0, context =>
        {
            if (ReferenceEquals(context.Data, failingModel))
                throw new InvalidOperationException("candidate factory failed");
            if (ReferenceEquals(context.Data, stagedModel))
                return staged = new CacheReleaseFaultNode(context.Data, context.Index);
            return new TrackingNode(context.Data, context.Index);
        }));
        SetSource(fixture.Items, new[] { committedModel });
        fixture.Attach();
        TrackingNode committed = Assert.Single(fixture.Nodes);

        AggregateException failure = Assert.Throws<AggregateException>(() =>
            SetSource(fixture.Items, new[] { stagedModel, failingModel }));

        Assert.Collection(failure.InnerExceptions,
            primary => Assert.Equal("candidate factory failed", primary.Message),
            cleanup => Assert.Equal("candidate cache release failed", cleanup.Message));
        Assert.NotNull(staged);
        Assert.Null(staged.LogicalParent);
        Assert.Equal(0, staged.AttachCount);
        Assert.Equal(1, staged.CacheReleaseCount);
        Assert.Same(committed, Assert.Single(fixture.Nodes));
        Assert.Equal(0, committed.DetachCount);
    }

    [Fact]
    public void FailedPreflightPreservesOldColliderButDoesNotReportRequestedSourceReady()
    {
        Model oldModel = new("old");
        Model failingModel = new("failing");
        Sprite2D? committed = null;
        using CollectionFixture fixture = new();
        fixture.Items.Templates.Add(new ContentTemplate<Model>("collider", null, 0, context =>
        {
            if (ReferenceEquals(context.Data, failingModel))
                throw new InvalidOperationException("requested candidate factory failed");
            Sprite2D sprite = new() { Collider = new BoxCollider2D { Width = 10, Height = 10 } };
            committed ??= sprite;
            return sprite;
        }));
        SetSource(fixture.Items, new[] { oldModel });
        fixture.Attach();
        Assert.Same(committed, Assert.Single(fixture.Items.LogicalChildren));

        InvalidOperationException preflight = Assert.Throws<InvalidOperationException>(() =>
            SetSource(fixture.Items, new[] { oldModel, failingModel }));
        Assert.Equal("requested candidate factory failed", preflight.Message);
        Assert.Same(committed, Assert.Single(fixture.Items.LogicalChildren));
        Assert.Equal(1, fixture.Items.RealizedItemCount);

        Task<SceneCollisionRegion2D> preparing = fixture.Scene.CollisionWorld
            .PrepareRegionAsync(new DrawRect(0, 0, 10, 10)).AsTask();
        Assert.True(SpinWait.SpinUntil(() =>
        {
            Exception? pumpError = Record.Exception(fixture.Tick);
            if (pumpError is not null)
                Assert.Contains("requested candidate factory failed", pumpError.ToString(), StringComparison.Ordinal);
            return preparing.IsCompleted;
        }, TimeSpan.FromSeconds(5)), "Collision preparation did not finish after failed preflight.");
        Assert.True(preparing.IsFaulted);
        AggregateException readinessFailure = Assert.IsType<AggregateException>(preparing.Exception);
        Assert.Contains("requested candidate factory failed", readinessFailure.ToString(), StringComparison.Ordinal);
        Assert.Same(committed, Assert.Single(fixture.Items.LogicalChildren));
    }

    [Fact]
    public void EnumerationFailurePreservesCommittedTreeAndRefreshCanRetry()
    {
        Model a = new("a");
        Model b = new("b");
        CountingEnumerable<Model> source = new([a]);
        using CollectionFixture fixture = new();
        fixture.Items.Templates.Add(ModelTemplate());
        SetSource(fixture.Items, source);
        fixture.Attach();
        TrackingNode committed = Assert.Single(fixture.Nodes);

        source.Values.Add(b);
        source.ThrowOnEnumeration = true;
        InvalidOperationException error = Assert.Throws<InvalidOperationException>(fixture.Items.Refresh);
        Assert.Equal("source enumeration failed", error.Message);
        Assert.Same(committed, Assert.Single(fixture.Nodes));
        Assert.Equal(1, fixture.Items.RealizedItemCount);

        source.ThrowOnEnumeration = false;
        fixture.Items.Refresh();
        Assert.Equal(2, fixture.Nodes.Length);
    }

    [Fact]
    public void ReentrantFactorySourceChangeCannotPublishItsStaleCandidate()
    {
        GetEnumerableItemsSourceProperty();
        Model staleModel = new("stale");
        Model newerModel = new("newer");
        using CollectionFixture fixture = new();
        bool reentered = false;
        TrackingNode? staleCandidate = null;
        fixture.Items.Templates.Add(new ContentTemplate<Model>("reentrant", null, 0, context =>
        {
            TrackingNode node = new(context.Data, context.Index);
            if (!reentered && ReferenceEquals(context.Data, staleModel))
            {
                reentered = true;
                staleCandidate = node;
                SetSource(fixture.Items, new[] { newerModel });
            }
            return node;
        }));
        fixture.Attach();

        Record.Exception(() => SetSource(fixture.Items, new[] { staleModel }));
        Assert.NotNull(staleCandidate);
        Assert.DoesNotContain(fixture.Items.LogicalChildren, node => ReferenceEquals(node, staleCandidate));
        Assert.Equal(0, staleCandidate.AttachCount);
        Assert.DoesNotContain(fixture.Nodes, node => ReferenceEquals(node.Model, staleModel));
    }

    [Fact]
    public void ReentrantTemplateEditUsesTheCompletedOneShotSnapshot()
    {
        Model model = new("one-shot");
        OneShotEnumerable<Model> source = new([model]);
        using CollectionFixture fixture = new();
        TrackingNode? firstCandidate = null;
        TrackingNode? replacementCandidate = null;
        fixture.Items.Templates.Add(new ContentTemplate<Model>("first", null, 0,
            context => firstCandidate = new TrackingNode(context.Data, context.Index)));
        ContentTemplate<Model> replacement = new("replacement", null, 0,
            context => replacementCandidate = new TrackingNode(context.Data, context.Index));
        fixture.Attach();
        bool once = true;
        fixture.Items.LogicalChildren.Changed += (_, change) =>
        {
            if (!once || change.Child is not TrackingNode node || !ReferenceEquals(node.Model, model)) return;
            once = false;
            fixture.Items.Templates[0] = replacement;
        };

        SetSource(fixture.Items, source);

        Assert.Same(source, fixture.Items.ItemsSource);
        Assert.Same(replacement, fixture.Items.Templates[0]);
        Assert.NotNull(firstCandidate);
        Assert.NotNull(replacementCandidate);
        Assert.Same(replacementCandidate, Assert.Single(fixture.Nodes));
        Assert.NotSame(firstCandidate, replacementCandidate);
        Assert.Same(model, replacementCandidate.DataContext);
        Assert.Equal(1, firstCandidate.DetachCount);
        Assert.Equal(1, source.EnumerationCount);
        Assert.Equal(1, fixture.Items.RealizedItemCount);
        Assert.Null(fixture.Items.CollisionReadinessError);
    }

    [Fact]
    public void ReentrantExplicitRefreshStillEnumeratesAfterStructuralMutation()
    {
        Model model = new("refresh");
        CountingEnumerable<Model> source = new([model]);
        using CollectionFixture fixture = new();
        fixture.Items.Templates.Add(ModelTemplate());
        SetSource(fixture.Items, source);
        fixture.Attach();
        TrackingNode original = Assert.Single(fixture.Nodes);
        bool once = true;
        fixture.Items.LogicalChildren.Changed += (_, change) =>
        {
            if (!once || !ReferenceEquals(change.Child, original)) return;
            once = false;
            fixture.Items.Refresh();
        };

        fixture.Items.Templates[0] = ModelTemplate("replacement");

        Assert.Equal(2, source.EnumerationCount);
        Assert.NotSame(original, Assert.Single(fixture.Nodes));
        Assert.Equal(1, original.DetachCount);
        Assert.Null(fixture.Items.CollisionReadinessError);
    }

    [Fact]
    public void SuccessfulStructuralReentryStopsBeforeAttachingLaterStaleCandidates()
    {
        Model a = new("a");
        Model firstStale = new("first stale");
        Model laterStale = new("later stale");
        Model replacement = new("replacement");
        EventSource<Model> source = new([a]);
        TrackingNode? laterCandidate = null;
        using CollectionFixture fixture = new();
        fixture.Items.Templates.Add(new ContentTemplate<Model>("reentrant", null, 0, context =>
        {
            TrackingNode node = new(context.Data, context.Index);
            if (ReferenceEquals(context.Data, laterStale)) laterCandidate = node;
            return node;
        }));
        SetSource(fixture.Items, source);
        fixture.Attach();
        bool once = true;
        fixture.Items.LogicalChildren.Changed += (_, change) =>
        {
            if (!once || change.Child is not TrackingNode node || !ReferenceEquals(node.Model, firstStale)) return;
            once = false;
            SetSource(fixture.Items, new[] { replacement });
        };

        source.AddRange([firstStale, laterStale]);

        Assert.NotNull(laterCandidate);
        Assert.Equal(0, laterCandidate.AttachCount);
        Assert.Equal(new object?[] { replacement }, fixture.Nodes.Select(node => node.Model).ToArray());
        Assert.Equal(fixture.Items.LogicalChildren.Count, fixture.Items.RealizedItemCount);
        Assert.Equal(0, source.SubscriberCount);
    }

    [Fact]
    public void ContextDetachDuringStructuralCallbackRetiresPartialGenerationBeforeReattach()
    {
        Model a = new("a");
        Model b = new("b");
        Model c = new("c");
        EventSource<Model> source = new([a]);
        TrackingNode? laterCandidate = null;
        using CollectionFixture fixture = new();
        fixture.Items.Templates.Add(new ContentTemplate<Model>("context", null, 0, context =>
        {
            TrackingNode node = new(context.Data, context.Index);
            if (ReferenceEquals(context.Data, c)) laterCandidate = node;
            return node;
        }));
        SetSource(fixture.Items, source);
        fixture.Attach();
        bool once = true;
        fixture.Items.LogicalChildren.Changed += (_, change) =>
        {
            if (!once || change.Child is not TrackingNode node || !ReferenceEquals(node.Model, b)) return;
            once = false;
            fixture.Detach();
        };

        source.AddRange([b, c]);

        Assert.NotNull(laterCandidate);
        Assert.Equal(0, laterCandidate.AttachCount);
        Assert.Empty(fixture.Nodes);
        Assert.Equal(0, fixture.Items.RealizedItemCount);
        Assert.Equal(0, source.SubscriberCount);
        fixture.Attach();
        Assert.Equal(new object?[] { a, b, c }, fixture.Nodes.Select(node => node.Model).ToArray());
        Assert.Equal(1, source.SubscriberCount);
    }

    [Fact]
    public void OneSceneNodeCannotRepresentTwoOccurrences()
    {
        Model a = new("a");
        Model repeated = new("repeated");
        TrackingNode shared = new(repeated, -1);
        using CollectionFixture fixture = new();
        fixture.Items.Templates.Add(new ContentTemplate<Model>("shared", null, 0, context =>
            ReferenceEquals(context.Data, repeated) ? shared : new TrackingNode(context.Data, context.Index)));
        SetSource(fixture.Items, new[] { a });
        fixture.Attach();
        TrackingNode committed = Assert.Single(fixture.Nodes);

        InvalidOperationException failure = Assert.Throws<InvalidOperationException>(() =>
            SetSource(fixture.Items, new[] { repeated, repeated }));
        Assert.Contains("node", failure.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Same(committed, Assert.Single(fixture.Nodes));
        Assert.Equal(0, committed.DetachCount);
    }

    [Theory]
    [InlineData(StructuralFault.Attach)]
    [InlineData(StructuralFault.Detach)]
    [InlineData(StructuralFault.Changed)]
    public void StructuralCallbackFaultIsTerminalAndCountMatchesActualMembership(StructuralFault fault)
    {
        Model a = new("a");
        Model b = new("b");
        EventSource<Model> source = new([a]);
        using CollectionFixture fixture = new();
        FaultingNode? oldNode = null;
        fixture.Items.Templates.Add(new ContentTemplate<Model>("fault", null, 0, context =>
        {
            FaultingNode node = new(context.Data, context.Index);
            if (ReferenceEquals(context.Data, a)) oldNode = node;
            if (fault == StructuralFault.Attach && ReferenceEquals(context.Data, b)) node.ThrowOnAttach = true;
            return node;
        }));
        SetSource(fixture.Items, source);
        fixture.Attach();
        Assert.Equal(1, source.SubscriberCount);
        Assert.NotNull(oldNode);

        if (fault == StructuralFault.Detach) oldNode.ThrowOnDetach = true;
        if (fault == StructuralFault.Changed)
        {
            bool first = true;
            fixture.Items.LogicalChildren.Changed += (_, _) =>
            {
                if (first)
                {
                    first = false;
                    throw new InvalidOperationException("changed callback failed");
                }
            };
        }

        Exception? failure = Record.Exception(() =>
        {
            if (fault == StructuralFault.Detach) source.RemoveAt(0);
            else source.Add(b);
        });
        Assert.NotNull(failure);
        Assert.Contains("failed", failure.ToString(), StringComparison.OrdinalIgnoreCase);
        Assert.Equal(0, source.SubscriberCount);
        Assert.Equal(fixture.Items.LogicalChildren.Count, fixture.Items.RealizedItemCount);
        Assert.NotNull(Record.Exception(fixture.Items.Refresh));
        Assert.Equal(fixture.Items.LogicalChildren.Count, fixture.Items.RealizedItemCount);

        int committedMembership = fixture.Items.LogicalChildren.Count;
        source.Add(new Model("after fault"));
        Assert.Equal(committedMembership, fixture.Items.LogicalChildren.Count);
        Assert.Equal(committedMembership, fixture.Items.RealizedItemCount);
        Assert.NotNull(Record.Exception(() => SetSource(fixture.Items, new[] { new Model("rebind") })));
        Assert.Equal(committedMembership, fixture.Items.LogicalChildren.Count);
    }

    [Fact]
    public void ReentrantSourceChangeDuringStructuralCallbackCannotPublishLateGenerationAfterFault()
    {
        Model a = new("a");
        Model b = new("b");
        Model late = new("late");
        ObservableCollection<Model> source = new() { a };
        using CollectionFixture fixture = new();
        fixture.Items.Templates.Add(ModelTemplate());
        SetSource(fixture.Items, source);
        fixture.Attach();
        bool first = true;
        fixture.Items.LogicalChildren.Changed += (_, _) =>
        {
            if (!first) return;
            first = false;
            SetSource(fixture.Items, new[] { late });
            throw new InvalidOperationException("changed callback failed");
        };

        Exception? fault = Record.Exception(() => source.Add(b));
        Assert.NotNull(fault);
        Assert.Equal(fixture.Items.LogicalChildren.Count, fixture.Items.RealizedItemCount);
        Assert.DoesNotContain(fixture.Nodes, node => ReferenceEquals(node.Model, late));
        Record.Exception(fixture.Tick);
        Assert.DoesNotContain(fixture.Nodes, node => ReferenceEquals(node.Model, late));
        Assert.NotNull(Record.Exception(fixture.Items.Refresh));
    }

    [Fact]
    public void StructuralFaultCannotPublishReadyCollisionPreparation()
    {
        Model a = new("a");
        Model b = new("b");
        ObservableCollection<Model> source = new() { a };
        using CollectionFixture fixture = new();
        fixture.Items.Templates.Add(new ContentTemplate<Model>("fault", null, 0, context =>
            new FaultingNode(context.Data, context.Index)
            {
                ThrowOnAttach = ReferenceEquals(context.Data, b)
            }));
        SetSource(fixture.Items, source);
        fixture.Attach();
        Assert.Throws<InvalidOperationException>(() => source.Add(b));

        Task<SceneCollisionRegion2D> preparing = fixture.Scene.CollisionWorld
            .PrepareRegionAsync(new DrawRect(0, 0, 10, 10)).AsTask();
        Assert.True(SpinWait.SpinUntil(() =>
        {
            Exception? pumpError = Record.Exception(fixture.Tick);
            if (pumpError is not null)
                Assert.Contains("attach callback failed", pumpError.ToString(), StringComparison.Ordinal);
            return preparing.IsCompleted;
        }, TimeSpan.FromSeconds(5)), "Collision preparation did not finish after terminal structural fault.");
        Assert.True(preparing.IsFaulted);
        AggregateException failure = Assert.IsType<AggregateException>(preparing.Exception);
        Assert.Contains("attach callback failed", failure.ToString(), StringComparison.Ordinal);
    }

    [Fact]
    public async Task TemplateProducedSceneNodeReceivesRealClickAndKeyInput()
    {
        Model model = new("clickable");
        Scene2D world = new();
        SceneItems2D items = new();
        int clicks = 0;
        int keys = 0;
        items.Templates.Add(new ContentTemplate<Model>("input", null, 0, _ =>
        {
            Sprite2D sprite = new() { Width = 16, Height = 12, Focusable = true };
            ServoApi.SetId(sprite, "collection-item");
            sprite.MouseDown += (_, _) => clicks++;
            sprite.KeyDown += (_, _) => keys++;
            return sprite;
        }));
        SetSource(items, new[] { model });
        world.Children.Add(items);

        UIRoot root = new(200, 200);
        root.VisualChildren.Add(new RenderSurface2D
        {
            Width = 200, Height = 200, Scene = world,
            ViewBox = new DrawRect(0, 0, 100, 100), Stretch = DrawBrushStretch.Fill
        });
        UiHost host = new(new UiHostOptions { Root = root, Viewport = new UiViewport(200, 200) });
        host.Update(new InputFrame(PointerSnapshot.Empty, PointerSnapshot.Empty,
            KeyboardSnapshot.Empty, KeyboardSnapshot.Empty, []), host.Viewport, TimeSpan.Zero);
        ServoApi servo = new(host);
        ServoTarget target = ServoTarget.ById("collection-item");

        await servo.ClickAsync(target);
        await servo.PressKeyAsync(InputKey.Enter);
        Assert.Equal(1, clicks);
        Assert.Equal(1, keys);
    }

    private static ContentTemplate<Model> ModelTemplate(string name = "model") =>
        new(name, null, 0, context => new TrackingNode(context.Data, context.Index));

    private static PropertyInfo GetEnumerableItemsSourceProperty()
    {
        PropertyInfo? property = typeof(SceneItems2D).GetProperty(nameof(SceneItems2D.ItemsSource));
        Assert.NotNull(property);
        Assert.Equal(typeof(IEnumerable), property.PropertyType);
        return property;
    }

    private static void SetSource(SceneItems2D items, IEnumerable? source)
    {
        PropertyInfo property = GetEnumerableItemsSourceProperty();
        try { property.SetValue(items, source); }
        catch (TargetInvocationException error) when (error.InnerException is not null)
        {
            ExceptionDispatchInfo.Capture(error.InnerException).Throw();
            throw;
        }
    }

    private static Exception? RunOnWorker(Action action)
    {
        Task worker = Task.Run(action);
        Assert.True(SpinWait.SpinUntil(() => worker.IsCompleted, TimeSpan.FromSeconds(5)),
            "Worker action did not complete.");
        Assert.False(worker.IsCanceled);
        return worker.Exception?.GetBaseException();
    }

    private sealed record Model(string Id);

    private sealed class CountingEnumerable<T>(IEnumerable<T> values) : IEnumerable<T>
    {
        internal List<T> Values { get; } = [.. values];
        internal int EnumerationCount { get; private set; }
        internal bool ThrowOnEnumeration { get; set; }
        public IEnumerator<T> GetEnumerator()
        {
            EnumerationCount++;
            if (ThrowOnEnumeration) throw new InvalidOperationException("source enumeration failed");
            return Values.GetEnumerator();
        }
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }

    private sealed class OneShotEnumerable<T>(IEnumerable<T> values) : IEnumerable<T>
    {
        private readonly List<T> snapshot = [.. values];
        internal int EnumerationCount { get; private set; }
        public IEnumerator<T> GetEnumerator()
        {
            EnumerationCount++;
            if (EnumerationCount > 1) throw new InvalidOperationException("One-shot source was enumerated again.");
            return snapshot.GetEnumerator();
        }
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }

    private sealed class EventSource<T>(IEnumerable<T> values) : IEnumerable<T>, INotifyCollectionChanged
    {
        private NotifyCollectionChangedEventHandler? changed;
        internal List<T> Values { get; } = [.. values];
        internal int EnumerationCount { get; private set; }
        internal int SubscriberCount { get; private set; }
        public event NotifyCollectionChangedEventHandler? CollectionChanged
        {
            add { changed += value; SubscriberCount++; }
            remove { changed -= value; SubscriberCount--; }
        }
        public IEnumerator<T> GetEnumerator()
        {
            EnumerationCount++;
            return Values.GetEnumerator();
        }
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        internal void Add(T item)
        {
            int index = Values.Count;
            Values.Add(item);
            changed?.Invoke(this, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add, item, index));
        }
        internal void AddRange(IReadOnlyList<T> items)
        {
            int index = Values.Count;
            Values.AddRange(items);
            changed?.Invoke(this, new NotifyCollectionChangedEventArgs(
                NotifyCollectionChangedAction.Add, (IList)items.ToList(), index));
        }
        internal void AddRangeWithInvalidIndex(IReadOnlyList<T> items)
        {
            Values.AddRange(items);
            changed?.Invoke(this, new NotifyCollectionChangedEventArgs(
                NotifyCollectionChangedAction.Add, (IList)items.ToList(), Values.Count + 10));
        }
        internal void Move(int oldIndex, int newIndex)
        {
            T item = Values[oldIndex];
            Values.RemoveAt(oldIndex);
            Values.Insert(newIndex, item);
            changed?.Invoke(this, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Move, item, newIndex, oldIndex));
        }
        internal void RemoveAt(int index)
        {
            T item = Values[index];
            Values.RemoveAt(index);
            changed?.Invoke(this, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Remove, item, index));
        }
        internal void Replace(int index, T item)
        {
            T previous = Values[index];
            Values[index] = item;
            changed?.Invoke(this, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Replace, item, previous, index));
        }
        internal void Reset() => changed?.Invoke(this, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
    }

    private sealed class CollectionFixture : IDisposable
    {
        internal UIRoot Root { get; } = new(100, 100);
        internal Scene2D Scene { get; } = new();
        internal SceneItems2D Items { get; } = new();
        internal RenderSurface2D Surface { get; } = new() { ViewBox = new(0, 0, 100, 100) };
        internal TrackingNode[] Nodes => Items.LogicalChildren.Cast<TrackingNode>().ToArray();
        private bool attached;

        internal void Attach()
        {
            if (attached) return;
            if (!Scene.Children.Contains(Items)) Scene.Children.Add(Items);
            Surface.Scene = Scene;
            attached = true;
            Root.VisualChildren.Add(Surface);
            Tick();
        }

        internal void Detach()
        {
            if (!attached) return;
            Root.VisualChildren.Remove(Surface);
            attached = false;
        }

        internal void Tick()
        {
            ((ITimeSensitiveRenderElement)Surface).UpdateRenderTime(TimeSpan.FromMilliseconds(16));
            Root.ProcessFrame();
        }

        public void Dispose() => Detach();
    }

    private class TrackingNode(object? model, int templateIndex) : SceneNode2D
    {
        internal object? Model { get; } = model;
        internal int TemplateIndex { get; } = templateIndex;
        internal int AttachCount { get; private set; }
        internal int DetachCount { get; private set; }
        protected override void OnAttached() { AttachCount++; base.OnAttached(); }
        protected override void OnDetached() { DetachCount++; base.OnDetached(); }
        internal override void Record(Scene2DRecordContext context) =>
            context.Frame.FillRectangle(new(0, 0, 10, 10), Color.Black);
        internal override SceneBounds2D GetVisibleLocalBounds() =>
            SceneBounds2D.Known(new(0, 0, 10, 10));
    }

    public enum StructuralFault { Attach, Detach, Changed }

    private sealed class FaultingNode(object? model, int templateIndex) : TrackingNode(model, templateIndex)
    {
        internal bool ThrowOnAttach { get; set; }
        internal bool ThrowOnDetach { get; set; }
        protected override void OnAttached()
        {
            base.OnAttached();
            if (ThrowOnAttach) { ThrowOnAttach = false; throw new InvalidOperationException("attach callback failed"); }
        }
        protected override void OnDetached()
        {
            base.OnDetached();
            if (ThrowOnDetach) { ThrowOnDetach = false; throw new InvalidOperationException("detach callback failed"); }
        }
    }

    private sealed class CacheReleaseFaultNode(object? model, int templateIndex) : TrackingNode(model, templateIndex)
    {
        internal int CacheReleaseCount { get; private set; }
        internal override void ReleaseRenderCaches()
        {
            CacheReleaseCount++;
            base.ReleaseRenderCaches();
            throw new IOException("candidate cache release failed");
        }
    }
}
