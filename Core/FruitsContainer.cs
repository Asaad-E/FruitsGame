using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml;
using System.Xml.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using MonoGame.Extended;
using MonoGame.Extended.ViewportAdapters;
using nkast.Aether.Physics2D.Collision.Shapes;
using nkast.Aether.Physics2D.Common;
using nkast.Aether.Physics2D.Diagnostics;
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
    public RenderTarget2D _frame;
    public GraphicsDevice GraphicsDevice;

    // Physic simulation
    private readonly World _world;
    private const float _globalRestitution = 0.3f;
    private const float _globalFriction = 0.9f;

    private List<Fruit> _deleteQueue;
    private List<Action> _creationQueue;

    // wall
    private Body[] _walls;
    private int _WallThick = 20;
    private Texture2D _wallTexture;
    private Color _wallColor = new(222, 206, 195);
    public Rectangle[] _wallRectangles = new Rectangle[3];

    // Player
    public Player Player;

    public int CurrentFruit;
    public int NextFruit;
    public int MinFruitRange = 0;
    public int MaxFruitsRange = 3;

    // Fruits
    public List<Fruit> Fruits;
    public const int MaxFruits = 11;
    private readonly Texture2D[] _fruitTextures = new Texture2D[MaxFruits];

    public FruitsContainer(Rectangle rect, int horizontalPadding, int verticalPadding, GraphicsDevice graphicsDevice, ContentManager content)
    {
        // Create container of the game
        Rectangle = rect;

        HorizontalPadding = horizontalPadding;
        VerticalPadding = verticalPadding;

        BoxWidth = rect.Width - horizontalPadding * 2;
        BoxHeight = rect.Height - verticalPadding;

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
            content
        );

        // load textures
        _frame = new(graphicsDevice, rect.Width, rect.Height);

        _wallTexture = new Texture2D(graphicsDevice, 1, 1);
        _wallTexture.SetData([Color.White]);


        for (int i = 0; i < MaxFruits; i++)
        {
            _fruitTextures[i] = content.Load<Texture2D>($"Images/Fruits/circle{i}");
        }
    }

    public void CreateBoxBorders()
    {
        // walls
        _walls = new Body[3];

        // floor, rigth, left 
        _wallRectangles[0] = new Rectangle((int)HorizontalPadding, (int)Rectangle.Height - _WallThick, (int)BoxWidth, _WallThick);
        _wallRectangles[1] = new Rectangle(Rectangle.Width - _WallThick - (int)HorizontalPadding, (int)VerticalPadding, _WallThick, (int)BoxHeight);
        _wallRectangles[2] = new Rectangle((int)HorizontalPadding, (int)VerticalPadding, _WallThick, (int)BoxHeight);

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

    public Fruit CreateFruit(Vector2 pos, int value = 1)
    {
        // Create fruit and its physical body
        Fruit newFruit = new(value, _fruitTextures[value]);
        Body fruitBody = _world.CreateCircle(
            ConvertUnits.ToSim(newFruit.Radius),
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
    }

    public int GetRandomFruit()
    {
        return Random.Shared.Next(MinFruitRange, MaxFruitsRange);
    }

    public void DropFruit()
    {
        // Drop the fruit
        Fruit newFruit = CreateFruit(Player.PointOfRelease.ToVector2(), CurrentFruit);
        newFruit.Body.Rotation = 0;
        newFruit.Body.AngularVelocity = Random.Shared.NextSingle() * MathF.Tau * 0.35f;

        // Calculate next fruit
        CurrentFruit = NextFruit;

        NextFruit = GetRandomFruit();

    }

    public void Update(float deltaTime)
    {
        // perform deletion of bodies 
        if (_deleteQueue.Count > 0)
        {
            foreach (Fruit fruit in _deleteQueue)
            {
                Fruits.Remove(fruit);
                _world.Remove(fruit.Body);

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
    }

    public RenderTarget2D GetFrame(SpriteBatch spriteBatch, GraphicsDevice graphicsDevice)
    {
        graphicsDevice.SetRenderTarget(_frame);
        graphicsDevice.Clear(Color.Transparent);
        spriteBatch.Begin();

        // Player
        spriteBatch.Draw(
            Player.Texture,
            Player.Rectangle,
            null,
            Color.White,
            0,
            Player.Origin,
            SpriteEffects.None,
            0
        );

        // Draw a dummy of the next fruit on top on the player
        spriteBatch.Draw(
                    _fruitTextures[CurrentFruit],
                    new Rectangle(
                        Player.PointOfRelease,
                        new Point(
                            Fruit.GetRadiusFromValue(CurrentFruit) * 2,
                            Fruit.GetRadiusFromValue(CurrentFruit) * 2)
                    ),
                    null,
                    Color.White,
                    0,
                    new Vector2(
                            _fruitTextures[CurrentFruit].Width / 2,
                            _fruitTextures[CurrentFruit].Height / 2),
                    SpriteEffects.None,
                    0
                );

        // fruits
        foreach (var fruit in Fruits)
        {
            spriteBatch.Draw(
                fruit.Texture,
                fruit.Rectangle,
                null,
                Color.White,
                fruit.Body.Rotation,
                fruit.Origin,
                SpriteEffects.None,
                1f
            );
        }

        // walls
        for (int i = 0; i < 3; i++)
        {
            spriteBatch.Draw(
            _wallTexture,
            _wallRectangles[i],
            _wallColor);
        }

        spriteBatch.End();
        graphicsDevice.SetRenderTarget(null);
        return _frame;
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
    }
}
