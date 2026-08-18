using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;

public class Player
{
    public Vector2 Position; // Center of the sprite
    public Point PointOfRelease => Position.ToPoint() + new Point(0, Size.Y/2);
    public Texture2D Texture;
    public Vector2 Origin;
    public int Width = 150; // Real size of the sprite
    public Point Size;
    public Rectangle Rectangle => new(Position.ToPoint(), Size);

    // move
    public float Speed = 300;
    public int Direction = 0; // Direction of movvemnt that frame, after move reset to 0

    public Player(Vector2 postion, ContentManager content)
    {
        Position = postion;
        Texture = content.Load<Texture2D>("Images/cloud");

        Origin = new(Texture.Width / 2, Texture.Height / 2);

        // Calculate the heigth based on the texture aspect ratio
        Size = new(Width, (int)(Width * Texture.Height / (float)Texture.Width));
    }

    public void Update(float deltaTime)
    {
        Position.X += Direction * Speed * deltaTime;
        Direction = 0;
    }

}