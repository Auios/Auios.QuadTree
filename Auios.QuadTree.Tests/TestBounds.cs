using System.Numerics;
using Auios.QuadTree;

namespace Auios.QuadTree.Tests;

internal sealed class PointBounds : IQuadTreeObjectBounds<Vector2> {
    public float GetBottom(Vector2 obj) => obj.Y;
    public float GetTop(Vector2 obj) => obj.Y;
    public float GetLeft(Vector2 obj) => obj.X;
    public float GetRight(Vector2 obj) => obj.X;
}

internal sealed class RectBounds : IQuadTreeObjectBounds<QuadTreeRect> {
    public float GetBottom(QuadTreeRect obj) => obj.Bottom;
    public float GetTop(QuadTreeRect obj) => obj.Top;
    public float GetLeft(QuadTreeRect obj) => obj.Left;
    public float GetRight(QuadTreeRect obj) => obj.Right;
}
