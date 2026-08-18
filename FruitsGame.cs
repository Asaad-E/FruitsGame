using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using MonoGame.Extended.Input;
using FruitsGame.Core;
using MonoGame.Extended.ViewportAdapters;
using MonoGame.Extended;
using Apos.Shapes;
using Microsoft.Xna.Framework.Content;
using FontStashSharp;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Media;

namespace FruitsGame;

public readonly record struct RenderContext(GraphicsDevice GraphicsDevice, SpriteBatch SpriteBatch, ShapeBatch ShapeBatch, ContentManager Content);

public class FruitsGame : Game
{
    private GraphicsDeviceManager _graphics;
    private SpriteBatch _spriteBatch;
    private ShapeBatch _shapeBatch;
    private RenderContext _renderContext;
    private ShapeFont _font;

    // screen
    public const int VirtualWidth = 1920;
    public const int VirtualHeight = 1080;

    private ViewportAdapter _viewportAdapter;
    private OrthographicCamera _camera;

    // Game
    private FruitsContainer _fruitsContainer;
    private Song BGMusic;

    public FruitsGame()
    {
        _graphics = new GraphicsDeviceManager(this);
        Content.RootDirectory = "Content";
        IsMouseVisible = true;
        IsFixedTimeStep = true;
        _graphics.HardwareModeSwitch = false;
        Window.AllowUserResizing = true;
    }
    protected override void Initialize()
    {
        // display
        _graphics.PreferredBackBufferWidth = 1280;
        _graphics.PreferredBackBufferHeight = 720;
        _graphics.GraphicsProfile = GraphicsProfile.HiDef;
        _graphics.ApplyChanges();

        base.Initialize();
    }

    protected override void LoadContent()
    {
        // Batch
        _spriteBatch = new SpriteBatch(GraphicsDevice);
        _shapeBatch = new ShapeBatch(GraphicsDevice);
        _renderContext = new RenderContext(GraphicsDevice, _spriteBatch, _shapeBatch, Content);

        // initialize viewpoer and camera
        _viewportAdapter = new BoxingViewportAdapter(Window, GraphicsDevice, VirtualWidth, VirtualHeight);
        _camera = new(_viewportAdapter);

        // create the game fruit container
        _fruitsContainer = new(
            new Rectangle(
                // position of the  TopLeft
                VirtualWidth / 3, 0,
                // Size
                VirtualWidth / 3, VirtualHeight
            ),
            horizontalPadding: 0,
            verticalPadding: 230,
            renderContext: _renderContext);

        // load texture

        using var ttf = TitleContainer.OpenStream(System.IO.Path.Combine(Content.RootDirectory, "font.ttf"));
        _font = new(ttf);

        BGMusic = Content.Load<Song>("SFX/bg");
        SoundEffect.MasterVolume = 1;
        MediaPlayer.IsRepeating = true;
        MediaPlayer.Volume = 0.4f*0;
        MediaPlayer.Play(BGMusic);

        base.LoadContent();
    }

    protected override void Update(GameTime gameTime)
    {
        // new frame
        float deltaTime = (float)gameTime.ElapsedGameTime.TotalSeconds;
        MouseExtended.Update();
        KeyboardExtended.Update();

        MouseStateExtended mouseState = MouseExtended.GetState();
        KeyboardStateExtended kbState = KeyboardExtended.GetState();

        // exit
        if (kbState.WasKeyPressed(Keys.Escape))
            Exit();

        // Player control
        if (kbState.IsKeyDown(Keys.A))
        {
            _fruitsContainer.MovePlayer(-1);
        }
        if (kbState.IsKeyDown(Keys.D))
        {
            _fruitsContainer.MovePlayer(1);

        }
        if (kbState.WasKeyPressed(Keys.Space))
        {
            _fruitsContainer.DropFruit();
        }

        _fruitsContainer.Update(deltaTime);

        base.Update(gameTime);
    }

    protected override void Draw(GameTime gameTime)
    {
        // get current frame
        var frame = _fruitsContainer.GetFrame(_spriteBatch, _shapeBatch, GraphicsDevice);

        // background        
        GraphicsDevice.Viewport = new Viewport(GraphicsDevice.PresentationParameters.Bounds);
        _viewportAdapter.Reset();

        GraphicsDevice.Clear(new Color(25, 30, 48));

        // TODO: Add your drawing code here

        _spriteBatch.Begin(samplerState: SamplerState.AnisotropicWrap, transformMatrix: _camera.GetViewMatrix());

        _spriteBatch.Draw(
            frame,
            _fruitsContainer.Rectangle,
            Color.White
        );

        // draw guide

        int separation = 40;
        float verticalOffset = _fruitsContainer.VerticalPadding + 50;
        _shapeBatch.Begin(_camera.GetViewMatrix());


        _shapeBatch.BeginFillPath(5, Color.White);
        _shapeBatch.PathTo(new Vector2(_fruitsContainer.Rectangle.Left - separation, verticalOffset));
        int triangleSize = 10;
        Vector2 lastPoint = new(_fruitsContainer.Rectangle.Left - separation, verticalOffset + 630);
        _shapeBatch.PathTo(lastPoint);
        _shapeBatch.EndPath();

        _shapeBatch.FillTriangle(
            lastPoint - Vector2.UnitX * triangleSize,
            lastPoint + Vector2.UnitX * triangleSize,
            lastPoint + Vector2.UnitY * triangleSize * 2,
            Color.White
        );
        for (int i = 0; i < FruitsContainer.MaxFruits; i++)
        {
            int offset = 10;
            int radius = 30 * (i + offset) / (FruitsContainer.MaxFruits + offset);
            verticalOffset += radius * 2.5f;

            _fruitsContainer.DrawFruit(
                new Vector2(_fruitsContainer.Rectangle.Left - separation, verticalOffset),
                radius,
                i,
                0
            );
        }


        // draw next
        int sizeOffet = 4;
        Vector2 nextPos = new Vector2(_fruitsContainer.Rectangle.Right + separation * 3, _fruitsContainer.VerticalPadding + separation * 3);
        _fruitsContainer.DrawFruit(
            nextPos,
            80 * (_fruitsContainer.NextFruit + sizeOffet)/(FruitsContainer.MaxFruits + sizeOffet),
            _fruitsContainer.NextFruit,
            0
        );

        var text = "Next";
        var textSize = 60;
        Vector2 size = _font.MeasureString(text, textSize);
        _shapeBatch.DrawString(_font, "Next", nextPos - new Vector2(size.X / 2, size.Y * 1.8f), textSize, Color.White);

        _shapeBatch.DrawString(_font, $"{_fruitsContainer.Points:D5}", Vector2.One * 30, 80, Color.White);


        _shapeBatch.End();
        _spriteBatch.End();

        base.Draw(gameTime);
    }
}
