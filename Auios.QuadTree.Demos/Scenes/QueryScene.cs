using System.Numerics;
using Raylib_cs;
using static Raylib_cs.Raylib;

namespace Auios.QuadTree.Demos.Scenes;

/// <summary>Points in clusters, searched with a rectangle or circle that follows the mouse.</summary>
internal sealed class QueryScene : Scene {
    private sealed class PointBounds : IQuadTreeObjectBounds<Vector2> {
        public float GetLeft(Vector2 point) => point.X;
        public float GetTop(Vector2 point) => point.Y;
        public float GetRight(Vector2 point) => point.X;
        public float GetBottom(Vector2 point) => point.Y;
    }

    private readonly QuadTreeRect _world;
    private readonly QuadTree<Vector2> _tree;
    private readonly List<Vector2> _points = [];
    private readonly List<Vector2> _found = [];
    private readonly Random _random = new(7);

    private Vector2 _cursor;
    private float _size = 220;
    private bool _circle;
    private int _candidates;
    private int _bruteForceCount;
    private Timing _treeTime;
    private Timing _bruteForceTime;

    public QueryScene(QuadTreeRect world) {
        _world = world;
        _tree = new QuadTree<Vector2>(world.X, world.Y, world.Width, world.Height, new PointBounds(), maxObjects: 8, maxLevel: 8);
        Generate();
    }

    public override string Name => "Query";
    public override string Slug => "query";
    public override string Summary => "Search a region; touched quadrants light up.";
    public override (string Key, string Action)[] Controls => [
        ("Mouse", "move the search region"),
        ("Wheel", "resize it"),
        ("Tab", "rectangle / circle"),
        ("Left drag", "spray points (Insert)"),
        ("Right drag", "erase points (Remove)"),
        ("Space", "new point cloud"),
    ];

    private float Radius => _size * 0.5f;

    private QuadTreeRect Region => new(_cursor.X - Radius, _cursor.Y - Radius, _size, _size);

    public override void Update(float dt) {
        if (IsKeyPressed(KeyboardKey.Space)) Generate();
        if (IsKeyPressed(KeyboardKey.Tab)) _circle = !_circle;
        _size = Math.Clamp(_size * (1 + Pointer.Wheel * 0.12f), 12, 1000);
        _cursor = Pointer.Position(_world);

        if (Pointer.IsDown(MouseButton.Left)) {
            for (int i = 0; i < 16; i++) {
                Vector2 point = _cursor + Gaussian() * Radius * 0.35f;
                if (_tree.Insert(point)) _points.Add(point);
            }
        }

        _treeTime.Start();
        _found.Clear();
        _tree.FindObjects(Region, _found);
        _candidates = _found.Count;
        if (_circle) {
            // The tree answers rectangles. For a circle, search its bounding box and keep points inside the radius.
            _found.RemoveAll(p => !InShape(p));
        }
        _treeTime.Stop();

        _bruteForceTime.Start();
        _bruteForceCount = 0;
        foreach (Vector2 point in _points) {
            if (InShape(point)) _bruteForceCount++;
        }
        _bruteForceTime.Stop();

        if (Pointer.IsDown(MouseButton.Right) && _found.Count > 0) {
            foreach (Vector2 point in _found) _tree.Remove(point);
            _points.RemoveAll(InShape);
            _found.Clear();
        }
    }

    public override void Draw() {
        GridRenderer.Draw(_tree);

        foreach (Vector2 point in _points) {
            DrawRectangleV(point - new Vector2(1.5f), new Vector2(3), Theme.Object);
        }
        foreach (Vector2 point in _found) {
            DrawRectangleV(point - new Vector2(2.5f), new Vector2(5), Theme.Found);
        }

        if (_circle) {
            DrawRectangleLinesEx(GridRenderer.ToRaylib(Region), 1, ColorAlpha(Theme.Accent, 0.35f));
            DrawCircleV(_cursor, Radius, Theme.AccentFill);
            DrawCircleLinesV(_cursor, Radius, Theme.Accent);
        } else {
            DrawRectangleRec(GridRenderer.ToRaylib(Region), Theme.AccentFill);
            DrawRectangleLinesEx(GridRenderer.ToRaylib(Region), 2, Theme.Accent);
        }
    }

    public override void GetStats(List<(string Label, string Value)> stats) {
        stats.Add(("Points", $"{_points.Count:N0}"));
        stats.Add(("Quadrants", $"{_tree.NodeCount:N0}"));
        stats.Add(("Found", _circle ? $"{_found.Count:N0} of {_candidates:N0} in box" : $"{_found.Count:N0}"));
        stats.Add(("QuadTree search", _treeTime.Format()));
        double speedup = _bruteForceTime.Milliseconds / Math.Max(_treeTime.Milliseconds, 1e-6);
        stats.Add(("Checking every point", $"{_bruteForceTime.Format()}  ({speedup:0.0}x slower)"));
    }

    private bool InShape(Vector2 point) {
        if (_circle) return Vector2.DistanceSquared(point, _cursor) <= Radius * Radius;
        QuadTreeRect region = Region;
        return point.X >= region.Left && point.X <= region.Right && point.Y >= region.Top && point.Y <= region.Bottom;
    }

    private void Generate() {
        _points.Clear();
        _tree.Clear();

        int clusters = 7;
        for (int c = 0; c < clusters; c++) {
            Vector2 center = new(
                _world.X + 120 + _random.NextSingle() * (_world.Width - 240),
                _world.Y + 120 + _random.NextSingle() * (_world.Height - 240));
            float spread = 25 + _random.NextSingle() * 90;
            int count = 400 + _random.Next(900);
            for (int i = 0; i < count; i++) {
                AddPoint(center + Gaussian() * spread);
            }
        }
        for (int i = 0; i < 800; i++) {
            AddPoint(new Vector2(_world.X + _random.NextSingle() * _world.Width, _world.Y + _random.NextSingle() * _world.Height));
        }
    }

    private void AddPoint(Vector2 point) {
        if (_tree.Insert(point)) _points.Add(point);
    }

    private Vector2 Gaussian() {
        // Box-Muller transform.
        float u = 1 - _random.NextSingle();
        float v = _random.NextSingle();
        float magnitude = MathF.Sqrt(-2 * MathF.Log(u));
        return new Vector2(magnitude * MathF.Cos(MathF.Tau * v), magnitude * MathF.Sin(MathF.Tau * v));
    }
}
