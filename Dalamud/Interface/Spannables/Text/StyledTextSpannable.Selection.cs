using System.Collections.Generic;

namespace Dalamud.Interface.Spannables.Text;

#pragma warning disable SA1010

/// <summary>Spannable dealing with <see cref="StyledText"/>.</summary>
public sealed partial class StyledTextSpannable
{
    private readonly List<(int Begin, int End, int Caret)> selections = [];

    /// <summary>Inserts styled text to all current selections.</summary>
    /// <param name="st">Styled text to insert.</param>
    public void InsertAfterCarets(StyledText st)
    {
        for (var i = 0; i < this.selections.Count; i++)
        {
            this.dataMemory
            this.selections[i].Caret
        }
    }

    /// <summary>Adds a selection range.</summary>
    /// <param name="begin">Beginning text offset of the selection.</param>
    /// <param name="end">Ending text offset of the selection.</param>
    /// <param name="caret">Offset of the caret.</param>
    public void AddSelection(int begin, int end, int caret)
    {
        if (begin > end)
            (begin, end) = (end, begin);

        var i = this.selections.BinarySearch((begin, end, caret));
        if (i >= 0)
            return;
        i = ~i;

        var mergeNext = i < this.selections.Count && this.selections[i].Begin < end;
        var mergePrev = i > 0 && this.selections[i - 1].End > begin;
        if (mergeNext && mergePrev)
        {
            this.selections[i - 1] = (
                                         Math.Min(this.selections[i - 1].Begin, begin),
                                         Math.Max(this.selections[i].End, end),
                                         caret);
            this.selections.RemoveAt(i);
            return;
        }

        if (mergeNext)
        {
            this.selections[i] = (
                                     Math.Min(this.selections[i].Begin, begin),
                                     Math.Max(this.selections[i].End, end),
                                     caret);
            return;
        }

        if (mergePrev)
        {
            this.selections[i - 1] = (
                                         Math.Min(this.selections[i - 1].Begin, begin),
                                         Math.Max(this.selections[i - 1].End, end),
                                         caret);
            return;
        }

        this.selections.Insert(i, (begin, end, caret));
    }

    /// <summary>Sets the selection range.</summary>
    /// <param name="begin">Beginning text offset of the selection.</param>
    /// <param name="end">Ending text offset of the selection.</param>
    /// <param name="caret">Offset of the caret.</param>
    public void SetSelection(int begin, int end, int caret)
    {
        if (begin > end)
            (begin, end) = (end, begin);

        this.selections.Clear();
        this.selections.Add((begin, end, caret));
    }

    /// <summary>Removes a selection range.</summary>
    /// <param name="begin">Beginning text offset of the selection.</param>
    /// <param name="end">Ending text offset of the selection.</param>
    public void RemoveSelection(int begin, int end)
    {
        var i = this.selections.BinarySearch((begin, end, begin));
        if (i >= 0)
        {
            this.selections.RemoveAt(i);
            return;
        }

        i = ~i;
        for (var j = i; j >= 0; j--)
        {
            if (this.selections[j].End < begin)
                break;
            if (this.selections[j].Begin > begin)
            {
                i--;
                this.selections.RemoveAt(j);
            }
            else
            {
                this.selections[j] = (this.selections[j].Begin, end, Math.Min(this.selections[j].Caret, end));
                break;
            }
        }

        for (; i < this.selections.Count; i++)
        {
            if (this.selections[i].Begin > end)
                break;
            if (this.selections[i].End < end)
            {
                i--;
                this.selections.RemoveAt(i);
            }
            else
            {
                this.selections[i] = (begin, this.selections[i].End, Math.Max(this.selections[i].Caret, begin));
                break;
            }
        }
    }

    /// <summary>Clears the selection range.</summary>
    public void ClearSelection()
    {
        this.selections.Clear();
    }
}
