#nullable enable

using System;
using Cerneala.Playground.Samples;
using Cerneala.UI.Elements;
using Cerneala.UI.Hosting;
using Cerneala.UI.Hosting.MonoGame;
using Cerneala.UI.Resources;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace Cerneala.Playground;

public class Game1 : Game
{
    private static readonly ResourceId<FontResource> PlaygroundFontId = new("Playground/Body");

    private GraphicsDeviceManager _graphics;
    private ResourceStore? _resources;
    private MonoGameUiHost? _uiHost;
    private SampleSelector? _sampleSelector;
    private SpriteBatch? _spriteBatch;
    private Texture2D? _whitePixel;

    public Game1()
    {
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
        _uiHost = new MonoGameUiHost(new MonoGameUiHostOptions
        {
            SpriteBatch = _spriteBatch,
            WhitePixel = _whitePixel,
            Root = uiRoot,
            Viewport = GetViewport()
        });

        _resources = new ResourceStore();
        _resources.SetResource(PlaygroundFontId, new FontResource(_uiHost.ContentServices.LoadFont("Arial", 16)));
        _sampleSelector = SampleSelector.CreateDefault(_resources, PlaygroundFontId);
        uiRoot.VisualChildren.Add(_sampleSelector.Root);
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

        UiFrame frame = RequireUiHost().Update(GetViewport(), gameTime.ElapsedGameTime);
        _sampleSelector?.UpdateFrame(frame);
        base.Update(gameTime);
    }

    protected override void Draw(GameTime gameTime)
    {
        GraphicsDevice.Clear(new Color(248, 250, 252));

        RequireUiHost().Draw();

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

    private MonoGameUiHost RequireUiHost()
    {
        return _uiHost ?? throw new InvalidOperationException("Game1 requires LoadContent to create the retained UI host before update or draw.");
    }

}
