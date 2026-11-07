using System.Numerics;
using Raylib_cs;
using static Raylib_cs.Raylib;

namespace Auios.QuadTree.Demos.Scenes;

/// <summary>
/// RTS-style box selection over objects of very different sizes. The tree is built once and kept up to date
/// with <see cref="QuadTree{T}.Remove"/> and <see cref="QuadTree{T}.Insert"/> as objects are moved or deleted.
/// </summary>
internal sealed class SelectionScene : Scene {
    private sealed class Unit {
        public float X, Y, Width, Height;
        public Color Color;
        public bool Selected;
        public float Area => Width * Height;
    }

    private sealed class UnitBounds : IQuadTreeObjectBounds<Unit> {
        public float GetLeft(Unit unit) => unit.X;
        public float GetTop(Unit unit) => unit.Y;
        public float GetRight(Unit unit) => unit.X + unit.Width;
        public float GetBottom(Unit unit) => unit.Y + unit.Height;
    }

    private readonly QuadTreeRect _world;
    private readonly QuadTree<Unit> _tree;
    private readonly List<Unit> _units = [];
    private readonly List<Unit> _hits = [];
    private readonly Random _random = new(5);

    private Vector2 _dragStart;
    private bool _dragging;
    private bool _containedOnly;
    private Unit? _hover;
    private Vector2 _lastMouse;
    private int _selectedCount;
    private QuadTreeRect? _box;
    private Timing _searchTime;

    public SelectionScene(QuadTreeRect world) {
        _world = world;
        // Pad the tree by a pixel: a unit clamped flush to the right edge can round a hair past it.
        _tree = new QuadTree<Unit>(world.X - 1, world.Y - 1, world.Width + 2, world.Height + 2, new UnitBounds(), maxObjects: 6, maxLevel: 8);
        Generate();
    }

    public override string Name => "Selection";
    public override string Slug => "selection";
    public override string Summary => "Built once, then updated with Remove + Insert.";
    public override (string Key, string Action)[] Controls => [
        ("Left drag", "box select (Shift adds)"),
        ("Right drag", "move the selection"),
        ("C", "touching / fully inside"),
        ("Delete", "remove the selection"),
        ("Space", "new map"),
    ];

    public override void Update(float dt) {
        if (IsKeyPressed(KeyboardKey.Space)) Generate();
        if (IsKeyPressed(KeyboardKey.C)) _containedOnly = !_containedOnly;
        if (IsKeyPressed(KeyboardKey.Delete) || IsKeyPressed(KeyboardKey.Backspace)) DeleteSelection();

        Vector2 mouse = Pointer.Position(_world);

        if (Pointer.IsPressed(MouseButton.Left)) {
            _dragStart = mouse;
            _dragging = true;
        }

        if (Pointer.IsDown(MouseButton.Right)) {
            MoveSelection(mouse - _lastMouse);
        }
        _lastMouse = mouse;

        // Hover: a zero-size search is a point pick. Prefer the smallest unit, which is drawn on top.
        _hits.Clear();
        _tree.FindObjects(new QuadTreeRect(mouse.X, mouse.Y, 0, 0), _hits);
        _hover = _hits.Count == 0 ? null : _hits.MinBy(u => u.Area);

        // Box selection. When the mouse is outside the window a box follows the automatic pointer instead.
        _box = null;
        if (_dragging) {
            _box = Normalize(_dragStart, mouse);
        } else if (!Pointer.IsUser) {
            float t = (float)GetTime();
            Vector2 half = new(120 + 40 * MathF.Sin(t * 0.9f), 80 + 25 * MathF.Cos(t * 0.7f));
            _box = Normalize(mouse - half, mouse + half);
        }

        _hits.Clear();
        if (_box is { } region) {
            _searchTime.Start();
            _tree.FindObjects(region, _hits);
            if (_containedOnly) {
                _hits.RemoveAll(u => u.X < region.Left || u.X + u.Width > region.Right || u.Y < region.Top || u.Y + u.Height > region.Bottom);
            }
            _searchTime.Stop();
        }

        if (_dragging && Pointer.IsReleased(MouseButton.Left)) {
            _dragging = false;
            bool additive = IsKeyDown(KeyboardKey.LeftShift) || IsKeyDown(KeyboardKey.RightShift);
            bool click = Vector2.Distance(_dragStart, mouse) < 4;
            if (!additive) {
                foreach (Unit unit in _units) unit.Selected = false;
            }
            if (click) {
                if (_hover is not null) _hover.Selected = true;
            } else {
                foreach (Unit unit in _hits) unit.Selected = true;
            }
            _hits.Clear();
        }

        _selectedCount = _units.Count(u => u.Selected);
    }

    public override void Draw() {
        GridRenderer.Draw(_tree, highlightSearch: false, depthColors: true);

        foreach (Unit unit in _units) {
            Rectangle rect = new(unit.X, unit.Y, unit.Width, unit.Height);
            if (unit.Selected) {
                DrawRectangleRec(rect, ColorAlpha(Theme.Found, 0.55f));
                DrawRectangleLinesEx(rect, 1, Theme.Found);
            } else {
                DrawRectangleRec(rect, ColorAlpha(unit.Color, 0.16f));
                DrawRectangleLinesEx(rect, 1, ColorAlpha(unit.Color, 0.7f));
            }
        }
        foreach (Unit unit in _hits) {
            Rectangle rect = new(unit.X, unit.Y, unit.Width, unit.Height);
            DrawRectangleRec(rect, ColorAlpha(Theme.Accent, 0.35f));
            DrawRectangleLinesEx(rect, 1, Theme.Accent);
        }
        if (_hover is not null) {
            DrawRectangleLinesEx(new Rectangle(_hover.X - 2, _hover.Y - 2, _hover.Width + 4, _hover.Height + 4), 2, Color.White);
        }

        if (_box is { } region) {
            DrawRectangleRec(GridRenderer.ToRaylib(region), Theme.AccentFill);
            DrawRectangleLinesEx(GridRenderer.ToRaylib(region), 1.5f, Theme.Accent);
        }
    }

    public override void GetStats(List<(string Label, string Value)> stats) {
        stats.Add(("Units", $"{_units.Count:N0}"));
        stats.Add(("Quadrants", $"{_tree.NodeCount:N0}"));
        stats.Add(("Box selects", _containedOnly ? "fully inside" : "touching"));
        stats.Add(("In box", $"{_hits.Count:N0}"));
        stats.Add(("Selected", $"{_selectedCount:N0}"));
        stats.Add(("Box search", _searchTime.Format()));
    }

    private void MoveSelection(Vector2 delta) {
        if (delta == Vector2.Zero) return;
        foreach (Unit unit in _units) {
            if (!unit.Selected) continue;
            // Remove while the tree still knows the old bounds, then insert at the new position.
            _tree.Remove(unit);
            unit.X = Math.Clamp(unit.X + delta.X, _world.Left, _world.Right - unit.Width);
            unit.Y = Math.Clamp(unit.Y + delta.Y, _world.Top, _world.Bottom - unit.Height);
            _tree.Insert(unit);
        }
    }

    private void DeleteSelection() {
        foreach (Unit unit in _units) {
            if (unit.Selected) _tree.Remove(unit);
        }
        _units.RemoveAll(u => u.Selected);
    }

    private void Generate() {
        _units.Clear();
        _tree.Clear();
        for (int i = 0; i < 2500; i++) {
            // Mostly small units, some medium, a few large buildings that straddle quadrant lines.
            float roll = _random.NextSingle();
            float size = roll < 0.85f ? 4 + _random.NextSingle() * 10
                : roll < 0.985f ? 18 + _random.NextSingle() * 26
                : 60 + _random.NextSingle() * 80;
            float width = size * (0.7f + _random.NextSingle() * 0.6f);
            float height = size * (0.7f + _random.NextSingle() * 0.6f);
            Unit unit = new() {
                Width = width,
                Height = height,
                X = _world.X + _random.NextSingle() * (_world.Width - width),
                Y = _world.Y + _random.NextSingle() * (_world.Height - height),
                Color = ColorFromHSV(roll < 0.85f ? 150 + _random.NextSingle() * 40 : roll < 0.97f ? 30 + _random.NextSingle() * 20 : 280, 0.5f, 0.85f),
            };
            _units.Add(unit);
            _tree.Insert(unit);
        }
        // Draw large units first so small ones stay visible on top.
        _units.Sort((a, b) => b.Area.CompareTo(a.Area));
    }

    private static QuadTreeRect Normalize(Vector2 a, Vector2 b) {
        Vector2 min = Vector2.Min(a, b);
        Vector2 max = Vector2.Max(a, b);
        return new QuadTreeRect(min.X, min.Y, max.X - min.X, max.Y - min.Y);
    }
}
