using System.Reflection;
using Cerneala.UI.Controls;
using Cerneala.UI.Elements;

namespace Cerneala.Tests.UI.Servo;

public sealed class ServoIdContractTests
{
    [Fact]
    public void IdNormalizesWhitespaceClearsAndRejectsNullElements()
    {
        Type? servo = typeof(Window).Assembly.GetType("Cerneala.UI.Servo.Servo", throwOnError: false);
        Assert.True(servo is { IsPublic: true }, "Required public Servo type 'Cerneala.UI.Servo.Servo' is missing.");
        MethodInfo getId = Assert.Single(servo.GetMethods(), method => method.Name == "GetId");
        MethodInfo setId = Assert.Single(servo.GetMethods(), method => method.Name == "SetId");
        UIElement element = new();

        setId.Invoke(null, [element, "  target  "]);
        Assert.Equal("target", getId.Invoke(null, [element]));

        setId.Invoke(null, [element, " \t "]);
        Assert.Null(getId.Invoke(null, [element]));

        setId.Invoke(null, [element, null]);
        Assert.Null(getId.Invoke(null, [element]));
        AssertInvocationInner<ArgumentNullException>(() => getId.Invoke(null, [null]));
        AssertInvocationInner<ArgumentNullException>(() => setId.Invoke(null, [null, "target"]));
    }

    private static void AssertInvocationInner<TException>(Action action)
        where TException : Exception
    {
        TargetInvocationException wrapper = Assert.Throws<TargetInvocationException>(action);
        Assert.IsType<TException>(wrapper.InnerException);
    }
}
