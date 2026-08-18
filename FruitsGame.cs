using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

using nkast.Aether.Physics2D.Dynamics;
using nkast.Aether.Physics2D.Diagnostics;
using MonoGame.Extended.Input;
using nkast.Aether.Physics2D.Dynamics.Contacts;
using System.Collections.Generic;
using System;
using nkast.Aether.Physics2D.Common;
using FruitsGame.Core;
using MonoGame.Extended.ViewportAdapters;
using MonoGame.Extended;

namespace FruitsGame;

public class FruitsGame : Game
{
    private GraphicsDeviceManager _graphics;
    private SpriteBatch _spriteBatch;

    // screen
    public const int VirtualWidth = 1280;
    public const int VirtualHeight = 720;

    private ViewportAdapter _viewportAdapter;
    private OrthographicCamera _camera;
    private Texture2D _bg;

    // Game
    private FruitsContainer _fruitsContainer;

    public FruitsGame()
    {
        _graphics = new GraphicsDeviceManager(this);
        Content.RootDirectory = "Content";
        IsMouseVisible = true;
        IsFixedTimeStep = true;
        _graphics.HardwareModeSwitch = false;
        Window.AllowUserResizing = false;
    }


    protected override void Initialize()
    {
        // display
        _graphics.PreferredBackBufferWidth = VirtualWidth;
        _graphics.PreferredBackBufferHeight = VirtualHeight;
        _graphics.ApplyChanges();

        base.Initialize();
    }

    protected override void LoadContent()
    {
        _spriteBatch = new SpriteBatch(GraphicsDevice);

        // initialize viewpoer and camera
        _viewportAdapter = new BoxingViewportAdapter(Window, GraphicsDevice, VirtualWidth, VirtualHeight);
        _camera = new(_viewportAdapter);

        // create the fruit container
        _fruitsContainer = new(
            new Rectangle(
                VirtualWidth / 3, 0, VirtualWidth / 3, VirtualHeight
            ),
            horizontalPadding: 0,
            verticalPadding: 0,
            graphicsDevice: GraphicsDevice,
            content: Content);

        // load texture
        _bg = Content.Load<Texture2D>("Images/bg");

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
        var frame = _fruitsContainer.GetFrame(_spriteBatch, GraphicsDevice);

        // background
        GraphicsDevice.Clear(Color.Black);

        GraphicsDevice.Viewport = new Viewport(GraphicsDevice.PresentationParameters.Bounds);
        _spriteBatch.Begin(samplerState: SamplerState.PointWrap);
        _spriteBatch.Draw(
            _bg,
            GraphicsDevice.PresentationParameters.Bounds,
            GraphicsDevice.PresentationParameters.Bounds,
            Color.White);
        _spriteBatch.End();

        _viewportAdapter.Reset();

        // TODO: Add your drawing code here

        _spriteBatch.Begin(samplerState: SamplerState.LinearClamp, transformMatrix: _camera.GetViewMatrix());

        _spriteBatch.Draw(
            frame,
            _fruitsContainer.Rectangle,
            Color.White
        );
        _spriteBatch.End();

        base.Draw(gameTime);
    }
}
