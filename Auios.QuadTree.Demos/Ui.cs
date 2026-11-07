using System.Numerics;
using Raylib_cs;
using static Raylib_cs.Raylib;

namespace Auios.QuadTree.Demos;

internal static class Ui {
    public const int TopBarHeight = 36;
    private const float BodySize = 17;
    private const float TitleSize = 22;
    private const int Padding = 14;
    private const int PanelWidth = 380;

    private static Font s_font;
    private static bool s_ownsFont;
    private static readonly List<(string Label, string Value)> s_stats = [];

    /// <summary>Uses a system UI font when one is found, so text stays sharp at any size. Falls back to raylib's built-in font.</summary>
    public static void Load() {
        string[] candidates = [
            @"C:\Windows\Fonts\segoeui.ttf",
            "/System/Library/Fonts/Supplemental/Arial.ttf",
            "/usr/share/fonts/truetype/dejavu/DejaVuSans.ttf",
        ];
        string? path = candidates.FirstOrDefault(File.Exists);
        if (path is not null) {
            s_font = LoadFontEx(path, 48, null!, 0);
            SetTextureFilter(s_font.Texture, TextureFilter.Bilinear);
            s_ownsFont = true;
        } else {
            s_font = GetFontDefault();
        }
    }

    public static void Unload() {
        if (s_ownsFont) UnloadFont(s_font);
    }

    public static void Text(string text, float x, float y, float size, Color color) =>
        DrawTextEx(s_font, text, new Vector2(MathF.Round(x), MathF.Round(y)), size, 0, color);

    public static float Measure(string text, float size) => MeasureTextEx(s_font, text, size, 0).X;

    public static void DrawTopBar(IReadOnlyList<Scene> scenes, int current) {
        int width = GetScreenWidth();
        DrawRectangle(0, 0, width, TopBarHeight, Theme.Panel);
        DrawLine(0, TopBarHeight, width, TopBarHeight, Theme.PanelBorder);

        float x = 8;
        for (int i = 0; i < scenes.Count; i++) {
            string label = $"{i + 1}  {scenes[i].Name}";
            float tabWidth = Measure(label, BodySize) + 24;
            if (i == current) {
                DrawRectangleRounded(new Rectangle(x, 5, tabWidth, TopBarHeight - 10), 0.3f, 6, Theme.TabActive);
            }
            Text(label, x + 12, 9, BodySize, i == current ? Theme.Text : Theme.TextDim);
            x += tabWidth + 4;
        }

        string right = $"{GetFPS()} FPS   G grid   H panel";
        Text(right, width - Measure(right, BodySize) - 14, 9, BodySize, Theme.TextDim);
    }

    public static void DrawInfoPanel(Scene scene) {
        s_stats.Clear();
        scene.GetStats(s_stats);
        (string Key, string Action)[] controls = scene.Controls;

        float line = BodySize + 5;
        float height = Padding + TitleSize + 6 + line          // title + summary
            + 8 + s_stats.Count * line                          // stats
            + 10 + controls.Length * line + Padding - 4;        // controls
        float x = 12;
        float y = GetScreenHeight() - height - 12;

        Rectangle panel = new(x, y, PanelWidth, height);
        DrawRectangleRounded(panel, 0.04f, 6, Theme.Panel);
        DrawRectangleRoundedLinesEx(panel, 0.04f, 6, 1, Theme.PanelBorder);

        float tx = x + Padding;
        float ty = y + Padding;
        Text(scene.Name, tx, ty, TitleSize, Theme.Text);
        ty += TitleSize + 6;
        Text(scene.Summary, tx, ty, BodySize - 1, Theme.TextDim);
        ty += line + 8;

        float valueX = x + PanelWidth - Padding;
        foreach ((string label, string value) in s_stats) {
            Text(label, tx, ty, BodySize, Theme.TextDim);
            Text(value, valueX - Measure(value, BodySize), ty, BodySize, Theme.Text);
            ty += line;
        }

        ty += 4;
        DrawLine((int)tx, (int)ty, (int)(x + PanelWidth - Padding), (int)ty, Theme.PanelBorder);
        ty += 6;
        foreach ((string key, string action) in controls) {
            Text(key, tx, ty, BodySize, Theme.Accent);
            Text(action, tx + 112, ty, BodySize, Theme.TextDim);
            ty += line;
        }
    }
}
