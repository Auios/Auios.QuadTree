namespace Auios.QuadTree.Demos;

/// <summary>One demo. Scenes keep their state while another scene is shown.</summary>
internal abstract class Scene {
    public abstract string Name { get; }
    public abstract string Slug { get; }
    public abstract string Summary { get; }
    public abstract (string Key, string Action)[] Controls { get; }

    public abstract void Update(float dt);
    public abstract void Draw();

    /// <summary>Adds label/value lines shown in the info panel.</summary>
    public abstract void GetStats(List<(string Label, string Value)> stats);
}

internal static class Settings {
    public static bool ShowGrid = true;
}
