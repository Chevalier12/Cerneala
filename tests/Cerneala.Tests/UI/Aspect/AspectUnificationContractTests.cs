using System.Collections.Generic;
using Cerneala.Drawing;
using Cerneala.UI;
using Cerneala.UI.Aspect;
using Cerneala.UI.Controls;
using Cerneala.UI.Controls.Templates;
using Cerneala.UI.Core;
using Cerneala.UI.Elements;
using Cerneala.UI.Media;

namespace Cerneala.Tests.UI.Aspect;

public sealed class AspectUnificationContractTests
{
    [Fact]
    public void MatchingRuleEvaluatesPredicateExactlyOnce()
    {
        int calls = 0;
        AspectRuleSet rule = Rule(
            "single-evaluation",
            typeof(Button),
            AspectCondition.Predicate("count", _ =>
            {
                calls++;
                return true;
            }));

        new AspectEngine().Resolve(new Button(), Catalog("Single", rule), new AspectEnvironment("test"));

        Assert.Equal(1, calls);
    }

    [Fact]
    public void NonMatchingTargetDoesNotEvaluatePredicateOrCaptureItsDependency()
    {
        int calls = 0;
        AspectRuleSet rule = Rule(
            "wrong-type",
            typeof(TextBlock),
            AspectCondition.Predicate("must-not-run", _ =>
            {
                calls++;
                return true;
            }));

        ResolvedAspect resolved = new AspectEngine().Resolve(
            new Button(),
            Catalog("WrongType", rule),
            new AspectEnvironment("test"));

        Assert.Equal(0, calls);
        Assert.Empty(resolved.Dependencies.Data);
        Assert.Empty(resolved.Dependencies.Properties);
        Assert.Empty(resolved.Dependencies.States);
        Assert.Empty(resolved.Dependencies.Variants);
    }

    [Fact]
    public void TemplateSlotRejectsElementOutsideDeclaredTargetType()
    {
        TemplateSlotMap slots = new();
        AspectSlot<Button, TextBlock> slot = AspectSlot.For<Button, TextBlock>("Content");

        Assert.Throws<ArgumentException>(() => slots.Register(slot, new Border()));
    }

    [Fact]
    public void SlotFactoriesAndMatchingEnforceOwnerAndTargetTypes()
    {
        Assert.Throws<ArgumentException>(() => AspectSlot.For<string, TextBlock>("invalid-owner"));
        Assert.Throws<ArgumentException>(() => AspectSlot.For<Button, string>("invalid-target"));

        AspectSlot<Button, TextBlock> slot = AspectSlot.For<Button, TextBlock>("Content");
        AspectTarget target = new(typeof(TextBlock), slot);
        AspectMatchContext wrongOwner = new(
            new TextBlock(),
            ownerComponent: new Border(),
            slotPath: new AspectSlotPath(slot));
        ComponentTemplateContext context = new(
            new Border(),
            new AspectEnvironment("wrong-owner"));

        Assert.False(target.Matches(wrongOwner));
        Assert.Throws<ArgumentException>(() => context.RegisterSlot(slot, new TextBlock()));
    }

    [Fact]
    public void ControlRejectsVariantKeyOwnedByDifferentControlType()
    {
        Border border = new();
        AspectVariantKey<Button, string> key = AspectVariantKey.For<Button, string>("kind");

        Assert.Throws<ArgumentException>(() => border.SetAspectVariant(key, "primary"));
        Assert.Throws<ArgumentException>(() => AspectVariantKey.For<string, string>("invalid-owner"));
    }

    [Fact]
    public void RegistryPackageSnapshotCannotBeMutatedThroughPublicSurface()
    {
        AspectRegistry registry = new();
        registry.Register(AspectPackage.Create("First"));
        int version = registry.Version;
        AspectCatalog catalog = registry.BuildCatalog();
        IList<AspectPackage> packages = Assert.IsAssignableFrom<IList<AspectPackage>>(registry.Packages);

        Assert.Throws<NotSupportedException>(() => packages.Add(AspectPackage.Create("Injected")));
        Assert.Equal(version, registry.Version);
        Assert.Same(catalog, registry.BuildCatalog());
        Assert.Single(registry.Packages);
    }

    [Fact]
    public void PackageAndCatalogCollectionsAreImmutableSnapshots()
    {
        AspectRuleSet original = Rule("original", typeof(Button));
        AspectPackage package = AspectPackage.Create("Immutable")
            .Components(components => components.AddRule(original));
        IList<AspectRuleSet> packageRules = Assert.IsAssignableFrom<IList<AspectRuleSet>>(package.Rules);
        AspectRuleSet replacement = Rule("replacement", typeof(Button));
        AspectBehavior behavior = new(typeof(Button), _ => null);

        Assert.Throws<NotSupportedException>(() => packageRules[0] = replacement);

        AspectPackage behaviorPackage = AspectPackage.Create("Behavior")
            .Components(components => components.AddBehavior(behavior));
        IList<AspectBehavior> packageBehaviors = Assert.IsAssignableFrom<IList<AspectBehavior>>(behaviorPackage.Behaviors);
        Assert.Throws<NotSupportedException>(() => packageBehaviors.Add(behavior));

        AspectCatalog catalog = new AspectRegistry().Register(package).BuildCatalog();
        IList<AspectRuleSet> catalogRules = Assert.IsAssignableFrom<IList<AspectRuleSet>>(catalog.Rules);
        IDictionary<AspectToken, AspectValue> tokenDefaults =
            Assert.IsAssignableFrom<IDictionary<AspectToken, AspectValue>>(catalog.TokenDefaults);

        Assert.Throws<NotSupportedException>(() => catalogRules.Add(replacement));
        AspectToken<float> injected = AspectToken.Float("injected");
        Assert.Throws<NotSupportedException>(() =>
            tokenDefaults.Add(injected, AspectValue<float>.Literal(1)));
        Assert.Single(package.Rules);
        Assert.Single(catalog.Rules);
        Assert.Empty(catalog.TokenDefaults);
    }

    [Fact]
    public void PackageBehaviorAttachesOnceAndIsDisposedOnReplacementAndDetach()
    {
        int firstAttach = 0;
        int firstDispose = 0;
        int secondAttach = 0;
        int secondDispose = 0;
        AspectBehavior first = new(typeof(Button), _ =>
        {
            firstAttach++;
            return new CallbackLifetime(() => firstDispose++);
        });
        AspectBehavior second = new(typeof(Button), _ =>
        {
            secondAttach++;
            return new CallbackLifetime(() => secondDispose++);
        });
        Border scope = new();
        scope.Resources["Behavior"] = BehaviorPackage("First", first);
        Button button = new();
        scope.Child = button;
        UIRoot root = new();
        root.VisualChildren.Add(scope);

        root.ProcessFrame();
        root.ProcessFrame();
        Assert.Equal(1, firstAttach);
        Assert.Equal(0, firstDispose);

        scope.Resources["Behavior"] = BehaviorPackage("Second", second);
        root.ProcessFrame();
        Assert.Equal(1, firstDispose);
        Assert.Equal(1, secondAttach);

        scope.Child = null;
        root.ProcessFrame();
        Assert.Equal(1, secondDispose);
    }

    [Fact]
    public void DetachingWithAThrowingPackageBehaviorLifetimeDisposesEveryLifetimeOnceAndClearsAspectState()
    {
        List<string> events = [];
        int[] counts = new int[3];
        SolidColorBrush brush = new(Color.Red);
        AspectPackage package = AspectPackage.Create("A").Components(components =>
        {
            components.AddRule(new AspectRuleSet(
                "A.button",
                AspectLayer.App,
                new AspectTarget(typeof(Button)),
                [new AspectDeclaration(Control.BackgroundProperty, AspectValue<Brush?>.Literal(brush))],
                declarationOrder: 0));
            for (int behaviorIndex = 0; behaviorIndex < 3; behaviorIndex++)
            {
                int i = behaviorIndex;
                components.AddBehavior(new AspectBehavior(typeof(Button), _ =>
                {
                    int n = ++counts[i];
                    events.Add($"create B{i}#{n}");
                    return new RecordingLifetime(() =>
                    {
                        events.Add($"dispose B{i}#{n}");
                        if (i == 2 && n == 1)
                        {
                            throw new InvalidOperationException("B2 dispose failed");
                        }
                    });
                }));
            }
        });
        UIRoot root = new();
        root.AspectRegistry.Register(package);
        Button button = new();
        root.VisualChildren.Add(button);
        root.ProcessFrame();
        Assert.Same(brush, button.Background);

        InvalidOperationException failure = Assert.Throws<InvalidOperationException>(
            () => root.VisualChildren.Remove(button));

        Assert.Equal("B2 dispose failed", failure.Message);
        Assert.Equal(
            ["create B0#1", "create B1#1", "create B2#1", "dispose B2#1", "dispose B1#1", "dispose B0#1"],
            events);
        Assert.NotSame(brush, button.Background);

        root.VisualChildren.Add(button);
        root.ProcessFrame();
        Assert.Equal(["create B0#2", "create B1#2", "create B2#2"], events[6..]);
        Assert.Same(brush, button.Background);

        root.VisualChildren.Remove(button);
        Assert.Equal(["dispose B2#2", "dispose B1#2", "dispose B0#2"], events[9..]);
    }

    [Fact]
    public void ReplacingAPackageBehaviorWhoseLifetimeThrowsKeepsTheNewLifetimeAndDisposesTheOldOnce()
    {
        List<string> events = [];
        int xCount = 0;
        int yCount = 0;
        AspectBehavior x = new(typeof(Border), _ =>
        {
            int n = ++xCount;
            events.Add($"create X#{n}");
            return new RecordingLifetime(() =>
            {
                events.Add($"dispose X#{n}");
                if (n == 1)
                {
                    throw new InvalidOperationException("X dispose failed");
                }
            });
        });
        AspectBehavior y = new(typeof(Border), _ =>
        {
            int n = ++yCount;
            events.Add($"create Y#{n}");
            return new RecordingLifetime(() => events.Add($"dispose Y#{n}"));
        });
        UIRoot root = new();
        root.AspectRegistry.Register(BehaviorPackage("X", x));
        Border border = new();
        root.VisualChildren.Add(border);
        root.ProcessFrame();

        root.AspectRegistry.Register(BehaviorPackage("Y", y), notify: false);
        root.AspectRegistry.Unregister("X");
        InvalidOperationException failure = Assert.Throws<InvalidOperationException>(() => root.ProcessFrame());

        Assert.Equal("X dispose failed", failure.Message);
        root.ProcessFrame();
        root.VisualChildren.Remove(border);
        Assert.Equal(["create X#1", "create Y#1", "dispose X#1", "dispose Y#1"], events);
    }

    [Fact]
    public void FailedPackageBehaviorAttachKeepsItsExceptionWhenARolledBackLifetimeThrows()
    {
        int rollbackDisposals = 0;
        AspectBehavior attached = new(typeof(Border), _ => new RecordingLifetime(() =>
        {
            rollbackDisposals++;
            throw new InvalidOperationException("rollback dispose failed");
        }));
        AspectBehavior failing = new(typeof(Border), _ => throw new InvalidOperationException("attach failed"));
        UIRoot root = new();
        Border border = new();
        root.VisualChildren.Add(border);
        root.ProcessFrame();

        root.AspectRegistry.Register(AspectPackage.Create("Failing").Components(components =>
        {
            components.AddBehavior(attached);
            components.AddBehavior(failing);
        }));
        InvalidOperationException failure = Assert.Throws<InvalidOperationException>(() => root.ProcessFrame());

        Assert.Equal("attach failed", failure.Message);
        Assert.Equal(1, rollbackDisposals);
    }

    [Fact]
    public void PackageBehaviorFactoryThatDetachesItsElementDisposesItsLifetimeOnce()
    {
        List<string> events = [];
        int aCount = 0;
        int bCount = 0;
        UIRoot root = new();
        Border border = new();
        AspectBehavior a = new(typeof(Border), _ =>
        {
            int n = ++aCount;
            events.Add($"create A#{n}");
            return new RecordingLifetime(() => events.Add($"dispose A#{n}"));
        });
        AspectBehavior b = new(typeof(Border), _ =>
        {
            int n = ++bCount;
            events.Add($"create B#{n}");
            if (n == 1)
            {
                root.VisualChildren.Remove(border);
            }

            return new RecordingLifetime(() => events.Add($"dispose B#{n}"));
        });
        root.AspectRegistry.Register(BehaviorPackage("A", a));
        root.VisualChildren.Add(border);
        root.ProcessFrame();

        root.AspectRegistry.Register(BehaviorPackage("B", b));
        root.ProcessFrame();
        Assert.Equal(["create A#1", "create B#1", "dispose A#1", "dispose B#1"], events);

        root.VisualChildren.Add(border);
        root.ProcessFrame();
        Assert.Equal(["create A#2", "create B#2"], events[4..]);

        root.VisualChildren.Remove(border);
        Assert.Equal(["dispose B#2", "dispose A#2"], events[6..]);
    }

    [Fact]
    public void PackageBehaviorAfterAFactoryThatDetachesTheElementIsNotAttachedToTheDetachedElement()
    {
        List<string> events = [];
        int xCount = 0;
        int yCount = 0;
        UIRoot root = new();
        Border border = new();
        AspectBehavior x = new(typeof(Border), _ =>
        {
            int n = ++xCount;
            events.Add($"create X#{n}");
            if (n == 1)
            {
                root.VisualChildren.Remove(border);
            }

            return new RecordingLifetime(() => events.Add($"dispose X#{n}"));
        });
        AspectBehavior y = new(typeof(Border), _ =>
        {
            int n = ++yCount;
            events.Add($"create Y#{n}");
            return new RecordingLifetime(() => events.Add($"dispose Y#{n}"));
        });
        root.VisualChildren.Add(border);
        root.ProcessFrame();

        root.AspectRegistry.Register(AspectPackage.Create("XY").Components(components =>
        {
            components.AddBehavior(x);
            components.AddBehavior(y);
        }));
        root.ProcessFrame();
        Assert.Equal(["create X#1", "dispose X#1"], events);

        root.VisualChildren.Add(border);
        root.ProcessFrame();
        root.VisualChildren.Remove(border);
        Assert.Equal(["create X#2", "create Y#1", "dispose Y#1", "dispose X#2"], events[2..]);
    }

    [Fact]
    public void PackageBehaviorFactoryThatDetachesItsElementLeavesNoAspectValuesOnTheDetachedElement()
    {
        SolidColorBrush brush = new(Color.Red);
        UIRoot root = new();
        Button button = new();
        int attaches = 0;
        root.VisualChildren.Add(button);
        root.ProcessFrame();

        root.AspectRegistry.Register(AspectPackage.Create("P").Components(components =>
        {
            components.AddRule(new AspectRuleSet(
                "P.button",
                AspectLayer.App,
                new AspectTarget(typeof(Button)),
                [new AspectDeclaration(Control.BackgroundProperty, AspectValue<Brush?>.Literal(brush))],
                declarationOrder: 0));
            components.AddBehavior(new AspectBehavior(typeof(Button), _ =>
            {
                if (++attaches == 1)
                {
                    root.VisualChildren.Remove(button);
                }

                return null;
            }));
        }));
        root.ProcessFrame();

        Assert.False(button.IsAttached);
        Assert.NotSame(brush, button.Background);
        Assert.NotEqual(UiPropertyValueSource.AspectBase, button.GetValueSource(Control.BackgroundProperty));
        Assert.Equal(0, root.AspectProcessor.Engine.GetDependencies(button).CatalogVersion);

        root.VisualChildren.Add(button);
        root.ProcessFrame();
        Assert.Same(brush, button.Background);
        Assert.Equal(UiPropertyValueSource.AspectBase, button.GetValueSource(Control.BackgroundProperty));
    }

    [Fact]
    public void PackageBehaviorFactoryThatDetachesAndReattachesItsElementLeavesTheRuleApplied()
    {
        SolidColorBrush brush = new(Color.Red);
        UIRoot root = new();
        Button button = new();
        int attaches = 0;
        root.VisualChildren.Add(button);
        root.ProcessFrame();

        root.AspectRegistry.Register(AspectPackage.Create("P").Components(components =>
        {
            components.AddRule(new AspectRuleSet(
                "P.button",
                AspectLayer.App,
                new AspectTarget(typeof(Button)),
                [new AspectDeclaration(Control.BackgroundProperty, AspectValue<Brush?>.Literal(brush))],
                declarationOrder: 0));
            components.AddBehavior(new AspectBehavior(typeof(Button), _ =>
            {
                if (++attaches == 1)
                {
                    root.VisualChildren.Remove(button);
                    root.VisualChildren.Add(button);
                }

                return null;
            }));
        }));
        root.ProcessFrame();
        root.ProcessFrame();

        Assert.True(button.IsAttached);
        Assert.Same(brush, button.Background);
        Assert.Equal(UiPropertyValueSource.AspectBase, button.GetValueSource(Control.BackgroundProperty));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void PropertyChangedHandlerThatDetachesItsElementDuringTheApplyLeavesNoAspectStateOnTheDetachedElement(
        bool detachOnLastDeclaration)
    {
        SolidColorBrush background = new(Color.Red);
        SolidColorBrush foreground = new(Color.Blue);
        UiProperty trigger = detachOnLastDeclaration ? Control.ForegroundProperty : Control.BackgroundProperty;
        UIRoot root = new();
        Button button = new();
        root.VisualChildren.Add(button);
        root.ProcessFrame();
        bool detached = false;
        button.PropertyChanged += (_, args) =>
        {
            if (!detached && ReferenceEquals(args.Property, trigger) && button.IsAttached)
            {
                detached = true;
                root.VisualChildren.Remove(button);
            }
        };

        root.AspectRegistry.Register(AspectPackage.Create("P").Components(components =>
        {
            components.AddRule(new AspectRuleSet(
                "P.button",
                AspectLayer.App,
                new AspectTarget(typeof(Button)),
                [
                    new AspectDeclaration(Control.BackgroundProperty, AspectValue<Brush?>.Literal(background)),
                    new AspectDeclaration(Control.ForegroundProperty, AspectValue<Brush?>.Literal(foreground)),
                ],
                declarationOrder: 0));
        }));
        root.ProcessFrame();

        Assert.False(button.IsAttached);
        Assert.NotEqual(UiPropertyValueSource.AspectBase, button.GetValueSource(Control.BackgroundProperty));
        Assert.NotEqual(UiPropertyValueSource.AspectBase, button.GetValueSource(Control.ForegroundProperty));
        Assert.Equal(0, root.AspectProcessor.Engine.GetDependencies(button).CatalogVersion);

        root.VisualChildren.Add(button);
        root.ProcessFrame();
        Assert.Same(background, button.Background);
        Assert.Same(foreground, button.Foreground);
        Assert.Equal(UiPropertyValueSource.AspectBase, button.GetValueSource(Control.BackgroundProperty));
        Assert.Equal(UiPropertyValueSource.AspectBase, button.GetValueSource(Control.ForegroundProperty));
    }

    [Fact]
    public void PropertyChangedHandlerThatDetachesItsElementDuringTheApplyLeavesItsComponentTemplateLikeANormalDetach()
    {
        UIRoot root = new();
        ComponentTemplate<Button> template = new("App.Button", _ => new Border());
        root.AspectRegistry.Register(AspectPackage.Create("App").Components(components => components.AddTemplate(
            new ComponentTemplateDefinition("App.Button", typeof(Button), template))));
        Button button = new() { ComponentTemplateKey = "App.Button" };
        root.VisualChildren.Add(button);
        root.ProcessFrame();
        ComponentTemplateInstance? instance = button.ComponentTemplateInstance;
        Assert.NotNull(instance);
        bool detached = false;
        button.PropertyChanged += (_, args) =>
        {
            if (!detached && ReferenceEquals(args.Property, Control.BackgroundProperty) && button.IsAttached)
            {
                detached = true;
                root.VisualChildren.Remove(button);
            }
        };

        root.AspectRegistry.Register(AspectPackage.Create("P").Components(components =>
        {
            components.AddRule(new AspectRuleSet(
                "P.button",
                AspectLayer.App,
                new AspectTarget(typeof(Button)),
                [new AspectDeclaration(Control.BackgroundProperty, AspectValue<Brush?>.Literal(new SolidColorBrush(Color.Red)))],
                declarationOrder: 0));
        }));
        root.ProcessFrame();

        Assert.False(button.IsAttached);
        Assert.Same(instance, button.ComponentTemplateInstance);
    }

    [Fact]
    public void PropertyChangedHandlerThatDetachesAndReattachesItsElementDuringTheApplyLeavesTheRuleApplied()
    {
        SolidColorBrush background = new(Color.Red);
        SolidColorBrush foreground = new(Color.Blue);
        UIRoot root = new();
        Button button = new();
        root.VisualChildren.Add(button);
        root.ProcessFrame();
        bool reattached = false;
        button.PropertyChanged += (_, args) =>
        {
            if (!reattached && ReferenceEquals(args.Property, Control.BackgroundProperty) && button.IsAttached)
            {
                reattached = true;
                root.VisualChildren.Remove(button);
                root.VisualChildren.Add(button);
            }
        };

        root.AspectRegistry.Register(AspectPackage.Create("P").Components(components =>
        {
            components.AddRule(new AspectRuleSet(
                "P.button",
                AspectLayer.App,
                new AspectTarget(typeof(Button)),
                [
                    new AspectDeclaration(Control.BackgroundProperty, AspectValue<Brush?>.Literal(background)),
                    new AspectDeclaration(Control.ForegroundProperty, AspectValue<Brush?>.Literal(foreground)),
                ],
                declarationOrder: 0));
        }));
        root.ProcessFrame();
        root.ProcessFrame();

        Assert.True(reattached);
        Assert.True(button.IsAttached);
        Assert.Same(background, button.Background);
        Assert.Same(foreground, button.Foreground);
        Assert.Equal(UiPropertyValueSource.AspectBase, button.GetValueSource(Control.BackgroundProperty));
        Assert.Equal(UiPropertyValueSource.AspectBase, button.GetValueSource(Control.ForegroundProperty));
        Assert.NotEqual(0, root.AspectProcessor.Engine.GetDependencies(button).CatalogVersion);
    }

    // An ItemsPresenter applies the Aspect of the rows it realizes during its measure, so a tree change made there
    // behaves like one made in the frame's Aspect phase.
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void PropertyChangedHandlerThatDetachesItsElementDuringTheApplyDoesNotThrow(bool insideItemsControl)
    {
        SolidColorBrush brush = new(Color.Red);
        UIRoot root = new(300, 200);
        root.AspectRegistry.Register(AspectPackage.Create("App").Components(components => components.AddRule(
            new AspectRuleSet(
                "button",
                AspectLayer.App,
                new AspectTarget(typeof(Button)),
                [new AspectDeclaration(Control.BackgroundProperty, AspectValue<Brush?>.Literal(brush))],
                declarationOrder: 0))));
        Button button = new();
        Border border = new() { Child = button };
        button.PropertyChanged += (_, args) =>
        {
            if (ReferenceEquals(args.Property, Control.BackgroundProperty) && button.IsAttached)
            {
                border.Child = null;
            }
        };
        AddRow(root, border, insideItemsControl);

        Exception? failure = Record.Exception(() => root.ProcessFrame());

        Assert.Null(failure);
        Assert.False(button.IsAttached);
        Assert.Null(border.Child);
        Assert.NotEqual(UiPropertyValueSource.AspectBase, button.GetValueSource(Control.BackgroundProperty));
        Assert.Equal(0, root.AspectProcessor.Engine.GetDependencies(button).CatalogVersion);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void PropertyChangedHandlerThatDetachesALaterRowElementDuringTheApplyLeavesNoAspectValuesOnIt(
        bool insideItemsControl)
    {
        SolidColorBrush brush = new(Color.Red);
        UIRoot root = new(300, 200);
        root.AspectRegistry.Register(AspectPackage.Create("App").Components(components =>
        {
            components.AddRule(new AspectRuleSet(
                "trigger",
                AspectLayer.App,
                new AspectTarget(typeof(TriggerControl)),
                [new AspectDeclaration(Control.BackgroundProperty, AspectValue<Brush?>.Literal(brush))],
                declarationOrder: 0));
            components.AddRule(new AspectRuleSet(
                "button",
                AspectLayer.App,
                new AspectTarget(typeof(Button)),
                [new AspectDeclaration(Control.BackgroundProperty, AspectValue<Brush?>.Literal(brush))],
                declarationOrder: 1));
        }));
        TriggerControl trigger = new();
        Button button = new();
        StackPanel row = new();
        row.Children.Add(trigger);
        row.Children.Add(button);
        trigger.PropertyChanged += (_, args) =>
        {
            if (ReferenceEquals(args.Property, Control.BackgroundProperty) && button.IsAttached)
            {
                row.Children.Remove(button);
            }
        };
        AddRow(root, row, insideItemsControl);

        Exception? failure = Record.Exception(() => root.ProcessFrame());

        Assert.Null(failure);
        Assert.Same(brush, trigger.Background);
        Assert.False(button.IsAttached);
        Assert.NotEqual(UiPropertyValueSource.AspectBase, button.GetValueSource(Control.BackgroundProperty));
        Assert.Equal(0, root.AspectProcessor.Engine.GetDependencies(button).CatalogVersion);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void PropertyChangedHandlerThatDetachesAndReattachesARowElementDuringTheApplyLeavesTheRuleApplied(
        bool insideItemsControl)
    {
        SolidColorBrush background = new(Color.Red);
        SolidColorBrush foreground = new(Color.Blue);
        UIRoot root = new(300, 200);
        root.AspectRegistry.Register(AspectPackage.Create("App").Components(components => components.AddRule(
            new AspectRuleSet(
                "button",
                AspectLayer.App,
                new AspectTarget(typeof(Button)),
                [
                    new AspectDeclaration(Control.BackgroundProperty, AspectValue<Brush?>.Literal(background)),
                    new AspectDeclaration(Control.ForegroundProperty, AspectValue<Brush?>.Literal(foreground)),
                ],
                declarationOrder: 0))));
        Button button = new();
        Border border = new() { Child = button };
        bool reattached = false;
        button.PropertyChanged += (_, args) =>
        {
            if (!reattached && ReferenceEquals(args.Property, Control.BackgroundProperty) && button.IsAttached)
            {
                reattached = true;
                border.Child = null;
                border.Child = button;
            }
        };
        AddRow(root, border, insideItemsControl);

        Exception? failure = Record.Exception(() =>
        {
            root.ProcessFrame();
            root.ProcessFrame();
        });

        Assert.Null(failure);
        Assert.True(reattached);
        Assert.True(button.IsAttached);
        Assert.Same(background, button.Background);
        Assert.Same(foreground, button.Foreground);
        Assert.Equal(UiPropertyValueSource.AspectBase, button.GetValueSource(Control.BackgroundProperty));
        Assert.Equal(UiPropertyValueSource.AspectBase, button.GetValueSource(Control.ForegroundProperty));
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void PackageBehaviorFactoryThatDetachesARowElementDoesNotThrow(bool insideItemsControl)
    {
        SolidColorBrush brush = new(Color.Red);
        UIRoot root = new(300, 200);
        Button button = new();
        Border border = new() { Child = button };
        int attaches = 0;
        root.AspectRegistry.Register(AspectPackage.Create("P").Components(components =>
        {
            components.AddRule(new AspectRuleSet(
                "P.button",
                AspectLayer.App,
                new AspectTarget(typeof(Button)),
                [new AspectDeclaration(Control.BackgroundProperty, AspectValue<Brush?>.Literal(brush))],
                declarationOrder: 0));
            components.AddBehavior(new AspectBehavior(typeof(Button), _ =>
            {
                if (++attaches == 1)
                {
                    border.Child = null;
                }

                return null;
            }));
        }));
        AddRow(root, border, insideItemsControl);

        Exception? failure = Record.Exception(() => root.ProcessFrame());

        Assert.Null(failure);
        Assert.False(button.IsAttached);
        Assert.NotEqual(UiPropertyValueSource.AspectBase, button.GetValueSource(Control.BackgroundProperty));

        border.Child = button;
        root.ProcessFrame();
        Assert.Same(brush, button.Background);
        Assert.Equal(UiPropertyValueSource.AspectBase, button.GetValueSource(Control.BackgroundProperty));
    }

    // The trigger's apply runs in the frame's Aspect phase after the row's, so the Aspect work that registering the
    // behavior package queues for the row waits for the ItemsPresenter's measure, which the row's new width causes
    // and which attaches the behavior to the attached row.
    [Fact]
    public void PackageBehaviorFactoryThatDetachesARowElementWhileTheItemsPresenterMeasuresDoesNotThrow()
    {
        SolidColorBrush brush = new(Color.Red);
        UIRoot root = new(300, 200);
        Button button = new();
        Border border = new() { Child = button };
        ItemsControl items = new();
        items.SetItems(new object[] { border });
        TriggerControl trigger = new();
        root.VisualChildren.Add(items);
        root.VisualChildren.Add(trigger);
        root.ProcessFrame();

        int attaches = 0;
        AspectPackage behaviorPackage = AspectPackage.Create("P").Components(components =>
        {
            components.AddRule(new AspectRuleSet(
                "P.button",
                AspectLayer.App,
                new AspectTarget(typeof(Button)),
                [new AspectDeclaration(Control.BackgroundProperty, AspectValue<Brush?>.Literal(brush))],
                declarationOrder: 0));
            components.AddBehavior(new AspectBehavior(typeof(Button), _ =>
            {
                if (++attaches == 1)
                {
                    border.Child = null;
                }

                return null;
            }));
        });
        bool registered = false;
        trigger.PropertyChanged += (_, args) =>
        {
            if (!registered && ReferenceEquals(args.Property, Control.BackgroundProperty))
            {
                registered = true;
                root.AspectRegistry.Register(behaviorPackage);
                border.Width = 40;
            }
        };
        root.AspectRegistry.Register(AspectPackage.Create("T").Components(components => components.AddRule(
            new AspectRuleSet(
                "T.trigger",
                AspectLayer.App,
                new AspectTarget(typeof(TriggerControl)),
                [new AspectDeclaration(Control.BackgroundProperty, AspectValue<Brush?>.Literal(new SolidColorBrush(Color.Blue)))],
                declarationOrder: 0))));

        Exception? failure = Record.Exception(() => root.ProcessFrame());

        Assert.Null(failure);
        Assert.True(registered);
        Assert.Equal(1, attaches);
        Assert.False(button.IsAttached);
        Assert.Null(border.Child);
        Assert.NotEqual(UiPropertyValueSource.AspectBase, button.GetValueSource(Control.BackgroundProperty));

        border.Child = button;
        root.ProcessFrame();
        Assert.Equal(2, attaches);
        Assert.Same(brush, button.Background);
        Assert.Equal(UiPropertyValueSource.AspectBase, button.GetValueSource(Control.BackgroundProperty));
    }

    // The nested Apply after the Clear stores its own result; the ended outer apply does not remove its values.
    [Fact]
    public void PropertyChangedHandlerThatClearsAndAppliesItsElementAgainDuringTheApplyKeepsTheNestedResult()
    {
        SolidColorBrush background = new(Color.Red);
        SolidColorBrush foreground = new(Color.Blue);
        AspectCatalog catalog = new AspectRegistry().Register(AspectPackage.Create("P").Components(components =>
        {
            components.AddRule(new AspectRuleSet(
                "P.button",
                AspectLayer.App,
                new AspectTarget(typeof(Button)),
                [
                    new AspectDeclaration(Control.BackgroundProperty, AspectValue<Brush?>.Literal(background)),
                    new AspectDeclaration(Control.ForegroundProperty, AspectValue<Brush?>.Literal(foreground)),
                ],
                declarationOrder: 0));
        })).BuildCatalog();
        AspectEngine engine = new();
        AspectEnvironment environment = new("test");
        Button button = new();
        bool nested = false;
        button.PropertyChanged += (_, args) =>
        {
            if (!nested && ReferenceEquals(args.Property, Control.BackgroundProperty))
            {
                nested = true;
                engine.Clear(button);
                engine.Apply(button, catalog, environment);
            }
        };

        engine.Apply(button, catalog, environment);

        Assert.True(nested);
        Assert.Equal(
            "Background=AspectBase/True Foreground=AspectBase/True",
            $"Background={button.GetValueSource(Control.BackgroundProperty)}/{ReferenceEquals(background, button.Background)} " +
            $"Foreground={button.GetValueSource(Control.ForegroundProperty)}/{ReferenceEquals(foreground, button.Foreground)}");
    }

    [Fact]
    public void RuleAndTargetCopyCallerOwnedCollections()
    {
        List<AspectCondition> conditions =
        [
            AspectCondition.Predicate("stable", _ => true)
        ];
        List<AspectDeclaration> declarations =
        [
            new AspectDeclaration(
                Control.BackgroundProperty,
                AspectValue<Brush?>.Literal(new SolidColorBrush(Color.White)))
        ];
        AspectTarget target = new(typeof(Button), conditions: conditions);
        AspectRuleSet rule = new("stable", AspectLayer.App, target, declarations, 0);

        conditions.Clear();
        declarations.Clear();

        Assert.Single(target.Conditions);
        Assert.Single(rule.Declarations);
    }

    [Fact]
    public void CatalogDiagnosticsKeepOriginalPackageAfterSharedRulesBuildAnotherCatalog()
    {
        AspectRuleSet shared = Rule("shared", typeof(Button));
        AspectCatalog firstCatalog = Catalog("First", shared);
        Button first = new();
        AspectEngine firstEngine = new();
        firstEngine.Apply(first, firstCatalog, new AspectEnvironment("first"));
        Assert.Equal("First", Assert.Single(firstEngine.GetDiagnostics(first).ResolutionSteps).PackageName);

        AspectCatalog secondCatalog = Catalog("Second", shared);
        Button second = new();
        AspectEngine secondEngine = new();
        secondEngine.Apply(second, secondCatalog, new AspectEnvironment("second"));
        firstEngine.Apply(first, firstCatalog, new AspectEnvironment("first-again"));

        Assert.Equal("First", Assert.Single(firstEngine.GetDiagnostics(first).ResolutionSteps).PackageName);
        Assert.Equal("Second", Assert.Single(secondEngine.GetDiagnostics(second).ResolutionSteps).PackageName);
    }

    [Fact]
    public void ElementAspectUsesCanonicalEngineSourceAndQueuesIncrementalMutation()
    {
        UIRoot root = new();
        Button button = new();
        ElementAspect aspect = new(
            [new ElementAspectValue(Control.BackgroundProperty, new SolidColorBrush(Color.White))]);
        root.VisualChildren.Add(button);
        root.ProcessFrame();

        button.Aspect = aspect;
        root.ProcessFrame();

        Assert.Equal(UiPropertyValueSource.AspectBase, button.GetValueSource(Control.BackgroundProperty));

        Assert.True(aspect.SetValue(Control.BackgroundProperty, new SolidColorBrush(Color.Black)));
        var update = root.ProcessFrame();
        var idle = root.ProcessFrame();

        Assert.True(update.AspectElements > 0);
        Assert.False(idle.HasWork);
    }

    [Fact]
    public void ApplicationAspectUsesCanonicalEngineSource()
    {
        Application application = new();
        SolidColorBrush brush = new(new Color(12, 34, 56));
        application.Resources["ApplicationAspect"] = Package("Application", brush);
        UIRoot root = new();
        root.SetResourceProvider(application.Resources);
        Button button = new();

        root.VisualChildren.Add(button);
        root.ProcessFrame();

        Assert.Same(brush, button.Background);
        Assert.Equal(UiPropertyValueSource.AspectBase, button.GetValueSource(Control.BackgroundProperty));
    }

    [Fact]
    public void ScopedAspectAppearsInCanonicalEngineDiagnostics()
    {
        UIRoot root = new();
        Border scope = new();
        scope.Resources["ScopedAspect"] = Package(
            "Scoped",
            new SolidColorBrush(Color.White));
        Button button = new();
        scope.Child = button;

        root.VisualChildren.Add(scope);
        root.ProcessFrame();

        Assert.Contains(
            root.Detective.CaptureAspect(button).ResolutionSteps,
            step => string.Equals(step.PackageName, "Scoped", StringComparison.Ordinal));
    }

    [Fact]
    public void RuntimeCascadeComposesRootApplicationScopesAndElementAspect()
    {
        SolidColorBrush rootBrush = new(new Color(10, 0, 0));
        SolidColorBrush applicationBrush = new(new Color(20, 0, 0));
        SolidColorBrush outerBrush = new(new Color(30, 0, 0));
        SolidColorBrush innerBrush = new(new Color(40, 0, 0));
        SolidColorBrush elementBrush = new(new Color(50, 0, 0));
        UIRoot root = new();
        root.AspectRegistry.Register(Package("Root", rootBrush));
        Application application = new();
        application.Resources["Application"] = Package("Application", applicationBrush);
        root.SetResourceProvider(application.Resources);
        Border outer = new();
        outer.Resources["Outer"] = Package("Outer", outerBrush);
        Border inner = new();
        inner.Resources["Inner"] = Package("Inner", innerBrush);
        Button button = new()
        {
            Aspect = new ElementAspect(
                [new ElementAspectValue(Control.BackgroundProperty, elementBrush)])
        };
        inner.Child = button;
        outer.Child = inner;
        root.VisualChildren.Add(outer);

        root.ProcessFrame();
        Assert.Same(elementBrush, button.Background);
        Assert.Equal(UiPropertyValueSource.AspectBase, button.GetValueSource(Control.BackgroundProperty));

        button.Aspect = null;
        root.ProcessFrame();
        Assert.Same(innerBrush, button.Background);

        inner.Resources.Remove("Inner");
        root.ProcessFrame();
        Assert.Same(outerBrush, button.Background);

        outer.Resources.Remove("Outer");
        root.ProcessFrame();
        Assert.Same(applicationBrush, button.Background);

        application.Resources.Remove("Application");
        root.ProcessFrame();
        Assert.Same(rootBrush, button.Background);
        Assert.False(root.ProcessFrame().HasWork);
    }

    [Fact]
    public void ReplacingScopedPackageRecomputesOnlyThroughAspectQueueAndReturnsIdle()
    {
        UIRoot root = new();
        Border scope = new();
        SolidColorBrush first = new(new Color(1, 2, 3));
        SolidColorBrush second = new(new Color(4, 5, 6));
        scope.Resources["Scoped"] = Package("Scoped.First", first);
        Button button = new();
        scope.Child = button;
        root.VisualChildren.Add(scope);
        root.ProcessFrame();

        scope.Resources["Scoped"] = Package("Scoped.Second", second);
        var update = root.ProcessFrame();
        var idle = root.ProcessFrame();

        Assert.Same(second, button.Background);
        Assert.True(update.AspectElements > 0);
        Assert.False(idle.HasWork);
    }

    [Fact]
    public void InnerScopeTokenOverridesOuterScopeWithoutLeakingToSibling()
    {
        AspectToken<Brush?> token = AspectToken.Create<Brush?>("scope.background");
        SolidColorBrush outerBrush = new(new Color(11, 22, 33));
        SolidColorBrush innerBrush = new(new Color(44, 55, 66));
        AspectPackage outerPackage = AspectPackage.Create("Outer.Token")
            .Tokens(tokens => tokens.Set(token, outerBrush))
            .Components(components => components.AddRule(new AspectRuleSet(
                "token.button",
                AspectLayer.App,
                new AspectTarget(typeof(Button)),
                [new AspectDeclaration(Control.BackgroundProperty, token.Ref())],
                0)));
        AspectPackage innerPackage = AspectPackage.Create("Inner.Token")
            .Tokens(tokens => tokens.Set(token, innerBrush));
        Border outer = new();
        outer.Resources["Outer"] = outerPackage;
        StackPanel children = new();
        Border inner = new();
        inner.Resources["Inner"] = innerPackage;
        Button innerButton = new();
        inner.Child = innerButton;
        Button sibling = new();
        children.VisualChildren.Add(inner);
        children.VisualChildren.Add(sibling);
        outer.Child = children;
        UIRoot root = new();
        root.VisualChildren.Add(outer);

        root.ProcessFrame();

        Assert.Same(innerBrush, innerButton.Background);
        Assert.Same(outerBrush, sibling.Background);
    }

    [Fact]
    public void SharedElementAspectInvalidatesAllConsumersAndSurvivesDetachReattach()
    {
        SolidColorBrush first = new(new Color(10, 20, 30));
        SolidColorBrush second = new(new Color(40, 50, 60));
        ElementAspect aspect = new(
            [new ElementAspectValue(Control.BackgroundProperty, first)]);
        UIRoot root = new();
        Button firstButton = new() { Aspect = aspect };
        Button secondButton = new() { Aspect = aspect };
        root.VisualChildren.Add(firstButton);
        root.VisualChildren.Add(secondButton);
        root.ProcessFrame();

        Assert.True(aspect.SetValue(Control.BackgroundProperty, second));
        var sharedUpdate = root.ProcessFrame();

        Assert.True(sharedUpdate.AspectElements >= 2);
        Assert.Same(second, firstButton.Background);
        Assert.Same(second, secondButton.Background);
        Assert.Equal(UiPropertyValueSource.AspectBase, firstButton.GetValueSource(Control.BackgroundProperty));

        root.VisualChildren.Remove(secondButton);
        root.ProcessFrame();
        Assert.True(aspect.SetValue(Control.BackgroundProperty, first));
        root.ProcessFrame();
        root.VisualChildren.Add(secondButton);
        root.ProcessFrame();

        Assert.Same(first, secondButton.Background);
        Assert.Equal(UiPropertyValueSource.AspectBase, secondButton.GetValueSource(Control.BackgroundProperty));
        Assert.False(root.ProcessFrame().HasWork);
    }

    [Fact]
    public void LongRunningSessionDoesNotExhaustCompositeCatalogVersions()
    {
        SolidColorBrush first = new(new Color(10, 20, 30));
        SolidColorBrush second = new(new Color(40, 50, 60));
        ElementAspect aspect = new(
            [new ElementAspectValue(Control.BackgroundProperty, first)]);
        UIRoot root = new();
        Button[] buttons = [new() { Aspect = aspect }, new() { Aspect = aspect }, new() { Aspect = aspect }];
        foreach (Button button in buttons)
        {
            root.VisualChildren.Add(button);
        }

        root.ProcessFrame();

        // Each element attach and each consumer of a changed shared ElementAspect composes
        // its catalog again. A composition must not consume a number from a finite space,
        // or ~2^31 valid compositions in a long-running session make ProcessFrame throw:
        // a composed catalog keeps the registry version it was built from.
        for (int round = 0; round < 3; round++)
        {
            SolidColorBrush next = round % 2 == 0 ? second : first;
            Assert.True(aspect.SetValue(Control.BackgroundProperty, next));
            root.ProcessFrame();

            Assert.All(buttons, button =>
            {
                Assert.Same(next, button.Background);
                Assert.Equal(
                    root.AspectRegistry.Version,
                    root.AspectProcessor.Engine.GetDependencies(button).CatalogVersion);
            });
        }
    }

    [Fact]
    public void ScopedPackageProvidesComponentTemplateThroughCanonicalCatalog()
    {
        Border generatedRoot = new();
        Border replacementRoot = new();
        ComponentTemplate<Button> template = new("Scoped.Template", _ => generatedRoot);
        ComponentTemplate<Button> replacement = new("Scoped.Template.Replacement", _ => replacementRoot);
        AspectPackage package = AspectPackage.Create("Scoped.Templates")
            .Components(components => components.AddTemplate(
                new ComponentTemplateDefinition("Scoped.Template", typeof(Button), template)));
        Border scope = new();
        scope.Resources["Templates"] = package;
        Button button = new() { ComponentTemplateKey = "Scoped.Template" };
        scope.Child = button;
        UIRoot root = new();
        root.VisualChildren.Add(scope);

        root.ProcessFrame();

        Assert.Same(generatedRoot, button.ComponentTemplateInstance!.Root);

        scope.Resources["Templates"] = AspectPackage.Create("Scoped.Templates.Replacement")
            .Components(components => components.AddTemplate(
                new ComponentTemplateDefinition("Scoped.Template", typeof(Button), replacement)))
            .Build();
        root.ProcessFrame();
        root.ProcessFrame();

        Assert.Same(replacementRoot, button.ComponentTemplateInstance!.Root);
        Assert.Null(generatedRoot.LogicalParent);
        Assert.Null(generatedRoot.VisualParent);
        Assert.False(root.ProcessFrame().HasWork);
    }

    [Fact]
    public void NamedAspectAppearsInCanonicalEngineDiagnostics()
    {
        ElementAspect named = new(
            "Named",
            typeof(Button),
            [new ElementAspectValue(
                Control.BackgroundProperty,
                new SolidColorBrush(Color.White))]);
        UIRoot root = new();
        Button button = new() { Aspect = named };
        root.VisualChildren.Add(button);
        root.ProcessFrame();

        Assert.Contains(
            root.Detective.CaptureAspect(button).ResolutionSteps,
            step => string.Equals(step.PackageName, "Named", StringComparison.Ordinal));
    }

    [Fact]
    public void LegacyMarkupAspectRuntimeTypeIsAbsent()
    {
        Type? legacyType = typeof(UIElement).Assembly.GetType(
            "Cerneala.UI.Markup.MarkupAspectResource",
            throwOnError: false);

        Assert.Null(legacyType);
    }

    private static AspectCatalog Catalog(string packageName, params AspectRuleSet[] rules)
    {
        AspectPackage package = AspectPackage.Create(packageName)
            .Components(components =>
            {
                foreach (AspectRuleSet rule in rules)
                {
                    components.AddRule(rule);
                }
            });
        return new AspectRegistry().Register(package).BuildCatalog();
    }

    // #850: the Resources of an Overlay are a scope of its content, which the open overlay projects into the root's
    // overlay layer, so adding, replacing or removing a package there reaches the content on the next frame.
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void ScopedPackageOnAnOverlayReachesItsProjectedContent(bool resourcesBeforeFirstFrame)
    {
        UIRoot root = new(100, 100);
        Border target = new() { Width = 20, Height = 10 };
        Button button = new() { Width = 30, Height = 15 };
        Overlay overlay = new()
        {
            PlacementTarget = target,
            Content = button,
            IsOpen = true
        };
        SolidColorBrush brush = new(new Color(9, 8, 7));
        SolidColorBrush replacement = new(new Color(1, 2, 3));
        if (resourcesBeforeFirstFrame)
        {
            overlay.Resources["Scoped"] = Package("Scoped", brush);
        }

        Cerneala.UI.Layout.Panels.Canvas canvas = new();
        canvas.VisualChildren.Add(target);
        canvas.VisualChildren.Add(overlay);
        root.VisualChildren.Add(canvas);
        root.ProcessFrame();
        Brush? unscoped = resourcesBeforeFirstFrame ? null : button.Background;
        if (!resourcesBeforeFirstFrame)
        {
            overlay.Resources["Scoped"] = Package("Scoped", brush);
            root.ProcessFrame();
        }

        root.ProcessFrame();

        Assert.NotSame(canvas, button.VisualParent?.VisualParent);
        Assert.Same(brush, button.Background);

        overlay.Resources["Scoped"] = Package("Scoped", replacement);
        root.ProcessFrame();
        Assert.Same(replacement, button.Background);

        overlay.Resources.Remove("Scoped");
        root.ProcessFrame();
        Assert.NotSame(replacement, button.Background);
        if (unscoped is not null)
        {
            Assert.Equal(unscoped, button.Background);
        }
    }

    private static AspectPackage Package(string packageName, Brush brush)
    {
        return AspectPackage.Create(packageName)
            .Components(components => components.AddRule(new AspectRuleSet(
                packageName + ".button",
                AspectLayer.App,
                new AspectTarget(typeof(Button)),
                [new AspectDeclaration(
                    Control.BackgroundProperty,
                    AspectValue<Brush?>.Literal(brush))],
                declarationOrder: 0)));
    }

    // Inside an ItemsControl the row is realized by its ItemsPresenter during the measure.
    private static void AddRow(UIRoot root, UIElement row, bool insideItemsControl)
    {
        if (insideItemsControl)
        {
            ItemsControl items = new();
            items.SetItems(new object[] { row });
            root.VisualChildren.Add(items);
        }
        else
        {
            root.VisualChildren.Add(row);
        }
    }

    private static AspectPackage BehaviorPackage(string packageName, AspectBehavior behavior)
    {
        return AspectPackage.Create(packageName)
            .Components(components => components.AddBehavior(behavior));
    }

    private static AspectRuleSet Rule(
        string name,
        Type targetType,
        params AspectCondition[] conditions)
    {
        return new AspectRuleSet(
            name,
            AspectLayer.App,
            new AspectTarget(targetType, conditions: conditions),
            [new AspectDeclaration(
                Control.BackgroundProperty,
                AspectValue<Brush?>.Literal(new SolidColorBrush(Color.White)))],
            declarationOrder: 0);
    }

    private sealed class CallbackLifetime(Action dispose) : IDisposable
    {
        private Action? dispose = dispose;

        public void Dispose()
        {
            Interlocked.Exchange(ref dispose, null)?.Invoke();
        }
    }

    // Runs the callback on every Dispose call, so a second disposal is visible.
    private sealed class TriggerControl : Control
    {
    }

    private sealed class RecordingLifetime(Action dispose) : IDisposable
    {
        public void Dispose() => dispose();
    }
}
