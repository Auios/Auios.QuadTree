namespace Auios.QuadTree {
    public interface IQuadTreeObjectBounds<in T> {
        float GetTop(T obj);
        float GetBottom(T obj);
        float GetLeft(T obj);
        float GetRight(T obj);
    }
}
