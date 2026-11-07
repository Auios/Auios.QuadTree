using Raylib_cs;
using static Raylib_cs.Raylib;

namespace Auios.QuadTree.Demos;

internal static class GridRenderer {
    private static readonly List<QuadTreeRect> s_grid = [];

    /// <summary>Draws every quadrant. Quadrants touched by the latest search are highlighted.</summary>
    /// <param name="depthColors">Tint quadrants by depth instead of a single grid color.</param>
    public static void Draw<T>(QuadTree<T> tree, bool highlightSearch = true, bool depthColors = false) {
        if (!Settings.ShowGrid) return;

        s_grid.Clear();
        tree.GetGrid(s_grid);
        float rootWidth = s_grid[0].Width;

        foreach (QuadTreeRect rect in s_grid) {
            if (highlightSearch && rect.IsOverlapped) continue;
            Color color = Theme.Grid;
            if (depthColors) {
                int depth = (int)MathF.Round(MathF.Log2(rootWidth / rect.Width));
                color = ColorFromHSV(215 + depth * 12, 0.35f, 0.2f + depth * 0.035f);
            }
            DrawRectangleLinesEx(ToRaylib(rect), 1, color);
        }

        if (!highlightSearch) return;

        // Skip the root fill so the whole screen is not tinted. Nested quadrants stack, so deeper ones glow more.
        for (int i = 1; i < s_grid.Count; i++) {
            if (s_grid[i].IsOverlapped) DrawRectangleRec(ToRaylib(s_grid[i]), Theme.OverlappedFill);
        }
        foreach (QuadTreeRect rect in s_grid) {
            if (rect.IsOverlapped) DrawRectangleLinesEx(ToRaylib(rect), 1, ColorAlpha(Theme.Overlapped, 0.75f));
        }
    }

    public static Rectangle ToRaylib(QuadTreeRect rect) => new(rect.X, rect.Y, rect.Width, rect.Height);
}
