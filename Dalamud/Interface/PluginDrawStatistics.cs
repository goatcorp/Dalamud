using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

using Dalamud.Bindings.ImGui;

namespace Dalamud.Interface;

/// <summary> 
/// Tracks draw timing statistics and resource stack leaks for a plugin's rendering operations.
/// </summary>
internal sealed class PluginDrawStatistics
{
    private readonly Stopwatch stopwatch = new();
    private readonly Queue<long> drawTimeHistory = [];

    private int styleStack;
    private int colorStack;
    private int fontStack;
    private short disabledStack;

    /// <summary>
    /// Gets the time this plugin took to draw on the last frame.
    /// </summary>
    public long LastDrawTime { get; private set; } = -1;

    /// <summary>
    /// Gets the longest amount of time this plugin ever took to draw.
    /// </summary>
    public long MaxDrawTime { get; private set; } = -1;

    /// <summary> 
    /// Gets the average draw time.
    /// </summary>
    public long AverageDrawTime { get; private set; } = -1;

    /// <summary> Gets the total amount of leaked style variables from this plugin. </summary>
    public int LeakedStyles { get; private set; }

    /// <summary> Gets the total amount of leaked colors from this plugin. </summary>
    public int LeakedColors { get; private set; }

    /// <summary> Gets the total amount of leaked fonts from this plugin. </summary>
    public int LeakedFonts { get; private set; }

    /// <summary> Gets the total amount of leaked disabled states from this plugin. </summary>
    public int LeakedDisableds { get; private set; }

    /// <summary> Gets the total amount of leaked stack values from this plugin. </summary>
    public int LeakedStacks { get; private set; }

    /// <summary> Initialize the data tracking before the actual draw operation. </summary>
    public void StartUpdate()
    {
        this.stopwatch.Restart();
        var context = ImGui.GetCurrentContext();
        this.styleStack = context.StyleVarStack.Size;
        this.colorStack = context.ColorStack.Size;
        this.fontStack = context.FontStack.Size;
        this.disabledStack = context.DisabledStackSize;
        this.drawTimeHistory.EnsureCapacity(100);
    }

    /// <summary> Finalize and update the tracked data after the actual draw operation. </summary>
    public void EndUpdate()
    {
        this.stopwatch.Stop();
        this.LastDrawTime = this.stopwatch.ElapsedTicks;
        this.MaxDrawTime = Math.Max(this.LastDrawTime, this.MaxDrawTime);
        while (this.drawTimeHistory.Count >= 100) this.drawTimeHistory.Dequeue();
        this.drawTimeHistory.Enqueue(this.LastDrawTime);
        this.AverageDrawTime = this.drawTimeHistory.Sum() / this.drawTimeHistory.Count;

        var context = ImGui.GetCurrentContext();
        if (this.styleStack < context.StyleVarStack.Size)
            this.LeakedStyles += context.StyleVarStack.Size - this.styleStack;
        if (this.colorStack < context.ColorStack.Size)
            this.LeakedColors += context.ColorStack.Size - this.colorStack;
        if (this.fontStack < context.FontStack.Size)
            this.LeakedFonts += context.FontStack.Size - this.fontStack;
        if (this.disabledStack < context.DisabledStackSize)
            this.LeakedDisableds += context.DisabledStackSize - this.disabledStack;
        this.LeakedStacks = this.LeakedStyles + this.LeakedColors + this.LeakedFonts + this.LeakedDisableds;
    }

    /// <summary> Clear all tracking data. </summary>
    public void Reset()
    {
        this.AverageDrawTime = -1;
        this.LastDrawTime = -1;
        this.MaxDrawTime = -1;
        this.drawTimeHistory.Clear();
        this.LeakedStyles = 0;
        this.LeakedColors = 0;
        this.LeakedFonts = 0;
        this.LeakedDisableds = 0;
        this.LeakedStacks = 0;
    }
}
