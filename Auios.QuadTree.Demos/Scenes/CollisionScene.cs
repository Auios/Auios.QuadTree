using System.Numerics;
using Raylib_cs;
using static Raylib_cs.Raylib;

namespace Auios.QuadTree.Demos.Scenes;

/// <summary>
/// Bouncing balls. The tree is cleared and rebuilt every frame, then each ball asks it for nearby balls.
/// The tree stores ball indices; this scene supplies their bounds by implementing <see cref="IQuadTreeObjectBounds{T}"/>.
/// </summary>
internal sealed class CollisionScene : Scene, IQuadTreeObjectBounds<int> {
    private struct Ball {
        public Vector2 Position;
        public Vector2 Velocity;
        public float Radius;
        public float Heat;
        public Color Color;
    }

    private static readonly int[] s_counts = [500, 1000, 2000, 4000, 8000, 16000, 32000];

    private readonly QuadTreeRect _world;
    private readonly QuadTree<int> _tree;
    private readonly List<int> _nearby = new(64);
    private readonly Random _random = new(3);
    private Ball[] _balls = [];
    private int _countIndex = 3;
    private bool _bruteForce;
    private long _pairChecks;
    private int _collisions;
    private Timing _buildTime;
    private Timing _collideTime;

    public CollisionScene(QuadTreeRect world) {
        _world = world;
        // Pad the tree by a pixel: a ball clamped flush to the right wall can round a hair past it.
        _tree = new QuadTree<int>(world.X - 1, world.Y - 1, world.Width + 2, world.Height + 2, this, maxObjects: 8, maxLevel: 7);
        Spawn();
    }

    public override string Name => "Collisions";
    public override string Slug => "collisions";
    public override string Summary => "Rebuilt every frame, then one search per ball.";
    public override (string Key, string Action)[] Controls => [
        ("Up / Down", "more / fewer balls"),
        ("B", "brute force (every pair)"),
        ("Left drag", "push balls away"),
        ("Right drag", "pull balls in"),
        ("Space", "respawn"),
    ];

    float IQuadTreeObjectBounds<int>.GetLeft(int i) => _balls[i].Position.X - _balls[i].Radius;
    float IQuadTreeObjectBounds<int>.GetTop(int i) => _balls[i].Position.Y - _balls[i].Radius;
    float IQuadTreeObjectBounds<int>.GetRight(int i) => _balls[i].Position.X + _balls[i].Radius;
    float IQuadTreeObjectBounds<int>.GetBottom(int i) => _balls[i].Position.Y + _balls[i].Radius;

    public override void Update(float dt) {
        if (IsKeyPressed(KeyboardKey.Up) && _countIndex < s_counts.Length - 1) { _countIndex++; Spawn(); }
        if (IsKeyPressed(KeyboardKey.Down) && _countIndex > 0) { _countIndex--; Spawn(); }
        if (IsKeyPressed(KeyboardKey.Space)) Spawn();
        if (IsKeyPressed(KeyboardKey.B)) _bruteForce = !_bruteForce;

        Move(dt);

        _buildTime.Start();
        _tree.Clear();
        for (int i = 0; i < _balls.Length; i++) {
            _tree.Insert(i);
        }
        _buildTime.Stop();

        ApplyMouseForce(dt);

        _collideTime.Start();
        _pairChecks = 0;
        _collisions = 0;
        if (_bruteForce) {
            for (int i = 0; i < _balls.Length; i++) {
                for (int j = i + 1; j < _balls.Length; j++) {
                    Collide(i, j);
                }
            }
        } else {
            for (int i = 0; i < _balls.Length; i++) {
                _nearby.Clear();
                _tree.FindObjects(i, _nearby);
                foreach (int j in _nearby) {
                    if (j > i) Collide(i, j);
                }
            }
        }
        _collideTime.Stop();
    }

    public override void Draw() {
        GridRenderer.Draw(_tree, highlightSearch: false);

        foreach (ref readonly Ball ball in _balls.AsSpan()) {
            Color color = ColorLerp(ball.Color, Color.White, ball.Heat * 0.85f);
            Sprites.Circle(ball.Position, ball.Radius, color);
        }

        if (Pointer.IsUser && (IsMouseButtonDown(MouseButton.Left) || IsMouseButtonDown(MouseButton.Right))) {
            DrawCircleLinesV(GetMousePosition(), MouseRadius, Theme.Accent);
        }
    }

    public override void GetStats(List<(string Label, string Value)> stats) {
        long allPairs = (long)_balls.Length * (_balls.Length - 1) / 2;
        stats.Add(("Balls", $"{_balls.Length:N0}"));
        stats.Add(("Mode", _bruteForce ? "brute force" : "QuadTree"));
        stats.Add(("Rebuild tree", _buildTime.Format()));
        stats.Add(("Collision pass", _collideTime.Format()));
        stats.Add(("Pairs checked", $"{_pairChecks:N0}"));
        stats.Add(("Every pair would be", $"{allPairs:N0}"));
        stats.Add(("Touching pairs", $"{_collisions:N0}"));
    }

    private const float MouseRadius = 140;

    private void ApplyMouseForce(float dt) {
        bool push = Pointer.IsDown(MouseButton.Left);
        bool pull = Pointer.IsDown(MouseButton.Right);
        if (!push && !pull) return;

        Vector2 mouse = GetMousePosition();
        _nearby.Clear();
        _tree.FindObjects(new QuadTreeRect(mouse.X - MouseRadius, mouse.Y - MouseRadius, MouseRadius * 2, MouseRadius * 2), _nearby);
        foreach (int i in _nearby) {
            ref Ball ball = ref _balls[i];
            Vector2 offset = ball.Position - mouse;
            float distance = offset.Length();
            if (distance > MouseRadius || distance < 0.001f) continue;
            float strength = (1 - distance / MouseRadius) * 2600 * dt;
            ball.Velocity += offset / distance * (push ? strength : -strength);
        }
    }

    private void Collide(int i, int j) {
        _pairChecks++;
        ref Ball a = ref _balls[i];
        ref Ball b = ref _balls[j];
        Vector2 delta = b.Position - a.Position;
        float minDistance = a.Radius + b.Radius;
        float distanceSquared = delta.LengthSquared();
        if (distanceSquared >= minDistance * minDistance || distanceSquared < 1e-8f) return;

        _collisions++;
        float distance = MathF.Sqrt(distanceSquared);
        Vector2 normal = delta / distance;
        float massA = a.Radius * a.Radius;
        float massB = b.Radius * b.Radius;
        float inverseTotal = 1 / (massA + massB);

        // Separate the balls, heavier ones move less.
        float overlap = minDistance - distance;
        a.Position -= normal * (overlap * massB * inverseTotal);
        b.Position += normal * (overlap * massA * inverseTotal);

        // Elastic bounce along the contact normal.
        float approach = Vector2.Dot(b.Velocity - a.Velocity, normal);
        if (approach < 0) {
            float impulse = -1.9f * approach * massA * massB * inverseTotal;
            a.Velocity -= normal * (impulse / massA);
            b.Velocity += normal * (impulse / massB);
        }

        a.Heat = 1;
        b.Heat = 1;
    }

    private void Move(float dt) {
        float left = _world.Left, top = _world.Top, right = _world.Right, bottom = _world.Bottom;
        for (int i = 0; i < _balls.Length; i++) {
            ref Ball ball = ref _balls[i];
            ball.Velocity *= MathF.Pow(0.995f, dt * 60);
            ball.Position += ball.Velocity * dt;
            ball.Heat = MathF.Max(0, ball.Heat - dt * 3);

            // Keep balls fully inside the world so the tree always accepts them.
            float r = ball.Radius;
            if (ball.Position.X < left + r) { ball.Position.X = left + r; ball.Velocity.X = MathF.Abs(ball.Velocity.X); }
            if (ball.Position.X > right - r) { ball.Position.X = right - r; ball.Velocity.X = -MathF.Abs(ball.Velocity.X); }
            if (ball.Position.Y < top + r) { ball.Position.Y = top + r; ball.Velocity.Y = MathF.Abs(ball.Velocity.Y); }
            if (ball.Position.Y > bottom - r) { ball.Position.Y = bottom - r; ball.Velocity.Y = -MathF.Abs(ball.Velocity.Y); }
        }
    }

    private void Spawn() {
        int count = s_counts[_countIndex];
        // Pick a size so the balls cover about a fifth of the screen.
        float averageRadius = MathF.Sqrt(0.2f * _world.Width * _world.Height / (count * MathF.PI));
        _balls = new Ball[count];
        for (int i = 0; i < count; i++) {
            float radius = averageRadius * (0.55f + _random.NextSingle() * 0.9f);
            float angle = _random.NextSingle() * MathF.Tau;
            float speed = 40 + _random.NextSingle() * 120;
            _balls[i] = new Ball {
                Radius = radius,
                Position = new Vector2(
                    _world.X + radius + _random.NextSingle() * (_world.Width - radius * 2),
                    _world.Y + radius + _random.NextSingle() * (_world.Height - radius * 2)),
                Velocity = new Vector2(MathF.Cos(angle), MathF.Sin(angle)) * speed,
                Color = ColorFromHSV(190 + _random.NextSingle() * 70, 0.55f, 0.75f + _random.NextSingle() * 0.2f),
            };
        }
    }
}
