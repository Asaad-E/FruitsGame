using System;

namespace FruitsGame;

public static class Programs
{
    [STAThread]
    public static void Main()
    {
        using var game = new FruitsGame();
        game.Run();
    }
}