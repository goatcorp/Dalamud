using Dalamud.Interface.SpannedStrings.Internal;

namespace Dalamud.Interface.SpannedStrings;

/// <summary>A spannable that can provide an instance of <see cref="SpannedStringData"/>.</summary>
internal interface ISpannableDataProvider
{
    /// <summary>Gets the data.</summary>
    /// <returns>The data.</returns>
    SpannedStringData GetData();
}
