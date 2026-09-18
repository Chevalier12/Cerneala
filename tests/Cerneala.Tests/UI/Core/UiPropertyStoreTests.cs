using Cerneala.UI.Core;

namespace Cerneala.Tests.UI.Core;

public sealed class UiPropertyStoreTests
{
    [Fact]
    public void EffectiveValueUsesExplicitPrecedence()
    {
        UiProperty<string> property = UiProperty<string>.Register(
            UniqueName(),
            typeof(UiPropertyStoreTests),
            new UiPropertyMetadata<string>("default"));
        UiObject owner = new();

        owner.SetValue(property, "inherited", UiPropertyValueSource.Inherited);
        owner.SetValue(property, "aspect-base", UiPropertyValueSource.AspectBase);
        owner.SetValue(property, "aspect-state", UiPropertyValueSource.AspectVisualState);
        owner.SetValue(property, "animation", UiPropertyValueSource.Animation);
        owner.SetValue(property, "local", UiPropertyValueSource.Local);

        Assert.Equal("local", owner.GetValue(property));
        Assert.Equal(UiPropertyValueSource.Local, owner.GetValueSource(property));
    }

    [Fact]
    public void ClearingHigherSourceRevealsNextEffectiveValue()
    {
        UiProperty<string> property = UiProperty<string>.Register(
            UniqueName(),
            typeof(UiPropertyStoreTests),
            new UiPropertyMetadata<string>("default"));
        UiObject owner = new();
        owner.SetValue(property, "aspect", UiPropertyValueSource.AspectBase);
        owner.SetValue(property, "local");

        string previous = owner.ClearValue(property);

        Assert.Equal("local", previous);
        Assert.Equal("aspect", owner.GetValue(property));
        Assert.Equal(UiPropertyValueSource.AspectBase, owner.GetValueSource(property));
    }

    [Fact]
    public void MarkupSourcesSitBetweenAnimationAndAspectState()
    {
        UiProperty<string> property = UiProperty<string>.Register(
            UniqueName(),
            typeof(UiPropertyStoreTests),
            new UiPropertyMetadata<string>("default"));
        UiObject owner = new();

        owner.SetValue(property, "aspect-base", UiPropertyValueSource.AspectBase);
        owner.SetValue(property, "aspect-state", UiPropertyValueSource.AspectVisualState);
        owner.SetValue(property, "markup-base", UiPropertyValueSource.MarkupBase);
        owner.SetValue(property, "markup-condition", UiPropertyValueSource.MarkupConditional);
        Assert.Equal("markup-condition", owner.GetValue(property));

        owner.SetValue(property, "animation", UiPropertyValueSource.Animation);
        Assert.Equal("animation", owner.GetValue(property));

        owner.SetValue(property, "local", UiPropertyValueSource.Local);
        Assert.Equal("local", owner.GetValue(property));

        owner.ClearValue(property, UiPropertyValueSource.Local);
        owner.ClearValue(property, UiPropertyValueSource.Animation);
        owner.ClearValue(property, UiPropertyValueSource.MarkupConditional);
        Assert.Equal("markup-base", owner.GetValue(property));
    }

    [Fact]
    public void ExplicitTemplateOwnerBindingOverridesChildAspectButNotMarkup()
    {
        UiProperty<string> property = UiProperty<string>.Register(
            UniqueName(),
            typeof(UiPropertyStoreTests),
            new UiPropertyMetadata<string>("default"));
        UiObject owner = new();

        owner.SetValue(property, "aspect", UiPropertyValueSource.AspectVisualState);
        owner.SetValue(property, "owner", UiPropertyValueSource.TemplateOwnerBinding);
        Assert.Equal("owner", owner.GetValue(property));

        owner.SetValue(property, "markup", UiPropertyValueSource.MarkupBase);
        Assert.Equal("markup", owner.GetValue(property));
    }

    [Fact]
    public void StoreRejectsDefaultAsStoredSource()
    {
        UiPropertyStore store = new();
        UiProperty<int> property = UiProperty<int>.Register(
            UniqueName(),
            typeof(UiPropertyStoreTests),
            new UiPropertyMetadata<int>(0));

        Assert.Throws<ArgumentOutOfRangeException>(
            () => store.SetValue(property, UiPropertyValueSource.Default, 1));
    }

    [Fact]
    public void SourceValidationDoesNotUseEnumMetadataOrReflection()
    {
        DirectoryInfo? directory = new(AppContext.BaseDirectory);
        while (directory is not null &&
            !File.Exists(Path.Combine(directory.FullName, "Cerneala.slnx")))
        {
            directory = directory.Parent;
        }

        Assert.NotNull(directory);
        string source = File.ReadAllText(Path.Combine(directory.FullName, "UI", "Core", "UiPropertyStore.cs"));
        string[] forbiddenTokens =
        [
            "Enum.", "System.Reflection", "BindingFlags", "MethodInfo", "PropertyInfo",
            "FieldInfo", "GetCustomAttribute", "Activator.CreateInstance"
        ];
        Assert.Empty(forbiddenTokens.Where(token => source.Contains(token, StringComparison.Ordinal)));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    public void SourceValidationDoesNotAllocateAfterCollection(int operation)
    {
        UiPropertyStore store = new();
        UiProperty<object?> property = UiProperty<object?>.Register(
            UniqueName(), typeof(UiPropertyStoreTests), new UiPropertyMetadata<object?>(null));
        object value = new();
        Action act = operation switch
        {
            0 => () => store.SetValue(property, UiPropertyValueSource.Local, value),
            1 => () => { _ = store.GetSourceValue(property, UiPropertyValueSource.Local); },
            2 => () => store.ClearValue(property, UiPropertyValueSource.Local),
            _ => throw new ArgumentOutOfRangeException(nameof(operation))
        };
        for (int iteration = 0; iteration < 128; iteration++)
        {
            store.SetValue(property, UiPropertyValueSource.Local, value);
            act();
        }

        for (int cycle = 0; cycle < 8; cycle++)
        {
            store.SetValue(property, UiPropertyValueSource.Local, value);
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();
            long before = GC.GetAllocatedBytesForCurrentThread();
            act();
            long allocated = GC.GetAllocatedBytesForCurrentThread() - before;

            Assert.Equal(0, allocated);
        }
    }

    [Theory]
    [InlineData(int.MinValue)]
    [InlineData(-1)]
    [InlineData(0)]
    [InlineData(10)]
    [InlineData(int.MaxValue)]
    public void SourceValidationRejectsDefaultAndUndefinedValuesWithoutMutating(int invalid)
    {
        UiPropertyStore store = new();
        UiProperty<string> property = UiProperty<string>.Register(
            UniqueName(), typeof(UiPropertyStoreTests), new UiPropertyMetadata<string>("default"));
        store.SetValue(property, UiPropertyValueSource.Local, "local");
        long version = store.ValueVersion;
        UiPropertyValueSource source = (UiPropertyValueSource)invalid;

        ArgumentOutOfRangeException set = Assert.Throws<ArgumentOutOfRangeException>(() => store.SetValue(property, source, "invalid"));
        ArgumentOutOfRangeException get = Assert.Throws<ArgumentOutOfRangeException>(() => store.GetSourceValue(property, source));
        ArgumentOutOfRangeException clear = Assert.Throws<ArgumentOutOfRangeException>(() => store.ClearValue(property, source));
        foreach (ArgumentOutOfRangeException error in new[] { set, get, clear })
        {
            Assert.Equal("source", error.ParamName);
            Assert.Equal(source, error.ActualValue);
        }
        Assert.Equal(version, store.ValueVersion);
        Assert.Equal("local", store.GetValue(property));
    }

    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    [InlineData(4)]
    [InlineData(5)]
    [InlineData(6)]
    [InlineData(7)]
    [InlineData(8)]
    [InlineData(9)]
    public void SourceValidationPreservesEveryConcreteSource(int concrete)
    {
        UiPropertyStore store = new();
        UiProperty<string> property = UiProperty<string>.Register(
            UniqueName(), typeof(UiPropertyStoreTests), new UiPropertyMetadata<string>("default"));
        UiPropertyValueSource source = (UiPropertyValueSource)concrete;

        store.SetValue(property, source, "value");
        Assert.Equal("value", store.GetSourceValue(property, source));
        Assert.Equal("value", store.GetValue(property));
        Assert.Equal(source, store.GetValueSource(property));
        store.ClearValue(property, source);
        Assert.Null(store.GetSourceValue(property, source));
        Assert.Equal("default", store.GetValue(property));
        Assert.Equal(UiPropertyValueSource.Default, store.GetValueSource(property));
    }

    [Fact]
    public void PublicClearRejectsReadOnlyProperty()
    {
        UiPropertyKey<int> key = UiProperty<int>.RegisterReadOnly(
            UniqueName(),
            typeof(UiPropertyStoreTests),
            new UiPropertyMetadata<int>(0));
        UiObject owner = new();
        owner.SetValue(key, 7);

        Assert.Throws<InvalidOperationException>(() => owner.ClearValue(key.Property));
        Assert.Equal(7, owner.GetValue(key.Property));
    }

    private static string UniqueName()
    {
        return $"{nameof(UiPropertyStoreTests)}_{Guid.NewGuid():N}";
    }
}
