using System;
using System.Collections.Generic;
using Apos.Shapes;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Graphics;
using nkast.Aether.Physics2D.Common;
using nkast.Aether.Physics2D.Dynamics;
using nkast.Aether.Physics2D.Dynamics.Contacts;

namespace FruitsGame.Core;

/// <summary>
/// The container of the game. Make to be independent of other game elements.
/// 
/// </summary>
public class FruitsContainer
{
    // representation in the screen
    public Rectangle Rectangle;
    public float BoxWidth;
    public float BoxHeight;
    public int HorizontalPadding;
    public int VerticalPadding;

    // drawing
    public RenderTarget2D _frame;
    private RenderContext _renderContext;

    // Physic simulation
    private readonly World _world;
    private const float _globalRestitution = 0.3f;
    private const float _globalFriction = 0.9f;

    private List<Fruit> _deleteQueue;
    private List<Action> _creationQueue;

    // wall
    private Body[] _walls;
    private const int _WallThick = 60;
    public Rectangle[] _wallRectangles = new Rectangle[3];

    // Player
    public Player Player;
    public int Points = 0;
    public int CurrentFruit;
    public int NextFruit;
    public int MinFruitRange = 0;
    public int MaxFruitsRange = 3;

    // Fruits
    public List<Fruit> Fruits;
    public const int MaxFruits = 11;

    public int[] PrecomputedRadius;

    public SoundEffect[] FusionSonds = new SoundEffect[MaxFruits];
    public SoundEffect DropSound;

    public static readonly Color[] InnerColors =
    [
        new Color(255, 120, 140), // 1. Cherry
        new Color(255, 190, 210), // 2. Strawberry
        new Color(130, 180, 255), // 3. Grape
        new Color(255, 210, 100), // 4. Dekopon
        new Color(255, 180,  80), // 5. Persimmon
        new Color(230, 100, 210), // 6. Apple
        new Color(255, 255, 180), // 7. Pear
        new Color(255, 220, 225), // 8. Peach
        new Color(255, 240, 130), // 9. Pineapple
        new Color(156, 219, 200), // 10. Melon
        new Color( 90, 230,  90)  // 11. Watermelon
    ];

    // Outer edge base colors
    public static readonly Color[] OuterColors =
    [
        new Color(215,  20,  45), // 1. Cherry
        new Color(255,  80, 110), // 2. Strawberry
        new Color( 30,  90, 220), // 3. Grape
        new Color(245, 130,  15), // 4. Dekopon
        new Color(235,  80,  15), // 5. Persimmon
        new Color(160,  30, 130), // 6. Apple
        new Color(215, 200,  70), // 7. Pear
        new Color(250, 150, 165), // 8. Peach
        new Color(245, 175,  15), // 9. Pineapple
        new Color( 12, 100, 123), // 10. Melon
        new Color( 10, 120,  40)  // 11. Watermelon
    ];

    public FruitsContainer(Rectangle rect, int horizontalPadding, int verticalPadding, RenderContext renderContext)
    {
        // drawing
        _renderContext = renderContext;

        // Create container of the game
        Rectangle = rect;

        HorizontalPadding = Math.Max(horizontalPadding, 5);
        VerticalPadding = verticalPadding;

        BoxWidth = rect.Width - HorizontalPadding * 2;
        BoxHeight = rect.Height - verticalPadding;

        // Radius
        PrecomputedRadius = new int[MaxFruits];
        for (int i = 0; i < MaxFruits; i++)
        {
            PrecomputedRadius[i] = Fruit.GetRadiusFromValue(i);
        }

        // initialize physic engine
        _world = new(new Vector2(0, 9.8f));
        _world.ContactManager.BeginContact += OnFruitCollision;

        _deleteQueue = [];
        _creationQueue = [];
        Fruits = [];

        CreateBoxBorders();

        // load player
        Player = new(
            postion: new Vector2(Rectangle.Width / 2, verticalPadding / 2),
            _renderContext.Content
        );

        // load sound effects

        for (int i = 0; i < MaxFruits; i++)
        {
            FusionSonds[i] = _renderContext.Content.Load<SoundEffect>($"SFX/pop{i}");
        }

        DropSound = _renderContext.Content.Load<SoundEffect>("SFX/click");

        // load textures
        _frame = new RenderTarget2D(_renderContext.GraphicsDevice, rect.Width, rect.Height, false, SurfaceFormat.Color, DepthFormat.None, 4, RenderTargetUsage.DiscardContents);
    
        // crate color
        WallGradient = new(
            new Vector2(0, 0), new Color(96, 165, 250),
            new Vector2(0, Rectangle.Bottom), new Color(160, 100, 250)
        );
    }

    public void CreateBoxBorders()
    {
        // walls
        _walls = new Body[3];

        // left, rigth, floor,  
        _wallRectangles[0] = new Rectangle((int)HorizontalPadding, (int)VerticalPadding, _WallThick, (int)BoxHeight);
        _wallRectangles[1] = new Rectangle((int)HorizontalPadding, (int)Rectangle.Height - _WallThick, (int)BoxWidth, _WallThick);
        _wallRectangles[2] = new Rectangle(Rectangle.Width - _WallThick - (int)HorizontalPadding, (int)VerticalPadding, _WallThick, (int)BoxHeight);

        for (int i = 0; i < 3; i++)
        {
            _walls[i] = _world.CreateRectangle(
                ConvertUnits.ToSim(_wallRectangles[i].Width),
                ConvertUnits.ToSim(_wallRectangles[i].Height),
                1f,
                ConvertUnits.ToSim(
                    _wallRectangles[i].Center.ToVector2()
                ),
                0,
                BodyType.Static
                );
        }

        foreach (Body wall in _walls)
        {
            wall.FixtureList[0].Restitution = _globalRestitution;
            wall.FixtureList[0].Friction = _globalFriction;
        }

    }

    public Fruit CreateFruit(Vector2 pos, int value = 0)
    {
        // Create fruit and its physical body
        Fruit newFruit = new(value);
        Body fruitBody = _world.CreateCircle(
            ConvertUnits.ToSim(newFruit.Radius + 0.5f),
            1f,
            ConvertUnits.ToSim(pos),
            BodyType.Dynamic
            );

        // make the link betweetthe two
        newFruit.Body = fruitBody;
        fruitBody.Tag = newFruit;

        // Set fixture parameters
        fruitBody.FixtureList[0].Restitution = _globalRestitution;
        fruitBody.FixtureList[0].Friction = _globalFriction;
        fruitBody.Rotation = Random.Shared.NextSingle() * MathF.Tau;
        fruitBody.AngularVelocity = Random.Shared.NextSingle() * MathF.Tau * 0.2f;

        Fruits.Add(newFruit);

        return newFruit;
    }

    public void Initialize()
    {
        CurrentFruit = 0;
        NextFruit = GetRandomFruit();
        MaxFruitsRange = 3;
        Points = 0;
        Player.Position.X = Rectangle.Width / 2;

        for (int i = 0; i < Fruits.Count; i++)
        {
            _world.Remove(Fruits[i].Body);
        }

        _creationQueue.Clear();
        _deleteQueue.Clear();
        Fruits.Clear();
    }

    public int GetRandomFruit()
    {
        int a = Random.Shared.Next(MinFruitRange, MaxFruitsRange);
        int b = Random.Shared.Next(MinFruitRange, MaxFruitsRange);
        return Math.Min(a, b);
    }

    public void DropFruit()
    {
        // Check player timer
        if (Player.DropTimer != 0) return;

        // play sound
        DropSound.Play(0.4f, 0, 0);

        // Drop the fruit
        Fruit newFruit = CreateFruit(Player.PointOfRelease.ToVector2(), CurrentFruit);
        newFruit.Body.Rotation = 0;
        newFruit.Body.AngularVelocity = Random.Shared.NextSingle() * MathF.Tau * 0.35f;

        // Calculate next fruit
        CurrentFruit = NextFruit;

        NextFruit = GetRandomFruit();

        Player.SetCooldown();
    }

    public void Update(float deltaTime)
    {
        // perform deletion of bodies 
        if (_deleteQueue.Count > 0)
        {
            foreach (Fruit fruit in _deleteQueue)
            {
                try
                {
                    Fruits.Remove(fruit);
                    _world.Remove(fruit.Body);
                }
                catch { }
            }
            _deleteQueue.Clear();
        }

        // perform creation of new fruits
        if (_creationQueue.Count > 0)
        {
            foreach (Action func in _creationQueue)
            {
                func.Invoke();
            }
            _creationQueue.Clear();
        }

        // update player
        Player.Update(deltaTime);

        // update world
        _world.Step(deltaTime);

        // CHECK IF OVERFLOW

        for (int i = 0; i < Fruits.Count; i++)
        {
            if (Fruits[i].Position.Y + Fruits[i].Radius < VerticalPadding)
            {
                Initialize();
                break;
            }
        }
    }

    public RenderTarget2D GetFrame()
    {
        _renderContext.GraphicsDevice.SetRenderTarget(_frame);
        _renderContext.GraphicsDevice.Clear(Color.Transparent);

        // Player
        _renderContext.SpriteBatch.Begin();
        _renderContext.SpriteBatch.Draw(
            Player.Texture,
            Player.Rectangle,
            null,
            Color.White,
            0,
            Player.Origin,
            SpriteEffects.None,
            0
        );
        _renderContext.SpriteBatch.End();

        _renderContext.ShapeBatch.Begin();

        // Draw a dummy of the next fruit on top on the player
        DrawFruit(Player.PointOfRelease.ToVector2(), PrecomputedRadius[CurrentFruit], CurrentFruit);

        // fruits
        foreach (var fruit in Fruits)
        {
            DrawFruit(fruit.Position.ToVector2(), fruit.Radius, fruit.Value, fruit.Body.Rotation);
        }

        // walls
        DrawWalls();

        // Close batch
        _renderContext.ShapeBatch.End();
        
        // Change the target to the screen
        _renderContext.GraphicsDevice.SetRenderTarget(null);

        return _frame;
    }

    public const int WallMargin = 10;
    public const int WallBorderSize = 2;
    public Gradient WallGradient;
    public void DrawWalls()
    {
        // Limit line
        _renderContext.ShapeBatch.FillLine(
            new Vector2(_wallRectangles[0].Right, _wallRectangles[0].Top + WallMargin),
            new Vector2(_wallRectangles[2].Left, _wallRectangles[2].Top + WallMargin),
            4,
            new Color(245, 245, 245, 150),
            dash: new DashStyle(15, 10, 0.5f)
        );

        // Walls
        for (int i = 0; i < 3; i++)
        {
            _renderContext.ShapeBatch.FillRectangle(
                _wallRectangles[i].Location.ToVector2(),
                _wallRectangles[i].Size.ToVector2(),
                WallGradient,
                10f
            );
        }

        // Wall border
        _renderContext.ShapeBatch.FillPath(
            [
                _wallRectangles[0].Location.ToVector2(),
                _wallRectangles[0].Location.ToVector2() + Vector2.UnitX * _wallRectangles[0].Width,
                _wallRectangles[1].Location.ToVector2() + Vector2.UnitX * _wallRectangles[0].Width,
                _wallRectangles[1].Location.ToVector2() + Vector2.UnitX * _wallRectangles[1].Width - Vector2.UnitX * _wallRectangles[2].Width,
                _wallRectangles[2].Location.ToVector2(),
                _wallRectangles[2].Location.ToVector2() + Vector2.UnitX * _wallRectangles[2].Width,
                new Vector2(_wallRectangles[2].Right, _wallRectangles[2].Bottom),
                new Vector2(_wallRectangles[0].Left, _wallRectangles[0].Bottom)
            ],
            WallBorderSize,
            new Color(235, 240, 248),
            PathJoin.Round,
            PathCap.Round,
            null,
            closed: true
        );
    }

    public void DrawFruit(Vector2 pos, int radius, int value, float rotation = 0)
    {
        _renderContext.ShapeBatch.DrawCircle(
                pos,
                radius,
                new Gradient(
                    new Vector2(-radius, -radius), InnerColors[value],
                    new Vector2(radius, radius), OuterColors[value],
                    isLocal: true,
                    s: Gradient.Shape.Linear,
                    bOffset: radius * 0.2f
                ),
                new Color(235, 240, 248),
                thickness: radius * 0.05f,
                rotation: rotation
            );
    }

    public bool OnFruitCollision(Contact contact)
    {
        Body bodyA = contact.FixtureA.Body;
        Body bodyB = contact.FixtureB.Body;

        if (!(bodyA.Tag is Fruit && bodyB.Tag is Fruit)) return true;

        Fruit fruitA = bodyA.Tag as Fruit;
        Fruit fruitB = bodyB.Tag as Fruit;

        if (fruitA.Value == fruitB.Value && fruitA.Deleted == false && fruitB.Deleted == false)
        {
            Console.WriteLine(fruitA.Value);

            fruitA.Deleted = true;
            fruitA.Deleted = true;

            _deleteQueue.Add(fruitA);
            _deleteQueue.Add(fruitB);

            Vector2 velAvg = (bodyA.LinearVelocity + bodyB.LinearVelocity) * 0.5f;
            float rotationAvg = (bodyA.Rotation + bodyB.Rotation) * 0.5f;
            float angularVelocityAvg = (bodyA.AngularVelocity + bodyB.AngularVelocity) * 0.5f;

            contact.GetWorldManifold(out Vector2 normal, out FixedArray2<Vector2> points);

            int newValue = Math.Clamp(fruitA.Value + 1, 0, MaxFruits - 1);

            Points += (fruitA.Value + 2) * (fruitA.Value + 1) / 2;

            // play sound
            FusionSonds[fruitA.Value].Play();

            // Do not create a new fruit when you get to the last one
            if (fruitA.Value == MaxFruits - 1) return false;

            if (fruitA.Value > MaxFruitsRange)
            {
                MaxFruitsRange = Math.Min(fruitA.Value + 1, MaxFruits - 4);
            }

            // create a new fruit with one value more
            _creationQueue.Add(
                () =>
                {
                    Fruit fruit = CreateFruit(
                        ConvertUnits.ToDisplay(points[0]),
                        newValue
                    );

                    fruit.Body.LinearVelocity = velAvg;
                    fruit.Body.Rotation = rotationAvg;
                    fruit.Body.AngularVelocity = angularVelocityAvg;
                    fruit.Body.FixtureList[0].Shape.Density = (fruitA.Value + 1) * 0.3f;
                }
            );

            return false;
        }


        return true;
    }

    public void MovePlayer(int direction)
    {
        Player.Direction += direction;

        int margin = -5;
        if (Player.Position.X - Player.Size.X / 2 + margin < HorizontalPadding)
        {
            Player.Position.X = Player.Size.X / 2 + HorizontalPadding - margin;
        }
        if (Player.Position.X + Player.Size.X / 2 - margin > BoxWidth + HorizontalPadding)
        {
            Player.Position.X = BoxWidth + HorizontalPadding - Player.Size.X / 2 + margin;
        }
    }
}
