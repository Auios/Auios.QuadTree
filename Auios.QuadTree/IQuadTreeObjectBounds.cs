// Copyright 2020 Connor Andrew Ngo
// Licensed under the MIT License

namespace Auios.QuadTree
{
    /// <summary>Provides a set of methods for getting boundaries of the object.</summary>
    /// <typeparam name="T">The type of object in the QuadTree.</typeparam>
    public interface IQuadTreeObjectBounds<in T>
    {
        /// <summary>Gets let most edge of the object.</summary>
        float GetLeft(T obj);
        /// <summary>Gets the right most edge of the object.</summary>
        float GetRight(T obj);
        /// <summary>Gets the top most edge of the object.</summary>
        float GetTop(T obj);
        /// <summary>Gets the bottom most edge of the object.</summary>
        float GetBottom(T obj);
    }
}
