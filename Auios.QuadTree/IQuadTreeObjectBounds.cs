// Copyright 2024 Connor O'Connor
// Licensed under the MIT License

namespace Auios.QuadTree {
    public interface IQuadTreeObjectBounds<in T> {
        float GetTop(T obj);
        float GetBottom(T obj);
        float GetLeft(T obj);
        float GetRight(T obj);
    }
}
