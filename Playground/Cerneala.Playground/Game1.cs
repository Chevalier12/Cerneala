#nullable enable

using System;
using Cerneala.Drawing;
using Cerneala.UI.Elements;
using Cerneala.UI.Hosting;
using Cerneala.UI.Hosting.MonoGame;
using Cerneala.UI.Layout;
using Cerneala.UI.Rendering;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace Cerneala.Playground;

public class Game1 : Game
{
    private const string DemoFontFamily = "sans-serif";

    private GraphicsDeviceManager _graphics;
    private MonoGameUiHost? _uiHost;
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

        IDrawFont font = _uiHost.ContentServices.LoadFont(DemoFontFamily, 28);
        uiRoot.VisualChildren.Add(new PlaygroundDemoElement(font));
        Window.TextInput += OnTextInput;
    }

    protected override void UnloadContent()
    {
        Window.TextInput -= OnTextInput;
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

        RequireUiHost().Update(GetViewport(), gameTime.ElapsedGameTime);
        base.Update(gameTime);
    }

    protected override void Draw(GameTime gameTime)
    {
        GraphicsDevice.Clear(global::Cerneala.GameBootstrap.CreateDefaultClearColor());

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

    private sealed class PlaygroundDemoElement : UIElement
    {
        private readonly IDrawFont font;

        public PlaygroundDemoElement(IDrawFont font)
        {
            this.font = font;
        }

        protected override LayoutSize MeasureCore(MeasureContext context)
        {
            return new LayoutSize(240, 96);
        }

        protected override LayoutRect ArrangeCore(ArrangeContext context)
        {
            return new LayoutRect(32, 32, DesiredSize.Width, DesiredSize.Height);
        }

        protected override void OnRender(RenderContext context)
        {
            context.DrawingContext.FillRectangle(new DrawRect(32, 32, 240, 96), new DrawColor(20, 20, 24, 220));
            context.DrawingContext.DrawRectangle(new DrawRect(32, 32, 240, 96), DrawColor.White, 2);
            context.DrawingContext.DrawText(new DrawTextRun(font, "Hello world!", 28), new DrawPoint(56, 72), DrawColor.White);
        }
    }
}
