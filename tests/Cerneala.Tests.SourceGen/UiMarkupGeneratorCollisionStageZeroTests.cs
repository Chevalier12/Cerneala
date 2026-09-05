using System.ComponentModel;
using Cerneala.Drawing;
using Microsoft.CodeAnalysis;

namespace Cerneala.Tests.SourceGen;

public sealed partial class UiMarkupGeneratorTests
{
    [Fact]
    [Trait("CollisionStage", "0")]
    public void ColliderMarkupUsesSceneStructureBindingsAspectAndMotionWithoutPrism()
    {
        const string markup = """
            <RenderSurface2D>
              <RenderSurface2D.Scene>
                <Scene2D>
                  <Scene2D TranslateX="64" TranslateY="32">
                    <Sprite2D />
                    <BoxCollider2D Width="32" Height="8" OffsetY="24" CollisionLayer="2" CollisionMask="4294967295">
                      <BoxCollider2D.Aspect>
                        @on Loaded
                        {
                          @parallel
                          {
                            @set { Enabled = true; IsTrigger = false; CollisionLayer = 2; CollisionMask = 4294967295; }
                            @animate with Tween(100ms)
                            {
                              @to { OffsetX = 2; OffsetY = 24; Width = 30; Height = 8; }
                            }
                          }
                        }
                      </BoxCollider2D.Aspect>
                    </BoxCollider2D>
                    <CircleCollider2D Radius="6" OffsetX="16" OffsetY="16" />
                    <PolygonCollider2D Points="0,0 10,0 12,8 0,8" />
                  </Scene2D>
                </Scene2D>
              </RenderSurface2D.Scene>
            </RenderSurface2D>
            """;

        GeneratorRunResult result = RunGenerator("CollisionScene.crn", markup, out Compilation compilation);

        Assert.DoesNotContain(result.Diagnostics, static diagnostic => diagnostic.Severity == DiagnosticSeverity.Error);
        Assert.DoesNotContain(compilation.GetDiagnostics(), static diagnostic => diagnostic.Severity == DiagnosticSeverity.Error);
    }

    [Fact]
    [Trait("CollisionStage", "0")]
    public void DoorTemplateCanBindVisualAndColliderToTheSameOpenState()
    {
        const string markup = """
            <RenderSurface2D DataType="Cerneala.Tests.SourceGen.CollisionDoorState">
              <RenderSurface2D.Scene>
                <Scene2D>
                  <Scene2D>
                    <Sprite2D IsVisible="$DataContext.IsClosed:OneWay" />
                    <BoxCollider2D Enabled="$DataContext.IsClosed:OneWay" Width="16" Height="4" />
                  </Scene2D>
                </Scene2D>
              </RenderSurface2D.Scene>
            </RenderSurface2D>
            """;

        GeneratorRunResult result = RunGenerator("CollisionDoor.crn", markup, out Compilation compilation);

        Assert.DoesNotContain(result.Diagnostics, static diagnostic => diagnostic.Severity == DiagnosticSeverity.Error);
        Assert.DoesNotContain(compilation.GetDiagnostics(), static diagnostic => diagnostic.Severity == DiagnosticSeverity.Error);
    }

    [Fact]
    [Trait("CollisionStage", "5")]
    public void HouseDoorAndPlayerDocumentationMarkupCompiles()
    {
        const string markup = """
            <RenderSurface2D DataType="Cerneala.Tests.SourceGen.CollisionDoorState"
                             ViewBox="$DataContext.WorldView:OneWay"
                             xmlns:resources="clr-namespace:Cerneala.UI.Resources;assembly=Cerneala">
              <RenderSurface2D.Resources>
                <resources:ImageResource Name="WorldAtlas" Source="Assets/world.png" />
              </RenderSurface2D.Resources>
              <RenderSurface2D.Scene>
                <Scene2D OrderMode="LayerThenY">
                  <Scene2D Name="House" TranslateX="40" TranslateY="16" Layer="1">
                    <Sprite2D SourceResourceId="$WorldAtlas"
                              Destination="$DataContext.HouseDestination:OneWay" />
                    <BoxCollider2D Width="80" Height="8" />
                    <BoxCollider2D Width="8" Height="56" OffsetY="8" />
                    <BoxCollider2D Width="8" Height="56" OffsetX="72" OffsetY="8" />
                    <BoxCollider2D Width="28" Height="8" OffsetY="56" />
                    <BoxCollider2D Width="28" Height="8" OffsetX="52" OffsetY="56" />
                    <Scene2D Name="Door" TranslateX="28" TranslateY="56" Layer="2">
                      <Sprite2D SourceResourceId="$WorldAtlas"
                                Destination="$DataContext.DoorDestination:OneWay" />
                      <BoxCollider2D Width="24"
                                     Height="8"
                                     Enabled="$DataContext.IsClosed:OneWay"
                                     CollisionLayer="2"
                                     CollisionMask="1" />
                    </Scene2D>
                  </Scene2D>
                  <Scene2D Name="Player" TranslateX="72" TranslateY="84" Layer="3">
                    <Sprite2D SourceResourceId="$WorldAtlas"
                              Destination="$DataContext.PlayerDestination:OneWay" />
                    <CircleCollider2D Radius="4"
                                      OffsetX="4"
                                      OffsetY="4"
                                      CollisionLayer="1"
                                      CollisionMask="2" />
                  </Scene2D>
                </Scene2D>
              </RenderSurface2D.Scene>
            </RenderSurface2D>
            """;

        GeneratorRunResult result = RunGenerator("CollisionHouse.crn", markup, out Compilation compilation);

        Assert.DoesNotContain(result.Diagnostics, static diagnostic => diagnostic.Severity == DiagnosticSeverity.Error);
        Assert.DoesNotContain(compilation.GetDiagnostics(), static diagnostic => diagnostic.Severity == DiagnosticSeverity.Error);
    }

    [Fact]
    [Trait("CollisionStage", "5")]
    public void SceneEntityDocumentationMarkupCanBindInheritedMouseEvent()
    {
        const string inputSource = """
            using Cerneala.UI.Controls;
            using Cerneala.UI.Input;
            namespace TestInput.Views;

            public sealed class WorldState
            {
                public bool IsClosed { get; set; } = true;
            }

            public partial class WorldView : UserControl<WorldState>
            {
                private void OnDoorMouseDown(UiElementId sender, RoutedEventArgs args)
                {
                    ViewModel.IsClosed = false;
                    args.Handled = true;
                }
            }
            """;
        const string markup = """
            <UserControl>
              <RenderSurface2D>
                <RenderSurface2D.Scene>
                  <Scene2D>
                    <Scene2D Name="Door" MouseDown="OnDoorMouseDown">
                      <BoxCollider2D Width="24" Height="8" />
                    </Scene2D>
                  </Scene2D>
                </RenderSurface2D.Scene>
              </RenderSurface2D>
            </UserControl>
            """;

        GeneratorRunResult result = RunPairedGenerator(
            "Views/WorldView.crn",
            markup,
            inputSource,
            out Compilation compilation);

        Assert.DoesNotContain(result.Diagnostics, static diagnostic => diagnostic.Severity == DiagnosticSeverity.Error);
        Assert.DoesNotContain(compilation.GetDiagnostics(), static diagnostic => diagnostic.Severity == DiagnosticSeverity.Error);
    }

    [Fact]
    [Trait("CollisionStage", "3")]
    public void PromotedTileOwnsDeclarativeCollidersThroughItsContentProperty()
    {
        const string markup = """
            <RenderSurface2D>
              <RenderSurface2D.Scene>
                <Scene2D>
                  <TileMap2D>
                    <TileLayer2D LayerId="Structures">
                      <TileInstance2D X="2" Y="3" ReplacesImportedColliders="true">
                        <BoxCollider2D Width="16" Height="4" OffsetY="12" CollisionLayer="2" CollisionMask="1" />
                      </TileInstance2D>
                    </TileLayer2D>
                  </TileMap2D>
                </Scene2D>
              </RenderSurface2D.Scene>
            </RenderSurface2D>
            """;

        GeneratorRunResult result = RunGenerator("PromotedTileCollider.crn", markup, out Compilation compilation);

        Assert.DoesNotContain(result.Diagnostics, static diagnostic => diagnostic.Severity == DiagnosticSeverity.Error);
        Assert.DoesNotContain(compilation.GetDiagnostics(), static diagnostic => diagnostic.Severity == DiagnosticSeverity.Error);
    }
}

public sealed class CollisionDoorState : INotifyPropertyChanged
{
    private bool isClosed = true;

    public event PropertyChangedEventHandler? PropertyChanged;

    public DrawRect? WorldView { get; } = new DrawRect(0, 0, 160, 96);

    public DrawRect HouseDestination { get; } = new(0, 0, 80, 64);

    public DrawRect DoorDestination { get; } = new(0, 0, 24, 8);

    public DrawRect PlayerDestination { get; } = new(0, 0, 8, 8);

    public bool IsClosed
    {
        get => isClosed;
        set
        {
            isClosed = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsClosed)));
        }
    }
}
