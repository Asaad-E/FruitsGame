using System;
using Microsoft.Xna.Framework;
using nkast.Aether.Physics2D.Dynamics;

namespace FruitsGame;

public class Fruit
{
    // shape
    public int Radius;
    public Point Size;

    // Fruit value
    public int Value;
    public bool Deleted = false;

    // Physical body
    public Body Body;
    public Point Position => ConvertUnits.ToDisplay(Body.Position).ToPoint();

    public Fruit(int value)
    {
        Value = value;

        Radius = GetRadiusFromValue(value);
        Size = new Point(Radius * 2, Radius * 2);
    }

    public static int GetRadiusFromValue(int value)
    {
        return (int)MathF.Pow(value + 3, 1.4f) * 5 + 13;
    }

}