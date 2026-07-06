#nullable enable

using System;
using System.IO;
using Cerneala.Drawing.MonoGame;
using Cerneala.Playground.Samples;
using Cerneala.UI.Elements;
using Cerneala.UI.Hosting;
using Cerneala.UI.Hosting.MonoGame;
using Cerneala.UI.Input;
using Cerneala.UI.Resources;
using Cerneala.UI.Styling;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace Cerneala.Playground;

public class Game1 : Game
{
    private static readonly ResourceId<FontResource> PlaygroundFontId = new("Playground/Body");
    private static readonly ResourceId<ImageResource> PlaygroundPreviewImageId = new("Playground/PreviewImage");

    private GraphicsDeviceManager _graphics;
    private ResourceStore? _resources;
    private MonoGameUiHost? _uiHost;
    private SampleSelector? _sampleSelector;
    private SpriteBatch? _spriteBatch;
    private Texture2D? _whitePixel;
    private readonly bool _exitAfterFirstSuccessfulDraw;
    private bool _smokeDrawCompleted;

    public Game1(bool exitAfterFirstSuccessfulDraw = false)
    {
        _exitAfterFirstSuccessfulDraw = exitAfterFirstSuccessfulDraw;
        _graphics = new GraphicsDeviceManager(this);
        Content.RootDirectory = "Content";
        IsMouseVisible = true;
    }

    protected override void Initialize()
    {
        base.Initialize();
    }

    protected override void LoadContent()
    {
        _spriteBatch = new SpriteBatch(GraphicsDevice);
        _whitePixel = new Texture2D(GraphicsDevice, 1, 1);
        _whitePixel.SetData(new[] { Color.White });
        UIRoot uiRoot = new(GraphicsDevice.Viewport.Width, GraphicsDevice.Viewport.Height);
        uiRoot.SetThemeProvider(new ThemeProvider(DefaultTheme.Create()));
        uiRoot.SetStyleSheet(DefaultTheme.CreateStyleSheet());
        _uiHost = new MonoGameUiHost(new MonoGameUiHostOptions
        {
            SpriteBatch = _spriteBatch,
            WhitePixel = _whitePixel,
            Root = uiRoot,
            Viewport = GetViewport()
        });

        _resources = new ResourceStore();
        _resources.SetResource(PlaygroundFontId, new FontResource(_uiHost.ContentServices.LoadFont("Arial", 16)));
        _resources.SetResource(PlaygroundPreviewImageId, new ImageResource(Path.Combine(AppContext.BaseDirectory, "Content", "PreviewImage.png")));
        uiRoot.SetResourceProvider(_resources);
        _sampleSelector = SampleSelector.CreateDefault(_resources, PlaygroundFontId, PlaygroundPreviewImageId);
        uiRoot.VisualChildren.Add(_sampleSelector.Root);
        PrimeUiFrameForFirstDraw();
        Window.TextInput += OnTextInput;
    }

    protected override void UnloadContent()
    {
        Window.TextInput -= OnTextInput;
        _sampleSelector = null;
        _resources = null;
        _uiHost?.Dispose();
        _uiHost = null;
        _whitePixel?.Dispose();
        _whitePixel = null;
        _spriteBatch?.Dispose();
        _spriteBatch = null;
        base.UnloadContent();
    }

    protected override void Update(GameTime gameTime)
    {
        if (GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed || Keyboard.GetState().IsKeyDown(Keys.Escape))
            Exit();

        MonoGameUiHost host = RequireUiHost();
        _sampleSelector?.UpdateFrame(host.LastFrame);
        host.Update(GetViewport(), gameTime.ElapsedGameTime);
        base.Update(gameTime);
    }

    protected override void Draw(GameTime gameTime)
    {
        GraphicsDevice.Clear(new Color(248, 250, 252));

        RequireUiHost().Draw();
        if (_exitAfterFirstSuccessfulDraw && !_smokeDrawCompleted)
        {
            _smokeDrawCompleted = true;
            Exit();
        }

        base.Draw(gameTime);
    }

    private UiViewport GetViewport()
    {
        Viewport viewport = GraphicsDevice.Viewport;
        return new UiViewport(viewport.Width, viewport.Height);
    }

    private void OnTextInput(object? sender, TextInputEventArgs e)
    {
        _uiHost?.QueueTextInput(e.Character.ToString());
    }

    private void PrimeUiFrameForFirstDraw()
    {
        RequireUiHost().Update(CreateEmptyInputFrame(), GetViewport(), TimeSpan.Zero);
    }

    private static InputFrame CreateEmptyInputFrame()
    {
        return new InputFrame(
            PointerSnapshot.Empty,
            PointerSnapshot.Empty,
            KeyboardSnapshot.Empty,
            KeyboardSnapshot.Empty,
            []);
    }

    private MonoGameUiHost RequireUiHost()
    {
        return _uiHost ?? throw new InvalidOperationException("Game1 requires LoadContent to create the retained UI host before update or draw.");
    }

}
