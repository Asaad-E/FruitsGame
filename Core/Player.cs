using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;

public class Player
{
    public Vector2 Position; // Center of the sprite
    public Point PointOfRelease => Position.ToPoint() + new Point(0, Size.Y / 2);
    public Texture2D Texture;
    public Vector2 Origin;
    public int Width = 150; // Real size of the sprite
    public Point Size;
    public Rectangle Rectangle => new(Position.ToPoint(), Size);

    public const float DropCoolDown = 0.5f;
    public float DropTimer = 0;

    // move
    public float Speed = 350;
    public int Direction = 0; // Direction of movvemnt that frame, after move reset to 0

    public Player(Vector2 position, ContentManager content)
    {
        Position = position;
        Texture = content.Load<Texture2D>("Images/cloud");

        Origin = new(Texture.Width / 2, Texture.Height / 2);

        // Calculate the heigth based on the texture aspect ratio
        Size = (new Vector2(Width, (int)(Width * Texture.Height / (float)Texture.Width)) * 1.6f).ToPoint();
    }

    public void Update(float deltaTime)
    {
        Position.X += Direction * Speed * deltaTime;
        Direction = 0;


        if (DropTimer > 0)
        {
            DropTimer -= deltaTime;
        }

        if (DropTimer < 0)
        {
            DropTimer = 0;
        }
    }

    public void SetCooldown()
    {
        DropTimer = DropCoolDown;
    }

}