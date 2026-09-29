using Cerneala.Drawing;
using Cerneala.UI.Controls;
using SceneGraph2D = Cerneala.UI.Controls.Scene2D;

namespace Cerneala.Tests.Controls;

public sealed class SceneChildBoundsIndexTests
{
    [Fact]
    public void RepeatedCellQueriesKeepDynamicAttentionAndEmptyUnknownSemantics()
    {
        var (scene, index) = CreateRootedIndex();
        SceneBounds2D view = SceneBounds2D.Known(new(0, 0, 10, 10));
        Assert.Equal([0], Collect(index, view, false));
        Assert.Equal([0], Collect(index, view, false));
        Assert.Empty(Collect(index, SceneBounds2D.Empty, false));
        Assert.Equal([0, 1], Collect(index, SceneBounds2D.Unknown, false));
        Assert.Equal([0], Collect(index, view, false));

        var entries = index.Refresh(scene);
        foreach (int slot in index.Query(SceneBounds2D.Unknown))
        {
            entries[slot].MayHoldResources = slot == 1;
        }
        Assert.Equal([0, 1], Collect(index, view, false));
        entries[1].MayHoldResources = false;
        Collect(index, view, false); // Completes the outstanding attention visit.
        Assert.Equal([0], Collect(index, view, false));
    }

    [Fact]
    public void CellCandidatesRefreshAfterGeometryChangesAndCollectionRebuild()
    {
        var (scene, index) = CreateRootedIndex();
        SceneBounds2D view = SceneBounds2D.Known(new(0, 0, 10, 10));
        Assert.Equal([0], Collect(index, view, false));
        Sprite2D distant = (Sprite2D)scene.Children[1];
        distant.X = 0;
        index.MarkDirty(distant);
        RetireAttention(scene, index);
        Assert.Equal([0, 1], Collect(index, view, false));
        distant.X = 10000;
        index.MarkDirty(distant);
        RetireAttention(scene, index);
        Assert.Equal([0], Collect(index, view, false));
        scene.Children.Clear();
        scene.Children.Add(new Sprite2D { X = 10000, Width = 10, Height = 10 });
        index.Invalidate();
        RetireAttention(scene, index);
        Assert.Empty(Collect(index, view, false));
    }

    [Fact]
    public void InputColliderCandidatesDoNotLeakIntoLaterViewportQueries()
    {
        var (scene, index) = CreateRootedIndex();
        SceneBounds2D view = SceneBounds2D.Known(new(0, 0, 10, 10));
        Assert.Equal([0], Collect(index, view, false));
        index.CollectInputCandidates(scene, new(0, 0), new HashSet<SceneNode2D> { scene.Children[1] });
        Assert.Equal([0], Collect(index, view, false));
        Assert.Equal([1], Collect(index, SceneBounds2D.Known(new(10000, 0, 10, 10)), false));
        Assert.Equal([0], Collect(index, view, false));
    }

    private static (SceneGraph2D Scene, SceneChildBoundsIndex2D Index) CreateRootedIndex()
    {
        SceneGraph2D scene = CreateScene(2);
        ((Sprite2D)scene.Children[1]).X = 10000;
        Cerneala.UI.Elements.UIRoot root = new(100, 100);
        root.VisualChildren.Add(new RenderSurface2D { Scene = scene });
        SceneChildBoundsIndex2D index = new();
        RetireAttention(scene, index);
        return (scene, index);
    }

    private static void RetireAttention(SceneGraph2D scene, SceneChildBoundsIndex2D index)
    {
        var entries = index.Refresh(scene);
        foreach (int slot in index.Query(SceneBounds2D.Unknown))
        {
            entries[slot].MayHoldResources = false;
        }
    }

    [Theory]
    [InlineData(1)]
    [InlineData(65)]
    [InlineData(130)]
    public void RetiredOffscreenAnimationsAreOptionalButRemainAvailableForReactivation(int count)
    {
        SceneGraph2D scene = CreateScene(count);
        SceneChildBoundsIndex2D index = new(parksAnimations: true);
        var entries = index.Refresh(scene);
        foreach (int slot in index.Query(SceneBounds2D.Empty))
        {
            entries[slot].MayHoldResources = false;
            entries[slot].ParkEvaluated = true;
            entries[slot].AnimationParkHint = true;
        }

        Assert.Empty(Collect(index, SceneBounds2D.Empty, includeParked: false));
        Assert.Equal(Enumerable.Range(0, count), Collect(index, SceneBounds2D.Empty, includeParked: true));
        // Revisiting for an effect must not consume the parked obligation.
        Assert.Equal(count, Collect(index, SceneBounds2D.Empty, includeParked: true).Count);
        Assert.Empty(Collect(index, SceneBounds2D.Empty, includeParked: false));
        Assert.Equal(count, Collect(index, SceneBounds2D.Unknown, includeParked: false).Count);
        Assert.Contains(0, Collect(index, SceneBounds2D.Known(new DrawRect(0, 0, 10, 10)), false));
    }

    [Fact]
    public void ResourceAndPendingParkingObligationsSurviveExcludingParkedAnimations()
    {
        SceneGraph2D scene = CreateScene(3);
        SceneChildBoundsIndex2D index = new(parksAnimations: true);
        var entries = index.Refresh(scene);
        foreach (int slot in index.Query(SceneBounds2D.Empty))
        {
            entries[slot].MayHoldResources = slot == 0;
            entries[slot].ParkEvaluated = slot != 1;
            entries[slot].AnimationParkHint = true;
        }

        Assert.Equal([0, 1], Collect(index, SceneBounds2D.Empty, false));
        foreach (int slot in index.Query(SceneBounds2D.Empty))
        {
            entries[slot].MayHoldResources = false;
            entries[slot].ParkEvaluated = true;
            entries[slot].AnimationParkHint = false;
        }
        Assert.Empty(Collect(index, SceneBounds2D.Empty, true));
    }

    [Fact]
    public void EarlyExitAndRebuildRetainConservativeAttention()
    {
        SceneGraph2D scene = CreateScene(65);
        SceneChildBoundsIndex2D index = new(parksAnimations: true);
        var entries = index.Refresh(scene);
        foreach (int slot in index.Query(SceneBounds2D.Empty))
        {
            entries[slot].MayHoldResources = false;
            entries[slot].ParkEvaluated = true;
            entries[slot].AnimationParkHint = true;
        }
        foreach (int slot in index.Query(SceneBounds2D.Empty))
        {
            entries[slot].MayHoldResources = true;
            break;
        }
        Assert.Equal([0], Collect(index, SceneBounds2D.Empty, false));

        scene.Children.Clear();
        scene.Children.Add(new Sprite2D { Width = 10, Height = 10 });
        index.Invalidate();
        entries = index.Refresh(scene);
        foreach (int slot in index.Query(SceneBounds2D.Empty, includeParkedAnimations: false))
        {
            Assert.Equal(0, slot);
            entries[slot].MayHoldResources = false;
            entries[slot].ParkEvaluated = true;
        }
        Assert.Empty(Collect(index, SceneBounds2D.Empty, true));
    }

    private static SceneGraph2D CreateScene(int count)
    {
        SceneGraph2D scene = new();
        for (int i = 0; i < count; i++)
        {
            scene.Children.Add(new Sprite2D { X = i * 100, Width = 10, Height = 10 });
        }
        return scene;
    }

    private static List<int> Collect(SceneChildBoundsIndex2D index, SceneBounds2D bounds, bool includeParked)
    {
        List<int> result = [];
        foreach (int slot in index.Query(bounds, includeParked)) { result.Add(slot); }
        return result;
    }
}
