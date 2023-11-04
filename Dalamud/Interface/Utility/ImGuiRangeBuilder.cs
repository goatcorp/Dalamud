using System.Collections.Generic;
using System.Linq;

namespace Dalamud.Interface.Utility;

internal sealed class ImGuiRangeBuilder
{
    private readonly List<(ushort, ushort)> codepointRanges = new();
    private readonly List<ushort> flattenedRanges = new();

    public ImGuiRangeBuilder WithClear()
    {
        this.codepointRanges.Clear();
        this.flattenedRanges.Clear();
        return this;
    }

    public ImGuiRangeBuilder WithEnsureCapacity(int n)
    {
        this.codepointRanges.EnsureCapacity(n);
        return this;
    }

    public ImGuiRangeBuilder With(params ushort[] c)
    {
        this.codepointRanges.AddRange(c.Select(x => (x, x)));
        return this;
    }

    public ImGuiRangeBuilder WithRange(ushort l, ushort r)
    {
        this.codepointRanges.Add((l, r));
        return this;
    }

    public ImGuiRangeBuilder WithRanges(IEnumerable<(ushort From, ushort To)> ranges)
    {
        this.codepointRanges.AddRange(ranges);
        return this;
    }

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
