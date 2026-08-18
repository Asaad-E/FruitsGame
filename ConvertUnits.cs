using Microsoft.Xna.Framework;

public static class ConvertUnits
{
    public static float MeterToPixel = 100f;

    public static float PixelToMeter = 1f / MeterToPixel;

    public static Vector2 ToDisplay(Vector2 vector)
    {
        return vector * MeterToPixel;
    }

    public static float ToDisplay(float meter)
    {
        return meter * MeterToPixel;
    }
    public static Vector2 ToSim(Vector2 vector)
    {
        return vector * PixelToMeter;
    }

    public static float ToSim(float pixel)
    {
        return pixel * PixelToMeter;
    }

}