using Auios.QuadTree;

namespace Auios.QuadTree.Tests;

/// <summary>Compares the QuadTree against a brute-force scan over random data.</summary>
public class QuadTreeRandomizedTests {
    public static TheoryData<int, int, int, bool> Configurations => new() {
        { 1, 0, 1, false },
        { 1, 6, 2, false },
        { 4, 8, 3, true },
        { 10, 5, 4, true },
        { 32, 12, 5, true },
        { 0, 4, 6, true },
    };

    [Theory]
    [MemberData(nameof(Configurations))]
    public void FindObjects_MatchesBruteForce(int maxObjects, int maxLevel, int seed, bool sizedObjects) {
        Random random = new(seed);
        QuadTreeRect world = new(-500, -250, 1000, 500);
        QuadTree<Box> tree = new(world.X, world.Y, world.Width, world.Height, new BoxBounds(), maxObjects, maxLevel);
        List<Box> boxes = [];

        for (int i = 0; i < 2000; i++) {
            Box box = RandomBox(random, world, sizedObjects ? 60 : 0);
            boxes.Add(box);
            Assert.True(tree.Insert(box));
        }

        Assert.Equal(boxes.Count, tree.Count());
        List<Box> results = [];
        for (int q = 0; q < 300; q++) {
            QuadTreeRect query = RandomQuery(random, world);
            results.Clear();
            tree.FindObjects(query, results);
            AssertSameObjects(BruteForce(boxes, query), results);
        }
    }

    [Theory]
    [MemberData(nameof(Configurations))]
    public void RemoveAndRebuild_MatchBruteForce(int maxObjects, int maxLevel, int seed, bool sizedObjects) {
        Random random = new(seed);
        QuadTreeRect world = new(0, 0, 800, 600);
        QuadTree<Box> tree = new(world.Width, world.Height, new BoxBounds(), maxObjects, maxLevel);
        List<Box> boxes = [];
        for (int i = 0; i < 1000; i++) {
            Box box = RandomBox(random, world, sizedObjects ? 40 : 0);
            boxes.Add(box);
            tree.Insert(box);
        }

        // Remove half of them, some after they have moved.
        for (int i = boxes.Count - 1; i >= 0; i -= 2) {
            Box box = boxes[i];
            if (i % 3 == 0) {
                box.X = random.NextSingle() * 700;
            }
            Assert.True(tree.Remove(box));
            boxes.RemoveAt(i);
        }

        Assert.Equal(boxes.Count, tree.Count());
        List<Box> results = [];
        for (int q = 0; q < 100; q++) {
            QuadTreeRect query = RandomQuery(random, world);
            results.Clear();
            tree.FindObjects(query, results);
            AssertSameObjects(BruteForce(boxes, query), results);
        }

        // Rebuilding into the same tree must give the same answers.
        tree.Clear();
        foreach (Box box in boxes) {
            box.Y = Math.Clamp(box.Y + random.NextSingle() * 20 - 10, 0, world.Height - box.Height);
            tree.Insert(box);
        }
        for (int q = 0; q < 100; q++) {
            QuadTreeRect query = RandomQuery(random, world);
            results.Clear();
            tree.FindObjects(query, results);
            AssertSameObjects(BruteForce(boxes, query), results);
        }
    }

    private static Box RandomBox(Random random, QuadTreeRect world, float maxSize) {
        float width = random.NextSingle() * maxSize;
        float height = random.NextSingle() * maxSize;
        // Snap some boxes to a coarse grid so many of them sit exactly on split lines.
        float x = world.X + random.NextSingle() * (world.Width - width);
        float y = world.Y + random.NextSingle() * (world.Height - height);
        if (random.Next(4) == 0) {
            x = MathF.Min(MathF.Round(x / 125) * 125, world.Right - width);
            y = MathF.Min(MathF.Round(y / 125) * 125, world.Bottom - height);
        }
        return new Box(x, y, width, height);
    }

    private static QuadTreeRect RandomQuery(Random random, QuadTreeRect world) {
        float width = random.Next(3) switch {
            0 => random.NextSingle() * 20,
            1 => random.NextSingle() * 200,
            _ => random.NextSingle() * world.Width * 1.5f,
        };
        float height = width * (0.5f + random.NextSingle());
        float x = world.X - width * 0.5f + random.NextSingle() * world.Width;
        float y = world.Y - height * 0.5f + random.NextSingle() * world.Height;
        return new QuadTreeRect(x, y, width, height);
    }

    private static List<Box> BruteForce(List<Box> boxes, QuadTreeRect query) =>
        boxes.Where(b => b.X <= query.Right && b.X + b.Width >= query.Left && b.Y <= query.Bottom && b.Y + b.Height >= query.Top).ToList();

    private static void AssertSameObjects(List<Box> expected, List<Box> actual) {
        Assert.Equal(expected.Count, actual.Count);
        HashSet<Box> expectedSet = new(expected, ReferenceEqualityComparer.Instance);
        Assert.All(actual, box => Assert.Contains(box, expectedSet));
    }
}
