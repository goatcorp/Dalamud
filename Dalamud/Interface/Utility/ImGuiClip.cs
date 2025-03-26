using System.Collections.Generic;
using System.Linq;
using System.Numerics;

using Dalamud.Interface.Utility.Raii;
using ImGuiNET;

namespace Dalamud.Interface.Utility;

public static class ImGuiClip
{
    // Get the number of skipped items of a given height necessary for the current scroll bar,
    // and apply the dummy of the appropriate height, removing one item spacing.
    // The height has to contain the spacing.
    public static int GetNecessarySkips(float height)
    {
        var curY  = ImGui.GetScrollY();
        var skips = (int)(curY / height);
        if (skips > 0)
            ImGui.Dummy(new Vector2(1, skips * height - ImGui.GetStyle().ItemSpacing.Y));

        return skips;
    }

    // Draw the dummy for the remaining items computed by ClippedDraw,
    // removing one item spacing.
    public static void DrawEndDummy(int remainder, float height)
    {
        if (remainder > 0)
            ImGui.Dummy(new Vector2(1, remainder * height - ImGui.GetStyle().ItemSpacing.Y));
    }

    // Draw a clipped random-access collection of consistent height lineHeight.
    // Uses ImGuiListClipper and thus handles start- and end-dummies itself.
    public static void ClippedDraw<T>(IReadOnlyList<T> data, Action<T> draw, float lineHeight)
    {
        ImGuiListClipperPtr clipper;
        unsafe
        {
            clipper = new ImGuiListClipperPtr(ImGuiNative.ImGuiListClipper_ImGuiListClipper());
        }

        clipper.Begin(data.Count, lineHeight);
        while (clipper.Step())
        {
            for (var actualRow = clipper.DisplayStart; actualRow < clipper.DisplayEnd; actualRow++)
            {
                if (actualRow >= data.Count)
                    return;

                if (actualRow < 0)
                    continue;

                draw(data[actualRow]);
            }
        }

        clipper.End();
        clipper.Destroy();
    }

    /// <summary>
    /// Draws the enumerable data with <paramref name="itemsPerLine"/> number of items per line.
    /// </summary>
    /// <param name="data">Enumerable containing data to draw.</param>
    /// <param name="draw">The function to draw a single item.</param>
    /// <param name="itemsPerLine">How many items to draw per line.</param>
    /// <param name="lineHeight">How tall each line is.</param>
    /// <typeparam name="T">The type of data to draw.</typeparam>
    public static void ClippedDraw<T>(IReadOnlyList<T> data, Action<T> draw, int itemsPerLine, float lineHeight)
    {
        ImGuiListClipperPtr clipper;
        unsafe
        {
            clipper = new ImGuiListClipperPtr(ImGuiNative.ImGuiListClipper_ImGuiListClipper());
        }
        
        var maxRows = (int)MathF.Ceiling((float)data.Count / itemsPerLine);
        
        clipper.Begin(maxRows, lineHeight);
        while (clipper.Step())
        {
            for (var actualRow = clipper.DisplayStart; actualRow < clipper.DisplayEnd; actualRow++)
            {
                if (actualRow >= maxRows)
                    return;

                if (actualRow < 0)
                    continue;

                var itemsForRow = data
                                  .Skip(actualRow * itemsPerLine)
                                  .Take(itemsPerLine);
                
                var currentIndex = 0;
                foreach (var item in itemsForRow)
                {
                    if (currentIndex++ != 0 && currentIndex < itemsPerLine + 1)
                    {
                        ImGui.SameLine();
                    }
                    
                    draw(item);
                }
            }
        }

        clipper.End();
        clipper.Destroy();
    }

    // Draw a clipped random-access collection of consistent height lineHeight.
    // Uses ImGuiListClipper and thus handles start- and end-dummies itself, but acts on type and index.
    public static void ClippedDraw<T>(IReadOnlyList<T> data, Action<T, int> draw, float lineHeight)
    {
        ImGuiListClipperPtr clipper;
        unsafe
        {
            clipper = new ImGuiListClipperPtr(ImGuiNative.ImGuiListClipper_ImGuiListClipper());
        }

        clipper.Begin(data.Count, lineHeight);
        while (clipper.Step())
        {
            for (var actualRow = clipper.DisplayStart; actualRow < clipper.DisplayEnd; actualRow++)
            {
                if (actualRow >= data.Count)
                    return;

                if (actualRow < 0)
                    continue;

                draw(data[actualRow], actualRow);
            }
        }

        clipper.End();
        clipper.Destroy();
    }

    // Draw non-random-access data without storing state.
    // Use GetNecessarySkips first and use its return value for skips.
    // startIndex can be set if using multiple separate chunks of data with different filter or draw functions (of the same height).
    // Returns either the non-negative remaining objects in data that could not be drawn due to being out of the visible area,
    // if count was given this will be subtracted instead of counted,
    // or the bitwise-inverse of the next startIndex for subsequent collections, if there is still room for more visible objects.
    public static int ClippedDraw<T>(IEnumerable<T> data, int skips, Action<T> draw, int? count = null, int startIndex = 0)
    {
        if (count != null && count.Value + startIndex <= skips)
            return ~(count.Value + startIndex);

        using var it      = data.GetEnumerator();
        var       visible = false;
        var       idx     = startIndex;
        while (it.MoveNext())
        {
            if (idx >= skips)
            {
                using var group = ImRaii.Group();
                draw(it.Current);
                group.Dispose();
                if (!ImGui.IsItemVisible())
                {
                    if (visible)
                    {
                        if (count != null)
                            return Math.Max(0, count.Value - idx + startIndex - 1);

                        var remainder = 0;
                        while (it.MoveNext())
                            ++remainder;

                        return remainder;
                    }
                }
                else
                {
                    visible = true;
                }
            }

            ++idx;
        }

        return ~idx;
    }

    // Draw non-random-access data that gets filtered without storing state.
    // Use GetNecessarySkips first and use its return value for skips.
    // checkFilter should return true for items that should be displayed and false for those that should be skipped.
    // startIndex can be set if using multiple separate chunks of data with different filter or draw functions (of the same height).
    // Returns either the non-negative remaining objects in data that could not be drawn due to being out of the visible area,
    // or the bitwise-inverse of the next startIndex for subsequent collections, if there is still room for more visible objects.
    public static int FilteredClippedDraw<T>(IEnumerable<T> data, int skips, Func<T, bool> checkFilter, Action<T> draw, int startIndex = 0)
        => ClippedDraw(data.Where(checkFilter), skips, draw, null, startIndex);
}
