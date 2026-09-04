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
        Assert.Same(a, found[0]);
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
}
