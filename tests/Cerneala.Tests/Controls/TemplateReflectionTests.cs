namespace Cerneala.Tests.Controls;

public sealed class TemplateReflectionTests
{
    [Fact]
    public void TemplateRuntimeDoesNotUseReflection()
    {
        string root = FindRepositoryRoot();
        string templateRoot = Path.Combine(root, "UI", "Controls", "Templates");
        string[] forbiddenTokens =
        [
            "System.Reflection",
            "MethodInfo",
            "PropertyInfo",
            "FieldInfo",
            "BindingFlags",
            "MakeGenericType",
            "MakeGenericMethod",
            "Activator.CreateInstance",
            "GetCustomAttribute",
            "GetCustomAttributes",
            "Attribute.GetCustomAttributes"
        ];

        string[] offenders = Directory
            .EnumerateFiles(templateRoot, "*.cs", SearchOption.AllDirectories)
            .SelectMany(file =>
            {
                string text = File.ReadAllText(file);
                return forbiddenTokens
                    .Where(token => text.Contains(token, StringComparison.Ordinal))
                    .Select(token => $"{Path.GetRelativePath(root, file)} contains {token}");
            })
            .ToArray();

        Assert.Empty(offenders);
    }

    private static string FindRepositoryRoot()
    {
        DirectoryInfo? directory = new(AppContext.BaseDirectory);
        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "Cerneala.slnx")))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        throw new InvalidOperationException("Could not locate repository root.");
    }
}
