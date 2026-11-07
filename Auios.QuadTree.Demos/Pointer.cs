using System.Numerics;
using Raylib_cs;
using static Raylib_cs.Raylib;

namespace Auios.QuadTree.Demos;

/// <summary>The mouse, or an automatic path when the mouse is outside the window so the demos keep moving.</summary>
internal static class Pointer {
    /// <summary>Ignore the real mouse (used when capturing screenshots).</summary>
    public static bool AutoPilotOnly;

    public static bool IsUser => !AutoPilotOnly && IsCursorOnScreen();

    public static Vector2 Position(QuadTreeRect world) => IsUser ? GetMousePosition() : AutoPilot(world);

    public static bool IsDown(MouseButton button) => IsUser && IsMouseButtonDown(button);

    public static bool IsPressed(MouseButton button) => IsUser && IsMouseButtonPressed(button);

    public static bool IsReleased(MouseButton button) => IsUser && IsMouseButtonReleased(button);

    public static float Wheel => IsUser ? GetMouseWheelMove() : 0;

    /// <summary>A slow Lissajous path, kept to the right of the info panel in the bottom-left corner.</summary>
    public static Vector2 AutoPilot(QuadTreeRect world) {
        float t = (float)GetTime();
        return new Vector2(
            world.X + world.Width * (0.66f + 0.24f * MathF.Sin(t * 0.43f)),
            world.CenterY + world.Height * 0.30f * MathF.Sin(t * 0.67f + 0.6f));
    }
}
