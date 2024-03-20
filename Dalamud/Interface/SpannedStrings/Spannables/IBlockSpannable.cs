namespace Dalamud.Interface.SpannedStrings.Spannables;

/// <summary>A spannable that can provide an instance of <see cref="SpannedStringData"/>.</summary>
public interface IBlockSpannable
{
    /// <summary>Gets the data.</summary>
    /// <returns>The data.</returns>
    SpannedStringData GetData();
}
