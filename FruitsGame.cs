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
using System;

namespace FruitsGame;

public record struct RenderContext(GraphicsDevice GraphicsDevice, SpriteBatch SpriteBatch, ShapeBatch ShapeBatch, ContentManager Content);

public class FruitsGame : Game
{
    private GraphicsDeviceManager _graphics;
    private SpriteBatch _spriteBatch;
    private ShapeBatch _shapeBatch;
    private RenderContext _renderContext;
    private ShapeFont _font;
    private int framescount = 0;

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
        _graphics = new GraphicsDeviceManager(this)
        {
            PreferredBackBufferWidth = 1280,
            PreferredBackBufferHeight = 720,
            GraphicsProfile = GraphicsProfile.HiDef
        };
        _graphics.ApplyChanges();

        Content.RootDirectory = "Content";
        IsMouseVisible = true;
        IsFixedTimeStep = true;
        _graphics.HardwareModeSwitch = false;
        Window.AllowUserResizing = true;
    }
    protected override void Initialize()
    {
        // display

        base.Initialize();
    }

    protected override void LoadContent()
    {
        // Batch
        _shapeBatch = new ShapeBatch(GraphicsDevice);
        _shapeBatch.Begin();
        _shapeBatch.FillRectangle(Vector2.One, Vector2.One, Color.Red);
        _shapeBatch.End();

        _spriteBatch = new SpriteBatch(GraphicsDevice);
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
        MediaPlayer.Volume = 0.4f * 0;
        MediaPlayer.Play(BGMusic);
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

        framescount++;

        base.Update(gameTime);
    }

    protected override void Draw(GameTime gameTime)
    {
        Console.WriteLine(framescount);

        // get current frame
        var frame = _fruitsContainer.GetFrame();

        // background        
        GraphicsDevice.Viewport = new Viewport(GraphicsDevice.PresentationParameters.Bounds);
        _viewportAdapter.Reset();

        GraphicsDevice.Clear(new Color(25, 30, 48));

        // TODO: Add your drawing code here
        _spriteBatch.Begin(samplerState: SamplerState.AnisotropicWrap, transformMatrix: _camera.GetViewMatrix());

        _shapeBatch.Begin(_camera.GetViewMatrix());


        _spriteBatch.Draw(
            frame,
            _fruitsContainer.Rectangle,
            Color.White
        );

        // draw guide
        DrawGuide(new Vector2(_fruitsContainer.Rectangle.Left - Separation, _fruitsContainer.VerticalPadding + Separation));

        // draw next
        int sizeOffet = 4;
        Vector2 nextPos = new(_fruitsContainer.Rectangle.Right + Separation * 3, _fruitsContainer.VerticalPadding + Separation * 3);
        _fruitsContainer.DrawFruit(
            nextPos,
            80 * (_fruitsContainer.NextFruit + sizeOffet) / (FruitsContainer.MaxFruits + sizeOffet),
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

    public const int Separation = 40;
    public const int TriangleSize = 10;
    public const int RadiusOffset = 8;
    public const int RadiusGuide = 35;
    public int CustomRadius(int i) => RadiusGuide * (i + RadiusOffset) / (FruitsContainer.MaxFruits + RadiusOffset);

    public void DrawGuide(Vector2 pos)
    {
        _shapeBatch.BeginFillPath(5, Color.White);
        _shapeBatch.PathTo(pos);

        float acumulator = Separation;
        for (int i = 0; i < FruitsContainer.MaxFruits; i++)
        {
            acumulator += CustomRadius(i) * 2.5f;
        }
        acumulator += Separation / 2;
        Vector2 lastPoint = pos + Vector2.UnitY * acumulator;
        _shapeBatch.PathTo(lastPoint);
        _shapeBatch.EndPath();

        _shapeBatch.FillTriangle(
            lastPoint - Vector2.UnitX * TriangleSize,
            lastPoint + Vector2.UnitX * TriangleSize,
            lastPoint + Vector2.UnitY * TriangleSize * 2,
            Color.White
        );

        float verticalOffset = pos.Y;
        for (int i = 0; i < FruitsContainer.MaxFruits; i++)
        {
            int radius = CustomRadius(i);
            verticalOffset += radius * 2.5f;

            _fruitsContainer.DrawFruit(
                new Vector2(_fruitsContainer.Rectangle.Left - Separation, verticalOffset),
                radius,
                i,
                0
            );
        }
    }
}
