using System.Collections.Generic;
using System.Linq;
using System.Text;

using Dalamud.Utility;

using TerraFX.Interop.Windows;

using static TerraFX.Interop.Windows.Windows;

namespace Dalamud.Memory;

/// <summary>
/// A single region of the address space.
/// </summary>
/// <param name="Start">Base address of the region.</param>
/// <param name="Size">Size of the region in bytes.</param>
/// <param name="State">MEM_* state of the region.</param>
/// <param name="Protect">PAGE_* protection of the region.</param>
internal readonly record struct MemoryRegion(ulong Start, ulong Size, uint State, uint Protect)
{
    /// <summary>
    /// Gets the end address of the region.
    /// </summary>
    public ulong End => this.Start + this.Size;

    /// <summary>
    /// Gets a value indicating whether this region is free.
    /// </summary>
    public bool IsFree => (this.State & MEM.MEM_FREE) != 0;

    /// <summary>
    /// Gets a value indicating whether this region is committed.
    /// </summary>
    public bool IsCommitted => (this.State & MEM.MEM_COMMIT) != 0;

    /// <summary>
    /// Gets a name for the state of this region.
    /// </summary>
    public string StateName => this.IsFree ? "FREE" : this.IsCommitted ? "COMMIT" : "RESERVE";
}

/// <summary>
/// A free block of the address space.
/// </summary>
/// <param name="Start">Start of the block.</param>
/// <param name="End">End of the block, exclusive.</param>
/// <param name="Granules">Amount of 64KB slots that fit fully into the block.</param>
internal readonly record struct FreeBlock(ulong Start, ulong End, ulong Granules);

/// <summary>
/// Statistics about the rel32 window around an address.
/// </summary>
internal struct AddressSpaceWindow
{
    /// <summary>
    /// The address the window is centered on.
    /// </summary>
    public ulong Anchor;

    /// <summary>
    /// The start of the window.
    /// </summary>
    public ulong WindowStart;

    /// <summary>
    /// The end of the window.
    /// </summary>
    public ulong WindowEnd;

    /// <summary>
    /// The size of the window or 0 if no window could be calculated.
    /// </summary>
    public ulong WindowSize;

    /// <summary>
    /// The amount of free bytes in the window, including bytes that cannot actually be allocated.
    /// </summary>
    public ulong Free;

    /// <summary>
    /// The amount of reserved bytes in the window.
    /// </summary>
    public ulong Reserved;

    /// <summary>
    /// The amount of committed bytes in the window.
    /// </summary>
    public ulong Committed;

    /// <summary>
    /// The amount of 64KB slots that can still be allocated in the window.
    /// </summary>
    public ulong AllocatableGranules;

    /// <summary>
    /// The size of the largest allocatable run in the window.
    /// </summary>
    public ulong LargestFreeRun;
}

/// <summary>
/// Helpers for inspecting how much of the address space around a given address is still usable.
/// </summary>
internal static unsafe class AddressSpaceAnalysis
{
    /// <summary>
    /// The allocation granularity of the address space.
    /// </summary>
    public const ulong Granularity = 0x10000;

    /// <summary>
    /// The reach of a relative jump (required for hook trampolines).
    /// </summary>
    public const ulong RelativeJumpReach = int.MaxValue;

    /// <summary>
    /// Walk the entire address space of the current process.
    /// </summary>
    /// <returns>All regions in the process.</returns>
    public static List<MemoryRegion> ScanRegions()
    {
        var regions = new List<MemoryRegion>();

        MEMORY_BASIC_INFORMATION mbi;
        ulong address = 0;

        while (VirtualQuery((void*)address, &mbi, (nuint)sizeof(MEMORY_BASIC_INFORMATION)) != 0)
        {
            var start = (ulong)mbi.BaseAddress;
            var size = (ulong)mbi.RegionSize;

            if (size == 0)
                break;

            regions.Add(new MemoryRegion(start, size, mbi.State, mbi.Protect));

            var next = start + size;
            if (next <= address)
                break;

            address = next;
        }

        return regions;
    }

    /// <summary>
    /// Get stats about the relative jump window around an address.
    /// </summary>
    /// <param name="anchor">The address to center the window on.</param>
    /// <param name="regions">Previously scanned regions.</param>
    /// <returns>Statistics about the window.</returns>
    public static AddressSpaceWindow AnalyzeWindow(ulong anchor, IReadOnlyList<MemoryRegion> regions)
    {
        if (anchor == 0)
            return default;

        var windowStart = anchor > RelativeJumpReach ? anchor - RelativeJumpReach : 0;
        var windowEnd = anchor + RelativeJumpReach;

        var result = new AddressSpaceWindow
        {
            Anchor = anchor,
            WindowStart = windowStart,
            WindowEnd = windowEnd,
        };

        foreach (var region in regions)
        {
            var start = Math.Max(region.Start, windowStart);
            var end = Math.Min(region.End, windowEnd);

            if (end <= start)
                continue;

            var length = end - start;

            if (region.IsFree)
            {
                result.Free += length;

                var (count, _) = CountGranules(start, end);
                result.AllocatableGranules += count;

                if (count * Granularity > result.LargestFreeRun)
                    result.LargestFreeRun = count * Granularity;
            }
            else if (region.IsCommitted)
            {
                result.Committed += length;
            }
            else
            {
                result.Reserved += length;
            }
        }

        result.WindowSize = windowEnd - windowStart;
        return result;
    }

    /// <summary>
    /// Get the free blocks inside a window, ordered by the amount of 64KB slots they can hand out.
    /// </summary>
    /// <param name="regions">Previously scanned regions.</param>
    /// <param name="window">The window to clip the regions to.</param>
    /// <param name="limit">The maximum amount of blocks to return.</param>
    /// <returns>The largest free blocks in the window.</returns>
    public static List<FreeBlock> GetLargestFreeBlocks(
        IReadOnlyList<MemoryRegion> regions, AddressSpaceWindow window, int limit)
    {
        return regions
               .Where(x => x.IsFree)
               .Select(x => (Start: Math.Max(x.Start, window.WindowStart), End: Math.Min(x.End, window.WindowEnd)))
               .Where(x => x.End > x.Start)
               .Select(x => new FreeBlock(x.Start, x.End, CountGranules(x.Start, x.End).Count))
               .OrderByDescending(x => x.Granules)
               .Take(limit)
               .ToList();
    }

    /// <summary>
    /// Find the region a given address falls into.
    /// </summary>
    /// <param name="regions">Previously scanned regions.</param>
    /// <param name="address">The address to look up.</param>
    /// <returns>The region containing the address or null if we could not find it.</returns>
    public static MemoryRegion? FindRegion(IReadOnlyList<MemoryRegion> regions, ulong address)
    {
        foreach (var region in regions)
        {
            if (address >= region.Start && address < region.End)
                return region;
        }

        return null;
    }

    /// <summary>
    /// Count the 64KB granule-aligned slots that fit fully into a range.
    /// </summary>
    /// <param name="start">Start of the range, inclusive.</param>
    /// <param name="end">End of the range, exclusive.</param>
    /// <returns>The amount of slots and the address of the first one.</returns>
    public static (ulong Count, ulong First) CountGranules(ulong start, ulong end)
    {
        if (end <= start || end - start < Granularity)
            return (0, 0);

        var first = (start + Granularity - 1) & ~(Granularity - 1);
        var last = (end - Granularity) & ~(Granularity - 1);

        if (last < first)
            return (0, 0);

        return (((last - first) / Granularity) + 1, first);
    }

    /// <summary>
    /// Append a full dump of all regions to the passed String Builder.
    /// </summary>
    /// <param name="sb">The builder to append to.</param>
    /// <param name="regions">Previously scanned regions.</param>
    public static void AppendRegionDump(StringBuilder sb, IReadOnlyList<MemoryRegion> regions)
    {
        foreach (var region in regions)
        {
            sb.AppendLine(
                $"0x{region.Start:X16}-0x{region.End:X16} {region.Size,16} {region.StateName,-8} " +
                $"protect=0x{region.Protect:X}");
        }
    }

    /// <summary>
    /// Build a human-readable report about the state of the address space around an address.
    /// </summary>
    /// <param name="address">The address to center the report on.</param>
    /// <param name="freeBlockCount">The amount of free blocks to list.</param>
    /// <returns>The report.</returns>
    public static string BuildReport(nint address, int freeBlockCount = 10)
    {
        var anchor = (ulong)address;
        var regions = ScanRegions();
        var window = AnalyzeWindow(anchor, regions);

        var sb = new StringBuilder();
        sb.AppendLine($"Address space around {Util.DescribeAddress(address)}. {regions.Count} regions total.");

        if (FindRegion(regions, anchor) is { } target)
        {
            sb.AppendLine(
                $"Target region: 0x{target.Start:X}-0x{target.End:X} ({Util.FormatBytes(target.Size)}) " +
                $"{target.StateName} protect=0x{target.Protect:X}");
        }
        else
        {
            sb.AppendLine("Target region: not mapped");
        }

        if (window.WindowSize == 0)
        {
            sb.AppendLine("Could not compute a rel32 window for this address.");
            return sb.ToString();
        }

        var allocatable = window.AllocatableGranules * Granularity;
        var stranded = window.Free > allocatable ? window.Free - allocatable : 0;
        var exhaustion = 1.0 - (allocatable / (double)window.WindowSize);

        sb.AppendLine(
            $"rel32 window: 0x{window.WindowStart:X}-0x{window.WindowEnd:X} ({Util.FormatBytes(window.WindowSize)}), " +
            $"{exhaustion * 100:F2}% exhausted");
        sb.AppendLine(
            $"  Allocatable:      {Util.FormatBytes(allocatable)} in {window.AllocatableGranules} granules of 64KB");
        sb.AppendLine($"  Largest run:      {Util.FormatBytes(window.LargestFreeRun)}");
        sb.AppendLine($"  Free (raw):       {Util.FormatBytes(window.Free)}");
        sb.AppendLine($"  Free but stranded: {Util.FormatBytes(stranded)}");
        sb.AppendLine($"  Reserved:         {Util.FormatBytes(window.Reserved)}");
        sb.AppendLine($"  Committed:        {Util.FormatBytes(window.Committed)}");

        if (window.AllocatableGranules == 0)
        {
            sb.AppendLine(
                "No 64KB slot free, no hook possible");
        }

        var blocks = GetLargestFreeBlocks(regions, window, freeBlockCount);
        if (blocks.Count > 0)
        {
            sb.AppendLine($"Largest {blocks.Count} free blocks in window:");
            foreach (var block in blocks)
            {
                sb.AppendLine(
                    $"  0x{block.Start:X16}-0x{block.End:X16} {Util.FormatBytes(block.End - block.Start),12} " +
                    $"{block.Granules} slots");
            }
        }

        return sb.ToString();
    }
}
