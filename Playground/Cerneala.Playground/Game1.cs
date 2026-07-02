using Cerneala.Drawing;
using Cerneala.Drawing.MonoGame;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace Cerneala.Playground;

public class Game1 : Game
{
    private GraphicsDeviceManager _graphics;
    private SpriteBatch _spriteBatch;
    private DrawCommandList _drawCommands;
    private DrawingContext _drawing;
    private MonoGameDrawingBackend _drawingBackend;
    private Texture2D _whitePixel;

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
        _drawCommands = new DrawCommandList();
        _drawing = new DrawingContext(_drawCommands);
        _drawingBackend = new MonoGameDrawingBackend(_spriteBatch, _whitePixel);
    }

    protected override void Update(GameTime gameTime)
    {
        if (GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed || Keyboard.GetState().IsKeyDown(Keys.Escape))
            Exit();

        base.Update(gameTime);
    }

    protected override void Draw(GameTime gameTime)
    {
        GraphicsDevice.Clear(global::Cerneala.GameBootstrap.CreateDefaultClearColor());

        _drawCommands.Clear();
        _drawing.FillRectangle(new DrawRect(32, 32, 240, 96), new DrawColor(20, 20, 24, 220));
        _drawing.DrawRectangle(new DrawRect(32, 32, 240, 96), DrawColor.White, 2);

        _spriteBatch.Begin();
        _drawingBackend.Render(_drawCommands);
        _spriteBatch.End();

        base.Draw(gameTime);
    }
}
