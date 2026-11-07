using System.Diagnostics;

namespace Auios.QuadTree.Demos;

/// <summary>A smoothed duration, so numbers in the info panel are readable.</summary>
internal struct Timing {
    private long _start;
    public double Milliseconds { get; private set; }

    public void Start() => _start = Stopwatch.GetTimestamp();

    public void Stop() {
        double sample = Stopwatch.GetElapsedTime(_start).TotalMilliseconds;
        Milliseconds = Milliseconds == 0 ? sample : Milliseconds + (sample - Milliseconds) * 0.08;
    }

    public readonly string Format() => Milliseconds >= 1 ? $"{Milliseconds:0.00} ms" : $"{Milliseconds * 1000:0} us";
}
