using System.Reflection;
using Cerneala.UI.Accessibility;
using Cerneala.UI.Controls;
using Cerneala.UI.Elements;
using Cerneala.UI.Hosting;
using Cerneala.UI.Layout;

namespace Cerneala.Tests.UI.Servo;

public sealed class ServoPublicApiContractTests
{
    private static readonly Assembly CernealaAssembly = typeof(Window).Assembly;

    [Fact]
    public void PublicSurfaceMatchesTheApprovedServoContract()
    {
        Type servo = RequirePublicType("Cerneala.UI.Servo.Servo");
        Type options = RequirePublicType("Cerneala.UI.Servo.ServoOptions");
        Type target = RequirePublicType("Cerneala.UI.Servo.ServoTarget");
        Type element = RequirePublicType("Cerneala.UI.Servo.ServoElement");
        Type point = RequirePublicType("Cerneala.UI.Servo.ServoPoint");
        Type condition = RequirePublicType("Cerneala.UI.Servo.ServoCondition");
        Type modifiers = RequirePublicType("Cerneala.UI.Servo.ServoModifiers");
        Type exception = RequirePublicType("Cerneala.UI.Servo.ServoException");

        Assert.NotNull(servo.GetConstructor([typeof(Window), options]));
        Assert.NotNull(servo.GetConstructor([typeof(UiHost), options]));
        Assert.NotNull(servo.GetField("IdProperty", BindingFlags.Public | BindingFlags.Static));
        Assert.NotNull(servo.GetMethod("GetId", [typeof(UIElement)]));
        Assert.NotNull(servo.GetMethod("SetId", [typeof(UIElement), typeof(string)]));

        string[] asyncOperations =
        [
            "FindAsync",
            "FindAllAsync",
            "ExistsAsync",
            "ClickAsync",
            "HoverAsync",
            "DragAsync",
            "ScrollAsync",
            "PressKeyAsync",
            "SendTextAsync",
            "TypeIntoAsync",
            "ReplaceTextAsync",
            "WaitForAsync",
            "WaitUntilAsync",
            "WaitForIdleAsync",
            "SaveScreenshotAsync"
        ];
        foreach (string operation in asyncOperations)
        {
            MethodInfo[] overloads = servo.GetMethods(BindingFlags.Instance | BindingFlags.Public)
                .Where(method => method.Name == operation)
                .ToArray();
            Assert.NotEmpty(overloads);
            Assert.All(overloads, method =>
            {
                Assert.True(typeof(Task).IsAssignableFrom(method.ReturnType), $"{method} must return Task or Task<T>.");
                Assert.Equal(typeof(CancellationToken), method.GetParameters()[^1].ParameterType);
            });
        }

        Assert.NotNull(target.GetMethod("ById", BindingFlags.Public | BindingFlags.Static));
        Assert.NotNull(target.GetMethod("ByName", BindingFlags.Public | BindingFlags.Static));
        Assert.NotNull(target.GetMethod("ByRole", BindingFlags.Public | BindingFlags.Static));
        Assert.NotNull(target.GetMethod("WithName", BindingFlags.Public | BindingFlags.Instance));
        Assert.NotNull(target.GetMethod("Within", BindingFlags.Public | BindingFlags.Instance));

        AssertServoElementProperty(element, "TypeName", typeof(string));
        AssertServoElementProperty(element, "Id", typeof(string));
        AssertServoElementProperty(element, "Name", typeof(string));
        AssertServoElementProperty(element, "Role", typeof(SemanticsRole));
        AssertServoElementProperty(element, "Bounds", typeof(LayoutRect));
        AssertServoElementProperty(element, "IsVisible", typeof(bool));
        AssertServoElementProperty(element, "IsEnabled", typeof(bool));
        AssertServoElementProperty(element, "IsFocused", typeof(bool));
        AssertServoElementProperty(element, "Value", typeof(string));
        Assert.NotNull(element.GetProperty("Properties", BindingFlags.Public | BindingFlags.Instance));

        Assert.True(point.IsValueType);
        Assert.NotNull(point.GetProperty("X", BindingFlags.Public | BindingFlags.Instance));
        Assert.NotNull(point.GetProperty("Y", BindingFlags.Public | BindingFlags.Instance));
        AssertEnumNames(condition, "Exists", "Missing", "Visible", "Hidden", "Enabled", "Disabled", "Focused");
        AssertEnumNames(modifiers, "None", "Shift", "Control", "Alt");
        Assert.Contains(options.GetProperties(), property => property.PropertyType == typeof(TimeSpan));

        string[] exceptionTypes =
        [
            "ServoTargetNotFoundException",
            "ServoTargetAmbiguousException",
            "ServoTargetNotActionableException",
            "ServoTimeoutException"
        ];
        foreach (string exceptionType in exceptionTypes)
        {
            Assert.True(RequirePublicType($"Cerneala.UI.Servo.{exceptionType}").IsSubclassOf(exception));
        }

        Assert.DoesNotContain(
            CernealaAssembly.GetExportedTypes(),
            type => type.Namespace == "Cerneala.UI.Servo" && type.Name.Contains("Driver", StringComparison.Ordinal));
        Assert.DoesNotContain(
            servo.GetMethods(BindingFlags.Instance | BindingFlags.Public)
                .SelectMany(method => method.GetParameters().Select(parameter => parameter.ParameterType).Append(method.ReturnType)),
            ExposesLiveElement);
    }

    private static Type RequirePublicType(string fullName)
    {
        Type? type = CernealaAssembly.GetType(fullName, throwOnError: false);
        Assert.True(type is { IsPublic: true }, $"Required public Servo type '{fullName}' is missing.");
        return type!;
    }

    private static void AssertServoElementProperty(Type element, string name, Type propertyType)
    {
        PropertyInfo? property = element.GetProperty(name, BindingFlags.Public | BindingFlags.Instance);
        Assert.NotNull(property);
        Assert.Equal(propertyType, property.PropertyType);
        Assert.False(property.CanWrite);
    }

    private static void AssertEnumNames(Type enumType, params string[] expectedNames)
    {
        Assert.True(enumType.IsEnum);
        Assert.Equal(expectedNames, Enum.GetNames(enumType));
    }

    private static bool ExposesLiveElement(Type type)
    {
        if (type == typeof(UIElement) || typeof(UIElement).IsAssignableFrom(type))
        {
            return true;
        }

        return type.IsGenericType && type.GetGenericArguments().Any(ExposesLiveElement);
    }
}
