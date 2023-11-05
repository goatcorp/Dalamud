using System.Collections.Generic;
using System.Linq;

namespace Dalamud.Interface.Utility;

/// <summary>
/// Helper class for building ImGui font ranges.
/// </summary>
internal sealed class ImGuiRangeBuilder
{
    private readonly List<(ushort, ushort)> codepointRanges = new();
    private readonly List<ushort> flattenedRanges = new();

    /// <summary>
    /// Reset.
    /// </summary>
    /// <returns>This.</returns>
    public ImGuiRangeBuilder WithClear()
    {
        this.codepointRanges.Clear();
        this.flattenedRanges.Clear();
        return this;
    }

    /// <inheritdoc cref="List{T}.EnsureCapacity"/>
    /// <returns>This.</returns>
    public ImGuiRangeBuilder WithEnsureCapacity(int n)
    {
        this.codepointRanges.EnsureCapacity(n);
        return this;
    }

    /// <summary>
    /// Add the given characters to ranges.
    /// </summary>
    /// <param name="c">Characters.</param>
    /// <returns>This.</returns>
    public ImGuiRangeBuilder With(params ushort[] c)
    {
        this.codepointRanges.AddRange(c.Select(x => (x, x)));
        return this;
    }

    /// <summary>
    /// Add the given range to ranges.
    /// </summary>
    /// <param name="l">Range start (inclusive).</param>
    /// <param name="r">Range end (inclusive).</param>
    /// <returns>This.</returns>
    public ImGuiRangeBuilder WithRange(ushort l, ushort r)
    {
        this.codepointRanges.Add((l, r));
        return this;
    }
    
    /// <summary>
    /// Add the given ranges to ranges.
    /// </summary>
    /// <param name="ranges">Ranges.</param>
    /// <returns>This.</returns>
    public ImGuiRangeBuilder WithRanges(IEnumerable<(ushort From, ushort To)> ranges)
    {
        this.codepointRanges.AddRange(ranges);
        return this;
    }

    /// <summary>
    /// Build an array containing desired ImGui font range.
    /// </summary>
    /// <returns>Built font range for use with ImGui.</returns>
    public ushort[] Build()
    {
        this.codepointRanges.Sort();
        foreach (var range in this.codepointRanges)
        {
            if (this.flattenedRanges.Any() && this.flattenedRanges[^1] >= range.Item1 - 1)
            {
                this.flattenedRanges[^1] = Math.Max(this.flattenedRanges[^1], range.Item2);
            }
            else
            {
                this.flattenedRanges.Add(range.Item1);
                this.flattenedRanges.Add(range.Item2);
            }
        }

        this.flattenedRanges.Add(0);
        return this.flattenedRanges.ToArray();
    }
}
