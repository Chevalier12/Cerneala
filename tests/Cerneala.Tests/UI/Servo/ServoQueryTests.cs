using System.Reflection;
using Cerneala.UI.Accessibility;
using Cerneala.UI.Controls;
using Cerneala.UI.Elements;
using Cerneala.UI.Hosting;
using Cerneala.UI.Input;
using Cerneala.UI.Layout;
using Cerneala.UI.Servo;
using ServoApi = Cerneala.UI.Servo.Servo;

namespace Cerneala.Tests.UI.Servo;

public sealed class ServoQueryTests
{
    [Fact]
    public void PublicValueTypesAndOptionsValidateTheirInputs()
    {
        ServoOptions options = new();
        Assert.Equal(TimeSpan.FromSeconds(5), options.DefaultTimeout);
        Assert.Throws<ArgumentOutOfRangeException>(() => options.DefaultTimeout = TimeSpan.Zero);
        Assert.Throws<ArgumentOutOfRangeException>(() => options.DefaultTimeout = Timeout.InfiniteTimeSpan);
        options.DefaultTimeout = TimeSpan.FromMilliseconds(250);
        Assert.Equal(TimeSpan.FromMilliseconds(250), options.DefaultTimeout);

        Assert.Throws<ArgumentOutOfRangeException>(() => new ServoPoint(float.NaN, 0));
        Assert.Throws<ArgumentOutOfRangeException>(() => new ServoPoint(0, float.PositiveInfinity));
        Assert.Equal(new ServoPoint(12.5f, -4), new ServoPoint(12.5f, -4));

        Assert.Throws<ArgumentNullException>(() => new ServoApi((Window)null!));
        Assert.Throws<ArgumentNullException>(() => new ServoApi((UiHost)null!));
        Assert.Throws<ArgumentException>(() => ServoTarget.ById(" "));
        Assert.Throws<ArgumentException>(() => ServoTarget.ByName(""));
        Assert.Throws<ArgumentOutOfRangeException>(() => ServoTarget.ByRole((SemanticsRole)int.MaxValue));
        Assert.Throws<ArgumentNullException>(() => ServoTarget.ById("target").Within(null!));
    }

    [Fact]
    public async Task SelectorsComposeAndSnapshotsReadLiveElementState()
    {
        UIRoot root = new(320, 160);
        UIElement group = new();
        ServoApi.SetId(group, "form");
        Button save = new() { Content = "Save", Width = 120, Height = 40 };
        ServoApi.SetId(save, "save");
        group.VisualChildren.Add(save);
        root.VisualChildren.Add(group);
        UiHost host = CreateHost(root, 320, 160);
        ServoApi servo = new(host);

        ServoElement byId = await servo.FindAsync(ServoTarget.ById("save"));
        ServoElement byName = await servo.FindAsync(ServoTarget.ByName("Save"));
        ServoElement composed = await servo.FindAsync(
            ServoTarget.ByRole(SemanticsRole.Button)
                .WithName("Save")
                .Within(ServoTarget.ById("form")));

        Assert.Equal("Button", byId.TypeName);
        Assert.Equal("save", byId.Id);
        Assert.Equal("Save", byId.Name);
        Assert.Equal(SemanticsRole.Button, byId.Role);
        Assert.True(byId.IsVisible);
        Assert.True(byId.IsEnabled);
        Assert.Equal(save.ArrangedBounds, byId.Bounds);
        Assert.Equal(byId.Id, byName.Id);
        Assert.Equal(byId.Id, composed.Id);
        Assert.False(await servo.ExistsAsync(ServoTarget.ByName("save")));

        LayoutRect movedBounds = new(37, 23, 91, 29);
        save.Arrange(new ArrangeContext(movedBounds));

        ServoElement moved = await servo.FindAsync(ServoTarget.ById("save"));
        Assert.Equal(movedBounds, moved.Bounds);
    }

    [Fact]
    public async Task CardinalityContractsDistinguishMissingSingleAndAmbiguousTargets()
    {
        UIRoot root = new(320, 160);
        Button first = new() { Content = "First" };
        Button second = new() { Content = "Second" };
        ServoApi.SetId(first, "duplicate");
        ServoApi.SetId(second, "duplicate");
        root.VisualChildren.Add(first);
        root.VisualChildren.Add(second);
        ServoApi servo = new(CreateHost(root, 320, 160));

        IReadOnlyList<ServoElement> matches = await servo.FindAllAsync(ServoTarget.ById("duplicate"));

        Assert.Equal(new[] { "First", "Second" }, matches.Select(match => match.Name));
        Assert.True(await servo.ExistsAsync(ServoTarget.ById("duplicate")));
        await Assert.ThrowsAsync<ServoTargetAmbiguousException>(
            () => servo.FindAsync(ServoTarget.ById("duplicate")));

        Assert.Empty(await servo.FindAllAsync(ServoTarget.ById("missing")));
        Assert.False(await servo.ExistsAsync(ServoTarget.ById("missing")));
        await Assert.ThrowsAsync<ServoTargetNotFoundException>(
            () => servo.FindAsync(ServoTarget.ById("missing")));
    }

    [Fact]
    public async Task HiddenNodesRemainQueryableAndDistinctFromMissingNodes()
    {
        UIRoot root = new(320, 160);
        UIElement hiddenAncestor = new() { Visibility = Visibility.Hidden };
        Button child = new() { Content = "Hidden child" };
        ServoApi.SetId(child, "hidden-child");
        hiddenAncestor.VisualChildren.Add(child);
        root.VisualChildren.Add(hiddenAncestor);
        ServoApi servo = new(CreateHost(root, 320, 160));

        ServoElement hidden = await servo.FindAsync(ServoTarget.ById("hidden-child"));

        Assert.False(hidden.IsVisible);
        Assert.True(await servo.ExistsAsync(ServoTarget.ById("hidden-child")));
        Assert.False(await servo.ExistsAsync(ServoTarget.ById("missing")));
    }

    [Fact]
    public async Task ReusedTargetsResolveReplacementAndCurrentAncestry()
    {
        UIRoot root = new(320, 160);
        UIElement firstContainer = new();
        UIElement secondContainer = new();
        ServoApi.SetId(firstContainer, "first-container");
        ServoApi.SetId(secondContainer, "second-container");
        Button original = new() { Content = "Original" };
        ServoApi.SetId(original, "mutable-target");
        firstContainer.VisualChildren.Add(original);
        root.VisualChildren.Add(firstContainer);
        root.VisualChildren.Add(secondContainer);
        ServoApi servo = new(CreateHost(root, 320, 160));
        ServoTarget reusable = ServoTarget.ById("mutable-target");

        ServoElement firstSnapshot = await servo.FindAsync(reusable);
        firstContainer.VisualChildren.Remove(original);
        Button replacement = new() { Content = "Replacement" };
        ServoApi.SetId(replacement, "mutable-target");
        secondContainer.VisualChildren.Add(replacement);

        ServoElement secondSnapshot = await servo.FindAsync(reusable);

        Assert.Equal("Original", firstSnapshot.Name);
        Assert.Equal("Replacement", secondSnapshot.Name);
        Assert.False(await servo.ExistsAsync(reusable.Within(ServoTarget.ById("first-container"))));
        Assert.True(await servo.ExistsAsync(reusable.Within(ServoTarget.ById("second-container"))));

        secondContainer.VisualChildren.Remove(replacement);
        firstContainer.VisualChildren.Add(replacement);

        Assert.True(await servo.ExistsAsync(reusable.Within(ServoTarget.ById("first-container"))));
        Assert.False(await servo.ExistsAsync(reusable.Within(ServoTarget.ById("second-container"))));
    }

    [Fact]
    public void PublicQuerySurfaceDoesNotExposeLiveElementsOrInfrastructure()
    {
        Assembly assembly = typeof(ServoApi).Assembly;
        Type[] servoTypes = assembly.GetExportedTypes()
            .Where(type => type.Namespace == "Cerneala.UI.Servo")
            .ToArray();

        Assert.DoesNotContain(servoTypes, type => type.Name.Contains("Driver", StringComparison.Ordinal));
        Assert.DoesNotContain(
            typeof(ServoElement).GetMembers(BindingFlags.Public | BindingFlags.Instance)
                .SelectMany(MemberTypes),
            type => typeof(UIElement).IsAssignableFrom(type));
        Assert.Empty(typeof(ServoElement).GetConstructors(BindingFlags.Public | BindingFlags.Instance));
        Assert.All(
            typeof(ServoTarget).GetProperties(BindingFlags.Public | BindingFlags.Instance),
            property => Assert.False(property.CanWrite));

        ServoException[] specializedExceptions =
        [
            new ServoTargetNotFoundException("missing"),
            new ServoTargetAmbiguousException("ambiguous"),
            new ServoTargetNotActionableException("not actionable"),
            new ServoTimeoutException("timeout")
        ];
        Assert.All(specializedExceptions, exception => Assert.IsAssignableFrom<ServoException>(exception));
    }

    [Fact]
    public async Task QueriesHonorPreCanceledTokensAndRejectNullTargets()
    {
        ServoApi servo = new(CreateHost(new UIRoot(100, 100), 100, 100));
        using CancellationTokenSource cancellation = new();
        cancellation.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => servo.FindAsync(ServoTarget.ById("target"), cancellation.Token));
        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => servo.FindAllAsync(ServoTarget.ById("target"), cancellation.Token));
        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => servo.ExistsAsync(ServoTarget.ById("target"), cancellation.Token));
        Assert.Throws<ArgumentNullException>(() => { _ = servo.FindAsync(null!); });
        Assert.Throws<ArgumentNullException>(() => { _ = servo.FindAllAsync(null!); });
        Assert.Throws<ArgumentNullException>(() => { _ = servo.ExistsAsync(null!); });
    }

    private static UiHost CreateHost(UIRoot root, float width, float height)
    {
        UiHost host = new(new UiHostOptions
        {
            Root = root,
            Viewport = new UiViewport(width, height)
        });
        host.Update(
            new InputFrame(
                PointerSnapshot.Empty,
                PointerSnapshot.Empty,
                KeyboardSnapshot.Empty,
                KeyboardSnapshot.Empty,
                []),
            host.Viewport,
            TimeSpan.Zero);
        return host;
    }

    private static IEnumerable<Type> MemberTypes(MemberInfo member)
    {
        return member switch
        {
            PropertyInfo property => [property.PropertyType],
            MethodInfo method => method.GetParameters()
                .Select(parameter => parameter.ParameterType)
                .Append(method.ReturnType),
            FieldInfo field => [field.FieldType],
            _ => []
        };
    }
}
