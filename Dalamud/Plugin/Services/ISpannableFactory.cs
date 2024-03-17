using Dalamud.Interface.SpannedStrings;
using Dalamud.Interface.SpannedStrings.Internal;

using ImGuiNET;

namespace Dalamud.Plugin.Services;

/// <summary>Factory for custom text rendering.</summary>
public interface ISpannableFactory
{
    /// <summary>Rents an instance of the <see cref="SpannedStringRenderer"/> class.</summary>
    /// <param name="usage">The usage.<ul>
    /// <li>Specify a <see cref="string"/>, <see cref="ReadOnlySpan{T}"/> of <see cref="char"/> or <see cref="byte"/>,
    /// or an integer to use it as an interactable item.</li>
    /// <li>Specify <c>true</c> to draw to the current window, but not make it interactable.</li>
    /// <li>Specify a <see cref="ImDrawListPtr"/> to only draw, without moving the cursor after doing so.</li>
    /// <li>Specify <c>default</c> or <c>false</c> to measure.</li>
    /// </ul></param>
    /// <param name="options">The renderer parameters. If null, current ImGui state will be checked to deduce
    /// default values.</param>
    /// <returns>The rented renderer.</returns>
    /// <exception cref="InvalidOperationException">Thrown if called outside the main thread.</exception>
    ISpannedStringRenderer Rent(ISpannedStringRenderer.Usage usage, ISpannedStringRenderer.Options options = default);
}
