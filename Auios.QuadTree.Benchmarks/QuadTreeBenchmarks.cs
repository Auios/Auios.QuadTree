using Auios.QuadTree;
using BenchmarkDotNet.Attributes;

namespace Auios.QuadTree.Benchmarks;

public sealed class Entity(float x, float y, float size) {
    public float X = x;
    public float Y = y;
    public float Size = size;
}

public sealed class EntityBounds : IQuadTreeObjectBounds<Entity> {
    public float GetLeft(Entity e) => e.X;
    public float GetTop(Entity e) => e.Y;
    public float GetRight(Entity e) => e.X + e.Size;
    public float GetBottom(Entity e) => e.Y + e.Size;
}

[MemoryDiagnoser]
public class QuadTreeBenchmarks {
    private const float World = 4096;

    [Params(10_000, 100_000)]
    public int Count;

    [Params(5, 8)]
    public int MaxLevel;

    private Entity[] _entities = [];
    private QuadTreeRect[] _smallQueries = [];
    private QuadTreeRect[] _largeQueries = [];
    private QuadTree<Entity> _tree = null!;
    private readonly List<Entity> _results = new(1 << 16);

    [GlobalSetup]
    public void Setup() {
        Random random = new(42);
        _entities = new Entity[Count];
        for (int i = 0; i < Count; i++) {
            float size = 1 + random.NextSingle() * 8;
            _entities[i] = new Entity(random.NextSingle() * (World - size), random.NextSingle() * (World - size), size);
        }
        _smallQueries = Enumerable.Range(0, 1000)
            .Select(_ => new QuadTreeRect(random.NextSingle() * World, random.NextSingle() * World, 32, 32)).ToArray();
        _largeQueries = Enumerable.Range(0, 20)
            .Select(_ => new QuadTreeRect(random.NextSingle() * World * 0.5f, random.NextSingle() * World * 0.5f, 1024, 1024)).ToArray();
        _tree = new QuadTree<Entity>(World, World, new EntityBounds(), maxObjects: 10, MaxLevel);
        _tree.InsertRange(_entities);
    }

    [Benchmark(Description = "Build a new tree")]
    public int Build() {
        QuadTree<Entity> tree = new(World, World, new EntityBounds(), maxObjects: 10, MaxLevel);
        tree.InsertRange(_entities);
        return tree.Count();
    }

    [Benchmark(Description = "Clear + rebuild")]
    public int Rebuild() {
        _tree.Clear();
        foreach (Entity entity in _entities) _tree.Insert(entity);
        return _tree.Count();
    }

    [Benchmark(Description = "1000 small searches")]
    public int SmallSearches() {
        int found = 0;
        foreach (QuadTreeRect query in _smallQueries) {
            _results.Clear();
            _tree.FindObjects(query, _results);
            found += _results.Count;
        }
        return found;
    }

    [Benchmark(Description = "20 large searches")]
    public int LargeSearches() {
        int found = 0;
        foreach (QuadTreeRect query in _largeQueries) {
            _results.Clear();
            _tree.FindObjects(query, _results);
            found += _results.Count;
        }
        return found;
    }

    [Benchmark(Description = "Search around every object")]
    public int SearchAroundEveryObject() {
        int found = 0;
        foreach (Entity entity in _entities) {
            _results.Clear();
            _tree.FindObjects(entity, _results);
            found += _results.Count;
        }
        return found;
    }
}
