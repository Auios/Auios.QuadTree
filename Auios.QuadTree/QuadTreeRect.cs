using System.Numerics;

namespace Auios.QuadTree;

/// <summary>Stores the position and size of a quadrant or search region.</summary>
/// <param name="x">The x-coordinate of the upper-left corner of the rectangle.</param>
/// <param name="y">The y-coordinate of the upper-left corner of the rectangle.</param>
/// <param name="width">The width of the rectangle.</param>
/// <param name="height">The height of the rectangle.</param>
public class QuadTreeRect(float x, float y, float width, float height) {
    public float X = x;
    public float Y = y;
    public float Width = width;
    public float Height = height;

    public float Top => Y;
    public float Left => X;
    public float Bottom => Y + Height;
    public float Right => X + Width;
    public float HalfWidth => Width * 0.5f;
    public float HalfHeight => Height * 0.5f;
    public float CenterX => X + HalfWidth;
    public float CenterY => Y + HalfHeight;
    public Vector2 Position => new(X, Y);
    public Vector2 Size => new(Width, Height);
    public Vector2 Half => new(HalfWidth, HalfHeight);
    public Vector2 Center => new(CenterX, CenterY);

    /// <summary>Set to true when this quadrant overlaps a search region during <see cref="QuadTree{T}.FindObjects(QuadTreeRect)"/>. Cleared by <see cref="QuadTree{T}.Clear"/> and at the start of a new search.</summary>
    public bool IsOverlapped;
}
