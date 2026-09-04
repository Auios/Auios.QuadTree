using System.Numerics;

namespace Auios.QuadTree;

/// <summary>
/// A tree data structure in which each node has exactly four children.
/// Used to partition a two-dimensional space by recursively subdividing it into four quadrants.
/// Allows to efficiently find objects spatially relative to each other.
/// </summary>
/// <typeparam name="T">The type of elements in the QuadTree.</typeparam>
/// <param name="x">The x-coordinate of the upper-left corner of the boundary rectangle.</param>
/// <param name="y">The y-coordinate of the upper-left corner of the boundary rectangle.</param>
/// <param name="width">The width of the boundary rectangle.</param>
/// <param name="height">The height of the boundary rectangle.</param>
/// <param name="objectBounds">The set of methods for getting boundaries of an element.</param>
/// <param name="maxObjects">The max number of elements in one rectangle.</param>
/// <param name="maxLevel">The max depth level.</param>
/// <param name="currentLevel">The current depth level. Leave default if this is the root QuadTree.</param>
public class QuadTree<T>(float x, float y, float width, float height, IQuadTreeObjectBounds<T> objectBounds, int maxObjects = 10, int maxLevel = 5, int currentLevel = 0) {
    /// <summary>The area of this quadrant.</summary>
    public QuadTreeRect Area = new(x, y, width, height);
    /// <summary>Objects stored on this node. After a split, objects that do not fit in a single child remain here.</summary>
    private readonly HashSet<T> _objects = [];
    /// <summary>The bounds which this quadrant expects its objects to conform to.</summary>
    private readonly IQuadTreeObjectBounds<T> _objectBounds = objectBounds;
    /// <summary>If this quadrant has sub quadrants.</summary>
    private bool _hasChildren;

    private QuadTree<T>? _quadTl;
    private QuadTree<T>? _quadTr;
    private QuadTree<T>? _quadBl;
    private QuadTree<T>? _quadBr;

    /// <summary>Gets the current depth level of this quadrant.</summary>
    public int CurrentLevel { get; } = currentLevel;
    /// <summary>Gets the max depth level.</summary>
    public int MaxLevel { get; } = maxLevel;
    /// <summary>Gets the max number of objects in this quadrant.</summary>
    public int MaxObjects { get; } = maxObjects;

    /// <summary>Initializes a new instance of the <see cref="QuadTree{T}"/> class.</summary>
    public QuadTree(float width, float height, IQuadTreeObjectBounds<T> objectBounds, int maxObjects = 10, int maxLevel = 5, int currentLevel = 0)
        : this(0, 0, width, height, objectBounds, maxObjects, maxLevel, currentLevel) { }

    /// <summary>Initializes a new instance of the <see cref="QuadTree{T}"/> class.</summary>
    public QuadTree(Vector2 size, IQuadTreeObjectBounds<T> objectBounds, int maxObjects = 10, int maxLevel = 5, int currentLevel = 0)
        : this(0, 0, size.X, size.Y, objectBounds, maxObjects, maxLevel, currentLevel) { }

    /// <summary>Initializes a new instance of the <see cref="QuadTree{T}"/> class.</summary>
    public QuadTree(Vector2 position, Vector2 size, IQuadTreeObjectBounds<T> objectBounds, int maxObjects = 10, int maxLevel = 5, int currentLevel = 0)
        : this(position.X, position.Y, size.X, size.Y, objectBounds, maxObjects, maxLevel, currentLevel) { }

    private bool IsObjectFullyContained(T obj) {
        if (_objectBounds.GetLeft(obj) < Area.Left) return false;
        if (_objectBounds.GetRight(obj) > Area.Right) return false;
        if (_objectBounds.GetTop(obj) < Area.Top) return false;
        if (_objectBounds.GetBottom(obj) > Area.Bottom) return false;
        return true;
    }

    private bool ObjectIntersects(T obj, QuadTreeRect rect) {
        if (_objectBounds.GetTop(obj) > rect.Bottom) return false;
        if (_objectBounds.GetBottom(obj) < rect.Top) return false;
        if (_objectBounds.GetLeft(obj) > rect.Right) return false;
        if (_objectBounds.GetRight(obj) < rect.Left) return false;
        return true;
    }

    private static bool AreasOverlap(QuadTreeRect a, QuadTreeRect b) {
        if (a.Right < b.Left || a.Left > b.Right) return false;
        if (a.Top > b.Bottom || a.Bottom < b.Top) return false;
        return true;
    }

    /// <summary>Splits the current quadrant into four new quadrants and drops objects into children when they fit.</summary>
    private void Quarter() {
        if (CurrentLevel >= MaxLevel) return;

        int nextLevel = CurrentLevel + 1;
        _hasChildren = true;
        _quadTl = new QuadTree<T>(Area.X, Area.Y, Area.HalfWidth, Area.HalfHeight, _objectBounds, MaxObjects, MaxLevel, nextLevel);
        _quadTr = new QuadTree<T>(Area.CenterX, Area.Y, Area.HalfWidth, Area.HalfHeight, _objectBounds, MaxObjects, MaxLevel, nextLevel);
        _quadBl = new QuadTree<T>(Area.X, Area.CenterY, Area.HalfWidth, Area.HalfHeight, _objectBounds, MaxObjects, MaxLevel, nextLevel);
        _quadBr = new QuadTree<T>(Area.CenterX, Area.CenterY, Area.HalfWidth, Area.HalfHeight, _objectBounds, MaxObjects, MaxLevel, nextLevel);

        T[] pending = [.. _objects];
        _objects.Clear();
        foreach (T obj in pending) {
            Insert(obj);
        }
    }

    /// <summary>Removes all elements from the QuadTree.</summary>
    public void Clear() {
        if (_hasChildren) {
            _quadTl!.Clear();
            _quadTl = null;
            _quadTr!.Clear();
            _quadTr = null;
            _quadBl!.Clear();
            _quadBl = null;
            _quadBr!.Clear();
            _quadBr = null;
        }

        _objects.Clear();
        _hasChildren = false;
        Area.IsOverlapped = false;
    }

    /// <summary>Inserts an object into the QuadTree.</summary>
    /// <returns>true if the object is added; false if it lies outside this quadrant.</returns>
    public bool Insert(T obj) {
        if (obj is null) throw new ArgumentNullException(nameof(obj));

        if (!IsObjectFullyContained(obj)) return false;

        if (_hasChildren) {
            if (_quadTl!.Insert(obj)) return true;
            if (_quadTr!.Insert(obj)) return true;
            if (_quadBl!.Insert(obj)) return true;
            if (_quadBr!.Insert(obj)) return true;
            _objects.Add(obj);
            return true;
        }

        _objects.Add(obj);
        if (_objects.Count > MaxObjects) {
            Quarter();
        }

        return true;
    }

    /// <summary>Inserts a collection of objects into the QuadTree.</summary>
    public void InsertRange(IEnumerable<T> objects) {
        foreach (T obj in objects) {
            Insert(obj);
        }
    }

    /// <summary>Returns the total number of objects in this instance and its children.</summary>
    public int Count() {
        int count = _objects.Count;
        if (_hasChildren) {
            count += _quadTl!.Count();
            count += _quadTr!.Count();
            count += _quadBl!.Count();
            count += _quadBr!.Count();
        }

        return count;
    }

    /// <summary>Returns every <see cref="QuadTreeRect"/> from the QuadTree.</summary>
    public QuadTreeRect[] GetGrid() {
        List<QuadTreeRect> grid = [Area];
        if (_hasChildren) {
            grid.AddRange(_quadTl!.GetGrid());
            grid.AddRange(_quadTr!.GetGrid());
            grid.AddRange(_quadBl!.GetGrid());
            grid.AddRange(_quadBr!.GetGrid());
        }
        return [.. grid];
    }

    /// <summary>Finds objects whose bounds intersect the search region.</summary>
    public T[] FindObjects(QuadTreeRect rect) {
        ResetOverlapFlags();
        HashSet<T> found = [];
        CollectIntersecting(rect, found);
        return [.. found];
    }

    /// <summary>Finds objects whose bounds intersect the given object's bounds.</summary>
    public T[] FindObjects(T bounds) {
        float left = _objectBounds.GetLeft(bounds);
        float top = _objectBounds.GetTop(bounds);
        float width = _objectBounds.GetRight(bounds) - left;
        float height = _objectBounds.GetBottom(bounds) - top;
        return FindObjects(new QuadTreeRect(left, top, width, height));
    }

    private void ResetOverlapFlags() {
        Area.IsOverlapped = false;
        _quadTl?.ResetOverlapFlags();
        _quadTr?.ResetOverlapFlags();
        _quadBl?.ResetOverlapFlags();
        _quadBr?.ResetOverlapFlags();
    }

    private void CollectIntersecting(QuadTreeRect rect, HashSet<T> found) {
        if (!AreasOverlap(Area, rect)) return;

        Area.IsOverlapped = true;

        foreach (T obj in _objects) {
            if (ObjectIntersects(obj, rect)) {
                found.Add(obj);
            }
        }

        if (_hasChildren) {
            _quadTl!.CollectIntersecting(rect, found);
            _quadTr!.CollectIntersecting(rect, found);
            _quadBl!.CollectIntersecting(rect, found);
            _quadBr!.CollectIntersecting(rect, found);
        }
    }
}
