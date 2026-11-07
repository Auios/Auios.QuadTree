using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics;

namespace Auios.QuadTree;

/// <summary>
/// A tree data structure in which each node has exactly four children.
/// Used to partition a two-dimensional space by recursively subdividing it into four quadrants.
/// Allows to efficiently find objects spatially relative to each other.
/// </summary>
/// <remarks>
/// <para>Each object's bounds are read once, when it is inserted. If an object moves, either
/// <see cref="Remove"/> and re-<see cref="Insert"/> it, or <see cref="Clear"/> and rebuild the tree.
/// Rebuilding every frame is cheap: <see cref="Clear"/> keeps the node and item storage for reuse.</para>
/// <para>An object is stored on the deepest quadrant that fully contains it, so objects that straddle a
/// split line stay on the parent quadrant.</para>
/// </remarks>
/// <typeparam name="T">The type of elements in the QuadTree.</typeparam>
public class QuadTree<T> {
    private const int MinItemCapacity = 4;
    private const int MaxInitialItemCapacity = 64;

    /// <summary>A quadrant. Children are allocated as a block of four: top-left, top-right, bottom-left, bottom-right.</summary>
    private struct Node {
        public float Left, Top, Right, Bottom, CenterX, CenterY;
        /// <summary>Index of the top-left child, or -1 for a leaf.</summary>
        public int FirstChild;
        public int Level;
        /// <summary>Number of items stored directly on this node.</summary>
        public int Count;
        /// <summary>The search stamp of the last search that overlapped this node.</summary>
        public uint Stamp;
        /// <summary>Items stored directly on this node. Kept across <see cref="Clear"/> so rebuilding does not allocate.</summary>
        public Item[]? Items;
    }

    /// <summary>Edges of an object. Kept in its own sequential struct so a search can load all four as one vector;
    /// the runtime may reorder the fields of <see cref="Item"/> because it holds a <typeparamref name="T"/>.</summary>
    [StructLayout(LayoutKind.Sequential)]
    private struct Box {
        public float Left, Top, Right, Bottom;
    }

    /// <summary>An object together with the bounds it had when it was inserted.</summary>
    private struct Item {
        public Box Bounds;
        public T Value;
    }

    /// <summary>A search region, passed by reference to keep the recursive search cheap.</summary>
    private readonly struct Query(float left, float top, float right, float bottom, uint stamp) {
        public readonly float Left = left, Top = top, Right = right, Bottom = bottom;
        public readonly uint Stamp = stamp;
        /// <summary>(Right, Bottom, +inf, +inf): an object's (Left, Top, Right, Bottom) must be less or equal.</summary>
        public readonly Vector128<float> Max = Vector128.Create(right, bottom, float.PositiveInfinity, float.PositiveInfinity);
        /// <summary>(-inf, -inf, Left, Top): an object's (Left, Top, Right, Bottom) must be greater or equal.</summary>
        public readonly Vector128<float> Min = Vector128.Create(float.NegativeInfinity, float.NegativeInfinity, left, top);
    }

    private readonly IQuadTreeObjectBounds<T> _objectBounds;
    private readonly float _x, _y, _width, _height;
    private readonly int _initialItemCapacity;
    private Node[] _nodes;
    private int _nodeCount;
    private int _count;
    private uint _stamp;

    /// <summary>Gets the depth level of the root quadrant.</summary>
    public int CurrentLevel { get; }
    /// <summary>Gets the max depth level.</summary>
    public int MaxLevel { get; }
    /// <summary>Gets the max number of objects a quadrant holds before it splits.</summary>
    public int MaxObjects { get; }

    /// <summary>Gets the area covered by the QuadTree.</summary>
    public QuadTreeRect Area => new(_x, _y, _width, _height) { IsOverlapped = IsNodeOverlapped(0) };

    /// <summary>Gets the number of quadrants in the QuadTree, including the root.</summary>
    public int NodeCount => _nodeCount;

    /// <summary>Initializes a new instance of the <see cref="QuadTree{T}"/> class.</summary>
    /// <param name="x">The x-coordinate of the upper-left corner of the boundary rectangle.</param>
    /// <param name="y">The y-coordinate of the upper-left corner of the boundary rectangle.</param>
    /// <param name="width">The width of the boundary rectangle.</param>
    /// <param name="height">The height of the boundary rectangle.</param>
    /// <param name="objectBounds">The set of methods for getting boundaries of an element.</param>
    /// <param name="maxObjects">The max number of elements in one quadrant before it splits.</param>
    /// <param name="maxLevel">The max depth level.</param>
    /// <param name="currentLevel">The depth level of the root quadrant. Leave default.</param>
    public QuadTree(float x, float y, float width, float height, IQuadTreeObjectBounds<T> objectBounds, int maxObjects = 10, int maxLevel = 5, int currentLevel = 0) {
        ArgumentNullException.ThrowIfNull(objectBounds);
        ArgumentOutOfRangeException.ThrowIfNegative(maxObjects);
        _objectBounds = objectBounds;
        _x = x;
        _y = y;
        _width = width;
        _height = height;
        MaxObjects = maxObjects;
        MaxLevel = maxLevel;
        CurrentLevel = currentLevel;
        _initialItemCapacity = Math.Clamp(maxObjects + 1, MinItemCapacity, MaxInitialItemCapacity);
        _nodes = new Node[16];
        ResetNode(ref _nodes[0], x, y, x + width, y + height, currentLevel);
        _nodeCount = 1;
    }

    /// <summary>Initializes a new instance of the <see cref="QuadTree{T}"/> class.</summary>
    public QuadTree(float width, float height, IQuadTreeObjectBounds<T> objectBounds, int maxObjects = 10, int maxLevel = 5, int currentLevel = 0)
        : this(0, 0, width, height, objectBounds, maxObjects, maxLevel, currentLevel) { }

    /// <summary>Initializes a new instance of the <see cref="QuadTree{T}"/> class.</summary>
    public QuadTree(Vector2 size, IQuadTreeObjectBounds<T> objectBounds, int maxObjects = 10, int maxLevel = 5, int currentLevel = 0)
        : this(0, 0, size.X, size.Y, objectBounds, maxObjects, maxLevel, currentLevel) { }

    /// <summary>Initializes a new instance of the <see cref="QuadTree{T}"/> class.</summary>
    public QuadTree(Vector2 position, Vector2 size, IQuadTreeObjectBounds<T> objectBounds, int maxObjects = 10, int maxLevel = 5, int currentLevel = 0)
        : this(position.X, position.Y, size.X, size.Y, objectBounds, maxObjects, maxLevel, currentLevel) { }

    /// <summary>Removes all elements from the QuadTree. Allocated storage is kept for reuse.</summary>
    public void Clear() {
        bool clearReferences = RuntimeHelpers.IsReferenceOrContainsReferences<T>();
        for (int i = 0; i < _nodeCount; i++) {
            ref Node node = ref _nodes[i];
            if (clearReferences && node.Count > 0) {
                Array.Clear(node.Items!, 0, node.Count);
            }
            node.Count = 0;
        }

        ref Node root = ref _nodes[0];
        ResetNode(ref root, root.Left, root.Top, root.Right, root.Bottom, CurrentLevel);
        _nodeCount = 1;
        _count = 0;
    }

    /// <summary>Inserts an object into the QuadTree.</summary>
    /// <returns>true if the object is added; false if it lies outside the QuadTree.</returns>
    public bool Insert(T obj) {
        if (obj is null) throw new ArgumentNullException(nameof(obj));

        Item item;
        item.Bounds.Left = _objectBounds.GetLeft(obj);
        item.Bounds.Top = _objectBounds.GetTop(obj);
        item.Bounds.Right = _objectBounds.GetRight(obj);
        item.Bounds.Bottom = _objectBounds.GetBottom(obj);
        item.Value = obj;

        ref Node root = ref _nodes[0];
        ref Box bounds = ref item.Bounds;
        // Written so that NaN bounds are rejected.
        if (!(bounds.Left >= root.Left && bounds.Right <= root.Right && bounds.Top >= root.Top && bounds.Bottom <= root.Bottom)) {
            return false;
        }

        int index = 0;
        while (true) {
            ref Node node = ref _nodes[index];
            if (node.FirstChild >= 0) {
                int quadrant = GetQuadrant(in node, bounds.Left, bounds.Top, bounds.Right, bounds.Bottom);
                if (quadrant >= 0) {
                    index = node.FirstChild + quadrant;
                    continue;
                }
                AddItem(ref node, in item);
                break;
            }

            AddItem(ref node, in item);
            if (node.Count > MaxObjects && node.Level < MaxLevel) {
                Split(index);
            }
            break;
        }

        _count++;
        return true;
    }

    /// <summary>Inserts a collection of objects into the QuadTree.</summary>
    public void InsertRange(IEnumerable<T> objects) {
        ArgumentNullException.ThrowIfNull(objects);
        foreach (T obj in objects) {
            Insert(obj);
        }
    }

    /// <summary>Removes the first occurrence of an object from the QuadTree.</summary>
    /// <remarks>Fastest when the object's bounds are unchanged since it was inserted. If they changed, or the
    /// object is not in the tree, every quadrant is searched.</remarks>
    /// <returns>true if the object was found and removed.</returns>
    public bool Remove(T obj) {
        if (obj is null) throw new ArgumentNullException(nameof(obj));

        float left = _objectBounds.GetLeft(obj);
        float top = _objectBounds.GetTop(obj);
        float right = _objectBounds.GetRight(obj);
        float bottom = _objectBounds.GetBottom(obj);

        ref Node root = ref _nodes[0];
        if (left >= root.Left && right <= root.Right && top >= root.Top && bottom <= root.Bottom) {
            int index = 0;
            while (true) {
                ref Node node = ref _nodes[index];
                if (RemoveFromNode(ref node, obj)) {
                    _count--;
                    return true;
                }
                if (node.FirstChild < 0) break;
                int quadrant = GetQuadrant(in node, left, top, right, bottom);
                if (quadrant < 0) break;
                index = node.FirstChild + quadrant;
            }
        }

        for (int i = 0; i < _nodeCount; i++) {
            if (RemoveFromNode(ref _nodes[i], obj)) {
                _count--;
                return true;
            }
        }

        return false;
    }

    /// <summary>Returns the total number of objects in the QuadTree.</summary>
    public int Count() => _count;

    /// <summary>Returns every <see cref="QuadTreeRect"/> from the QuadTree. The root area is first.</summary>
    public QuadTreeRect[] GetGrid() {
        QuadTreeRect[] grid = new QuadTreeRect[_nodeCount];
        for (int i = 0; i < grid.Length; i++) {
            grid[i] = GetNodeRect(i);
        }
        return grid;
    }

    /// <summary>Adds every <see cref="QuadTreeRect"/> from the QuadTree to <paramref name="results"/>. The root area is first.</summary>
    public void GetGrid(List<QuadTreeRect> results) {
        ArgumentNullException.ThrowIfNull(results);
        results.EnsureCapacity(results.Count + _nodeCount);
        for (int i = 0; i < _nodeCount; i++) {
            results.Add(GetNodeRect(i));
        }
    }

    /// <summary>Finds objects whose bounds intersect the search region.</summary>
    public T[] FindObjects(QuadTreeRect rect) {
        List<T> results = [];
        Search(rect.Left, rect.Top, rect.Right, rect.Bottom, results);
        return [.. results];
    }

    /// <summary>Finds objects whose bounds intersect the given object's bounds.</summary>
    public T[] FindObjects(T bounds) {
        List<T> results = [];
        FindObjects(bounds, results);
        return [.. results];
    }

    /// <summary>Adds objects whose bounds intersect the search region to <paramref name="results"/>.</summary>
    /// <remarks>Does not allocate when <paramref name="results"/> has enough capacity. Existing items are kept.</remarks>
    public void FindObjects(QuadTreeRect rect, List<T> results) {
        ArgumentNullException.ThrowIfNull(results);
        Search(rect.Left, rect.Top, rect.Right, rect.Bottom, results);
    }

    /// <summary>Adds objects whose bounds intersect the given object's bounds to <paramref name="results"/>.</summary>
    /// <remarks>Does not allocate when <paramref name="results"/> has enough capacity. Existing items are kept.</remarks>
    public void FindObjects(T bounds, List<T> results) {
        if (bounds is null) throw new ArgumentNullException(nameof(bounds));
        ArgumentNullException.ThrowIfNull(results);
        Search(_objectBounds.GetLeft(bounds), _objectBounds.GetTop(bounds), _objectBounds.GetRight(bounds), _objectBounds.GetBottom(bounds), results);
    }

    private void Search(float left, float top, float right, float bottom, List<T> results) {
        uint stamp = NextStamp();
        ref Node root = ref _nodes[0];
        // Written so that a NaN search region matches nothing.
        if (!(left <= root.Right && right >= root.Left && top <= root.Bottom && bottom >= root.Top)) return;
        Query query = new(left, top, right, bottom, stamp);
        SearchNode(0, in query, results);
    }

    private void SearchNode(int index, in Query query, List<T> results) {
        ref Node node = ref _nodes[index];
        node.Stamp = query.Stamp;

        if (query.Left <= node.Left && query.Right >= node.Right && query.Top <= node.Top && query.Bottom >= node.Bottom) {
            // The search region covers this whole quadrant: everything below it matches.
            CollectAll(index, query.Stamp, results);
            return;
        }

        ReadOnlySpan<Item> items = new(node.Items, 0, node.Count);
        if (Vector128.IsHardwareAccelerated) {
            // Test all four edges at once: (L, T, R, B) <= (qR, qB, inf, inf) and >= (-inf, -inf, qL, qT).
            Vector128<float> max = query.Max;
            Vector128<float> min = query.Min;
            foreach (ref readonly Item item in items) {
                Vector128<float> bounds = Vector128.LoadUnsafe(ref Unsafe.As<Box, float>(ref Unsafe.AsRef(in item.Bounds)));
                Vector128<float> inside = Vector128.LessThanOrEqual(bounds, max) & Vector128.GreaterThanOrEqual(bounds, min);
                if (inside.ExtractMostSignificantBits() == 0b1111) {
                    results.Add(item.Value);
                }
            }
        } else {
            foreach (ref readonly Item item in items) {
                ref readonly Box bounds = ref item.Bounds;
                if ((bounds.Left <= query.Right) & (bounds.Right >= query.Left) & (bounds.Top <= query.Bottom) & (bounds.Bottom >= query.Top)) {
                    results.Add(item.Value);
                }
            }
        }

        int first = node.FirstChild;
        if (first < 0) return;

        // The search region already overlaps this node, so each child needs only one test per axis.
        bool west = query.Left <= node.CenterX;
        bool east = query.Right >= node.CenterX;
        bool north = query.Top <= node.CenterY;
        bool south = query.Bottom >= node.CenterY;
        if (north) {
            if (west) SearchNode(first, in query, results);
            if (east) SearchNode(first + 1, in query, results);
        }
        if (south) {
            if (west) SearchNode(first + 2, in query, results);
            if (east) SearchNode(first + 3, in query, results);
        }
    }

    private void CollectAll(int index, uint stamp, List<T> results) {
        ref Node node = ref _nodes[index];
        node.Stamp = stamp;

        foreach (ref readonly Item item in new ReadOnlySpan<Item>(node.Items, 0, node.Count)) {
            results.Add(item.Value);
        }

        int first = node.FirstChild;
        if (first < 0) return;
        CollectAll(first, stamp, results);
        CollectAll(first + 1, stamp, results);
        CollectAll(first + 2, stamp, results);
        CollectAll(first + 3, stamp, results);
    }

    /// <summary>Splits a leaf into four quadrants and moves down every item that fits in one of them.</summary>
    private void Split(int index) {
        int first = _nodeCount;
        if (first + 4 > _nodes.Length) {
            Array.Resize(ref _nodes, Math.Max(_nodes.Length * 2, first + 4));
        }
        _nodeCount = first + 4;

        ref Node node = ref _nodes[index];
        int level = node.Level + 1;
        ResetNode(ref _nodes[first], node.Left, node.Top, node.CenterX, node.CenterY, level);
        ResetNode(ref _nodes[first + 1], node.CenterX, node.Top, node.Right, node.CenterY, level);
        ResetNode(ref _nodes[first + 2], node.Left, node.CenterY, node.CenterX, node.Bottom, level);
        ResetNode(ref _nodes[first + 3], node.CenterX, node.CenterY, node.Right, node.Bottom, level);
        node.FirstChild = first;

        Item[] items = node.Items!;
        int count = node.Count;
        int kept = 0;
        for (int i = 0; i < count; i++) {
            ref Item item = ref items[i];
            int quadrant = GetQuadrant(in node, item.Bounds.Left, item.Bounds.Top, item.Bounds.Right, item.Bounds.Bottom);
            if (quadrant < 0) {
                items[kept++] = item;
            } else {
                AddItem(ref _nodes[first + quadrant], in item);
            }
        }
        if (RuntimeHelpers.IsReferenceOrContainsReferences<T>()) {
            Array.Clear(items, kept, count - kept);
        }
        node.Count = kept;

        // Splitting a child can grow _nodes, so re-read each child by index.
        for (int child = first; child < first + 4; child++) {
            if (_nodes[child].Count > MaxObjects && _nodes[child].Level < MaxLevel) {
                Split(child);
            }
        }
    }

    /// <summary>Returns the child quadrant (0 TL, 1 TR, 2 BL, 3 BR) that fully contains the bounds, or -1 if they straddle a split line.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static int GetQuadrant(in Node node, float left, float top, float right, float bottom) {
        int quadrant;
        if (right <= node.CenterX) quadrant = 0;
        else if (left >= node.CenterX) quadrant = 1;
        else return -1;

        if (bottom <= node.CenterY) return quadrant;
        if (top >= node.CenterY) return quadrant + 2;
        return -1;
    }

    private void AddItem(ref Node node, in Item item) {
        Item[]? items = node.Items;
        if (items is null) {
            node.Items = items = new Item[_initialItemCapacity];
        } else if (node.Count == items.Length) {
            Array.Resize(ref node.Items, items.Length * 2);
            items = node.Items;
        }
        items[node.Count++] = item;
    }

    private static bool RemoveFromNode(ref Node node, T obj) {
        int count = node.Count;
        if (count == 0) return false;

        Item[] items = node.Items!;
        for (int i = 0; i < count; i++) {
            if (EqualityComparer<T>.Default.Equals(items[i].Value, obj)) {
                int last = count - 1;
                items[i] = items[last];
                items[last] = default;
                node.Count = last;
                return true;
            }
        }
        return false;
    }

    private static void ResetNode(ref Node node, float left, float top, float right, float bottom, int level) {
        node.Left = left;
        node.Top = top;
        node.Right = right;
        node.Bottom = bottom;
        node.CenterX = left + (right - left) * 0.5f;
        node.CenterY = top + (bottom - top) * 0.5f;
        node.FirstChild = -1;
        node.Level = level;
        node.Count = 0;
        node.Stamp = 0;
    }

    private uint NextStamp() {
        if (++_stamp == 0) {
            // Wrapped around: forget every old stamp so no stale node looks overlapped.
            for (int i = 0; i < _nodes.Length; i++) {
                _nodes[i].Stamp = 0;
            }
            _stamp = 1;
        }
        return _stamp;
    }

    private bool IsNodeOverlapped(int index) => _stamp != 0 && _nodes[index].Stamp == _stamp;

    private QuadTreeRect GetNodeRect(int index) {
        if (index == 0) return Area;
        ref Node node = ref _nodes[index];
        return new QuadTreeRect(node.Left, node.Top, node.Right - node.Left, node.Bottom - node.Top) {
            IsOverlapped = IsNodeOverlapped(index),
        };
    }
}
