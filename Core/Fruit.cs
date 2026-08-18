using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using nkast.Aether.Physics2D.Dynamics;

namespace FruitsGame;

public class Fruit
{
    // shape
    public Texture2D Texture;
    public int Radius;
    public Vector2 Origin;
    public Point Size;

    // Fruit value
    public int Value;
    public bool Deleted = false;

    // Physical body
    public Body Body;
    public Point Position => ConvertUnits.ToDisplay(Body.Position).ToPoint();
    public Rectangle Rectangle => new(Position, Size);

    public Fruit(int value, Texture2D texture)
    {
        Value = value;
        Texture = texture;

        Radius = GetRadiusFromValue(value);
        Size = new Point(Radius * 2, Radius * 2);

        Origin = new Vector2(texture.Width / 2, texture.Height / 2);
    }

    public static int GetRadiusFromValue(int value)
    {
        return (int)MathF.Pow(value + 1, 0.5f) * 20;
    }

}