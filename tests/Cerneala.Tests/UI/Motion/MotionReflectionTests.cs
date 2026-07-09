namespace Cerneala.Tests.UI.Motion;

public sealed class MotionReflectionTests
{
    [Fact]
    public void MotionRuntimeDoesNotUseReflection()
    {
        string root = FindRepositoryRoot();
        string motionRoot = Path.Combine(root, "UI", "Motion");
        string[] forbiddenTokens =
        [
            "System.Reflection",
            "MethodInfo",
            "PropertyInfo",
            "FieldInfo",
            "BindingFlags",
            ".GetMethod(",
            ".GetProperty(",
            ".GetField(",
            "MakeGenericMethod",
            "Activator.CreateInstance",
            "TargetInvocationException"
        ];

        string[] offenders = Directory
            .EnumerateFiles(motionRoot, "*.cs", SearchOption.AllDirectories)
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
