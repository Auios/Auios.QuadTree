﻿// Copyright 2020 Connor Andrew Ngo
// Licensed under the MIT License

namespace Auios.QuadTree
{
    using Rectangle;

    /// <summary>Stores the position and size of the quadrant.</summary>
    public class QuadTreeRect : Rectangle
    {
        /// <summary>If an overlap check from the QuadTree returns true on this quadrant this flag will be set to true. Set to false on QuadTree.Clear().</summary>
        public bool isOverlapped;

        /// <summary>Initializes a new instance of the <see cref="T:Auios.QuadTree.QuadTreeRect"></see> struct with the specified position and size.</summary>
        /// <param name="x">The x-coordinate of the upper-left corner of the quadrant.</param>
        /// <param name="y">The y-coordinate of the upper-left corner of the quadrant.</param>
        /// <param name="width">The width of the quadrant.</param>
        /// <param name="height">The height of the quadrant.</param>
        public QuadTreeRect(float x, float y, float width, float height) : base(x, y, width, height)
        {
            isOverlapped = false;
        }
    }
}
