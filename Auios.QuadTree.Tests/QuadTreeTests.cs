using System.Numerics;
using Auios.QuadTree;

namespace Auios.QuadTree.Tests;

public class QuadTreeTests {
    [Fact]
    public void Insert_And_Count_TracksObjects() {
        QuadTree<Vector2> tree = new(100, 100, new PointBounds());

        Assert.True(tree.Insert(new Vector2(10, 10)));
        Assert.True(tree.Insert(new Vector2(50, 50)));
        Assert.Equal(2, tree.Count());
    }

    [Fact]
    public void Insert_RejectsObjectsOutsideBounds() {
        QuadTree<Vector2> tree = new(100, 100, new PointBounds());

        Assert.False(tree.Insert(new Vector2(150, 50)));
        Assert.Equal(0, tree.Count());
    }

    [Fact]
    public void InsertRange_AddsAllObjects() {
        QuadTree<Vector2> tree = new(100, 100, new PointBounds());
        Vector2[] points = [new(1, 1), new(2, 2), new(3, 3)];

        tree.InsertRange(points);

        Assert.Equal(3, tree.Count());
    }

    [Fact]
    public void Clear_RemovesAllObjectsAndChildren() {
        QuadTree<Vector2> tree = new(100, 100, new PointBounds(), maxObjects: 2);
        tree.InsertRange([new(10, 10), new(20, 20), new(30, 30)]);

        tree.Clear();

        Assert.Equal(0, tree.Count());
        Assert.Single(tree.GetGrid());
    }

    [Fact]
    public void Insert_SubdividesWhenExceedingMaxObjects() {
        QuadTree<Vector2> tree = new(100, 100, new PointBounds(), maxObjects: 2);
        tree.InsertRange([
            new(10, 10),
            new(20, 20),
            new(80, 80),
        ]);

        Assert.Equal(3, tree.Count());
        Assert.True(tree.GetGrid().Length > 1);
    }

    [Fact]
    public void FindObjects_ReturnsOnlyIntersectingObjects() {
        QuadTree<Vector2> tree = new(100, 100, new PointBounds(), maxObjects: 1);
        Vector2 inside = new(15, 15);
        Vector2 outside = new(80, 80);
        tree.Insert(inside);
        tree.Insert(outside);

        Vector2[] found = tree.FindObjects(new QuadTreeRect(0, 0, 30, 30));

        Assert.Contains(inside, found);
        Assert.DoesNotContain(outside, found);
    }

    [Fact]
    public void FindObjects_WithNegativeOrigin_ReturnsOnlyOutsideStripObject() {
        // Regression for https://github.com/Auios/Auios.QuadTree/issues/3
        QuadTree<Vector2> tree = new(
            new Vector2(-200, -200),
            new Vector2(2200f, 1280f),
            new PointBounds(),
            maxObjects: 10);

        List<Vector2> onScreen = [];
        for (int i = 0; i < 100; i++) {
            onScreen.Add(new Vector2(100 + i, 100 + (i % 50)));
        }
        tree.InsertRange(onScreen);

        Vector2 outside = new(-100, 100);
        tree.Insert(outside);

        Vector2[] found = tree.FindObjects(new QuadTreeRect(-250, 0, 200, 1080));

        Assert.Single(found);
        Assert.Equal(outside, found[0]);
    }

    [Fact]
    public void FindObjects_WithObjectBounds_UsesWidthAndHeight() {
        QuadTree<QuadTreeRect> tree = new(200, 200, new RectBounds());
        QuadTreeRect a = new(10, 10, 20, 20);
        QuadTreeRect b = new(100, 100, 20, 20);
        tree.Insert(a);
        tree.Insert(b);

        QuadTreeRect query = new(15, 15, 5, 5);
        QuadTreeRect[] found = tree.FindObjects(query);

        Assert.Single(found);
        Assert.Equal(a, found[0]);
    }

    [Fact]
    public void Insert_RetainsObjectsThatSpanChildQuadrants() {
        QuadTree<QuadTreeRect> tree = new(100, 100, new RectBounds(), maxObjects: 1);
        QuadTreeRect filler = new(10, 10, 5, 5);
        QuadTreeRect spanning = new(40, 40, 30, 30); // crosses the center split at (50,50)

        Assert.True(tree.Insert(filler));
        Assert.True(tree.Insert(spanning));

        Assert.Equal(2, tree.Count());
        Assert.Contains(spanning, tree.FindObjects(new QuadTreeRect(0, 0, 100, 100)));
    }

    [Fact]
    public void GetGrid_IncludesRootArea() {
        QuadTree<Vector2> tree = new(80, 60, new PointBounds());
        QuadTreeRect[] grid = tree.GetGrid();

        Assert.Single(grid);
        Assert.Equal(0, grid[0].X);
        Assert.Equal(0, grid[0].Y);
        Assert.Equal(80, grid[0].Width);
        Assert.Equal(60, grid[0].Height);
    }

    [Fact]
    public void FindObjects_SetsIsOverlappedOnMatchingNodes() {
        QuadTree<Vector2> tree = new(100, 100, new PointBounds());
        tree.Insert(new Vector2(10, 10));

        _ = tree.FindObjects(new QuadTreeRect(0, 0, 50, 50));

        Assert.True(tree.Area.IsOverlapped);
    }

    [Fact]
    public void Insert_KeepsDuplicateValues() {
        QuadTree<Vector2> tree = new(100, 100, new PointBounds());
        Vector2 point = new(25, 25);

        tree.Insert(point);
        tree.Insert(point);

        Assert.Equal(2, tree.Count());
        Assert.Equal(2, tree.FindObjects(new QuadTreeRect(20, 20, 10, 10)).Length);
    }

    [Fact]
    public void Insert_AcceptsObjectsOnTheOuterEdge() {
        QuadTree<Vector2> tree = new(100, 100, new PointBounds());

        Assert.True(tree.Insert(new Vector2(0, 0)));
        Assert.True(tree.Insert(new Vector2(100, 100)));
        Assert.Equal(2, tree.Count());
    }

    [Fact]
    public void Insert_RejectsNaNBounds() {
        QuadTree<Vector2> tree = new(100, 100, new PointBounds());

        Assert.False(tree.Insert(new Vector2(float.NaN, 10)));
        Assert.Equal(0, tree.Count());
    }

    [Fact]
    public void FindObjects_WithNaNRegion_ReturnsNothing() {
        QuadTree<Vector2> tree = new(100, 100, new PointBounds());
        tree.Insert(new Vector2(10, 10));

        Assert.Empty(tree.FindObjects(new QuadTreeRect(float.NaN, 0, 100, 100)));
    }

    [Fact]
    public void FindObjects_IncludesObjectsTouchingTheRegionEdge() {
        QuadTree<Vector2> tree = new(100, 100, new PointBounds(), maxObjects: 1);
        tree.InsertRange([new(30, 30), new(31, 31), new(70, 70)]);

        Vector2[] found = tree.FindObjects(new QuadTreeRect(0, 0, 30, 30));

        Assert.Equal([new Vector2(30, 30)], found);
    }

    [Fact]
    public void FindObjects_WithList_AppendsWithoutClearing() {
        QuadTree<Vector2> tree = new(100, 100, new PointBounds());
        tree.Insert(new Vector2(10, 10));
        List<Vector2> results = [new Vector2(-1, -1)];

        tree.FindObjects(new QuadTreeRect(0, 0, 50, 50), results);

        Assert.Equal([new Vector2(-1, -1), new Vector2(10, 10)], results);
    }

    [Fact]
    public void FindObjects_ByObjectBounds_UsesExactEdges() {
        QuadTree<Box> tree = new(1000, 1000, new BoxBounds());
        Box a = new(100.1f, 100.1f, 0.3f, 0.3f);
        Box touching = new(100.4f, 100.4f, 1, 1);
        tree.Insert(a);
        tree.Insert(touching);

        Box[] found = tree.FindObjects(a);

        Assert.Contains(a, found);
        Assert.Contains(touching, found);
    }

    [Fact]
    public void FindObjects_OnlyFlagsQuadrantsTouchedByTheLatestSearch() {
        QuadTree<Vector2> tree = new(100, 100, new PointBounds(), maxObjects: 1);
        tree.InsertRange([new(10, 10), new(90, 10), new(10, 90), new(90, 90)]);

        tree.FindObjects(new QuadTreeRect(0, 0, 20, 20));
        tree.FindObjects(new QuadTreeRect(80, 80, 20, 20));
        QuadTreeRect[] grid = tree.GetGrid();

        Assert.Equal(5, grid.Length);
        Assert.True(grid[0].IsOverlapped);
        QuadTreeRect[] flaggedChildren = grid.Skip(1).Where(r => r.IsOverlapped).ToArray();
        Assert.Single(flaggedChildren);
        Assert.Equal(new QuadTreeRect(50, 50, 50, 50) { IsOverlapped = true }, flaggedChildren[0]);
    }

    [Fact]
    public void Clear_ThenRebuild_ProducesTheSameResults() {
        QuadTree<Vector2> tree = new(100, 100, new PointBounds(), maxObjects: 2);
        Vector2[] points = [new(10, 10), new(20, 20), new(80, 80), new(85, 15), new(60, 60)];
        tree.InsertRange(points);
        Vector2[] before = tree.FindObjects(new QuadTreeRect(0, 0, 50, 50));
        int nodesBefore = tree.NodeCount;

        tree.Clear();
        Assert.Equal(1, tree.NodeCount);
        Assert.False(tree.Area.IsOverlapped);
        tree.InsertRange(points);

        Assert.Equal(points.Length, tree.Count());
        Assert.Equal(nodesBefore, tree.NodeCount);
        Assert.Equal(before.OrderBy(p => p.X), tree.FindObjects(new QuadTreeRect(0, 0, 50, 50)).OrderBy(p => p.X));
    }

    [Fact]
    public void Remove_RemovesOneOccurrence() {
        QuadTree<Vector2> tree = new(100, 100, new PointBounds(), maxObjects: 1);
        tree.InsertRange([new(10, 10), new(10, 10), new(90, 90)]);

        Assert.True(tree.Remove(new Vector2(10, 10)));

        Assert.Equal(2, tree.Count());
        Assert.Single(tree.FindObjects(new QuadTreeRect(0, 0, 20, 20)));
    }

    [Fact]
    public void Remove_ReturnsFalseWhenMissing() {
        QuadTree<Vector2> tree = new(100, 100, new PointBounds());
        tree.Insert(new Vector2(10, 10));

        Assert.False(tree.Remove(new Vector2(11, 11)));
        Assert.False(tree.Remove(new Vector2(500, 500)));
        Assert.Equal(1, tree.Count());
    }

    [Fact]
    public void Remove_FindsObjectsThatMovedSinceInsert() {
        QuadTree<Box> tree = new(100, 100, new BoxBounds(), maxObjects: 1);
        Box moving = new(10, 10, 1, 1);
        tree.Insert(moving);
        tree.Insert(new Box(90, 90, 1, 1));
        tree.Insert(new Box(60, 10, 1, 1));

        moving.X = 95;
        moving.Y = 5;

        Assert.True(tree.Remove(moving));
        Assert.Equal(2, tree.Count());
        Assert.DoesNotContain(moving, tree.FindObjects(new QuadTreeRect(0, 0, 100, 100)));
    }

    [Fact]
    public void Constructor_ValidatesArguments() {
        Assert.Throws<ArgumentNullException>(() => new QuadTree<Vector2>(100, 100, null!));
        Assert.Throws<ArgumentOutOfRangeException>(() => new QuadTree<Vector2>(100, 100, new PointBounds(), maxObjects: -1));
    }

    [Fact]
    public void Insert_StopsSplittingAtMaxLevel() {
        QuadTree<Vector2> tree = new(100, 100, new PointBounds(), maxObjects: 1, maxLevel: 2);
        for (int i = 0; i < 50; i++) {
            tree.Insert(new Vector2(1 + i * 0.01f, 1));
        }

        Assert.Equal(50, tree.Count());
        Assert.Equal(1 + 4 + 4, tree.NodeCount);
    }
}
