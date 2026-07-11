using System.Reflection;
using Cerneala.Drawing;
using Cerneala.UI.Aspect;
using Cerneala.UI.Controls;
using Cerneala.UI.Controls.Primitives;
using Cerneala.UI.Core;
using Cerneala.UI.Layout;

namespace Cerneala.Tests.UI.Aspect;

public sealed class AspectRuleSetTests
{
    [Fact]
    public void BaseRuleMatchesControlTypeAndSlot()
    {
        AspectSlot<Button, Button> slot = AspectSlot.Root<Button>();
        AspectTarget target = new(typeof(Button), slot);
        AspectMatchContext context = Context(new Button(), slot);

        Assert.True(target.Matches(context));
    }

    [Fact]
    public void StateRuleMatchesOnlyWhenStateIsPresent()
    {
        AspectCondition condition = AspectCondition.State(AspectState.Hover);

        Assert.True(condition.Evaluate(Context(new Button(), states: AspectStateSet.Empty.Add(AspectState.Hover))).Matches);
        Assert.False(condition.Evaluate(Context(new Button())).Matches);
    }

    [Fact]
    public void VariantRuleMatchesOnlyWhenVariantValueMatches()
    {
        AspectVariantKey<Button, ButtonKind> key = AspectVariantKey.For<Button, ButtonKind>("kind");
        AspectCondition condition = AspectCondition.Variant(key, ButtonKind.Primary);

        Assert.True(condition.Evaluate(Context(new Button(), variants: AspectVariantSet.Empty.Set(key, ButtonKind.Primary))).Matches);
        Assert.False(condition.Evaluate(Context(new Button(), variants: AspectVariantSet.Empty.Set(key, ButtonKind.Neutral))).Matches);
    }

    [Fact]
    public void PropertyConditionMatchesCurrentUiPropertyValue()
    {
        Button button = new() { IsPressed = true };

        AspectCondition condition = AspectCondition.Property(ButtonBase.IsPressedProperty).Is(true);

        Assert.True(condition.Evaluate(Context(button)).Matches);
    }

    [Fact]
    public void DataConditionMatchesTypedTemplateData()
    {
        AspectCondition condition = AspectCondition.Data<UserCard>(
            "important user",
            user => user.IsImportant,
            AspectDataDependency.Property<UserCard, bool>(nameof(UserCard.IsImportant)));

        Assert.True(condition.Evaluate(Context(new Button(), data: new UserCard(true))).Matches);
        Assert.False(condition.Evaluate(Context(new Button(), data: new UserCard(false))).Matches);
        Assert.False(condition.Evaluate(Context(new Button(), data: "wrong")).Matches);
    }

    [Fact]
    public void MultiConditionAllRequiresEveryCondition()
    {
        AspectCondition condition = AspectCondition.All(
            AspectCondition.State(AspectState.Hover),
            AspectCondition.State(AspectState.Focus));

        Assert.True(condition.Evaluate(Context(new Button(), states: AspectStateSet.Empty.Add(AspectState.Hover).Add(AspectState.Focus))).Matches);
        Assert.False(condition.Evaluate(Context(new Button(), states: AspectStateSet.Empty.Add(AspectState.Hover))).Matches);
    }

    [Fact]
    public void MultiConditionAnyRequiresAtLeastOneCondition()
    {
        AspectCondition condition = AspectCondition.Any(
            AspectCondition.State(AspectState.Hover),
            AspectCondition.State(AspectState.Focus));

        Assert.True(condition.Evaluate(Context(new Button(), states: AspectStateSet.Empty.Add(AspectState.Focus))).Matches);
        Assert.False(condition.Evaluate(Context(new Button())).Matches);
    }

    [Fact]
    public void MultiConditionNotInvertsCondition()
    {
        AspectCondition condition = AspectCondition.Not(AspectCondition.State(AspectState.Disabled));

        Assert.True(condition.Evaluate(Context(new Button())).Matches);
        Assert.False(condition.Evaluate(Context(new Button(), states: AspectStateSet.Empty.Add(AspectState.Disabled))).Matches);
    }

    [Fact]
    public void ConditionDependenciesAreReportedForInvalidation()
    {
        AspectVariantKey<Button, ButtonKind> key = AspectVariantKey.For<Button, ButtonKind>("kind");
        AspectCondition condition = AspectCondition.All(
            AspectCondition.State(AspectState.Hover),
            AspectCondition.Variant(key, ButtonKind.Primary),
            AspectCondition.Property(ButtonBase.IsPressedProperty).Is(true),
            AspectCondition.Data<UserCard>("important user", user => user.IsImportant, AspectDataDependency.Named("user")));

        AspectConditionResult result = condition.Evaluate(Context(new Button(), data: new UserCard(true)));

        Assert.Contains(result.Dependencies, dependency => dependency.Kind == AspectConditionDependencyKind.State);
        Assert.Contains(result.Dependencies, dependency => dependency.Kind == AspectConditionDependencyKind.Variant);
        Assert.Contains(result.Dependencies, dependency => dependency.Kind == AspectConditionDependencyKind.UiProperty);
        Assert.Contains(result.Dependencies, dependency => dependency.Kind == AspectConditionDependencyKind.DataContext);
    }

    [Fact]
    public void AspectDoesNotExposeTriggerCollections()
    {
        Type[] aspectTypes = typeof(AspectCondition).Assembly.GetTypes()
            .Where(type => type.Namespace == "Cerneala.UI.Aspect")
            .ToArray();

        Assert.DoesNotContain(aspectTypes, type => type.Name.Contains("Trigger", StringComparison.Ordinal));
        Assert.DoesNotContain(typeof(AspectRuleSet).GetProperties(BindingFlags.Public | BindingFlags.Instance), property => property.Name.Contains("Trigger", StringComparison.Ordinal));
    }

    [Fact]
    public void LayerOrderWinsBeforeDeclarationOrder()
    {
        AspectDeclaration theme = Declaration(Color.White);
        AspectDeclaration app = Declaration(Color.Black);
        AspectMatchContext context = Context(new Button());
        AspectRuleSet first = Rule("theme", AspectLayer.Theme, theme, declarationOrder: 10);
        AspectRuleSet second = Rule("app", AspectLayer.App, app, declarationOrder: 1);

        IReadOnlyDictionary<UiProperty, AspectDeclaration> resolved = AspectRuleSet.ResolveDeclarations([first, second], context);

        Assert.Same(app, resolved[Control.BackgroundProperty]);
    }

    [Fact]
    public void HigherSpecificityWinsWithinSameLayer()
    {
        AspectDeclaration baseDeclaration = Declaration(Color.White);
        AspectDeclaration stateDeclaration = Declaration(Color.Black);
        AspectMatchContext context = Context(new Button(), states: AspectStateSet.Empty.Add(AspectState.Hover));
        AspectRuleSet baseRule = Rule("base", AspectLayer.App, baseDeclaration, declarationOrder: 2);
        AspectRuleSet stateRule = new(
            "state",
            AspectLayer.App,
            new AspectTarget(typeof(Button), conditions: [AspectCondition.State(AspectState.Hover)]),
            [stateDeclaration],
            declarationOrder: 1);

        IReadOnlyDictionary<UiProperty, AspectDeclaration> resolved = AspectRuleSet.ResolveDeclarations([baseRule, stateRule], context);

        Assert.Same(stateDeclaration, resolved[Control.BackgroundProperty]);
    }

    [Fact]
    public void LaterDeclarationWinsForEqualLayerAndSpecificity()
    {
        AspectDeclaration firstDeclaration = Declaration(Color.White);
        AspectDeclaration secondDeclaration = Declaration(Color.Black);
        AspectMatchContext context = Context(new Button());
        AspectRuleSet first = Rule("first", AspectLayer.App, firstDeclaration, declarationOrder: 1);
        AspectRuleSet second = Rule("second", AspectLayer.App, secondDeclaration, declarationOrder: 2);

        IReadOnlyDictionary<UiProperty, AspectDeclaration> resolved = AspectRuleSet.ResolveDeclarations([first, second], context);

        Assert.Same(secondDeclaration, resolved[Control.BackgroundProperty]);
    }

    private static AspectMatchContext Context(
        Button element,
        AspectSlot? slot = null,
        AspectStateSet? states = null,
        AspectVariantSet? variants = null,
        object? data = null)
    {
        return new AspectMatchContext(
            element,
            ownerComponent: element,
            slotPath: slot is null ? null : new AspectSlotPath(slot),
            states: states ?? AspectStateSet.Empty,
            variants: variants ?? AspectVariantSet.Empty,
            environmentVersion: 0,
            dataContext: new AspectDataContext(data, data?.GetType()));
    }

    private static AspectRuleSet Rule(string name, AspectLayer layer, AspectDeclaration declaration, int declarationOrder)
    {
        return new AspectRuleSet(
            name,
            layer,
            new AspectTarget(typeof(Button)),
            [declaration],
            declarationOrder);
    }

    private static AspectDeclaration Declaration(Color color)
    {
        return new AspectDeclaration(Control.BackgroundProperty, AspectValue<Color>.Literal(color));
    }

    private sealed record UserCard(bool IsImportant);

    private enum ButtonKind
    {
        Neutral,
        Primary
    }
}
