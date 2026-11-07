using System.Numerics;
using Raylib_cs;
using static Raylib_cs.Raylib;

namespace Auios.QuadTree.Demos.Scenes;

/// <summary>Boids: each one steers using only the neighbors the tree finds within its view radius.</summary>
internal sealed class FlockingScene : Scene, IQuadTreeObjectBounds<int> {
    private struct Boid {
        public Vector2 Position;
        public Vector2 Velocity;
    }

    private const float ViewRadius = 38;
    private const float SeparationRadius = 15;
    private const float MinSpeed = 70;
    private const float MaxSpeed = 165;
    private const float MaxForce = 260;
    private const float EdgeMargin = 70;
    private const float MouseRadius = 170;

    private static readonly int[] s_counts = [500, 1000, 2000, 4000, 8000, 16000];

    private readonly QuadTreeRect _world;
    private readonly QuadTree<int> _tree;
    private readonly List<int> _nearby = new(64);
    private readonly Random _random = new(11);
    private Boid[] _boids = [];
    private Vector2[] _steering = [];
    private int _countIndex = 2;
    private long _neighborTotal;
    private int _inspected = -1;
    private Timing _buildTime;
    private Timing _flockTime;

    public FlockingScene(QuadTreeRect world) {
        _world = world;
        _tree = new QuadTree<int>(world.X, world.Y, world.Width, world.Height, this, maxObjects: 12, maxLevel: 7);
        Spawn();
    }

    public override string Name => "Flocking";
    public override string Slug => "flocking";
    public override string Summary => "Each boid steers by its neighbors only.";
    public override (string Key, string Action)[] Controls => [
        ("Up / Down", "more / fewer boids"),
        ("Hover", "inspect a boid's neighbors"),
        ("Left drag", "attract"),
        ("Right drag", "scare"),
        ("Space", "respawn"),
    ];

    float IQuadTreeObjectBounds<int>.GetLeft(int i) => _boids[i].Position.X;
    float IQuadTreeObjectBounds<int>.GetTop(int i) => _boids[i].Position.Y;
    float IQuadTreeObjectBounds<int>.GetRight(int i) => _boids[i].Position.X;
    float IQuadTreeObjectBounds<int>.GetBottom(int i) => _boids[i].Position.Y;

    public override void Update(float dt) {
        if (IsKeyPressed(KeyboardKey.Up) && _countIndex < s_counts.Length - 1) { _countIndex++; Spawn(); }
        if (IsKeyPressed(KeyboardKey.Down) && _countIndex > 0) { _countIndex--; Spawn(); }
        if (IsKeyPressed(KeyboardKey.Space)) Spawn();

        _buildTime.Start();
        _tree.Clear();
        for (int i = 0; i < _boids.Length; i++) {
            _tree.Insert(i);
        }
        _buildTime.Stop();

        Vector2 pointer = Pointer.Position(_world);
        bool attract = Pointer.IsDown(MouseButton.Left);
        bool scare = Pointer.IsDown(MouseButton.Right);

        _flockTime.Start();
        _neighborTotal = 0;
        for (int i = 0; i < _boids.Length; i++) {
            _steering[i] = Steer(i, pointer, attract, scare);
        }
        _flockTime.Stop();

        for (int i = 0; i < _boids.Length; i++) {
            ref Boid boid = ref _boids[i];
            boid.Velocity += _steering[i] * dt;
            float speed = boid.Velocity.Length();
            if (speed > MaxSpeed) boid.Velocity *= MaxSpeed / speed;
            else if (speed < MinSpeed) boid.Velocity *= MinSpeed / MathF.Max(speed, 0.001f);
            boid.Position += boid.Velocity * dt;
            boid.Position = Vector2.Clamp(boid.Position, new Vector2(_world.Left, _world.Top), new Vector2(_world.Right, _world.Bottom));
        }

        _inspected = FindNearest(pointer, 40);
    }

    public override void Draw() {
        GridRenderer.Draw(_tree);

        float size = _boids.Length > 4000 ? 3.2f : 4.5f;
        foreach (ref readonly Boid boid in _boids.AsSpan()) {
            Vector2 forward = Vector2.Normalize(boid.Velocity);
            Vector2 side = new(-forward.Y, forward.X);
            float hue = (MathF.Atan2(forward.Y, forward.X) * (180 / MathF.PI) + 360) % 360;
            Vector2 tip = boid.Position + forward * size * 1.8f;
            Vector2 back = boid.Position - forward * size;
            // raylib wants counter-clockwise vertices on screen.
            DrawTriangle(tip, back - side * size * 0.85f, back + side * size * 0.85f, ColorFromHSV(hue, 0.5f, 0.95f));
        }

        if (_inspected >= 0) {
            Vector2 center = _boids[_inspected].Position;
            DrawCircleV(center, ViewRadius, Theme.AccentFill);
            DrawCircleLinesV(center, ViewRadius, Theme.Accent);
            QuadTreeRect view = new(center.X - ViewRadius, center.Y - ViewRadius, ViewRadius * 2, ViewRadius * 2);
            _nearby.Clear();
            _tree.FindObjects(view, _nearby);
            DrawRectangleLinesEx(GridRenderer.ToRaylib(view), 1, ColorAlpha(Theme.Accent, 0.3f));
            foreach (int j in _nearby) {
                if (j == _inspected) continue;
                Vector2 other = _boids[j].Position;
                if (Vector2.DistanceSquared(other, center) <= ViewRadius * ViewRadius) {
                    DrawLineEx(center, other, 1.5f, Theme.Found);
                }
            }
        }
    }

    public override void GetStats(List<(string Label, string Value)> stats) {
        stats.Add(("Boids", $"{_boids.Length:N0}"));
        stats.Add(("Quadrants", $"{_tree.NodeCount:N0}"));
        stats.Add(("Rebuild tree", _buildTime.Format()));
        stats.Add(("Neighbor searches", _flockTime.Format()));
        stats.Add(("Avg. neighbors", $"{(double)_neighborTotal / Math.Max(1, _boids.Length):0.0}"));
    }

    private Vector2 Steer(int i, Vector2 pointer, bool attract, bool scare) {
        Boid self = _boids[i];
        Vector2 alignment = Vector2.Zero, cohesion = Vector2.Zero, separation = Vector2.Zero;
        int neighbors = 0;
        bool crowded = false;

        _nearby.Clear();
        _tree.FindObjects(new QuadTreeRect(self.Position.X - ViewRadius, self.Position.Y - ViewRadius, ViewRadius * 2, ViewRadius * 2), _nearby);
        foreach (int j in _nearby) {
            if (j == i) continue;
            Vector2 offset = _boids[j].Position - self.Position;
            float distanceSquared = offset.LengthSquared();
            if (distanceSquared > ViewRadius * ViewRadius) continue;

            neighbors++;
            alignment += _boids[j].Velocity;
            cohesion += offset;
            if (distanceSquared < SeparationRadius * SeparationRadius) {
                // Closer neighbors push harder.
                separation -= offset / MathF.Max(distanceSquared, 0.01f);
                crowded = true;
            }
        }
        _neighborTotal += neighbors;

        // Reynolds-style steering: turn toward a desired heading at full speed, limited to MaxForce per rule.
        Vector2 steer = Vector2.Zero;
        if (neighbors > 0) {
            steer += SteerTowards(alignment, self.Velocity) * 1.0f;
            steer += SteerTowards(cohesion, self.Velocity) * 0.75f;
        }
        if (crowded) {
            steer += SteerTowards(separation, self.Velocity) * 1.7f;
        }

        // Turn away from the edges.
        Vector2 p = self.Position;
        if (p.X < _world.Left + EdgeMargin) steer.X += (_world.Left + EdgeMargin - p.X) * 9;
        if (p.X > _world.Right - EdgeMargin) steer.X -= (p.X - (_world.Right - EdgeMargin)) * 9;
        if (p.Y < _world.Top + EdgeMargin) steer.Y += (_world.Top + EdgeMargin - p.Y) * 9;
        if (p.Y > _world.Bottom - EdgeMargin) steer.Y -= (p.Y - (_world.Bottom - EdgeMargin)) * 9;

        if (attract || scare) {
            Vector2 toPointer = pointer - p;
            float distance = toPointer.Length();
            if (distance < MouseRadius * 2 && distance > 0.001f) {
                steer += toPointer / distance * (scare ? -900 : 420) * (1 - distance / (MouseRadius * 2));
            }
        }

        return steer;
    }

    private static Vector2 SteerTowards(Vector2 direction, Vector2 velocity) {
        float lengthSquared = direction.LengthSquared();
        if (lengthSquared < 1e-8f) return Vector2.Zero;
        Vector2 steer = direction / MathF.Sqrt(lengthSquared) * MaxSpeed - velocity;
        float steerLength = steer.Length();
        return steerLength > MaxForce ? steer * (MaxForce / steerLength) : steer;
    }

    private int FindNearest(Vector2 point, float radius) {
        _nearby.Clear();
        _tree.FindObjects(new QuadTreeRect(point.X - radius, point.Y - radius, radius * 2, radius * 2), _nearby);
        int best = -1;
        float bestDistance = radius * radius;
        foreach (int j in _nearby) {
            float distance = Vector2.DistanceSquared(_boids[j].Position, point);
            if (distance < bestDistance) {
                best = j;
                bestDistance = distance;
            }
        }
        return best;
    }

    private void Spawn() {
        int count = s_counts[_countIndex];
        _boids = new Boid[count];
        _steering = new Vector2[count];
        for (int i = 0; i < count; i++) {
            float angle = _random.NextSingle() * MathF.Tau;
            _boids[i] = new Boid {
                Position = new Vector2(
                    _world.X + EdgeMargin + _random.NextSingle() * (_world.Width - EdgeMargin * 2),
                    _world.Y + EdgeMargin + _random.NextSingle() * (_world.Height - EdgeMargin * 2)),
                Velocity = new Vector2(MathF.Cos(angle), MathF.Sin(angle)) * (MinSpeed + _random.NextSingle() * (MaxSpeed - MinSpeed)),
            };
        }
    }
}
