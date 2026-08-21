using System;
using System.Linq;
using System.Reflection;
using Cerneala.UI.Controls;
using Cerneala.UI.Elements;
using Microsoft.CodeAnalysis;
using Xunit;

namespace Cerneala.Tests.SourceGen;

public sealed partial class UiMarkupGeneratorTests
{
    [Fact]
    public void UiMarkupGeneratorMenuTests_NestedMenusBindingsAndItemsSourcesCompileAndRun()
    {
        const string inputSource = """
            using System.Collections;
            using System.ComponentModel;
            using Cerneala.UI.Input;

            namespace TestInput;

            public sealed class MenuViewModel : INotifyPropertyChanged
            {
                public event PropertyChangedEventHandler? PropertyChanged;
                public object? Header { get; } = "File";
                public ICommand Command { get; } = new ActionCommand(_ => { });
                public object? Parameter { get; } = "document.crn";
                public IEnumerable? Rows { get; } = new[]
                {
                    new MenuRow { Label = "One" },
                    new MenuRow { Label = "Two" }
                };
            }

            public sealed class MenuRow
            {
                public string Label { get; set; } = string.Empty;
            }
            """;
        const string markup = """
            <StackPanel DataType="TestInput.MenuViewModel">
              <MenuBar>
                <MenuItem Header="$DataContext.Header:OneWay">
                  <MenuItem Header="Recent">
                    <MenuItem
                        Header="Open"
                        Command="$DataContext.Command:OneWay"
                        CommandParameter="$DataContext.Parameter:OneWay" />
                  </MenuItem>
                </MenuItem>
              </MenuBar>
              <Menu>
                <MenuItem Header="Tools" />
              </Menu>
              <Menu DisplayMemberPath="Label" ItemsSource="$DataContext.Rows:OneWay" />
            </StackPanel>
            """;

        GeneratorRunResult result = RunGeneratorWithInput(
            "MenuSurface.crn",
            markup,
            inputSource,
            out Compilation compilation);

        Assert.DoesNotContain(result.Diagnostics, diagnostic => diagnostic.Severity == DiagnosticSeverity.Error);
        Assert.DoesNotContain(compilation.GetDiagnostics(), diagnostic => diagnostic.Id == "CS8620");
        Assembly assembly = EmitBindingTestAssembly(compilation);
        Type viewModelType = assembly.GetType("TestInput.MenuViewModel", throwOnError: true)!;
        object viewModel = Activator.CreateInstance(viewModelType)!;
        StackPanel panel = Assert.IsType<StackPanel>(InvokeBindingTestCreate(
            assembly,
            "Cerneala.GeneratedUi.MenuSurfaceFactory",
            viewModel));
        UIRoot root = new(360, 240);
        root.VisualChildren.Add(panel);
        root.ProcessFrame();

        MenuBar menuBar = Assert.IsType<MenuBar>(panel.VisualChildren[0]);
        MenuItem file = Assert.IsType<MenuItem>(Assert.Single(menuBar.Items));
        MenuItem recent = Assert.IsType<MenuItem>(Assert.Single(file.Items));
        MenuItem open = Assert.IsType<MenuItem>(Assert.Single(recent.Items));
        file.IsSubmenuOpen = true;
        root.ProcessFrame();
        recent.IsSubmenuOpen = true;
        root.ProcessFrame();
        Assert.Equal("File", file.Header);
        Assert.Equal("Recent", recent.Header);
        Assert.Equal("Open", open.Header);
        Assert.Same(viewModelType.GetProperty("Command")!.GetValue(viewModel), open.Command);
        Assert.Equal("document.crn", open.CommandParameter);
        Assert.DoesNotContain(recent, file.VisualChildren);
        Assert.DoesNotContain(open, recent.VisualChildren);

        Menu directMenu = Assert.IsType<Menu>(panel.VisualChildren[1]);
        MenuItem tools = Assert.IsType<MenuItem>(Assert.Single(directMenu.Items));
        Assert.Equal("Tools", tools.Header);
        Assert.DoesNotContain(tools, directMenu.VisualChildren);

        Menu dataMenu = Assert.IsType<Menu>(panel.VisualChildren[2]);
        Assert.Equal(2, dataMenu.ItemCount);
        Assert.Same(viewModelType.GetProperty("Rows")!.GetValue(viewModel), dataMenu.ItemsSource);
        Assert.Equal(
            new[] { "One", "Two" },
            Enumerable.Range(0, dataMenu.ItemCount)
                .Select(index => dataMenu.GetItemAt(index)!.GetType().GetProperty("Label")!.GetValue(dataMenu.GetItemAt(index)))
                .ToArray());
    }
}
