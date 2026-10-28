using Cerneala.UI.Aspect;
using Cerneala.UI.Controls;
using Cerneala.UI.Controls.Templates;
using Cerneala.UI.Core;
using Cerneala.UI.Elements;
using Cerneala.UI.Layout;
using Cerneala.UI.Motion.Presence;
using Cerneala.Tests.UI.Motion.Core;
using MotionFactory = Cerneala.UI.Motion.Specs.Motion;

namespace Cerneala.Tests.Controls;

public sealed class ContentControlTests
{
    [Fact]
    public void UiElementContentBecomesLogicalAndVisualChild()
    {
        ContentControl control = new();
        UIElement child = new();

        control.Content = child;

        Assert.Same(control, child.LogicalParent);
        Assert.Same(control, child.VisualParent);
        Assert.Contains(child, control.LogicalChildren);
        Assert.Contains(child, control.VisualChildren);
    }

    [Fact]
    public void ReplacingContentRemovesOldElement()
    {
        ContentControl control = new();
        UIElement oldChild = new();
        UIElement newChild = new();
        control.Content = oldChild;

        control.Content = newChild;

        Assert.Null(oldChild.LogicalParent);
        Assert.Null(oldChild.VisualParent);
        Assert.Same(control, newChild.LogicalParent);
        Assert.Same(control, newChild.VisualParent);
    }

    [Fact]
    public void ReplacingEqualElementContentUpdatesRetainedOwnership()
    {
        ContentControl control = new();
        EqualElement oldChild = new(1);
        EqualElement newChild = new(1);
        control.Content = oldChild;
        int layoutVersion = control.LayoutVersion;
        int renderVersion = control.RenderVersion;

        control.Content = newChild;

        Assert.Same(newChild, control.Content);
        Assert.True(control.LayoutVersion > layoutVersion);
        Assert.True(control.RenderVersion > renderVersion);
        Assert.Null(oldChild.LogicalParent);
        Assert.Null(oldChild.VisualParent);
        Assert.Same(control, newChild.LogicalParent);
        Assert.Same(control, newChild.VisualParent);
    }

    [Fact]
    public void TemplateContentPresenterCanHostExistingElementContent()
    {
        ContentControl control = new();
        UIElement child = new();
        ContentPresenter? presenter = null;
        control.Content = child;

        control.ComponentTemplate = new ComponentTemplate<ContentControl>("test", context =>
        {
            presenter = new ContentPresenter { Content = context.Owner.Content };
            return presenter;
        });

        Assert.Same(child, presenter!.PresentedChild);
        Assert.Same(presenter, child.LogicalParent);
        Assert.Same(presenter, child.VisualParent);
        Assert.DoesNotContain(child, control.LogicalChildren);
        Assert.DoesNotContain(child, control.VisualChildren);
        Assert.Contains(presenter, control.LogicalChildren);
        Assert.Contains(presenter, control.VisualChildren);
    }

    [Fact]
    public void RejectedReparentLeavesExistingContentOwned()
    {
        ContentControl control = new();
        UIElement oldChild = new();
        UIElement newChild = new();
        UIElement visualParent = new();
        control.Content = oldChild;
        visualParent.VisualChildren.Add(newChild);

        Assert.Throws<InvalidOperationException>(() => control.Content = newChild);

        Assert.Same(oldChild, control.Content);
        Assert.Same(control, oldChild.LogicalParent);
        Assert.Same(control, oldChild.VisualParent);
        Assert.Null(newChild.LogicalParent);
        Assert.Same(visualParent, newChild.VisualParent);
        Assert.DoesNotContain(newChild, control.LogicalChildren);
        Assert.DoesNotContain(newChild, control.VisualChildren);
    }

    [Fact]
    public void RejectedReparentDoesNotDetachExistingAttachedContent()
    {
        UIRoot root = new();
        ContentControl control = new();
        LifecycleElement oldChild = new();
        UIElement newChild = new();
        UIElement visualParent = new();
        root.VisualChildren.Add(control);
        control.Content = oldChild;
        visualParent.VisualChildren.Add(newChild);
        int attachedCount = oldChild.AttachedCount;
        int detachedCount = oldChild.DetachedCount;

        Assert.Throws<InvalidOperationException>(() => control.Content = newChild);

        Assert.Same(oldChild, control.Content);
        Assert.Same(control, oldChild.LogicalParent);
        Assert.Same(control, oldChild.VisualParent);
        Assert.Equal(attachedCount, oldChild.AttachedCount);
        Assert.Equal(detachedCount, oldChild.DetachedCount);
    }

    [Fact]
    public void SetValueRejectsTemplatedElementContentThatHasAnotherParentLikeTheContentSetter()
    {
        ContentControl control = new()
        {
            ComponentTemplate = new ComponentTemplate<ContentControl>("test", context =>
                new ContentPresenter { Content = context.Owner.Content })
        };
        UIElement oldChild = new();
        UIElement parented = new();
        UIElement parent = new();
        control.Content = oldChild;
        parent.LogicalChildren.Add(parented);

        Assert.Throws<InvalidOperationException>(() => control.Content = parented);
        InvalidOperationException rejected = Assert.Throws<InvalidOperationException>(
            () => control.SetValue(ContentControl.ContentProperty, (object?)parented));

        Assert.Equal("Element must be removed from its current parent before reparenting.", rejected.Message);
        Assert.Same(oldChild, control.Content);
        Assert.Same(parent, parented.LogicalParent);

        UIElement loose = new();
        control.SetValue(ContentControl.ContentProperty, (object?)loose);
        Assert.Same(loose, control.Content);
    }

    [Fact]
    public void ClearValueRejectsTemplatedLowerSourceElementContentThatGotAnotherParent()
    {
        UIRoot root = new(300, 200);
        ContentControl control = new()
        {
            ComponentTemplate = new ComponentTemplate<ContentControl>("test", context =>
                new ContentPresenter { Content = context.Owner.Content }),
            Content = "text"
        };
        Border holder = new();
        Border revealed = new() { Width = 10, Height = 10 };
        root.VisualChildren.Add(control);
        root.ProcessFrame();

        control.SetValue(ContentControl.ContentProperty, (object?)revealed, UiPropertyValueSource.AspectBase);
        holder.Child = revealed;
        root.VisualChildren.Add(holder);

        InvalidOperationException rejected = Assert.Throws<InvalidOperationException>(
            () => control.ClearValue(ContentControl.ContentProperty));

        Assert.Equal("Element must be removed from its current parent before reparenting.", rejected.Message);
        Assert.Equal("text", control.Content);
        Assert.Equal(UiPropertyValueSource.Local, control.GetValueSource(ContentControl.ContentProperty));
        Assert.Same(holder, revealed.LogicalParent);
        root.ProcessFrame();
        Assert.Same(holder, revealed.VisualParent);
    }

    [Fact]
    public void SetValueStoresShadowedTemplatedElementContentThatHasAnotherParentWithoutChangingContent()
    {
        ContentControl control = new()
        {
            ComponentTemplate = new ComponentTemplate<ContentControl>("test", context =>
                new ContentPresenter { Content = context.Owner.Content }),
            Content = "text"
        };
        UIElement parented = new();
        UIElement parent = new();
        parent.LogicalChildren.Add(parented);
        parent.VisualChildren.Add(parented);

        control.SetValue(ContentControl.ContentProperty, (object?)parented, UiPropertyValueSource.MarkupConditional);

        Assert.Equal("text", control.Content);
        Assert.Equal(UiPropertyValueSource.Local, control.GetValueSource(ContentControl.ContentProperty));
        Assert.Same(parented, control.GetSourceValue(ContentControl.ContentProperty, UiPropertyValueSource.MarkupConditional));
        Assert.Same(parent, parented.LogicalParent);
        Assert.Same(parent, parented.VisualParent);
    }

    [Fact]
    public void SetValueHostsUntemplatedElementContentLikeTheContentSetter()
    {
        ContentControl control = new();
        UIElement first = new();
        UIElement second = new();

        control.SetValue(ContentControl.ContentProperty, (object?)first);

        Assert.Same(control, first.LogicalParent);
        Assert.Same(control, first.VisualParent);

        control.SetValue(ContentControl.ContentProperty, (object?)second);

        Assert.Null(first.LogicalParent);
        Assert.Null(first.VisualParent);
        Assert.Same(control, second.LogicalParent);
        Assert.Same(control, second.VisualParent);

        control.ClearValue(ContentControl.ContentProperty);

        Assert.Null(control.Content);
        Assert.Null(second.LogicalParent);
        Assert.Null(second.VisualParent);
        Assert.Empty(control.LogicalChildren);
        Assert.Empty(control.VisualChildren);
    }

    [Fact]
    public void ClearingAValueSourceHostsTheElementContentOfTheNextSource()
    {
        ContentControl control = new();
        UIElement conditional = new();
        UIElement local = new();
        control.SetValue(ContentControl.ContentProperty, (object?)conditional, UiPropertyValueSource.MarkupConditional);

        control.SetValue(ContentControl.ContentProperty, (object?)local);

        Assert.Same(local, control.Content);
        Assert.Same(control, local.VisualParent);
        Assert.Null(conditional.LogicalParent);
        Assert.Null(conditional.VisualParent);

        control.ClearValue(ContentControl.ContentProperty);

        Assert.Same(conditional, control.Content);
        Assert.Same(control, conditional.LogicalParent);
        Assert.Same(control, conditional.VisualParent);
        Assert.Null(local.LogicalParent);
        Assert.Null(local.VisualParent);
    }

    [Fact]
    public void SetValueRejectsUntemplatedElementContentThatHasAnotherParentLikeTheContentSetter()
    {
        ContentControl control = new();
        UIElement oldChild = new();
        UIElement parented = new();
        UIElement parent = new();
        control.Content = oldChild;
        parent.LogicalChildren.Add(parented);

        Assert.Throws<InvalidOperationException>(() => control.Content = parented);
        InvalidOperationException rejected = Assert.Throws<InvalidOperationException>(
            () => control.SetValue(ContentControl.ContentProperty, (object?)parented));

        Assert.Equal("Element must be removed from its current parent before reparenting.", rejected.Message);
        Assert.Same(oldChild, control.Content);
        Assert.Same(control, oldChild.LogicalParent);
        Assert.Same(control, oldChild.VisualParent);
        Assert.Same(parent, parented.LogicalParent);
        Assert.DoesNotContain(parented, control.LogicalChildren);
    }

    [Fact]
    public void ContentSetterWhoseElementAttachThrowsRestoresThePreviousOwnership()
    {
        UIRoot root = new(200, 200);
        ContentControl control = new();
        root.VisualChildren.Add(control);
        root.ProcessFrame();
        Border oldChild = new();
        control.Content = oldChild;
        ThrowingAttachElement failed = new();

        Assert.Throws<InvalidOperationException>(() => control.Content = failed);

        AssertPreviousOwnershipRestored(control, oldChild, failed);
    }

    [Theory]
    [InlineData(UiPropertyValueSource.Local)]
    [InlineData(UiPropertyValueSource.MarkupConditional)]
    public void SetValueWhoseElementAttachThrowsRestoresThePreviousContentAndOwnership(UiPropertyValueSource source)
    {
        UIRoot root = new(200, 200);
        ContentControl control = new();
        root.VisualChildren.Add(control);
        root.ProcessFrame();
        Border oldChild = new();
        control.SetValue(ContentControl.ContentProperty, (object?)oldChild, source);
        ThrowingAttachElement failed = new();

        Assert.Throws<InvalidOperationException>(
            () => control.SetValue(ContentControl.ContentProperty, (object?)failed, source));

        AssertPreviousOwnershipRestored(control, oldChild, failed);
        Assert.Equal(source, control.GetValueSource(ContentControl.ContentProperty));
    }

    [Fact]
    public void FrameworkDefaultWhoseElementAttachThrowsRestoresThePreviousContentAndOwnership()
    {
        UIRoot root = new(200, 200);
        ContentControl control = new();
        root.VisualChildren.Add(control);
        root.ProcessFrame();
        Border oldChild = new();
        control.SetFrameworkDefault(ContentControl.ContentProperty, (object?)oldChild);
        ThrowingAttachElement failed = new();

        Assert.Throws<InvalidOperationException>(
            () => control.SetFrameworkDefault(ContentControl.ContentProperty, (object?)failed));

        AssertPreviousOwnershipRestored(control, oldChild, failed);
        Assert.Equal(UiPropertyValueSource.Default, control.GetValueSource(ContentControl.ContentProperty));
    }

    [Fact]
    public void ClearValueWhoseRevealedElementAttachThrowsRestoresThePreviousContentAndOwnership()
    {
        UIRoot root = new(200, 200);
        ContentControl control = new();
        root.VisualChildren.Add(control);
        root.ProcessFrame();
        Border oldChild = new();
        control.Content = oldChild;
        ThrowingAttachElement failed = new();
        control.SetValue(ContentControl.ContentProperty, (object?)failed, UiPropertyValueSource.MarkupConditional);

        Assert.Throws<InvalidOperationException>(() => control.ClearValue(ContentControl.ContentProperty));

        AssertPreviousOwnershipRestored(control, oldChild, failed);
        Assert.Equal(UiPropertyValueSource.Local, control.GetValueSource(ContentControl.ContentProperty));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void ReplacementWhoseElementLoadedHandlerThrowsReleasesTheElementForReuse(bool useSetValue)
    {
        UIRoot root = new(300, 200);
        ContentControl control = new();
        root.VisualChildren.Add(control);
        root.ProcessFrame();
        Button oldChild = new() { Content = "old" };
        control.Content = oldChild;
        Button failed = new() { Content = "next" };
        failed.Loaded += (_, _) => throw new InvalidOperationException("Loaded handler failed");

        Assert.Throws<InvalidOperationException>(() =>
        {
            if (useSetValue)
            {
                control.SetValue(ContentControl.ContentProperty, (object?)failed);
            }
            else
            {
                control.Content = failed;
            }
        });

        AssertPreviousOwnershipRestored(control, oldChild, failed);
        Assert.True(oldChild.IsLoaded);
        Assert.False(failed.IsLoaded);
        Border reuse = new();
        reuse.Child = failed;
        Assert.Same(reuse, failed.LogicalParent);
    }

    [Fact]
    public void FailedRestoreOfThePreviousElementRethrowsTheFirstFailureWithoutRestoringAgain()
    {
        UIRoot root = new(200, 200);
        ContentControl control = new();
        root.VisualChildren.Add(control);
        root.ProcessFrame();
        ThrowingAttachElement oldChild = new() { Throws = false };
        control.Content = oldChild;
        oldChild.Throws = true;
        ThrowingAttachElement failed = new();

        InvalidOperationException failure = Assert.Throws<InvalidOperationException>(() => control.Content = failed);

        Assert.Equal("attach failed", failure.Message);
        Assert.Same(oldChild, control.Content);
        Assert.Empty(control.LogicalChildren);
        Assert.Empty(control.VisualChildren);
        Assert.Null(failed.LogicalParent);
        Assert.Null(failed.VisualParent);
        Assert.Null(oldChild.LogicalParent);
        Assert.Null(oldChild.VisualParent);
    }

    // Issue #616: removing the visual child first and the logical child second canceled the exit at once.
    [Fact]
    public void ContentSetToNullKeepsTheExitOfAnElementWithPresenceRunningUntilItEnds()
    {
        ManualMotionClock clock = new();
        UIRoot root = new(200, 200, motionClock: clock);
        ContentControl control = new();
        root.VisualChildren.Add(control);
        Border child = new() { Width = 50, Height = 20, Presence = CreatePresence() };
        control.Content = child;
        root.ProcessFrame();

        control.Content = null;
        root.ProcessFrame();

        Assert.Null(control.Content);
        Assert.Equal(PresenceState.Exiting, root.Motion.Presence.GetState(child));
        Assert.Equal([child], root.Motion.Presence.GetExitingVisualChildren(control));
        Assert.True(child.IsAttached);
        Assert.Null(child.LogicalParent);
        Assert.Empty(control.VisualChildren);
        Assert.Empty(control.LogicalChildren);

        clock.Advance(TimeSpan.FromMilliseconds(120));
        root.ProcessFrame();

        Assert.Equal(PresenceState.Detached, root.Motion.Presence.GetState(child));
        Assert.Empty(root.Motion.Presence.GetExitingVisualChildren(control));
        Assert.False(child.IsAttached);
    }

    // Issue #616: replaced element content with presence exits while the new element becomes present.
    [Fact]
    public void ContentReplacedKeepsTheExitOfTheOldElementWithPresenceRunningAndTheNewElementIsPresent()
    {
        ManualMotionClock clock = new();
        UIRoot root = new(200, 200, motionClock: clock);
        ContentControl control = new();
        root.VisualChildren.Add(control);
        Border oldChild = new() { Width = 50, Height = 20, Presence = CreatePresence() };
        Border newChild = new() { Width = 50, Height = 20, Presence = CreatePresence() };
        control.Content = oldChild;
        root.ProcessFrame();

        control.Content = newChild;
        root.ProcessFrame();

        Assert.Same(newChild, control.Content);
        Assert.Equal(PresenceState.Exiting, root.Motion.Presence.GetState(oldChild));
        Assert.Equal([oldChild], root.Motion.Presence.GetExitingVisualChildren(control));
        Assert.True(oldChild.IsAttached);
        Assert.Equal(PresenceState.Present, root.Motion.Presence.GetState(newChild));
        Assert.Equal([newChild], control.VisualChildren);
        Assert.Equal([newChild], control.LogicalChildren);

        clock.Advance(TimeSpan.FromMilliseconds(120));
        root.ProcessFrame();

        Assert.Equal(PresenceState.Detached, root.Motion.Presence.GetState(oldChild));
        Assert.Empty(root.Motion.Presence.GetExitingVisualChildren(control));
        Assert.False(oldChild.IsAttached);
        Assert.Equal(PresenceState.Present, root.Motion.Presence.GetState(newChild));
    }

    // Issue #616: setting the exiting element as Content again cancels its exit and hosts it in both trees again.
    [Fact]
    public void ContentSetAgainWhileItsElementExitsCancelsTheExit()
    {
        ManualMotionClock clock = new();
        UIRoot root = new(200, 200, motionClock: clock);
        ContentControl control = new();
        root.VisualChildren.Add(control);
        Border child = new() { Width = 50, Height = 20, Presence = CreatePresence() };
        control.Content = child;
        root.ProcessFrame();
        control.Content = null;
        root.ProcessFrame();

        control.Content = child;
        root.ProcessFrame();

        Assert.Same(child, control.Content);
        Assert.Same(control, child.LogicalParent);
        Assert.Same(control, child.VisualParent);
        Assert.Equal(PresenceState.Present, root.Motion.Presence.GetState(child));
        Assert.Empty(root.Motion.Presence.GetExitingVisualChildren(control));
        Assert.True(child.IsAttached);
    }

    // Issue #616: a failed replacement restores the previous element without an exit and removes the rejected one at once.
    [Fact]
    public void FailedReplacementRestoresThePreviousElementWithPresenceAndRemovesTheRejectedElementAtOnce()
    {
        ManualMotionClock clock = new();
        UIRoot root = new(200, 200, motionClock: clock);
        ContentControl control = new();
        root.VisualChildren.Add(control);
        Border oldChild = new() { Width = 50, Height = 20, Presence = CreatePresence() };
        control.Content = oldChild;
        root.ProcessFrame();
        Button failed = new() { Content = "next", Presence = CreatePresence() };
        failed.Loaded += (_, _) => throw new InvalidOperationException("Loaded handler failed");

        Assert.Throws<InvalidOperationException>(() => control.Content = failed);
        root.ProcessFrame();

        AssertPreviousOwnershipRestored(control, oldChild, failed);
        Assert.Equal(PresenceState.Present, root.Motion.Presence.GetState(oldChild));
        Assert.Equal(PresenceState.Detached, root.Motion.Presence.GetState(failed));
        Assert.Empty(root.Motion.Presence.GetExitingVisualChildren(control));
        Assert.True(oldChild.IsAttached);
        Assert.False(failed.IsAttached);
    }

    // Issue #653: content that a new owner takes moves without an exit, so a template that wraps it can add it.
    [Fact]
    public void ContentReleasedForANewOwnerLeavesWithoutAnExitAndTheNewTemplateHostsIt()
    {
        ManualMotionClock clock = new();
        UIRoot root = new(200, 200, motionClock: clock);
        Border child = new() { Width = 50, Height = 20, Presence = CreatePresence() };
        ContentControl? oldControl = null;
        ContentPresenter presenter = new()
        {
            Content = child,
            ContentTemplate = new ContentTemplate<UIElement>(
                "cc",
                key: null,
                priority: 0,
                context => oldControl = new ContentControl { Content = context.Data })
        };
        root.VisualChildren.Add(presenter);
        root.ProcessFrame();
        Assert.Same(oldControl, child.VisualParent);

        presenter.ContentTemplate = new ContentTemplate<UIElement>(
            "wrap",
            key: null,
            priority: 0,
            context => new Border { Child = context.Data });
        root.ProcessFrame();

        Border wrapper = Assert.IsType<Border>(child.VisualParent);
        Assert.Same(child, wrapper.Child);
        Assert.Same(presenter, wrapper.VisualParent);
        Assert.Null(oldControl!.Content);
        Assert.Empty(oldControl.VisualChildren);
        Assert.Empty(oldControl.LogicalChildren);
        Assert.Equal(PresenceState.Present, root.Motion.Presence.GetState(child));
        Assert.Empty(root.Motion.Presence.GetExitingVisualChildren(oldControl));
    }

    // The release for a new owner raises Unloaded; content that a handler sets on the releasing control then stays, as
    // in ContentPresenter.ReleaseContentForNewOwner.
    [Fact]
    public void ContentSetByUnloadedWhileANewOwnerTakesTheHostedElementStays()
    {
        UIRoot root = new(200, 200);
        Border child = new() { Width = 50, Height = 20 };
        Border replacement = new() { Width = 10, Height = 10 };
        ContentControl? oldControl = null;
        ContentPresenter presenter = new()
        {
            Content = child,
            ContentTemplate = new ContentTemplate<UIElement>(
                "cc",
                key: null,
                priority: 0,
                context => oldControl = new ContentControl { Content = context.Data })
        };
        root.VisualChildren.Add(presenter);
        root.ProcessFrame();
        ContentControl first = oldControl!;
        bool changed = false;
        child.Unloaded += (_, _) =>
        {
            if (!changed)
            {
                changed = true;
                first.Content = replacement;
            }
        };

        presenter.ContentTemplate = new ContentTemplate<UIElement>(
            "wrap",
            key: null,
            priority: 0,
            context => new Border { Child = context.Data });
        root.ProcessFrame();

        Assert.True(changed);
        Assert.Same(replacement, first.Content);
        Assert.Same(first, replacement.VisualParent);
        Assert.Same(first, replacement.LogicalParent);
        Border wrapper = Assert.IsType<Border>(child.VisualParent);
        Assert.Same(child, wrapper.Child);
    }

    // Issue #981: removing the old element raises its Unloaded at once, and a Content change in that handler hosts the
    // new effective Content. The outer replacement then does not host its superseded value as a second child.
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void ReplacingContentFromTheOldElementsUnloadedHandlerHostsOnlyTheEffectiveContent(bool handlerSetsOtherContent)
    {
        UIRoot root = new(200, 200);
        ContentControl host = new();
        Border old = new() { Width = 10, Height = 10 };
        Border other = new() { Width = 10, Height = 10 };
        Border value = new() { Width = 10, Height = 10 };
        root.VisualChildren.Add(host);
        root.ProcessFrame();
        host.Content = old;
        root.ProcessFrame();
        if (handlerSetsOtherContent)
        {
            bool done = false;
            old.Unloaded += (_, _) =>
            {
                if (!done)
                {
                    done = true;
                    host.Content = other;
                }
            };
        }

        host.Content = value;

        string Name(object? element) => ReferenceEquals(element, old) ? "old" : ReferenceEquals(element, other) ? "other" : ReferenceEquals(element, value) ? "value" : "null";
        string detail = $"Content={Name(host.Content)}, visual children=[{string.Join(", ", host.VisualChildren.Select(Name))}], logical children=[{string.Join(", ", host.LogicalChildren.Select(Name))}]";
        Assert.True(
            host.VisualChildren.Count == 1 && ReferenceEquals(host.VisualChildren[0], host.Content) &&
            host.LogicalChildren.Count == 1 && ReferenceEquals(host.LogicalChildren[0], host.Content),
            $"handler sets other: {handlerSetsOtherContent}: {detail}.");
        foreach (Border element in new[] { old, other, value })
        {
            Assert.True(
                ReferenceEquals(element, host.Content) || (element.VisualParent is null && element.LogicalParent is null),
                $"{Name(element)} is still hosted although it is not the Content ({detail}).");
        }
    }

    // Issue #981: adding the new element raises its Loaded at once, and a Content change in that handler hosts the new
    // effective Content. The outer replacement then does not finish hosting its superseded value.
    [Fact]
    public void ReplacingContentFromTheNewElementsLoadedHandlerHostsOnlyTheEffectiveContent()
    {
        UIRoot root = new(200, 200);
        ContentControl host = new();
        Border old = new() { Width = 10, Height = 10 };
        Border other = new() { Width = 10, Height = 10 };
        Border value = new() { Width = 10, Height = 10 };
        root.VisualChildren.Add(host);
        root.ProcessFrame();
        host.Content = old;
        root.ProcessFrame();
        bool done = false;
        value.Loaded += (_, _) =>
        {
            if (!done)
            {
                done = true;
                host.Content = other;
            }
        };

        host.Content = value;

        string Name(object? element) => ReferenceEquals(element, old) ? "old" : ReferenceEquals(element, other) ? "other" : ReferenceEquals(element, value) ? "value" : "null";
        string detail = $"Content={Name(host.Content)}, visual children=[{string.Join(", ", host.VisualChildren.Select(Name))}], logical children=[{string.Join(", ", host.LogicalChildren.Select(Name))}]";
        Assert.True(done);
        Assert.True(
            ReferenceEquals(other, host.Content) &&
            host.VisualChildren.Count == 1 && ReferenceEquals(host.VisualChildren[0], other) &&
            host.LogicalChildren.Count == 1 && ReferenceEquals(host.LogicalChildren[0], other),
            detail);
        Assert.Null(value.VisualParent);
        Assert.Null(value.LogicalParent);
        Assert.False(value.IsLoaded);
        Assert.Null(old.VisualParent);
        Assert.Null(old.LogicalParent);
    }

    // Issue #1191: removing the old element raises its Unloaded at once, and a handler that throws fails the write. The
    // previous content ownership is then restored whatever wrote the value, as for a new element whose attach throws.
    [Theory]
    [InlineData("Setter")]
    [InlineData("SetValue")]
    [InlineData("ClearValue")]
    public void ReplacementWhoseOldElementUnloadedHandlerThrowsRestoresThePreviousOwnership(string write)
    {
        UIRoot root = new(200, 200);
        ContentControl host = new();
        Border old = new();
        Border value = new();
        root.VisualChildren.Add(host);
        host.Content = old;
        if (write == "ClearValue")
        {
            host.SetValue(ContentControl.ContentProperty, (object?)value, UiPropertyValueSource.MarkupConditional);
        }

        root.ProcessFrame();
        bool armed = true;
        old.Unloaded += (_, _) =>
        {
            if (armed)
            {
                armed = false;
                throw new InvalidOperationException("unloaded failed");
            }
        };

        InvalidOperationException failure = Assert.Throws<InvalidOperationException>(() =>
        {
            switch (write)
            {
                case "Setter":
                    host.Content = value;
                    break;
                case "SetValue":
                    host.SetValue(ContentControl.ContentProperty, (object?)value);
                    break;
                default:
                    host.ClearValue(ContentControl.ContentProperty);
                    break;
            }
        });

        Assert.Equal("unloaded failed", failure.Message);
        AssertPreviousOwnershipRestored(host, old, value);
        Assert.Equal(UiPropertyValueSource.Local, host.GetValueSource(ContentControl.ContentProperty));
    }

    // Issues #1191 and #1157 together: a replacement by text content whose old element's Unloaded throws restores the
    // old element, and the presenter that the text content would get is not left beside it.
    [Fact]
    public void ReplacementByTextWhoseOldElementUnloadedHandlerThrowsRestoresTheOldElementWithoutAPresenter()
    {
        UIRoot root = new(200, 200);
        ContentControl host = new();
        Border old = new();
        root.VisualChildren.Add(host);
        host.Content = old;
        root.ProcessFrame();
        bool armed = true;
        old.Unloaded += (_, _) =>
        {
            if (armed)
            {
                armed = false;
                throw new InvalidOperationException("unloaded failed");
            }
        };

        InvalidOperationException failure = Assert.Throws<InvalidOperationException>(() =>
            host.SetValue(ContentControl.ContentProperty, (object?)"text"));
        root.ProcessFrame();

        Assert.Equal("unloaded failed", failure.Message);
        Assert.Same(old, host.Content);
        Assert.Equal([old], host.LogicalChildren);
        Assert.Equal([old], host.VisualChildren);
    }

    // Issues #794 and #1157 together: while a ComponentTemplateKey does not resolve, text content gets no generated
    // presenter, so layout reserves only the insets.
    [Fact]
    public void TextContentOfAnUnresolvedComponentTemplateKeyIsNotPresentedOrMeasured()
    {
        UIRoot root = new(100, 100);
        ContentControl control = new() { Content = "text", ComponentTemplateKey = "missing", Padding = new Thickness(2) };
        root.VisualChildren.Add(control);

        root.ProcessFrame();

        Assert.Empty(control.VisualChildren);
        Assert.Empty(control.LogicalChildren);
        Assert.Equal(new LayoutSize(4, 4), control.DesiredSize);
    }

    private static PresenceOptions CreatePresence()
    {
        return PresenceOptions.FadeAndScale(
            MotionFactory.Tween<float>(TimeSpan.FromMilliseconds(100)),
            MotionFactory.Tween<float>(TimeSpan.FromMilliseconds(100)));
    }

    private static void AssertPreviousOwnershipRestored(ContentControl control, UIElement oldChild, UIElement failed)
    {
        Assert.Same(oldChild, control.Content);
        Assert.Same(control, oldChild.LogicalParent);
        Assert.Same(control, oldChild.VisualParent);
        Assert.Equal([oldChild], control.LogicalChildren);
        Assert.Equal([oldChild], control.VisualChildren);
        Assert.Null(failed.LogicalParent);
        Assert.Null(failed.VisualParent);
    }

    [Fact]
    public void MeasuresAndArrangesContentInsideInsets()
    {
        ContentControl control = new()
        {
            Padding = new Thickness(2),
            // A negative side is the inward border: it takes room from the content (a positive side takes none).
            BorderThickness = new Thickness(-1)
        };
        FixedElement child = new(new LayoutSize(10, 5));
        control.Content = child;

        LayoutSize desired = control.Measure(new MeasureContext(new LayoutSize(100, 100)));
        control.Arrange(new ArrangeContext(new LayoutRect(0, 0, 30, 20)));

        Assert.Equal(new LayoutSize(16, 11), desired);
        Assert.Equal(new LayoutRect(3, 3, 24, 14), child.ArrangedBounds);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void ContentThatTheControlDoesNotHostIsNotMeasured(bool withUnresolvedTemplateKey)
    {
        UIRoot root = new(100, 100);
        FixedElement content = new(new LayoutSize(20, 10));
        ContentControl control = new() { Content = content };
        if (withUnresolvedTemplateKey)
        {
            control.ComponentTemplateKey = "missing";
        }

        root.VisualChildren.Add(control);
        root.ProcessFrame();

        if (withUnresolvedTemplateKey)
        {
            Assert.Null(content.VisualParent);
            Assert.Equal(LayoutSize.Zero, control.DesiredSize);
        }
        else
        {
            Assert.Same(control, content.VisualParent);
            Assert.Equal(new LayoutSize(20, 10), control.DesiredSize);
        }
    }

    // Issue #794: with an unresolved ComponentTemplateKey the control does not host its element Content, so layout
    // neither measures nor arranges it and reserves only the insets. When the key resolves, the template child is measured.
    [Fact]
    public void ContentOfAnUnresolvedComponentTemplateKeyIsNotLaidOutUntilTheTemplateResolves()
    {
        UIRoot root = new(100, 100);
        CountingElement content = new(new LayoutSize(20, 10));
        ContentControl control = new() { Content = content, ComponentTemplateKey = "App.Content", Padding = new Thickness(2) };
        root.VisualChildren.Add(control);
        root.ProcessFrame();

        Assert.Null(content.VisualParent);
        Assert.Equal(new LayoutSize(4, 4), control.DesiredSize);
        Assert.Equal(0, content.MeasureCount);
        Assert.Equal(0, content.ArrangeCount);

        FixedElement? templateChild = null;
        ComponentTemplate<ContentControl> template = new("App.Content", _ => templateChild = new FixedElement(new LayoutSize(30, 15)));
        root.AspectRegistry.Register(AspectPackage.Create("App")
            .Components(components => components.AddTemplate(
                new ComponentTemplateDefinition("App.Content", typeof(ContentControl), template))));
        root.AspectProcessor.Process(control);
        root.ProcessFrame();

        Assert.NotNull(control.ComponentTemplateInstance);
        Assert.NotNull(templateChild);
        Assert.Equal(new LayoutSize(30, 15), templateChild.DesiredSize);
        Assert.Equal(0, content.MeasureCount);
        Assert.Equal(0, content.ArrangeCount);
    }

    private sealed class FixedElement(LayoutSize size) : UIElement
    {
        protected override LayoutSize MeasureCore(MeasureContext context)
        {
            return size;
        }
    }

    private sealed class CountingElement(LayoutSize size) : UIElement
    {
        public int MeasureCount { get; private set; }

        public int ArrangeCount { get; private set; }

        protected override LayoutSize MeasureCore(MeasureContext context)
        {
            MeasureCount++;
            return size;
        }

        protected override LayoutRect ArrangeCore(ArrangeContext context)
        {
            ArrangeCount++;
            return base.ArrangeCore(context);
        }
    }

    private sealed class EqualElement(int id) : UIElement
    {
        private readonly int id = id;

        public override bool Equals(object? obj)
        {
            return obj is EqualElement other && other.id == id;
        }

        public override int GetHashCode()
        {
            return id;
        }
    }

    private sealed class LifecycleElement : UIElement
    {
        public int AttachedCount { get; private set; }

        public int DetachedCount { get; private set; }

        protected override void OnAttached()
        {
            AttachedCount++;
        }

        protected override void OnDetached()
        {
            DetachedCount++;
        }
    }

    private sealed class ThrowingAttachElement : Border
    {
        public bool Throws { get; set; } = true;

        protected override void OnAttached()
        {
            base.OnAttached();
            if (Throws)
            {
                throw new InvalidOperationException("attach failed");
            }
        }
    }
}
