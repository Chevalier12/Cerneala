using System.Reflection;
using Cerneala.Backends.SdlGpu;
using Cerneala.Platforms.Sdl3;
using Cerneala.UI.Hosting.Sdl;

namespace Cerneala.Tests.SdlGpu;

public sealed class SdlArchitectureTests
{
    [Fact]
    public void SdlGpuBackendPublicApiIsLimitedToTheApplicationBootstrap()
    {
        Type[] exportedTypes = typeof(SdlGpuDeviceOwner).Assembly
            .GetExportedTypes()
            .OrderBy(static type => type.FullName, StringComparer.Ordinal)
            .ToArray();

        Assert.Equal([typeof(SdlGpuApplicationBackend)], exportedTypes);
    }

    [Fact]
    public void SdlBindingTypesDoNotEscapeTheAdapterPublicApis()
    {
        Assembly[] adapters =
        [
            typeof(SdlPlatformLifetime).Assembly,
            typeof(SdlGpuDeviceOwner).Assembly
        ];

        foreach (Assembly adapter in adapters.Distinct())
        {
            foreach (Type type in adapter.GetExportedTypes())
            {
                Assert.False(IsSdlBindingType(type.BaseType), $"{type.FullName} has an SDL base type.");
                Assert.DoesNotContain(type.GetInterfaces(), IsSdlBindingType);
                foreach (MemberInfo member in type.GetMembers(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static))
                {
                    Assert.False(MemberExposesSdlType(member), $"{type.FullName}.{member.Name} exposes SDL3-CS.");
                }
            }
        }
    }

    private static bool MemberExposesSdlType(MemberInfo member) => member switch
    {
        MethodInfo method => IsSdlBindingType(method.ReturnType) ||
            method.GetParameters().Any(parameter => IsSdlBindingType(parameter.ParameterType)),
        ConstructorInfo constructor =>
            constructor.GetParameters().Any(parameter => IsSdlBindingType(parameter.ParameterType)),
        PropertyInfo property => IsSdlBindingType(property.PropertyType),
        FieldInfo field => IsSdlBindingType(field.FieldType),
        EventInfo @event => IsSdlBindingType(@event.EventHandlerType),
        _ => false
    };

    private static bool IsSdlBindingType(Type? type)
    {
        if (type is null)
        {
            return false;
        }

        if (type.IsArray || type.IsByRef || type.IsPointer)
        {
            return IsSdlBindingType(type.GetElementType());
        }

        return string.Equals(type.Assembly.GetName().Name, "SDL3-CS", StringComparison.Ordinal) ||
            type.IsGenericType && type.GetGenericArguments().Any(IsSdlBindingType);
    }
}
