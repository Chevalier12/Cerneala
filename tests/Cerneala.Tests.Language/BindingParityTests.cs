namespace Cerneala.Tests.Language;

// The editor (semantic model) must report the binding diagnostics the build (source generator) reports, for the
// same markup and the same view model source.
public sealed class BindingParityTests
{
    private const string ViewModelSource = """
        using System.ComponentModel;

        namespace Demo;

        public sealed class VM : INotifyPropertyChanged
        {
            public event PropertyChangedEventHandler? PropertyChanged;
            public string Name { get; set; } = string.Empty;
            public int Idx { get; set; }
            public static string Version { get; } = "1";
            public bool A { get; set; }
            public bool B { get; set; }
        }

        // Caption and Flag are plain CLR properties: no CaptionProperty or FlagProperty UiProperty field.
        public sealed class Plain : Cerneala.UI.Controls.TextBlock
        {
            public string Caption { get; set; } = string.Empty;
            public bool Flag { get; set; }
        }

        public sealed class PrivateGetVM : INotifyPropertyChanged
        {
            public event PropertyChangedEventHandler? PropertyChanged;
            public string Name { private get; set; } = string.Empty;
        }
        """;

    [Theory]
    [InlineData("<StackPanel><ItemsControl Name=\"L\" /><TextBlock Text=\"$L.ItemCount\" /></StackPanel>", "CERNEALAUI007")]
    [InlineData("<ListBoxItem DataType=\"Demo.VM\" ItemIndex=\"$DataContext.Idx\" />", "CERNEALAUI007")]
    [InlineData("<d:Plain xmlns:d=\"clr-namespace:Demo\" DataType=\"Demo.VM\" Caption=\"Hello $DataContext.Name!\" />", "CERNEALAUI007")]
    [InlineData("<d:Plain xmlns:d=\"clr-namespace:Demo\" DataType=\"Demo.VM\" Flag=\"$DataContext.A and $DataContext.B\" />", "CERNEALAUI007")]
    [InlineData("<d:Plain xmlns:d=\"clr-namespace:Demo\" Caption=\"Hello\" Flag=\"true\" />", "")]
    public void BindingsToNonUiPropertiesAreReportedLikeTheBuild(string markup, string generatorIds) =>
        AssertParity(markup, generatorIds);

    [Theory]
    [InlineData("<TextBlock DataType=\"Demo.VM\" Text=\"$DataContext.$Name\" />", "CERNEALAUI007")]
    public void MalformedBindingPathsAreReportedLikeTheBuild(string markup, string generatorIds) =>
        AssertParity(markup, generatorIds);

    [Theory]
    [InlineData("<TextBlock DataType=\"Demo.VM\" Text=\"$DataContext.Version\" />", "CERNEALAUI007")]
    [InlineData("<TextBlock DataType=\"Demo.PrivateGetVM\" Text=\"$DataContext.Name\" />", "CERNEALAUI007")]
    public void StaticAndWriteOnlyViewModelMembersAreReportedLikeTheBuild(string markup, string generatorIds) =>
        AssertParity(markup, generatorIds);

    [Theory]
    [InlineData("<TextBlock DataType=\"Demo.VM\" Text=\"Hello $DataContext.Name!\" />", "")]
    [InlineData("<TextBlock DataType=\"Demo.VM\" Text=\"$DataContext.Name\" />", "")]
    [InlineData("<StackPanel><TextBox Name=\"L\" /><TextBlock Text=\"$L.Text\" /></StackPanel>", "")]
    public void InterpolatedStringsAreAcceptedLikeTheBuild(string markup, string generatorIds) =>
        AssertParity(markup, generatorIds);

    private static void AssertParity(string markup, string generatorIds)
    {
        LanguagePipelineResult result = LanguagePipelineHarness.Analyze("Parity.crn", markup, "Vm.cs", ViewModelSource);

        string[] generator = result.SourceGeneratorDiagnostics.Select(diagnostic => diagnostic.Id).ToArray();
        string[] editor = result.SemanticDiagnostics.Select(diagnostic => diagnostic.Id).ToArray();
        string[] expected = generatorIds.Length == 0 ? [] : generatorIds.Split(',');
        string messages = string.Join(" | ", result.SourceGeneratorDiagnostics.Select(diagnostic => diagnostic.Id + ": " + diagnostic.Message));
        string detail = "generator=[" + string.Join(",", generator) + "] editor=[" + string.Join(",", editor) +
            "] messages=[" + messages + "] markup=" + markup;

        Assert.True(generator.SequenceEqual(expected), "Premise failed: " + detail);
        Assert.True(editor.SequenceEqual(generator), "Parity failed: " + detail);
    }
}
