using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.ExceptionServices;
using Cerneala.UI.Elements;
using Cerneala.UI.Hosting;
using Cerneala.UI.Hosting.MonoGame;
using Cerneala.UI.Input;
using Cerneala.UI.Input.MonoGame;
using Microsoft.Xna.Framework.Input;

namespace Cerneala.Tests.Input;

public sealed class MonoGameInputCoordinateScaleTests
{
    [Fact]
    public void MonoGameInputSourceExposesDefaultCoordinateScaleOfOne()
    {
        MonoGameInputSource source = new();

        Assert.Equal(1f, GetCoordinateScale(source));
    }

    [Fact]
    public void CoordinateScaleDividesMousePositionIntoLogicalCoordinates()
    {
        MonoGameInputSource source = CreateInputSource(() => Mouse(x: 300, y: 150));

        SetCoordinateScale(source, 2.5f);

        InputFrame frame = source.GetFrame();

        Assert.Equal(120f, frame.Pointer.X);
        Assert.Equal(60f, frame.Pointer.Y);
    }

    [Fact]
    public void CoordinateScaleDoesNotAffectWheelDeltaOrButtons()
    {
        MonoGameInputSource source = CreateInputSource(() => Mouse(
            x: 80,
            y: 160,
            wheel: 240,
            leftButton: ButtonState.Pressed,
            rightButton: ButtonState.Pressed));

        SetCoordinateScale(source, 4f);

        InputFrame frame = source.GetFrame();

        Assert.Equal(240, frame.Pointer.WheelValue);
        Assert.Equal(240, frame.Pointer.WheelDelta);
        Assert.True(frame.Pointer.IsDown(InputMouseButton.Left));
        Assert.True(frame.Pointer.IsDown(InputMouseButton.Right));
        Assert.True(frame.Pointer.IsPressed(InputMouseButton.Left));
        Assert.False(frame.Pointer.IsDown(InputMouseButton.Middle));
    }

    [Fact]
    public void CoordinateScaleRejectsZeroNegativeOrNaN()
    {
        MonoGameInputSource source = new();
        _ = CoordinateScaleProperty;

        foreach (float invalidScale in new[] { 0f, -1f, float.NaN })
        {
            Assert.IsType<ArgumentOutOfRangeException>(
                Record.Exception(() => SetCoordinateScale(source, invalidScale)));
        }
    }

    [Fact]
    public void MonoGameUiHostUpdatesInputSourceScaleBeforeReadingFrame()
    {
        MonoGameInputSource? inputSource = null;
        float? scaleWhenMouseWasRead = null;

        inputSource = CreateInputSource(() =>
        {
            scaleWhenMouseWasRead = GetCoordinateScale(inputSource);
            return Mouse(x: 250, y: 125);
        });

        MonoGameUiHost monoGameHost = CreateMonoGameHostWithoutGraphics(inputSource);

        monoGameHost.Update(new UiViewport(100, 50, 2.5f), TimeSpan.FromMilliseconds(16));

        Assert.Equal(2.5f, scaleWhenMouseWasRead);
    }

    private static MonoGameInputSource CreateInputSource(Func<MouseState> readMouseState)
    {
        ConstructorInfo? constructor = typeof(MonoGameInputSource).GetConstructor(
            BindingFlags.Instance | BindingFlags.NonPublic,
            binder: null,
            [typeof(Func<MouseState>), typeof(Func<KeyboardState>)],
            modifiers: null);

        Assert.True(
            constructor is not null,
            "Expected MonoGameInputSource to expose an internal test seam constructor: " +
            ".ctor(Func<MouseState> readMouseState, Func<KeyboardState> readKeyboardState).");

        return (MonoGameInputSource)constructor.Invoke(
            [readMouseState, static () => new KeyboardState()]);
    }

    private static MonoGameUiHost CreateMonoGameHostWithoutGraphics(MonoGameInputSource inputSource)
    {
        MonoGameUiHost monoGameHost = (MonoGameUiHost)RuntimeHelpers.GetUninitializedObject(typeof(MonoGameUiHost));
        UiHost coreHost = new(new UiHostOptions
        {
            Root = new UIRoot(),
            InputSource = inputSource
        });

        SetField(monoGameHost, "host", coreHost);
        SetField(monoGameHost, "<InputSource>k__BackingField", inputSource);

        return monoGameHost;
    }

    private static void SetField(object instance, string fieldName, object value)
    {
        FieldInfo? field = instance.GetType().GetField(
            fieldName,
            BindingFlags.Instance | BindingFlags.NonPublic);

        Assert.True(field is not null, $"Expected field '{fieldName}' to exist on {instance.GetType().Name}.");
        field.SetValue(instance, value);
    }

    private static float GetCoordinateScale(MonoGameInputSource? source)
    {
        ArgumentNullException.ThrowIfNull(source);
        return (float)CoordinateScaleProperty.GetValue(source)!;
    }

    private static void SetCoordinateScale(MonoGameInputSource source, float scale)
    {
        try
        {
            CoordinateScaleProperty.SetValue(source, scale);
        }
        catch (TargetInvocationException exception) when (exception.InnerException is not null)
        {
            ExceptionDispatchInfo.Capture(exception.InnerException).Throw();
        }
    }

    private static PropertyInfo CoordinateScaleProperty
    {
        get
        {
            PropertyInfo? property = typeof(MonoGameInputSource).GetProperty(
                "CoordinateScale",
                BindingFlags.Instance | BindingFlags.Public);

            Assert.True(
                property is not null,
                "Expected MonoGameInputSource to expose public float CoordinateScale { get; set; }.");
            Assert.Equal(typeof(float), property.PropertyType);
            Assert.True(property.CanRead);
            Assert.True(property.CanWrite);

            return property;
        }
    }

    private static MouseState Mouse(
        int x,
        int y,
        int wheel = 0,
        ButtonState leftButton = ButtonState.Released,
        ButtonState middleButton = ButtonState.Released,
        ButtonState rightButton = ButtonState.Released,
        ButtonState xButton1 = ButtonState.Released,
        ButtonState xButton2 = ButtonState.Released)
    {
        object[] eightParameterArguments =
        [
            x,
            y,
            wheel,
            leftButton,
            middleButton,
            rightButton,
            xButton1,
            xButton2
        ];

        ConstructorInfo? eightParameterConstructor = typeof(MouseState).GetConstructor(
            eightParameterArguments.Select(argument => argument.GetType()).ToArray());

        if (eightParameterConstructor is not null)
        {
            return (MouseState)eightParameterConstructor.Invoke(eightParameterArguments);
        }

        object[] nineParameterArguments =
        [
            x,
            y,
            wheel,
            0,
            leftButton,
            middleButton,
            rightButton,
            xButton1,
            xButton2
        ];

        ConstructorInfo? nineParameterConstructor = typeof(MouseState).GetConstructor(
            nineParameterArguments.Select(argument => argument.GetType()).ToArray());

        Assert.True(nineParameterConstructor is not null, "Expected a supported MouseState constructor.");
        return (MouseState)nineParameterConstructor.Invoke(nineParameterArguments);
    }
}
