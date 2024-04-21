using System.Diagnostics;

using Serilog;

namespace Dalamud.Utility;

/// <summary>
/// Utility class to detect hitches.
/// </summary>
public class HitchDetector
{
    private readonly TimeSpan cooldownTime = TimeSpan.FromSeconds(30);

    private readonly string name;
    private readonly double millisecondsMax;

    private DateTime lastTriggeredTime;
    private Stopwatch stopwatch = new();

    /// <summary>
    /// Initializes a new instance of the <see cref="HitchDetector"/> class.
    /// </summary>
    /// <param name="name">Name to log.</param>
    /// <param name="millisecondsMax">Milliseconds to print a warning for.</param>
    public HitchDetector(string name, double millisecondsMax = 20)
    {
        this.name = name;
        this.millisecondsMax = millisecondsMax;

        this.lastTriggeredTime = DateTime.Now - this.cooldownTime;
    }

    /// <summary>
    /// Start the time tracking.
    /// </summary>
    public void Start()
    {
        this.stopwatch.Restart();
    }

    /// <summary>
    /// Stop the time tracking, and print a warning, if applicable.
    /// </summary>
    public void Stop()
    {
        this.stopwatch.Stop();

        if (this.stopwatch.Elapsed.TotalMilliseconds > this.millisecondsMax &&
            DateTime.Now - this.lastTriggeredTime > this.cooldownTime)
        {
            Log.Warning("[HITCH] Long {Name} detected, {Total}ms > {Max}ms - check in the plugin stats window.", this.name, this.stopwatch.Elapsed.TotalMilliseconds, this.millisecondsMax);
            this.lastTriggeredTime = DateTime.Now;
        }
    }
}
