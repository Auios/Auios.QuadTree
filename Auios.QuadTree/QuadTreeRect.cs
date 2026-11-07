using System.Numerics;

namespace Auios.QuadTree;

/// <summary>Stores the position and size of a quadrant or search region.</summary>
/// <remarks>This is a value type, so searching with a new <see cref="QuadTreeRect"/> does not allocate.</remarks>
/// <param name="x">The x-coordinate of the upper-left corner of the rectangle.</param>
/// <param name="y">The y-coordinate of the upper-left corner of the rectangle.</param>
/// <param name="width">The width of the rectangle.</param>
/// <param name="height">The height of the rectangle.</param>
public struct QuadTreeRect(float x, float y, float width, float height) : IEquatable<QuadTreeRect> {
    public float X = x;
    public float Y = y;
    public float Width = width;
    public float Height = height;

    /// <summary>True when this quadrant overlapped the most recent search region passed to <see cref="QuadTree{T}.FindObjects(QuadTreeRect)"/>. Only set on rectangles returned by <see cref="QuadTree{T}.Area"/> and <see cref="QuadTree{T}.GetGrid()"/>.</summary>
    public bool IsOverlapped;

    public readonly float Top => Y;
    public readonly float Left => X;
    public readonly float Bottom => Y + Height;
    public readonly float Right => X + Width;
    public readonly float HalfWidth => Width * 0.5f;
    public readonly float HalfHeight => Height * 0.5f;
    public readonly float CenterX => X + HalfWidth;
    public readonly float CenterY => Y + HalfHeight;
    public readonly Vector2 Position => new(X, Y);
    public readonly Vector2 Size => new(Width, Height);
    public readonly Vector2 Half => new(HalfWidth, HalfHeight);
    public readonly Vector2 Center => new(CenterX, CenterY);

    /// <summary>Initializes a rectangle from a position and size.</summary>
    public QuadTreeRect(Vector2 position, Vector2 size) : this(position.X, position.Y, size.X, size.Y) { }

    /// <summary>Returns true if this rectangle and <paramref name="other"/> overlap or touch.</summary>
    public readonly bool Intersects(QuadTreeRect other) =>
        X <= other.Right && Right >= other.X && Y <= other.Bottom && Bottom >= other.Y;

    /// <summary>Returns true if the point lies inside this rectangle or on its edge.</summary>
    public readonly bool Contains(Vector2 point) =>
        point.X >= X && point.X <= Right && point.Y >= Y && point.Y <= Bottom;

    public readonly bool Equals(QuadTreeRect other) =>
        X.Equals(other.X) && Y.Equals(other.Y) && Width.Equals(other.Width) && Height.Equals(other.Height) && IsOverlapped == other.IsOverlapped;

    public override readonly bool Equals(object? obj) => obj is QuadTreeRect other && Equals(other);

    public override readonly int GetHashCode() => HashCode.Combine(X, Y, Width, Height, IsOverlapped);

    public override readonly string ToString() => $"{{X={X}, Y={Y}, Width={Width}, Height={Height}}}";

    public static bool operator ==(QuadTreeRect left, QuadTreeRect right) => left.Equals(right);

    public static bool operator !=(QuadTreeRect left, QuadTreeRect right) => !left.Equals(right);
}
