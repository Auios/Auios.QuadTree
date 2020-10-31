// Copyright 2020 Connor Andrew Ngo
// Licensed under the MIT License

namespace Auios.QuadTree
{
    /// <summary>Stores the position and size of the quadrant.</summary>
    public struct QuadTreeRect
    {
        public readonly float X;
        public readonly float Y;
        public readonly float Width;
        public readonly float Height;
        public readonly float Top => Y;
        public readonly float Bottom => Y + Height;
        public readonly float Left => X;
        public readonly float Right => X + Width;
        public readonly float HalfWidth => Width * 0.5f;
        public readonly float HalfHeight => Height * 0.5f;
        public readonly float MiddleX => X + HalfWidth;
        public readonly float MiddleY => Y + HalfHeight;
        public bool isOverlapped;

        /// <summary>Initializes a new instance of the <see cref="T:Auios.QuadTree.QuadTreeRect"></see> struct with the specified position and size.</summary>
        /// <param name="x">The x-coordinate of the upper-left corner of the quadrant.</param>
        /// <param name="y">The y-coordinate of the upper-left corner of the quadrant.</param>
        /// <param name="width">The width of the quadrant.</param>
        /// <param name="height">The height of the quadrant.</param>
        public QuadTreeRect(float x, float y, float width, float height)
        {
            X = x;
            Y = y;
            Width = width;
            Height = height;
            isOverlapped = false;
        }
    }
}
