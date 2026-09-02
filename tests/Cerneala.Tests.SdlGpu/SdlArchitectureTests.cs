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
        Assembly assembly = typeof(SdlGpuDeviceOwner).Assembly;
        Type[] exportedTypes = assembly
            .GetExportedTypes()
            .OrderBy(static type => type.FullName, StringComparer.Ordinal)
            .ToArray();

        Assert.Equal([typeof(SdlGpuApplicationBackend)], exportedTypes);

        MethodInfo entryPoint = Assert.Single(typeof(SdlGpuApplicationBackend).GetMethods(
            BindingFlags.Public | BindingFlags.Static | BindingFlags.DeclaredOnly));
        Assert.Equal(nameof(SdlGpuApplicationBackend.EnsureRegistered), entryPoint.Name);
        Assert.Equal(typeof(void), entryPoint.ReturnType);
        Assert.Empty(entryPoint.GetParameters());
    }

    [Fact]
    public void CerberusIsTopLevelInternalAndDoesNotExposeItsBackendOwnerInSignatures()
    {
        Assembly assembly = typeof(SdlGpuDeviceOwner).Assembly;
        Type cerberus = assembly.GetType("Cerneala.Backends.SdlGpu.Cerberus") ??
            throw new Xunit.Sdk.XunitException(
                "Expected a top-level Cerneala.Backends.SdlGpu.Cerberus type; " +
                "the current owner-coupled implementation is still nested.");

        Assert.False(cerberus.IsNested);
        Assert.False(cerberus.IsPublic);
        Assert.True(cerberus.IsSealed);

        const BindingFlags declaredMembers = BindingFlags.Public |
            BindingFlags.NonPublic |
            BindingFlags.Instance |
            BindingFlags.Static |
            BindingFlags.DeclaredOnly;
        Type ownerType = typeof(SdlGpuDrawingBackend);
        foreach (MemberInfo member in cerberus.GetMembers(declaredMembers))
        {
            Assert.False(
                MemberSignatureReferencesType(member, ownerType),
                $"Cerberus member '{member}' exposes its SdlGpuDrawingBackend owner.");
        }
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

    private static bool MemberSignatureReferencesType(MemberInfo member, Type forbidden) =>
        member switch
        {
            MethodInfo method => ReferencesType(method.ReturnType, forbidden) ||
                method.GetParameters().Any(parameter =>
                    ReferencesType(parameter.ParameterType, forbidden)),
            ConstructorInfo constructor => constructor.GetParameters().Any(parameter =>
                ReferencesType(parameter.ParameterType, forbidden)),
            PropertyInfo property => ReferencesType(property.PropertyType, forbidden),
            FieldInfo field => ReferencesType(field.FieldType, forbidden),
            _ => false
        };

    private static bool ReferencesType(Type candidate, Type forbidden)
    {
        if (candidate == forbidden)
        {
            return true;
        }

        if (candidate.IsArray || candidate.IsByRef || candidate.IsPointer)
        {
            return ReferencesType(candidate.GetElementType()!, forbidden);
        }

        return candidate.IsGenericType &&
            candidate.GetGenericArguments().Any(argument => ReferencesType(argument, forbidden));
    }

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
