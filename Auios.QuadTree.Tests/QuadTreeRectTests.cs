using Auios.QuadTree;

namespace Auios.QuadTree.Tests;

public class QuadTreeRectTests {
    [Fact]
    public void ComputedEdges_MatchPositionAndSize() {
        QuadTreeRect rect = new(10, 20, 30, 40);

        Assert.Equal(10, rect.Left);
        Assert.Equal(20, rect.Top);
        Assert.Equal(40, rect.Right);
        Assert.Equal(60, rect.Bottom);
        Assert.Equal(15, rect.HalfWidth);
        Assert.Equal(20, rect.HalfHeight);
        Assert.Equal(25, rect.CenterX);
        Assert.Equal(40, rect.CenterY);
    }
}
