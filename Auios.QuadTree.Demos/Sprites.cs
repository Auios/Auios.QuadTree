using System.Numerics;
using Raylib_cs;
using static Raylib_cs.Raylib;

namespace Auios.QuadTree.Demos;

/// <summary>A pre-rendered anti-aliased circle. Drawing it as a textured quad is much cheaper than <c>DrawCircleV</c> for thousands of balls.</summary>
internal static class Sprites {
    private const int Size = 64;
    private static Texture2D s_circle;
    private static readonly Rectangle s_source = new(0, 0, Size, Size);

    public static void Load() {
        Image image = GenImageColor(Size, Size, Color.Blank);
        float center = Size * 0.5f;
        float radius = center - 1;
        for (int y = 0; y < Size; y++) {
            for (int x = 0; x < Size; x++) {
                float distance = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(center));
                float alpha = Math.Clamp(radius - distance + 0.5f, 0, 1);
                ImageDrawPixel(ref image, x, y, new Color(255, 255, 255, (int)(alpha * 255)));
            }
        }
        s_circle = LoadTextureFromImage(image);
        UnloadImage(image);
        GenTextureMipmaps(ref s_circle);
        SetTextureFilter(s_circle, TextureFilter.Trilinear);
    }

    public static void Unload() => UnloadTexture(s_circle);

    public static void Circle(Vector2 center, float radius, Color color) {
        Rectangle dest = new(center.X - radius, center.Y - radius, radius * 2, radius * 2);
        DrawTexturePro(s_circle, s_source, dest, Vector2.Zero, 0, color);
    }
}
